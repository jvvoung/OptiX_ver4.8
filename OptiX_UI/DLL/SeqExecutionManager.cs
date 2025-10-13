using System;
using System.Collections.Generic;
using System.Diagnostics;
using OptiX.Common;

namespace OptiX.DLL
{
    // SEQ 실행 및 Zone 시간/결과 관리 클래스
    public static class SeqExecutionManager
    {
        // SEQ 전체 시퀀스 시작 시간
        private static DateTime _seqStartTime;
        
        // SEQ 전체 시퀀스 종료 시간
        private static DateTime _seqEndTime;
        
        // Zone별 SEQ 시작 시간 (Zone 번호 -> 시작 시간)
        private static Dictionary<int, DateTime> _zoneSeqStartTimes = new Dictionary<int, DateTime>();
        
        // Zone 종료 시간 키 오프셋 (시작/종료 시간 구분용, 예: Zone 1 시작 = 1, 종료 = 1001)
        private const int ZONE_END_TIME_OFFSET = DllConstants.ZONE_END_TIME_OFFSET;
        
        // 현재 실행 중인 Zone 번호 (1-based)
        private static int _currentZone = 1;
        
        // Zone별 DLL 실행 결과 저장
        private static Dictionary<int, Output> _zoneResults = new Dictionary<int, Output>();
        
        // 저장된 Zone 결과를 가져오는 함수
        public static Output? GetStoredZoneResult(int zoneNumber)
        {
            lock (_zoneResults)
            {
                if (_zoneResults.ContainsKey(zoneNumber))
                {
                    return _zoneResults[zoneNumber];
                }
                else
                {
                    return null;
                }
            }
        }
        
        // Zone 결과를 직접 설정하는 함수
        public static void SetZoneResult(int zoneNumber, Output output)
        {
            lock (_zoneResults)
            {
                _zoneResults[zoneNumber] = output;
            }
        }
        
        // SEQ 함수명을 받아 해당 DLL 함수 호출
        public static bool ExecuteMapped(string functionName, int? arg, int zoneNumber = 1)
        {
            if (!DllManager.IsInitialized)
                throw new InvalidOperationException("DLL이 초기화되지 않았습니다.");

            try
            {
                // Zone 번호 저장 (MAKE_RESULT_LOG에서 사용)
                _currentZone = zoneNumber - 1; // 0-based로 변환하여 저장
                
                // SEQ 시작 시간 설정 (첫 번째 함수 호출 시)
                if (_seqStartTime == default(DateTime))
                {
                    _seqStartTime = DateTime.Now;
                }
                
                switch (functionName?.Trim().ToUpperInvariant())
                {
                    case "PGTURN":
                        return DllFunctions.CallPGTurn(arg ?? 0);
                    case "MEASTURN":
                        return DllFunctions.CallMeasTurn(arg ?? 0);
                    case "PGPATTERN":
                        return DllFunctions.CallPGPattern(arg ?? 0);
                    case "MEAS":
                    {
                        // MEAS는 실제 DLL 호출하지 않음
                        return true;
                    }
                    case "MTP":
                    {
                        Debug.WriteLine($"[Zone {zoneNumber}] MTP 실행 시작");
                        Common.ErrorLogger.Log($"MTP 실행 시작", Common.ErrorLogger.LogLevel.INFO, zoneNumber);
                        
                        // Zone별 CELL_ID, INNER_ID 가져오기 (전역 변수에서 - INI 파일 읽기 없음)
                        var (cellId, innerId) = GlobalDataManager.GetZoneInfo(zoneNumber);
                        
                        var input = new Input
                        {
                            CELL_ID = cellId,
                            INNER_ID = innerId,
                            total_point = DllConstants.DEFAULT_CURRENT_POINT,
                            cur_point = DllConstants.DEFAULT_CURRENT_POINT
                        };
                        
                        var (output, ok) = DllFunctions.CallMTPTestFunction(input);
                        
                        // 실제 DLL 결과를 Zone별로 저장
                        if (ok)
                        {
                            lock (_zoneResults)
                            {
                                _zoneResults[zoneNumber] = output;
                            }
                            Debug.WriteLine($"[Zone {zoneNumber}] MTP 완료 (성공)");
                            Common.ErrorLogger.Log($"MTP 완료 (성공)", Common.ErrorLogger.LogLevel.INFO, zoneNumber);
                        }
                        else
                        {
                            Debug.WriteLine($"[Zone {zoneNumber}] MTP 완료 (실패)");
                            Common.ErrorLogger.Log($"MTP 완료 (실패)", Common.ErrorLogger.LogLevel.WARNING, zoneNumber);
                        }
                        
                        return ok;
                    }
                    case "IPVS":
                    {
                        Debug.WriteLine($"[Zone {zoneNumber}] IPVS 실행 시작");
                        Common.ErrorLogger.Log($"IPVS 실행 시작", Common.ErrorLogger.LogLevel.INFO, zoneNumber);
                        
                        // Zone별 CELL_ID, INNER_ID 가져오기 (IPVS 전용)
                        var (cellId, innerId) = GlobalDataManager.GetIPVSZoneInfo(zoneNumber);
                        
                        var input = new Input
                        {
                            CELL_ID = cellId,
                            INNER_ID = innerId,
                            total_point = DllConstants.DEFAULT_IPVS_TOTAL_POINT,  // IPVS는 5개 포인트
                            cur_point = DllConstants.DEFAULT_CURRENT_POINT
                        };
                        
                        Debug.WriteLine($"[Zone {zoneNumber}] IPVS Input - Cell ID: '{cellId}', Inner ID: '{innerId}'");
                        Common.ErrorLogger.Log($"IPVS Input - Cell ID: '{cellId}', Inner ID: '{innerId}'", Common.ErrorLogger.LogLevel.DEBUG, zoneNumber);
                        
                        var (output, ok) = DllFunctions.CallIPVSTestFunction(input);
                        
                        // 실제 DLL 결과를 Zone별로 저장
                        if (ok)
                        {
                            lock (_zoneResults)
                            {
                                _zoneResults[zoneNumber] = output;
                            }
                            Debug.WriteLine($"[Zone {zoneNumber}] IPVS 완료 (성공)");
                            Common.ErrorLogger.Log($"IPVS 완료 (성공)", Common.ErrorLogger.LogLevel.INFO, zoneNumber);
                        }
                        else
                        {
                            Debug.WriteLine($"[Zone {zoneNumber}] IPVS 완료 (실패)");
                            Common.ErrorLogger.Log($"IPVS 완료 (실패)", Common.ErrorLogger.LogLevel.WARNING, zoneNumber);
                        }
                        
                        return ok;
                    }
                    case "MAKE_RESULT_LOG":
                    {
                        // MAKE_RESULT_LOG는 모든 SEQ 완료 후 일괄 처리
                        return true;
                    }
                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Zone {zoneNumber}] {functionName} 예외: {ex.Message}");
                ErrorLogger.LogException(ex, $"SEQ 실행 중 예외 - Function: {functionName}", zoneNumber);
                return false;
            }
        }
        
        // SEQ 시작/종료 시간 리셋
        public static void ResetSeqStartTime()
        {
            _seqStartTime = default(DateTime);
            _seqEndTime = default(DateTime);
            lock (_zoneSeqStartTimes)
            {
                _zoneSeqStartTimes.Clear();
            }
        }
        
        // SEQ 시작 시간 가져오기
        public static DateTime GetSeqStartTime()
        {
            return _seqStartTime;
        }
        
        // SEQ 종료 시간 설정
        public static void SetSeqEndTime(DateTime endTime)
        {
            _seqEndTime = endTime;
        }
        
        // SEQ 종료 시간 가져오기
        public static DateTime GetSeqEndTime()
        {
            return _seqEndTime;
        }
        
        // Zone별 SEQ 시작 시간 설정
        public static void SetZoneSeqStartTime(int zoneNumber, DateTime startTime)
        {
            lock (_zoneSeqStartTimes)
            {
                _zoneSeqStartTimes[zoneNumber] = startTime;
            }
        }
        
        // Zone별 SEQ 시작 시간 가져오기
        public static DateTime GetZoneSeqStartTime(int zoneNumber)
        {
            lock (_zoneSeqStartTimes)
            {
                if (_zoneSeqStartTimes.ContainsKey(zoneNumber))
                {
                    return _zoneSeqStartTimes[zoneNumber];
                }
                else
                {
                    return _seqStartTime;
                }
            }
        }
        
        // Zone별 SEQ 종료 시간 설정
        public static void SetZoneSeqEndTime(int zoneNumber, DateTime endTime)
        {
            lock (_zoneSeqStartTimes)
            {
                _zoneSeqStartTimes[zoneNumber + ZONE_END_TIME_OFFSET] = endTime;
            }
        }
        
        // Zone별 SEQ 종료 시간 가져오기
        public static DateTime GetZoneSeqEndTime(int zoneNumber)
        {
            lock (_zoneSeqStartTimes)
            {
                int endTimeKey = zoneNumber + ZONE_END_TIME_OFFSET;
                if (_zoneSeqStartTimes.ContainsKey(endTimeKey))
                {
                    return _zoneSeqStartTimes[endTimeKey];
                }
                else
                {
                    return _seqEndTime;
                }
            }
        }
        
        // 현재 Zone 번호 설정
        public static void SetCurrentZone(int zoneNumber)
        {
            _currentZone = zoneNumber;
        }
    }
}


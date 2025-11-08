using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using OptiX.Common;

namespace OptiX.DLL
{
    //25.10.29 - Zone별 SEQ 컨텍스트 클래스 추가 (Input/Output 공유 관리)
    /// <summary>
    /// Zone별 SEQ 컨텍스트 (Input/Output 공유 관리)
    /// SEQ 시작 시 생성되며, 해당 Zone의 모든 DLL 함수가 이 컨텍스트를 공유합니다.
    /// </summary>
    public class ZoneContext
    {
        public int ZoneNumber { get; set; }
        public Input SharedInput { get; set; }
        public Output SharedOutput { get; set; }
        public DateTime SeqStartTime { get; set; }
        public DateTime SeqEndTime { get; set; }
        
        //25.11.08 - ZoneTestResult 저장 필드 추가
        public ZoneTestResult? TestResult { get; set; }
        
        /// <summary>
        /// 컨텍스트 초기화
        /// </summary>
        public void Reset()
        {
            SharedInput = new Input
            {
                CELL_ID = "",
                INNER_ID = "",
                total_point = 0,
                cur_point = 0
            };
            
            SharedOutput = new Output 
            { 
                data = new Pattern[DllConstants.OPTIC_DATA_SIZE],
                IPVS_data = new Pattern[DllConstants.IPVS_DATA_SIZE],
                measure = new Pattern[DllConstants.MAX_WAD_COUNT],
                lut = new LUTParameter[DllConstants.RGB_CHANNEL_COUNT]
            };
            
            SeqStartTime = default(DateTime);
            SeqEndTime = default(DateTime);
            TestResult = null; //25.11.08 - TestResult 초기화
        }
        
        //25.10.29 - Input 설정 메서드 추가
        /// <summary>
        /// Input 설정 (CELL_ID, INNER_ID, total_point 등)
        /// </summary>
        public void ConfigureInput(string cellId, string innerId, int totalPoint, int curPoint = 0)
        {
            // 구조체는 값 타입이므로 전체를 다시 할당해야 함
            var newInput = SharedInput;
            newInput.CELL_ID = cellId;
            newInput.INNER_ID = innerId;
            newInput.total_point = totalPoint;
            newInput.cur_point = curPoint;
            SharedInput = newInput;
        }
    }

    //25.10.29 - SEQ 실행 관리자 전체 리팩토링 (Zone 컨텍스트 기반)
    /// <summary>
    /// SEQ 실행 및 Zone 컨텍스트 관리 클래스 (리팩토링 버전)
    /// 
    /// 주요 변경사항:
    /// - Zone별 Input/Output을 ZoneContext로 통합 관리
    /// - Thread-safe한 ConcurrentDictionary 사용
    /// - SEQ 시작 시 컨텍스트 생성, 종료 시 초기화
    /// - 모든 DLL 함수가 공유 Input/Output 사용
    /// </summary>
    public static class SeqExecutionManager
    {
        //25.10.29 - Zone별 컨텍스트 저장 (Thread-safe)
        private static ConcurrentDictionary<int, ZoneContext> _zoneContexts = new ConcurrentDictionary<int, ZoneContext>();
        
        // 전역 SEQ 시작/종료 시간
        private static DateTime _globalSeqStartTime;
        private static DateTime _globalSeqEndTime;
        
        // Lock 객체
        private static readonly object _lockObject = new object();
        
        #region Zone Context 관리
        
        //25.10.29 - Zone SEQ 시작 메서드 추가
        /// <summary>
        /// Zone SEQ 시작 - 컨텍스트 생성 및 초기화
        /// </summary>
        public static void StartZoneSeq(int zoneNumber, string cellId, string innerId, int totalPoint, bool isIPVS = false)
        {
            // 전역 SEQ 시작 시간 설정 (첫 번째 Zone)
            lock (_lockObject)
            {
                if (_globalSeqStartTime == default(DateTime))
                {
                    _globalSeqStartTime = DateTime.Now;
                }
            }
            
            // Zone 컨텍스트 생성 또는 가져오기
            var context = _zoneContexts.GetOrAdd(zoneNumber, new ZoneContext { ZoneNumber = zoneNumber });
            
            // 컨텍스트 초기화
            context.Reset();
            context.SeqStartTime = DateTime.Now;
            
            // Input 설정
            int defaultTotalPoint = isIPVS ? DllConstants.DEFAULT_IPVS_TOTAL_POINT : DllConstants.DEFAULT_CURRENT_POINT;
            context.ConfigureInput(cellId, innerId, totalPoint > 0 ? totalPoint : defaultTotalPoint);
            
            Debug.WriteLine($"[Zone {zoneNumber}] SEQ 시작 - CELL_ID: {cellId}, INNER_ID: {innerId}, total_point: {context.SharedInput.total_point}");
            ErrorLogger.Log($"Zone {zoneNumber} SEQ 시작 (CELL_ID: {cellId})", ErrorLogger.LogLevel.INFO, zoneNumber);
        }
        
        //25.10.29 - Zone SEQ 종료 메서드 추가
        /// <summary>
        /// Zone SEQ 종료 - 컨텍스트 종료 시간 기록
        /// </summary>
        public static void EndZoneSeq(int zoneNumber)
        {
            if (_zoneContexts.TryGetValue(zoneNumber, out var context))
            {
                context.SeqEndTime = DateTime.Now;
                Debug.WriteLine($"[Zone {zoneNumber}] SEQ 종료");
                ErrorLogger.Log($"Zone {zoneNumber} SEQ 종료", ErrorLogger.LogLevel.INFO, zoneNumber);
            }
            
            // 전역 SEQ 종료 시간 설정
            lock (_lockObject)
            {
                _globalSeqEndTime = DateTime.Now;
            }
        }
        
        //25.10.29 - 전체 초기화 메서드 추가
        /// <summary>
        /// 모든 Zone 컨텍스트 초기화 (전체 SEQ 종료 시)
        /// </summary>
        public static void ResetAllZones()
        {
            _zoneContexts.Clear();
            _globalSeqStartTime = default(DateTime);
            _globalSeqEndTime = default(DateTime);
            
            Debug.WriteLine("[SeqExecutionManager] 모든 Zone 컨텍스트 초기화 완료");
            ErrorLogger.Log("모든 Zone 컨텍스트 초기화", ErrorLogger.LogLevel.INFO);
        }
        
        //25.10.29 - Zone 컨텍스트 가져오기 메서드 추가
        /// <summary>
        /// 특정 Zone 컨텍스트 가져오기
        /// </summary>
        public static ZoneContext GetZoneContext(int zoneNumber)
        {
            if (_zoneContexts.TryGetValue(zoneNumber, out var context))
            {
                return context;
            }
            
            throw new InvalidOperationException($"Zone {zoneNumber}의 컨텍스트가 초기화되지 않았습니다. StartZoneSeq()를 먼저 호출하세요.");
        }
        
        /// <summary>
        /// Zone Output 가져오기 (하위 호환성)
        /// </summary>
        public static Output? GetStoredZoneResult(int zoneNumber)
        {
            if (_zoneContexts.TryGetValue(zoneNumber, out var context))
            {
                return context.SharedOutput;
            }
            return null;
        }
        
        //25.11.08 - ZoneTestResult 저장 및 가져오기 메서드 추가
        /// <summary>
        /// Zone 테스트 결과 저장 (ErrorName, Tact, Judgment 등)
        /// </summary>
        public static void StoreZoneTestResult(int zoneNumber, ZoneTestResult testResult)
        {
            if (_zoneContexts.TryGetValue(zoneNumber, out var context))
            {
                context.TestResult = testResult;
                Debug.WriteLine($"[Zone {zoneNumber}] TestResult 저장: ErrorName={testResult.ErrorName}, Tact={testResult.Tact}, Judgment={testResult.Judgment}");
            }
        }
        
        /// <summary>
        /// Zone 테스트 결과 가져오기
        /// </summary>
        public static ZoneTestResult? GetStoredZoneTestResult(int zoneNumber)
        {
            if (_zoneContexts.TryGetValue(zoneNumber, out var context))
            {
                return context.TestResult;
            }
            return null;
        }
        
        /// <summary>
        /// Zone 시작 시간 가져오기 (하위 호환성)
        /// </summary>
        public static DateTime GetZoneSeqStartTime(int zoneNumber)
        {
            if (_zoneContexts.TryGetValue(zoneNumber, out var context))
            {
                return context.SeqStartTime;
            }
            return _globalSeqStartTime;
        }
        
        /// <summary>
        /// Zone 종료 시간 가져오기 (하위 호환성)
        /// </summary>
        public static DateTime GetZoneSeqEndTime(int zoneNumber)
        {
            if (_zoneContexts.TryGetValue(zoneNumber, out var context))
            {
                return context.SeqEndTime;
            }
            return _globalSeqEndTime;
        }
        
        #endregion
        
        #region SEQ 실행
        
        //25.10.29 - ExecuteMappedAsync 리팩토링 (공유 Input/Output 사용)
        /// <summary>
        /// SEQ 함수 실행 (리팩토링 버전 - 공유 Input/Output 사용)
        /// </summary>
        public static async Task<bool> ExecuteMappedAsync(string functionName, int? arg, int zoneNumber)
        {
            if (!DllManager.IsInitialized)
                throw new InvalidOperationException("DLL이 초기화되지 않았습니다.");

            try
            {
                // Zone 컨텍스트 가져오기
                var context = GetZoneContext(zoneNumber);
                
                switch (functionName?.Trim().ToUpperInvariant())
                {
                    //25.10.29 - Task.Run() 제거 (이미 백그라운드 스레드에서 실행 중)
                    case "PGTURN":
                        return DllFunctions.CallPGTurn(arg ?? 0);

                    case "MEASTURN":
                        return DllFunctions.CallMeasTurn(arg ?? 0);

                    case "PGPATTERN":
                        return DllFunctions.CallPGPattern(arg ?? 0);

                    case "MEAS":
                        return true;
                        
                    case "MTP":
                    {
                        Debug.WriteLine($"[Zone {zoneNumber}] MTP 실행 시작 (공유 Input 사용)");
                        ErrorLogger.Log($"MTP 실행 시작 - CELL_ID: {context.SharedInput.CELL_ID}", ErrorLogger.LogLevel.INFO, zoneNumber);
                        
                        //25.10.29 - 공유 Input 사용 (매번 생성하지 않음!)
                        //25.10.29 - Task.Run() 제거 (이미 백그라운드 스레드에서 실행 중)
                        var (output, ok) = DllFunctions.CallMTPTestFunction(context.SharedInput);

                        if (ok)
                        {
                            //25.10.29 - 공유 Output에 결과 저장
                            context.SharedOutput = output;
                            Debug.WriteLine($"[Zone {zoneNumber}] MTP 완료 (성공) - Output 저장됨");
                            ErrorLogger.Log($"MTP 완료 (성공)", ErrorLogger.LogLevel.INFO, zoneNumber);
                        }
                        else
                        {
                            Debug.WriteLine($"[Zone {zoneNumber}] MTP 완료 (실패)");
                            ErrorLogger.Log($"MTP 완료 (실패)", ErrorLogger.LogLevel.WARNING, zoneNumber);
                        }
                        
                        return ok;
                    }
                    
                    case "IPVS":
                    {
                        Debug.WriteLine($"[Zone {zoneNumber}] IPVS 실행 시작 (공유 Input 사용)");
                        ErrorLogger.Log($"IPVS 실행 시작 - CELL_ID: {context.SharedInput.CELL_ID}", ErrorLogger.LogLevel.INFO, zoneNumber);
                        
                        //25.10.29 - 공유 Input 사용 (매번 생성하지 않음!)
                        //25.10.29 - Task.Run() 제거 (이미 백그라운드 스레드에서 실행 중)
                        var (output, ok) = DllFunctions.CallIPVSTestFunction(context.SharedInput);
                        
                        if (ok)
                        {
                            //25.10.29 - 공유 Output에 결과 저장
                            context.SharedOutput = output;
                            Debug.WriteLine($"[Zone {zoneNumber}] IPVS 완료 (성공) - Output 저장됨");
                            ErrorLogger.Log($"IPVS 완료 (성공)", ErrorLogger.LogLevel.INFO, zoneNumber);
                        }
                        else
                        {
                            Debug.WriteLine($"[Zone {zoneNumber}] IPVS 완료 (실패)");
                            ErrorLogger.Log($"IPVS 완료 (실패)", ErrorLogger.LogLevel.WARNING, zoneNumber);
                        }
                        
                        return ok;
                    }
                    
                    //25.10.29 - Graycrushing 함수 추가 (MTP 결과를 활용)
                    case "GRAYCRUSHING":
                    {
                        Debug.WriteLine($"[Zone {zoneNumber}] Graycrushing 실행 시작");
                        ErrorLogger.Log($"Graycrushing 실행 시작 (MTP 결과 활용)", ErrorLogger.LogLevel.INFO, zoneNumber);
                        
                        //25.10.29 - 현재 SharedOutput(MTP 결과)을 Graycrushing에 전달
                        //25.10.29 - Task.Run() 제거 (이미 백그라운드 스레드에서 실행 중)
                        var (output, ok) = DllFunctions.CallGraycrushing(zoneNumber, context.SharedOutput);

                        if (ok)
                        {
                            // 보정된 결과로 공유 Output 업데이트
                            context.SharedOutput = output;
                            
                            Debug.WriteLine($"[Zone {zoneNumber}] Graycrushing 완료 (성공) - Output 보정됨");
                            ErrorLogger.Log($"Graycrushing 완료 (성공)", ErrorLogger.LogLevel.INFO, zoneNumber);
                        }
                        else
                        {
                            Debug.WriteLine($"[Zone {zoneNumber}] Graycrushing 완료 (실패) - 원본 유지");
                            ErrorLogger.Log($"Graycrushing 완료 (실패)", ErrorLogger.LogLevel.WARNING, zoneNumber);
                        }
                        
                        return ok;
                    }
                    
                    case "MAKE_RESULT_LOG":
                        return true;
                        
                    default:
                        Debug.WriteLine($"[Zone {zoneNumber}] 알 수 없는 함수: {functionName}");
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
        
        #endregion
        
        #region 하위 호환성 메서드 (기존 코드와의 호환)
        
        /// <summary>
        /// Zone별 SEQ 시작 시간 설정 (하위 호환성)
        /// </summary>
        [Obsolete("StartZoneSeq()를 사용하세요")]
        public static void SetZoneSeqStartTime(int zoneNumber, DateTime startTime)
        {
            if (_zoneContexts.TryGetValue(zoneNumber, out var context))
            {
                context.SeqStartTime = startTime;
            }
        }
        
        /// <summary>
        /// Zone별 SEQ 종료 시간 설정 (하위 호환성)
        /// </summary>
        [Obsolete("EndZoneSeq()를 사용하세요")]
        public static void SetZoneSeqEndTime(int zoneNumber, DateTime endTime)
        {
            if (_zoneContexts.TryGetValue(zoneNumber, out var context))
            {
                context.SeqEndTime = endTime;
            }
        }
        
        /// <summary>
        /// SEQ 시작/종료 시간 리셋 (하위 호환성)
        /// </summary>
        [Obsolete("ResetAllZones()를 사용하세요")]
        public static void ResetSeqStartTime()
        {
            ResetAllZones();
        }
        
        /// <summary>
        /// Zone 결과 직접 설정 (하위 호환성)
        /// </summary>
        [Obsolete("GetZoneContext()를 통해 SharedOutput에 직접 접근하세요")]
        public static void SetZoneResult(int zoneNumber, Output output)
        {
            if (_zoneContexts.TryGetValue(zoneNumber, out var context))
            {
                context.SharedOutput = output;
            }
        }
        
        /// <summary>
        /// 현재 Zone 번호 설정 (하위 호환성 - 더 이상 필요 없음)
        /// </summary>
        [Obsolete("Zone 번호는 ExecuteMappedAsync의 파라미터로 전달됩니다")]
        public static void SetCurrentZone(int zoneNumber)
        {
            // 빈 메서드 (하위 호환성 유지)
        }
        
        public static DateTime GetSeqStartTime() => _globalSeqStartTime;
        public static DateTime GetSeqEndTime() => _globalSeqEndTime;
        public static void SetSeqEndTime(DateTime endTime) => _globalSeqEndTime = endTime;
        
        #endregion
    }
}

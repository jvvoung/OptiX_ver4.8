using System;
using System.IO;
using System.Text;
using OptiX.Common;
using OptiX.DLL;

namespace OptiX.Result_LOG.OPTIC
{
    /// <summary>
    /// OPTIC EECP_SUMMARY 로그 파일 생성 및 관리 클래스 (Singleton)
    /// 경로: INI 파일에서 로드 (MTP_PATHS.EECP_SUMMARY_FOLDER)
    /// 파일명: EECP_SUMMARY_YYYYMMDD.csv
    /// </summary>
    public class OpticEECPSummaryLogger
    {
        private static readonly object _fileLock = new object();
        private static OpticEECPSummaryLogger _instance;
        private readonly string _basePath;
        private readonly string _filePath;     // 현재 모드의 파일 경로
        private readonly bool _isHviMode;      // HVI 모드 여부

        /// <summary>
        /// Singleton Instance
        /// </summary>
        public static OpticEECPSummaryLogger Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_fileLock)
                    {
                        if (_instance == null)
                        {
                            _instance = new OpticEECPSummaryLogger();
                        }
                    }
                }
                return _instance;
            }
        }

        //25.12.08 - 프로그램 시작 시 모드 확인하여 해당 파일만 준비
        private OpticEECPSummaryLogger()
        {
            lock (_fileLock)
            {
                try
                {
                    // INI 파일에서 EECP_SUMMARY 폴더 경로 읽기
                    string rawPath = GlobalDataManager.GetValue("MTP_PATHS", "EECP_SUMMARY_FOLDER", @"D:\Project\Log\Result\OPTIC\EECP_Summary");
                    
                    // 경로 정리 및 검증
                    _basePath = CleanPath(rawPath);
                    
                    // 디렉토리가 없으면 생성
                    if (!Directory.Exists(_basePath))
                    {
                        Directory.CreateDirectory(_basePath);
                        System.Diagnostics.Debug.WriteLine($"OPTIC EECP_SUMMARY 디렉토리 생성: {_basePath}");
                    }
                    
                    // OptiX.ini에서 HVI 모드 확인
                    string hviModeStr = GlobalDataManager.GetValue("Settings", "HVI_MODE", "F");
                    _isHviMode = (hviModeStr == "T" || hviModeStr.ToUpper() == "TRUE");
                    
                    // 모드에 따라 파일명 결정
                    string fileName = _isHviMode 
                        ? $"EECP_SUMMARY_HVI_{DateTime.Now:yyyyMMdd}.csv"
                        : $"EECP_SUMMARY_{DateTime.Now:yyyyMMdd}.csv";
                    
                    _filePath = Path.Combine(_basePath, fileName);
                    
                    // 파일이 없으면 헤더 생성
                    if (!File.Exists(_filePath))
                    {
                        if (_isHviMode)
                        {
                            CreateHeaderHvi();
                            System.Diagnostics.Debug.WriteLine($"OPTIC EECP_SUMMARY HVI 헤더 파일 생성: {_filePath}");
                        }
                        else
                        {
                            CreateHeaderNormal();
                            System.Diagnostics.Debug.WriteLine($"OPTIC EECP_SUMMARY Normal 헤더 파일 생성: {_filePath}");
                        }
                    }
                    else
                    {
                        string modeStr = _isHviMode ? "HVI" : "Normal";
                        System.Diagnostics.Debug.WriteLine($"OPTIC EECP_SUMMARY {modeStr} 로거 초기화: {_filePath}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"OPTIC EECP_SUMMARY 로거 초기화 오류: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// 경로 정리 및 검증
        /// </summary>
        private string CleanPath(string rawPath)
        {
            if (string.IsNullOrWhiteSpace(rawPath))
            {
                return @"D:\Project\Log\Result\OPTIC\EECP_Summary";
            }
            
            // 앞뒤 공백 제거
            rawPath = rawPath.Trim();
            
            // 잘못된 문자 제거 또는 대체
            char[] invalidChars = Path.GetInvalidPathChars();
            foreach (char c in invalidChars)
            {
                rawPath = rawPath.Replace(c, '_');
            }
            
            // 연속된 백슬래시 정리
            while (rawPath.Contains("\\\\"))
            {
                rawPath = rawPath.Replace("\\\\", "\\");
            }
            
            return rawPath;
        }

        //25.12.08 - Normal 모드 CSV 헤더 생성
        /// <summary>
        /// Normal 모드 CSV 헤더 생성
        /// </summary>
        private void CreateHeaderNormal()
        {
            var header = new StringBuilder();
            header.AppendLine("START TIME,END TIME,CELL ID,INNER ID,ZONE,SUMMARY_DATA,TACT,JUDGMENT,ERROR_NAME,TOTAL_POINT,CUR_POINT");
            
            File.WriteAllText(_filePath, header.ToString(), Encoding.UTF8);
        }

        //25.12.08 - HVI 모드 CSV 헤더 생성
        /// <summary>
        /// HVI 모드 CSV 헤더 생성
        /// </summary>
        private void CreateHeaderHvi()
        {
            var header = new StringBuilder();
            header.AppendLine("START TIME,END TIME,CELL ID,INNER ID,SEQUENCE,SUMMARY_DATA,TACT,JUDGMENT,ERROR_NAME,TOTAL_POINT,CUR_POINT");
            
            File.WriteAllText(_filePath, header.ToString(), Encoding.UTF8);
        }

        /// <summary>
        /// EECP_SUMMARY 로그 데이터 기록
        /// </summary>
        public void LogEECPSummaryData(DateTime startTime, DateTime endTime, string cellId, string innerId, int zoneNumber, string summaryData, Input input, ZoneTestResult testResult)
        {
            var logEntry = new StringBuilder();
            
            logEntry.Append($"{startTime:yyyy:MM:dd HH:mm:ss:fff},");
            logEntry.Append($"{endTime:yyyy:MM:dd HH:mm:ss:fff},");
            logEntry.Append($"{cellId},");
            logEntry.Append($"{innerId},");
            logEntry.Append($"{zoneNumber},");
            logEntry.Append($"{summaryData},");
            double tact = (endTime - startTime).TotalSeconds;
            logEntry.Append($"{tact:F3},");
            logEntry.Append($"{testResult.Judgment},");
            logEntry.Append($"{testResult.ErrorName},");
            logEntry.Append($"{input.total_point},");
            logEntry.AppendLine($"{input.cur_point}");
            
            lock (_fileLock)
            {
                File.AppendAllText(_filePath, logEntry.ToString(), Encoding.UTF8);
            }
        }

        /// <summary>
        /// 간단한 로그 메서드
        /// </summary>
        public void LogEECPSummaryData(string cellId, string innerId, int zoneNumber, string summaryData, Input input, ZoneTestResult testResult)
        {
            var now = DateTime.Now;
            LogEECPSummaryData(now.AddSeconds(-10), now, cellId, innerId, zoneNumber, summaryData, input, testResult);
        }

        //25.12.08 - HVI 모드 전용 EECP_SUMMARY 로그 기록
        /// <summary>
        /// HVI 모드 전용 EECP_SUMMARY 로그 기록
        /// 파일명: EECP_SUMMARY_HVI_yyyyMMdd.csv (생성자에서 초기화)
        /// SEQUENCE별로 한 행 생성
        /// </summary>
        public bool LogEECPSummaryDataHvi(DateTime startTime, DateTime endTime, string cellId, string innerId, int zoneCount, int sequenceCount, Input input, ZoneTestResult testResult)
        {
            try
            {
                // SEQUENCE별로 한 행씩 생성
                for (int seq = 0; seq < sequenceCount; seq++)
                {
                    var logEntry = new StringBuilder();
                    
                    logEntry.Append($"{startTime:yyyy:MM:dd HH:mm:ss:fff},");
                    logEntry.Append($"{endTime:yyyy:MM:dd HH:mm:ss:fff},");
                    logEntry.Append($"{cellId},");
                    logEntry.Append($"{innerId},");
                    logEntry.Append($"SEQ{seq + 1},");
                    logEntry.Append($"ZONE_COUNT={zoneCount},");
                    double tact = (endTime - startTime).TotalSeconds;
                    logEntry.Append($"{tact:F3},");
                    logEntry.Append($"{testResult.Judgment},");
                    logEntry.Append($"{testResult.ErrorName},");
                    logEntry.Append($"{input.total_point},");
                    logEntry.AppendLine($"{input.cur_point}");
                    
                    lock (_fileLock)
                    {
                        File.AppendAllText(_filePath, logEntry.ToString(), Encoding.UTF8);
                    }
                }
                
                ErrorLogger.Log($"HVI EECP_SUMMARY 로그 생성 완료: {sequenceCount}개 SEQUENCE", ErrorLogger.LogLevel.INFO);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OPTIC EECP_SUMMARY 로그 생성 오류 (HVI): {ex.Message}");
                ErrorLogger.LogException(ex, "HVI EECP_SUMMARY 로그 생성 중 오류");
                return false;
            }
        }
    }
}


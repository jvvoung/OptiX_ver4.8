using System;
using System.IO;
using System.Text;
using OptiX.Common;

namespace OptiX.Result_LOG.IPVS
{
    /// <summary>
    /// IPVS VALIDATION 로그 파일 생성 및 관리 클래스 (Singleton)
    /// 경로: INI 파일에서 로드 (IPVS_PATHS.VALID_FOLDER)
    /// 파일명: VALIDATION_YYYYMMDD.ini
    /// </summary>
    public class IPVSValidationLogger
    {
        private static readonly object _fileLock = new object();
        private static IPVSValidationLogger _instance;
        private readonly string _basePath;
        private readonly string _fileName;
        private readonly string _fullPath;

        /// <summary>
        /// Singleton Instance
        /// </summary>
        public static IPVSValidationLogger Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_fileLock)
                    {
                        if (_instance == null)
                        {
                            _instance = new IPVSValidationLogger();
                        }
                    }
                }
                return _instance;
            }
        }

        private IPVSValidationLogger()
        {
            lock (_fileLock)
            {
                try
                {
                    // INI 파일에서 VALIDATION 폴더 경로 읽기
                    string rawPath = GlobalDataManager.GetValue("IPVS_PATHS", "VALID_FOLDER", @"D:\Project\Log\Result\IPVS\Validation");
                    
                    // 경로 정리 및 검증
                    _basePath = CleanPath(rawPath);
                    _fileName = $"VALIDATION_{DateTime.Now:yyyyMMdd}.ini";
                    _fullPath = Path.Combine(_basePath, _fileName);
                    
                    System.Diagnostics.Debug.WriteLine($"IPVS VALIDATION 로거 초기화: {_fullPath}");
                    
                    // 디렉토리가 없으면 생성
                    if (!Directory.Exists(_basePath))
                    {
                        Directory.CreateDirectory(_basePath);
                        System.Diagnostics.Debug.WriteLine($"IPVS VALIDATION 디렉토리 생성: {_basePath}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"IPVS VALIDATION 로거 초기화 오류: {ex.Message}");
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
                return @"D:\Project\Log\Result\IPVS\Validation";
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

        /// <summary>
        /// VALIDATION 로그 데이터 기록
        /// </summary>
        public void LogValidationData(DateTime startTime, DateTime endTime, string cellId, string innerId, int zoneNumber, string validationData)
        {
            var logEntry = new StringBuilder();
            
            logEntry.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]");
            logEntry.AppendLine($"START_TIME={startTime:yyyy:MM:dd HH:mm:ss:fff}");
            logEntry.AppendLine($"END_TIME={endTime:yyyy:MM:dd HH:mm:ss:fff}");
            logEntry.AppendLine($"CELL_ID={cellId}");
            logEntry.AppendLine($"INNER_ID={innerId}");
            logEntry.AppendLine($"ZONE={zoneNumber}");
            logEntry.AppendLine($"VALIDATION_DATA={validationData}");
            logEntry.AppendLine();
            
            lock (_fileLock)
            {
                File.AppendAllText(_fullPath, logEntry.ToString(), Encoding.UTF8);
            }
        }

        /// <summary>
        /// 간단한 로그 메서드
        /// </summary>
        public void LogValidationData(string cellId, string innerId, int zoneNumber, string validationData)
        {
            var now = DateTime.Now;
            LogValidationData(now.AddSeconds(-10), now, cellId, innerId, zoneNumber, validationData);
        }

        /// <summary>
        /// 검증 결과를 INI 섹션으로 기록
        /// </summary>
        public void LogValidationSection(string sectionName, string cellId, string innerId, System.Collections.Generic.Dictionary<string, string> validationResults)
        {
            var logEntry = new StringBuilder();
            
            logEntry.AppendLine($"[{sectionName}_{DateTime.Now:yyyyMMdd_HHmmss}]");
            logEntry.AppendLine($"CELL_ID={cellId}");
            logEntry.AppendLine($"INNER_ID={innerId}");
            logEntry.AppendLine($"TIMESTAMP={DateTime.Now:yyyy:MM:dd HH:mm:ss:fff}");
            
            foreach (var result in validationResults)
            {
                logEntry.AppendLine($"{result.Key}={result.Value}");
            }
            
            logEntry.AppendLine();
            
            File.AppendAllText(_fullPath, logEntry.ToString(), Encoding.UTF8);
        }
    }
}


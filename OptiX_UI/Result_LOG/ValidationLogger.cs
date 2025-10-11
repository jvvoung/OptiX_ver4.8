using System;
using System.IO;
using System.Text;

namespace OptiX_UI.Result_LOG
{
    /// <summary>
    /// VALIDATION 로그 파일 생성 및 관리 클래스 (Singleton)
    /// 경로: D:\Project\Log\Result\VALIDATION
    /// 파일명: VALIDATION_YYYYMMDD.ini
    /// </summary>
    public class ValidationLogger
    {
        private static readonly object _fileLock = new object();
        private static ValidationLogger _instance;
        private readonly string _basePath = @"D:\Project\Log\Result\VALIDATION";
        private readonly string _fileName;
        private readonly string _fullPath;

        /// <summary>
        /// Singleton Instance
        /// </summary>
        public static ValidationLogger Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_fileLock)
                    {
                        if (_instance == null)
                        {
                            _instance = new ValidationLogger();
                        }
                    }
                }
                return _instance;
            }
        }

        private ValidationLogger()
        {
            lock (_fileLock)
            {
                try
                {
                    _fileName = $"VALIDATION_{DateTime.Now:yyyyMMdd}.ini";
                    _fullPath = Path.Combine(_basePath, _fileName);
                    
                    System.Diagnostics.Debug.WriteLine($"VALIDATION 로거 초기화: {_fullPath}");
                    
                    // 디렉토리가 없으면 생성
                    if (!Directory.Exists(_basePath))
                    {
                        Directory.CreateDirectory(_basePath);
                        System.Diagnostics.Debug.WriteLine($"VALIDATION 디렉토리 생성: {_basePath}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"VALIDATION 로거 초기화 오류: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// VALIDATION 로그 데이터 기록
        /// </summary>
        /// <param name="startTime">시작 시간</param>
        /// <param name="endTime">종료 시간</param>
        /// <param name="cellId">셀 ID</param>
        /// <param name="innerId">내부 ID</param>
        /// <param name="zoneNumber">Zone 번호</param>
        /// <param name="validationData">검증 데이터</param>
        public void LogValidationData(DateTime startTime, DateTime endTime, string cellId, string innerId, int zoneNumber, string validationData)
        {
            var logEntry = new StringBuilder();
            
            // INI 파일 형식으로 데이터 기록
            logEntry.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]");
            logEntry.AppendLine($"START_TIME={startTime:yyyy:MM:dd HH:mm:ss:fff}");
            logEntry.AppendLine($"END_TIME={endTime:yyyy:MM:dd HH:mm:ss:fff}");
            logEntry.AppendLine($"CELL_ID={cellId}");
            logEntry.AppendLine($"INNER_ID={innerId}");
            logEntry.AppendLine($"ZONE={zoneNumber}");
            logEntry.AppendLine($"VALIDATION_DATA={validationData}");
            logEntry.AppendLine();
            
            // 파일에 추가 (동기화 처리)
            lock (_fileLock)
            {
                File.AppendAllText(_fullPath, logEntry.ToString(), Encoding.UTF8);
            }
        }

        /// <summary>
        /// 간단한 로그 메서드 (기본값 사용)
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

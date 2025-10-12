using System;
using System.IO;
using System.Text;

namespace OptiX.Result_LOG.OPTIC
{
    /// <summary>
    /// OPTIC EECP_SUMMARY 로그 파일 생성 및 관리 클래스 (Singleton)
    /// 경로: D:\Project\Log\Result\특성
    /// 파일명: EECP_SUMMARY_YYYYMMDD.csv
    /// </summary>
    public class OpticEECPSummaryLogger
    {
        private static readonly object _fileLock = new object();
        private static OpticEECPSummaryLogger _instance;
        private readonly string _basePath = @"D:\Project\Log\Result\특성\EECP_Summary";
        private readonly string _fileName;
        private readonly string _fullPath;

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

        private OpticEECPSummaryLogger()
        {
            lock (_fileLock)
            {
                try
                {
                    _fileName = $"EECP_SUMMARY_{DateTime.Now:yyyyMMdd}.csv";
                    _fullPath = Path.Combine(_basePath, _fileName);
                    
                    System.Diagnostics.Debug.WriteLine($"OPTIC EECP_SUMMARY 로거 초기화: {_fullPath}");
                    
                    // 디렉토리가 없으면 생성
                    if (!Directory.Exists(_basePath))
                    {
                        Directory.CreateDirectory(_basePath);
                        System.Diagnostics.Debug.WriteLine($"OPTIC EECP_SUMMARY 디렉토리 생성: {_basePath}");
                    }
                    
                    // 파일이 없으면 헤더 생성
                    if (!File.Exists(_fullPath))
                    {
                        CreateHeader();
                        System.Diagnostics.Debug.WriteLine($"OPTIC EECP_SUMMARY 헤더 파일 생성: {_fullPath}");
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
        /// CSV 헤더 생성
        /// </summary>
        private void CreateHeader()
        {
            var header = new StringBuilder();
            header.AppendLine("START TIME,END TIME,CELL ID,INNER ID,ZONE,SUMMARY_DATA");
            
            File.WriteAllText(_fullPath, header.ToString(), Encoding.UTF8);
        }

        /// <summary>
        /// EECP_SUMMARY 로그 데이터 기록
        /// </summary>
        public void LogEECPSummaryData(DateTime startTime, DateTime endTime, string cellId, string innerId, int zoneNumber, string summaryData)
        {
            var logEntry = new StringBuilder();
            
            logEntry.Append($"{startTime:yyyy:MM:dd HH:mm:ss:fff},");
            logEntry.Append($"{endTime:yyyy:MM:dd HH:mm:ss:fff},");
            logEntry.Append($"{cellId},");
            logEntry.Append($"{innerId},");
            logEntry.Append($"{zoneNumber},");
            logEntry.AppendLine(summaryData);
            
            lock (_fileLock)
            {
                File.AppendAllText(_fullPath, logEntry.ToString(), Encoding.UTF8);
            }
        }

        /// <summary>
        /// 간단한 로그 메서드
        /// </summary>
        public void LogEECPSummaryData(string cellId, string innerId, int zoneNumber, string summaryData)
        {
            var now = DateTime.Now;
            LogEECPSummaryData(now.AddSeconds(-10), now, cellId, innerId, zoneNumber, summaryData);
        }
    }
}


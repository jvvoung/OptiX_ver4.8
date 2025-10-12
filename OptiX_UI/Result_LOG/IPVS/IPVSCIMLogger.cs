using System;
using System.IO;
using System.Text;
using OptiX.Common;

namespace OptiX.Result_LOG.IPVS
{
    /// <summary>
    /// IPVS CIM 로그 파일 생성 및 관리 클래스 (Singleton)
    /// 경로: INI 파일에서 로드 (IPVS_PATHS.CIM_FOLDER)
    /// 파일명: CIM_YYYYMMDD.dat
    /// </summary>
    public class IPVSCIMLogger
    {
        private static readonly object _fileLock = new object();
        private static IPVSCIMLogger _instance;
        private readonly string _basePath;
        private readonly string _fileName;
        private readonly string _fullPath;

        /// <summary>
        /// Singleton Instance
        /// </summary>
        public static IPVSCIMLogger Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_fileLock)
                    {
                        if (_instance == null)
                        {
                            _instance = new IPVSCIMLogger();
                        }
                    }
                }
                return _instance;
            }
        }

        private IPVSCIMLogger()
        {
            lock (_fileLock)
            {
                try
                {
                    // INI 파일에서 CIM 폴더 경로 읽기
                    _basePath = GlobalDataManager.GetValue("IPVS_PATHS", "CIM_FOLDER", @"D:\Project\Log\Result\IPVS\CIM");
                    _fileName = $"CIM_{DateTime.Now:yyyyMMdd}.dat";
                    _fullPath = Path.Combine(_basePath, _fileName);
                    
                    System.Diagnostics.Debug.WriteLine($"IPVS CIM 로거 초기화: {_fullPath}");
                    
                    // 디렉토리가 없으면 생성
                    if (!Directory.Exists(_basePath))
                    {
                        Directory.CreateDirectory(_basePath);
                        System.Diagnostics.Debug.WriteLine($"IPVS CIM 디렉토리 생성: {_basePath}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"IPVS CIM 로거 초기화 오류: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// CIM 로그 데이터 기록
        /// </summary>
        public void LogCIMData(DateTime startTime, DateTime endTime, string cellId, string innerId, int zoneNumber, string cimData)
        {
            var logEntry = new StringBuilder();
            
            logEntry.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] CIM Log Entry");
            logEntry.AppendLine($"START_TIME: {startTime:yyyy:MM:dd HH:mm:ss:fff}");
            logEntry.AppendLine($"END_TIME: {endTime:yyyy:MM:dd HH:mm:ss:fff}");
            logEntry.AppendLine($"CELL_ID: {cellId}");
            logEntry.AppendLine($"INNER_ID: {innerId}");
            logEntry.AppendLine($"ZONE: {zoneNumber}");
            logEntry.AppendLine($"CIM_DATA: {cimData}");
            logEntry.AppendLine("----------------------------------------");
            
            lock (_fileLock)
            {
                File.AppendAllText(_fullPath, logEntry.ToString(), Encoding.UTF8);
            }
        }

        /// <summary>
        /// 간단한 로그 메서드
        /// </summary>
        public void LogCIMData(string cellId, string innerId, int zoneNumber, string cimData)
        {
            var now = DateTime.Now;
            LogCIMData(now.AddSeconds(-10), now, cellId, innerId, zoneNumber, cimData);
        }

        /// <summary>
        /// CIM 데이터를 바이너리 형식으로 기록
        /// </summary>
        public void LogCIMBinaryData(DateTime startTime, DateTime endTime, string cellId, string innerId, byte[] binaryData)
        {
            var logEntry = new StringBuilder();
            
            logEntry.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] CIM Binary Log Entry");
            logEntry.AppendLine($"START_TIME: {startTime:yyyy:MM:dd HH:mm:ss:fff}");
            logEntry.AppendLine($"END_TIME: {endTime:yyyy:MM:dd HH:mm:ss:fff}");
            logEntry.AppendLine($"CELL_ID: {cellId}");
            logEntry.AppendLine($"INNER_ID: {innerId}");
            logEntry.AppendLine($"BINARY_DATA_SIZE: {binaryData.Length} bytes");
            logEntry.AppendLine($"BINARY_DATA: {Convert.ToBase64String(binaryData)}");
            logEntry.AppendLine("----------------------------------------");
            
            File.AppendAllText(_fullPath, logEntry.ToString(), Encoding.UTF8);
        }
    }
}


using System;
using System.IO;
using System.Text;
using OptiX.Common;

namespace OptiX.Result_LOG.IPVS
{
    //25.10.30 - CIM 로거 Zone별 파일 분리 (Singleton → Static 메서드)
    /// <summary>
    /// IPVS CIM 로그 파일 생성 및 관리 클래스
    /// 경로: INI 파일에서 로드 (IPVS_PATHS.CIM_FOLDER)
    /// 파일명: CIM_YYYYMMDD_ZoneN.dat (Zone별 분리)
    /// </summary>
    public static class IPVSCIMLogger
    {
        private static readonly object _fileLock = new object();

        /// <summary>
        /// 경로 정리 및 검증
        /// </summary>
        private static string CleanPath(string rawPath)
        {
            if (string.IsNullOrWhiteSpace(rawPath))
            {
                return @"D:\Project\Log\Result\IPVS\CIM";
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

        //25.10.30 - Zone별 파일 경로 생성 메서드 추가
        /// <summary>
        /// Zone별 CIM 파일 경로 생성
        /// 파일명: CIM_YYYYMMDD_ZoneN.dat
        /// </summary>
        private static string GetZoneFilePath(int zoneNumber)
        {
            string rawPath = GlobalDataManager.GetValue("IPVS_PATHS", "CIM_FOLDER", @"D:\Project\Log\Result\IPVS\CIM");
            string basePath = CleanPath(rawPath);
            
            // 디렉토리가 없으면 생성
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }
            
            string fileName = $"CIM_{DateTime.Now:yyyyMMdd}_Zone{zoneNumber}.dat";
            return Path.Combine(basePath, fileName);
        }

        //25.10.30 - CIM 로그 데이터 기록 (Zone별 파일)
        /// <summary>
        /// CIM 로그 데이터 기록 (Zone별 파일 생성)
        /// </summary>
        public static void LogCIMData(DateTime startTime, DateTime endTime, string cellId, string innerId, int zoneNumber, string cimData)
        {
            try
            {
                string filePath = GetZoneFilePath(zoneNumber);
                
                var logEntry = new StringBuilder();
                logEntry.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] CIM Log Entry");
                logEntry.AppendLine($"START_TIME: {startTime:yyyy:MM:dd HH:mm:ss:fff}");
                logEntry.AppendLine($"END_TIME: {endTime:yyyy:MM:dd HH:mm:ss:fff}");
                logEntry.AppendLine($"CELL_ID: {cellId}");
                logEntry.AppendLine($"INNER_ID: {innerId}");
                logEntry.AppendLine($"ZONE: {zoneNumber}");
                logEntry.AppendLine($"CIM_DATA: {cimData}");
                logEntry.AppendLine("----------------------------------------");
                
                //25.10.30 - Zone별 파일이므로 Lock 경쟁 최소화
                lock (_fileLock)
                {
                    File.AppendAllText(filePath, logEntry.ToString(), Encoding.UTF8);
                }
                
                System.Diagnostics.Debug.WriteLine($"IPVS CIM 로그 생성: {filePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"IPVS CIM 로그 생성 오류 (Zone {zoneNumber}): {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 간단한 로그 메서드
        /// </summary>
        public static void LogCIMData(string cellId, string innerId, int zoneNumber, string cimData)
        {
            var now = DateTime.Now;
            LogCIMData(now.AddSeconds(-10), now, cellId, innerId, zoneNumber, cimData);
        }
    }
}

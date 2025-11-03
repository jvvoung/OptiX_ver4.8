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
        /// 파일명: ZONE1.dat, ZONE2.dat (덮어쓰기)
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
            
            string fileName = $"ZONE{zoneNumber}.dat";
            return Path.Combine(basePath, fileName);
        }

        //25.10.30 - CIM 로그 데이터 기록 (Zone별 파일)
        //25.11.02 - CIM 로그 형식 변경: IPVS_data[WAD][PATTERN] 값을 "데이터명 = 값" 형식으로 출력
        /// <summary>
        /// CIM 로그 데이터 기록 (Zone별 파일 생성, 덮어쓰기)
        /// 형식: 데이터명 = 값 (struct pattern IPVS_data[7][10] 구조)
        /// </summary>
        public static void LogCIMData(DateTime startTime, DateTime endTime, string cellId, string innerId, int zoneNumber, OptiX.DLL.Output outputData)
        {
            try
            {
                string filePath = GetZoneFilePath(zoneNumber);
                
                var logEntry = new StringBuilder();
                
                // 기본 정보
                logEntry.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] CIM Log Entry");
                logEntry.AppendLine($"START_TIME = {startTime:yyyy:MM:dd HH:mm:ss:fff}");
                logEntry.AppendLine($"END_TIME = {endTime:yyyy:MM:dd HH:mm:ss:fff}");
                
                // TACT 계산
                double tact = (endTime - startTime).TotalSeconds;
                logEntry.AppendLine($"TACT = {tact:F3}");
                
                logEntry.AppendLine($"CELL_ID = {cellId}");
                logEntry.AppendLine($"INNER_ID = {innerId}");
                logEntry.AppendLine($"ZONE = {zoneNumber}");
                logEntry.AppendLine();
                
                // [DATA] 섹션 시작
                logEntry.AppendLine("[DATA]");
                
                // struct pattern IPVS_data[7][10] 형식으로 작성
                // IPVS_data[WAD][PATTERN] 구조
                string[] wadNames = { "WAD_0", "WAD_30", "WAD_45", "WAD_60", "WAD_15", "WAD_A", "WAD_B" };
                string[] patternNames = { "W", "R", "G", "B", "WG", "WG2", "WG3", "WG4", "WG5", "WG10" };
                
                for (int wad = 0; wad < 7; wad++)
                {
                    logEntry.AppendLine($"// {wadNames[wad]} 데이터");
                    
                    for (int pattern = 0; pattern < 10; pattern++)
                    {
                        // IPVS_data[wad][pattern] 인덱스 계산
                        int index = wad * 10 + pattern;
                        
                        if (outputData.IPVS_data != null && index < outputData.IPVS_data.Length)
                        {
                            var data = outputData.IPVS_data[index];
                            string prefix = $"{wadNames[wad]}_{patternNames[pattern]}";
                            
                            logEntry.AppendLine($"{prefix}_X = {data.x:F3}");
                            logEntry.AppendLine($"{prefix}_Y = {data.y:F3}");
                            logEntry.AppendLine($"{prefix}_u = {data.u:F3}");
                            logEntry.AppendLine($"{prefix}_v = {data.v:F3}");
                            logEntry.AppendLine($"{prefix}_L = {data.L:F3}");
                            logEntry.AppendLine($"{prefix}_전류 = {data.cur:F3}");
                            logEntry.AppendLine($"{prefix}_효율 = {data.eff:F3}");
                            logEntry.AppendLine($"{prefix}_판정 = {(data.result == 0 ? "OK" : data.result == 1 ? "NG" : "PTN")}");
                            logEntry.AppendLine();
                        }
                    }
                }
                
                logEntry.AppendLine("========================================");
                logEntry.AppendLine();
                
                //25.10.30 - Zone별 파일 덮어쓰기 (기존 파일 삭제 후 새로 작성)
                lock (_fileLock)
                {
                    File.WriteAllText(filePath, logEntry.ToString(), Encoding.UTF8);
                }
                
                System.Diagnostics.Debug.WriteLine($"IPVS CIM 로그 생성: {filePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"IPVS CIM 로그 생성 오류 (Zone {zoneNumber}): {ex.Message}");
                throw;
            }
        }
    }
}

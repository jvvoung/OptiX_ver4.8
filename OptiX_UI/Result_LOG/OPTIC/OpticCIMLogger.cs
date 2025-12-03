using System;
using System.IO;
using System.Text;
using OptiX.Common;
using OptiX.DLL;

namespace OptiX.Result_LOG.OPTIC
{
    //25.10.30 - CIM 로거 Zone별 파일 분리 (Singleton → Static 메서드)
    /// <summary>
    /// OPTIC CIM 로그 파일 생성 및 관리 클래스
    /// 경로: INI 파일에서 로드 (MTP_PATHS.CIM_FOLDER)
    /// 파일명: CIM_YYYYMMDD_ZoneN.dat (Zone별 분리)
    /// </summary>
    public static class OpticCIMLogger
    {
        private static readonly object _fileLock = new object();

        /// <summary>
        /// 경로 정리 및 검증
        /// </summary>
        private static string CleanPath(string rawPath)
        {
            if (string.IsNullOrWhiteSpace(rawPath))
            {
                return @"D:\Project\Log\Result\OPTIC\CIM";
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
            string rawPath = GlobalDataManager.GetValue("MTP_PATHS", "CIM_FOLDER", @"D:\Project\Log\Result\OPTIC\CIM");
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
        //25.11.02 - CIM 로그 형식 변경: data[WAD][PATTERN] 값을 "데이터명 = 값" 형식으로 출력
        // 기존: 단순 문자열 전달
        // 변경: Output 구조체를 직접 전달하여 data[7][17] 배열을 순회하며 포맷팅
        //25.11.08 - ZoneTestResult 구조체 추가하여 ErrorName, Tact, Judgment 등을 구조화
        /// <summary>
        /// CIM 로그 데이터 기록 (Zone별 파일 생성)
        /// 형식: 데이터명 = 값 (struct pattern data[7][17] 구조)
        /// </summary>
        public static void LogCIMData(DateTime startTime, DateTime endTime, string cellId, string innerId, int zoneNumber, Output outputData, ZoneTestResult testResult, Input input)
        {
            try
            {
                string filePath = GetZoneFilePath(zoneNumber);
                
                var logEntry = new StringBuilder();
                
                // 기본 정보
                logEntry.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] CIM Log Entry");
                logEntry.AppendLine($"START_TIME = {startTime:yyyy:MM:dd HH:mm:ss:fff}");
                logEntry.AppendLine($"END_TIME = {endTime:yyyy:MM:dd HH:mm:ss:fff}");
                
                //25.11.08 - ZoneTestResult 구조체에서 TACT 가져오기
                logEntry.AppendLine($"TACT = {testResult.Tact}");
                
                logEntry.AppendLine($"CELL_ID = {cellId}");
                logEntry.AppendLine($"INNER_ID = {innerId}");
                logEntry.AppendLine($"ZONE = {zoneNumber}");
                logEntry.AppendLine($"TOTAL_POINT = {input.total_point}");
                logEntry.AppendLine($"CUR_POINT = {input.cur_point}");
                
                //25.11.08 - ZoneTestResult 구조체에서 판정 정보 추가
                logEntry.AppendLine($"ERROR_NAME = {testResult.ErrorName}");
                logEntry.AppendLine($"JUDGMENT = {testResult.Judgment}");
                
                //25.11.08 - 향후 세부 판정 필드 추가 시 사용 (현재는 빈 값)
                if (!string.IsNullOrEmpty(testResult.ColorJudgment))
                    logEntry.AppendLine($"COLOR_JUDGMENT = {testResult.ColorJudgment}");
                if (!string.IsNullOrEmpty(testResult.LuminanceJudgment))
                    logEntry.AppendLine($"LUMINANCE_JUDGMENT = {testResult.LuminanceJudgment}");
                if (!string.IsNullOrEmpty(testResult.EfficiencyJudgment))
                    logEntry.AppendLine($"EFFICIENCY_JUDGMENT = {testResult.EfficiencyJudgment}");
                if (!string.IsNullOrEmpty(testResult.CurrentJudgment))
                    logEntry.AppendLine($"CURRENT_JUDGMENT = {testResult.CurrentJudgment}");
                if (!string.IsNullOrEmpty(testResult.PatternJudgment))
                    logEntry.AppendLine($"PATTERN_JUDGMENT = {testResult.PatternJudgment}");
                
                logEntry.AppendLine();
                
                // [DATA] 섹션 시작
                logEntry.AppendLine("[DATA]");
                
                // struct pattern data[7][17] 형식으로 작성
                // data[WAD][PATTERN] 구조
                string[] wadNames = { "WAD_0", "WAD_30", "WAD_45", "WAD_60", "WAD_15", "WAD_A", "WAD_B" };
                string[] patternNames = { "W", "R", "G", "B", "WG", "WG2", "WG3", "WG4", "WG5", "WG6", "WG7", "WG8", "WG9", "WG10", "WG11", "WG12", "WG13" };
                
                for (int wad = 0; wad < 7; wad++)
                {
                    logEntry.AppendLine($"// {wadNames[wad]} 데이터");
                    
                    for (int pattern = 0; pattern < 17; pattern++)
                    {
                        // data[wad][pattern] 인덱스 계산
                        int index = wad * 17 + pattern;
                        
                        if (index < outputData.data.Length)
                        {
                            var data = outputData.data[index];
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
                
                System.Diagnostics.Debug.WriteLine($"OPTIC CIM 로그 생성: {filePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OPTIC CIM 로그 생성 오류 (Zone {zoneNumber}): {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// HVI 모드 전용 CIM 로그 데이터 기록 (Output 배열 전체 처리)
        /// </summary>
        public static void LogCIMDataHvi(DateTime startTime, DateTime endTime, string cellId, string innerId, Output[] outputs, ZoneTestResult testResult, Input input)
        {
            if (outputs == null || outputs.Length == 0)
            {
                return;
            }

            for (int index = 0; index < outputs.Length; index++)
            {
                var output = outputs[index];
                int zoneNumber = index + 1;
                LogCIMData(startTime, endTime, cellId, innerId, zoneNumber, output, testResult, input);
            }
        }
    }
}


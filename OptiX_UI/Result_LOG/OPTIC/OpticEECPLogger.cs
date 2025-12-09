using System;
using System.IO;
using System.Text;
using OptiX.DLL;
using OptiX.Common;

namespace OptiX.Result_LOG.OPTIC
{
    /// <summary>
    /// OPTIC EECP 로그 파일 생성 및 관리 클래스 (Singleton)
    /// 경로: INI 파일에서 로드 (MTP_PATHS.EECP_FOLDER)
    /// 파일명: EECP_YYYYMMDD.csv
    /// </summary>
    public class OpticEECPLogger
    {
        private static readonly object _fileLock = new object();
        private static OpticEECPLogger _instance;
        private readonly string _basePath;
        private readonly string _filePath;     // 현재 모드의 파일 경로
        private readonly bool _isHviMode;      // HVI 모드 여부

        /// <summary>
        /// Singleton Instance
        /// </summary>
        public static OpticEECPLogger Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_fileLock)
                    {
                        if (_instance == null)
                        {
                            _instance = new OpticEECPLogger();
                        }
                    }
                }
                return _instance;
            }
        }

        //25.12.08 - 프로그램 시작 시 모드 확인하여 해당 파일만 준비
        private OpticEECPLogger()
        {
            lock (_fileLock)
            {
                try
                {
                    // INI 파일에서 EECP 폴더 경로 읽기
                    string rawPath = GlobalDataManager.GetValue("MTP_PATHS", "EECP_FOLDER", @"D:\Project\Log\Result\OPTIC\EECP");
                    
                    // 경로 정리 및 검증
                    _basePath = CleanPath(rawPath);
                    
                    // 디렉토리가 없으면 생성
                    if (!Directory.Exists(_basePath))
                    {
                        Directory.CreateDirectory(_basePath);
                        System.Diagnostics.Debug.WriteLine($"OPTIC EECP 디렉토리 생성: {_basePath}");
                    }
                    
                    // OptiX.ini에서 HVI 모드 확인
                    string hviModeStr = GlobalDataManager.GetValue("Settings", "HVI_MODE", "F");
                    _isHviMode = (hviModeStr == "T" || hviModeStr.ToUpper() == "TRUE");
                    
                    // 모드에 따라 파일명 결정
                    string fileName = _isHviMode 
                        ? $"EECP_HVI_{DateTime.Now:yyyyMMdd}.csv"
                        : $"EECP_{DateTime.Now:yyyyMMdd}.csv";
                    
                    _filePath = Path.Combine(_basePath, fileName);
                    
                    // 파일이 없으면 헤더 생성
                    if (!File.Exists(_filePath))
                    {
                        if (_isHviMode)
                        {
                            CreateHeaderHvi();
                            System.Diagnostics.Debug.WriteLine($"OPTIC EECP HVI 헤더 파일 생성: {_filePath}");
                        }
                        else
                        {
                            CreateHeaderNormal();
                            System.Diagnostics.Debug.WriteLine($"OPTIC EECP Normal 헤더 파일 생성: {_filePath}");
                        }
                    }
                    else
                    {
                        string modeStr = _isHviMode ? "HVI" : "Normal";
                        System.Diagnostics.Debug.WriteLine($"OPTIC EECP {modeStr} 로거 초기화: {_filePath}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"OPTIC EECP 로거 초기화 오류: {ex.Message}");
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
                return @"D:\Project\Log\Result\OPTIC\EECP";
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
            header.Append("START TIME,END TIME,TACT,CELL ID,INNER ID,ZONE");
            
            // 패턴별 WAD 컬럼 생성
            string[] wadNames = { "", "_WAD_30", "_WAD_45", "_WAD_60", "_WAD_15", "_WAD_A", "_WAD_B" };
            string[] patternNames = { "W", "R", "G", "B", "WG", "WG2", "WG3", "WG4", "WG5", "WG6", "WG7", "WG8", "WG9", "WG10", "WG11", "WG12", "WG13" };
            
            for (int pattern = 0; pattern < 17; pattern++)
            {
                for (int wad = 0; wad < 7; wad++)
                {
                    string wadName = wadNames[wad];
                    string patternName = patternNames[pattern];
                    header.Append($",{patternName}{wadName}_X,{patternName}{wadName}_Y,{patternName}{wadName}_u,{patternName}{wadName}_v,{patternName}{wadName}_L,{patternName}{wadName}_전류,{patternName}{wadName}_효율");
                }
            }
            
            header.Append(",ERROR_NAME,JUDGMENT,TOTAL_POINT,CUR_POINT");
            header.AppendLine();
            File.WriteAllText(_filePath, header.ToString(), Encoding.UTF8);
        }

        //25.12.08 - HVI 모드 CSV 헤더 생성 (Zone별 접미사 포함)
        /// <summary>
        /// HVI 모드 CSV 헤더 생성 (Zone별 접미사 포함)
        /// 생성자에서 초기화 시 MTP_ZONE 개수만큼 생성
        /// </summary>
        private void CreateHeaderHvi()
        {
            // INI에서 Zone 개수 읽기 (최대치로 헤더 생성)
            string zoneCountStr = GlobalDataManager.GetValue("Settings", "MTP_ZONE", "3");
            int maxZoneCount = int.TryParse(zoneCountStr, out int zoneCount) ? zoneCount : 3;
            
            var header = new StringBuilder();
            header.Append("START TIME,END TIME,TACT,CELL ID,INNER ID,SEQUENCE");
            
            // 패턴별 WAD 컬럼 생성
            string[] wadNames = { "", "_WAD_30", "_WAD_45", "_WAD_60", "_WAD_15", "_WAD_A", "_WAD_B" };
            string[] patternNames = { "W", "R", "G", "B", "WG", "WG2", "WG3", "WG4", "WG5", "WG6", "WG7", "WG8", "WG9", "WG10", "WG11", "WG12", "WG13" };
            string[] zoneSuffix = { "_C", "_L", "_R", "_T", "_B", "_F", "_S", "_E" };
            
            // Zone별로 컬럼 생성
            for (int zone = 0; zone < maxZoneCount; zone++)
            {
                string suffix = zone < zoneSuffix.Length ? zoneSuffix[zone] : $"_Z{zone}";
                
                for (int pattern = 0; pattern < 17; pattern++)
                {
                    for (int wad = 0; wad < 7; wad++)
                    {
                        string wadName = wadNames[wad];
                        string patternName = patternNames[pattern];
                        header.Append($",{patternName}{wadName}{suffix}_X");
                        header.Append($",{patternName}{wadName}{suffix}_Y");
                        header.Append($",{patternName}{wadName}{suffix}_u");
                        header.Append($",{patternName}{wadName}{suffix}_v");
                        header.Append($",{patternName}{wadName}{suffix}_L");
                        header.Append($",{patternName}{wadName}{suffix}_전류");
                        header.Append($",{patternName}{wadName}{suffix}_효율");
                    }
                }
            }
            
            header.Append(",ERROR_NAME,JUDGMENT,TOTAL_POINT,CUR_POINT");
            header.AppendLine();
            File.WriteAllText(_filePath, header.ToString(), Encoding.UTF8);
        }

        /// <summary>
        /// EECP 로그 데이터 기록
        /// </summary>
        public void LogEECPData(DateTime startTime, DateTime endTime, string cellId, string innerId, int zoneNumber, Output outputData, Input input, ZoneTestResult testResult)
        {
            var logEntry = new StringBuilder();
            
            // TACT 계산
            double tact = (endTime - startTime).TotalSeconds;
            
            // 기본 정보
            logEntry.Append($"{startTime:yyyy:MM:dd HH:mm:ss:fff},");
            logEntry.Append($"{endTime:yyyy:MM:dd HH:mm:ss:fff},");
            logEntry.Append($"{tact:F3},");
            logEntry.Append($"{cellId},");
            logEntry.Append($"{innerId},");
            logEntry.Append($"{zoneNumber},");
            
            // 구조체 데이터 처리 (패턴별 WAD 순서)
            for (int pattern = 0; pattern < 17; pattern++)
            {
                for (int wad = 0; wad < 7; wad++)
                {
                    int index = wad * 17 + pattern;
                    if (index < outputData.data.Length)
                    {
                        logEntry.Append($"{outputData.data[index].x:F3},");
                        logEntry.Append($"{outputData.data[index].y:F3},");
                        logEntry.Append($"{outputData.data[index].u:F3},");
                        logEntry.Append($"{outputData.data[index].v:F3},");
                        logEntry.Append($"{outputData.data[index].L:F3},");
                        logEntry.Append($"{outputData.data[index].cur:F3},");
                        logEntry.Append($"{outputData.data[index].eff:F3},");
                    }
                    else
                    {
                        logEntry.Append("0.000,0.000,0.000,0.000,0.000,0.000,0.000,");
                    }
                }
            }
            
            logEntry.Append($"{testResult.ErrorName},");
            logEntry.Append($"{testResult.Judgment},");
            logEntry.Append($"{input.total_point},");
            logEntry.Append($"{input.cur_point},");

            if (logEntry.Length > 0 && logEntry[logEntry.Length - 1] == ',')
            {
                logEntry.Length--;
            }
            
            logEntry.AppendLine();
            
            lock (_fileLock)
            {
                File.AppendAllText(_filePath, logEntry.ToString(), Encoding.UTF8);
            }
        }

        /// <summary>
        /// 간단한 로그 메서드
        /// </summary>
        public void LogEECPData(string cellId, string innerId, int zoneNumber, Output outputData, Input input, ZoneTestResult testResult)
        {
            var now = DateTime.Now;
            LogEECPData(now.AddSeconds(-10), now, cellId, innerId, zoneNumber, outputData, input, testResult);
        }

        //25.12.08 - HVI 모드 전용 EECP 로그 기록 (SEQUENCE × Zone 2중 배열 처리)
        /// <summary>
        /// HVI 모드 전용 EECP 로그 기록
        /// Output 배열 순서: [Zone0_SEQ0, Zone0_SEQ1, ..., Zone1_SEQ0, Zone1_SEQ1, ...]
        /// 인덱스 공식: idx = zone * sequenceCount + seq
        /// SEQUENCE별로 한 행 생성, 각 Zone 데이터를 열로 나열
        /// 파일명: EECP_HVI_yyyyMMdd.csv (생성자에서 초기화)
        /// </summary>
        public bool LogEECPDataHvi(DateTime startTime, DateTime endTime, string cellId, string innerId, Output[] outputs, Input input, ZoneTestResult testResult, int zoneCount, int sequenceCount)
        {
            if (outputs == null || outputs.Length == 0)
            {
                return false;
            }

            try
            {
                // Zone별 접미사
                string[] zoneSuffix = { "_C", "_L", "_R", "_T", "_B", "_F", "_S", "_E" };
                
                // SEQUENCE별로 한 행씩 생성
                for (int seq = 0; seq < sequenceCount; seq++)
                {
                    var logEntry = new StringBuilder();
                    
                    // TACT 계산
                    double tact = (endTime - startTime).TotalSeconds;
                    
                    // 기본 정보
                    logEntry.Append($"{startTime:yyyy:MM:dd HH:mm:ss:fff},");
                    logEntry.Append($"{endTime:yyyy:MM:dd HH:mm:ss:fff},");
                    logEntry.Append($"{tact:F3},");
                    logEntry.Append($"{cellId},");
                    logEntry.Append($"{innerId},");
                    logEntry.Append($"SEQ{seq + 1},");
                    
                    // Zone별로 데이터 추가
                    for (int zone = 0; zone < zoneCount; zone++)
                    {
                        // 인덱스 계산: idx = zone * sequenceCount + seq
                        int idx = zone * sequenceCount + seq;
                        
                        if (idx >= outputs.Length)
                        {
                            ErrorLogger.Log($"HVI EECP: 인덱스 범위 초과 (idx={idx}, length={outputs.Length})", ErrorLogger.LogLevel.WARNING);
                            continue;
                        }
                        
                        var output = outputs[idx];
                        string suffix = zone < zoneSuffix.Length ? zoneSuffix[zone] : $"_Z{zone}";
                        
                        // 구조체 데이터 처리 (패턴별 WAD 순서)
                        for (int pattern = 0; pattern < 17; pattern++)
                        {
                            for (int wad = 0; wad < 7; wad++)
                            {
                                int dataIndex = wad * 17 + pattern;
                                if (dataIndex < output.data.Length)
                                {
                                    logEntry.Append($"{output.data[dataIndex].x:F3},");
                                    logEntry.Append($"{output.data[dataIndex].y:F3},");
                                    logEntry.Append($"{output.data[dataIndex].u:F3},");
                                    logEntry.Append($"{output.data[dataIndex].v:F3},");
                                    logEntry.Append($"{output.data[dataIndex].L:F3},");
                                    logEntry.Append($"{output.data[dataIndex].cur:F3},");
                                    logEntry.Append($"{output.data[dataIndex].eff:F3},");
                                }
                                else
                                {
                                    logEntry.Append("0.000,0.000,0.000,0.000,0.000,0.000,0.000,");
                                }
                            }
                        }
                    }
                    
                    logEntry.Append($"{testResult.ErrorName},");
                    logEntry.Append($"{testResult.Judgment},");
                    logEntry.Append($"{input.total_point},");
                    logEntry.Append($"{input.cur_point},");

                    if (logEntry.Length > 0 && logEntry[logEntry.Length - 1] == ',')
                    {
                        logEntry.Length--;
                    }
                    
                    logEntry.AppendLine();
                    
                    lock (_fileLock)
                    {
                        File.AppendAllText(_filePath, logEntry.ToString(), Encoding.UTF8);
                    }
                }
                
                ErrorLogger.Log($"HVI EECP 로그 생성 완료: {zoneCount}개 Zone × {sequenceCount}개 SEQUENCE", ErrorLogger.LogLevel.INFO);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OPTIC EECP 로그 생성 오류 (HVI): {ex.Message}");
                ErrorLogger.LogException(ex, "HVI EECP 로그 생성 중 오류");
                return false;
            }
        }
    }
}


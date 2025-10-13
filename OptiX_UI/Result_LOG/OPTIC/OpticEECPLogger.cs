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
        private readonly string _fileName;
        private readonly string _fullPath;

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
                    _fileName = $"EECP_{DateTime.Now:yyyyMMdd}.csv";
                    _fullPath = Path.Combine(_basePath, _fileName);
                    
                    System.Diagnostics.Debug.WriteLine($"OPTIC EECP 로거 초기화: {_fullPath}");
                    
                    // 디렉토리가 없으면 생성
                    if (!Directory.Exists(_basePath))
                    {
                        Directory.CreateDirectory(_basePath);
                        System.Diagnostics.Debug.WriteLine($"OPTIC EECP 디렉토리 생성: {_basePath}");
                    }
                    
                    // 파일이 없으면 헤더 생성
                    if (!File.Exists(_fullPath))
                    {
                        CreateHeader();
                        System.Diagnostics.Debug.WriteLine($"OPTIC EECP 헤더 파일 생성: {_fullPath}");
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

        /// <summary>
        /// CSV 헤더 생성
        /// </summary>
        private void CreateHeader()
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
            
            header.AppendLine();
            File.WriteAllText(_fullPath, header.ToString(), Encoding.UTF8);
        }

        /// <summary>
        /// EECP 로그 데이터 기록
        /// </summary>
        public void LogEECPData(DateTime startTime, DateTime endTime, string cellId, string innerId, int zoneNumber, Output outputData)
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
            
            // 마지막 쉼표 제거
            if (logEntry.Length > 0 && logEntry[logEntry.Length - 1] == ',')
            {
                logEntry.Length--;
            }
            
            logEntry.AppendLine();
            
            lock (_fileLock)
            {
                File.AppendAllText(_fullPath, logEntry.ToString(), Encoding.UTF8);
            }
        }

        /// <summary>
        /// 간단한 로그 메서드
        /// </summary>
        public void LogEECPData(string cellId, string innerId, int zoneNumber, Output outputData)
        {
            var now = DateTime.Now;
            LogEECPData(now.AddSeconds(-10), now, cellId, innerId, zoneNumber, outputData);
        }
    }
}


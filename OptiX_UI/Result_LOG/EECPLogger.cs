using System;
using System.IO;
using System.Text;
using System.Globalization;

namespace OptiX_UI.Result_LOG
{
    /// <summary>
    /// EECP 로그 파일 생성 및 관리 클래스 (Singleton)
    /// 경로: D:\Project\Log\Result\EECP
    /// 파일명: EECP_YYYYMMDD.csv
    /// </summary>
    public class EECPLogger
    {
        private static readonly object _fileLock = new object();
        private static EECPLogger _instance;
        private readonly string _basePath = @"D:\Project\Log\Result\EECP";
        private readonly string _fileName;
        private readonly string _fullPath;

        /// <summary>
        /// Singleton Instance
        /// </summary>
        public static EECPLogger Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_fileLock)
                    {
                        if (_instance == null)
                        {
                            _instance = new EECPLogger();
                        }
                    }
                }
                return _instance;
            }
        }

        private EECPLogger()
        {
            lock (_fileLock)
            {
                try
                {
                    _fileName = $"EECP_{DateTime.Now:yyyyMMdd}.csv";
                    _fullPath = Path.Combine(_basePath, _fileName);
                    
                    System.Diagnostics.Debug.WriteLine($"EECP 로거 초기화: {_fullPath}");
                    
                    // 디렉토리가 없으면 생성
                    if (!Directory.Exists(_basePath))
                    {
                        Directory.CreateDirectory(_basePath);
                        System.Diagnostics.Debug.WriteLine($"EECP 디렉토리 생성: {_basePath}");
                    }
                    
                    // 파일이 없으면 헤더 생성 (이미 lock 안에 있음)
                    if (!File.Exists(_fullPath))
                    {
                        CreateHeader();
                        System.Diagnostics.Debug.WriteLine($"EECP 헤더 파일 생성: {_fullPath}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"EECP 로거 초기화 오류: {ex.Message}");
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
        /// <param name="startTime">시작 시간</param>
        /// <param name="endTime">종료 시간</param>
        /// <param name="cellId">셀 ID</param>
        /// <param name="innerId">내부 ID</param>
        /// <param name="zoneNumber">Zone 번호</param>
        /// <param name="outputData">구조체 출력 데이터</param>
        public void LogEECPData(DateTime startTime, DateTime endTime, string cellId, string innerId, int zoneNumber, OutputData outputData)
        {
            var logEntry = new StringBuilder();
            
            // TACT 계산 (소수점 3자리)
            double tact = (endTime - startTime).TotalSeconds;
            
            // 기본 정보
            logEntry.Append($"{startTime:yyyy:MM:dd HH:mm:ss:fff},");
            logEntry.Append($"{endTime:yyyy:MM:dd HH:mm:ss:fff},");
            logEntry.Append($"{tact:F3},"); // TACT 컬럼 추가
            logEntry.Append($"{cellId},");
            logEntry.Append($"{innerId},");
            logEntry.Append($"{zoneNumber},");
            
            // 구조체 데이터 처리 (패턴별 WAD 순서)
            for (int pattern = 0; pattern < 17; pattern++)
            {
                for (int wad = 0; wad < 7; wad++)
                {
                    if (wad < outputData.data.GetLength(0) && pattern < outputData.data.GetLength(1))
                    {
                        logEntry.Append($"{outputData.data[wad, pattern].x:F3},");
                        logEntry.Append($"{outputData.data[wad, pattern].y:F3},");
                        logEntry.Append($"{outputData.data[wad, pattern].u:F3},");
                        logEntry.Append($"{outputData.data[wad, pattern].v:F3},");
                        logEntry.Append($"{outputData.data[wad, pattern].L:F3},");
                        logEntry.Append($"{outputData.data[wad, pattern].cur:F3},");
                        logEntry.Append($"{outputData.data[wad, pattern].eff:F3},");
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
            
            // 파일에 추가 (동기화 처리)
            lock (_fileLock)
            {
                File.AppendAllText(_fullPath, logEntry.ToString(), Encoding.UTF8);
            }
        }

        /// <summary>
        /// 간단한 로그 메서드 (기본값 사용)
        /// </summary>
        public void LogEECPData(string cellId, string innerId, int zoneNumber, OutputData outputData)
        {
            var now = DateTime.Now;
            LogEECPData(now.AddSeconds(-10), now, cellId, innerId, zoneNumber, outputData);
        }
    }

    /// <summary>
    /// 패턴 구조체
    /// </summary>
    public struct Pattern
    {
        public double x;   // X 좌표 (CIE 1931 색공간)
        public double y;   // Y 좌표 (CIE 1931 색공간)
        public double u;   // u' 좌표 (CIE 1976 색공간)
        public double v;   // v' 좌표 (CIE 1976 색공간)
        public double L;   // 밝기 (Luminance, cd/m²)
        public double cur; // 전류 (Current, mA)
        public double eff; // 효율 (Efficiency, %)
    }

    /// <summary>
    /// LUT 파라미터 구조체
    /// </summary>
    public struct LutParameter
    {
        public double value1;
        public double value2;
        public double value3;
    }

    /// <summary>
    /// 출력 데이터 구조체
    /// </summary>
    public struct OutputData
    {
        public Pattern[,] data;      // [7][17] - WAD별 패턴 데이터
        public Pattern[] measure;    // [7] - 측정 데이터
        public LutParameter[] lut;   // [3] - LUT 파라미터
    }
}

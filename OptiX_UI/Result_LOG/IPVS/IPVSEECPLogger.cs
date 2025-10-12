using System;
using System.IO;
using System.Text;
using OptiX.DLL;
using OptiX.Common;

namespace OptiX.Result_LOG.IPVS
{
    /// <summary>
    /// IPVS EECP 로그 파일 생성 및 관리 클래스 (Singleton)
    /// 경로: INI 파일에서 로드 (IPVS_PATHS.EECP_FOLDER)
    /// 파일명: EECP_YYYYMMDD.csv
    /// </summary>
    public class IPVSEECPLogger
    {
        private static readonly object _fileLock = new object();
        private static IPVSEECPLogger _instance;
        private readonly string _basePath;
        private readonly string _fileName;
        private readonly string _fullPath;

        /// <summary>
        /// Singleton Instance
        /// </summary>
        public static IPVSEECPLogger Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_fileLock)
                    {
                        if (_instance == null)
                        {
                            _instance = new IPVSEECPLogger();
                        }
                    }
                }
                return _instance;
            }
        }

        private IPVSEECPLogger()
        {
            lock (_fileLock)
            {
                try
                {
                    // INI 파일에서 EECP 폴더 경로 읽기
                    _basePath = GlobalDataManager.GetValue("IPVS_PATHS", "EECP_FOLDER", @"D:\Project\Log\Result\IPVS\EECP");
                    _fileName = $"EECP_{DateTime.Now:yyyyMMdd}.csv";
                    _fullPath = Path.Combine(_basePath, _fileName);
                    
                    System.Diagnostics.Debug.WriteLine($"IPVS EECP 로거 초기화: {_fullPath}");
                    
                    // 디렉토리가 없으면 생성
                    if (!Directory.Exists(_basePath))
                    {
                        Directory.CreateDirectory(_basePath);
                        System.Diagnostics.Debug.WriteLine($"IPVS EECP 디렉토리 생성: {_basePath}");
                    }
                    
                    // 파일이 없으면 헤더 생성
                    if (!File.Exists(_fullPath))
                    {
                        CreateHeader();
                        System.Diagnostics.Debug.WriteLine($"IPVS EECP 헤더 파일 생성: {_fullPath}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"IPVS EECP 로거 초기화 오류: {ex.Message}");
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
            header.Append("START TIME,END TIME,TACT,CELL ID,INNER ID,ZONE,POINT");
            
            // WAD별 컬럼 생성 (IPVS는 7개 WAD 각도)
            string[] wadNames = { "", "_WAD_30", "_WAD_45", "_WAD_60", "_WAD_15", "_WAD_A", "_WAD_B" };
            for (int pointnum = 0; pointnum < 10; pointnum++)                
            {
                string s_point = (pointnum + 1).ToString();
                for (int wad = 0; wad < 7; wad++)
                {
                    string wadName = wadNames[wad];                    
                    header.Append($",W{wadName}_X_{s_point},W{wadName}_Y_{s_point},W{wadName}_L_{s_point},W{wadName}_전류_{s_point},W{wadName}_효율_{s_point}");
                }                
            }
            
            header.AppendLine();
            File.WriteAllText(_fullPath, header.ToString(), Encoding.UTF8);
        }

        /// <summary>
        /// EECP 로그 데이터 기록 (Zone별 1번 호출, 모든 Point의 모든 WAD 데이터를 한 줄에 기록)
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
            logEntry.Append($"ALL,"); // POINT 컬럼: 모든 포인트 데이터 포함
            
            // IPVS_data 처리 (10개 Point × 7개 WAD) - 헤더 순서와 동일
            for (int pointnum = 0; pointnum < 10; pointnum++)
            {
                for (int wad = 0; wad < 7; wad++)
                {
                    int index = wad * 10 + pointnum; // 1차원 배열 인덱스
                    if (index < outputData.IPVS_data.Length)
                    {
                        logEntry.Append($"{outputData.IPVS_data[index].x:F3},");
                        logEntry.Append($"{outputData.IPVS_data[index].y:F3},");
                        logEntry.Append($"{outputData.IPVS_data[index].L:F3},");
                        logEntry.Append($"{outputData.IPVS_data[index].cur:F3},");
                        logEntry.Append($"{outputData.IPVS_data[index].eff:F3},");
                    }
                    else
                    {
                        logEntry.Append("0.000,0.000,0.000,0.000,0.000,");
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


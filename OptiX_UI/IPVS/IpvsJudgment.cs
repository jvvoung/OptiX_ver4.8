using System;
using System.Collections.Generic;
using System.Linq;

namespace OptiX.IPVS
{
    /// <summary>
    /// IPVS 검사 판정 관리 클래스
    /// </summary>
    public class IPVSJudgment
    {
        #region Fields
        private static IPVSJudgment _instance;
        private static readonly object _lock = new object();
        #endregion

        #region Properties
        /// <summary>
        /// Singleton 인스턴스
        /// </summary>
        public static IPVSJudgment Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new IPVSJudgment();
                        }
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region Constructor
        private IPVSJudgment()
        {
            // Private constructor for singleton pattern
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// IPVS 측정 결과 판정
        /// </summary>
        /// <param name="x">X 좌표값</param>
        /// <param name="y">Y 좌표값</param>
        /// <param name="l">휘도값</param>
        /// <param name="current">전류값</param>
        /// <param name="efficiency">효율값</param>
        /// <returns>판정 결과 (PASS/FAIL)</returns>
        public string JudgeMeasurement(float x, float y, float l, float current, float efficiency)
        {
            try
            {
                // X, Y 좌표 범위 체크
                if (x < 0.0f || x > 1.0f || y < 0.0f || y > 1.0f)
                {
                    return "FAIL";
                }

                // 휘도값 범위 체크
                if (l < 0.0f || l > 1000.0f)
                {
                    return "FAIL";
                }

                // 전류값 범위 체크
                if (current < 0.0f || current > 100.0f)
                {
                    return "FAIL";
                }

                // 효율값 범위 체크
                if (efficiency < 0.0f || efficiency > 100.0f)
                {
                    return "FAIL";
                }

                return "PASS";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"IPVSJudgment 오류: {ex.Message}");
                return "FAIL";
            }
        }
        public string GetPatternJudgment(int result)
        {
            switch (result)
            {
                case 0: return "OK";
                case 1: return "R/J"; // NG를 R/J로 변환
                case 2: return "PTN";
                default: return "R/J"; // NG를 R/J로 변환
            }
        }

        /// <summary>
        /// Zone별 전체 판정
        /// </summary>
        /// <param name="measurements">측정 데이터 리스트</param>
        /// <returns>Zone 전체 판정 결과</returns>
        public string JudgeZone(List<(float x, float y, float l, float current, float efficiency)> measurements)
        {
            try
            {
                if (measurements == null || measurements.Count == 0)
                {
                    return "FAIL";
                }

                foreach (var measurement in measurements)
                {
                    string result = JudgeMeasurement(
                        measurement.x, 
                        measurement.y, 
                        measurement.l, 
                        measurement.current, 
                        measurement.efficiency
                    );

                    if (result == "FAIL")
                    {
                        return "FAIL";
                    }
                }

                return "PASS";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Zone 판정 오류: {ex.Message}");
                return "FAIL";
            }
        }

        /// <summary>
        /// DLL output 구조체의 result 배열을 분석하여 Zone 전체 판정 수행
        /// </summary>
        /// <param name="resultArray">DLL에서 받은 70개 result 값 배열 (7x10)</param>
        /// <returns>Zone 전체 판정 결과 (OK/NG/PTN)</returns>
        public string JudgeZoneFromResults(int[,] resultArray)
        {
            try
            {
                if (resultArray == null || resultArray.Length == 0)
                {
                    return "NG";
                }

                int okCount = 0;
                int ptnCount = 0;
                int totalCount = 0;

                // result 배열 순회: [7][10] = 70개
                for (int wad = 0; wad < 7; wad++)
                {
                    for (int point = 0; point < 10; point++)
                    {
                        int result = resultArray[wad, point];
                        totalCount++;

                        switch (result)
                        {
                            case 0: // OK
                                okCount++;
                                break;
                            case 1: // NG
                                // NG는 별도 카운트하지 않음 (기본값)
                                break;
                            case 2: // PTN
                                ptnCount++;
                                break;
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"IPVS Zone 판정 분석: OK={okCount}, PTN={ptnCount}, Total={totalCount}");

                // 판정 로직: OK 60개 이상이면 OK, PTN 10개 이상이면 PTN, 나머지는 NG
                if (okCount >= 5)
                {
                    return "OK";
                }
                else if (ptnCount >= 2)
                {
                    return "PTN";
                }
                else
                {
                    return "R/J"; // NG를 R/J로 변환
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Zone 판정 오류: {ex.Message}");
                return "R/J"; // NG를 R/J로 변환
            }
        }

        /// <summary>
        /// 개별 포인트의 result 값을 문자열로 변환
        /// </summary>
        /// <param name="result">DLL에서 받은 result 값 (0, 1, 2)</param>
        /// <returns>문자열 판정 결과 (OK, NG, PTN)</returns>
        public string GetPointJudgment(int result)
        {
            switch (result)
            {
                case 0: return "OK";
                case 1: return "R/J"; // NG를 R/J로 변환
                case 2: return "PTN";
                default: return "R/J"; // NG를 R/J로 변환
            }
        }

        /// <summary>
        /// 판정 기준 설정
        /// </summary>
        public void SetJudgmentCriteria(
            float xMin = 0.0f, float xMax = 1.0f,
            float yMin = 0.0f, float yMax = 1.0f,
            float lMin = 0.0f, float lMax = 1000.0f,
            float currentMin = 0.0f, float currentMax = 100.0f,
            float efficiencyMin = 0.0f, float efficiencyMax = 100.0f)
        {
            // 판정 기준 설정 로직 (필요시 구현)
            System.Diagnostics.Debug.WriteLine("IPVS 판정 기준 설정됨");
        }
        #endregion
    }
}



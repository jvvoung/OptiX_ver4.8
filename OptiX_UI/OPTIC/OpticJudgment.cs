using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptiX.OPTIC
{
    /// <summary>
    /// OPTIC 검사 판정 관리 클래스
    /// </summary>
    public class OpticJudgment
    {
        #region Fields
        private static OpticJudgment _instance;
        private static readonly object _lock = new object();
        #endregion

        #region Properties
        /// <summary>
        /// Singleton 인스턴스
        /// </summary>
        public static OpticJudgment Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new OpticJudgment();
                        }
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region Constructor
        private OpticJudgment()
        {
            // Private constructor for singleton pattern
        }
        #endregion

                                  #region Public Methods
        /// <summary>
        /// OPTIC 측정 결과 판정
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
                if (x < DLL.DllConstants.X_MIN || x > DLL.DllConstants.X_MAX || 
                    y < DLL.DllConstants.Y_MIN || y > DLL.DllConstants.Y_MAX)
                {
                    return "FAIL";
                }

                // 휘도값 범위 체크
                if (l < DLL.DllConstants.LUMINANCE_MIN || l > DLL.DllConstants.LUMINANCE_MAX)
                {
                    return "FAIL";
                }

                // 전류값 범위 체크
                if (current < DLL.DllConstants.CURRENT_MIN || current > DLL.DllConstants.CURRENT_MAX)
                {
                    return "FAIL";
                }

                // 효율값 범위 체크
                if (efficiency < DLL.DllConstants.EFFICIENCY_MIN || efficiency > DLL.DllConstants.EFFICIENCY_MAX)
                {
                    return "FAIL";
                }

                return "PASS";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OpticJudgment 오류: {ex.Message}");
                return "FAIL";
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
        /// <param name="resultArray">DLL에서 받은 OPTIC_DATA_SIZE개 result 값 배열 (MAX_WAD_COUNT x MAX_PATTERN_COUNT)</param>
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

                // result 배열 순회: [MAX_WAD_COUNT][MAX_PATTERN_COUNT] = OPTIC_DATA_SIZE개
                for (int wad = 0; wad < DLL.DllConstants.MAX_WAD_COUNT; wad++)
                {
                    for (int pattern = 0; pattern < DLL.DllConstants.MAX_PATTERN_COUNT; pattern++)
                    {
                        int result = resultArray[wad, pattern];
                        totalCount++;

                        switch (result)
                        {
                            case DLL.DllConstants.RESULT_OK:
                                okCount++;
                                break;
                            case DLL.DllConstants.RESULT_NG:
                                // NG는 별도 카운트하지 않음 (기본값)
                                break;
                            case DLL.DllConstants.RESULT_PTN:
                                ptnCount++;
                                break;
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Zone 판정 분석: OK={okCount}, PTN={ptnCount}, Total={totalCount}");

                // 판정 로직: OK가 OPTIC_OK_THRESHOLD 이상이면 OK, PTN이 OPTIC_PTN_THRESHOLD 이상이면 PTN, 나머지는 NG
                if (okCount >= DLL.DllConstants.OPTIC_OK_THRESHOLD)
                {
                    return "OK";
                }
                else if (ptnCount >= DLL.DllConstants.OPTIC_PTN_THRESHOLD)
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
        /// 개별 패턴의 result 값을 문자열로 변환
        /// </summary>
        /// <param name="result">DLL에서 받은 result 값 (RESULT_OK, RESULT_NG, RESULT_PTN)</param>
        /// <returns>문자열 판정 결과 (OK, NG, PTN)</returns>
        public string GetPatternJudgment(int result)
        {
            switch (result)
            {
                case DLL.DllConstants.RESULT_OK: return "OK";
                case DLL.DllConstants.RESULT_NG: return "R/J"; // NG를 R/J로 변환
                case DLL.DllConstants.RESULT_PTN: return "PTN";
                default: return "R/J"; // NG를 R/J로 변환
            }
        }

        /// <summary>
        /// 판정 기준 설정
        /// </summary>
        /// <param name="xMin">X 최소값</param>
        /// <param name="xMax">X 최대값</param>
        /// <param name="yMin">Y 최소값</param>
        /// <param name="yMax">Y 최대값</param>
        /// <param name="lMin">휘도 최소값</param>
        /// <param name="lMax">휘도 최대값</param>
        /// <param name="currentMin">전류 최소값</param>
        /// <param name="currentMax">전류 최대값</param>
        /// <param name="efficiencyMin">효율 최소값</param>
        /// <param name="efficiencyMax">효율 최대값</param>
        public void SetJudgmentCriteria(
            float xMin = DLL.DllConstants.X_MIN, float xMax = DLL.DllConstants.X_MAX,
            float yMin = DLL.DllConstants.Y_MIN, float yMax = DLL.DllConstants.Y_MAX,
            float lMin = DLL.DllConstants.LUMINANCE_MIN, float lMax = DLL.DllConstants.LUMINANCE_MAX,
            float currentMin = DLL.DllConstants.CURRENT_MIN, float currentMax = DLL.DllConstants.CURRENT_MAX,
            float efficiencyMin = DLL.DllConstants.EFFICIENCY_MIN, float efficiencyMax = DLL.DllConstants.EFFICIENCY_MAX)
        {
            // 판정 기준 설정 로직 (필요시 구현)
            System.Diagnostics.Debug.WriteLine("OPTIC 판정 기준 설정됨");
        }
        #endregion
    }
}



using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptiX.IPVS
{
    /// <summary>
    /// IPVS 검사 판정 관리 클래스
    /// </summary>
    public class IpvsJudgment
    {
        #region Fields
        private static IpvsJudgment _instance;
        private static readonly object _lock = new object();
        #endregion

        #region Properties
        /// <summary>
        /// Singleton 인스턴스
        /// </summary>
        public static IpvsJudgment Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new IpvsJudgment();
                        }
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region Constructor
        private IpvsJudgment()
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
                // IPVS 특화 판정 기준
                
                // X, Y 좌표 범위 체크 (IPVS는 더 넓은 범위 허용)
                if (x < -0.1f || x > 1.1f || y < -0.1f || y > 1.1f)
                {
                    return "FAIL";
                }

                // 휘도값 범위 체크 (IPVS는 더 높은 휘도 허용)
                if (l < 0.0f || l > 2000.0f)
                {
                    return "FAIL";
                }

                // 전류값 범위 체크
                if (current < 0.0f || current > 150.0f)
                {
                    return "FAIL";
                }

                // 효율값 범위 체크 (IPVS는 더 높은 효율 허용)
                if (efficiency < 0.0f || efficiency > 120.0f)
                {
                    return "FAIL";
                }

                // IPVS 특화 검증 로직
                if (IsValidIpvsMeasurement(x, y, l, current, efficiency))
                {
                    return "PASS";
                }
                else
                {
                    return "FAIL";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"IpvsJudgment 오류: {ex.Message}");
                return "FAIL";
            }
        }

        /// <summary>
        /// Zone별 전체 판정 (IPVS 특화)
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

                // IPVS는 모든 측정값이 유효해야 함
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

                // IPVS 특화 추가 검증
                if (IsValidIpvsZone(measurements))
                {
                    return "PASS";
                }
                else
                {
                    return "FAIL";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"IPVS Zone 판정 오류: {ex.Message}");
                return "FAIL";
            }
        }

        /// <summary>
        /// IPVS 특화 측정값 검증
        /// </summary>
        private bool IsValidIpvsMeasurement(float x, float y, float l, float current, float efficiency)
        {
            try
            {
                // IPVS 특화 검증 로직
                
                // 좌표 안정성 체크
                if (Math.Abs(x - 0.5f) > 0.6f || Math.Abs(y - 0.5f) > 0.6f)
                {
                    return false;
                }

                // 휘도 일관성 체크
                if (l < 50.0f || l > 1800.0f)
                {
                    return false;
                }

                // 전류 안정성 체크
                if (current < 10.0f || current > 140.0f)
                {
                    return false;
                }

                // 효율 최적화 체크
                if (efficiency < 80.0f || efficiency > 115.0f)
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// IPVS Zone 전체 검증
        /// </summary>
        private bool IsValidIpvsZone(List<(float x, float y, float l, float current, float efficiency)> measurements)
        {
            try
            {
                // Zone 내 측정값들의 일관성 체크
                var xValues = measurements.Select(m => m.x).ToList();
                var yValues = measurements.Select(m => m.y).ToList();
                var lValues = measurements.Select(m => m.l).ToList();

                // 좌표 분산 체크
                double xVariance = CalculateVariance(xValues);
                double yVariance = CalculateVariance(yValues);
                double lVariance = CalculateVariance(lValues);

                // IPVS는 더 엄격한 일관성 요구
                if (xVariance > 0.1 || yVariance > 0.1 || lVariance > 10000)
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 분산 계산
        /// </summary>
        private double CalculateVariance(List<float> values)
        {
            if (values.Count <= 1) return 0;

            double mean = values.Average();
            double variance = values.Sum(v => Math.Pow(v - mean, 2)) / (values.Count - 1);
            return variance;
        }

        /// <summary>
        /// IPVS 판정 기준 설정
        /// </summary>
        public void SetIpvsJudgmentCriteria(
            float xMin = -0.1f, float xMax = 1.1f,
            float yMin = -0.1f, float yMax = 1.1f,
            float lMin = 0.0f, float lMax = 2000.0f,
            float currentMin = 0.0f, float currentMax = 150.0f,
            float efficiencyMin = 0.0f, float efficiencyMax = 120.0f)
        {
            // IPVS 특화 판정 기준 설정 로직
            System.Diagnostics.Debug.WriteLine("IPVS 판정 기준 설정됨");
        }

        /// <summary>
        /// IPVS 특화 품질 등급 판정
        /// </summary>
        /// <param name="measurements">측정 데이터</param>
        /// <returns>품질 등급 (A, B, C, FAIL)</returns>
        public string JudgeQualityGrade(List<(float x, float y, float l, float current, float efficiency)> measurements)
        {
            try
            {
                if (measurements == null || measurements.Count == 0)
                {
                    return "FAIL";
                }

                // IPVS 품질 등급 계산
                var avgEfficiency = measurements.Average(m => m.efficiency);
                var avgLuminance = measurements.Average(m => m.l);
                var consistency = CalculateConsistency(measurements);

                if (avgEfficiency >= 110.0f && avgLuminance >= 1500.0f && consistency >= 0.95f)
                {
                    return "A";
                }
                else if (avgEfficiency >= 100.0f && avgLuminance >= 1200.0f && consistency >= 0.90f)
                {
                    return "B";
                }
                else if (avgEfficiency >= 90.0f && avgLuminance >= 1000.0f && consistency >= 0.85f)
                {
                    return "C";
                }
                else
                {
                    return "FAIL";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"IPVS 품질 등급 판정 오류: {ex.Message}");
                return "FAIL";
            }
        }

        /// <summary>
        /// 측정값 일관성 계산
        /// </summary>
        private float CalculateConsistency(List<(float x, float y, float l, float current, float efficiency)> measurements)
        {
            try
            {
                var efficiencyValues = measurements.Select(m => m.efficiency).ToList();
                var mean = efficiencyValues.Average();
                var variance = efficiencyValues.Sum(v => Math.Pow(v - mean, 2)) / efficiencyValues.Count;
                var stdDev = Math.Sqrt(variance);
                
                // 표준편차가 작을수록 일관성이 높음
                return Math.Max(0, 1.0f - (float)(stdDev / mean));
            }
            catch
            {
                return 0.0f;
            }
        }
        #endregion
    }
}






using System;

namespace OptiX.IPVS
{
    /// <summary>
    /// IPVS 페이지 공통 헬퍼 메서드 모음
    /// 
    /// 역할:
    /// - WAD 각도 문자열을 enum/인덱스로 변환
    /// - Point 번호를 배열 인덱스로 변환
    /// - 기타 공통 유틸리티 함수
    /// 
    /// 특징:
    /// - 모든 메서드는 static
    /// - 상태를 가지지 않음 (Stateless)
    /// - 다른 클래스에서 자유롭게 호출 가능
    /// </summary>
    public static class IPVSHelpers
    {
        /// <summary>
        /// WAD 문자열을 배열 인덱스(0 ~ MAX_WAD_COUNT-1)로 변환
        /// </summary>
        public static int GetWadArrayIndex(string wadValue)
        {
            switch (wadValue?.Trim())
            {
                case "0": return 0;
                case "30": return 1;
                case "45": return 2;
                case "60": return 3;
                case "15": return 4;
                case "A": return 5;
                case "B": return 6;
                default: return 0;
            }
        }

        /// <summary>
        /// Point 번호를 배열 인덱스(0 ~ MAX_POINT_COUNT-1)로 변환
        /// </summary>
        public static int GetPointArrayIndex(int pointNumber)
        {
            // Point는 1 ~ MAX_POINT_COUNT까지, 배열 인덱스는 0 ~ MAX_POINT_COUNT-1
            if (pointNumber >= 1 && pointNumber <= DLL.DllConstants.MAX_POINT_COUNT)
            {
                return pointNumber - 1;
            }
            return 0;
        }

        /// <summary>
        /// float 값의 유효성 검사 (NaN, Infinity 체크)
        /// </summary>
        public static bool IsValidFloat(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        /// <summary>
        /// 이상한 값을 0으로 변환
        /// </summary>
        public static float SanitizeFloat(float value)
        {
            if (!IsValidFloat(value))
                return 0.0f;
            return value;
        }

        /// <summary>
        /// 2D 배열 인덱스를 1D 배열 인덱스로 변환 (IPVS_data 접근용)
        /// </summary>
        /// <param name="wadIndex">WAD 인덱스 (0~6)</param>
        /// <param name="pointIndex">Point 인덱스 (0~9)</param>
        /// <returns>1D 배열 인덱스</returns>
        public static int Get1DIndex(int wadIndex, int pointIndex)
        {
            // output.IPVS_data[wadIndex * 10 + pointIndex]
            return wadIndex * 10 + pointIndex;
        }
    }
}


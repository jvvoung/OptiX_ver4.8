using System;

namespace OptiX.OPTIC
{
    /// <summary>
    /// OPTIC 페이지 공통 헬퍼 메서드 모음
    /// 
    /// 역할:
    /// - WAD 각도 문자열을 enum/인덱스로 변환
    /// - Pattern 카테고리 문자열을 배열 인덱스로 변환
    /// - 기타 공통 유틸리티 함수
    /// 
    /// 특징:
    /// - 모든 메서드는 static
    /// - 상태를 가지지 않음 (Stateless)
    /// - 다른 클래스에서 자유롭게 호출 가능
    /// </summary>
    public static class OpticHelpers
    {
        /// <summary>
        /// WAD 문자열을 WadAngle enum으로 변환
        /// </summary>
        public static WadAngle GetWadAngle(string wadValue)
        {
            switch (wadValue?.Trim())
            {
                case "0": return WadAngle.Angle0;
                case "30": return WadAngle.Angle30;
                case "45": return WadAngle.Angle45;
                case "60": return WadAngle.Angle60;
                case "15": return WadAngle.Angle15;
                case "A": return WadAngle.AngleA;
                case "B": return WadAngle.AngleB;
                default: return WadAngle.Angle0;
            }
        }

        /// <summary>
        /// WAD 문자열을 배열 인덱스(0 ~ MAX_WAD_COUNT-1)로 변환
        /// </summary>
        public static int GetWadArrayIndex(string wadValue)
        {
            return (int)GetWadAngle(wadValue);
        }

        /// <summary>
        /// Pattern 카테고리를 배열 인덱스(0 ~ MAX_PATTERN_COUNT-1)로 변환
        /// </summary>
        public static int GetPatternArrayIndex(string category)
        {
            switch (category?.Trim())
            {
                case "W": return 0;
                case "R": return 1;
                case "G": return 2;
                case "B": return 3;
                case "WG": return 4;
                case "WG2": return 5;
                case "WG3": return 6;
                case "WG4": return 7;
                case "WG5": return 8;
                case "WG6": return 9;
                case "WG7": return 10;
                case "WG8": return 11;
                case "WG9": return 12;
                case "WG10": return 13;
                case "WG11": return 14;
                case "WG12": return 15;
                case "WG13": return 16;
                default: return -1;
            }
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
    }
}






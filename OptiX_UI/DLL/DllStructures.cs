using System;
using System.Runtime.InteropServices;

namespace OptiX.DLL
{
    /// <summary>
    /// DLL 입력 구조체 (C++ struct input과 일치)
    /// C++: struct input { char CELL_ID[256]; char INNER_ID[256]; int total_point; int cur_point; }
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct Input
    {
        /// <summary>
        /// 셀 ID (최대 256자, ANSI)
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = DllConstants.STRING_BUFFER_SIZE)]
        public string CELL_ID;
        
        /// <summary>
        /// 내부 ID (최대 256자, ANSI)
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = DllConstants.STRING_BUFFER_SIZE)]
        public string INNER_ID;
        
        /// <summary>
        /// 전체 포인트 수
        /// </summary>
        public int total_point;
        
        /// <summary>
        /// 현재 포인트
        /// </summary>
        public int cur_point;
    }

    /// <summary>
    /// 측정 데이터 패턴 구조체 (C++ struct pattern과 일치)
    /// C++: struct pattern { float x, y, u, v, L, cur, eff; int result; }
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Pattern
    {
        /// <summary>
        /// CIE 1931 X 좌표
        /// </summary>
        public float x;
        
        /// <summary>
        /// CIE 1931 Y 좌표
        /// </summary>
        public float y;
        
        /// <summary>
        /// CIE 1976 u' 좌표
        /// </summary>
        public float u;
        
        /// <summary>
        /// CIE 1976 v' 좌표
        /// </summary>
        public float v;
        
        /// <summary>
        /// 밝기 (cd/m²)
        /// </summary>
        public float L;
        
        /// <summary>
        /// 전류 (mA)
        /// </summary>
        public float cur;
        
        /// <summary>
        /// 효율 (%)
        /// </summary>
        public float eff;
        
        /// <summary>
        /// 결과 플래그 (0:OK, 1:NG, 2:PTN)
        /// </summary>
        public int result;
    }

    /// <summary>
    /// DLL 출력 구조체 (C++ struct output과 일치)
    /// C++: struct output { struct pattern data[7][17]; struct pattern IPVS_data[7][10]; struct pattern measure[7]; struct lut_parameter lut[3]; }
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Output
    {
        /// <summary>
        /// MAX_WAD_COUNT × MAX_PATTERN_COUNT 패턴 측정 데이터 배열 (MTP용)
        /// [MAX_WAD_COUNT]:WAD => 0:0도, 1:30도, 2:45도, 3:60도, 4:15도, 5:A도, 6:B도
        /// [MAX_PATTERN_COUNT]:패턴 => 0:W, 1:R, 2:G, 3:B, 4:WG, 5:WG2, 6:WG3 ~ 16:WG13
        /// 접근: data[wadIndex * MAX_PATTERN_COUNT + patternIndex]
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = DllConstants.OPTIC_DATA_SIZE)]
        public Pattern[] data;
        
        /// <summary>
        /// MAX_WAD_COUNT × MAX_POINT_COUNT 측정 데이터 배열 (IPVS용)
        /// [MAX_WAD_COUNT]:WAD => 0:0도, 1:30도, 2:45도, 3:60도, 4:15도, 5:A도, 6:B도
        /// [MAX_POINT_COUNT]:포인트수
        /// 접근: IPVS_data[wadIndex * MAX_POINT_COUNT + pointIndex]
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = DllConstants.IPVS_DATA_SIZE)]
        public Pattern[] IPVS_data;
        
        /// <summary>
        /// MAX_WAD_COUNT개 WAD의 현재 측정값
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = DllConstants.MAX_WAD_COUNT)]
        public Pattern[] measure;
        
        /// <summary>
        /// RGB_CHANNEL_COUNT 채널의 LUT 파라미터 (0:Red, 1:Green, 2:Blue)
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = DllConstants.RGB_CHANNEL_COUNT)]
        public LUTParameter[] lut;
    }

    /// <summary>
    /// LUT 데이터 구조체 (C++ struct LUT_Data와 일치)
    /// C++: struct LUT_Data { int index; double voltage; double luminance; }
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LUT_Data
    {
        /// <summary>
        /// 계조 인덱스
        /// </summary>
        public int index;
        
        /// <summary>
        /// 전압 (V)
        /// </summary>
        public double voltage;
        
        /// <summary>
        /// 휘도 (cd/m²)
        /// </summary>
        public double luminance;
    }

    /// <summary>
    /// LUT 파라미터 구조체 (C++ struct lut_parameter와 일치)
    /// C++: struct lut_parameter { float max_lumi; float max_index; float gamma; float black; }
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LUTParameter
    {
        /// <summary>
        /// 최대 밝기 (cd/m²)
        /// </summary>
        public float max_lumi;
        
        /// <summary>
        /// 최대 인덱스
        /// </summary>
        public float max_index;
        
        /// <summary>
        /// 감마 값
        /// </summary>
        public float gamma;
        
        /// <summary>
        /// 블랙 레벨 (cd/m²)
        /// </summary>
        public float black;
    }
}



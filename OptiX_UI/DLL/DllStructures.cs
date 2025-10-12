using System;
using System.Runtime.InteropServices;

namespace OptiX.DLL
{
    // DLL 입력 구조체 (C++ struct input과 일치)
    [StructLayout(LayoutKind.Sequential)]
    public struct Input
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = DllConstants.STRING_BUFFER_SIZE)]
        public string CELL_ID;
        
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = DllConstants.STRING_BUFFER_SIZE)]
        public string INNER_ID;
        
        public int total_point;
        
        public int cur_point;
    }

    // 측정 데이터 패턴 구조체 (C++ struct Pattern과 일치)
    [StructLayout(LayoutKind.Sequential)]
    public struct Pattern
    {
        public float x;      // CIE 1931 X 좌표
        public float y;      // CIE 1931 Y 좌표
        public float u;      // CIE 1976 u' 좌표
        public float v;      // CIE 1976 v' 좌표
        public float L;      // 밝기 (cd/m²)
        public float cur;    // 전류 (mA)
        public float eff;    // 효율 (%)
        public int result;   // 결과 플래그
    }

    // DLL 출력 구조체 (C++ struct output과 일치)
    [StructLayout(LayoutKind.Sequential)]
    public struct Output
    {
        // MAX_WAD_COUNT × MAX_PATTERN_COUNT 패턴 측정 데이터 배열 (MTP용)
        // [MAX_WAD_COUNT]:WAD => 0:0도, 1:30도, 2:45도, 3:60도, 4:15도, 5:A도, 6:B도
        // [MAX_PATTERN_COUNT]:패턴 => 0:W, 1:R, 2:G, 3:B, 4:WG, 5:WG2, 6:WG3 ~ 16:WG13
        // 접근: data[wadIndex * MAX_PATTERN_COUNT + patternIndex]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = DllConstants.OPTIC_DATA_SIZE)]
        public Pattern[] data;
        
        // MAX_WAD_COUNT × MAX_POINT_COUNT 측정 데이터 배열 (IPVS용)
        // [MAX_WAD_COUNT]:WAD => 0:0도, 1:30도, 2:45도, 3:60도, 4:15도, 5:A도, 6:B도
        // [MAX_POINT_COUNT]:포인트수
        // 접근: IPVS_data[wadIndex * MAX_POINT_COUNT + pointIndex]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = DllConstants.IPVS_DATA_SIZE)]
        public Pattern[] IPVS_data;
        
        // MAX_WAD_COUNT개 WAD의 현재 측정값
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = DllConstants.MAX_WAD_COUNT)]
        public Pattern[] measure;
        
        // RGB_CHANNEL_COUNT 채널의 LUT 파라미터
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = DllConstants.RGB_CHANNEL_COUNT)]
        public LUTParameter[] lut;
    }

    // LUT 데이터 구조체 (C++ struct LUT_Data와 일치)
    [StructLayout(LayoutKind.Sequential)]
    public struct LUT_Data
    {
        public int index;         // 계조 인덱스
        public double voltage;    // 전압 (V)
        public double luminance;  // 휘도 (cd/m²)
    }

    // LUT 파라미터 구조체 (C++ struct LUTParameter와 일치)
    [StructLayout(LayoutKind.Sequential)]
    public struct LUTParameter
    {
        public float max_lumi;   // 최대 밝기 (cd/m²)
        public float max_index;  // 최대 인덱스
        public float gamma;      // 감마 값
        public float black;      // 블랙 레벨 (cd/m²)
    }
}



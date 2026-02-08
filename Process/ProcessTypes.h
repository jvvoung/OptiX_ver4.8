#pragma once
// ProcessTypes.h : Process DLL에서 사용하는 구조체 및 타입 정의
// C# DllImport와 호환되도록 설계됨

#pragma pack(push, 1) // 1바이트 정렬 (C# Pack = 1과 매칭)

#ifdef __cplusplus
extern "C" {
#endif

    // 입력 구조체 - 셀 ID 및 테스트 정보
    struct input {
        char CELL_ID[256];      // 셀 ID
        char INNER_ID[256];     // 내부 ID  
        int total_point;        // 전체 포인트 수
        int cur_point;          // 현재 포인트
    };

    // 측정 패턴 구조체 - 단일 측정값
    struct pattern {
        float x, y;             // CIE 1931 좌표
        float u, v;             // CIE 1976 좌표
        float L;                // 휘도 (cd/m²)
        float cur;              // 전류 (mA)
        float eff;              // 효율 (%)
        int result;             // 판정 결과 (0:OK, 1:NG, 2:PTN)
    };

    // LUT 데이터 구조체
    struct LUT_Data {
        int index;              // 계조 index
        double voltage;         // 전압
        double luminance;       // 휘도
    };

    // LUT 파라미터 구조체
    struct lut_parameter {
        float max_lumi;         // 최대 밝기
        float max_index;        // 최대 인덱스
        float gamma;            // 감마 값
        float black;            // 블랙 레벨
    };

    // 출력 구조체 - 모든 측정 데이터
    struct output {
        struct pattern data[7][17];        // MTP 데이터 [WAD][패턴]
        //[7]:WAD => 0:0도, 1:30도, 2:45도, 3:60도, 4:15도, 5:A도, 6:B도
        //[17]:패턴 => 0:W, 1:R, 2:G, 3:B, 4:WG, 5:WG2, 6:WG3 ~ 16:WG13

        struct pattern IPVS_data[7][10];   // IPVS 데이터 [WAD][포인트]
        //[7]:WAD => 0:0도, 1:30도, 2:45도, 3:60도, 4:15도, 5:A도, 6:B도
        //[10]:포인트수

        struct pattern measure[7];         // 현재 측정값 [WAD]
        struct lut_parameter lut[3];       // LUT 파라미터 [RGB]
    };

    // 포트 연결 상태 관리 구조체 (25.02.08 - 종료 처리 강화)
    struct port_state {
        int pg_port;           // PG 포트 번호 (-1: 연결 안 됨)
        int meas_port;         // MEAS 포트 번호 (-1: 연결 안 됨)
        bool pg_connected;     // PG 연결 상태
        bool meas_connected;   // MEAS 연결 상태
    };

#ifdef __cplusplus
}
#endif

#pragma pack(pop) // 정렬 복원

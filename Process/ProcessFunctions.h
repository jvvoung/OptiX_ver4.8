#pragma once
// ProcessFunctions.h : Process DLL 외부 함수 선언
// C# DllImport에서 호출 가능한 C 스타일 함수들

#include "ProcessTypes.h"
#include <vector>

#ifdef __cplusplus
extern "C" {
#endif

    // MTP 테스트 함수
    __declspec(dllexport) int MTP_test(struct input* in, struct output* out);

    // IPVS 테스트 함수  
    __declspec(dllexport) int IPVS_test(struct input* in, struct output* out);

    // PG 제어 함수
    __declspec(dllexport) bool PGTurn(int port);
    __declspec(dllexport) bool PGPattern(int pattern);
    __declspec(dllexport) bool PGVoltagesnd(int RV, int GV, int BV);

    // 측정 제어 함수
    __declspec(dllexport) bool Meas_Turn(int port);
    __declspec(dllexport) bool Getdata(struct output* out);

    // LUT 계산 함수
    __declspec(dllexport) bool getLUTdata(int rgb, float RV, float GV, float BV, 
                                          int interval, int cnt, struct output* out);

    // ===== 장비 종료 함수 (25.02.08 - 종료 처리 강화) =====
    
    /// <summary>
    /// PG(Pattern Generator) 포트 연결 해제 및 전원 차단
    /// 호출 시점: 프로그램 종료 시 또는 테스트 완료 후
    /// </summary>
    __declspec(dllexport) bool pg_off();

    /// <summary>
    /// 측정기(Measurement Equipment) 포트 연결 해제
    /// 호출 시점: 프로그램 종료 시 또는 테스트 완료 후
    /// </summary>
    __declspec(dllexport) bool meas_off();

    /// <summary>
    /// 모든 장비 리소스 해제 (pg_off + meas_off)
    /// 호출 시점: 프로그램 종료 시 (OnExit)
    /// </summary>
    __declspec(dllexport) bool cleanup_all_devices();

    /// <summary>
    /// 현재 포트 연결 상태 조회
    /// </summary>
    __declspec(dllexport) void get_port_state(struct port_state* state);

#ifdef __cplusplus
}
#endif

#ifdef __cplusplus
// C++ 전용 내부 함수
void cal_lut(std::vector<LUT_Data> pattern_inf[3], struct output* out);
#endif

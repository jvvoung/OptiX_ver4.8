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

#ifdef __cplusplus
}
#endif

#ifdef __cplusplus
// C++ 전용 내부 함수
void cal_lut(std::vector<LUT_Data> pattern_inf[3], struct output* out);
#endif

#pragma once
#include <random>
#include <vector>
#include <cmath>



#ifdef __cplusplus
extern "C" {
#endif
    struct input {
        char CELL_ID[256];
        char INNER_ID[256];
        int total_point;
        int cur_point;
    };

    struct pattern {
        float x, y, u,v, L, cur, eff;
        int result;
    };


    struct LUT_Data {
        int index;       // 계조 index
        double voltage;  // 전압
        double luminance; // 휘도
    };
    struct lut_parameter {
        float max_lumi;
        float max_index;
        float gamma;
        float black;
    };
    
    struct output {
        struct pattern data[7][17]; // struct 키워드 명시
        //[7]:WAD => 0:0도, 1:30도, 2:45도, 3:60도, 4:15도, 5:A도, 6:B도
        //[17]:패턴 => 0:W, 1:R, 2:G, 3:B, 4:WG, 5:WG2, 6:WG3 ~ 16:WG13
        struct pattern measure[7];
        struct lut_parameter lut[3]; 
    };


    __declspec(dllexport) int test(struct input* in, struct output* out);
    __declspec(dllexport) bool PGTurn(int port);
    __declspec(dllexport) bool PGPattern(int pattern);
    __declspec(dllexport) bool PGVoltagesnd(int RV, int GV, int BV);
    __declspec(dllexport) bool Meas_Turn(int port);
    __declspec(dllexport) bool Getdata(struct output* out);
    __declspec(dllexport) bool getLUTdata(int rgb, float RV, float GV, float BV, int interval, int cnt, struct output* out);

#ifdef __cplusplus
}
#endif

#ifdef __cplusplus
void cal_lut(std::vector<LUT_Data> pattern_inf[3], struct output* out);
#endif
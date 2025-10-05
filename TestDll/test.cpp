#include "pch.h"
#include "test.h"
#include <random>

extern "C" {
    __declspec(dllexport) int test(struct input* in, struct output* out) {
        if (in == nullptr || out == nullptr) {
            return 0;
        }
        int cnt = 0;
        for (int i = 0; i < 7; i++) {
            for (int j = 0; j < 17; j++) {
                out->data[i][j].x = cnt + 1;
                out->data[i][j].y = cnt + 2;
                out->data[i][j].L = cnt + 3;
                out->data[i][j].cur = cnt + 4;
                out->data[i][j].eff = cnt + 5;
                cnt++;
            }
        }
        return 1;
    }
    __declspec(dllexport) bool PGTurn(int port) {
        if (port < 0)
            return false;
        return true;
    }
    __declspec(dllexport) bool PGPattern(int pattern) {
        if (pattern<0)
            return false;
        return true;
    }
    __declspec(dllexport) bool PGVoltagesnd(int RV, int GV, int BV) {
        if (RV == 0 || GV == 0 || BV == 0)
            return false;
        return true;
    }
    __declspec(dllexport) bool Meas_Turn(int port) {
        if (port < 0)
            return false;
        return true;
    }
    __declspec(dllexport) bool Getdata(struct output* out) {
        if (out == nullptr) {
            return false;
        }
        
        // 기본 WAD 인덱스 (0)를 사용
        int wad = 0;
        
        static std::random_device rd;
        static std::mt19937 gen(rd());
        static std::uniform_real_distribution<double> dis_xy(0.28, 0.35);
        static std::uniform_int_distribution<int> dis_L(1000, 1500);
        static std::uniform_int_distribution<int> dis_eff_cur(30, 800);

        // 랜덤값 생성
        out->measure[wad].x = dis_xy(gen);
        out->measure[wad].y = dis_xy(gen);
        out->measure[wad].L = dis_L(gen);
        out->measure[wad].eff = dis_eff_cur(gen);
        out->measure[wad].cur = dis_eff_cur(gen);
        out->measure[wad].u = (4 * out->measure[wad].x) / (-2 * out->measure[wad].x + 12 * out->measure[wad].y + 3);
        out->measure[wad].v = (9 * out->measure[wad].y) / (-2 * out->measure[wad].x + 12 * out->measure[wad].y + 3);
        return true;
    }
    __declspec(dllexport) bool getLUTdata(int rgb, float RV, float GV, float BV, int interval, int cnt, struct output* out) {
        if (out == nullptr) {
            return false;
        }
        static std::random_device rd;
        static std::mt19937 gen(rd());
        static std::uniform_real_distribution<float> max_L(1000.0f, 1500.0f);
        static std::uniform_int_distribution<int> max_index(3000, 3500);
        static std::uniform_real_distribution<float> gamma(2.0f, 2.9f);
        static std::uniform_real_distribution<float> black(0.0f, 0.1f);
        out->lut[rgb].max_lumi = max_L(gen);
        out->lut[rgb].max_index = max_index(gen);
        out->lut[rgb].gamma = gamma(gen);
        out->lut[rgb].black = black(gen);
        
        return true;
    }
}
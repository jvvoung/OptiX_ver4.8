// ProcessFunctions.cpp : Process DLL 비즈니스 로직 구현
// TESTDLL에서 이전된 모든 함수 구현

#include "pch.h"
#include "ProcessFunctions.h"
#include "Process.h"
#include <random>
#include <cmath>
#include <ctime>

extern "C" {

    //25.10.30 - MTP 테스트 함수 - 7x17 패턴 데이터 생성
    // AFX_MANAGE_STATE 제거 (전역 Lock으로 인한 Zone 간 경쟁 제거)
    __declspec(dllexport) int MTP_test(struct input* in, struct output* out) 
    {
        // AFX_MANAGE_STATE(AfxGetStaticModuleState()); // 제거! (MFC 리소스 미사용)

        if (in == nullptr || out == nullptr) {
            return 0;
        }

        // 랜덤 시드 초기화
        static bool first_call = true;
        if (first_call) {
            srand((unsigned int)(time(NULL) * 1000 + (uintptr_t)in));
            first_call = false;
        }

        int cnt = 0;
        int ok = 0;
        int ng = 0;
        int ptn = 0;

        // 7개 WAD, 17개 패턴 데이터 생성
        for (int i = 0; i < 7; i++) {
            for (int j = 0; j < 17; j++) {
                out->data[i][j].x = cnt + 1.0f;
                out->data[i][j].y = cnt + 2.0f;
                out->data[i][j].L = cnt + 3.0f;
                out->data[i][j].cur = cnt + 4.0f;
                out->data[i][j].eff = cnt + 5.0f;

                // 랜덤 판정 생성 (80% OK, 10% NG, 10% PTN)
                int random = (rand() + cnt) % 10;

                if (random < 8) {
                    out->data[i][j].result = 0;  // OK
                    ok++;
                }
                else if (random == 8) {
                    out->data[i][j].result = 1;  // NG
                    ng++;
                }
                else {
                    out->data[i][j].result = 2;  // PTN
                    ptn++;
                }
                cnt++;
            }
        }
        return 1;
    }

    //25.10.30 - IPVS 테스트 함수 - 7xN 포인트 데이터 생성
    // AFX_MANAGE_STATE 제거 (전역 Lock으로 인한 Zone 간 경쟁 제거)
    __declspec(dllexport) int IPVS_test(struct input* in, struct output* out) 
    {
        // AFX_MANAGE_STATE(AfxGetStaticModuleState()); // 제거! (MFC 리소스 미사용)

        if (in == nullptr || out == nullptr) {
            return 0;
        }

        int point = in->cur_point;

        // 랜덤 시드 초기화
        static bool first_call = true;
        if (first_call) {
            srand((unsigned int)(time(NULL) * 1000 + (uintptr_t)in));
            first_call = false;
        }

        int cnt = 0;
        int ok = 0;
        int ng = 0;
        int ptn = 0;

        // 7개 WAD의 현재 포인트 데이터 생성
        for (int i = 0; i < 7; i++) {
            out->IPVS_data[i][point].x = cnt + 1.0f;
            out->IPVS_data[i][point].y = cnt + 2.0f;
            out->IPVS_data[i][point].L = cnt + 3.0f;
            out->IPVS_data[i][point].cur = cnt + 4.0f;
            out->IPVS_data[i][point].eff = cnt + 5.0f;

            // 랜덤 판정 생성
            int random = (rand() + cnt) % 10;

            if (random < 8) {
                out->IPVS_data[i][point].result = 0;  // OK
                ok++;
            }
            else if (random == 8) {
                out->IPVS_data[i][point].result = 1;  // NG
                ng++;
            }
            else {
                out->IPVS_data[i][point].result = 2;  // PTN
                ptn++;
            }
            cnt++;
        }
        return 1;
    }

    // PG 포트 제어
    //25.10.30 - AFX_MANAGE_STATE 제거 (전역 Lock 제거)
    __declspec(dllexport) bool PGTurn(int port) 
    {
        // AFX_MANAGE_STATE(AfxGetStaticModuleState()); // 제거!

        if (port < 0)
            return false;
        return true;
    }

    //25.10.30 - PG 패턴 제어 (AFX_MANAGE_STATE 제거)
    __declspec(dllexport) bool PGPattern(int pattern) 
    {
        // AFX_MANAGE_STATE(AfxGetStaticModuleState()); // 제거!

        if (pattern < 0)
            return false;
        return true;
    }

    //25.10.30 - PG 전압 전송 (AFX_MANAGE_STATE 제거)
    __declspec(dllexport) bool PGVoltagesnd(int RV, int GV, int BV) 
    {
        // AFX_MANAGE_STATE(AfxGetStaticModuleState()); // 제거!

        if (RV == 0 || GV == 0 || BV == 0)
            return false;
        return true;
    }

    //25.10.30 - 측정 포트 제어 (AFX_MANAGE_STATE 제거)
    __declspec(dllexport) bool Meas_Turn(int port) 
    {
        // AFX_MANAGE_STATE(AfxGetStaticModuleState()); // 제거!

        if (port < 0)
            return false;
        return true;
    }

    //25.10.30 - 측정 데이터 획득 (AFX_MANAGE_STATE 제거)
    __declspec(dllexport) bool Getdata(struct output* out) 
    {
        // AFX_MANAGE_STATE(AfxGetStaticModuleState()); // 제거!

        if (out == nullptr) {
            return false;
        }

        // 기본 WAD 인덱스 (0) 사용
        int wad = 0;

        static std::random_device rd;
        static std::mt19937 gen(rd());
        static std::uniform_real_distribution<float> dis_xy(0.28f, 0.35f);
        static std::uniform_real_distribution<float> dis_L(1000.0f, 1500.0f);
        static std::uniform_real_distribution<float> dis_eff_cur(30.0f, 800.0f);

        // 랜덤값 생성
        out->measure[wad].x = dis_xy(gen);
        out->measure[wad].y = dis_xy(gen);
        out->measure[wad].L = dis_L(gen);
        out->measure[wad].eff = dis_eff_cur(gen);
        out->measure[wad].cur = dis_eff_cur(gen);
        out->measure[wad].u = (4 * out->measure[wad].x) / 
                              (-2 * out->measure[wad].x + 12 * out->measure[wad].y + 3);
        out->measure[wad].v = (9 * out->measure[wad].y) / 
                              (-2 * out->measure[wad].x + 12 * out->measure[wad].y + 3);
        return true;
    }

    //25.10.30 - LUT 데이터 계산 (AFX_MANAGE_STATE 제거)
    __declspec(dllexport) bool getLUTdata(int rgb, float RV, float GV, float BV, 
                                          int interval, int cnt, struct output* out) 
    {
        // AFX_MANAGE_STATE(AfxGetStaticModuleState()); // 제거!

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
        out->lut[rgb].max_index = (float)max_index(gen);
        out->lut[rgb].gamma = gamma(gen);
        out->lut[rgb].black = black(gen);

        return true;
    }

} // extern "C"

// C++ 내부 함수 - LUT 계산 로직
void cal_lut(std::vector<LUT_Data> pattern_inf[3], struct output* out)
{
    if (!out) return;

    float max_index_val = 0.0f;
    float max_lumi_val = 0.0f;
    float gamma_val = 1.0f;
    float black_val = 0.0f;

    for (int ch = 0; ch < 3; ++ch)
    {
        // 각 채널마다 기본값으로 초기화
        max_index_val = 0.0f;
        max_lumi_val = 0.0f;
        gamma_val = 1.0f;
        black_val = 0.0f;

        const auto& vec = pattern_inf[ch];
        if (vec.empty()) {
            out->lut[ch].max_index = max_index_val;
            out->lut[ch].max_lumi = max_lumi_val;
            out->lut[ch].gamma = gamma_val;
            out->lut[ch].black = black_val;
            continue;
        }

        // 1) 마지막 유효점 찾기
        int anchor = -1;
        for (int i = (int)vec.size() - 1; i >= 0; --i) {
            if (vec[i].index > 0 && vec[i].luminance > 0.0) {
                anchor = i;
                break;
            }
        }

        if (anchor < 0) {
            out->lut[ch].max_index = max_index_val;
            out->lut[ch].max_lumi = max_lumi_val;
            out->lut[ch].gamma = gamma_val;
            out->lut[ch].black = black_val;
            continue;
        }

        double Xmax = static_cast<double>(vec[anchor].index);
        double Ymax = vec[anchor].luminance;
        max_index_val = static_cast<float>(Xmax);
        max_lumi_val = static_cast<float>(Ymax);

        // 2) 로그-앵커 회귀로 gamma 추정
        if (Xmax > 0.0 && Ymax > 0.0) {
            const double xM = std::log(Xmax);
            const double yM = std::log(Ymax);
            double num = 0.0;
            double den = 0.0;
            int used = 0;

            for (const auto& p : vec) {
                if (p.index <= 0 || p.luminance <= 0.0) continue;
                const double xi = std::log(static_cast<double>(p.index));
                const double yi = std::log(p.luminance);
                const double dx = xi - xM;
                const double dy = yi - yM;
                num += dx * dy;
                den += dx * dx;
                ++used;
            }

            if (used >= 2 && std::fabs(den) > 1e-12) {
                gamma_val = static_cast<float>(num / den);
            }
            else {
                gamma_val = 1.0f;
            }
        }
        else {
            gamma_val = 1.0f;
        }

        // 3) 결과 기록
        out->lut[ch].max_index = max_index_val;
        out->lut[ch].max_lumi = max_lumi_val;
        out->lut[ch].gamma = gamma_val;
        out->lut[ch].black = black_val;
    }
}

#include "pch.h"
#include "test.h"

extern "C" {
    __declspec(dllexport) int test(struct input* in, struct output* out) {
        if (in == nullptr || out == nullptr) {
            return 0;
        }
        
        // 더 나은 랜덤 시드 생성 (밀리초 단위 + 주소값 활용)
        static bool first_call = true;
        if (first_call) {
            srand((unsigned int)(time(NULL) * 1000 + (uintptr_t)in));
            first_call = false;
        }
        
        int cnt = 0;
        int ok = 0;
        int ng = 0;
        int ptn = 0;
        
        for (int i = 0; i < 7; i++) {
            for (int j = 0; j < 17; j++) {
                out->data[i][j].x = cnt + 1;
                out->data[i][j].y = cnt + 2;
                out->data[i][j].L = cnt + 3;
                out->data[i][j].cur = cnt + 4;
                out->data[i][j].eff = cnt + 5;
                
                // 더 나은 랜덤 생성 (현재 카운터 값도 활용)
                int random = (rand() + cnt) % 10;
                
                if (random < 8) {
                    out->data[i][j].result = 0;  // 80% 확률
                    ok++;
                }
                else if (random == 8) {
                    out->data[i][j].result = 1;  // 10% 확률
                    ng++;
                }
                else {
                    out->data[i][j].result = 2;  // 10% 확률
                    ptn++;
                }
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
        if (pattern < 0)
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

// pattern_inf[ch] : ch=0(R),1(G),2(B)
// out->lut[ch]    : 결과 저장 (max_index, max_lumi, gamma, black=0)
void cal_lut(std::vector<LUT_Data> pattern_inf[3], struct output* out)
{
    if (!out) return;

    // 변수명 변경 (_로 시작하지 않게)
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

        // 1) 마지막 유효점( index>0 && luminance>0 )을 앵커로 사용
        int anchor = -1;
        for (int i = (int)vec.size() - 1; i >= 0; --i) {
            if (vec[i].index > 0 && vec[i].luminance > 0.0) {
                anchor = i;
                break;
            }
        }

        if (anchor < 0) {
            // 유효 데이터가 없음 → 기본값으로 반환
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

        // 2) 로그-앵커 회귀로 gamma 추정 (black=0)
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
#include "pch.h"
#include "test.h"

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
}
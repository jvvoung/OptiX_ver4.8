#pragma once
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
        int x, y, L, cur, eff;
    };

    struct output {
        struct pattern data[7][17]; // struct 키워드 명시
    };

    __declspec(dllexport) int test(struct input* in, struct output* out);
#ifdef __cplusplus
}
#endif
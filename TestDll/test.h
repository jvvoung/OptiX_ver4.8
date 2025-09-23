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
        //[7]:WAD => 0:0도, 1:30도, 2:45도, 3:60도, 4:15도, 5:A도, 6:B도
        //[17]:패턴 => 0:W, 1:R, 2:G, 3:B, 4:WG, 5:WG2, 6:WG3 ~ 16:WG13
    };

    __declspec(dllexport) int test(struct input* in, struct output* out);
#ifdef __cplusplus
}
#endif
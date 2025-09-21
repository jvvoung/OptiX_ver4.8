#include <windows.h>

extern "C" {
    __declspec(dllexport) int test(void* in, void* out) {
        return 1; // 항상 성공 반환
    }
}















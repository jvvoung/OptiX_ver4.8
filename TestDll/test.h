#pragma once
#include "pch.h"
#include <iostream>
#include <vector>
#include <algorithm>
#include <string>

using namespace std;

struct input {
	string name;
	int total_point;
	int cur_point;
};

struct pattern {
	int x, y, L, cur, eff;
};

struct output {
	pattern data[7][3];
};

extern "C" __declspec(dllexport) int test(input* in, output* out);
#include "pch.h"
#include "test.h"


int test(input* in, output* out) {
	int cnt=0;
	for(int i=0;i<in->total_point;i++){
		for(int j=0;j<3;j++){
			out->data[i][j].x=cnt+0.1;
			out->data[i][j].y=cnt+0.2;
			out->data[i][j].L=cnt+0.3;
			out->data[i][j].cur=cnt+0.4;
			out->data[i][j].eff=cnt+0.5;
			cnt++;
		}
	}
	return 1;
}
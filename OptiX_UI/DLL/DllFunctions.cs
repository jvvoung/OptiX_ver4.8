using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OptiX.DLL
{
    // TestDll.dll 함수 호출을 담당하는 클래스
    public static class DllFunctions
    {
        // MTP_test 함수 호출
        public static (Output output, bool success) CallMTPTestFunction(Input input)
        {
            if (!DllManager.IsInitialized)
            {
                Debug.WriteLine("[MTP_test] 오류: DLL이 초기화되지 않았습니다.");
                throw new InvalidOperationException("DLL이 초기화되지 않았습니다.");
            }

            try
            {
                // 구조체를 비관리 메모리에 마샬링
                IntPtr inputPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Input)));
                IntPtr outputPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Output)));

                try
                {
                    // 입력 구조체를 비관리 메모리에 복사
                    Marshal.StructureToPtr(input, inputPtr, false);

                    // DLL 함수 호출
                    var testFunc = DllManager.GetFunction<DllManager.TestFunction>("MTP_test");
                    int result = testFunc(inputPtr, outputPtr);

                    if (result == 1) // 성공
                    {
                        // 출력 구조체를 관리 메모리로 복사
                        Output output = Marshal.PtrToStructure<Output>(outputPtr);
                        return (output, true);
                    }
                    else
                    {
                        Debug.WriteLine($"[MTP_test] DLL 함수 실패 (결과 코드: {result})");
                        // 실패 시 빈 출력 반환
                        Output emptyOutput = new Output 
                        { 
                            data = new Pattern[119],
                            IPVS_data = new Pattern[70],
                            measure = new Pattern[7]
                        };
                        return (emptyOutput, false);
                    }
                }
                finally
                {
                    // 메모리 해제
                    Marshal.FreeHGlobal(inputPtr);
                    Marshal.FreeHGlobal(outputPtr);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MTP_test] 예외 발생: {ex.Message}");
                throw;
            }
        }

        // IPVS_test 함수 호출
        public static (Output output, bool success) CallIPVSTestFunction(Input input)
        {
            if (!DllManager.IsInitialized)
            {
                Debug.WriteLine("[IPVS_test] 오류: DLL이 초기화되지 않았습니다.");
                throw new InvalidOperationException("DLL이 초기화되지 않았습니다.");
            }

            try
            {
                // 구조체를 비관리 메모리에 마샬링
                IntPtr inputPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Input)));
                IntPtr outputPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Output)));

                try
                {
                    // 입력 구조체를 비관리 메모리에 복사
                    Marshal.StructureToPtr(input, inputPtr, false);

                    // DLL 함수 호출
                    var testFunc = DllManager.GetFunction<DllManager.TestFunction>("IPVS_test");
                    int result = testFunc(inputPtr, outputPtr);

                    if (result == 1) // 성공
                    {
                        // 출력 구조체를 관리 메모리로 복사
                        Output output = Marshal.PtrToStructure<Output>(outputPtr);
                        return (output, true);
                    }
                    else
                    {
                        Debug.WriteLine($"[IPVS_test] DLL 함수 실패 (결과 코드: {result})");
                        // 실패 시 빈 출력 반환
                        Output emptyOutput = new Output 
                        { 
                            data = new Pattern[119],
                            IPVS_data = new Pattern[70],
                            measure = new Pattern[7]
                        };
                        return (emptyOutput, false);
                    }
                }
                finally
                {
                    // 메모리 해제
                    Marshal.FreeHGlobal(inputPtr);
                    Marshal.FreeHGlobal(outputPtr);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[IPVS_test] 예외 발생: {ex.Message}");
                throw;
            }
        }

        // PG 포트 연결/해제
        public static bool CallPGTurn(int port)
        {
            if (!DllManager.IsInitialized)
            {
                throw new InvalidOperationException("DLL이 초기화되지 않았습니다.");
            }

            try
            {
                var pgTurnFunc = DllManager.GetFunction<DllManager.PGTurnFunction>("PGTurn");
                return pgTurnFunc(port);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PGTurn 함수 호출 오류: {ex.Message}");
                return false;
            }
        }

        // PG 패턴 전송
        public static bool CallPGPattern(int pattern)
        {
            if (!DllManager.IsInitialized)
            {
                throw new InvalidOperationException("DLL이 초기화되지 않았습니다.");
            }

            try
            {
                var pgPatternFunc = DllManager.GetFunction<DllManager.PGPatternFunction>("PGPattern");
                return pgPatternFunc(pattern);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PGPattern 함수 호출 오류: {ex.Message}");
                return false;
            }
        }

        // RGB 전압 전송
        public static bool CallPGVoltagesnd(int RV, int GV, int BV)
        {
            if (!DllManager.IsInitialized)
            {
                throw new InvalidOperationException("DLL이 초기화되지 않았습니다.");
            }

            try
            {
                var pgVoltagesndFunc = DllManager.GetFunction<DllManager.PGVoltagesndFunction>("PGVoltagesnd");
                return pgVoltagesndFunc(RV, GV, BV);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PGVoltagesnd 함수 호출 오류: {ex.Message}");
                return false;
            }
        }

        // 측정 포트 연결/해제
        public static bool CallMeasTurn(int port)
        {
            if (!DllManager.IsInitialized)
            {
                throw new InvalidOperationException("DLL이 초기화되지 않았습니다.");
            }

            try
            {
                var measTurnFunc = DllManager.GetFunction<DllManager.MeasTurnFunction>("Meas_Turn");
                return measTurnFunc(port);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Meas_Turn 함수 호출 오류: {ex.Message}");
                return false;
            }
        }

        // 측정 데이터 가져오기
        public static (Pattern measureData, bool success) CallGetdata()
        {
            if (!DllManager.IsInitialized)
            {
                throw new InvalidOperationException("DLL이 초기화되지 않았습니다.");
            }

            try
            {
                // C++ Output 구조체를 받기 위한 비관리 메모리 할당
                IntPtr outputPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Output)));

                try
                {
                    // 네이티브 Getdata 함수 호출 (C++: bool Getdata(struct output* out))
                    var getdataFunc = DllManager.GetFunction<DllManager.GetdataFunction>("Getdata");
                    bool result = getdataFunc(outputPtr);

                    if (result)
                    {
                        // C++에서 채운 Output 구조체를 .NET으로 복사
                        Output output = Marshal.PtrToStructure<Output>(outputPtr);
                        
                        // 첫 번째 WAD(각도 0도)의 측정 데이터만 반환 (UI 단순화)
                        // 실제로는 output.measure[0~6]에 7개 WAD 데이터가 모두 있음
                        Pattern measureData = output.measure.Length > 0 ? output.measure[0] : new Pattern();
                        
                        // 간단한 유효성 검증만 (TACT 시간 단축)
                        if (measureData.x >= 0 && measureData.x <= 1 && 
                            measureData.y >= 0 && measureData.y <= 1)
                        {
                            return (measureData, true);
                        }
                        else
                        {
                            // 유효하지 않은 데이터는 0으로 초기화된 빈 데이터 반환
                            Pattern emptyData = new Pattern();
                            return (emptyData, false);
                        }
                    }
                    else
                    {
                        // 측정 실패 시 빈 데이터 반환
                        Pattern emptyData = new Pattern();
                        return (emptyData, false);
                    }
                }
                finally
                {
                    // 메모리 누수 방지: 할당한 비관리 메모리 반드시 해제
                    Marshal.FreeHGlobal(outputPtr);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Getdata 함수 호출 오류: {ex.Message}");
                Pattern emptyData = new Pattern();
                return (emptyData, false);
            }
        }

        // LUT 데이터 계산
        public static (LUTParameter lutParam, bool success) CallGetLUTdata(int rgb, float RV, float GV, float BV, int interval, int cnt)
        {
            if (!DllManager.IsInitialized)
            {
                throw new InvalidOperationException("DLL이 초기화되지 않았습니다.");
            }

            try
            {
                // C++ Output 구조체를 받기 위한 비관리 메모리 할당
                IntPtr outputPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Output)));

                try
                {
                    // 네이티브 getLUTdata 함수 호출 (C++: bool getLUTdata(int rgb, float RV, float GV, float BV, int interval, int cnt, struct output* out))
                    var getLUTdataFunc = DllManager.GetFunction<DllManager.GetLUTdataFunction>("getLUTdata");
                    bool result = getLUTdataFunc(rgb, RV, GV, BV, interval, cnt, outputPtr);

                    if (result)
                    {
                        // C++에서 채운 Output 구조체를 .NET으로 복사
                        Output output = Marshal.PtrToStructure<Output>(outputPtr);
                        
                        // 지정된 RGB 채널의 LUT 파라미터 반환
                        // output.lut[0~2]에 Red, Green, Blue 각각의 LUT 파라미터가 저장됨
                        LUTParameter lutParam = output.lut[rgb];
                        return (lutParam, true);
                    }
                    else
                    {
                        // LUT 계산 실패 시 빈 파라미터 반환
                        LUTParameter emptyParam = new LUTParameter();
                        return (emptyParam, false);
                    }
                }
                finally
                {
                    // 메모리 누수 방지: 할당한 비관리 메모리 반드시 해제
                    Marshal.FreeHGlobal(outputPtr);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"getLUTdata 함수 호출 오류: {ex.Message}");
                LUTParameter emptyParam = new LUTParameter();
                return (emptyParam, false);
            }
        }
    }
}


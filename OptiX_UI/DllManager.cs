using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Diagnostics;

namespace OptiX
{
    /// <summary>
    /// DLL 관리 클래스 - 모든 DLL 함수 호출을 중앙 집중식으로 관리
    /// </summary>
    public static class DllManager
    {
        #region Private Fields
        private static IntPtr _dllHandle = IntPtr.Zero;
        private static string _dllPath = "";
        private static bool _isInitialized = false;
        #endregion

        #region Public Properties
        /// <summary>
        /// DLL이 초기화되었는지 확인
        /// </summary>
        public static bool IsInitialized => _isInitialized;

        /// <summary>
        /// 현재 로드된 DLL 경로
        /// </summary>
        public static string DllPath => _dllPath;
        #endregion

        #region P/Invoke Declarations
        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

        [DllImport("kernel32.dll")]
        private static extern bool FreeLibrary(IntPtr hModule);
        #endregion

        #region DLL Management Methods
        /// <summary>
        /// DLL 초기화 (프로그램 시작 시 한 번 호출)
        /// </summary>
        public static bool Initialize()
        {
            try
            {
                // 기존 DLL이 로드되어 있다면 해제
                if (_dllHandle != IntPtr.Zero)
                {
                    FreeLibrary(_dllHandle);
                    _dllHandle = IntPtr.Zero;
                }

                // DLL 경로 가져오기
                string dllFolder = GlobalDataManager.GetValue("Settings", "DLL_FOLDER", "");
                if (string.IsNullOrEmpty(dllFolder))
                {
                    Debug.WriteLine("DllManager 초기화 실패: DLL 폴더 경로가 설정되지 않음");
                    return false;
                }

                _dllPath = Path.Combine(dllFolder, "TestDll.dll");
                if (!File.Exists(_dllPath))
                {
                    Debug.WriteLine($"DllManager 초기화 실패: DLL 파일을 찾을 수 없음 - {_dllPath}");
                    return false;
                }

                // DLL 로드
                _dllHandle = LoadLibrary(_dllPath);
                if (_dllHandle == IntPtr.Zero)
                {
                    int error = Marshal.GetLastWin32Error();
                    Debug.WriteLine($"DllManager 초기화 실패: DLL 로딩 실패 - 오류 코드: {error}");
                    return false;
                }

                _isInitialized = true;
                Debug.WriteLine($"DllManager 초기화 성공: {_dllPath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DllManager 초기화 오류: {ex.Message}");
                _isInitialized = false;
                return false;
            }
        }

        /// <summary>
        /// DLL 재로드 (메인 설정창에서 SAVE 버튼 클릭 시 호출)
        /// </summary>
        public static bool Reload()
        {
            try
            {
                Debug.WriteLine("DllManager 재로드 시작...");
                return Initialize();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DllManager 재로드 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// DLL 해제
        /// </summary>
        public static void Dispose()
        {
            try
            {
                if (_dllHandle != IntPtr.Zero)
                {
                    FreeLibrary(_dllHandle);
                    _dllHandle = IntPtr.Zero;
                    _isInitialized = false;
                    Debug.WriteLine("DllManager 해제 완료");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DllManager 해제 오류: {ex.Message}");
            }
        }
        #endregion

        #region DLL Function Call Methods
        /// <summary>
        /// DLL에서 함수 주소 가져오기
        /// </summary>
        /// <param name="functionName">함수 이름</param>
        /// <returns>함수 주소</returns>
        public static IntPtr GetFunctionAddress(string functionName)
        {
            if (!_isInitialized || _dllHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException("DLL이 초기화되지 않았습니다. Initialize()를 먼저 호출하세요.");
            }

            IntPtr functionAddress = GetProcAddress(_dllHandle, functionName);
            if (functionAddress == IntPtr.Zero)
            {
                throw new InvalidOperationException($"함수를 찾을 수 없습니다: {functionName}");
            }

            return functionAddress;
        }

        /// <summary>
        /// DLL 함수 호출 (델리게이트를 통한 안전한 호출)
        /// </summary>
        /// <typeparam name="T">델리게이트 타입</typeparam>
        /// <param name="functionName">함수 이름</param>
        /// <returns>함수 델리게이트</returns>
        public static T GetFunction<T>(string functionName) where T : class
        {
            IntPtr functionAddress = GetFunctionAddress(functionName);
            return Marshal.GetDelegateForFunctionPointer<T>(functionAddress);
        }
        #endregion

        #region Specific DLL Functions
        /// <summary>
        /// TestDll의 test 함수 델리게이트 정의
        /// 실제 DLL: int test(struct input* in, struct output* out)
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int TestFunction(IntPtr input, IntPtr output);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool PGTurnFunction(int port);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool PGPatternFunction(int pattern);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool PGVoltagesndFunction(int RV, int GV, int BV);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool MeasTurnFunction(int port);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool GetdataFunction(IntPtr output);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool GetLUTdataFunction(int rgb, float RV, float GV, float BV, int interval, int cnt, IntPtr output);

        /// <summary>
        /// TestDll의 test 함수 호출
        /// </summary>
        /// <param name="input">입력 데이터</param>
        /// <returns>테스트 결과와 성공 여부</returns>
        public static (Output output, bool success) CallTestFunction(Input input)
        {
            if (!_isInitialized)
            {
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
                    var testFunc = GetFunction<TestFunction>("test");
                    int result = testFunc(inputPtr, outputPtr);

                    if (result == 1) // 성공
                    {
                        // 출력 구조체를 관리 메모리로 복사
                        Output output = Marshal.PtrToStructure<Output>(outputPtr);
                        return (output, true);
                    }
                    else
                    {
                        // 실패 시 빈 출력 반환
                        Output emptyOutput = new Output 
                        { 
                            data = new Pattern[119],
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
                Debug.WriteLine($"TestDll.test 함수 호출 오류: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Manual 페이지용 DLL 함수 호출 메서드들
        /// </summary>

        /// <summary>
        /// PG 포트 연결/해제
        /// </summary>
        /// <param name="port">포트 번호</param>
        /// <returns>성공 여부</returns>
        public static bool CallPGTurn(int port)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("DLL이 초기화되지 않았습니다.");
            }

            try
            {
                var pgTurnFunc = GetFunction<PGTurnFunction>("PGTurn");
                return pgTurnFunc(port);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PGTurn 함수 호출 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 패턴 전송
        /// </summary>
        /// <param name="pattern">패턴 번호</param>
        /// <returns>성공 여부</returns>
        public static bool CallPGPattern(int pattern)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("DLL이 초기화되지 않았습니다.");
            }

            try
            {
                var pgPatternFunc = GetFunction<PGPatternFunction>("PGPattern");
                return pgPatternFunc(pattern);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PGPattern 함수 호출 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// RGB 전압 전송
        /// </summary>
        /// <param name="RV">Red Voltage</param>
        /// <param name="GV">Green Voltage</param>
        /// <param name="BV">Blue Voltage</param>
        /// <returns>성공 여부</returns>
        public static bool CallPGVoltagesnd(int RV, int GV, int BV)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("DLL이 초기화되지 않았습니다.");
            }

            try
            {
                var pgVoltagesndFunc = GetFunction<PGVoltagesndFunction>("PGVoltagesnd");
                return pgVoltagesndFunc(RV, GV, BV);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PGVoltagesnd 함수 호출 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 측정 포트 연결/해제
        /// </summary>
        /// <param name="port">포트 번호</param>
        /// <returns>성공 여부</returns>
        public static bool CallMeasTurn(int port)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("DLL이 초기화되지 않았습니다.");
            }

            try
            {
                var measTurnFunc = GetFunction<MeasTurnFunction>("Meas_Turn");
                return measTurnFunc(port);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Meas_Turn 함수 호출 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 측정 데이터 가져오기
        /// </summary>
        /// <returns>측정 데이터와 성공 여부</returns>
        public static (Pattern measureData, bool success) CallGetdata()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("DLL이 초기화되지 않았습니다.");
            }

            try
            {
                // 출력 구조체를 비관리 메모리에 마샬링
                IntPtr outputPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Output)));

                try
                {
                    // DLL 함수 호출
                    var getdataFunc = GetFunction<GetdataFunction>("Getdata");
                    bool result = getdataFunc(outputPtr);

                    if (result)
                    {
                        // 출력 구조체를 관리 메모리로 복사
                        Output output = Marshal.PtrToStructure<Output>(outputPtr);
                        
                        // 첫 번째 WAD의 측정 데이터 반환 (기본값)
                        Pattern measureData = output.measure.Length > 0 ? output.measure[0] : new Pattern();
                        return (measureData, true);
                    }
                    else
                    {
                        // 실패 시 빈 데이터 반환
                        Pattern emptyData = new Pattern();
                        return (emptyData, false);
                    }
                }
                finally
                {
                    // 메모리 해제
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

        /// <summary>
        /// LUT 데이터 가져오기
        /// </summary>
        /// <param name="rgb">RGB 인덱스 (0=Red, 1=Green, 2=Blue)</param>
        /// <param name="RV">Red Voltage</param>
        /// <param name="GV">Green Voltage</param>
        /// <param name="BV">Blue Voltage</param>
        /// <param name="interval">간격</param>
        /// <param name="cnt">카운트</param>
        /// <returns>LUT 파라미터와 성공 여부</returns>
        public static (LUTParameter lutParam, bool success) CallGetLUTdata(int rgb, float RV, float GV, float BV, int interval, int cnt)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("DLL이 초기화되지 않았습니다.");
            }

            try
            {
                // Output 구조체를 비관리 메모리에 마샬링
                IntPtr outputPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Output)));

                try
                {
                    // DLL 함수 호출
                    var getLUTdataFunc = GetFunction<GetLUTdataFunction>("getLUTdata");
                    bool result = getLUTdataFunc(rgb, RV, GV, BV, interval, cnt, outputPtr);

                    if (result)
                    {
                        // 출력 구조체를 관리 메모리로 복사
                        Output output = Marshal.PtrToStructure<Output>(outputPtr);
                        
                        // 해당 RGB 인덱스의 LUT 파라미터 반환
                        LUTParameter lutParam = output.lut[rgb];
                        return (lutParam, true);
                    }
                    else
                    {
                        // 실패 시 빈 파라미터 반환
                        LUTParameter emptyParam = new LUTParameter();
                        return (emptyParam, false);
                    }
                }
                finally
                {
                    // 메모리 해제
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

        #endregion

        #region Utility Methods
        /// <summary>
        /// DLL 상태 정보 가져오기
        /// </summary>
        /// <returns>DLL 상태 정보</returns>
        public static string GetStatus()
        {
            if (!_isInitialized)
                return "DLL이 초기화되지 않음";

            return $"DLL 로드됨: {_dllPath}";
        }

        /// <summary>
        /// DLL 경로 유효성 검사
        /// </summary>
        /// <returns>유효한 경로인지 여부</returns>
        public static bool ValidateDllPath()
        {
            string dllFolder = GlobalDataManager.GetValue("Settings", "DLL_FOLDER", "");
            if (string.IsNullOrEmpty(dllFolder))
                return false;

            string fullPath = Path.Combine(dllFolder, "TestDll.dll");
            return File.Exists(fullPath);
        }
        #endregion
    }

    #region DLL Structure Definitions
    /// <summary>
    /// DLL 입력 구조체 (struct input)
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Input
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string CELL_ID;
        
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string INNER_ID;
        
        public int total_point;
        public int cur_point;
    }

    /// <summary>
    /// DLL에서 반환되는 데이터 구조체 (업데이트됨)
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Pattern
    {
        public float x;
        public float y;
        public float u;
        public float v;
        public float L;
        public float cur;
        public float eff;
    }

    /// <summary>
    /// DLL에서 반환되는 출력 구조체 (업데이트됨)
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Output
    {
        // 7x17 2차원 배열을 1차원 배열로 표현 (7*17 = 119)
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 119)]
        public Pattern[] data; // [7][17] => 0:0도, 1:30도, 2:45도, 3:60도, 4:15도, 5:A도, 6:B도
        
        // 측정 데이터 배열 (7개 WAD)
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
        public Pattern[] measure; // [7] WAD별 측정 데이터
        
        // LUT 파라미터 배열 (3개 RGB)
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public LUTParameter[] lut; // [3] RGB별 LUT 파라미터
    }

    /// <summary>
    /// LUT 파라미터 구조체
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct LUTParameter
    {
        public float max_lumi;
        public float max_index;
        public float gamma;
        public float black;
    }


    #endregion
}

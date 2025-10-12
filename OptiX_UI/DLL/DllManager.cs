using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using OptiX.Common;

namespace OptiX.DLL
{
    // TestDll.dll 로드/해제 및 함수 포인터 관리 클래스
    public static class DllManager
    {
        // 로드된 DLL 핸들
        private static IntPtr _dllHandle = IntPtr.Zero;
        
        // 로드된 DLL 전체 경로
        private static string _dllPath = "";
        
        // DLL 초기화 완료 여부
        private static bool _isInitialized = false;
        
        #region Public Properties
        // DLL 초기화 상태
        public static bool IsInitialized => _isInitialized;

        // 로드된 DLL 경로
        public static string DllPath => _dllPath;
        #endregion

        #region P/Invoke Declarations
        // Windows API: DLL 파일을 메모리에 로드
        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string dllToLoad);

        // Windows API: DLL에서 함수 주소 가져오기
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

        // Windows API: DLL 메모리 해제
        [DllImport("kernel32.dll")]
        private static extern bool FreeLibrary(IntPtr hModule);
        #endregion

        #region DLL Management Methods
        // DLL 초기화 (프로그램 시작 시 호출)
        public static bool Initialize()
        {
            try
            {
                // 기존 DLL이 로드되어 있다면 먼저 해제 (중복 로드 방지)
                if (_dllHandle != IntPtr.Zero)
                {
                    FreeLibrary(_dllHandle);
                    _dllHandle = IntPtr.Zero;
                }

                // DLL 경로 구성: Settings.DLL_FOLDER + "TestDll.dll"
                string dllFolder = GlobalDataManager.GetValue("Settings", "DLL_FOLDER", "");
                Console.WriteLine($"[DllManager] DLL 폴더 경로 = '{dllFolder}'");
                
                if (string.IsNullOrEmpty(dllFolder))
                {
                    Console.WriteLine("[DllManager] 초기화 실패: DLL 폴더 경로가 설정되지 않음");
                    return false;
                }

                _dllPath = Path.Combine(dllFolder, "TestDll.dll");
                Console.WriteLine($"[DllManager] DLL 전체 경로 = '{_dllPath}'");
                
                if (!File.Exists(_dllPath))
                {
                    Console.WriteLine($"[DllManager] 초기화 실패: DLL 파일을 찾을 수 없음 - {_dllPath}");
                    return false;
                }

                Console.WriteLine("[DllManager] DLL 파일 존재 확인됨, 로드 시도 중...");
                
                // Windows API를 통한 DLL 로드
                _dllHandle = LoadLibrary(_dllPath);
                if (_dllHandle == IntPtr.Zero)
                {
                    // 로드 실패 시 Windows 오류 코드 확인
                    int error = Marshal.GetLastWin32Error();
                    Console.WriteLine($"[DllManager] 초기화 실패: DLL 로딩 실패 - 경로: {_dllPath}, 오류 코드: {error}");
                    return false;
                }

                _isInitialized = true;
                Console.WriteLine($"[DllManager] 초기화 성공: {_dllPath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DllManager] 초기화 오류: {ex.Message}");
                _isInitialized = false;
                return false;
            }
        }

        // DLL 재로드 (설정 변경 시 호출)
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

        // DLL 메모리 해제 (프로그램 종료 시 호출)
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
        // DLL에서 함수 주소 가져오기
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

        // DLL 함수를 .NET 델리게이트로 변환
        public static T GetFunction<T>(string functionName) where T : class
        {
            IntPtr functionAddress = GetFunctionAddress(functionName);
            return Marshal.GetDelegateForFunctionPointer<T>(functionAddress);
        }
        #endregion

        #region Delegate Definitions
        // MTP_test 함수 델리게이트 (int MTP_test(struct input*, struct output*))
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int TestFunction(IntPtr input, IntPtr output);

        // PGTurn 함수 델리게이트
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool PGTurnFunction(int port);

        // PGPattern 함수 델리게이트
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool PGPatternFunction(int pattern);

        // PGVoltagesnd 함수 델리게이트
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool PGVoltagesndFunction(int RV, int GV, int BV);

        // Meas_Turn 함수 델리게이트
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool MeasTurnFunction(int port);

        // Getdata 함수 델리게이트
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool GetdataFunction(IntPtr output);

        // getLUTdata 함수 델리게이트
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool GetLUTdataFunction(int rgb, float RV, float GV, float BV, int interval, int cnt, IntPtr output);
        #endregion

        #region Utility Methods
        // DLL 상태 정보 가져오기
        public static string GetStatus()
        {
            if (!_isInitialized)
                return "DLL이 초기화되지 않음";

            return $"DLL 로드됨: {_dllPath}";
        }

        // DLL 경로 유효성 검사
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
}



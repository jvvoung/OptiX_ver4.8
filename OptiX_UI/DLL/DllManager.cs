using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using OptiX.Common;

namespace OptiX.DLL
{
    /// <summary>
    /// TestDll.dll P/Invoke 관리 클래스 (MFC DLL 안정적 로딩 지원)
    /// DllImport 방식으로 C++ MFC DLL 함수를 직접 호출합니다.
    /// </summary>
    public static class DllManager
    {
        private const string DLL_NAME = "Process.dll";
        
        // DLL 로드된 경로 (정보용)
        private static string _dllPath = "";
        
        // DLL 초기화 완료 여부
        private static bool _isInitialized = false;
        
        // SetDllDirectory가 이미 설정되었는지 추적 (에러 183 방지)
        private static bool _isDllDirectorySet = false;
        
        // 설정된 DLL 디렉토리 경로 캐싱
        private static string _currentDllDirectory = "";
        
        // 초기화 재시도 락 (중복 호출 방지)
        private static readonly object _initLock = new object();
        
        #region Public Properties
        /// <summary>
        /// DLL 초기화 상태
        /// </summary>
        public static bool IsInitialized => _isInitialized;

        /// <summary>
        /// 로드된 DLL 경로
        /// </summary>
        public static string DllPath => _dllPath;
        #endregion

        #region Windows API for DLL Path Setup and Dependency Check
        /// <summary>
        /// DLL 검색 경로 설정 (kernel32.dll SetDllDirectory)
        /// </summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);
        
        /// <summary>
        /// DLL 핸들 가져오기 (이미 로드된 DLL 확인용)
        /// </summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
        
        /// <summary>
        /// 명시적 DLL 로드 (의존성 사전 검증용)
        /// </summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);
        
        /// <summary>
        /// 명시적 DLL 언로드
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeLibrary(IntPtr hModule);
        
        // LoadLibraryEx 플래그
        private const uint LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR = 0x00000100;
        private const uint LOAD_LIBRARY_SEARCH_DEFAULT_DIRS = 0x00001000;
        #endregion

        #region DLL Management Methods
        /// <summary>
        /// DLL 초기화 (프로그램 시작 시 호출)
        /// MFC DLL을 안정적으로 로딩하고 의존성을 검증합니다.
        /// </summary>
        public static bool Initialize()
        {
            // 중복 초기화 방지 (락 사용)
            lock (_initLock)
            {
                // 이미 초기화된 경우 재초기화 스킵
                if (_isInitialized)
                {
                    Debug.WriteLine("[DllManager] 이미 초기화됨 - 스킵");
                    return true;
                }

                try
                {
                    Debug.WriteLine("[DllManager] ========== DLL 초기화 시작 ==========");
                    
                    // 1. DLL 경로 구성
                    string dllFolder = GlobalDataManager.GetValue("Settings", "DLL_FOLDER", "");
                    Debug.WriteLine($"[DllManager] DLL 폴더 경로 = '{dllFolder}'");
                    ErrorLogger.Log($"DLL 폴더 경로: {dllFolder}", ErrorLogger.LogLevel.DEBUG);
                    
                    if (string.IsNullOrEmpty(dllFolder))
                    {
                        string errorMsg = "DLL 폴더 경로가 설정되지 않음";
                        Debug.WriteLine($"[DllManager] 초기화 실패: {errorMsg}");
                        ErrorLogger.Log(errorMsg, ErrorLogger.LogLevel.ERROR);
                        return false;
                    }

                    _dllPath = Path.Combine(dllFolder, DLL_NAME);
                    Debug.WriteLine($"[DllManager] DLL 전체 경로 = '{_dllPath}'");
                    
                    // 2. DLL 파일 존재 확인
                    if (!File.Exists(_dllPath))
                    {
                        string errorMsg = $"DLL 파일을 찾을 수 없음 - {_dllPath}";
                        Debug.WriteLine($"[DllManager] 초기화 실패: {errorMsg}");
                        ErrorLogger.LogFileError(_dllPath, "DLL 검증", "파일을 찾을 수 없음");
                        return false;
                    }
                    Debug.WriteLine("[DllManager] ✓ DLL 파일 존재 확인");

                    // 3. MFC DLL 의존성 사전 검증
                    if (!ValidateMfcDependencies(dllFolder))
                    {
                        string errorMsg = "MFC DLL 의존성 검증 실패 (mfc140.dll, vcruntime140.dll 등 확인 필요)";
                        Debug.WriteLine($"[DllManager] 경고: {errorMsg}");
                        ErrorLogger.Log(errorMsg, ErrorLogger.LogLevel.WARNING);
                        // 경고만 남기고 계속 진행
                    }

                    // 4. SetDllDirectory 설정 (중복 호출 방지 - 에러 183 방지)
                    if (!_isDllDirectorySet || _currentDllDirectory != dllFolder)
                    {
                        Debug.WriteLine($"[DllManager] SetDllDirectory 설정 중: {dllFolder}");
                        
                        bool setDirResult = SetDllDirectory(dllFolder);
                        if (!setDirResult)
                        {
                            int error = Marshal.GetLastWin32Error();
                            
                            // 에러 183 (ERROR_ALREADY_EXISTS) 처리
                            if (error == 183)
                            {
                                Debug.WriteLine($"[DllManager] 경고: SetDllDirectory - 이미 설정됨 (에러 183)");
                                ErrorLogger.Log("SetDllDirectory 이미 설정됨 - 무시하고 계속 진행", ErrorLogger.LogLevel.WARNING);
                                // 에러 183은 무시하고 계속 (이미 설정된 상태)
                            }
                            else
                            {
                                string errorMsg = $"SetDllDirectory 실패 - 경로: {dllFolder}, 오류 코드: {error}";
                                Debug.WriteLine($"[DllManager] 경고: {errorMsg}");
                                ErrorLogger.Log(errorMsg, ErrorLogger.LogLevel.WARNING);
                            }
                        }
                        else
                        {
                            Debug.WriteLine("[DllManager] ✓ SetDllDirectory 성공");
                        }
                        
                        _isDllDirectorySet = true;
                        _currentDllDirectory = dllFolder;
                    }
                    else
                    {
                        Debug.WriteLine($"[DllManager] SetDllDirectory 이미 설정됨 - 스킵 (에러 183 방지)");
                    }

                    // 5. DLL 사전 로드 (MFC DLL DllMain 초기화 강제 실행)
                    if (!PreloadDll(_dllPath))
                    {
                        string errorMsg = "DLL 사전 로드 실패 - MFC 초기화 실패 가능성";
                        Debug.WriteLine($"[DllManager] 초기화 실패: {errorMsg}");
                        ErrorLogger.LogDllError("PreloadDll", errorMsg);
                        return false;
                    }

                    // 6. DLL 함수 호출 검증 (실제 P/Invoke 동작 확인)
                    if (!ValidateDllFunctions())
                    {
                        string errorMsg = "DLL 함수 호출 검증 실패";
                        Debug.WriteLine($"[DllManager] 초기화 실패: {errorMsg}");
                        ErrorLogger.LogDllError("DllImport", errorMsg);
                        return false;
                    }

                    _isInitialized = true;
                    Debug.WriteLine($"[DllManager] ========== 초기화 성공: {_dllPath} ==========");
                    ErrorLogger.Log($"DLL 초기화 성공: {_dllPath}", ErrorLogger.LogLevel.INFO);
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DllManager] 초기화 오류: {ex.Message}\n{ex.StackTrace}");
                    ErrorLogger.LogException(ex, "DLL 초기화 중 예외 발생");
                    _isInitialized = false;
                    return false;
                }
            }
        }
        
        /// <summary>
        /// MFC DLL 의존성 파일 검증
        /// </summary>
        private static bool ValidateMfcDependencies(string dllFolder)
        {
            try
            {
                Debug.WriteLine("[DllManager] MFC 의존성 검증 시작...");
                
                // MFC DLL에서 필요한 주요 의존성 파일들
                string[] dependencies = new[]
                {
                    "mfc140.dll",      // MFC 라이브러리
                    "mfc140u.dll",     // MFC 유니코드 라이브러리
                    "msvcp140.dll",    // C++ 표준 라이브러리
                    "vcruntime140.dll" // Visual C++ 런타임
                };

                bool allFound = true;
                foreach (string dep in dependencies)
                {
                    string depPath = Path.Combine(dllFolder, dep);
                    bool exists = File.Exists(depPath);
                    
                    // 시스템 PATH에서도 확인 (GetModuleHandle 사용)
                    IntPtr handle = GetModuleHandle(dep);
                    bool loadedInSystem = handle != IntPtr.Zero;
                    
                    if (exists || loadedInSystem)
                    {
                        Debug.WriteLine($"[DllManager]   ✓ {dep} 발견 (로컬: {exists}, 시스템: {loadedInSystem})");
                    }
                    else
                    {
                        Debug.WriteLine($"[DllManager]   ✗ {dep} 누락 (경고)");
                        allFound = false;
                    }
                }
                
                return allFound;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DllManager] 의존성 검증 중 오류: {ex.Message}");
                return false; // 검증 실패해도 계속 진행
            }
        }
        
        /// <summary>
        /// DLL 사전 로드 (MFC DllMain 초기화 보장)
        /// </summary>
        private static bool PreloadDll(string dllPath)
        {
            try
            {
                Debug.WriteLine($"[DllManager] DLL 사전 로드 시작: {dllPath}");
                
                // LoadLibraryEx로 명시적 로드 (LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR 플래그)
                IntPtr hModule = LoadLibraryEx(dllPath, IntPtr.Zero, 
                                                LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR | 
                                                LOAD_LIBRARY_SEARCH_DEFAULT_DIRS);
                
                if (hModule == IntPtr.Zero)
                {
                    int error = Marshal.GetLastWin32Error();
                    Debug.WriteLine($"[DllManager] LoadLibraryEx 실패 - 오류 코드: {error}");
                    
                    // 에러 코드 분석
                    switch (error)
                    {
                        case 126: // ERROR_MOD_NOT_FOUND
                            ErrorLogger.Log("DLL 또는 의존 DLL을 찾을 수 없음 (에러 126)", ErrorLogger.LogLevel.ERROR);
                            break;
                        case 127: // ERROR_PROC_NOT_FOUND
                            ErrorLogger.Log("DLL 진입점을 찾을 수 없음 (에러 127)", ErrorLogger.LogLevel.ERROR);
                            break;
                        case 183: // ERROR_ALREADY_EXISTS
                            Debug.WriteLine("[DllManager] DLL 이미 로드됨 (에러 183) - 정상");
                            return true; // 이미 로드된 경우는 성공으로 간주
                        default:
                            ErrorLogger.Log($"DLL 로드 실패 (에러 {error})", ErrorLogger.LogLevel.ERROR);
                            break;
                    }
                    
                    return false;
                }
                
                Debug.WriteLine($"[DllManager] ✓ DLL 사전 로드 성공 (핸들: 0x{hModule.ToInt64():X})");
                
                // 명시적으로 로드한 DLL은 해제하지 않음 (CLR이 관리하도록 유지)
                // FreeLibrary(hModule); // 주석 처리 - CLR의 DllImport와 충돌 방지
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DllManager] DLL 사전 로드 중 예외: {ex.Message}");
                ErrorLogger.LogException(ex, "DLL 사전 로드 중 예외");
                return false;
            }
        }
        
        /// <summary>
        /// DLL 함수 호출 검증
        /// </summary>
        private static bool ValidateDllFunctions()
        {
            try
            {
                Debug.WriteLine("[DllManager] DLL 함수 호출 검증 시작...");
                
                // PGTurn(0) 호출로 DLL 로드 가능 여부 테스트
                // 실제 장비가 연결되지 않았을 수 있으므로 결과는 무시
                bool result = PGTurn(0);
                Debug.WriteLine($"[DllManager] ✓ PGTurn(0) 호출 성공 (결과: {result})");
                
                return true;
            }
            catch (DllNotFoundException dllEx)
            {
                string errorMsg = $"DLL 로드 실패 - {DLL_NAME}을 찾을 수 없음: {dllEx.Message}";
                Debug.WriteLine($"[DllManager] 검증 실패: {errorMsg}");
                ErrorLogger.LogDllError("DllImport", errorMsg);
                return false;
            }
            catch (EntryPointNotFoundException entryEx)
            {
                string errorMsg = $"DLL 함수 진입점 오류 - {entryEx.Message}";
                Debug.WriteLine($"[DllManager] 검증 실패: {errorMsg}");
                ErrorLogger.LogDllError("DllImport", errorMsg);
                return false;
            }
            catch (BadImageFormatException imgEx)
            {
                string errorMsg = $"DLL 플랫폼 불일치 (x86/x64) - {imgEx.Message}";
                Debug.WriteLine($"[DllManager] 검증 실패: {errorMsg}");
                ErrorLogger.LogDllError("DllImport", errorMsg);
                return false;
            }
            catch (Exception ex)
            {
                // DLL은 로드되었지만 함수 실행 중 오류 (장비 미연결 등)
                // 이 경우는 정상으로 간주 (DLL 자체는 로드 가능)
                Debug.WriteLine($"[DllManager] DLL 로드됨 (함수 실행 오류는 정상: {ex.Message})");
                return true;
            }
        }

        /// <summary>
        /// DLL 재로드 (설정 변경 시 호출)
        /// 주의: DllImport는 CLR이 관리하므로 실제 언로드/재로드는 불가능
        /// </summary>
        public static bool Reload()
        {
            lock (_initLock)
            {
                try
                {
                    Debug.WriteLine("[DllManager] ========== DLL 재로드 시작 ==========");
                    ErrorLogger.Log("DLL 재로드 시작", ErrorLogger.LogLevel.INFO);
                    
                    // DllImport는 CLR이 관리하므로 명시적 언로드 불가
                    // 초기화 플래그만 리셋하고 재초기화 시도
                    _isInitialized = false;
                    _isDllDirectorySet = false; // SetDllDirectory 재설정 허용
                    _currentDllDirectory = "";
                    
                    bool result = Initialize();
                    
                    if (result)
                    {
                        Debug.WriteLine("[DllManager] ========== DLL 재로드 성공 ==========");
                    }
                    else
                    {
                        Debug.WriteLine("[DllManager] ========== DLL 재로드 실패 ==========");
                    }
                    
                    return result;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DllManager] 재로드 오류: {ex.Message}\n{ex.StackTrace}");
                    ErrorLogger.LogException(ex, "DLL 재로드 중 예외 발생");
                    return false;
                }
            }
        }

        /// <summary>
        /// DLL 정리 (프로그램 종료 시 호출)
        /// DllImport는 CLR이 자동 관리하므로 명시적 해제 불필요
        /// </summary>
        public static void Dispose()
        {
            lock (_initLock)
            {
                try
                {
                    _isInitialized = false;
                    _isDllDirectorySet = false;
                    _currentDllDirectory = "";
                    
                    Debug.WriteLine("[DllManager] 해제 완료 (DllImport는 CLR이 자동 관리)");
                    ErrorLogger.Log("DLL 해제 완료", ErrorLogger.LogLevel.INFO);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DllManager] 해제 오류: {ex.Message}");
                    ErrorLogger.LogException(ex, "DLL 해제 중 예외 발생");
                }
            }
        }
        #endregion

        #region DLL Function Declarations (P/Invoke)
        
        /// <summary>
        /// MTP 테스트 함수
        /// C++: int MTP_test(struct input* in, struct output* out)
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, 
                   EntryPoint = "MTP_test", ExactSpelling = true)]
        public static extern int MTP_test(IntPtr input, IntPtr output);

        /// <summary>
        /// IPVS 테스트 함수
        /// C++: int IPVS_test(struct input* in, struct output* out)
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl,
                   EntryPoint = "IPVS_test", ExactSpelling = true)]
        public static extern int IPVS_test(IntPtr input, IntPtr output);

        /// <summary>
        /// PG 포트 연결/해제
        /// C++: bool PGTurn(int port)
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl,
                   EntryPoint = "PGTurn", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]  // C++ bool을 C# bool로 올바르게 매핑
        public static extern bool PGTurn(int port);

        /// <summary>
        /// PG 패턴 전송
        /// C++: bool PGPattern(int pattern)
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl,
                   EntryPoint = "PGPattern", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool PGPattern(int pattern);

        /// <summary>
        /// RGB 전압 전송
        /// C++: bool PGVoltagesnd(int RV, int GV, int BV)
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl,
                   EntryPoint = "PGVoltagesnd", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool PGVoltagesnd(int RV, int GV, int BV);

        /// <summary>
        /// 측정 포트 연결/해제
        /// C++: bool Meas_Turn(int port)
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl,
                   EntryPoint = "Meas_Turn", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool Meas_Turn(int port);

        /// <summary>
        /// 측정 데이터 가져오기
        /// C++: bool Getdata(struct output* out)
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl,
                   EntryPoint = "Getdata", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool Getdata(IntPtr output);

        /// <summary>
        /// LUT 데이터 계산
        /// C++: bool getLUTdata(int rgb, float RV, float GV, float BV, int interval, int cnt, struct output* out)
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl,
                   EntryPoint = "getLUTdata", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool getLUTdata(int rgb, float RV, float GV, float BV, 
                                             int interval, int cnt, IntPtr output);

        #endregion

        #region Utility Methods
        /// <summary>
        /// DLL 상태 정보 가져오기
        /// </summary>
        public static string GetStatus()
        {
            if (!_isInitialized)
                return "DLL이 초기화되지 않음";

            return $"DLL 로드됨: {_dllPath}";
        }

        /// <summary>
        /// DLL 경로 유효성 검사
        /// </summary>
        public static bool ValidateDllPath()
        {
            string dllFolder = GlobalDataManager.GetValue("Settings", "DLL_FOLDER", "");
            if (string.IsNullOrEmpty(dllFolder))
                return false;

            string fullPath = Path.Combine(dllFolder, DLL_NAME);
            return File.Exists(fullPath);
        }
        #endregion
    }
}



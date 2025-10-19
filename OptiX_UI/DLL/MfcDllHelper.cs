using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using OptiX.Common;

namespace OptiX.DLL
{
    /// <summary>
    /// MFC DLL 초기화 및 관리 헬퍼 클래스
    /// MFC 공유 DLL의 안정적인 로딩과 DllMain 초기화를 보장합니다.
    /// </summary>
    public static class MfcDllHelper
    {
        #region MFC DLL 초기화 함수 (선택적)
        
        /// <summary>
        /// MFC DLL 초기화 함수 (C++ DLL에서 제공하는 경우)
        /// C++: extern "C" __declspec(dllexport) bool InitializeMfcDll();
        /// 주의: TestDll.dll에 이 함수가 없으면 EntryPointNotFoundException 발생
        /// </summary>
        /*
        [DllImport("TestDll.dll", CallingConvention = CallingConvention.Cdecl,
                   EntryPoint = "InitializeMfcDll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool InitializeMfcDll();
        
        /// <summary>
        /// MFC DLL 정리 함수 (C++ DLL에서 제공하는 경우)
        /// C++: extern "C" __declspec(dllexport) void UninitializeMfcDll();
        /// </summary>
        [DllImport("TestDll.dll", CallingConvention = CallingConvention.Cdecl,
                   EntryPoint = "UninitializeMfcDll", ExactSpelling = true)]
        private static extern void UninitializeMfcDll();
        */
        
        #endregion

        #region Windows Error Code 상수
        
        /// <summary>
        /// Windows 오류 코드: 모듈을 찾을 수 없음
        /// </summary>
        public const int ERROR_MOD_NOT_FOUND = 126;
        
        /// <summary>
        /// Windows 오류 코드: 프로시저를 찾을 수 없음
        /// </summary>
        public const int ERROR_PROC_NOT_FOUND = 127;
        
        /// <summary>
        /// Windows 오류 코드: 이미 존재함
        /// </summary>
        public const int ERROR_ALREADY_EXISTS = 183;
        
        /// <summary>
        /// Windows 오류 코드: 잘못된 이미지 형식 (플랫폼 불일치)
        /// </summary>
        public const int ERROR_BAD_EXE_FORMAT = 193;
        
        #endregion

        #region 에러 코드 해석
        
        /// <summary>
        /// Windows 오류 코드를 사람이 읽을 수 있는 메시지로 변환
        /// </summary>
        public static string GetErrorMessage(int errorCode)
        {
            switch (errorCode)
            {
                case ERROR_MOD_NOT_FOUND:
                    return "DLL 또는 의존 DLL을 찾을 수 없습니다. " +
                           "mfc140.dll, vcruntime140.dll, msvcp140.dll 등이 누락되었을 수 있습니다.";
                
                case ERROR_PROC_NOT_FOUND:
                    return "DLL 함수 진입점을 찾을 수 없습니다. " +
                           "함수 이름 또는 호출 규약이 일치하지 않습니다.";
                
                case ERROR_ALREADY_EXISTS:
                    return "DLL이 이미 로드되어 있거나 리소스가 이미 존재합니다. " +
                           "일반적으로 무시 가능한 경고입니다.";
                
                case ERROR_BAD_EXE_FORMAT:
                    return "DLL 플랫폼이 일치하지 않습니다 (x86 vs x64). " +
                           "애플리케이션과 DLL의 플랫폼을 일치시켜야 합니다.";
                
                default:
                    return $"알 수 없는 오류 (코드: {errorCode})";
            }
        }
        
        #endregion

        #region MFC DLL 문제 진단
        
        /// <summary>
        /// MFC DLL 로딩 실패 원인을 진단합니다
        /// </summary>
        public static void DiagnoseDllLoadingFailure(string dllPath, int errorCode)
        {
            Debug.WriteLine("========== MFC DLL 로딩 실패 진단 ==========");
            ErrorLogger.Log("MFC DLL 로딩 실패 진단 시작", ErrorLogger.LogLevel.ERROR);
            
            // 1. 에러 코드 분석
            Debug.WriteLine($"에러 코드: {errorCode}");
            Debug.WriteLine($"메시지: {GetErrorMessage(errorCode)}");
            ErrorLogger.Log($"에러 코드 {errorCode}: {GetErrorMessage(errorCode)}", ErrorLogger.LogLevel.ERROR);
            
            // 2. DLL 파일 존재 확인
            bool dllExists = System.IO.File.Exists(dllPath);
            Debug.WriteLine($"DLL 파일 존재: {(dllExists ? "예" : "아니오")} - {dllPath}");
            
            if (!dllExists)
            {
                ErrorLogger.Log($"DLL 파일이 존재하지 않음: {dllPath}", ErrorLogger.LogLevel.ERROR);
                return;
            }
            
            // 3. 의존 DLL 확인
            string dllFolder = System.IO.Path.GetDirectoryName(dllPath);
            string[] mfcDependencies = new[]
            {
                "mfc140.dll", "mfc140u.dll",
                "msvcp140.dll", "vcruntime140.dll",
                "ucrtbase.dll"
            };
            
            Debug.WriteLine("의존 DLL 확인:");
            foreach (string dep in mfcDependencies)
            {
                string depPath = System.IO.Path.Combine(dllFolder, dep);
                bool exists = System.IO.File.Exists(depPath);
                
                if (!exists)
                {
                    Debug.WriteLine($"  ✗ {dep} - 누락");
                    ErrorLogger.Log($"의존 DLL 누락: {dep}", ErrorLogger.LogLevel.ERROR);
                }
                else
                {
                    Debug.WriteLine($"  ✓ {dep} - 발견");
                }
            }
            
            // 4. 플랫폼 확인
            bool is64BitProcess = IntPtr.Size == 8;
            Debug.WriteLine($"프로세스 플랫폼: {(is64BitProcess ? "x64" : "x86")}");
            
            if (errorCode == ERROR_BAD_EXE_FORMAT)
            {
                ErrorLogger.Log(
                    $"플랫폼 불일치 - 현재 프로세스: {(is64BitProcess ? "x64" : "x86")}, " +
                    "DLL 플랫폼 확인 필요",
                    ErrorLogger.LogLevel.ERROR);
            }
            
            Debug.WriteLine("===========================================");
        }
        
        #endregion

        #region MFC DLL 초기화 가이드
        
        /// <summary>
        /// MFC DLL에서 제공해야 할 초기화 함수 예시 (C++ 코드)
        /// 
        /// C++ DLL에 아래 코드를 추가하면 명시적 초기화 가능:
        /// 
        /// // MfcDll.cpp
        /// extern "C" {
        ///     __declspec(dllexport) bool __cdecl InitializeMfcDll()
        ///     {
        ///         AFX_MANAGE_STATE(AfxGetStaticModuleState());
        ///         
        ///         // MFC 애플리케이션 초기화
        ///         if (!AfxWinInit(::GetModuleHandle(NULL), NULL, ::GetCommandLine(), 0))
        ///         {
        ///             return false;
        ///         }
        ///         
        ///         // MFC 확장 DLL 초기화
        ///         static AFX_EXTENSION_MODULE extensionModule = { NULL, NULL };
        ///         if (!AfxInitExtensionModule(extensionModule, AfxGetInstanceHandle()))
        ///         {
        ///             return false;
        ///         }
        ///         
        ///         return true;
        ///     }
        ///     
        ///     __declspec(dllexport) void __cdecl UninitializeMfcDll()
        ///     {
        ///         AFX_MANAGE_STATE(AfxGetStaticModuleState());
        ///         AfxTermExtensionModule(extensionModule);
        ///     }
        /// }
        /// </summary>
        public static void ShowMfcDllInitializationGuide()
        {
            string guide = @"
=== MFC DLL 초기화 가이드 ===

1. MFC DLL이 LoadLibrary 시 즉시 언로드되는 원인:
   - DllMain에서 AfxWinInit 실패 시 FALSE 반환
   - AfxInitExtensionModule 실패
   - 의존 DLL 누락 (mfc140.dll, vcruntime140.dll 등)
   - Thread Local Storage (TLS) 슬롯 부족

2. 해결 방법:
   a) 의존 DLL 배포
      - mfc140.dll (또는 mfc140u.dll for Unicode)
      - msvcp140.dll
      - vcruntime140.dll
      - ucrtbase.dll
   
   b) Visual C++ Redistributable 설치
      - VC++ 2015-2022 Redistributable (x64)
   
   c) DLL 프로젝트 설정 확인
      - 런타임 라이브러리: /MD (멀티스레드 DLL)
      - MFC 사용: 공유 DLL에서 MFC 사용
      - 플랫폼: x64 (애플리케이션과 일치)

3. DllImport 방식의 장점:
   - CLR이 의존성 자동 해결
   - SxS 매니페스트 지원
   - 안정적인 DLL 수명 관리

4. 문제 발생 시 진단:
   - Dependency Walker로 의존 DLL 확인
   - Process Monitor로 DLL 로딩 과정 추적
   - Debug 빌드로 DllMain 진입 확인
";
            
            Debug.WriteLine(guide);
            Console.WriteLine(guide);
        }
        
        #endregion
    }
}


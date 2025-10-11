using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Diagnostics;

namespace OptiX
{
    /// <summary>
    /// DllManager - 네이티브 TestDll.dll 관리 및 함수 호출 중앙화
    /// 
    /// 
    /// 
    /// 이 클래스는 C++로 작성된 TestDll.dll과의 상호작용을 담당합니다.
    /// 주요 기능:
    /// - DLL 로드/해제 및 함수 포인터 관리
    /// - P/Invoke를 통한 안전한 네이티브 함수 호출
    /// - 구조체 마샬링 (Input/Output/Pattern/LUTParameter)
    /// - Optic 시퀀스 실행을 위한 함수 매핑 (PGTurn, MEASTurn, PGPattern, MEAS, MTP)
    /// - 예외 처리 및 디버그 로깅
    /// 
    /// 사용 예시:
    /// - Initialize() → 프로그램 시작 시 한 번 호출
    /// - CallTestFunction(input) → MTP 테스트 실행
    /// - ExecuteMapped("PGTurn", 1) → 시퀀스 함수 호출
    /// - Dispose() → 프로그램 종료 시 호출
    /// </summary>
    public static class DllManager
    {
        #region Private Fields
        
        /// <summary>
        /// 로그 파일 접근 동기화를 위한 Lock 객체 (Zone 간 파일 충돌 방지)
        /// </summary>
        private static readonly object _logFileLock = new object();
        
        /// <summary>
        /// 로드된 DLL의 핸들 (Windows API LoadLibrary 반환값)
        /// IntPtr.Zero는 로드되지 않음을 의미
        /// </summary>
        private static IntPtr _dllHandle = IntPtr.Zero;
        
        /// <summary>
        /// 현재 로드된 DLL의 전체 경로
        /// GlobalDataManager에서 Settings.DLL_FOLDER + "TestDll.dll"로 구성
        /// </summary>
        private static string _dllPath = "";
        
        /// <summary>
        /// DLL 초기화 완료 여부 플래그
        /// true: 모든 함수 호출 가능, false: Initialize() 필요
        /// </summary>
        private static bool _isInitialized = false;
        
        /// <summary>
        /// SEQ 전체 시퀀스 시작 시간 (SEQ00=PGTurn,1 시작 시점)
        /// </summary>
        private static DateTime _seqStartTime;
        
        /// <summary>
        /// SEQ 전체 시퀀스 종료 시간 (모든 Zone 완료 후)
        /// </summary>
        private static DateTime _seqEndTime;
        
        /// <summary>
        /// Zone별 SEQ 시작 시간 (Zone 번호 -> 시작 시간)
        /// </summary>
        private static Dictionary<int, DateTime> _zoneSeqStartTimes = new Dictionary<int, DateTime>();
        
        /// <summary>
        /// 현재 실행 중인 Zone 번호 (1-based)
        /// </summary>
        private static int _currentZone = 1;
        
        /// <summary>
        /// Zone별 마지막 DLL 실행 결과 저장 (실제 측정 데이터)
        /// </summary>
        private static Dictionary<int, Output> _zoneResults = new Dictionary<int, Output>();
        
        /// <summary>
        /// 저장된 Zone 결과를 가져오는 함수
        /// </summary>
        public static Output? GetStoredZoneResult(int zoneNumber)
        {
            lock (_zoneResults)
            {
                if (_zoneResults.ContainsKey(zoneNumber))
                {
                    System.Diagnostics.Debug.WriteLine($"Zone {zoneNumber} 저장된 결과 반환 (총 {_zoneResults.Count}개 Zone)");
                    return _zoneResults[zoneNumber];
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Zone {zoneNumber} 저장된 결과 없음 (총 {_zoneResults.Count}개 Zone)");
                    return null;
                }
            }
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// DLL 초기화 상태 확인 (읽기 전용)
        /// </summary>
        /// <value>true: 초기화 완료, false: 초기화 필요</value>
        public static bool IsInitialized => _isInitialized;

        /// <summary>
        /// 현재 로드된 DLL의 전체 파일 경로 (읽기 전용)
        /// </summary>
        /// <value>DLL 파일의 절대 경로 문자열</value>
        public static string DllPath => _dllPath;
        #endregion

        #region P/Invoke Declarations
        /// <summary>
        /// Windows API: DLL 파일을 메모리에 로드
        /// </summary>
        /// <param name="dllToLoad">로드할 DLL 파일의 경로</param>
        /// <returns>DLL 핸들 (실패 시 IntPtr.Zero)</returns>
        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string dllToLoad);

        /// <summary>
        /// Windows API: 로드된 DLL에서 함수 주소 가져오기
        /// </summary>
        /// <param name="hModule">DLL 핸들</param>
        /// <param name="procedureName">함수 이름 (C++에서 extern "C"로 내보낸 이름)</param>
        /// <returns>함수 주소 (실패 시 IntPtr.Zero)</returns>
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

        /// <summary>
        /// Windows API: 로드된 DLL을 메모리에서 해제
        /// </summary>
        /// <param name="hModule">해제할 DLL 핸들</param>
        /// <returns>해제 성공 여부</returns>
        [DllImport("kernel32.dll")]
        private static extern bool FreeLibrary(IntPtr hModule);
        #endregion

        #region DLL Management Methods
        /// <summary>
        /// DLL 초기화 (프로그램 시작 시 한 번 호출)
        /// 
        /// 이 메서드는 TestDll.dll을 로드하고 모든 함수 포인터를 준비합니다.
        /// 초기화 과정:
        /// 1. 기존 DLL이 로드되어 있으면 먼저 해제
        /// 2. GlobalDataManager에서 Settings.DLL_FOLDER 경로 읽기
        /// 3. TestDll.dll 파일 존재 여부 확인
        /// 4. LoadLibrary로 DLL 로드
        /// 5. 로드 성공 시 _isInitialized = true 설정
        /// 
        /// 주의사항:
        /// - 프로그램 시작 시 MainWindow에서 반드시 호출해야 함
        /// - 실패 시 모든 DLL 함수 호출이 불가능
        /// - 경로가 잘못되었거나 DLL이 손상된 경우 false 반환
        /// </summary>
        /// <returns>초기화 성공 여부 (true: 성공, false: 실패)</returns>
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

        /// <summary>
        /// DLL 재로드 (메인 설정창에서 SAVE 버튼 클릭 시 호출)
        /// 
        /// 사용자가 설정에서 DLL 경로를 변경한 후 SAVE 버튼을 클릭했을 때 호출됩니다.
        /// 내부적으로 Initialize()를 다시 실행하여 새로운 경로의 DLL을 로드합니다.
        /// 
        /// 주의사항:
        /// - 기존 DLL이 사용 중이면 해제 후 새 DLL 로드
        /// - 새 경로의 DLL이 유효하지 않으면 기존 상태 유지
        /// - 재로드 중에는 일시적으로 모든 DLL 함수 호출이 불가능
        /// </summary>
        /// <returns>재로드 성공 여부 (true: 성공, false: 실패)</returns>
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
        /// DLL 해제 (프로그램 종료 시 호출)
        /// 
        /// 로드된 TestDll.dll을 메모리에서 안전하게 해제합니다.
        /// 프로그램 종료 시 자동으로 호출되지만, 명시적으로 호출해도 안전합니다.
        /// 
        /// 해제 과정:
        /// 1. _dllHandle이 유효한지 확인
        /// 2. FreeLibrary로 DLL 해제
        /// 3. 핸들과 상태 플래그 초기화
        /// 
        /// 주의사항:
        /// - 해제 후에는 모든 DLL 함수 호출이 불가능
        /// - 이미 해제된 DLL을 다시 해제해도 안전 (무시됨)
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
        /// 
        /// 로드된 TestDll.dll에서 지정된 함수의 메모리 주소를 가져옵니다.
        /// 이 주소는 Marshal.GetDelegateForFunctionPointer에서 델리게이트 생성에 사용됩니다.
        /// 
        /// 함수 이름 규칙:
        /// - C++에서 extern "C"로 내보낸 이름 사용
        /// - C++ 컴파일러가 이름 맹글링을 하지 않도록 extern "C" 필수
        /// - 예: "test", "PGTurn", "Meas_Turn" 등
        /// 
        /// 예외:
        /// - InvalidOperationException: DLL이 초기화되지 않음
        /// - InvalidOperationException: 함수를 찾을 수 없음
        /// </summary>
        /// <param name="functionName">C++ DLL에서 내보낸 함수 이름 (extern "C")</param>
        /// <returns>함수의 메모리 주소 (IntPtr)</returns>
        /// <exception cref="InvalidOperationException">DLL이 초기화되지 않았거나 함수를 찾을 수 없음</exception>
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
        /// DLL 함수를 .NET 델리게이트로 변환
        /// 
        /// 네이티브 DLL 함수를 .NET에서 안전하게 호출할 수 있는 델리게이트로 변환합니다.
        /// 이 메서드는 P/Invoke의 복잡성을 숨기고 타입 안전한 함수 호출을 제공합니다.
        /// 
        /// 변환 과정:
        /// 1. GetFunctionAddress로 함수 주소 가져오기
        /// 2. Marshal.GetDelegateForFunctionPointer로 델리게이트 생성
        /// 3. 타입 안전한 델리게이트 반환
        /// 
        /// 사용 예시:
        /// - var testFunc = GetFunction<TestFunction>("test");
        /// - var pgTurnFunc = GetFunction<PGTurnFunction>("PGTurn");
        /// 
        /// 주의사항:
        /// - T는 UnmanagedFunctionPointer 특성이 있는 델리게이트 타입이어야 함
        /// - 함수 시그니처가 정확히 일치해야 함 (CallingConvention 포함)
        /// </summary>
        /// <typeparam name="T">변환할 델리게이트 타입 (UnmanagedFunctionPointer 특성 필요)</typeparam>
        /// <param name="functionName">DLL에서 내보낸 함수 이름</param>
        /// <returns>함수에 대응하는 .NET 델리게이트</returns>
        /// <exception cref="InvalidOperationException">DLL이 초기화되지 않았거나 함수를 찾을 수 없음</exception>
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
        /// 
        /// 이 섹션의 메서드들은 ManualPage에서 개별적으로 테스트할 때 사용됩니다.
        /// 각 함수는 해당하는 C++ DLL 함수를 직접 호출합니다.
        /// </summary>

        /// <summary>
        /// PG 포트 연결/해제
        /// 
        /// Pattern Generator의 포트를 연결하거나 해제합니다.
        /// ManualPage에서 PG 포트 테스트 시 사용됩니다.
        /// 
        /// 포트 번호 규칙:
        /// - 양수: 포트 연결
        /// - 0 또는 음수: 포트 해제
        /// - 일반적으로 1, 2, 3 등의 포트 번호 사용
        /// </summary>
        /// <param name="port">연결/해제할 포트 번호 (양수=연결, 0=해제)</param>
        /// <returns>연결/해제 성공 여부</returns>
        /// <exception cref="InvalidOperationException">DLL이 초기화되지 않음</exception>
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
        /// 
        /// Pattern Generator에 특정 패턴을 전송합니다.
        /// ManualPage에서 패턴 테스트 시 사용됩니다.
        /// 
        /// 패턴 번호 규칙:
        /// - 0: 기본 패턴
        /// - 1~N: 사용자 정의 패턴
        /// - 패턴은 미리 정의된 테스트 패턴을 의미
        /// </summary>
        /// <param name="pattern">전송할 패턴 번호 (0=기본, 1~N=사용자 정의)</param>
        /// <returns>패턴 전송 성공 여부</returns>
        /// <exception cref="InvalidOperationException">DLL이 초기화되지 않음</exception>
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
        /// 
        /// Pattern Generator에 RGB 각 색상의 전압을 설정합니다.
        /// ManualPage에서 전압 테스트 시 사용됩니다.
        /// 
        /// 전압 설정 규칙:
        /// - RV: Red 전압 (일반적으로 0~255 또는 0~1000 범위)
        /// - GV: Green 전압 (일반적으로 0~255 또는 0~1000 범위)
        /// - BV: Blue 전압 (일반적으로 0~255 또는 0~1000 범위)
        /// - 각 전압은 디지털 값으로 변환되어 하드웨어에 전달
        /// </summary>
        /// <param name="RV">Red 전압 값 (0~255 또는 0~1000)</param>
        /// <param name="GV">Green 전압 값 (0~255 또는 0~1000)</param>
        /// <param name="BV">Blue 전압 값 (0~255 또는 0~1000)</param>
        /// <returns>전압 설정 성공 여부</returns>
        /// <exception cref="InvalidOperationException">DLL이 초기화되지 않음</exception>
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
        /// 
        /// 측정 장비의 포트를 연결하거나 해제합니다.
        /// ManualPage에서 측정 포트 테스트 시 사용됩니다.
        /// 
        /// 포트 번호 규칙:
        /// - 양수: 포트 연결
        /// - 0 또는 음수: 포트 해제
        /// - 일반적으로 1, 2, 3 등의 포트 번호 사용
        /// - PG 포트와는 별개의 측정 전용 포트
        /// </summary>
        /// <param name="port">연결/해제할 측정 포트 번호 (양수=연결, 0=해제)</param>
        /// <returns>연결/해제 성공 여부</returns>
        /// <exception cref="InvalidOperationException">DLL이 초기화되지 않음</exception>
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
        /// 
        /// 측정 장비에서 현재 측정된 데이터를 가져옵니다.
        /// OpticPage에서 MEAS 시퀀스 실행 시 사용됩니다.
        /// 
        /// 측정 데이터 구조:
        /// - Pattern 구조체: x, y, u, v, L, cur, eff 값 포함
        /// - 현재는 첫 번째 WAD(각도 0도)의 측정값만 반환
        /// - 실제로는 7개 WAD의 측정값이 모두 측정됨
        /// 
        /// 사용 시점:
        /// - PGTurn, MEASTurn, PGPattern 실행 후
        /// - MEAS 시퀀스 단계에서 호출
        /// - 측정 완료 후 UI에 데이터 표시
        /// </summary>
        /// <returns>튜플: (측정 데이터 구조체, 성공 여부)</returns>
        /// <exception cref="InvalidOperationException">DLL이 초기화되지 않음</exception>
        public static (Pattern measureData, bool success) CallGetdata()
        {
            if (!_isInitialized)
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
                    var getdataFunc = GetFunction<GetdataFunction>("Getdata");
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

        /// <summary>
        /// LUT 데이터 가져오기
        /// 
        /// Look-Up Table 데이터를 계산하여 가져옵니다.
        /// LUTPage에서 LUT 계산 시 사용됩니다.
        /// 
        /// LUT 계산 과정:
        /// 1. 지정된 RGB 색상에 대한 전압 설정
        /// 2. 샘플링 간격과 개수에 따라 데이터 수집
        /// 3. LUT 파라미터 계산 (max_lumi, max_index, gamma, black)
        /// 4. 계산된 파라미터 반환
        /// 
        /// RGB 인덱스:
        /// - 0: Red 채널
        /// - 1: Green 채널  
        /// - 2: Blue 채널
        /// 
        /// 샘플링 파라미터:
        /// - interval: 샘플 간격 (밀리초 단위)
        /// - cnt: 샘플 개수 (측정할 데이터 포인트 수)
        /// </summary>
        /// <param name="rgb">RGB 채널 인덱스 (0=Red, 1=Green, 2=Blue)</param>
        /// <param name="RV">Red 전압 값</param>
        /// <param name="GV">Green 전압 값</param>
        /// <param name="BV">Blue 전압 값</param>
        /// <param name="interval">샘플링 간격 (밀리초)</param>
        /// <param name="cnt">샘플링 개수</param>
        /// <returns>튜플: (LUT 파라미터 구조체, 성공 여부)</returns>
        /// <exception cref="InvalidOperationException">DLL이 초기화되지 않음</exception>
        public static (LUTParameter lutParam, bool success) CallGetLUTdata(int rgb, float RV, float GV, float BV, int interval, int cnt)
        {
            if (!_isInitialized)
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
                    var getLUTdataFunc = GetFunction<GetLUTdataFunction>("getLUTdata");
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

        #endregion

        #region Data Validation Methods
        
        /// <summary>
        /// Pattern 데이터 유효성 검증
        /// 
        /// CIE 좌표의 정상 범위를 확인하여 이상한 값(손상된 메모리, 초기화되지 않은 값)을 필터링합니다.
        /// 
        /// 검증 기준:
        /// - x: 0.0 ~ 0.8 (CIE 1931)
        /// - y: 0.0 ~ 0.9 (CIE 1931)
        /// - u: 0.0 ~ 0.6 (CIE 1976)
        /// - v: 0.0 ~ 0.6 (CIE 1976)
        /// - L: 0.0 ~ 10000 (휘도, cd/m²)
        /// - cur: 0.0 ~ 1000 (전류, mA)
        /// - eff: 0.0 ~ 100 (효율, %)
        /// 
        /// NaN, Infinity, 음수 값도 무효로 처리
        /// </summary>
        /// <param name="pattern">검증할 Pattern 구조체</param>
        /// <returns>유효한 데이터면 true, 이상한 값이면 false</returns>
        private static bool IsValidPattern(Pattern pattern)
        {
            try
            {
                // NaN, Infinity 체크
                if (float.IsNaN(pattern.x) || float.IsInfinity(pattern.x) ||
                    float.IsNaN(pattern.y) || float.IsInfinity(pattern.y) ||
                    float.IsNaN(pattern.u) || float.IsInfinity(pattern.u) ||
                    float.IsNaN(pattern.v) || float.IsInfinity(pattern.v) ||
                    float.IsNaN(pattern.L) || float.IsInfinity(pattern.L) ||
                    float.IsNaN(pattern.cur) || float.IsInfinity(pattern.cur) ||
                    float.IsNaN(pattern.eff) || float.IsInfinity(pattern.eff))
                {
                    return false;
                }
                
                // CIE 1931 좌표 범위 체크
                if (pattern.x < 0.0f || pattern.x > 0.8f ||
                    pattern.y < 0.0f || pattern.y > 0.9f)
                {
                    return false;
                }
                
                // CIE 1976 좌표 범위 체크
                if (pattern.u < 0.0f || pattern.u > 0.6f ||
                    pattern.v < 0.0f || pattern.v > 0.6f)
                {
                    return false;
                }
                
                // 물리적 한계 체크
                if (pattern.L < 0.0f || pattern.L > 10000.0f ||
                    pattern.cur < 0.0f || pattern.cur > 1000.0f ||
                    pattern.eff < 0.0f || pattern.eff > 1000.0f)  // 효율은 1000%까지 허용
                {
                    return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        
        #endregion

        #region Utility Methods
        /// <summary>
        /// 시퀀스 매핑 호출자
        /// 
        /// OpticPage의 SEQ 실행에서 사용되는 핵심 함수입니다.
        /// INI 파일의 [SEQ] 섹션에서 읽은 함수명과 인자를 받아 해당 DLL 함수를 호출합니다.
        /// 
        /// 지원하는 함수들:
        /// - PGTurn(port): PG 포트 연결/해제
        /// - MEASTurn(port): 측정 포트 연결/해제
        /// - PGPattern(pattern): 패턴 전송
        /// - MEAS(): 측정 데이터 가져오기 (인자 없음)
        /// - MTP(): MTP 테스트 실행 (기본값 사용)
        /// 
        /// 사용 예시:
        /// - ExecuteMapped("PGTurn", 1) → CallPGTurn(1)
        /// - ExecuteMapped("MEAS", null) → CallGetdata()
        /// - ExecuteMapped("MTP", null) → CallTestFunction(기본값)
        /// 
        /// 주의사항:
        /// - MTP는 실제로는 OpticPage에서 직접 CallTestFunction 호출
        /// - 여기서는 기본값으로 빈 Input 구조체 사용
        /// - 함수명은 대소문자 구분 없음
        /// </summary>
        /// <param name="functionName">SEQ에서 지정된 함수명 (PGTurn, MEASTurn, PGPattern, MEAS, MTP)</param>
        /// <param name="arg">함수에 전달할 정수 인자 (null 가능)</param>
        /// <returns>함수 호출 성공 여부</returns>
        /// <exception cref="InvalidOperationException">DLL이 초기화되지 않음</exception>
        public static bool ExecuteMapped(string functionName, int? arg, int zoneNumber = 1)
        {
            if (!_isInitialized)
                throw new InvalidOperationException("DLL이 초기화되지 않았습니다.");

            try
            {
                // Zone 번호 저장 (MAKE_RESULT_LOG에서 사용)
                _currentZone = zoneNumber - 1; // 0-based로 변환하여 저장
                
                // SEQ 시작 시간 설정 (첫 번째 함수 호출 시)
                if (_seqStartTime == default(DateTime))
                {
                    _seqStartTime = DateTime.Now;
                }
                
                switch (functionName?.Trim().ToUpperInvariant())
                {
                    case "PGTURN":
                        return CallPGTurn(arg ?? 0);
                    case "MEASTURN":
                        return CallMeasTurn(arg ?? 0);
                    case "PGPATTERN":
                        return CallPGPattern(arg ?? 0);
                    case "MEAS":
                    {
                        // DLL 호출부 - MEAS는 실제 DLL 호출하지 않음
                        System.Diagnostics.Debug.WriteLine($"[Zone {zoneNumber}] MEAS 명령어 실행 - DLL 호출하지 않음");
                        return true; // SEQ는 정상 진행
                    }
                    case "MTP":
                    {
                        System.Diagnostics.Debug.WriteLine($"[Zone {zoneNumber}] MTP 명령어 실행 시작 - test 함수 호출 예정");
                        
                        // Zone별 CELL_ID, INNER_ID 가져오기 (전역 변수에서 - INI 파일 읽기 없음)
                        var (cellId, innerId) = GlobalDataManager.GetZoneInfo(zoneNumber);
                        
                        var input = new Input
                        {
                            CELL_ID = cellId,
                            INNER_ID = innerId,
                            total_point = 0,
                            cur_point = 0
                        };
                        var (output, ok) = CallTestFunction(input);
                        
                        System.Diagnostics.Debug.WriteLine($"[Zone {zoneNumber}] MTP 명령어 실행 완료 - test 함수 호출됨, 결과: {ok}");
                        
                        // 실제 DLL 결과를 Zone별로 저장
                        if (ok)
                        {
                            lock (_zoneResults)
                            {
                                _zoneResults[zoneNumber] = output;
                                System.Diagnostics.Debug.WriteLine($"Zone {zoneNumber} MTP 결과 저장됨 - 저장된 Zone 개수: {_zoneResults.Count}");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Zone {zoneNumber} MTP 실행 실패 - 결과 저장 안됨");
                        }
                        
                        return ok;
                    }
                    case "MAKE_RESULT_LOG":
                    {
                        // MAKE_RESULT_LOG는 모든 SEQ 완료 후 한 번에 처리됨 (SEQ에서 제거)
                        System.Diagnostics.Debug.WriteLine($"[Zone {zoneNumber}] MAKE_RESULT_LOG은 SEQ에서 실행하지 않음 (모든 SEQ 완료 후 일괄 처리)");
                        return true; // SEQ는 정상 진행
                    }
                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// MAKE_RESULT_LOG 실행 메서드
        /// </summary>
        /// <param name="seqStartTime">SEQ 시작 시간</param>
        /// <param name="zoneNumber">현재 Zone 번호 (1-based)</param>
        /// <returns>로그 생성 성공 여부</returns>
        private static bool ExecuteMakeResultLog(DateTime seqStartTime, int zoneNumber = 1)
        {
            // Zone 간 파일 충돌 방지를 위한 Lock (여러 Zone이 동시에 같은 파일에 쓰기 방지)
            lock (_logFileLock)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[Zone {zoneNumber}] ExecuteMakeResultLog 시작 (Lock 획득)");
                    
                    // Zone 정보 사용
                    int currentZone = zoneNumber - 1; // 0-based로 변환
                    
                    // Cell ID와 Inner ID 가져오기
                    string cellIdKey = $"CELL_ID_ZONE_{zoneNumber}";
                    string innerIdKey = $"INNER_ID_ZONE_{zoneNumber}";
                    
                    string cellId = GlobalDataManager.GetValue("MTP", cellIdKey, "");
                    string innerId = GlobalDataManager.GetValue("MTP", innerIdKey, "");
                    
                    System.Diagnostics.Debug.WriteLine($"[Zone {zoneNumber}] Cell ID: {cellId}, Inner ID: {innerId}");
                    
                    // START TIME은 SEQ 시작 시간, END TIME은 로그 함수 진입 시간
                    var startTime = seqStartTime;
                    var endTime = DateTime.Now; // 로그 함수 진입 시점
                    
                    bool allSuccess = true;
                
                // EECP 로그 생성
                try
                {
                    string createEecp = GlobalDataManager.GetValue("MTP", "CREATE_EECP", "F");
                    if (createEecp == "T")
                    {
                        // Singleton Instance 사용
                        var eecpLogger = OptiX_UI.Result_LOG.EECPLogger.Instance;
                        
                        // 저장된 실제 DLL 결과 사용
                        if (_zoneResults.ContainsKey(zoneNumber))
                        {
                            var outputData = ConvertToOutputData(_zoneResults[zoneNumber]);
                            eecpLogger.LogEECPData(startTime, endTime, cellId, innerId, zoneNumber, outputData);
                            System.Diagnostics.Debug.WriteLine($"Zone {zoneNumber} EECP 로그 생성 완료 (실제 데이터 사용)");
                        }
                        else
                        {
                            // 저장된 데이터가 없으면 더미 데이터 사용
                            var dummyOutputData = CreateDummyOutputData();
                            eecpLogger.LogEECPData(startTime, endTime, cellId, innerId, zoneNumber, dummyOutputData);
                            System.Diagnostics.Debug.WriteLine($"Zone {zoneNumber} EECP 로그 생성 완료 (더미 데이터 사용)");
                        }
                    }
                }
                catch (Exception ex)
                {
                    allSuccess = false;
                }
                
                // EECP_SUMMARY 로그 생성
                try
                {
                    string createEecpSummary = GlobalDataManager.GetValue("MTP", "CREATE_EECP_SUMMARY", "F");
                    if (createEecpSummary == "T")
                    {
                        // Singleton Instance 사용
                        var eecpSummaryLogger = OptiX_UI.Result_LOG.EECPSummaryLogger.Instance;
                        string summaryData = $"Zone_{zoneNumber}_Summary_Data";
                        eecpSummaryLogger.LogEECPSummaryData(startTime, endTime, cellId, innerId, zoneNumber, summaryData);
                    }
                }
                catch (Exception ex)
                {
                    allSuccess = false;
                }
                
                // CIM 로그 생성
                try
                {
                    string createCim = GlobalDataManager.GetValue("MTP", "CREATE_CIM", "F");
                    if (createCim == "T")
                    {
                        // Singleton Instance 사용
                        var cimLogger = OptiX_UI.Result_LOG.CIMLogger.Instance;
                        string cimData = $"Zone_{zoneNumber}_CIM_Data";
                        cimLogger.LogCIMData(startTime, endTime, cellId, innerId, zoneNumber, cimData);
                    }
                }
                catch (Exception ex)
                {
                    allSuccess = false;
                }
                
                // VALIDATION 로그 생성
                try
                {
                    string createValidation = GlobalDataManager.GetValue("MTP", "CREATE_VALIDATION", "F");
                    if (createValidation == "T")
                    {
                        // Singleton Instance 사용
                        var validationLogger = OptiX_UI.Result_LOG.ValidationLogger.Instance;
                        string validationData = $"Zone_{zoneNumber}_Validation_Data";
                        validationLogger.LogValidationData(startTime, endTime, cellId, innerId, zoneNumber, validationData);
                    }
                }
                catch (Exception ex)
                {
                    allSuccess = false;
                }
                
                    return allSuccess;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Zone {zoneNumber}] ExecuteMakeResultLog 오류: {ex.Message}");
                    return false;
                }
            } // lock 종료
        }
        
        /// <summary>
        /// SEQ 시작 시간 리셋 (새로운 시퀀스 시작 시 호출)
        /// </summary>
        public static void ResetSeqStartTime()
        {
            _seqStartTime = default(DateTime);
            _seqEndTime = default(DateTime);
            lock (_zoneSeqStartTimes)
            {
                _zoneSeqStartTimes.Clear();
                System.Diagnostics.Debug.WriteLine("SEQ 시작/종료 시간 및 Zone별 시간 초기화 완료");
            }
        }
        
        /// <summary>
        /// SEQ 시작 시간 가져오기
        /// </summary>
        public static DateTime GetSeqStartTime()
        {
            return _seqStartTime;
        }
        
        /// <summary>
        /// SEQ 종료 시간 설정
        /// </summary>
        public static void SetSeqEndTime(DateTime endTime)
        {
            _seqEndTime = endTime;
            System.Diagnostics.Debug.WriteLine($"SEQ 종료 시간 설정: {endTime:HH:mm:ss.fff}");
        }
        
        /// <summary>
        /// SEQ 종료 시간 가져오기
        /// </summary>
        public static DateTime GetSeqEndTime()
        {
            return _seqEndTime;
        }
        
        /// <summary>
        /// Zone별 SEQ 시작 시간 설정
        /// </summary>
        public static void SetZoneSeqStartTime(int zoneNumber, DateTime startTime)
        {
            lock (_zoneSeqStartTimes)
            {
                _zoneSeqStartTimes[zoneNumber] = startTime;
                System.Diagnostics.Debug.WriteLine($"Zone {zoneNumber} SEQ 시작 시간 설정: {startTime:HH:mm:ss.fff}");
            }
        }
        
        /// <summary>
        /// Zone별 SEQ 시작 시간 가져오기
        /// </summary>
        public static DateTime GetZoneSeqStartTime(int zoneNumber)
        {
            lock (_zoneSeqStartTimes)
            {
                if (_zoneSeqStartTimes.ContainsKey(zoneNumber))
                {
                    return _zoneSeqStartTimes[zoneNumber];
                }
                else
                {
                    // Zone별 시작 시간이 없으면 전체 SEQ 시작 시간 반환
                    System.Diagnostics.Debug.WriteLine($"Zone {zoneNumber} SEQ 시작 시간이 없어서 전체 SEQ 시작 시간 사용");
                    return _seqStartTime;
                }
            }
        }
        
        /// <summary>
        /// Zone별 SEQ 종료 시간 설정
        /// </summary>
        public static void SetZoneSeqEndTime(int zoneNumber, DateTime endTime)
        {
            lock (_zoneSeqStartTimes) // 같은 lock 사용
            {
                _zoneSeqStartTimes[zoneNumber + 1000] = endTime; // 1000을 더해서 종료시간 구분
                System.Diagnostics.Debug.WriteLine($"Zone {zoneNumber} SEQ 종료 시간 설정: {endTime:HH:mm:ss.fff}");
            }
        }
        
        /// <summary>
        /// Zone별 SEQ 종료 시간 가져오기
        /// </summary>
        public static DateTime GetZoneSeqEndTime(int zoneNumber)
        {
            lock (_zoneSeqStartTimes)
            {
                int endTimeKey = zoneNumber + 1000;
                if (_zoneSeqStartTimes.ContainsKey(endTimeKey))
                {
                    return _zoneSeqStartTimes[endTimeKey];
                }
                else
                {
                    // Zone별 종료 시간이 없으면 전체 SEQ 종료 시간 반환
                    System.Diagnostics.Debug.WriteLine($"Zone {zoneNumber} SEQ 종료 시간이 없어서 전체 SEQ 종료 시간 사용");
                    return _seqEndTime;
                }
            }
        }
        
        /// <summary>
        /// Zone별 결과 로그를 생성 (모든 SEQ 완료 후 호출)
        /// Thread-Safe 하지 않으므로 순차 호출 필요
        /// </summary>
        public static bool CreateResultLogsForZone(
            DateTime startTime, 
            DateTime endTime, 
            string cellId, 
            string innerId, 
            int zoneNumber, 
            Output output)
        {
            // Zone 간 파일 충돌 방지를 위한 Lock (여러 Zone이 동시에 같은 파일에 쓰기 방지)
            lock (_logFileLock)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[Zone {zoneNumber}] 로그 생성 시작 (Lock 획득)");
                
                bool allSuccess = true;
                var outputData = ConvertToOutputData(output);
                
                // EECP 로그 생성
                try
                {
                    string createEecp = GlobalDataManager.GetValue("MTP", "CREATE_EECP", "F");
                    if (createEecp == "T")
                    {
                        // Singleton Instance 사용
                        var eecpLogger = OptiX_UI.Result_LOG.EECPLogger.Instance;
                        eecpLogger.LogEECPData(startTime, endTime, cellId, innerId, zoneNumber, outputData);
                        System.Diagnostics.Debug.WriteLine($"[Zone {zoneNumber}] EECP 로그 생성 완료");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Zone {zoneNumber}] EECP 로그 오류: {ex.Message}");
                    allSuccess = false;
                }
                
                // EECP_SUMMARY 로그 생성
                try
                {
                    string createEecpSummary = GlobalDataManager.GetValue("MTP", "CREATE_EECP_SUMMARY", "F");
                    if (createEecpSummary == "T")
                    {
                        // Singleton Instance 사용
                        var eecpSummaryLogger = OptiX_UI.Result_LOG.EECPSummaryLogger.Instance;
                        string summaryData = $"Zone_{zoneNumber}_Summary_Data";
                        eecpSummaryLogger.LogEECPSummaryData(startTime, endTime, cellId, innerId, zoneNumber, summaryData);
                        System.Diagnostics.Debug.WriteLine($"[Zone {zoneNumber}] EECP_SUMMARY 로그 생성 완료");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Zone {zoneNumber}] EECP_SUMMARY 로그 오류: {ex.Message}");
                    allSuccess = false;
                }
                
                // CIM 로그 생성
                try
                {
                    string createCim = GlobalDataManager.GetValue("MTP", "CREATE_CIM", "F");
                    if (createCim == "T")
                    {
                        // Singleton Instance 사용
                        var cimLogger = OptiX_UI.Result_LOG.CIMLogger.Instance;
                        string cimData = $"Zone_{zoneNumber}_CIM_Data";
                        cimLogger.LogCIMData(startTime, endTime, cellId, innerId, zoneNumber, cimData);
                        System.Diagnostics.Debug.WriteLine($"[Zone {zoneNumber}] CIM 로그 생성 완료");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Zone {zoneNumber}] CIM 로그 오류: {ex.Message}");
                    allSuccess = false;
                }
                
                // VALIDATION 로그 생성
                try
                {
                    string createValidation = GlobalDataManager.GetValue("MTP", "CREATE_VALIDATION", "F");
                    if (createValidation == "T")
                    {
                        // Singleton Instance 사용
                        var validationLogger = OptiX_UI.Result_LOG.ValidationLogger.Instance;
                        string validationData = $"Zone_{zoneNumber}_Validation_Data";
                        validationLogger.LogValidationData(startTime, endTime, cellId, innerId, zoneNumber, validationData);
                        System.Diagnostics.Debug.WriteLine($"[Zone {zoneNumber}] VALIDATION 로그 생성 완료");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Zone {zoneNumber}] VALIDATION 로그 오류: {ex.Message}");
                    allSuccess = false;
                }
                
                    return allSuccess;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Zone {zoneNumber}] 로그 생성 전체 오류: {ex.Message}");
                    return false;
                }
            } // lock 종료
        }
        
        /// <summary>
        /// 현재 Zone 설정 (Zone 변경 시 호출)
        /// </summary>
        /// <param name="zoneNumber">Zone 번호 (1-based)</param>
        public static void SetCurrentZone(int zoneNumber)
        {
            _currentZone = zoneNumber;
        }
        
        /// <summary>
        /// Output 구조체를 OutputData로 변환 (이상한 값은 0으로 처리)
        /// </summary>
        private static OptiX_UI.Result_LOG.OutputData ConvertToOutputData(Output output)
        {
            var outputData = new OptiX_UI.Result_LOG.OutputData();
            
            // data 배열 초기화 (7x17)
            outputData.data = new OptiX_UI.Result_LOG.Pattern[7, 17];
            
            // output.data를 2차원 배열로 변환 (이상한 값은 0으로 처리)
            for (int wad = 0; wad < 7; wad++)
            {
                for (int pattern = 0; pattern < 17; pattern++)
                {
                    int index = wad * 17 + pattern;
                    if (index < output.data.Length)
                    {
                        var sourcePattern = output.data[index];
                        outputData.data[wad, pattern] = new OptiX_UI.Result_LOG.Pattern
                        {
                            x = SanitizeValue(sourcePattern.x),
                            y = SanitizeValue(sourcePattern.y),
                            u = SanitizeValue(sourcePattern.u),
                            v = SanitizeValue(sourcePattern.v),
                            L = SanitizeValue(sourcePattern.L),
                            cur = SanitizeValue(sourcePattern.cur),
                            eff = SanitizeValue(sourcePattern.eff)
                        };
                    }
                }
            }
            
            // measure 배열 초기화 (7개)
            outputData.measure = new OptiX_UI.Result_LOG.Pattern[7];
            for (int i = 0; i < 7 && i < output.measure.Length; i++)
            {
                var sourceMeasure = output.measure[i];
                outputData.measure[i] = new OptiX_UI.Result_LOG.Pattern 
                { 
                    x = SanitizeValue(sourceMeasure.x),
                    y = SanitizeValue(sourceMeasure.y),
                    u = SanitizeValue(sourceMeasure.u),
                    v = SanitizeValue(sourceMeasure.v),
                    L = SanitizeValue(sourceMeasure.L),
                    cur = SanitizeValue(sourceMeasure.cur),
                    eff = SanitizeValue(sourceMeasure.eff)
                };
            }
            
            // lut 배열 초기화 (3개)
            outputData.lut = new OptiX_UI.Result_LOG.LutParameter[3];
            for (int i = 0; i < 3 && i < output.lut.Length; i++)
            {
                outputData.lut[i] = new OptiX_UI.Result_LOG.LutParameter
                {
                    value1 = SanitizeValue(output.lut[i].max_lumi),    // max_lumi → value1
                    value2 = SanitizeValue(output.lut[i].max_index),   // max_index → value2
                    value3 = SanitizeValue(output.lut[i].gamma)       // gamma → value3
                };
            }
            
            return outputData;
        }

        /// <summary>
        /// 이상한 값(6.49E+27 같은)을 0으로 처리
        /// </summary>
        private static float SanitizeValue(float value)
        {
            // NaN, Infinity, 매우 큰 값들을 0으로 처리
            if (float.IsNaN(value) || float.IsInfinity(value) || 
                Math.Abs(value) > 1e10f) // 10억보다 큰 값은 이상한 값으로 간주
            {
                return 0.0f;
            }
            return value;
        }

        /// <summary>
        /// 더미 OutputData 생성 (테스트용)
        /// </summary>
        private static OptiX_UI.Result_LOG.OutputData CreateDummyOutputData()
        {
            var outputData = new OptiX_UI.Result_LOG.OutputData();
            
            // data 배열 초기화 (7x17)
            outputData.data = new OptiX_UI.Result_LOG.Pattern[7, 17];
            
            // 더미 데이터 생성
            for (int wad = 0; wad < 7; wad++)
            {
                for (int pattern = 0; pattern < 17; pattern++)
                {
                    outputData.data[wad, pattern] = new OptiX_UI.Result_LOG.Pattern
                    {
                        x = 1.0f + wad * 0.1f + pattern * 0.01f,  // 기본값 1.0 추가
                        y = 2.0f + wad * 0.2f + pattern * 0.02f,  // 기본값 2.0 추가
                        u = 0.2f + wad * 0.15f + pattern * 0.015f, // 기본값 0.2 추가
                        v = 0.3f + wad * 0.25f + pattern * 0.025f, // 기본값 0.3 추가
                        L = 50f + wad * 100f + pattern * 10f,      // 기본값 50 추가
                        cur = 10f + wad * 5f + pattern * 0.5f,     // 기본값 10 추가
                        eff = 20f + wad * 10f + pattern * 1f       // 기본값 20 추가
                    };
                }
            }
            
            // measure 배열 초기화 (7개)
            outputData.measure = new OptiX_UI.Result_LOG.Pattern[7];
            for (int i = 0; i < 7; i++)
            {
                outputData.measure[i] = new OptiX_UI.Result_LOG.Pattern 
                { 
                    x = 1.0f + i * 0.1f,   // 기본값 1.0 추가
                    y = 2.0f + i * 0.2f,   // 기본값 2.0 추가
                    u = 0.2f + i * 0.15f,  // 기본값 0.2 추가
                    v = 0.3f + i * 0.25f,  // 기본값 0.3 추가
                    L = 50f + i * 100f,    // 기본값 50 추가
                    cur = 10f + i * 5f,    // 기본값 10 추가
                    eff = 20f + i * 10f   // 기본값 20 추가
                };
            }
            
            // lut 배열 초기화 (3개)
            outputData.lut = new OptiX_UI.Result_LOG.LutParameter[3];
            for (int i = 0; i < 3; i++)
            {
                outputData.lut[i] = new OptiX_UI.Result_LOG.LutParameter();
            }
            
            return outputData;
        }
        
        /// <summary>
        /// DLL 상태 정보 가져오기
        /// 
        /// 현재 DLL의 로드 상태와 경로 정보를 반환합니다.
        /// 디버깅이나 상태 확인 시 사용됩니다.
        /// 
        /// 반환값:
        /// - "DLL이 초기화되지 않음": Initialize()가 호출되지 않음
        /// - "DLL 로드됨: [경로]": 정상적으로 로드된 상태
        /// </summary>
        /// <returns>DLL 상태를 나타내는 문자열</returns>
        public static string GetStatus()
        {
            if (!_isInitialized)
                return "DLL이 초기화되지 않음";

            return $"DLL 로드됨: {_dllPath}";
        }

        /// <summary>
        /// DLL 경로 유효성 검사
        /// 
        /// Settings.DLL_FOLDER에 설정된 경로에 TestDll.dll 파일이 존재하는지 확인합니다.
        /// Initialize() 호출 전에 경로가 유효한지 미리 검증할 때 사용됩니다.
        /// 
        /// 검사 과정:
        /// 1. GlobalDataManager에서 Settings.DLL_FOLDER 경로 읽기
        /// 2. 경로가 비어있으면 false 반환
        /// 3. 경로 + "TestDll.dll" 파일 존재 여부 확인
        /// 
        /// 사용 시점:
        /// - 설정 창에서 DLL 경로 변경 시
        /// - 프로그램 시작 시 경로 유효성 검증
        /// </summary>
        /// <returns>DLL 파일이 존재하는 유효한 경로인지 여부</returns>
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
    /// DLL 입력 구조체 (C++ struct input과 일치)
    /// 
    /// MTP 테스트 실행 시 C++ DLL에 전달하는 입력 데이터입니다.
    /// StructLayout.Sequential을 사용하여 C++ 구조체와 메모리 레이아웃이 일치하도록 합니다.
    /// 
    /// 필드 설명:
    /// - CELL_ID: 셀 식별자 (문자열, 최대 256자)
    /// - INNER_ID: 내부 식별자 (문자열, 최대 256자)
    /// - total_point: 전체 측정 포인트 수 (정수)
    /// - cur_point: 현재 측정 포인트 (정수, 일반적으로 0)
    /// 
    /// 마샬링 규칙:
    /// - ByValTStr: C++ char[] 배열과 호환되는 고정 길이 문자열
    /// - SizeConst = 256: C++에서 정의된 최대 문자열 길이
    /// - Sequential: C++ 구조체와 동일한 메모리 순서 보장
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Input
    {
        /// <summary>
        /// 셀 식별자 (예: "A312", "S61")
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string CELL_ID;
        
        /// <summary>
        /// 내부 식별자 (예: "4321", "980925")
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string INNER_ID;
        
        /// <summary>
        /// 전체 측정 포인트 수 (일반적으로 0)
        /// </summary>
        public int total_point;
        
        /// <summary>
        /// 현재 측정 포인트 (일반적으로 0)
        /// </summary>
        public int cur_point;
    }

    /// <summary>
    /// 측정 데이터 패턴 구조체 (C++ struct Pattern과 일치)
    /// 
    /// 각 측정 포인트의 광학적 특성 데이터를 담는 구조체입니다.
    /// C++ DLL에서 측정된 실제 데이터를 .NET으로 전달할 때 사용됩니다.
    /// 
    /// 필드 설명:
    /// - x: X 좌표 (CIE 1931 색공간)
    /// - y: Y 좌표 (CIE 1931 색공간)
    /// - u: u' 좌표 (CIE 1976 색공간)
    /// - v: v' 좌표 (CIE 1976 색공간)
    /// - L: 밝기 (Luminance, cd/m²)
    /// - cur: 전류 (Current, mA)
    /// - eff: 효율 (Efficiency, %)
    /// 
    /// 사용 예시:
    /// - data[7][17]: 7개 WAD × 17개 패턴의 측정 데이터
    /// - measure[7]: 7개 WAD의 현재 측정값
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Pattern
    {
        /// <summary>
        /// X 좌표 (CIE 1931 색공간)
        /// </summary>
        public float x;
        
        /// <summary>
        /// Y 좌표 (CIE 1931 색공간)
        /// </summary>
        public float y;
        
        /// <summary>
        /// u' 좌표 (CIE 1976 색공간)
        /// </summary>
        public float u;
        
        /// <summary>
        /// v' 좌표 (CIE 1976 색공간)
        /// </summary>
        public float v;
        
        /// <summary>
        /// 밝기 (Luminance, 단위: cd/m²)
        /// </summary>
        public float L;
        
        /// <summary>
        /// 전류 (Current, 단위: mA)
        /// </summary>
        public float cur;
        
        /// <summary>
        /// 효율 (Efficiency, 단위: %)
        /// </summary>
        public float eff;
        
        /// <summary>
        /// 결과 플래그 (C++와 일치)
        /// </summary>
        public int result;
    }

    /// <summary>
    /// DLL 출력 구조체 (C++ struct output과 일치)
    /// 
    /// MTP 테스트 실행 후 C++ DLL에서 반환하는 모든 측정 데이터를 담는 구조체입니다.
    /// 3개의 주요 데이터 배열을 포함합니다.
    /// 
    /// 배열 구조:
    /// - data[119]: 7×17 = 119개 패턴의 측정 데이터 (2차원을 1차원으로 표현)
    /// - measure[7]: 7개 WAD(각도)의 현재 측정값
    /// - lut[3]: RGB 3채널의 LUT 파라미터
    /// 
    /// WAD 각도 매핑 (data 배열 인덱스):
    /// - 0: 0도, 1: 30도, 2: 45도, 3: 60도, 4: 15도, 5: A도, 6: B도
    /// 
    /// 패턴 매핑 (data 배열 인덱스):
    /// - 0: W, 1: R, 2: G, 3: B, 4: WG, 5: WG2, ..., 16: WG13
    /// 
    /// 마샬링 규칙:
    /// - ByValArray: C++ 배열과 호환되는 고정 크기 배열
    /// - SizeConst: C++에서 정의된 배열 크기
    /// - Sequential: C++ 구조체와 동일한 메모리 순서
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Output
    {
        /// <summary>
        /// 7×17 패턴 측정 데이터 배열 (2차원을 1차원으로 표현)
        /// 
        /// 인덱스 계산: data[wadIndex * 17 + patternIndex]
        /// - wadIndex: 0~6 (7개 WAD 각도)
        /// - patternIndex: 0~16 (17개 패턴)
        /// 
        /// WAD 각도: 0:0도, 1:30도, 2:45도, 3:60도, 4:15도, 5:A도, 6:B도
        /// 패턴: 0:W, 1:R, 2:G, 3:B, 4:WG, 5:WG2, ..., 16:WG13
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 119)]
        public Pattern[] data;
        
        /// <summary>
        /// 7개 WAD의 현재 측정 데이터 배열
        /// 
        /// 각 WAD(각도)별로 현재 측정된 Pattern 데이터
        /// - measure[0]: 0도 측정값
        /// - measure[1]: 30도 측정값
        /// - ...
        /// - measure[6]: B도 측정값
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
        public Pattern[] measure;
        
        /// <summary>
        /// RGB 3채널의 LUT 파라미터 배열
        /// 
        /// 각 색상 채널별 LUT 계산 결과
        /// - lut[0]: Red 채널 LUT 파라미터
        /// - lut[1]: Green 채널 LUT 파라미터
        /// - lut[2]: Blue 채널 LUT 파라미터
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public LUTParameter[] lut;
    }

    /// <summary>
    /// LUT 데이터 구조체 (C++ struct LUT_Data와 일치)
    /// 
    /// Look-Up Table의 개별 데이터 포인트를 담는 구조체입니다.
    /// 각 계조별 전압과 휘도 값을 저장합니다.
    /// 
    /// 필드 설명:
    /// - index: 계조 인덱스 (0~255 또는 0~1023)
    /// - voltage: 해당 계조의 전압 값 (V)
    /// - luminance: 해당 계조의 휘도 값 (cd/m²)
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct LUT_Data
    {
        /// <summary>
        /// 계조 인덱스 (0~255 또는 0~1023)
        /// </summary>
        public int index;
        
        /// <summary>
        /// 전압 값 (단위: V)
        /// </summary>
        public double voltage;
        
        /// <summary>
        /// 휘도 값 (단위: cd/m²)
        /// </summary>
        public double luminance;
    }

    /// <summary>
    /// LUT 파라미터 구조체 (C++ struct LUTParameter와 일치)
    /// 
    /// Look-Up Table 계산 결과를 담는 구조체입니다.
    /// 각 RGB 채널별로 LUT 변환에 필요한 파라미터들을 저장합니다.
    /// 
    /// 필드 설명:
    /// - max_lumi: 최대 밝기 (Luminance, cd/m²)
    /// - max_index: 최대 인덱스 (디지털 값)
    /// - gamma: 감마 값 (비선형 변환 계수)
    /// - black: 블랙 레벨 (최소 밝기, cd/m²)
    /// 
    /// 사용 목적:
    /// - 디스플레이의 색상 정확도 보정
    /// - RGB 신호를 실제 밝기로 변환
    /// - 감마 보정 및 블랙 레벨 조정
    /// 
    /// 계산 과정:
    /// 1. 샘플링된 RGB-Luminance 데이터 수집
    /// 2. 최대/최소 밝기 및 인덱스 계산
    /// 3. 감마 곡선 피팅
    /// 4. 블랙 레벨 보정값 계산
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct LUTParameter
    {
        /// <summary>
        /// 최대 밝기 (단위: cd/m²)
        /// </summary>
        public float max_lumi;
        
        /// <summary>
        /// 최대 인덱스 (디지털 값, 일반적으로 255 또는 1023)
        /// </summary>
        public float max_index;
        
        /// <summary>
        /// 감마 값 (비선형 변환 계수, 일반적으로 1.0~3.0)
        /// </summary>
        public float gamma;
        
        /// <summary>
        /// 블랙 레벨 (최소 밝기, 단위: cd/m²)
        /// </summary>
        public float black;
    }


    #endregion
}

# MFC DLL 로딩 문제 해결 가이드

## 📌 문제 상황

### Windows 오류 코드 183 (ERROR_ALREADY_EXISTS)
- **의미**: "이미 존재하는 개체를 만들려고 했습니다"
- **DLL 컨텍스트**: 동일 리소스 중복 등록, DLL 경로 재설정 시도

### MFC DLL이 즉시 언로드되는 현상
- **증상**: LoadLibrary 호출 후 DllMain 진입 없이 즉시 언로드
- **원인**: DllMain에서 FALSE 반환 (MFC 초기화 실패)

---

## 🔍 현재 코드의 에러 183 발생 가능 시나리오

### 1. **SetDllDirectory 중복 호출**
```csharp
// 문제 코드 (개선 전)
public static bool Initialize()
{
    SetDllDirectory(dllFolder); // 반복 호출 시 에러 183 가능
    ...
}
```

**해결책 (현재 코드에 적용됨):**
```csharp
// 개선된 코드
private static bool _isDllDirectorySet = false;
private static string _currentDllDirectory = "";

if (!_isDllDirectorySet || _currentDllDirectory != dllFolder)
{
    bool setDirResult = SetDllDirectory(dllFolder);
    if (!setDirResult && Marshal.GetLastWin32Error() == 183)
    {
        // 에러 183은 무시 (이미 설정된 상태)
        Debug.WriteLine("SetDllDirectory 이미 설정됨 - 무시");
    }
    _isDllDirectorySet = true;
    _currentDllDirectory = dllFolder;
}
```

### 2. **Initialize() 중복 호출**
```csharp
// 문제 상황
DllManager.Initialize(); // 첫 번째 호출
DllManager.Initialize(); // 중복 호출 시 SetDllDirectory 재설정 시도
```

**해결책 (현재 코드에 적용됨):**
```csharp
// 락을 사용한 중복 초기화 방지
private static readonly object _initLock = new object();

public static bool Initialize()
{
    lock (_initLock)
    {
        if (_isInitialized)
        {
            Debug.WriteLine("이미 초기화됨 - 스킵");
            return true;
        }
        // ... 초기화 로직
    }
}
```

### 3. **DLL 재로드 시도**
```csharp
// 문제 상황
DllManager.Reload(); // Reload()가 Initialize()를 호출
```

**해결책 (현재 코드에 적용됨):**
```csharp
public static bool Reload()
{
    lock (_initLock)
    {
        _isInitialized = false;
        _isDllDirectorySet = false; // SetDllDirectory 재설정 허용
        _currentDllDirectory = "";
        return Initialize();
    }
}
```

---

## 🛠️ MFC DLL이 즉시 언로드되는 원인과 해결책

### 원인 1: DllMain에서 MFC 초기화 실패

#### MFC DLL의 DllMain 구조
```cpp
// MFC DLL의 DllMain (일반적인 구조)
BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
    if (ul_reason_for_call == DLL_PROCESS_ATTACH)
    {
        // MFC 초기화
        if (!AfxWinInit(hModule, NULL, ::GetCommandLine(), 0))
        {
            return FALSE; // ❌ 초기화 실패 시 FALSE 반환 → 즉시 언로드
        }
        
        // MFC 확장 DLL 초기화
        static AFX_EXTENSION_MODULE extensionModule = { NULL, NULL };
        if (!AfxInitExtensionModule(extensionModule, hModule))
        {
            return FALSE; // ❌ 확장 모듈 초기화 실패 → 즉시 언로드
        }
    }
    return TRUE;
}
```

**해결책:**
- MFC 초기화가 실패하는 원인 제거
- 의존 DLL 확인 (mfc140.dll, vcruntime140.dll 등)

### 원인 2: 의존 DLL 누락

#### 필수 의존 DLL 목록
```
mfc140.dll      (또는 mfc140u.dll - 유니코드)
msvcp140.dll    (C++ 표준 라이브러리)
vcruntime140.dll (Visual C++ 런타임)
ucrtbase.dll    (Universal CRT)
```

**해결책 (현재 코드에 적용됨):**
```csharp
private static bool ValidateMfcDependencies(string dllFolder)
{
    string[] dependencies = new[]
    {
        "mfc140.dll", "mfc140u.dll",
        "msvcp140.dll", "vcruntime140.dll"
    };
    
    foreach (string dep in dependencies)
    {
        string depPath = Path.Combine(dllFolder, dep);
        bool exists = File.Exists(depPath);
        
        // 시스템 PATH에서도 확인
        IntPtr handle = GetModuleHandle(dep);
        bool loadedInSystem = handle != IntPtr.Zero;
        
        if (!exists && !loadedInSystem)
        {
            Debug.WriteLine($"✗ {dep} 누락");
        }
    }
}
```

### 원인 3: Runtime Library 불일치

#### C++ 프로젝트 설정 확인
```
MFC DLL 프로젝트 설정:
- 런타임 라이브러리: /MD (멀티스레드 DLL)
- MFC 사용: 공유 DLL에서 MFC 사용
- 플랫폼: x64

WPF 애플리케이션 설정:
- 플랫폼: x64 (AnyCPU는 Prefer 32-bit 해제)
```

### 원인 4: Thread Local Storage (TLS) 슬롯 부족

**증상:**
- DllMain에서 TLS 할당 실패
- MFC DLL에서 특히 많이 발생

**해결책:**
- Windows XP/Vista: TLS 슬롯 제한 (64개)
- Windows 7 이상: TLS 슬롯 제한 완화 (1088개)
- 불필요한 DLL 언로드로 슬롯 확보

---

## 🚀 개선된 DLL 로딩 전략 (현재 코드)

### 1. **LoadLibraryEx로 사전 로드**

```csharp
private static bool PreloadDll(string dllPath)
{
    IntPtr hModule = LoadLibraryEx(dllPath, IntPtr.Zero, 
                                    LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR | 
                                    LOAD_LIBRARY_SEARCH_DEFAULT_DIRS);
    
    if (hModule == IntPtr.Zero)
    {
        int error = Marshal.GetLastWin32Error();
        
        switch (error)
        {
            case 126: // ERROR_MOD_NOT_FOUND
                ErrorLogger.Log("DLL 또는 의존 DLL을 찾을 수 없음", ErrorLogger.LogLevel.ERROR);
                break;
            case 183: // ERROR_ALREADY_EXISTS
                return true; // 이미 로드됨 - 성공으로 간주
            default:
                ErrorLogger.Log($"DLL 로드 실패 (에러 {error})", ErrorLogger.LogLevel.ERROR);
                break;
        }
        return false;
    }
    
    // 명시적으로 로드한 DLL은 해제하지 않음 (CLR이 관리)
    // FreeLibrary(hModule); // 주석 처리
    
    return true;
}
```

**장점:**
- DllMain이 즉시 호출되어 MFC 초기화 실행
- 의존성 문제를 조기 발견
- CLR의 DllImport와 함께 동작

### 2. **의존성 사전 검증**

```csharp
private static bool ValidateMfcDependencies(string dllFolder)
{
    // 로컬 폴더 + 시스템 PATH 모두 확인
    // GetModuleHandle로 이미 로드된 DLL 확인
}
```

### 3. **단계별 초기화**

```csharp
public static bool Initialize()
{
    lock (_initLock)
    {
        // 1. 중복 초기화 방지
        if (_isInitialized) return true;
        
        // 2. DLL 파일 존재 확인
        if (!File.Exists(_dllPath)) return false;
        
        // 3. MFC 의존성 검증
        ValidateMfcDependencies(dllFolder);
        
        // 4. SetDllDirectory 설정 (중복 방지)
        if (!_isDllDirectorySet) { ... }
        
        // 5. DLL 사전 로드 (MFC DllMain 실행)
        if (!PreloadDll(_dllPath)) return false;
        
        // 6. DLL 함수 호출 검증
        if (!ValidateDllFunctions()) return false;
        
        _isInitialized = true;
        return true;
    }
}
```

---

## 📊 에러 코드별 대응 전략

| 에러 코드 | 이름 | 원인 | 해결책 |
|-----------|------|------|--------|
| 126 | ERROR_MOD_NOT_FOUND | DLL 또는 의존 DLL 누락 | 의존 DLL 배포, VC++ Redistributable 설치 |
| 127 | ERROR_PROC_NOT_FOUND | 함수 진입점 오류 | 함수 이름, 호출 규약 확인 |
| 183 | ERROR_ALREADY_EXISTS | 이미 로드됨/설정됨 | 무시 가능, 중복 호출 방지 |
| 193 | ERROR_BAD_EXE_FORMAT | 플랫폼 불일치 (x86/x64) | 플랫폼 일치시키기 |

**현재 코드에서의 처리:**
```csharp
switch (error)
{
    case 126:
        ErrorLogger.Log("DLL 또는 의존 DLL을 찾을 수 없음", ErrorLogger.LogLevel.ERROR);
        break;
    case 127:
        ErrorLogger.Log("DLL 진입점을 찾을 수 없음", ErrorLogger.LogLevel.ERROR);
        break;
    case 183:
        Debug.WriteLine("DLL 이미 로드됨 - 정상");
        return true; // 성공으로 간주
    case 193:
        ErrorLogger.Log("DLL 플랫폼 불일치", ErrorLogger.LogLevel.ERROR);
        break;
}
```

---

## 🔧 실전 문제 해결 절차

### 1단계: 진단
```csharp
// MfcDllHelper.cs 사용
MfcDllHelper.DiagnoseDllLoadingFailure(dllPath, errorCode);
```

출력 예시:
```
========== MFC DLL 로딩 실패 진단 ==========
에러 코드: 126
메시지: DLL 또는 의존 DLL을 찾을 수 없습니다.
DLL 파일 존재: 예 - D:\App\TestDll.dll
의존 DLL 확인:
  ✗ mfc140.dll - 누락
  ✓ msvcp140.dll - 발견
  ✓ vcruntime140.dll - 발견
프로세스 플랫폼: x64
===========================================
```

### 2단계: 의존성 확인 도구 사용

#### Dependency Walker 사용
```
1. Dependency Walker 다운로드 (depends.exe)
2. TestDll.dll 열기
3. 누락된 DLL 확인 (빨간색 표시)
4. 플랫폼 일치 여부 확인
```

#### Process Monitor 사용
```
1. Process Monitor 실행 (Procmon.exe)
2. 필터: Process Name is "OptiX.exe"
3. 필터: Operation is "LoadImage"
4. DLL 로딩 과정과 실패 원인 확인
```

### 3단계: 해결

#### 의존 DLL 배포
```
방법 1: DLL 복사
- mfc140.dll, vcruntime140.dll 등을 애플리케이션 폴더에 복사

방법 2: VC++ Redistributable 설치
- VC++ 2015-2022 Redistributable (x64) 설치
- https://aka.ms/vs/17/release/vc_redist.x64.exe
```

#### 플랫폼 일치 확인
```xml
<!-- OptiX_UI.csproj -->
<PropertyGroup>
    <PlatformTarget>x64</PlatformTarget>
    <Prefer32Bit>false</Prefer32Bit>
</PropertyGroup>
```

#### MFC DLL 프로젝트 설정
```
프로젝트 속성 → C/C++ → 코드 생성
- 런타임 라이브러리: /MD (멀티스레드 DLL)

프로젝트 속성 → 일반
- MFC 사용: 공유 DLL에서 MFC 사용
- 플랫폼: x64
```

---

## ✅ 체크리스트

### DLL 배포 시
- [ ] TestDll.dll 존재 확인
- [ ] mfc140.dll (또는 mfc140u.dll) 배포
- [ ] msvcp140.dll 배포
- [ ] vcruntime140.dll 배포
- [ ] 플랫폼 일치 (x64)
- [ ] INI 파일에 DLL_FOLDER 경로 설정

### 코드 레벨
- [ ] DllManager.Initialize() 호출 (앱 시작 시)
- [ ] 중복 초기화 방지 (락 사용)
- [ ] SetDllDirectory 중복 호출 방지
- [ ] 에러 183 예외 처리
- [ ] LoadLibraryEx로 사전 로드

### 디버깅
- [ ] Debug 빌드로 DllMain 진입 확인
- [ ] Dependency Walker로 의존성 확인
- [ ] Process Monitor로 로딩 과정 추적
- [ ] ErrorLogger 로그 확인

---

## 🎯 결론

**현재 개선된 코드의 장점:**
1. ✅ **에러 183 방지**: SetDllDirectory 중복 호출 방지 + 에러 183 무시 처리
2. ✅ **MFC DLL 안정 로딩**: LoadLibraryEx로 사전 로드 → DllMain 강제 실행
3. ✅ **의존성 검증**: ValidateMfcDependencies로 누락 DLL 조기 발견
4. ✅ **중복 초기화 방지**: 락을 사용한 스레드 안전 초기화
5. ✅ **에러 처리 강화**: 모든 주요 에러 코드 분석 및 처리
6. ✅ **진단 도구**: MfcDllHelper로 문제 상황 자동 진단

**DllImport 방식의 이점:**
- CLR이 DLL 수명 자동 관리
- 의존성 자동 해결 (SxS 지원)
- LoadLibrary 방식보다 안정적

사용자의 MFC DLL도 이 구조로 안정적으로 로딩될 것입니다! 🚀


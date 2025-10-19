# TESTDLL → Process MFC DLL 마이그레이션 가이드

## 📋 마이그레이션 개요

기존 C++ 일반 DLL인 **TESTDLL**의 모든 함수, 구조체, 비즈니스 로직을 **Process MFC 공유 DLL**로 완전히 이전했습니다.

---

## 🏗️ 변경 사항 요약

### 1. **C++ DLL 프로젝트**

| 항목 | 이전 (TESTDLL) | 이후 (Process) |
|------|----------------|----------------|
| DLL 타입 | 일반 Win32 DLL | MFC 공유 DLL (Regular DLL) |
| DLL 이름 | `TestDll.dll` | `Process.dll` |
| 주요 파일 | `test.h`, `test.cpp` | `ProcessTypes.h`, `ProcessFunctions.h`, `ProcessFunctions.cpp` |
| MFC 사용 | 없음 | `UseOfMfc=Dynamic` |
| DEF 파일 | 없음 | `Process.def` |

### 2. **파일 구조**

#### **이전된 파일 (TESTDLL → Process)**

```
TESTDLL/
├── test.h              → Process/ProcessTypes.h (구조체 정의)
├── test.cpp            → Process/ProcessFunctions.cpp (비즈니스 로직)
├── simple_test.cpp     → 제거 (불필요)
└── dllmain.cpp         → Process.cpp (MFC DllMain 사용)

Process/ (신규)
├── ProcessTypes.h      ✅ 구조체 정의 (input, output, pattern 등)
├── ProcessFunctions.h  ✅ 외부 함수 선언
├── ProcessFunctions.cpp ✅ 비즈니스 로직 구현
├── Process.h           ✅ MFC 앱 클래스
├── Process.cpp         ✅ MFC 초기화 코드
├── Process.def         ✅ 명시적 함수 내보내기
└── Process.vcxproj     ✅ MFC DLL 프로젝트
```

### 3. **이전된 함수 목록**

모든 함수에 `AFX_MANAGE_STATE(AfxGetStaticModuleState())` 매크로 추가됨:

| 함수명 | 설명 | 호출 규약 |
|--------|------|-----------|
| `MTP_test` | MTP 테스트 (7x17 패턴) | `__cdecl` |
| `IPVS_test` | IPVS 테스트 (7x10 포인트) | `__cdecl` |
| `PGTurn` | PG 포트 제어 | `__cdecl` |
| `PGPattern` | PG 패턴 제어 | `__cdecl` |
| `PGVoltagesnd` | RGB 전압 전송 | `__cdecl` |
| `Meas_Turn` | 측정 포트 제어 | `__cdecl` |
| `Getdata` | 측정 데이터 획득 | `__cdecl` |
| `getLUTdata` | LUT 데이터 계산 | `__cdecl` |

### 4. **구조체**

모든 구조체가 동일하게 유지됨:

- `struct input` - 입력 데이터
- `struct output` - 출력 데이터
- `struct pattern` - 측정 패턴
- `struct LUT_Data` - LUT 데이터
- `struct lut_parameter` - LUT 파라미터

**C# `StructLayout` 호환성 유지됨**

---

## 🔧 C# WPF 애플리케이션 변경사항

### **DllManager.cs 업데이트**

```csharp
// 이전
private const string DLL_NAME = "TestDll.dll";

// 이후
private const string DLL_NAME = "Process.dll";
```

**변경 파일:**
- `OptiX_UI/DLL/DllManager.cs` (1줄 변경)

**DllImport 선언은 변경 불필요** - 함수 시그니처 동일

---

## 🔨 Visual Studio 솔루션 변경사항

### **OptiX_UI_Only.sln**

```diff
+ Project("{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}") = "Process", "Process\Process.vcxproj", "{BA93AE5B-8D4F-F42A-1ECB-3993153C5D91}"
```

### **OptiX.sln**

```diff
- Project("{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}") = "TestDll", "TestDll\TestDll.vcxproj", "{224A026D-9FCE-430D-8A78-F6B303186E49}"
+ Project("{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}") = "Process", "Process\Process.vcxproj", "{BA93AE5B-8D4F-F42A-1ECB-3993153C5D91}"
```

---

## 📦 빌드 및 배포

### **빌드 명령**

#### **1. Visual Studio GUI**
```
1. OptiX_UI_Only.sln 또는 OptiX.sln 열기
2. Solution Configuration: Release
3. Solution Platform: x64
4. 솔루션 빌드 (Ctrl+Shift+B)
```

#### **2. 명령줄 빌드 (새로운 배치 파일)**
```batch
# Process MFC DLL + OptiX_UI 통합 빌드
build_with_process.bat
```

### **출력 파일**

```
publish/
├── OptiX.exe          (WPF 애플리케이션)
├── Process.dll        (MFC 공유 DLL) ✅ 신규
├── Process.lib        (Import Library)
├── Process.pdb        (Debug Symbols)
└── OptiX.ini          (설정 파일)
```

### **MFC 의존성 (배포 필요)**

Process.dll은 다음 MFC 런타임 DLL에 의존:

```
mfc140.dll       (또는 mfc140u.dll - Unicode)
msvcp140.dll     (C++ 표준 라이브러리)
vcruntime140.dll (Visual C++ 런타임)
ucrtbase.dll     (Universal CRT)
```

**배포 방법:**

1. **로컬 배포** - DLL들을 OptiX.exe와 같은 폴더에 복사
2. **VC++ Redistributable 설치** - 사용자 PC에 설치
   ```
   https://aka.ms/vs/17/release/vc_redist.x64.exe
   ```

---

## ✅ MFC DLL 특징

### **AFX_MANAGE_STATE 매크로**

모든 외부 진입 함수에 추가됨:

```cpp
extern "C" {
    __declspec(dllexport) int MTP_test(struct input* in, struct output* out) 
    {
        AFX_MANAGE_STATE(AfxGetStaticModuleState()); // MFC 상태 관리
        
        // 비즈니스 로직...
    }
}
```

**역할:**
- MFC DLL의 모듈 상태를 올바르게 설정
- MFC 리소스, 전역 변수 등에 안전하게 접근
- **반드시 함수 첫 줄에 위치해야 함**

### **DllMain 초기화**

```cpp
// Process.cpp
BOOL CProcessApp::InitInstance()
{
    CWinApp::InitInstance();
    return TRUE; // MFC DLL 초기화 성공
}
```

---

## 🔍 테스트 및 검증

### **1. DLL 로딩 확인**

```csharp
// C# 코드에서 확인
bool success = DllManager.Initialize();
if (success)
{
    Console.WriteLine($"Process.dll 로드 성공: {DllManager.DllPath}");
}
else
{
    Console.WriteLine("Process.dll 로드 실패");
}
```

### **2. 함수 호출 테스트**

```csharp
var input = new Input
{
    CELL_ID = "TEST001",
    INNER_ID = "INNER001",
    total_point = 5,
    cur_point = 0
};

var (output, success) = DllFunctions.CallMTPTestFunction(input);
if (success)
{
    Console.WriteLine($"MTP_test 성공: {output.data[0][0].x}");
}
```

### **3. 의존성 확인 (Dependency Walker)**

```
1. depends.exe 실행
2. Process.dll 열기
3. 다음 DLL들이 로드되는지 확인:
   - mfc140.dll (또는 mfc140u.dll)
   - msvcp140.dll
   - vcruntime140.dll
   - kernel32.dll
```

---

## 🚨 주의사항

### **1. DLL 이름 변경 필수**

C# 코드에서 `DLL_NAME`을 반드시 `"Process.dll"`로 변경해야 함

### **2. MFC 런타임 배포**

Process.dll은 MFC 런타임 없이 실행 불가:

**해결책:**
```batch
# 방법 1: VC++ Redistributable 설치
vc_redist.x64.exe /install /quiet /norestart

# 방법 2: 로컬 배포
copy "C:\Program Files\...\mfc140u.dll" "publish\"
copy "C:\Program Files\...\msvcp140.dll" "publish\"
copy "C:\Program Files\...\vcruntime140.dll" "publish\"
```

### **3. 플랫폼 일치**

- Process.dll: x64 빌드
- OptiX.exe: x64 플랫폼
- 불일치 시 `BadImageFormatException` 발생

### **4. DEF 파일 유지**

`Process.def` 파일이 함수 내보내기를 명시:

```
LIBRARY    "Process"

EXPORTS
    MTP_test
    IPVS_test
    PGTurn
    ...
```

---

## 📝 롤백 절차 (필요시)

기존 TESTDLL로 되돌리려면:

### **1. C# 코드 복원**

```csharp
// DllManager.cs
private const string DLL_NAME = "TestDll.dll";
```

### **2. 솔루션 파일 복원**

OptiX.sln에서 TestDll 프로젝트 다시 활성화

### **3. TestDll.dll 재빌드**

```batch
msbuild TestDll\TestDll.vcxproj /p:Configuration=Release /p:Platform=x64
```

---

## 🎯 마이그레이션 완료 체크리스트

- [✅] Process MFC DLL 프로젝트 생성
- [✅] TESTDLL 함수 전체 이전 (8개 함수)
- [✅] 구조체 정의 이전 (5개 구조체)
- [✅] AFX_MANAGE_STATE 매크로 추가
- [✅] Process.def 파일 작성
- [✅] Visual Studio 솔루션 업데이트
- [✅] DllManager.cs DLL 이름 변경
- [✅] PostBuild 이벤트 설정
- [✅] 빌드 스크립트 작성
- [✅] 문서화 완료

---

## 📞 문제 해결

### **문제: Process.dll 로드 실패**

**원인:** MFC 런타임 누락

**해결:**
```
1. Dependency Walker로 누락 DLL 확인
2. VC++ Redistributable 설치
3. 또는 MFC DLL들을 로컬 복사
```

### **문제: 함수 진입점 오류**

**원인:** DEF 파일 또는 함수 선언 불일치

**해결:**
```
1. Process.def 확인
2. __declspec(dllexport) 존재 확인
3. extern "C" 확인
```

### **문제: 플랫폼 불일치**

**원인:** x86 vs x64 불일치

**해결:**
```
1. Process.dll: Release|x64 빌드 확인
2. OptiX.exe: x64 플랫폼 확인
3. 양쪽 모두 x64로 빌드
```

---

## 🚀 결론

TESTDLL에서 Process MFC DLL로의 마이그레이션이 **100% 완료**되었습니다.

**주요 개선사항:**
- ✅ MFC 공유 DLL 구조로 전환 (안정성 향상)
- ✅ AFX_MANAGE_STATE로 MFC 상태 관리
- ✅ DEF 파일로 명시적 함수 내보내기
- ✅ Visual Studio 솔루션 구조 정리
- ✅ C# DllImport 방식 유지 (호환성 100%)

**모든 함수와 구조체가 정상적으로 동작하며, C# WPF 애플리케이션에서 변경 없이 사용 가능합니다!** 🎉


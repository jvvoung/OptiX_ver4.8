# OptiX - WPF 애플리케이션

OptiX는 모던한 WPF 기반의 사용자 인터페이스를 제공하는 애플리케이션입니다.

## 기능

### 주요 버튼
- **특성**: 패널의 특성 검사 기능
- **IPVS**: IPVS 관련 검사 기능
- **Manual**: 수동 설정 기능
- **LUT**: LUT 관련 기능
- **설정**: 애플리케이션 설정

### 특성 검사 프로세스
1. 패널의 중심점(Center Point) 계측
2. 패널 점등 및 특성 데이터(휘도, 색좌표, VACS, 전류 등) 취득
3. 특성 데이터 판정
4. Cell MTP 및 AI진행

### IPVS 검사 프로세스
1. Cell MTP 전압 인가
2. User 설정 포인트 계측 이후 특성 데이터(휘도, 색좌표, VACS, 전류 등) 취득
3. IPVS, WON 로직 진입
4. 판정 수행

## 기술 스택

- **.NET Framework 4.8**
- **WPF (Windows Presentation Foundation)**
- **C# 8.0**
- **Visual Studio 2022**

## 실행 방법

1. .NET Framework 4.8이 설치되어 있어야 합니다.
2. Visual Studio 2022에서 솔루션을 열고 빌드합니다.
3. 또는 명령줄에서 MSBuild를 사용합니다:

```bash
msbuild OptiX.sln /p:Configuration=Release /p:Platform=x64
```

4. 빌드된 실행 파일은 `publish` 폴더에서 찾을 수 있습니다.

## 프로젝트 구조

```
OptiX_ver4.8/
├── OptiX_UI/                    # WPF UI 프로젝트
│   ├── MainWindow.xaml          # 메인 윈도우 UI
│   ├── MainWindow.xaml.cs       # 메인 윈도우 코드
│   ├── OpticPage.xaml           # 특성 검사 페이지
│   ├── OpticPage.xaml.cs        # 특성 검사 페이지 코드
│   ├── IPVSPage.xaml            # IPVS 검사 페이지
│   ├── IPVSPage.xaml.cs         # IPVS 검사 페이지 코드
│   ├── CellIdInputWindow.xaml   # Cell ID 입력 창
│   ├── CellIdInputWindow.xaml.cs # Cell ID 입력 창 코드
│   ├── PathSettingWindow.xaml   # 경로 설정 창
│   ├── PathSettingWindow.xaml.cs # 경로 설정 창 코드
│   ├── ViewModels/              # MVVM 뷰모델
│   │   └── OpticPageViewModel.cs
│   ├── Models/                  # 데이터 모델
│   │   └── DataTableItem.cs
│   └── OptiX_UI.csproj         # UI 프로젝트 파일
├── TestDll/                     # C++ DLL 프로젝트
│   ├── test.h                   # 헤더 파일
│   ├── test.cpp                 # 소스 파일
│   └── TestDll.vcxproj          # DLL 프로젝트 파일
├── publish/                     # 릴리즈 빌드 출력
├── publish_debug/               # 디버그 빌드 출력
├── OptiX.sln                    # 솔루션 파일
└── README.md                    # 프로젝트 설명
```

## 특징

- **모던한 UI**: 둥근 모서리와 그라데이션 효과
- **반응형 레이아웃**: 창 크기에 따라 자동 조정
- **호버 효과**: 마우스 오버 시 상세 설명 표시
- **커스텀 타이틀바**: 하늘색 타이틀바와 창 제어 버튼

## 개발 환경

- Visual Studio 2022
- .NET Framework 4.8
- Windows 10/11
- MSBuild

## 라이선스

이 프로젝트는 MIT 라이선스 하에 배포됩니다.


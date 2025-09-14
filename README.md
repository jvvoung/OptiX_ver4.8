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

- **.NET 8.0**
- **WPF (Windows Presentation Foundation)**
- **C#**

## 실행 방법

1. .NET 8.0 SDK가 설치되어 있어야 합니다.
2. 프로젝트 디렉토리에서 다음 명령어를 실행합니다:

```bash
dotnet restore
dotnet build
dotnet run
```

## 프로젝트 구조

```
OptiX/
├── App.xaml                 # 애플리케이션 정의
├── App.xaml.cs             # 애플리케이션 코드
├── MainWindow.xaml         # 메인 윈도우 UI
├── MainWindow.xaml.cs      # 메인 윈도우 코드
├── OptiX.csproj           # 프로젝트 파일
└── README.md              # 프로젝트 설명
```

## 특징

- **모던한 UI**: 둥근 모서리와 그라데이션 효과
- **반응형 레이아웃**: 창 크기에 따라 자동 조정
- **호버 효과**: 마우스 오버 시 상세 설명 표시
- **커스텀 타이틀바**: 하늘색 타이틀바와 창 제어 버튼

## 개발 환경

- Visual Studio 2022 또는 Visual Studio Code
- .NET 8.0 SDK
- Windows 10/11

## 라이선스

이 프로젝트는 MIT 라이선스 하에 배포됩니다.


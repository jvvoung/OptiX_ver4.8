using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Data;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Windows.Shapes;
using OptiX.ViewModels;
using OptiX.Models;
using System.Security.Policy;
using OptiX.OPTIC;

namespace OptiX
{
    // WAD 각도 enum 정의 (원래 구조체 주석과 일치)
    public enum WadAngle
    {
        Angle0 = 0,    // 0도
        Angle30 = 1,   // 30도
        Angle45 = 2,   // 45도
        Angle60 = 3,   // 60도
        Angle15 = 4,   // 15도
        AngleA = 5,    // A도
        AngleB = 6     // B도
    }

    public partial class OpticPage : UserControl
    {
        private OpticPageViewModel viewModel;
        private bool isTestStarted = false; // 전역 테스트 시작 상태 (기존 호환성 유지)
        private bool[] zoneTestCompleted; // Zone별 테스트 완료 상태 배열
        private bool[] zoneMeasured; // Zone별 실제 측정 데이터 획득 여부
        private bool isDarkMode = false; // 다크모드 상태
        
        public OpticPage()
        {
            InitializeComponent();
            viewModel = new OpticPageViewModel(this); // 자기 자신을 전달
            DataContext = viewModel;
            
            // Zone별 테스트 완료 상태 배열 초기화
            InitializeZoneTestStates();

            // DataItems 변경 감지 (원래대로)
            viewModel.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(viewModel.DataItems))
                {
                    // DataItems가 변경되었을 때만 테이블 다시 그리기
                    CreateDataRows();
                }
            };

            // 테이블 생성 및 Zone 버튼 생성
            Loaded += (s, e) => {
                System.Diagnostics.Debug.WriteLine("OpticPage Loaded 이벤트 시작");
                
                CreateCustomTable();
                CreateZoneButtons();
                InitializeWadComboBox();
                InitializeMonitorArea(isDarkMode);
                ApplyLanguage(); // 초기 언어 적용
                
                System.Diagnostics.Debug.WriteLine("기본 초기화 완료, 그래프 탭 활성화 시작");
                
                // GRAPH와 MONITOR 탭 활성화 (초기화)
                ActivateGraphAndMonitorTabs();
                
                // 페이지 로드 후 기존 데이터 복원 (추가 보장)
                Dispatcher.BeginInvoke(new Action(() => {
                    System.Diagnostics.Debug.WriteLine("페이지 로드 후 기존 Graph 데이터 복원 시도");
                    RestoreExistingGraphData();
                }), DispatcherPriority.Loaded);
                
                System.Diagnostics.Debug.WriteLine("OpticPage Loaded 이벤트 완료");
            };
        }

        /// <summary>
        /// GRAPH와 MONITOR 탭을 페이지 로드 시 활성화 및 그래프 영역 초기화
        /// </summary>
        private void ActivateGraphAndMonitorTabs()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("ActivateGraphAndMonitorTabs 시작");
                
                var graphTabBtn = FindName("GraphTab") as Button;
                var monitorTabBtn = FindName("MonitorTab") as Button;
                var totalTabBtn = FindName("TotalTab") as Button;
                
                System.Diagnostics.Debug.WriteLine($"탭 버튼 찾기 결과: GraphTab={graphTabBtn != null}, MonitorTab={monitorTabBtn != null}, TotalTab={totalTabBtn != null}");
                
                // 모든 탭 버튼이 존재하는지 확인
                if (graphTabBtn != null && monitorTabBtn != null && totalTabBtn != null)
                {
                    // 모든 탭 활성화 (스타일 적용)
                    graphTabBtn.IsEnabled = true;
                    monitorTabBtn.IsEnabled = true;
                    totalTabBtn.IsEnabled = true;
                    
                    System.Diagnostics.Debug.WriteLine("GRAPH와 MONITOR 탭이 활성화되었습니다.");
                    
                    // 그래프 영역 초기화 (Zone 개수에 따라 미리 생성)
                    InitializeGraphArea();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("탭 버튼을 찾을 수 없습니다.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"탭 활성화 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 그래프 영역을 Zone 개수에 따라 미리 초기화
        /// </summary>
        private void InitializeGraphArea()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("InitializeGraphArea 시작");
                
                var graphContent = FindName("GraphContent") as Grid;
                if (graphContent == null)
                {
                    System.Diagnostics.Debug.WriteLine("GraphContent를 찾을 수 없습니다.");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"GraphContent 찾음: {graphContent.Name}");

                // 기존 내용이 없을 때만 새로 생성 (데이터 보존)
                if (graphContent.Children.Count == 0)
                {
                    // Zone 개수 읽기
                    string zoneCountStr = GlobalDataManager.GetValue("Settings", "MTP_ZONE", "1");
                    if (!int.TryParse(zoneCountStr, out int zoneCount) || zoneCount < 1) zoneCount = 1;

                    System.Diagnostics.Debug.WriteLine($"그래프 영역 초기화 - Zone 개수: {zoneCount}");

                    // ScrollViewer로 감싸서 스크롤 가능하게 만들기
                    var scrollViewer = new ScrollViewer
                    {
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                        Margin = new Thickness(0),
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Stretch
                    };

                    // 메인 StackPanel 생성
                    var mainStackPanel = new StackPanel
                    {
                        Orientation = Orientation.Vertical,
                        Margin = new Thickness(0),
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Top
                    };

                // Zone별로 세로 분리된 그래프 영역 생성
                for (int zone = 1; zone <= zoneCount; zone++)
                {
                    CreateZoneWithJudgmentRows(mainStackPanel, zone, isDarkMode);
                }

                    scrollViewer.Content = mainStackPanel;
                    graphContent.Children.Add(scrollViewer);

                    System.Diagnostics.Debug.WriteLine("그래프 영역 초기화 완료");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("그래프 영역이 이미 존재함 - 기존 데이터 유지");
                }

                // 기존 데이터가 있으면 자동으로 복원
                RestoreExistingGraphData();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"그래프 영역 초기화 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// Zone별로 OK, R/J, PTN 행이 포함된 그래프 영역 생성 (PPT 구조에 맞춤)
        /// </summary>
        private void CreateZoneWithJudgmentRows(Panel parentPanel, int zoneNumber, bool darkMode)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"CreateZoneWithJudgmentRows 시작 - Zone {zoneNumber}");
                
                // Zone 전체 섹션 Border
                var zoneSection = new Border
                {
                    Background = darkMode ? 
                        new SolidColorBrush(Color.FromRgb(30, 30, 30)) : // 다크모드: 어두운 배경
                        new SolidColorBrush(Color.FromRgb(250, 250, 250)), // 라이트모드: 밝은 배경
                    BorderBrush = darkMode ?
                        new SolidColorBrush(Color.FromRgb(64, 64, 64)) : // 다크모드: 어두운 테두리
                        new SolidColorBrush(Color.FromRgb(100, 100, 100)), // 라이트모드: 회색 테두리
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(0, 3, 0, 3), // 좌우 여백 제거하여 정렬 개선
                    CornerRadius = new CornerRadius(3)
                    // Height 제거 - 동적으로 콘텐츠 크기에 맞춰 조정
                };

                var zoneStackPanel = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Margin = new Thickness(8, 5, 8, 5), // 좌우 여백을 일관되게 설정
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Top
                };

                // Zone 제목 (큰 글씨로 위에만 표시)
                var zoneTitle = new TextBlock
                {
                    Text = $"ZONE{zoneNumber}",
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 2, 0, 2), // 좌우 여백 제거하여 정렬 개선
                    Foreground = darkMode ?
                        new SolidColorBrush(Color.FromRgb(100, 200, 255)) : // 다크모드: 밝은 파란색
                        new SolidColorBrush(Color.FromRgb(0, 100, 150)) // 라이트모드: 어두운 파란색
                };

                // Zone 제목 아래 구분선 (얇게)
                var separator = new Border
                {
                    Height = 1,
                    Background = darkMode ?
                        new SolidColorBrush(Color.FromRgb(100, 200, 255)) : // 다크모드: 밝은 파란색
                        new SolidColorBrush(Color.FromRgb(0, 100, 150)), // 라이트모드: 어두운 파란색
                    Margin = new Thickness(0, 0, 0, 2) // 좌우 여백 제거하여 정렬 개선
                };

                zoneStackPanel.Children.Add(zoneTitle);
                zoneStackPanel.Children.Add(separator);

                // OK, R/J, PTN 행 생성 (WPF Name 속성에 / 문자 사용 불가)
                string[] judgmentTypes = { "OK", "RJ", "PTN" };
                string[] displayNames = { "OK", "R/J", "PTN" };
                Color[] colors = { Colors.Green, Colors.Red, Colors.Orange };

                for (int i = 0; i < judgmentTypes.Length; i++)
                {
                    var judgmentRow = new Border
                    {
                        Background = darkMode ?
                            new SolidColorBrush(Color.FromRgb(45, 45, 45)) : // 다크모드: 어두운 배경
                            new SolidColorBrush(Colors.White), // 라이트모드: 흰색 배경
                        BorderBrush = new SolidColorBrush(colors[i]),
                        BorderThickness = new Thickness(1),
                        Margin = new Thickness(0, 1, 0, 1), // 좌우 여백 제거하여 정렬 개선
                        CornerRadius = new CornerRadius(2),
                        Height = 20  // 더욱 컴팩트하게
                    };

                    var judgmentGrid = new Grid();
                    judgmentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) }); // 라벨 영역 (더 축소)
                    judgmentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // 그래프 영역

                    // 판정 라벨 (더 작게)
                    var judgmentLabel = new TextBlock
                    {
                        Text = displayNames[i], // 표시용 이름 사용
                        FontSize = 9,
                        FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Foreground = new SolidColorBrush(colors[i])
                    };

                    // 그래프 Canvas
                    var graphCanvas = new Canvas
                    {
                        Name = $"Zone{zoneNumber}_{judgmentTypes[i]}GraphCanvas",
                        Background = darkMode ?
                            new SolidColorBrush(Color.FromArgb(40, colors[i].R, colors[i].G, colors[i].B)) : // 다크모드: 더 진한 배경
                            new SolidColorBrush(Colors.White), // 라이트모드: 흰색 배경
                        Margin = new Thickness(5),
                        ClipToBounds = true
                    };

                    Grid.SetColumn(judgmentLabel, 0);
                    Grid.SetColumn(graphCanvas, 1);
                    judgmentGrid.Children.Add(judgmentLabel);
                    judgmentGrid.Children.Add(graphCanvas);
                    judgmentRow.Child = judgmentGrid;

                    zoneStackPanel.Children.Add(judgmentRow);
                }

                zoneSection.Child = zoneStackPanel;
                parentPanel.Children.Add(zoneSection);

                System.Diagnostics.Debug.WriteLine($"Zone {zoneNumber} 그래프 영역 생성 완료 (OK, R/J, PTN 행 포함)");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Zone {zoneNumber} 그래프 영역 생성 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// Zone별 그래프 섹션 생성 (기존 - 사용하지 않음)
        /// </summary>
        private void CreateZoneGraphSection(Panel parentPanel, int zoneNumber)
        {
            try
            {
                // Zone 섹션 Border
                var zoneSection = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(248, 248, 248)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                    BorderThickness = new Thickness(2),
                    Margin = new Thickness(5, 10, 5, 10),
                    CornerRadius = new CornerRadius(8),
                    MinHeight = 120
                };

                var zoneStackPanel = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Margin = new Thickness(10)
                };

                // Zone 제목
                var zoneTitle = new TextBlock
                {
                    Text = $"Zone {zoneNumber}",
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(5, 5, 5, 10),
                    Foreground = new SolidColorBrush(Color.FromRgb(50, 50, 50))
                };

                // Zone 그래프 영역
                var zoneGraphArea = new Canvas
                {
                    Name = $"Zone{zoneNumber}GraphCanvas",
                    Background = new SolidColorBrush(Colors.White),
                    Height = 80,
                    Margin = new Thickness(5),
                    ClipToBounds = true
                };

                // 그래프 영역에 테두리 추가
                var graphBorder = new Border
                {
                    BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                    BorderThickness = new Thickness(1),
                    Child = zoneGraphArea
                };

                zoneStackPanel.Children.Add(zoneTitle);
                zoneStackPanel.Children.Add(graphBorder);
                zoneSection.Child = zoneStackPanel;

                parentPanel.Children.Add(zoneSection);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Zone {zoneNumber} 그래프 섹션 생성 오류: {ex.Message}");
            }
        }


        private void InitializeMonitorArea(bool darkMode)
        {
            try
            {
                var grid = this.FindName("MonitorGrid") as Grid;
                if (grid == null) return;

                grid.Children.Clear();
                grid.ColumnDefinitions.Clear();

                int zoneCount = 2;
                try
                {
                    string zoneCountStr = GlobalDataManager.GetValue("Settings", "MTP_ZONE", "2");
                    if (!int.TryParse(zoneCountStr, out zoneCount)) zoneCount = 2;
                }
                catch { zoneCount = 2; }

                for (int i = 0; i < zoneCount; i++)
                {
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    var panel = new StackPanel { Margin = new Thickness(4,0,4,0), HorizontalAlignment = HorizontalAlignment.Stretch };
                    
                    // Zone 헤더 (텍스트 + 상태 표시기)
                    var headerPanel = new StackPanel 
                    { 
                        Orientation = Orientation.Horizontal, 
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        Margin = new Thickness(0,0,0,4)
                    };
                    
                    var label = new TextBlock
                    {
                        Text = $"Zone{i + 1}",
                        FontWeight = FontWeights.Bold,
                        VerticalAlignment = VerticalAlignment.Center,
                        Foreground = darkMode ? Brushes.White : Brushes.Black
                    };
                    
                    // 상태 표시기 추가
                    var statusIndicator = new System.Windows.Shapes.Ellipse
                    {
                        Width = 8,
                        Height = 8,
                        Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80)), // 초록색
                        Margin = new Thickness(8, 0, 0, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    statusIndicator.Name = $"Zone{i + 1}StatusIndicator";
                    
                    headerPanel.Children.Add(label);
                    headerPanel.Children.Add(statusIndicator);
                    panel.Children.Add(headerPanel);
                    // 모던한 Border로 감싸기
                    var border = new Border
                    {
                        Background = darkMode ? 
                            new SolidColorBrush(Color.FromRgb(30, 30, 30)) : // 다크모드: 어두운 배경
                            new SolidColorBrush(Color.FromRgb(255, 255, 255)), // 라이트모드: 순백색 배경
                        BorderBrush = darkMode ?
                            new SolidColorBrush(Color.FromRgb(64, 64, 64)) : // 다크모드: 어두운 테두리
                            new SolidColorBrush(Color.FromRgb(226, 232, 240)), // 라이트모드: 연한 회색 테두리
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(6), // 둥근 모서리
                        Margin = new Thickness(0, 0, 0, 4),
                        Effect = new System.Windows.Media.Effects.DropShadowEffect
                        {
                            Color = Colors.Black,
                            Opacity = 0.05,
                            BlurRadius = 4,
                            ShadowDepth = 1
                        }
                    };
                    
                    var box = new TextBox
                    {
                        Name = $"MonitorBox_{i}",
                        FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                        FontSize = 11,
                        IsReadOnly = true,
                        TextWrapping = TextWrapping.Wrap,
                        AcceptsReturn = true,
                        Height = 160,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        Background = Brushes.Transparent, // 투명 배경
                        BorderThickness = new Thickness(0), // 테두리 제거
                        Padding = new Thickness(12, 8, 12, 8), // 더 넉넉한 패딩
                        Foreground = darkMode ? 
                            Brushes.White : // 다크모드: 흰색 텍스트
                            new SolidColorBrush(Color.FromRgb(51, 65, 85)) // 라이트모드: 진한 회색 텍스트
                    };
                    
                    border.Child = box;
                    panel.Children.Add(border);
                    Grid.SetColumn(panel, i);
                    grid.Children.Add(panel);
                }

                // 기존 로그 뿌리고 구독 연결
                foreach (var (zoneIndex, text) in MonitorLogService.Instance.GetRecentLogs())
                    AppendMonitor(zoneIndex, text);

                MonitorLogService.Instance.LogReceived -= OnMonitorLogReceived;
                MonitorLogService.Instance.LogReceived += OnMonitorLogReceived;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"InitializeMonitorArea 오류: {ex.Message}");
            }
        }

        private void OnMonitorLogReceived(int zoneIndex, string text)
        {
            // DispatcherPriority.Send로 즉시 UI 업데이트 (실시간 로그 표시)
            Dispatcher?.Invoke(new Action(() => 
            {
                AppendMonitor(zoneIndex, text);
                UpdateStatusIndicator(zoneIndex, text);
            }));
        }

        private void UpdateStatusIndicator(int zoneIndex, string text)
        {
            var indicatorName = $"Zone{zoneIndex + 1}StatusIndicator";
            var indicator = this.FindName(indicatorName) as System.Windows.Shapes.Ellipse;
            
            // System.Diagnostics.Debug.WriteLine($"[상태표시기] Zone{zoneIndex + 1}: '{text}' - 표시기: {(indicator != null ? "찾음" : "없음")}");
            
            if (indicator == null) 
            {
                // System.Diagnostics.Debug.WriteLine($"[상태표시기] 표시기를 찾을 수 없음: {indicatorName}");
                return;
            }

            // 로그 메시지에 따라 상태 표시기 색상 변경 (우선순위: FAIL > OK > ENTER)
            if (text.Contains("FAIL") || text.Contains("failed"))
            {
                indicator.Fill = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // 빨간색
                System.Diagnostics.Debug.WriteLine($"[상태표시기] Zone{zoneIndex + 1}: 빨간색으로 변경 (FAIL)");
            }
            else if (text.Contains("Execute") && text.Contains("=> OK"))
            {
                indicator.Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // 초록색
                System.Diagnostics.Debug.WriteLine($"[상태표시기] Zone{zoneIndex + 1}: 초록색으로 변경 (OK)");
            }
            else if (text.Contains("ENTER") || text.Contains("Execute"))
            {
                indicator.Fill = new SolidColorBrush(Color.FromRgb(255, 193, 7)); // 노란색
                System.Diagnostics.Debug.WriteLine($"[상태표시기] Zone{zoneIndex + 1}: 노란색으로 변경 (실행중)");
            }
            else if (text.Contains("OK") || text.Contains("완료"))
            {
                indicator.Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // 초록색
                System.Diagnostics.Debug.WriteLine($"[상태표시기] Zone{zoneIndex + 1}: 초록색으로 변경 (OK)");
            }
        }

        private void AppendMonitor(int zoneIndex, string line)
        {
            var grid = this.FindName("MonitorGrid") as Grid;
            if (grid == null) return;

            var box = LogicalTreeHelper.FindLogicalNode(grid, $"MonitorBox_{zoneIndex}") as TextBox;
            if (box == null && grid.Children.Count > 0)
            {
                // 폴백: 첫 번째 박스 사용
                box = LogicalTreeHelper.FindLogicalNode(grid, "MonitorBox_0") as TextBox;
            }
            if (box == null) return;

            box.AppendText(line + Environment.NewLine);
            box.CaretIndex = box.Text.Length;
            box.ScrollToEnd();
        }

        public void SetDarkMode(bool isDarkMode)
        {
            // ViewModel에 다크모드 상태 전달
            if (viewModel != null)
            {
                viewModel.IsDarkMode = isDarkMode;
            }
            
            // IPVSPage와 동일한 방식으로 다크모드 적용
            if (isDarkMode)
            {
                ThemeManager.UpdateDynamicColors(this, true);
            }
            else
            {
                ThemeManager.UpdateDynamicColors(this, false);
            }
            
            // 테이블, Zone 버튼, Monitor 영역, Graph 영역을 다시 생성하여 올바른 색상 적용
            CreateCustomTable();
            CreateZoneButtons();
            InitializeMonitorArea(isDarkMode); // Monitor 영역도 다크모드에 맞게 재생성
            InitializeGraphAreaWithDarkMode(isDarkMode); // Graph 영역 다크모드에 맞게 강제 재생성
            
            // 클래스 변수에 다크모드 상태 저장 (UpdateGraphAreaDarkMode 호출 이후)
            this.isDarkMode = isDarkMode;
        }

        /// <summary>
        /// Graph 영역의 다크모드 색상만 업데이트 (데이터 보존)
        /// </summary>
        private void UpdateGraphAreaDarkMode(bool darkMode)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Graph 영역 다크모드 색상 업데이트 시작 - 다크모드: {darkMode}");
                
                var graphContent = FindName("GraphContent") as Grid;
                if (graphContent == null || graphContent.Children.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("GraphContent가 없거나 비어있음 - 초기화 필요");
                    InitializeGraphArea(); // 기존 로직 사용
                    return;
                }

                // ScrollViewer에서 기존 UI 요소들의 색상만 업데이트 (항상 실행)
                var scrollViewer = graphContent.Children.OfType<ScrollViewer>().FirstOrDefault();
                if (scrollViewer?.Content is StackPanel mainStackPanel)
                {
                    UpdateZoneColors(mainStackPanel, darkMode);
                }

                System.Diagnostics.Debug.WriteLine("Graph 영역 다크모드 색상 업데이트 완료");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Graph 영역 다크모드 색상 업데이트 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// Zone UI 요소들의 색상을 다크모드에 맞게 업데이트
        /// </summary>
        private void UpdateZoneColors(StackPanel mainStackPanel, bool darkMode)
        {
            try
            {
                foreach (var child in mainStackPanel.Children)
                {
                    if (child is Border zoneSection)
                    {
                        // Zone 섹션 배경색 업데이트
                        zoneSection.Background = darkMode ?
                            new SolidColorBrush(Color.FromRgb(45, 45, 45)) : // 다크모드: 어두운 배경
                            new SolidColorBrush(Colors.White); // 라이트모드: 흰색 배경

                        var zoneStackPanel = zoneSection.Child as StackPanel;
                        if (zoneStackPanel != null)
                        {
                            foreach (var zoneChild in zoneStackPanel.Children)
                            {
                                if (zoneChild is TextBlock zoneTitle)
                                {
                                    // Zone 제목 색상 업데이트
                                    zoneTitle.Foreground = darkMode ?
                                        new SolidColorBrush(Color.FromRgb(100, 200, 255)) : // 다크모드: 밝은 파란색
                                        new SolidColorBrush(Color.FromRgb(0, 100, 150)); // 라이트모드: 어두운 파란색
                                }
                                else if (zoneChild is Border separator)
                                {
                                    // 구분선 색상 업데이트
                                    separator.Background = darkMode ?
                                        new SolidColorBrush(Color.FromRgb(100, 200, 255)) : // 다크모드: 밝은 파란색
                                        new SolidColorBrush(Color.FromRgb(0, 100, 150)); // 라이트모드: 어두운 파란색
                                }
                                else if (zoneChild is Border judgmentRow)
                                {
                                    // 판정 행 배경색 업데이트
                                    judgmentRow.Background = darkMode ?
                                        new SolidColorBrush(Color.FromRgb(45, 45, 45)) : // 다크모드: 어두운 배경
                                        new SolidColorBrush(Colors.White); // 라이트모드: 흰색 배경

                                    var judgmentGrid = judgmentRow.Child as Grid;
                                    if (judgmentGrid?.Children.Count > 1)
                                    {
                                        var canvas = judgmentGrid.Children[1] as Canvas;
                                        if (canvas != null)
                                        {
                                            // Canvas 배경색 업데이트
                                            canvas.Background = darkMode ?
                                                new SolidColorBrush(Color.FromRgb(30, 30, 30)) : // 다크모드: 더 어두운 배경
                                                new SolidColorBrush(Colors.White); // 라이트모드: 흰색 배경
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Zone 색상 업데이트 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 다크모드 전환 시 Graph 영역 강제 재생성 (기존 내용 무시)
        /// </summary>
        private void InitializeGraphAreaWithDarkMode(bool darkMode)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("다크모드 전환을 위한 Graph 영역 강제 재생성 시작");
                
                var graphContent = FindName("GraphContent") as Grid;
                if (graphContent == null)
                {
                    System.Diagnostics.Debug.WriteLine("GraphContent를 찾을 수 없습니다.");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"GraphContent 찾음: {graphContent.Name}");

                // 다크모드 전환 시 강제로 기존 내용 제거 (다른 영역과 동일하게)
                graphContent.Children.Clear();

                // Zone 개수 읽기
                string zoneCountStr = GlobalDataManager.GetValue("Settings", "MTP_ZONE", "1");
                if (!int.TryParse(zoneCountStr, out int zoneCount) || zoneCount < 1) zoneCount = 1;

                System.Diagnostics.Debug.WriteLine($"다크모드 Graph 영역 재생성 - Zone 개수: {zoneCount}");

                // ScrollViewer로 감싸서 스크롤 가능하게 만들기
                var scrollViewer = new ScrollViewer
                {
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Margin = new Thickness(0),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };

                // 메인 StackPanel 생성
                var mainStackPanel = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Margin = new Thickness(0),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Top
                };

                // Zone별로 세로 분리된 그래프 영역 생성 (다크모드 적용)
                for (int zone = 1; zone <= zoneCount; zone++)
                {
                    CreateZoneWithJudgmentRows(mainStackPanel, zone, darkMode);
                }

                scrollViewer.Content = mainStackPanel;
                graphContent.Children.Add(scrollViewer);

                System.Diagnostics.Debug.WriteLine("다크모드 Graph 영역 재생성 완료");

                // 기존 데이터가 있으면 자동으로 복원
                RestoreExistingGraphData();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"다크모드 Graph 영역 재생성 오류: {ex.Message}");
            }
        }
        

        private void CreateCustomTable()
        {
            try
            {
                // 기존 내용 클리어
                DataTableGrid.RowDefinitions.Clear();
                DataTableGrid.Children.Clear();

                // 헤더 행 추가
                CreateHeaderRow();

                // 데이터 행들 추가 (데이터가 있을 때만)
                if (viewModel?.DataItems != null && viewModel.DataItems.Count > 0)
                {
                CreateDataRows();
                    System.Diagnostics.Debug.WriteLine("데이터가 있어서 테이블 행 생성함");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("데이터가 없어서 헤더만 생성함");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"커스텀 테이블 생성 오류: {ex.Message}");
            }
        }

        private void CreateHeaderRow()
        {
            // 헤더 행 정의
            DataTableGrid.RowDefinitions.Add(new RowDefinition { Height = new System.Windows.GridLength(45) });

            string[] headers = { "Zone", "Cell ID", "Inner ID", "Category", "X", "Y", "L", "Current", "Efficiency", "Error Name", "Tact", "Judgment" };
            
            for (int i = 0; i < headers.Length; i++)
            {
                var headerBorder = new Border
                {
                    Background = (System.Windows.Media.SolidColorBrush)FindResource("PrimaryColor"),
                    BorderBrush = (System.Windows.Media.SolidColorBrush)FindResource("PrimaryColor"),
                    BorderThickness = new System.Windows.Thickness(1),
                    Child = new TextBlock
                    {
                        Text = headers[i],
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        VerticalAlignment = System.Windows.VerticalAlignment.Center,
                        FontWeight = System.Windows.FontWeights.SemiBold,
                        Foreground = new SolidColorBrush(Colors.White),
                        FontSize = 13
                    }
                };

                Grid.SetColumn(headerBorder, i);
                Grid.SetRow(headerBorder, 0);
                DataTableGrid.Children.Add(headerBorder);
            }
        }

        private void CreateDataRows()
        {
            if (viewModel?.DataItems == null || viewModel.DataItems.Count == 0) 
            {
                System.Diagnostics.Debug.WriteLine("CreateDataRows: DataItems가 비어있음 - 테이블 생성하지 않음");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"CreateDataRows: 전체 아이템 개수 = {viewModel.DataItems.Count}");

            // 기존 데이터 행들만 제거 (헤더는 유지)
            var existingDataChildren = DataTableGrid.Children.Cast<UIElement>()
                .Where(child => Grid.GetRow(child) > 0).ToList();

            foreach (var child in existingDataChildren)
            {
                DataTableGrid.Children.Remove(child);
            }

            // 기존 행 정의들 제거 (헤더 행은 유지)
            while (DataTableGrid.RowDefinitions.Count > 1)
            {
                DataTableGrid.RowDefinitions.RemoveAt(DataTableGrid.RowDefinitions.Count - 1);
            }

            // Zone별로 그룹화 (모든 Zone 표시)
            var groupedData = viewModel.DataItems.GroupBy(item => item.Zone).ToList();

            foreach (var zoneGroup in groupedData)
            {
                var zoneItems = zoneGroup.ToList();
                var firstItem = zoneItems.First();

                // 각 Zone의 카테고리 개수만큼 행 생성
                for (int i = 0; i < zoneItems.Count; i++)
                {
                    DataTableGrid.RowDefinitions.Add(new RowDefinition { Height = new System.Windows.GridLength(32) });
                    int currentRow = DataTableGrid.RowDefinitions.Count - 1;
                    var currentItem = zoneItems[i];

                    // Zone 컬럼 (첫 번째 행에서만 표시, RowSpan 적용)
                    if (i == 0)
                    {
                        var zoneBorder = new Border
                        {
                            Background = (System.Windows.Media.SolidColorBrush)FindResource("DynamicSurfaceColor"),
                            BorderBrush = (System.Windows.Media.SolidColorBrush)FindResource("DynamicBorderColor"),
                            BorderThickness = new System.Windows.Thickness(1),
                            Child = new TextBlock
                            {
                                Text = firstItem.Zone,
                                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                                FontWeight = System.Windows.FontWeights.SemiBold,
                                Foreground = (System.Windows.Media.SolidColorBrush)FindResource("DynamicTextPrimaryColor")
                            }
                        };
                        Grid.SetColumn(zoneBorder, 0);
                        Grid.SetRow(zoneBorder, currentRow);
                        Grid.SetRowSpan(zoneBorder, zoneItems.Count);
                        DataTableGrid.Children.Add(zoneBorder);
                    }

                    // Cell ID 컬럼 (첫 번째 행에서만 표시, RowSpan 적용)
                    if (i == 0)
                    {
                        var cellIdBorder = new Border
                        {
                            Background = (System.Windows.Media.SolidColorBrush)FindResource("DynamicSurfaceColor"),
                            BorderBrush = (System.Windows.Media.SolidColorBrush)FindResource("DynamicBorderColor"),
                            BorderThickness = new System.Windows.Thickness(1),
                            Child = new TextBlock
                            {
                                Text = firstItem.CellId,
                                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                                FontWeight = System.Windows.FontWeights.Medium,
                                Foreground = (System.Windows.Media.SolidColorBrush)FindResource("DynamicTextPrimaryColor")
                            }
                        };
                        Grid.SetColumn(cellIdBorder, 1);
                        Grid.SetRow(cellIdBorder, currentRow);
                        Grid.SetRowSpan(cellIdBorder, zoneItems.Count);
                        DataTableGrid.Children.Add(cellIdBorder);
                    }

                    // Inner ID 컬럼 (첫 번째 행에서만 표시, RowSpan 적용)
                    if (i == 0)
                    {
                        var innerIdBorder = new Border
                        {
                            Background = (System.Windows.Media.SolidColorBrush)FindResource("DynamicSurfaceColor"),
                            BorderBrush = (System.Windows.Media.SolidColorBrush)FindResource("DynamicBorderColor"),
                            BorderThickness = new System.Windows.Thickness(1),
                            Child = new TextBlock
                            {
                                Text = firstItem.InnerId,
                                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                                FontWeight = System.Windows.FontWeights.Medium,
                                Foreground = (System.Windows.Media.SolidColorBrush)FindResource("DynamicTextPrimaryColor")
                            }
                        };
                        Grid.SetColumn(innerIdBorder, 2);
                        Grid.SetRow(innerIdBorder, currentRow);
                        Grid.SetRowSpan(innerIdBorder, zoneItems.Count);
                        DataTableGrid.Children.Add(innerIdBorder);
                    }

                    // Category 컬럼 (각 행마다 개별 표시)
                    var categoryBorder = new Border
                    {
                        Background = (System.Windows.Media.SolidColorBrush)FindResource("DynamicSurfaceColor"),
                        BorderBrush = (System.Windows.Media.SolidColorBrush)FindResource("DynamicBorderColor"),
                        BorderThickness = new System.Windows.Thickness(1),
                        Child = new TextBlock
                        {
                            Text = currentItem.Category,
                            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                            VerticalAlignment = System.Windows.VerticalAlignment.Center,
                            FontWeight = System.Windows.FontWeights.Medium,
                            Foreground = (System.Windows.Media.SolidColorBrush)FindResource("DynamicTextPrimaryColor")
                        }
                    };
                    Grid.SetColumn(categoryBorder, 3);
                    Grid.SetRow(categoryBorder, currentRow);
                    DataTableGrid.Children.Add(categoryBorder);

                    // X, Y, L, Current, Efficiency 컬럼들 (각 행마다 개별 표시)
                    string[] dataValues = { currentItem.X, currentItem.Y, currentItem.L, currentItem.Current, currentItem.Efficiency };
                    int[] dataColumns = { 4, 5, 6, 7, 8 };

                    for (int j = 0; j < dataValues.Length; j++)
                    {
                        var dataBorder = new Border
                        {
                            Background = (System.Windows.Media.SolidColorBrush)FindResource("DynamicSurfaceColor"),
                            BorderBrush = (System.Windows.Media.SolidColorBrush)FindResource("DynamicBorderColor"),
                            BorderThickness = new System.Windows.Thickness(1),
                            Child = new TextBlock
                            {
                                Text = dataValues[j],
                                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                                FontWeight = System.Windows.FontWeights.Normal,
                                Foreground = (System.Windows.Media.SolidColorBrush)FindResource("DynamicTextPrimaryColor")
                            }
                        };
                        Grid.SetColumn(dataBorder, dataColumns[j]);
                        Grid.SetRow(dataBorder, currentRow);
                        DataTableGrid.Children.Add(dataBorder);
                    }

                    // Error Name, Tact, Judgment 컬럼들 (첫 번째 행에서만 표시, RowSpan 적용)
                    if (i == 0)
                    {
                        string[] mergedValues = { firstItem.ErrorName, firstItem.Tact, firstItem.Judgment };
                        int[] mergedColumns = { 9, 10, 11 };

                        for (int k = 0; k < mergedValues.Length; k++)
                        {
                            var mergedBorder = new Border
                            {
                                Background = (System.Windows.Media.SolidColorBrush)FindResource("DynamicSurfaceColor"),
                                BorderBrush = (System.Windows.Media.SolidColorBrush)FindResource("DynamicBorderColor"),
                                BorderThickness = new System.Windows.Thickness(1),
                                Child = new TextBlock
                                {
                                    Text = mergedValues[k],
                                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                                    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                                    FontWeight = System.Windows.FontWeights.Medium,
                                    Foreground = (System.Windows.Media.SolidColorBrush)FindResource("DynamicTextPrimaryColor")
                                }
                            };
                            Grid.SetColumn(mergedBorder, mergedColumns[k]);
                            Grid.SetRow(mergedBorder, currentRow);
                            Grid.SetRowSpan(mergedBorder, zoneItems.Count);
                            DataTableGrid.Children.Add(mergedBorder);
                        }
                    }
                }
            }
        }

        private void CreateZoneButtons()
        {
            try
            {
                var zoneButtonsPanel = this.FindName("ZoneButtonsPanel") as StackPanel;
                if (zoneButtonsPanel == null) return;

                // 기존 버튼들 제거
                zoneButtonsPanel.Children.Clear();

                // Settings에서 MTP_ZONE 개수 읽기
                string zoneCountStr = GlobalDataManager.GetValue("Settings", "MTP_ZONE", "2");
                int zoneCount = int.Parse(zoneCountStr);

                for (int i = 1; i <= zoneCount; i++)
                {
                    var zoneButton = new Button
                    {
                        Content = i.ToString(),
                        MinWidth = 28,
                        MinHeight = 28,
                        FontSize = 12,
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(3, 0, 3, 0),
                        Tag = i - 1 // 0-based index
                    };

                    // 첫 번째 버튼은 활성화 상태
                    if (i == 1)
                    {
                        zoneButton.Style = (Style)FindResource("ActiveZoneButtonStyle");
                    }
                    else
                    {
                        zoneButton.Style = (Style)FindResource("ZoneButtonStyle");
                    }

                    zoneButton.Click += (s, e) => {
                        if (s is Button btn && btn.Tag is int zoneIndex)
                        {
                            System.Diagnostics.Debug.WriteLine($"=== Zone 버튼 클릭 ===");
                            System.Diagnostics.Debug.WriteLine($"클릭된 버튼: Zone {i} (Tag: {zoneIndex})");
                            System.Diagnostics.Debug.WriteLine($"이전 CurrentZone: {viewModel.CurrentZone}");
                            
                            viewModel.CurrentZone = zoneIndex;
                            
                            System.Diagnostics.Debug.WriteLine($"새로운 CurrentZone: {viewModel.CurrentZone}");
                            System.Diagnostics.Debug.WriteLine($"업데이트될 targetZone: {zoneIndex + 1}");

                            // 모든 Zone 버튼 스타일 초기화
                            foreach (var child in zoneButtonsPanel.Children)
                            {
                                if (child is Button childBtn)
                                {
                                    childBtn.Style = (Style)FindResource("ZoneButtonStyle");
                                }
                            }

                            // 선택된 버튼 스타일 변경
                            btn.Style = (Style)FindResource("ActiveZoneButtonStyle");

                            // CreateCustomTable() 호출 제거 - 테이블을 다시 그리지 않음
                            System.Diagnostics.Debug.WriteLine($"Zone {zoneIndex + 1} 선택됨. 테이블 재생성 안함.");
                        }
                    };

                    zoneButtonsPanel.Children.Add(zoneButton);
                }

                // 첫 번째 Zone을 기본 선택
                if (zoneButtonsPanel.Children.Count > 0)
                {
                    viewModel.CurrentZone = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Zone 버튼 생성 오류: {ex.Message}");
            }
        }

        private void GraphTab_Click(object sender, RoutedEventArgs e)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            
            var graphTabBtn = FindName("GraphTab") as Button;
            var monitorTabBtn = FindName("MonitorTab") as Button;
            var totalTabBtn = FindName("TotalTab") as Button;
            var monitorContent = FindName("MonitorContent") as UIElement;
            var graphContent = FindName("GraphContent") as Grid;
            var totalContent = FindName("TotalContent") as UIElement;

            // 스타일 변경 (즉시 적용 - 우선순위 높음)
            if (graphTabBtn != null) 
            {
                graphTabBtn.Style = (Style)FindResource("ActiveTabButtonStyle");
                graphTabBtn.UpdateLayout(); // 강제 렌더링
            }
            if (monitorTabBtn != null) monitorTabBtn.Style = (Style)FindResource("TabButtonStyle");
            if (totalTabBtn != null) totalTabBtn.Style = (Style)FindResource("TabButtonStyle");

            // Visibility 변경 (즉시 적용)
            if (monitorContent != null) monitorContent.Visibility = Visibility.Collapsed;
            if (graphContent != null) 
            {
                graphContent.Visibility = Visibility.Visible;
                graphContent.UpdateLayout(); // 강제 렌더링
            }
            if (totalContent != null) totalContent.Visibility = Visibility.Collapsed;
            
            sw.Stop();
            System.Diagnostics.Debug.WriteLine($"[성능] 탭 전환 완료: {sw.ElapsedMilliseconds}ms");

            // Graph 탭 활성화 시 데이터 포인트 업데이트
            try
            {
                var graphSw = System.Diagnostics.Stopwatch.StartNew();
                
                if (graphContent != null)
                {
                    // ViewModel에서 그래프 데이터 가져와서 포인트 업데이트
                    if (viewModel != null && viewModel.GraphDataPoints != null && viewModel.GraphDataPoints.Count > 0)
                    {
                        UpdateGraphDataPoints(viewModel.GraphDataPoints);
                        graphSw.Stop();
                        System.Diagnostics.Debug.WriteLine($"[성능] 그래프 데이터 업데이트 완료: {graphSw.ElapsedMilliseconds}ms ({viewModel.GraphDataPoints.Count}개 포인트)");
                    }
                    else
                    {
                        // 데이터가 없어도 복원 시도 (혹시 놓친 데이터가 있을 수 있음)
                        System.Diagnostics.Debug.WriteLine("Graph 데이터가 없음 - 복원 시도");
                        RestoreExistingGraphData();
                        
                        graphSw.Stop();
                        System.Diagnostics.Debug.WriteLine($"[성능] 그래프 영역 표시 (데이터 없음): {graphSw.ElapsedMilliseconds}ms");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Graph 탭 클릭 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 기존 그래프 영역에 데이터 포인트만 업데이트 (빠른 업데이트용)
        /// </summary>
        private void UpdateGraphDataPoints(List<OpticPageViewModel.GraphDataPoint> dataPoints)
        {
            try
            {
                var graphContent = FindName("GraphContent") as Grid;
                if (graphContent == null) 
                {
                    System.Diagnostics.Debug.WriteLine("GraphContent를 찾을 수 없습니다!");
                    return;
                }
                
                // 그래프 영역이 초기화되지 않았으면 강제 초기화
                if (graphContent.Children.Count == 0)
                {
                    InitializeGraphArea();
                }

                // ScrollViewer에서 Canvas들 찾기
                var allCanvases = new List<Canvas>();
                FindCanvasesInPanel(graphContent, allCanvases);

                // Zone별 Judgment Canvas 찾기 (새로운 이름 형식: Zone{number}_{judgment}GraphCanvas)
                var zoneJudgmentCanvases = allCanvases.Where(c => c.Name.Contains("Zone") && c.Name.Contains("GraphCanvas")).ToList();

                // 각 Zone별 Judgment별로 데이터 포인트 그리기
                foreach (var canvas in zoneJudgmentCanvases)
                {
                    canvas.Children.Clear(); // 기존 포인트 제거
                    
                    // Canvas 이름 파싱: Zone{number}_{judgment}GraphCanvas
                    var nameParts = canvas.Name.Replace("GraphCanvas", "").Split('_');
                    if (nameParts.Length == 2)
                    {
                        var zoneNumber = int.Parse(nameParts[0].Replace("Zone", ""));
                        var judgmentType = nameParts[1];
                        
                        // 해당 Zone과 Judgment에 맞는 데이터 포인트 필터링
                        // R/J를 RJ로 변환하여 매칭
                        string searchJudgment = judgmentType == "RJ" ? "R/J" : judgmentType;
                        var filteredPoints = dataPoints.Where(p => p.ZoneNumber == zoneNumber && p.Judgment == searchJudgment).ToList();
                        
                        
                        DrawDataPointsOnCanvas(canvas, filteredPoints, $"{zoneNumber}_{judgmentType}");
                    }
                }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"그래프 데이터 포인트 업데이트 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// Panel에서 모든 Canvas를 재귀적으로 찾기
        /// </summary>
        private void FindCanvasesInPanel(Panel panel, List<Canvas> canvasList)
        {
            foreach (var child in panel.Children)
            {
                if (child is Canvas canvas)
                {
                    canvasList.Add(canvas);
                }
                else if (child is Panel childPanel)
                {
                    FindCanvasesInPanel(childPanel, canvasList);
                }
                else if (child is Border border && border.Child is Panel borderPanel)
                {
                    FindCanvasesInPanel(borderPanel, canvasList);
                }
                else if (child is ScrollViewer scrollViewer && scrollViewer.Content is Panel scrollPanel)
                {
                    FindCanvasesInPanel(scrollPanel, canvasList);
                }
            }
        }

        /// <summary>
        /// Canvas에 데이터 포인트 그리기
        /// </summary>
        private void DrawDataPointsOnCanvas(Canvas canvas, List<OpticPageViewModel.GraphDataPoint> points, object identifier)
        {
            try
            {
                if (points == null || points.Count == 0) return;

                var maxPoints = 300; // 최대 300개 포인트
                var canvasWidth = canvas.ActualWidth > 0 ? canvas.ActualWidth : 600; // 더 넓은 기본값
                var canvasHeight = canvas.ActualHeight > 0 ? canvas.ActualHeight : 80;
                
                // Canvas 너비 디버그 출력
                System.Diagnostics.Debug.WriteLine($"Canvas 너비: {canvasWidth}, 최대 포인트: {maxPoints}");

                // 최근 포인트만 사용 (FIFO) - .NET Framework 호환성
                var recentPoints = points.Skip(Math.Max(0, points.Count - maxPoints)).ToList();

                // 전역 인덱스 계산 (전체 데이터 기준으로 0부터 시작)
                var allDataPoints = viewModel?.GraphDataPoints;
                if (allDataPoints == null || allDataPoints.Count == 0) return;

                // 전체 데이터 포인트를 시간순 정렬하여 전역 인덱스 생성
                var globalAllPoints = allDataPoints
                    .OrderBy(dp => dp.Timestamp)
                    .ToList();

                for (int i = 0; i < recentPoints.Count; i++)
                {
                    var point = recentPoints[i];
                    
                    // 현재 포인트가 전체 데이터에서 몇 번째인지 찾기 (전역 인덱스)
                    var globalIndex = globalAllPoints.FindIndex(dp => 
                        dp.ZoneNumber == point.ZoneNumber && 
                        dp.Judgment == point.Judgment && 
                        dp.Timestamp == point.Timestamp);
                    
                    if (globalIndex >= 0)
                    {
                        // X 위치 계산 (전역 인덱스 사용하여 300개 기준으로 꽉 차게)
                        double availableWidth = canvasWidth - 10; // 양쪽 5px씩 마진
                        double stepWidth = availableWidth / 300.0; // 300개 기준으로 균등 배치
                        double x = globalIndex * stepWidth + 5; // 왼쪽 5px 마진에서 시작
                        
                        // 마지막 포인트가 Canvas 끝에 도달하도록 조정 (300개 기준)
                        if (globalIndex >= 299) // 300번째 포인트 이후
                        {
                            x = canvasWidth - 5; // Canvas 끝에서 5px 안쪽
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"그래프 포인트: Zone{point.ZoneNumber} {point.Judgment} -> 전역인덱스:{globalIndex}, X위치:{x:F1}, 시간:{point.Timestamp:HH:mm:ss.fff}");
                        
                        // Y 위치 계산 (Canvas 중앙)
                        double y = canvasHeight / 2;

                        // 포인트 색상 결정
                        var brush = GetJudgmentColor(point.Judgment);
                        
                        // 원형 포인트 그리기 (더 크게)
                        var ellipse = new Ellipse
                        {
                            Width = 6,
                            Height = 6,
                            Fill = brush,
                            Stroke = new SolidColorBrush(Colors.Black),
                            StrokeThickness = 0.5
                        };

                        Canvas.SetLeft(ellipse, x - 2);
                        Canvas.SetTop(ellipse, y - 2);
                        canvas.Children.Add(ellipse);
                    }
                }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Canvas 그리기 오류 ({identifier}): {ex.Message}");
            }
        }


        private void TotalTab_Click(object sender, RoutedEventArgs e)
        {
            var graphTabBtn = FindName("GraphTab") as Button;
            var monitorTabBtn = FindName("MonitorTab") as Button;
            var totalTabBtn = FindName("TotalTab") as Button;
            var monitorContent = FindName("MonitorContent") as UIElement;
            var graphContent = FindName("GraphContent") as UIElement;
            var totalContent = FindName("TotalContent") as UIElement;

            if (totalTabBtn != null) totalTabBtn.Style = (Style)FindResource("ActiveTabButtonStyle");
            if (monitorTabBtn != null) monitorTabBtn.Style = (Style)FindResource("TabButtonStyle");
            if (graphTabBtn != null) graphTabBtn.Style = (Style)FindResource("TabButtonStyle");

            if (monitorContent != null) monitorContent.Visibility = Visibility.Collapsed;
            if (graphContent != null) graphContent.Visibility = Visibility.Collapsed;
            if (totalContent != null) totalContent.Visibility = Visibility.Visible;
        }

        private void MonitorTab_Click(object sender, RoutedEventArgs e)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            
            
            var graphTabBtn = FindName("GraphTab") as Button;
            var monitorTabBtn = FindName("MonitorTab") as Button;
            var totalTabBtn = FindName("TotalTab") as Button;
            var monitorContent = FindName("MonitorContent") as UIElement;
            var graphContent = FindName("GraphContent") as UIElement;
            var totalContent = FindName("TotalContent") as UIElement;

            // 스타일 변경 (즉시 적용 - 우선순위 높음)
            if (monitorTabBtn != null) 
            {
                monitorTabBtn.Style = (Style)FindResource("ActiveTabButtonStyle");
                monitorTabBtn.UpdateLayout(); // 강제 렌더링
            }
            if (graphTabBtn != null) graphTabBtn.Style = (Style)FindResource("TabButtonStyle");
            if (totalTabBtn != null) totalTabBtn.Style = (Style)FindResource("TabButtonStyle");

            // Visibility 변경 (즉시 적용)
            if (monitorContent != null) 
            {
                monitorContent.Visibility = Visibility.Visible;
                monitorContent.UpdateLayout(); // 강제 렌더링
            }
            if (graphContent != null) graphContent.Visibility = Visibility.Collapsed;
            if (totalContent != null) totalContent.Visibility = Visibility.Collapsed;
            
            sw.Stop();
            System.Diagnostics.Debug.WriteLine($"[성능] MONITOR 탭 전환 완료: {sw.ElapsedMilliseconds}ms");
        }

        private void InitializeWadComboBox()
        {
            try
            {
                // WAD 콤보박스 초기화 시작 플래그 설정
                isInitializingWadComboBox = true;
                
                // WAD 콤보박스에 아이템 추가
                WadComboBox.Items.Clear();
                
                // INI 파일에서 WAD 값 읽기
                string wadValues = GlobalDataManager.GetValue("MTP", "WAD", "0,15,30,45");
                
                // 쉼표로 분리하여 배열로 변환
                string[] wadArray = wadValues.Split(',');
                
                // 각 WAD 값에 대해 아이템 추가
                foreach (string wadValue in wadArray)
                {
                    string trimmedValue = wadValue.Trim();
                    if (!string.IsNullOrEmpty(trimmedValue))
                    {
                        WadComboBox.Items.Add(trimmedValue);
                    }
                }
                
                // 기본값 설정 (이때 SelectionChanged 이벤트가 발생하지만 플래그로 차단됨)
                WadComboBox.SelectedIndex = 0;
                
                // WAD 콤보박스 초기화 완료 플래그 해제
                isInitializingWadComboBox = false;
                
                System.Diagnostics.Debug.WriteLine("WAD 콤보박스 초기화 완료 - 더미 데이터 생성하지 않음");
            }
            catch (Exception ex)
            {
                // 오류 발생 시에도 플래그 해제
                isInitializingWadComboBox = false;
                System.Diagnostics.Debug.WriteLine($"WAD 콤보박스 초기화 오류: {ex.Message}");
                
                // 오류 발생 시 기본값 사용
                WadComboBox.Items.Clear();
                string[] defaultWadArray = { "0", "15", "30", "45" };
                foreach (string wadValue in defaultWadArray)
                {
                    WadComboBox.Items.Add(wadValue);
                }
                WadComboBox.SelectedIndex = 0;
            }
        }

        private bool isInitializingWadComboBox = false; // WAD 콤보박스 초기화 중인지 확인하는 플래그

        private void WadComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // WAD 콤보박스 초기화 중이면 더미 데이터 생성하지 않음
                if (isInitializingWadComboBox)
                {
                    System.Diagnostics.Debug.WriteLine("WAD 콤보박스 초기화 중이므로 더미 데이터 생성하지 않음");
                    return;
                }

                if (WadComboBox.SelectedItem != null)
                {
                    string selectedWad = WadComboBox.SelectedItem.ToString();
                    
                    // WAD 값을 배열 인덱스로 변환
                    int wadIndex = GetWadArrayIndex(selectedWad);
                    
                    System.Diagnostics.Debug.WriteLine($"WAD 값 '{selectedWad}'이 선택되었습니다. 배열 인덱스: {wadIndex}");
                    
                    // 선택된 WAD에 해당하는 데이터로 UI 업데이트
                    UpdateDataForWad(wadIndex);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WAD 콤보박스 선택 변경 오류: {ex.Message}");
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("RESET 버튼 클릭됨");
                
                // 모든 Zone의 테스트 완료 상태 초기화
                for (int i = 0; i < zoneTestCompleted.Length; i++)
                {
                    zoneTestCompleted[i] = false;
                }
                
                // 전역 테스트 상태도 초기화
                isTestStarted = false;
                
                // 모든 측정값 클리어
                ClearMeasurementValues();
                
                // 판정 현황 카운터 초기화
                if (viewModel != null)
                {
                    viewModel.InitializeJudgmentCounters();
                    viewModel.UpdateJudgmentStatusUI();
                }
                
                // Graph 데이터 초기화
                ClearGraphData();
                
                // 테이블 다시 그리기
                CreateDataRows();
                
                // 팝업 메시지 표시
                ShowResetCompleteMessage();
                
                System.Diagnostics.Debug.WriteLine("모든 데이터가 초기화되었습니다.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RESET 버튼 클릭 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// Graph 데이터 초기화 (모든 그래프 포인트 제거)
        /// </summary>
        private void ClearGraphData()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Graph 데이터 초기화 시작");

                // ViewModel의 Graph 데이터 초기화
                if (viewModel != null)
                {
                    viewModel.GraphDataPoints?.Clear();
                    System.Diagnostics.Debug.WriteLine("ViewModel Graph 데이터 초기화 완료");
                }

                // Graph 영역의 모든 Canvas 초기화
                var graphContent = FindName("GraphContent") as Grid;
                if (graphContent != null)
                {
                    ClearAllGraphCanvases(graphContent);
                    System.Diagnostics.Debug.WriteLine("Graph Canvas 초기화 완료");
                }

                System.Diagnostics.Debug.WriteLine("Graph 데이터 초기화 완료");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Graph 데이터 초기화 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 모든 Graph Canvas의 포인트 제거
        /// </summary>
        private void ClearAllGraphCanvases(Grid graphContent)
        {
            try
            {
                // ScrollViewer 찾기
                var scrollViewer = graphContent.Children.OfType<ScrollViewer>().FirstOrDefault();
                if (scrollViewer?.Content is StackPanel mainStackPanel)
                {
                    // 모든 Zone의 Canvas 찾아서 포인트 제거
                    foreach (var child in mainStackPanel.Children)
                    {
                        if (child is Border zoneSection)
                        {
                            var zoneStackPanel = zoneSection.Child as StackPanel;
                            if (zoneStackPanel != null)
                            {
                                foreach (var zoneChild in zoneStackPanel.Children)
                                {
                                    if (zoneChild is Border judgmentRow)
                                    {
                                        var judgmentGrid = judgmentRow.Child as Grid;
                                        if (judgmentGrid?.Children.Count > 1)
                                        {
                                            // Canvas는 두 번째 컬럼에 있음
                                            var canvas = judgmentGrid.Children[1] as Canvas;
                                            if (canvas != null)
                                            {
                                                canvas.Children.Clear();
                                                System.Diagnostics.Debug.WriteLine($"Canvas 초기화: {canvas.Name}");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Canvas 초기화 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// RESET 완료 팝업 메시지 표시
        /// </summary>
        private void ShowResetCompleteMessage()
        {
            try
            {
                System.Windows.MessageBox.Show(
                    "데이터 RESET이 완료되었습니다.",
                    "RESET 완료",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                
                System.Diagnostics.Debug.WriteLine("RESET 완료 팝업 표시됨");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"팝업 표시 오류: {ex.Message}");
            }
        }

        private WadAngle GetWadAngle(string wadValue)
        {
            // INI 파일의 WAD 값에 따른 WadAngle enum 매핑
            switch (wadValue)
            {
                case "0": return WadAngle.Angle0;   // 0도
                case "15": return WadAngle.Angle15; // 15도
                case "30": return WadAngle.Angle30; // 30도
                case "45": return WadAngle.Angle45; // 45도
                case "60": return WadAngle.Angle60; // 60도
                case "A": return WadAngle.AngleA;   // A도
                case "B": return WadAngle.AngleB;   // B도
                default: return WadAngle.Angle0;    // 기본값은 0도
            }
        }

        private int GetWadArrayIndex(string wadValue)
        {
            // WadAngle enum을 배열 인덱스로 변환
            WadAngle angle = GetWadAngle(wadValue);
            return (int)angle;
        }

        private int GetPatternArrayIndex(string category)
        {
            // Category에 따른 패턴 배열 인덱스 매핑
            // [17]:패턴 => 0:W, 1:R, 2:G, 3:B, 4:WG, 5:WG2, 6:WG3 ~ 16:WG13
            switch (category)
            {
                case "W": return 0;   // W
                case "R": return 1;   // R
                case "G": return 2;   // G
                case "B": return 3;   // B
                case "WG": return 4;  // WG
                case "WG2": return 5; // WG2
                case "WG3": return 6; // WG3
                case "WG4": return 7; // WG4
                case "WG5": return 8; // WG5
                case "WG6": return 9; // WG6
                case "WG7": return 10; // WG7
                case "WG8": return 11; // WG8
                case "WG9": return 12; // WG9
                case "WG10": return 13; // WG10
                case "WG11": return 14; // WG11
                case "WG12": return 15; // WG12
                case "WG13": return 16; // WG13
                default: return 0;    // 기본값은 W
            }
        }

        private void UpdateDataForWad(int wadIndex)
        {
            try
            {
                // WadAngle enum으로 변환
                WadAngle angle = (WadAngle)wadIndex;
                
                System.Diagnostics.Debug.WriteLine($"WAD 각도 {angle} (인덱스: {wadIndex})에 해당하는 데이터로 UI 업데이트");
                System.Diagnostics.Debug.WriteLine($"현재 상태 - isTestStarted: {isTestStarted}, zoneTestCompleted: [{string.Join(", ", zoneTestCompleted ?? new bool[0])}], zoneMeasured: [{string.Join(", ", zoneMeasured ?? new bool[0])}]");
                
                int currentZone = viewModel.CurrentZone;
                
                // 테스트가 진행 중이거나 어떤 Zone이라도 테스트 완료된 경우
                if (isTestStarted && zoneTestCompleted != null && currentZone >= 0 && currentZone < zoneTestCompleted.Length && !zoneTestCompleted[currentZone])
                {
                // 테스트 진행 중: 실제 측정이 발생한 존만 업데이트
                if (zoneMeasured != null && currentZone >= 0 && currentZone < zoneMeasured.Length && zoneMeasured[currentZone])
                {
                    System.Diagnostics.Debug.WriteLine($"테스트 진행 중. Zone {currentZone + 1}에 측정 데이터가 있어 업데이트");
                    GenerateDataFromStruct(wadIndex, currentZone);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"테스트 진행 중. 아직 Zone {currentZone + 1}에 측정 데이터 없음 → 스킵");
                }
                }
                else if (zoneTestCompleted != null && zoneTestCompleted.Any(completed => completed))
                {
                    // 테스트 완료된 존들 업데이트 (측정 여부와 관계없이)
                    for (int zone = 0; zone < zoneTestCompleted.Length; zone++)
                    {
                        if (zoneTestCompleted[zone])
                        {
                            System.Diagnostics.Debug.WriteLine($"Zone {zone + 1}이 테스트 완료됨. WAD {wadIndex}로 데이터 업데이트 (측정 여부 무관)");
                            GenerateDataFromStruct(wadIndex, zone);
                        }
                    }
                }
                else
                {
                    // 테스트가 시작되지 않았고 어떤 Zone도 완료되지 않았으면 빈 데이터로 유지
                    System.Diagnostics.Debug.WriteLine($"테스트가 시작되지 않았거나 완료된 Zone이 없음 → 빈 데이터로 유지 (더미 데이터 생성하지 않음)");
                    ClearMeasurementValues();
                }
                
                // 테이블 다시 그리기
                System.Diagnostics.Debug.WriteLine($"CreateDataRows 호출 전 - DataItems 개수: {viewModel?.DataItems?.Count ?? 0}");
                CreateDataRows();
                System.Diagnostics.Debug.WriteLine($"CreateDataRows 호출 후 - DataItems 개수: {viewModel?.DataItems?.Count ?? 0}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WAD 데이터 업데이트 오류: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"스택 트레이스: {ex.StackTrace}");
            }
        }

        private void GenerateDataFromStruct(int wadIndex, int zoneIndex)
        {
            // 구조체 data[wadIndex][patternIndex]에 맞는 데이터 생성
            // 실제로는 DLL에서 data[wadIndex][patternIndex]를 가져와야 함
            
            if (viewModel?.DataItems == null) return;
            
            try
            {
                // INI 파일에서 설정 읽기
                // Category 목록 읽기
                string categoryStr = GlobalDataManager.GetValue("MTP", "Category", "W,R,G,B");
                string[] categories = categoryStr.Split(',').Select(c => c.Trim()).ToArray();
                
                // 지정된 Zone만 업데이트 (zoneIndex는 0-based)
                int targetZone = zoneIndex + 1; // 1-based로 변환
                System.Diagnostics.Debug.WriteLine($"Zone {targetZone} (인덱스: {zoneIndex}) 업데이트");
                
                // Zone별 Cell ID, Inner ID 읽기
                string cellId = GlobalDataManager.GetValue("MTP", $"CELL_ID_ZONE_{targetZone}", "");
                string innerId = GlobalDataManager.GetValue("MTP", $"INNER_ID_ZONE_{targetZone}", "");
                
                System.Diagnostics.Debug.WriteLine($"Zone {targetZone} - Cell ID: {cellId}, Inner ID: {innerId}");
                
                // 해당 Zone의 기존 데이터만 제거하고 올바른 위치에 새 데이터 삽입
                var itemsToRemove = viewModel.DataItems.Where(item => item.Zone == targetZone.ToString()).ToList();
                int insertIndex = 0;
                
                if (itemsToRemove.Count > 0)
                {
                    // 첫 번째 제거할 아이템의 인덱스 찾기
                    insertIndex = viewModel.DataItems.IndexOf(itemsToRemove[0]);
                    
                    // 해당 Zone의 모든 데이터 제거
                    foreach (var item in itemsToRemove)
                    {
                        viewModel.DataItems.Remove(item);
                    }
                }
                
                // 해당 Zone의 각 Category에 대해 구조체 데이터 생성
                for (int i = 0; i < categories.Length; i++)
                {
                    // Category를 패턴 인덱스로 변환
                    int patternIndex = GetPatternArrayIndex(categories[i]);
                    
                    // 실제 저장된 DLL 결과 데이터 사용 (더미 데이터 생성하지 않음)
                    var storedOutput = DllManager.GetStoredZoneResult(targetZone);
                    var structData = GetActualStructData(storedOutput, wadIndex, patternIndex, targetZone);
                    
                    var item = new DataTableItem
                    {
                        Zone = targetZone.ToString(),
                        CellId = cellId,
                        InnerId = innerId,
                        Category = categories[i],
                        X = structData.X,
                        Y = structData.Y,
                        L = structData.L,
                        Current = structData.Current,
                        Efficiency = structData.Efficiency,
                        ErrorName = structData.ErrorName,
                        Tact = structData.Tact,
                        Judgment = structData.Judgment
                    };
                    
                    // 올바른 위치에 삽입 (Zone 순서 유지)
                    viewModel.DataItems.Insert(insertIndex + i, item);
                }
                
                // Zone 전체 판정을 모든 아이템에 적용 (일관성 유지)
                var zoneItems = viewModel.DataItems.Where(item => item.Zone == targetZone.ToString()).ToList();
                if (zoneItems.Count > 0)
                {
                    // Zone 전체 판정 계산
                    var storedOutput = DllManager.GetStoredZoneResult(targetZone);
                    if (storedOutput.HasValue)
                    {
                        int[,] resultArray = new int[7, 17];
                        for (int wad = 0; wad < 7; wad++)
                        {
                            for (int pattern = 0; pattern < 17; pattern++)
                            {
                                int index = wad * 17 + pattern;
                                if (index < storedOutput.Value.data.Length)
                                {
                                    resultArray[wad, pattern] = storedOutput.Value.data[index].result;
                                }
                            }
                        }
                        string zoneJudgment = OpticJudgment.Instance.JudgeZoneFromResults(resultArray);
                        
                        // 모든 아이템에 Zone 전체 판정 적용
                        foreach (var item in zoneItems)
                        {
                            item.Judgment = zoneJudgment;
                        }
                        System.Diagnostics.Debug.WriteLine($"Zone {targetZone} WAD 변경 시 전체 판정 적용: {zoneJudgment}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"구조체 데이터 생성 오류: {ex.Message}");
                
                // 오류 발생 시 기본값 사용
                GenerateDefaultEmptyData();
            }
        }

        private StructPatternData GetActualStructData(Output? storedOutput, int wadIndex, int patternIndex, int zoneNumber)
        {
            // 실제 저장된 DLL 결과 데이터 사용 (더미 데이터 생성하지 않음)
            if (storedOutput.HasValue && storedOutput.Value.data != null)
            {
                // data 배열에서 올바른 인덱스로 패턴 데이터 가져오기
                // data[wadIndex * 17 + patternIndex] 형태로 저장됨
                int dataIndex = wadIndex * 17 + patternIndex;
                
                if (dataIndex < storedOutput.Value.data.Length)
                {
                    var pattern = storedOutput.Value.data[dataIndex];
                    
                    // Zone별로 동일한 TACT 계산 (wadIndex와 관계없이 Zone 기준)
                    DateTime startTime = DllManager.GetZoneSeqStartTime(zoneNumber);
                    DateTime endTime = DllManager.GetZoneSeqEndTime(zoneNumber);
                    double tactSeconds = (endTime - startTime).TotalSeconds;
                    
                    // System.Diagnostics.Debug.WriteLine($"Zone {zoneNumber} TACT 계산: 시작={startTime:HH:mm:ss.fff}, 종료={endTime:HH:mm:ss.fff}, TACT={tactSeconds:F3}초");
                    // System.Diagnostics.Debug.WriteLine($"Zone {zoneNumber}, WAD {wadIndex}, Pattern {patternIndex} 데이터: x={pattern.x}, y={pattern.y}, L={pattern.L}, result={pattern.result}");
                    
                    return new StructPatternData
                    {
                        X = pattern.x.ToString("F2"),
                        Y = pattern.y.ToString("F2"),
                        L = pattern.L.ToString("F2"),
                        Current = pattern.cur.ToString("F3"),
                        Efficiency = pattern.eff.ToString("F2"),
                        ErrorName = "", // Pattern 구조체에는 error_name이 없음
                        Tact = tactSeconds.ToString("F3"),
                        Judgment = OpticJudgment.Instance.GetPatternJudgment(pattern.result) // 올바른 판정 로직 사용
                    };
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"데이터 인덱스 {dataIndex}가 범위를 벗어남 (총 길이: {storedOutput.Value.data.Length})");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"저장된 출력 데이터가 없음 또는 data 배열이 null");
            }
            
            // 저장된 데이터가 없으면 빈 데이터 반환 (더미 데이터 생성하지 않음)
                return new StructPatternData
                {
                    X = "",
                    Y = "",
                    L = "",
                    Current = "",
                    Efficiency = "",
                    ErrorName = "",
                    Tact = "",
                    Judgment = ""
            };
        }

        // Zone별 테스트 상태 초기화
        private void InitializeZoneTestStates()
        {
            try
            {
                // INI 파일에서 Zone 개수 읽기
                int zoneCount = int.Parse(GlobalDataManager.GetValue("Settings", "MTP_ZONE", "2"));
                
                // Zone별 테스트 완료/측정 여부 배열 초기화 (모두 false)
                zoneTestCompleted = new bool[zoneCount];
                zoneMeasured = new bool[zoneCount];
                for (int i = 0; i < zoneCount; i++)
                {
                    zoneTestCompleted[i] = false;
                    zoneMeasured[i] = false;
                }
                
                System.Diagnostics.Debug.WriteLine($"Zone별 테스트 상태 초기화 완료. Zone 개수: {zoneCount}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Zone별 테스트 상태 초기화 오류: {ex.Message}");
                // 기본값으로 2개 Zone 설정
                zoneTestCompleted = new bool[2];
                zoneMeasured = new bool[2];
            }
        }

        // 테스트 시작 메서드 (UI 스레드 블로킹 방지)
        public void StartTest()
        {
            isTestStarted = true;
            System.Diagnostics.Debug.WriteLine("테스트 시작 - 백그라운드 실행");

            // 완전히 백그라운드 스레드에서 실행 (UI 스레드 블로킹 방지)
            Task.Run(async () =>
            {
                try
                {
                    await StartTestAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"테스트 실행 오류: {ex.Message}");
                    Dispatcher?.BeginInvoke(new Action(() =>
                    {
                        MessageBox.Show($"테스트 실행 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    }));
                }
            });
        }

        // 실제 테스트 로직 (비동기)
        private async Task StartTestAsync()
        {
            // 기본 INI 경로: MTP_PATHS.RECIPE_FOLDER → 기본값
            string iniPath = GlobalDataManager.GetValue("MTP_PATHS", "RECIPE_FOLDER", @"D:\\Project\\Recipe\\OptiX.ini");
            if (string.IsNullOrWhiteSpace(iniPath)) iniPath = @"D:\\Project\\Recipe\\OptiX.ini";

            // 존 목록 파싱: Settings.MTP_ZONE에서 Zone 개수 읽어서 1..N 생성
            string zoneCountStr = GlobalDataManager.GetValue("Settings", "MTP_ZONE", "1");
            if (!int.TryParse(zoneCountStr, out int zoneCount) || zoneCount < 1) zoneCount = 1;
            int[] zones = Enumerable.Range(1, zoneCount).ToArray();
            
            System.Diagnostics.Debug.WriteLine($"SEQ Zones: {string.Join(",", zones)}");
            System.Diagnostics.Debug.WriteLine($"Zone 2가 실행될 예정인가? {zones.Contains(2)}");
            
            // 캐시된 시퀀스 사용
            var orderedSeq = OpticPageViewModel.GetCachedSequence().ToList();
            System.Diagnostics.Debug.WriteLine($"캐시된 시퀀스 사용 - 총 {orderedSeq.Count}개 시퀀스");

            // 각 Zone을 독립적으로 실행 → 모든 Zone 실시간 MONITOR 로그 표시
            var tasks = new List<Task>();
            
            System.Diagnostics.Debug.WriteLine($"실행할 Zone 목록: {string.Join(", ", zones)}");
            
            foreach (int zoneId in zones)
            {
                System.Diagnostics.Debug.WriteLine($"[Zone {zoneId}] 시작 - 현재 스레드: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
                
                var task = Task.Run(async () =>
                {
                    try
                    {
                        await ExecuteSeqForZone(zoneId, orderedSeq);
                        System.Diagnostics.Debug.WriteLine($"[Zone {zoneId}] 완료");
                        
                        // 존 완료 표시 (UI 스레드에서 - 우선순위 낮게)
                        Dispatcher?.BeginInvoke(new Action(() =>
                        {
                            SetZoneTestCompleted(zoneId - 1, true);
                        }), DispatcherPriority.Send);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Zone {zoneId} SEQ 실행 오류: {ex.Message}");
                    }
                });
                
                tasks.Add(task);
            }
            
            // 모든 Zone이 완료될 때까지 대기 (UI 스레드 블로킹 방지)
            await Task.WhenAll(tasks).ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine("모든 Zone SEQ 완료");
            
            // 모든 Zone 완료 후 로그 생성 (SEQ에 MAKE_RESULT_LOG이 없어도 항상 실행)
            System.Diagnostics.Debug.WriteLine("SEQ 완료 후 로그 생성 시작 (MAKE_RESULT_LOG과 무관)");
            await CreateAllResultLogs(zones).ConfigureAwait(false);

            // 모든 존 완료 후 테이블 렌더링 (UI 스레드에서 - 우선순위 낮게)
            Dispatcher?.BeginInvoke(new Action(() =>
            {
                // 각 Zone의 저장된 DLL 결과를 ViewModel에 전달하여 UI 업데이트
                if (viewModel != null)
                {
                    foreach (int zoneNumber in zones)
                    {
                        var storedOutput = DllManager.GetStoredZoneResult(zoneNumber);
                        if (storedOutput.HasValue)
                        {
                            viewModel.UpdateDataTableWithDllResult(storedOutput.Value, zoneNumber);
                            
                            // 그래프 데이터 포인트 추가
                            var judgment = viewModel.GetJudgmentForZone(zoneNumber);
                            if (!string.IsNullOrEmpty(judgment))
                            {
                                viewModel.AddGraphDataPoint(zoneNumber, judgment);
                                System.Diagnostics.Debug.WriteLine($"Zone {zoneNumber} 그래프 데이터 추가: {judgment}");
                            }
                            
                            System.Diagnostics.Debug.WriteLine($"Zone {zoneNumber} DLL 결과로 UI 업데이트 완료");
                        }
                    }
                }
                
                // 모든 Zone 처리 완료 후 그래프 업데이트
                if (viewModel != null && viewModel.GraphDataPoints != null && viewModel.GraphDataPoints.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"그래프 표시 업데이트 호출: {viewModel.GraphDataPoints.Count}개 데이터 포인트");
                    UpdateGraphDisplay(viewModel.GraphDataPoints);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("그래프 데이터 포인트가 없습니다.");
                }
                
            int wadIndex = WadComboBox.SelectedIndex >= 0 ? WadComboBox.SelectedIndex : 0;
            UpdateDataForWad(wadIndex);
            CreateDataRows();
            System.Diagnostics.Debug.WriteLine("All zones completed → redraw once");
            }), DispatcherPriority.Send);
        }
        
        /// <summary>
        /// 모든 Zone의 결과 로그를 한 번에 생성 (SEQ 완료 후 호출)
        /// </summary>
        private async Task CreateAllResultLogs(int[] zones)
        {
            await Task.Run(() =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[로그 생성] 모든 Zone의 로그 생성 시작 (총 {zones.Length}개 Zone)");
                    
                    // 전체 SEQ 종료 시간 설정 (로그 생성 시작 시점)
                    DateTime seqEndTime = DateTime.Now;
                    DllManager.SetSeqEndTime(seqEndTime);
                    System.Diagnostics.Debug.WriteLine($"전체 SEQ 종료 시간 설정: {seqEndTime:HH:mm:ss.fff}");
                    
                    DateTime seqStartTime = DllManager.GetSeqStartTime();
                    
                    // 각 Zone의 로그를 순차적으로 생성
                    foreach (int zoneNumber in zones)
                    {
                        // 로그 작성 진입부에서 Zone 종료 시간 설정
                        DateTime logEntryTime = DateTime.Now;
                        DllManager.SetZoneSeqEndTime(zoneNumber, logEntryTime);
                        System.Diagnostics.Debug.WriteLine($"Zone {zoneNumber} 종료 시간 설정 (로그 진입부): {logEntryTime:HH:mm:ss.fff}");
                        
                        System.Diagnostics.Debug.WriteLine($"[로그 생성] Zone {zoneNumber} 로그 생성 중...");
                        
                        // DllManager에서 저장된 OUTPUT 데이터 가져오기
                        var storedOutput = DllManager.GetStoredZoneResult(zoneNumber);
                        
                        if (storedOutput.HasValue)
                        {
                            // Zone 정보 가져오기
                            var (cellId, innerId) = GlobalDataManager.GetZoneInfo(zoneNumber);
                            System.Diagnostics.Debug.WriteLine($"[로그 생성] Zone {zoneNumber} - Cell ID: '{cellId}', Inner ID: '{innerId}'");
                            
                            // Cell ID와 Inner ID가 비어있으면 기본값 사용
                            if (string.IsNullOrEmpty(cellId))
                            {
                                cellId = $"DEFAULT_CELL_{zoneNumber}";
                                System.Diagnostics.Debug.WriteLine($"[로그 생성] Zone {zoneNumber} Cell ID가 비어있어서 기본값 사용: {cellId}");
                            }
                            if (string.IsNullOrEmpty(innerId))
                            {
                                innerId = $"DEFAULT_INNER_{zoneNumber}";
                                System.Diagnostics.Debug.WriteLine($"[로그 생성] Zone {zoneNumber} Inner ID가 비어있어서 기본값 사용: {innerId}");
                            }
                            
                            // Zone별 SEQ 시작 시간과 종료 시간 사용
                            DateTime zoneSeqStartTime = DllManager.GetZoneSeqStartTime(zoneNumber);
                            DateTime zoneSeqEndTime = DllManager.GetZoneSeqEndTime(zoneNumber);
                            
                            // 로그 생성 함수 호출 (순차 실행)
                            bool success = DllManager.CreateResultLogsForZone(
                                zoneSeqStartTime, 
                                zoneSeqEndTime, 
                                cellId, 
                                innerId, 
                                zoneNumber, 
                                storedOutput.Value
                            );
                            
                            if (success)
                            {
                                System.Diagnostics.Debug.WriteLine($"[로그 생성] Zone {zoneNumber} 로그 생성 완료 ✓");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[로그 생성] Zone {zoneNumber} 로그 생성 실패 ✗");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[로그 생성] Zone {zoneNumber} 저장된 OUTPUT 데이터 없음");
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[로그 생성] 모든 Zone 로그 생성 완료");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[로그 생성] 오류 발생: {ex.Message}");
                }
            });
        }
        
        // 테스트 중지 메서드
        public void StopTest()
        {
            isTestStarted = false;
            System.Diagnostics.Debug.WriteLine("테스트 중지됨");
            
            // 모든 측정값 클리어
            ClearMeasurementValues();
            CreateDataRows();
        }
        
        // 특정 Zone의 테스트 완료 상태 설정
        public void SetZoneTestCompleted(int zoneIndex, bool completed)
        {
            if (zoneIndex >= 0 && zoneIndex < zoneTestCompleted.Length)
            {
                zoneTestCompleted[zoneIndex] = completed;
                System.Diagnostics.Debug.WriteLine($"Zone {zoneIndex + 1} 테스트 완료 상태: {completed}");
            }
        }
        
        // 특정 Zone의 테스트 완료 상태 확인
        public bool IsZoneTestCompleted(int zoneIndex)
        {
            if (zoneIndex >= 0 && zoneIndex < zoneTestCompleted.Length)
            {
                return zoneTestCompleted[zoneIndex];
            }
            return false;
        }
        

        // 구조체 패턴 데이터를 위한 클래스
        public class StructPatternData
        {
            public string X { get; set; } = "";
            public string Y { get; set; } = "";
            public string L { get; set; } = "";
            public string Current { get; set; } = "";
            public string Efficiency { get; set; } = "";
            public string ErrorName { get; set; } = "";
            public string Tact { get; set; } = "";
            public string Judgment { get; set; } = "";
        }


        private void ClearMeasurementValues()
        {
            // WAD 변경 시 측정값만 클리어하고 구조는 유지
            if (viewModel?.DataItems == null) return;
            
            foreach (var item in viewModel.DataItems)
            {
                // 측정값만 클리어 (Zone, Cell ID, Inner ID, Category는 유지)
                item.X = "";
                item.Y = "";
                item.L = "";
                item.Current = "";
                item.Efficiency = "";
                item.ErrorName = "";
                item.Tact = "";
                item.Judgment = "";
            }
        }


        private void GenerateDefaultEmptyData()
        {
            // 오류 발생 시 사용할 기본 빈 데이터
            if (viewModel?.DataItems == null) return;
            
            viewModel.DataItems.Clear();
            
            var categories = new[] { "W", "WG", "R", "G", "B" };
            
            // Zone 1 빈 데이터 생성
            for (int i = 0; i < categories.Length; i++)
            {
                var item = new DataTableItem
                {
                    Zone = "1",
                    CellId = "",
                    InnerId = "",
                    Category = categories[i],
                    X = "",
                    Y = "",
                    L = "",
                    Current = "",
                    Efficiency = "",
                    ErrorName = "",
                    Tact = "",
                    Judgment = ""
                };
                viewModel.DataItems.Add(item);
            }
            
            // Zone 2 빈 데이터 생성
            for (int i = 0; i < categories.Length; i++)
            {
                var item = new DataTableItem
                {
                    Zone = "2",
                    CellId = "",
                    InnerId = "",
                    Category = categories[i],
                    X = "",
                    Y = "",
                    L = "",
                    Current = "",
                    Efficiency = "",
                    ErrorName = "",
                    Tact = "",
                    Judgment = ""
                };
                viewModel.DataItems.Add(item);
            }
        }

        /// <summary>
        /// OpticPage에 언어 적용
        /// </summary>
        public void ApplyLanguage()
        {
            try
            {
                // 뒤로가기 버튼
                if (BackButton != null)
                {
                    var textBlock = BackButton.Content as TextBlock;
                    if (textBlock != null)
                        textBlock.Text = LanguageManager.GetText("OpticPage.Back");
                }

                // 모니터 탭 라벨은 XAML 고정값 사용

                // 데이터 테이블 제목
                var dataTableTitle = FindName("DataTableTitle") as System.Windows.Controls.TextBlock;
                if (dataTableTitle != null)
                    dataTableTitle.Text = LanguageManager.GetText("OpticPage.CharacteristicDataTable");
                
                // WAD 라벨
                var wadLabel = FindName("WadLabel") as System.Windows.Controls.TextBlock;
                if (wadLabel != null)
                    wadLabel.Text = LanguageManager.GetText("OpticPage.WAD");

                // RESET 버튼
                if (ResetButton != null)
                    ResetButton.Content = LanguageManager.GetText("OpticPage.Reset");

                // 컨트롤 패널 버튼들
                var settingButton = FindName("SettingButton") as System.Windows.Controls.Button;
                if (settingButton != null)
                    settingButton.Content = LanguageManager.GetText("OpticPage.Setting");

                var pathButton = FindName("PathButton") as System.Windows.Controls.Button;
                if (pathButton != null)
                    pathButton.Content = LanguageManager.GetText("OpticPage.Path");

                var startButton = FindName("StartButton") as System.Windows.Controls.Button;
                if (startButton != null)
                    startButton.Content = LanguageManager.GetText("OpticPage.Start");

                var stopButton = FindName("StopButton") as System.Windows.Controls.Button;
                if (stopButton != null)
                    stopButton.Content = LanguageManager.GetText("OpticPage.Stop");

                var chartButton = FindName("ChartButton") as System.Windows.Controls.Button;
                if (chartButton != null)
                    chartButton.Content = LanguageManager.GetText("OpticPage.Chart");

                var reportButton = FindName("ReportButton") as System.Windows.Controls.Button;
                if (reportButton != null)
                    reportButton.Content = LanguageManager.GetText("OpticPage.Report");

                var exitButton = FindName("ExitButton") as System.Windows.Controls.Button;
                if (exitButton != null)
                    exitButton.Content = LanguageManager.GetText("OpticPage.Exit");

                // 특성 판정 현황 제목
                var judgmentStatusTitle = FindName("JudgmentStatusTitle") as System.Windows.Controls.TextBlock;
                if (judgmentStatusTitle != null)
                    judgmentStatusTitle.Text = LanguageManager.GetText("OpticPage.CharacteristicJudgmentStatus");

                // 수량, 발생률 헤더
                var quantityHeader = FindName("QuantityHeader") as System.Windows.Controls.TextBlock;
                if (quantityHeader != null)
                    quantityHeader.Text = LanguageManager.GetText("OpticPage.Quantity");

                var occurrenceRateHeader = FindName("OccurrenceRateHeader") as System.Windows.Controls.TextBlock;
                if (occurrenceRateHeader != null)
                    occurrenceRateHeader.Text = LanguageManager.GetText("OpticPage.OccurrenceRate");

                // 컨트롤 패널 제목
                var controlPanelTitle = FindName("ControlPanelTitle") as System.Windows.Controls.TextBlock;
                if (controlPanelTitle != null)
                    controlPanelTitle.Text = LanguageManager.GetText("OpticPage.ControlPanel");

                System.Diagnostics.Debug.WriteLine($"OpticPage 언어 적용 완료: {LanguageManager.CurrentLanguage}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OpticPage 언어 적용 오류: {ex.Message}");
            }
        }

        #region 판정 현황 표 업데이트
        /// <summary>
        /// 판정 현황 표의 특정 행 업데이트
        /// </summary>
        /// <param name="rowName">행 이름 (Total, OK, R/J, PTN)</param>
        /// <param name="quantity">수량</param>
        /// <param name="rate">발생률</param>
        public void UpdateJudgmentStatusRow(string rowName, string quantity, string rate)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"판정 현황 업데이트: {rowName} - 수량: {quantity}, 비율: {rate}");
                
                // XAML에서 정의된 TextBlock들을 직접 찾아서 업데이트
                switch (rowName)
                {
                    case "Total":
                        var totalQty = this.FindName("TotalQuantity") as TextBlock;
                        var totalRate = this.FindName("TotalRate") as TextBlock;
                        if (totalQty != null) totalQty.Text = quantity;
                        if (totalRate != null) totalRate.Text = rate;
                        break;
                        
                    case "PTN":
                        var ptnQty = this.FindName("PTNQuantity") as TextBlock;
                        var ptnRate = this.FindName("PTNRate") as TextBlock;
                        if (ptnQty != null) ptnQty.Text = quantity;
                        if (ptnRate != null) ptnRate.Text = rate;
                        break;
                        
                    case "R/J":
                        var rjQty = this.FindName("RJQuantity") as TextBlock;
                        var rjRate = this.FindName("RJRate") as TextBlock;
                        if (rjQty != null) rjQty.Text = quantity;
                        if (rjRate != null) rjRate.Text = rate;
                        break;
                        
                    case "OK":
                        var okQty = this.FindName("OKQuantity") as TextBlock;
                        var okRate = this.FindName("OKRate") as TextBlock;
                        if (okQty != null) okQty.Text = quantity;
                        if (okRate != null) okRate.Text = rate;
                        break;
                }
                
                System.Diagnostics.Debug.WriteLine($"판정 현황 업데이트 완료: {rowName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"판정 현황 행 업데이트 오류 ({rowName}): {ex.Message}");
            }
        }

        /// <summary>
        /// 판정 현황 표의 TextBlock들을 직접 찾아서 업데이트
        /// </summary>
        private void UpdateJudgmentStatusTextBlocks(string rowName, string quantity, string rate)
        {
            try
            {
                // XAML에서 정의된 판정 현황 표의 TextBlock들을 찾기
                // 하단 가운데 영역에서 판정 현황 표 찾기
                var judgmentStatusContainer = this.FindName("JudgmentStatusContainer") as Grid;
                if (judgmentStatusContainer == null)
                {
                    // 대안: 하단 가운데 컬럼에서 찾기
                    var mainGrid = this.Content as Grid;
                    if (mainGrid != null && mainGrid.Children.Count > 0)
                    {
                        var bottomGrid = mainGrid.Children.OfType<Grid>().FirstOrDefault();
                        if (bottomGrid != null && bottomGrid.ColumnDefinitions.Count >= 3)
                        {
                            // 가운데 컬럼 (인덱스 1)에서 판정 현황 표 찾기
                            var middleColumnChildren = bottomGrid.Children.Cast<UIElement>()
                                .Where(child => Grid.GetColumn(child) == 1).ToList();
                            
                            foreach (var child in middleColumnChildren)
                            {
                                if (child is Border border)
                                {
                                    judgmentStatusContainer = FindGridInBorder(border);
                                    if (judgmentStatusContainer != null) break;
                                }
                            }
                        }
                    }
                }

                if (judgmentStatusContainer != null)
                {
                    UpdateTextBlocksInGrid(judgmentStatusContainer, rowName, quantity, rate);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("판정 현황 표 컨테이너를 찾을 수 없음");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"판정 현황 TextBlock 업데이트 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// Border 내부에서 Grid 찾기
        /// </summary>
        private Grid FindGridInBorder(Border border)
        {
            if (border.Child is Grid grid)
            {
                return grid;
            }
            else if (border.Child is FrameworkElement element)
            {
                // 재귀적으로 Grid 찾기
                return FindGridInElement(element);
            }
            return null;
        }

        /// <summary>
        /// FrameworkElement 내부에서 Grid 찾기
        /// </summary>
        private Grid FindGridInElement(FrameworkElement element)
        {
            if (element is Grid grid)
            {
                return grid;
            }
            else if (element is Panel panel)
            {
                foreach (var child in panel.Children.OfType<FrameworkElement>())
                {
                    var found = FindGridInElement(child);
                    if (found != null) return found;
                }
            }
            return null;
        }

        /// <summary>
        /// Grid 내부의 TextBlock들 업데이트
        /// </summary>
        private void UpdateTextBlocksInGrid(Grid grid, string rowName, string quantity, string rate)
        {
            try
            {
                // Grid의 모든 TextBlock 찾기
                var allTextBlocks = FindAllTextBlocks(grid);
                
                // 행 이름에 해당하는 TextBlock들 찾기
                var targetRow = allTextBlocks.Where(tb => tb.Text == rowName).FirstOrDefault();
                if (targetRow != null)
                {
                    // 같은 행의 다른 TextBlock들 찾기
                    var rowIndex = Grid.GetRow(targetRow);
                    var rowTextBlocks = allTextBlocks.Where(tb => Grid.GetRow(tb) == rowIndex).ToList();
                    
                    if (rowTextBlocks.Count >= 3)
                    {
                        // 수량과 발생률 TextBlock 업데이트
                        rowTextBlocks[1].Text = quantity; // 수량
                        rowTextBlocks[2].Text = rate;     // 발생률
                        
                        System.Diagnostics.Debug.WriteLine($"판정 현황 업데이트 완료: {rowName} - {quantity}, {rate}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Grid 내부 TextBlock 업데이트 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// Grid 내부의 모든 TextBlock 찾기
        /// </summary>
        private List<TextBlock> FindAllTextBlocks(DependencyObject parent)
        {
            var textBlocks = new List<TextBlock>();
            
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                if (child is TextBlock textBlock)
                {
                    textBlocks.Add(textBlock);
                }
                else
                {
                    textBlocks.AddRange(FindAllTextBlocks(child));
                }
            }
            
            return textBlocks;
        }

        /// <summary>
        /// 판정 현황 표 그리드 찾기
        /// </summary>
        private Grid FindStatusTableGrid()
        {
            try
            {
                // XAML에서 정의된 판정 현황 표의 그리드 찾기
                // 판정 현황 표는 하단 가운데 영역에 위치
                var judgmentStatusPanel = this.FindName("JudgmentStatusPanel") as Grid;
                if (judgmentStatusPanel != null)
                {
                    return judgmentStatusPanel;
                }
                
                // 대안: LogicalTreeHelper를 사용하여 찾기
                var statusTable = LogicalTreeHelper.FindLogicalNode(this, "JudgmentStatusTable") as Grid;
                return statusTable;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"판정 현황 표 그리드 찾기 오류: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 판정 현황 표 행 인덱스 가져오기
        /// </summary>
        private int GetStatusTableRowIndex(string rowName)
        {
            switch (rowName)
            {
                case "Total": return 0;
                case "PTN": return 1;
                case "R/J": return 2;
                case "OK": return 3;
                default: return -1;
            }
        }

        /// <summary>
        /// 판정 현황 표 셀 업데이트
        /// </summary>
        private void UpdateStatusTableCell(Grid grid, int row, int column, string value)
        {
            try
            {
                // 그리드에서 해당 위치의 TextBlock 찾아서 업데이트
                var children = grid.Children.Cast<UIElement>().Where(child => 
                    Grid.GetRow(child) == row && Grid.GetColumn(child) == column).ToList();
                
                foreach (var child in children)
                {
                    if (child is Border border && border.Child is Grid innerGrid)
                    {
                        var textBlocks = innerGrid.Children.OfType<TextBlock>().ToList();
                        if (textBlocks.Count > column)
                        {
                            textBlocks[column].Text = value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"판정 현황 표 셀 업데이트 오류: {ex.Message}");
            }
        }
        #endregion

        #region 그래프 영역 업데이트
        /// <summary>
        /// 그래프 표시 업데이트
        /// </summary>
        /// <param name="dataPoints">그래프 데이터 포인트들</param>
        public void UpdateGraphDisplay(List<ViewModels.OpticPageViewModel.GraphDataPoint> dataPoints)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"그래프 표시 업데이트 호출됨: {dataPoints?.Count ?? 0}개 데이터 포인트");

                // Graph 탭이 활성화되어 있을 때만 업데이트
                var graphContent = FindName("GraphContent") as Grid;
                if (graphContent == null)
                {
                    System.Diagnostics.Debug.WriteLine("GraphContent를 찾을 수 없음");
                    return;
                }

                // 기존 그래프 영역에 데이터 포인트만 업데이트 (빠른 업데이트)
                UpdateGraphDataPoints(dataPoints);
                
                if (graphContent.Visibility != Visibility.Visible)
                {
                    System.Diagnostics.Debug.WriteLine("Graph 탭이 현재 보이지 않음 - 데이터는 업데이트됨, 탭 전환 시 표시됨");
                }
                System.Diagnostics.Debug.WriteLine("그래프 표시 업데이트 완료");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"그래프 표시 업데이트 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 페이지 진입 시 기존 Graph 데이터 자동 복원
        /// </summary>
        private void RestoreExistingGraphData()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("기존 Graph 데이터 복원 시작");

                if (viewModel?.GraphDataPoints == null || viewModel.GraphDataPoints.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("복원할 Graph 데이터가 없습니다.");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"복원할 Graph 데이터: {viewModel.GraphDataPoints.Count}개 포인트");

                // 기존 데이터를 사용하여 그래프 업데이트
                UpdateGraphDataPoints(viewModel.GraphDataPoints);

                System.Diagnostics.Debug.WriteLine("기존 Graph 데이터 복원 완료");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"기존 Graph 데이터 복원 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// Zone 개수에 따라 동적으로 그래프 생성
        /// </summary>
        private void CreateDynamicGraph(List<ViewModels.OpticPageViewModel.GraphDataPoint> dataPoints)
        {
            try
            {
                var graphContent = FindName("GraphContent") as Grid;
                if (graphContent == null) return;

                // 기존 그래프 내용 클리어
                graphContent.Children.Clear();

                // Zone 개수 읽기
                int zoneCount = 2;
                try
                {
                    string zoneCountStr = GlobalDataManager.GetValue("Settings", "MTP_ZONE", "2");
                    if (!int.TryParse(zoneCountStr, out zoneCount)) zoneCount = 2;
                }
                catch { zoneCount = 2; }

                // Zone별 그래프 행 생성
                for (int zone = 1; zone <= zoneCount; zone++)
                {
                    CreateZoneGraphRow(graphContent, zone, dataPoints);
                }

                // OK, R/J, PTN 행 생성
                CreateJudgmentRows(graphContent, dataPoints);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"동적 그래프 생성 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// Zone별 그래프 행 생성
        /// </summary>
        private void CreateZoneGraphRow(Grid parentGrid, int zoneNumber, List<ViewModels.OpticPageViewModel.GraphDataPoint> dataPoints)
        {
            try
            {
                // Zone 행 추가
                parentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(60) });

                var zoneBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Margin = new Thickness(4, 2, 4, 2)
                };

                var zonePanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(8, 4, 8, 4)
                };

                // Zone 라벨
                var zoneLabel = new TextBlock
                {
                    Text = $"Zone{zoneNumber}",
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                    VerticalAlignment = VerticalAlignment.Center,
                    Width = 60
                };

                zonePanel.Children.Add(zoneLabel);

                // Zone별 데이터 포인트 표시 (최근 300개)
                var zoneDataPoints = dataPoints.Where(dp => dp.ZoneNumber == zoneNumber).ToList();
                if (zoneDataPoints.Count > 300)
                {
                    zoneDataPoints = zoneDataPoints.Skip(zoneDataPoints.Count - 300).ToList();
                }
                CreateDataPointsDisplay(zonePanel, zoneDataPoints);

                zoneBorder.Child = zonePanel;
                Grid.SetRow(zoneBorder, zoneNumber - 1);
                parentGrid.Children.Add(zoneBorder);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Zone 그래프 행 생성 오류 (Zone{zoneNumber}): {ex.Message}");
            }
        }

        /// <summary>
        /// OK, R/J, PTN 판정 행 생성
        /// </summary>
        private void CreateJudgmentRows(Grid parentGrid, List<ViewModels.OpticPageViewModel.GraphDataPoint> dataPoints)
        {
            try
            {
                string[] judgmentTypes = { "OK", "R/J", "PTN" };
                Color[] judgmentColors = { 
                    Color.FromRgb(76, 175, 80),   // OK - 초록색
                    Color.FromRgb(244, 67, 54),   // R/J - 빨간색  
                    Color.FromRgb(255, 193, 7)    // PTN - 노란색
                };

                for (int i = 0; i < judgmentTypes.Length; i++)
                {
                    // 판정 행 추가
                    parentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });

                    var judgmentBorder = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(6),
                        Margin = new Thickness(4, 2, 4, 2)
                    };

                    var judgmentPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        Margin = new Thickness(8, 4, 8, 4)
                    };

                    // 판정 라벨
                    var judgmentLabel = new TextBlock
                    {
                        Text = judgmentTypes[i],
                        FontWeight = FontWeights.Bold,
                        FontSize = 12,
                        Foreground = new SolidColorBrush(judgmentColors[i]),
                        VerticalAlignment = VerticalAlignment.Center,
                        Width = 60
                    };

                    judgmentPanel.Children.Add(judgmentLabel);

                    // 해당 판정의 데이터 포인트 표시 (최근 300개)
                    var judgmentDataPoints = dataPoints.Where(dp => dp.Judgment == judgmentTypes[i]).ToList();
                    if (judgmentDataPoints.Count > 300)
                    {
                        judgmentDataPoints = judgmentDataPoints.Skip(judgmentDataPoints.Count - 300).ToList();
                    }
                    CreateDataPointsDisplay(judgmentPanel, judgmentDataPoints);

                    judgmentBorder.Child = judgmentPanel;
                    Grid.SetRow(judgmentBorder, parentGrid.RowDefinitions.Count - 1);
                    parentGrid.Children.Add(judgmentBorder);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"판정 행 생성 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 데이터 포인트 표시 생성
        /// </summary>
        private void CreateDataPointsDisplay(StackPanel parentPanel, List<ViewModels.OpticPageViewModel.GraphDataPoint> dataPoints)
        {
            try
            {
                var canvas = new Canvas
                {
                    Height = 30,
                    Width = 800,
                    Background = new SolidColorBrush(Color.FromRgb(248, 250, 252))
                };

                // 최근 300개 데이터 포인트를 점으로 표시
                int pointCount = Math.Min(dataPoints.Count, 300);
                double pointWidth = canvas.Width / 300.0;

                for (int i = 0; i < pointCount; i++)
                {
                    var dataPoint = dataPoints[i];
                    double x = i * pointWidth;
                    
                    var ellipse = new System.Windows.Shapes.Ellipse
                    {
                        Width = 4,
                        Height = 4,
                        Fill = GetJudgmentColor(dataPoint.Judgment),
                        Stroke = GetJudgmentColor(dataPoint.Judgment),
                        StrokeThickness = 1
                    };

                    Canvas.SetLeft(ellipse, x);
                    Canvas.SetTop(ellipse, 13); // 중앙 정렬

                    canvas.Children.Add(ellipse);
                }

                parentPanel.Children.Add(canvas);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"데이터 포인트 표시 생성 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 판정 결과에 따른 색상 반환
        /// </summary>
        private SolidColorBrush GetJudgmentColor(string judgment)
        {
            switch (judgment)
            {
                case "OK":
                    return new SolidColorBrush(Color.FromRgb(76, 175, 80));   // 초록색
                case "NG":
                case "R/J":
                case "RJ":  // WPF Name 속성용
                    return new SolidColorBrush(Color.FromRgb(244, 67, 54));   // 빨간색
                case "PTN":
                    return new SolidColorBrush(Color.FromRgb(255, 193, 7));   // 노란색
                default:
                    return new SolidColorBrush(Color.FromRgb(158, 158, 158)); // 회색
            }
        }
        #endregion

        
    }

    // 내부 SEQ 실행 헬퍼들
    public partial class OpticPage
    {
        private async Task ExecuteSeqForZoneAsync(int zoneId, System.Collections.Generic.List<string> orderedSeq)
        {
            try
            {
                await ExecuteSeqForZone(zoneId, orderedSeq);
                // 존 완료 표시
                SetZoneTestCompleted(zoneId - 1, true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Zone {zoneId} SEQ 실행 오류: {ex.Message}");
            }
        }

        private async Task ExecuteSeqForZone(int zoneId, System.Collections.Generic.List<string> orderedSeq)
        {
            System.Diagnostics.Debug.WriteLine($"[Zone {zoneId}] ExecuteSeqForZone 시작 - 스레드: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
            System.Diagnostics.Debug.WriteLine($"[Zone {zoneId}] Zone 2 실행 확인: {zoneId == 2}");
            
            // 시퀀스를 Queue로 변환 (POP 방식으로 진행)
            var sequenceQueue = new Queue<string>(orderedSeq);
            bool isFirstCommand = true; // 첫 번째 명령어(SEQ00) 감지용
            
            while (sequenceQueue.Count > 0)
            {
                var item = sequenceQueue.Dequeue(); // Queue에서 POP
                
                // 예: "PGTurn,1" 또는 "MEAS" 같은 항목
                string fnName;
                int? arg = null;

                var parts = item.Split(',');
                fnName = parts[0].Trim();
                if (parts.Length > 1 && int.TryParse(parts[1].Trim(), out int parsed))
                    arg = parsed;

                System.Diagnostics.Debug.WriteLine($"[Zone {zoneId}] SEQ 실행: {fnName}({arg}) - 시간: {DateTime.Now:HH:mm:ss.fff}");
                
                // SEQ00(첫 번째 명령어) 시작 시 Zone별 SEQ 시작 시간 설정
                if (isFirstCommand)
                {
                    DllManager.SetZoneSeqStartTime(zoneId, DateTime.Now);
                    System.Diagnostics.Debug.WriteLine($"[Zone {zoneId}] SEQ00({fnName}) 시작 - Zone SEQ 시작 시간 설정: {DateTime.Now:HH:mm:ss.fff}");
                    isFirstCommand = false;
                }
                
                // 함수 진입 즉시 로그 (별도 스레드에서 처리 - UI 지연 없음)
                Task.Run(() =>
                {
                MonitorLogService.Instance.Log(zoneId - 1, $"ENTER {fnName}{(arg.HasValue ? "(" + arg.Value + ")" : string.Empty)}");
                });

                // DELAY 처리: 밀리초 지연 (비동기)
                if (string.Equals(fnName, "DELAY", StringComparison.OrdinalIgnoreCase))
                {
                    int delayMs = arg ?? 0;
                    if (delayMs > 0)
                    {
                        Task.Run(() =>
                    {
                        MonitorLogService.Instance.Log(zoneId - 1, $"DELAY start {delayMs}ms");
                        });
                        
                        await Task.Delay(delayMs);  // 비동기 지연으로 UI 스레드 블록 방지
                        
                        Task.Run(() =>
                        {
                        MonitorLogService.Instance.Log(zoneId - 1, "DELAY end");
                        });
                    }
                    continue; // 다음 SEQ 항목으로
                }

                // 모든 함수를 ExecuteMapped로 통일 처리 (비동기로 UI 스레드 블록 방지)
                bool ok = await Task.Run(() => DllManager.ExecuteMapped(fnName, arg, zoneId));
                
                // 실행 결과 로그 (별도 스레드에서 처리 - UI 지연 없음)
                Task.Run(() =>
                {
                MonitorLogService.Instance.Log(zoneId - 1, $"Execute {fnName}({(arg.HasValue ? arg.Value.ToString() : "-")}) => {(ok ? "OK" : "FAIL")}");
                });
                
                // MEAS나 MTP 함수 실행 후 실제 DLL 결과를 UI에 업데이트
                if (ok && (string.Equals(fnName, "MEAS", StringComparison.OrdinalIgnoreCase) || 
                          string.Equals(fnName, "MTP", StringComparison.OrdinalIgnoreCase)))
                {
                    try
                    {
                        // MTP의 경우 저장된 실제 데이터 사용, MEAS의 경우 새로 호출
                        // MTP와 MEAS 명령어 실행 확인 (UI 업데이트는 SEQ 완료 후 한 번만)
                        System.Diagnostics.Debug.WriteLine($"[Zone {zoneId}] {fnName} 명령어 실행 완료 - UI 업데이트는 SEQ 완료 후 처리");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Zone {zoneId}] UI 업데이트 오류: {ex.Message}");
                    }
                }
                
                if (!ok)
                {
                    System.Diagnostics.Debug.WriteLine($"[Zone {zoneId}] {fnName} 실패");
                    Task.Run(() =>
                    {
                    MonitorLogService.Instance.Log(zoneId - 1, $"{fnName} failed");
                    });
                    // 실패 정책: 일단 계속 진행
                }
            }
        }

    }
}

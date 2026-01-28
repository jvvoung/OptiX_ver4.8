using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Shapes;

namespace OptiX.Common
{
    /// <summary>
    /// OPTIC 페이지 Monitor 영역 관리 클래스 (OpticPage.xaml.cs에서 전체 로직 복사)
    /// 
    /// 역할:
    /// - Monitor 영역 UI 초기화 (Zone별 로그 TextBox 생성)
    /// - Zone별 실시간 로그 표시
    /// - Zone 상태 인디케이터 업데이트 (진행중/완료/오류)
    /// - 로그 자동 스크롤
    /// - 다크모드 전환 시 색상 업데이트
    /// 
    /// 사용하는 UI 요소:
    /// - MonitorGrid (Grid)
    /// - Zone별 TextBox (동적 생성)
    /// - Zone별 상태 인디케이터 (Ellipse)
    /// 
    /// 의존성:
    /// - GlobalDataManager (Zone 개수 읽기)
    /// - MonitorLogService (로그 수신 이벤트)
    /// 
    /// 성능 최적화:
    /// - 로그를 StringBuilder에 버퍼링
    /// - 16ms(60fps) 주기로 일괄 업데이트
    /// - UI 블록 최소화
    /// </summary>
    public class MonitorManager
    {
        private readonly Grid monitorGrid;
        private readonly UserControl page; // 동적 Ellipse 찾기 위해 필요 (OpticPage 또는 IPVSPage)
        private bool isDarkMode = false;
        
        // 로그 버퍼링 (Zone별)
        private readonly Dictionary<int, StringBuilder> _logBuffers = new Dictionary<int, StringBuilder>();
        private readonly Dictionary<int, string> _lastStatusTexts = new Dictionary<int, string>();
        private DispatcherTimer _flushTimer;
        private const int FLUSH_INTERVAL_MS = 16; // 60fps (16.67ms)

        public MonitorManager(Grid monitorGrid, UserControl page)
        {
            this.monitorGrid = monitorGrid ?? throw new ArgumentNullException(nameof(monitorGrid));
            this.page = page; // 동적 Ellipse 접근용
            
            // 타이머 초기화 (UI 스레드에서 실행)
            if (page?.Dispatcher != null)
            {
                _flushTimer = new DispatcherTimer(
                    TimeSpan.FromMilliseconds(FLUSH_INTERVAL_MS),
                    DispatcherPriority.Render, // Render 우선순위 (입력보다 낮음)
                    OnFlushTimer,
                    page.Dispatcher
                );
            }
        }

        /// <summary>
        /// 다크모드 상태 설정 및 UI 업데이트
        /// </summary>
        public void SetDarkMode(bool darkMode)
        {
            this.isDarkMode = darkMode;
            InitializeMonitorArea(darkMode);
        }

        /// <summary>
        /// Monitor 영역 초기화 (OpticPage.xaml.cs 382~492줄에서 복사)
        /// </summary>
        public void InitializeMonitorArea(bool darkMode)
        {
            try
            {
                if (monitorGrid == null)
                {
                    System.Diagnostics.Debug.WriteLine("❌ MonitorGrid를 찾을 수 없습니다!");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("✅ MonitorGrid 찾음 - 모니터 영역 초기화 시작");

                monitorGrid.Children.Clear();
                monitorGrid.ColumnDefinitions.Clear();

                int zoneCount = 2;
                try
                {
                    string zoneCountStr = GlobalDataManager.GetValue("Settings", "MTP_ZONE", "2");
                    if (!int.TryParse(zoneCountStr, out zoneCount)) zoneCount = 2;
                }
                catch { zoneCount = 2; }

                //25.01.29 - HVI 모드일 때는 Zone 1만 표시
                if (GlobalDataManager.IsHviModeEnabled())
                {
                    zoneCount = 1;
                }

                for (int i = 0; i < zoneCount; i++)
                {
                    monitorGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

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
                    var statusIndicator = new Ellipse
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
                        FontFamily = new FontFamily("Consolas"),
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
                    monitorGrid.Children.Add(panel);
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

        /// <summary>
        /// Zone별 로그 수신 이벤트 핸들러 (OpticPage.xaml.cs 494~502줄에서 복사)
        /// 
        /// 성능 최적화:
        /// - 로그를 즉시 표시하지 않고 버퍼에 저장
        /// - 16ms(60fps) 주기로 일괄 업데이트
        /// - UI 블록 최소화하면서도 실시간성 유지
        /// </summary>
        public void OnMonitorLogReceived(int zoneIndex, string text)
        {
            // 버퍼에 로그 추가
            lock (_logBuffers)
            {
                if (!_logBuffers.ContainsKey(zoneIndex))
                {
                    _logBuffers[zoneIndex] = new StringBuilder();
                }
                _logBuffers[zoneIndex].AppendLine(text);
                
                // 상태 텍스트 저장 (마지막 것만)
                _lastStatusTexts[zoneIndex] = text;
            }
            
            // 타이머 시작 (아직 실행 중이 아니면)
            if (_flushTimer != null && !_flushTimer.IsEnabled)
            {
                _flushTimer.Start();
            }
        }
        
        /// <summary>
        /// 타이머 콜백: 버퍼의 로그를 UI에 일괄 업데이트
        /// </summary>
        private void OnFlushTimer(object sender, EventArgs e)
        {
            lock (_logBuffers)
            {
                bool hasData = false;
                
                // 각 Zone의 버퍼를 UI에 반영
                foreach (var kvp in _logBuffers)
                {
                    int zoneIndex = kvp.Key;
                    StringBuilder buffer = kvp.Value;
                    
                    if (buffer.Length > 0)
                    {
                        hasData = true;
                        
                        // TextBox에 일괄 추가
                        AppendMonitor(zoneIndex, buffer.ToString().TrimEnd('\r', '\n'));
                        
                        // 상태 인디케이터 업데이트
                        if (_lastStatusTexts.ContainsKey(zoneIndex))
                        {
                            UpdateStatusIndicator(zoneIndex, _lastStatusTexts[zoneIndex]);
                        }
                        
                        // 버퍼 클리어
                        buffer.Clear();
                    }
                }
                
                // 모든 버퍼가 비었으면 타이머 중지
                if (!hasData)
                {
                    _flushTimer?.Stop();
                }
            }
        }

        /// <summary>
        /// Zone 상태 인디케이터 업데이트 (OpticPage.xaml.cs 504~538줄에서 복사)
        /// </summary>
        public void UpdateStatusIndicator(int zoneIndex, string text)
        {
            var indicatorName = $"Zone{zoneIndex + 1}StatusIndicator";
            var indicator = page.FindName(indicatorName) as Ellipse;
            
            if (indicator == null) 
            {
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

        /// <summary>
        /// Zone별 Monitor에 로그 추가 (OpticPage.xaml.cs 540~556줄에서 복사)
        /// </summary>
        public void AppendMonitor(int zoneIndex, string line)
        {
            if (monitorGrid == null) return;

            var box = LogicalTreeHelper.FindLogicalNode(monitorGrid, $"MonitorBox_{zoneIndex}") as TextBox;
            if (box == null && monitorGrid.Children.Count > 0)
            {
                // 폴백: 첫 번째 박스 사용
                box = LogicalTreeHelper.FindLogicalNode(monitorGrid, "MonitorBox_0") as TextBox;
            }
            if (box == null) return;

            box.AppendText(line + Environment.NewLine);
            box.CaretIndex = box.Text.Length;
            box.ScrollToEnd();
        }

        /// <summary>
        /// 모든 Zone의 Monitor 로그 클리어
        /// </summary>
        public void ClearAllMonitorLogs()
        {
            try
            {
                int zoneCount = int.Parse(GlobalDataManager.GetValue("Settings", "MTP_ZONE", "2"));

                if (monitorGrid == null) return;

                for (int i = 0; i < zoneCount; i++)
                {
                    var box = LogicalTreeHelper.FindLogicalNode(monitorGrid, $"MonitorBox_{i}") as TextBox;
                    if (box != null)
                    {
                        box.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Monitor 로그 클리어 오류: {ex.Message}");
            }
        }
    }
}



using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using OptiX.Common;
using OptiX.IPVS;

namespace OptiX
{
    public partial class IPVSPage : UserControl
    {
        // ViewModel
        private IPVSPageViewModel viewModel;
        
        // Manager 인스턴스들
        private IPVSDataTableManager dataTableManager;
        private GraphManager graphManager;
        private MonitorManager monitorManager;
        private JudgmentStatusManager judgmentStatusManager;
        private ZoneButtonManager zoneButtonManager;
        private IPVSSeqExecutor seqExecutor;
        
        // 상태 변수들
        private bool[] zoneTestCompleted;
        private bool[] zoneMeasured;
        private bool isDarkMode = false;
        
        public event EventHandler BackRequested;
        
        public IPVSPage()
        {
            InitializeComponent();
            
            // ViewModel 초기화
            viewModel = new IPVSPageViewModel();
            DataContext = viewModel;
            
            // ViewModel 이벤트 구독
            SubscribeToViewModelEvents();
            
            // Manager 인스턴스 생성 및 초기화
            InitializeManagers();
            
            // Zone별 테스트 완료 상태 배열 초기화
            int zoneCount = int.Parse(GlobalDataManager.GetValue("Settings", "IPVS_ZONE", "2"));
            zoneTestCompleted = new bool[zoneCount];
            zoneMeasured = new bool[zoneCount];

            // DataItems 변경 감지
            // ViewModel의 PropertyChanged 이벤트 구독
            // ⚠️ DataItems 변경 시 테이블 재생성하지 않음 (데이터 바인딩으로 자동 업데이트)
            // viewModel.PropertyChanged += (s, e) => {
            //     if (e.PropertyName == nameof(viewModel.DataItems))
            //     {
            //         dataTableManager.CreateCustomTable();
            //     }
            // };

            // 테이블 생성 및 Zone 버튼 생성
            Loaded += (s, e) => {
                System.Diagnostics.Debug.WriteLine("IPVSPage Loaded 이벤트 시작");
                
                // Manager를 통해 초기화
                dataTableManager.CreateCustomTable();
                dataTableManager.InitializeWadComboBox();
                
                if (monitorManager != null)
                {
                    monitorManager.InitializeMonitorArea(isDarkMode);
                }
                
                // 그래프 영역 초기화
                if (graphManager != null)
                {
                    System.Diagnostics.Debug.WriteLine("✅ GraphManager 초기화 시작");
                    graphManager.InitializeGraphArea();
                }
                
                ApplyLanguage();
                
                System.Diagnostics.Debug.WriteLine("IPVSPage Loaded 이벤트 완료");
            };
        }

        #region ViewModel 이벤트 구독
        private void SubscribeToViewModelEvents()
        {
            viewModel.TestStartRequested += (s, e) => StartTest();
            viewModel.JudgmentStatusUpdateRequested += (s, e) => OnJudgmentStatusUpdateRequested(e);
            viewModel.GraphDisplayUpdateRequested += (s, e) => UpdateGraphDisplay(e.DataPoints);
            viewModel.DataTableUpdateRequested += (s, e) => dataTableManager.CreateCustomTable(); // TEST START 후 테이블 재생성
        }
        #endregion

        #region Manager 초기화
        private void InitializeManagers()
        {
            // UI 요소 가져오기
            var dataTableGrid = this.FindName("DataTableGrid") as Grid;
            var wadComboBox = this.FindName("WadComboBox") as ComboBox;
            var monitorGrid = this.FindName("MonitorGrid") as Grid;
            var graphContent = this.FindName("GraphContent") as Grid;
            var graphScrollViewer = this.FindName("GraphScrollViewer") as ScrollViewer;
            var judgmentStatusContainer = this.FindName("JudgmentStatusContainer") as Grid;
            var zoneButtonsPanel = this.FindName("ZoneButtonsPanel") as StackPanel;
            
            // Manager 생성 (필요한 UI 요소만 전달)
            dataTableManager = new IPVSDataTableManager(dataTableGrid, wadComboBox, viewModel);
            
            // GraphManager 생성
            if (graphContent != null && graphScrollViewer != null)
            {
                graphManager = new GraphManager(graphContent, graphScrollViewer);
            }
            
            // MonitorGrid가 존재하는 경우에만 MonitorManager 생성
            if (monitorGrid != null)
            {
                monitorManager = new MonitorManager(monitorGrid, this);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("⚠️ MonitorGrid를 찾을 수 없습니다. MonitorManager 초기화 건너뜀.");
            }
            
            // JudgmentStatusManager 생성
            if (judgmentStatusContainer != null)
            {
                var statusTextBlocks = new Dictionary<string, (TextBlock quantity, TextBlock rate)>
                {
                    ["Total"] = (this.FindName("TotalQuantity") as TextBlock, this.FindName("TotalRate") as TextBlock),
                    ["OK"] = (this.FindName("OKQuantity") as TextBlock, this.FindName("OKRate") as TextBlock),
                    ["R/J"] = (this.FindName("RJQuantity") as TextBlock, this.FindName("RJRate") as TextBlock),
                    ["PTN"] = (this.FindName("PTNQuantity") as TextBlock, this.FindName("PTNRate") as TextBlock)
                };
                judgmentStatusManager = new JudgmentStatusManager(statusTextBlocks, judgmentStatusContainer, this, InspectionType.IPVS);
            }
            
            // ZoneButtonManager 생성
            if (zoneButtonsPanel != null)
            {
                zoneButtonManager = new ZoneButtonManager(
                    zoneButtonsPanel,
                    (zone) => viewModel.CurrentZone = zone,
                    () => viewModel.CurrentZone,
                    () => {
                        // RESET 시 초기화
                        viewModel.ClearGraphData(graphManager);
                        judgmentStatusManager?.ResetCounters(); // 카운터 초기화는 Manager가 처리
                    }
                );
                zoneButtonManager.SetDataTableManager(dataTableManager);
            }
            
            seqExecutor = new IPVSSeqExecutor(UpdateGraphDisplay, dataTableManager, viewModel, graphManager);
            
            // 현재 다크모드 상태 가져오기 (MainWindow에서)
            try
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    bool currentDarkMode = mainWindow.IsDarkMode;
                    this.isDarkMode = currentDarkMode;
                    
                    // 모든 Manager에 다크모드 상태 전달
                    dataTableManager?.SetDarkMode(currentDarkMode);
                    graphManager?.SetDarkMode(currentDarkMode);
                    monitorManager?.SetDarkMode(currentDarkMode);
                    judgmentStatusManager?.SetDarkMode(currentDarkMode);
                    zoneButtonManager?.SetDarkMode(currentDarkMode);
                    
                    System.Diagnostics.Debug.WriteLine($"페이지 초기 다크모드 상태 설정: {currentDarkMode}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"다크모드 상태 가져오기 오류: {ex.Message}");
            }
            
            System.Diagnostics.Debug.WriteLine("모든 Manager 초기화 완료");
        }
        #endregion

        #region 테스트 시작/종료
        private void StartTest()
        {
            seqExecutor.StartTest();
        }
        #endregion

        #region 판정 현황 업데이트
        private void OnJudgmentStatusUpdateRequested(JudgmentStatusUpdateEventArgs e)
        {
            // 판정 결과 전달 방식 (새 방식)
            if (!string.IsNullOrEmpty(e.Judgment))
            {
                // JudgmentStatusManager가 카운터 관리
                judgmentStatusManager?.IncrementCounter(e.Judgment);
            }
            // 레거시 방식 (행별 업데이트)
            else if (!string.IsNullOrEmpty(e.RowName))
            {
                judgmentStatusManager?.UpdateJudgmentStatusRow(e.RowName, e.Quantity, e.Rate);
            }
        }
        #endregion

        #region 그래프 업데이트
        private void UpdateGraphDisplay(List<GraphManager.GraphDataPoint> dataPoints)
        {
            graphManager?.UpdateGraphDisplay(dataPoints);
        }
        #endregion

        #region 탭 전환 이벤트
        private void GraphTab_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var graphTabBtn = FindName("GraphTab") as Button;
                var monitorTabBtn = FindName("MonitorTab") as Button;
                var totalTabBtn = FindName("TotalTab") as Button;
                var monitorContent = FindName("MonitorContent") as UIElement;
                var graphScrollViewer = FindName("GraphScrollViewer") as ScrollViewer;
                var graphContent = FindName("GraphContent") as Grid;
                var totalContent = FindName("TotalContent") as UIElement;

                // 스타일 변경 (보라색 활성화)
                if (graphTabBtn != null) 
                {
                    graphTabBtn.Style = (Style)FindResource("ActiveTabButtonStyle");
                    graphTabBtn.UpdateLayout();
                }
                if (monitorTabBtn != null) monitorTabBtn.Style = (Style)FindResource("TabButtonStyle");
                if (totalTabBtn != null) totalTabBtn.Style = (Style)FindResource("TabButtonStyle");

                // 콘텐츠 전환
                if (monitorContent != null) monitorContent.Visibility = Visibility.Collapsed;
                if (graphScrollViewer != null) graphScrollViewer.Visibility = Visibility.Visible;
                if (totalContent != null) totalContent.Visibility = Visibility.Collapsed;
                
                // 그래프 영역 다크모드 적용
                graphManager?.SetDarkMode(this.isDarkMode);
                
                // 그래프 업데이트
                graphManager?.RestoreExistingGraphData();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GraphTab_Click 오류: {ex.Message}");
            }
        }

        private void TotalTab_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var graphTabBtn = FindName("GraphTab") as Button;
                var monitorTabBtn = FindName("MonitorTab") as Button;
                var totalTabBtn = FindName("TotalTab") as Button;
                var monitorContent = FindName("MonitorContent") as UIElement;
                var graphScrollViewer = FindName("GraphScrollViewer") as ScrollViewer;
                var totalContent = FindName("TotalContent") as UIElement;

                // 스타일 변경
                if (totalTabBtn != null) totalTabBtn.Style = (Style)FindResource("ActiveTabButtonStyle");
                if (monitorTabBtn != null) monitorTabBtn.Style = (Style)FindResource("TabButtonStyle");
                if (graphTabBtn != null) graphTabBtn.Style = (Style)FindResource("TabButtonStyle");

                // 콘텐츠 전환
                if (monitorContent != null) monitorContent.Visibility = Visibility.Collapsed;
                if (graphScrollViewer != null) graphScrollViewer.Visibility = Visibility.Collapsed;
                if (totalContent != null) totalContent.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TotalTab_Click 오류: {ex.Message}");
            }
        }

        private void MonitorTab_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var graphTabBtn = FindName("GraphTab") as Button;
                var monitorTabBtn = FindName("MonitorTab") as Button;
                var totalTabBtn = FindName("TotalTab") as Button;
                var monitorContent = FindName("MonitorContent") as UIElement;
                var graphScrollViewer = FindName("GraphScrollViewer") as ScrollViewer;
                var totalContent = FindName("TotalContent") as UIElement;

                // 스타일 변경 (보라색 활성화)
                if (monitorTabBtn != null) 
                {
                    monitorTabBtn.Style = (Style)FindResource("ActiveTabButtonStyle");
                    monitorTabBtn.UpdateLayout();
                }
                if (graphTabBtn != null) graphTabBtn.Style = (Style)FindResource("TabButtonStyle");
                if (totalTabBtn != null) totalTabBtn.Style = (Style)FindResource("TabButtonStyle");

                // 콘텐츠 전환
                if (monitorContent != null) monitorContent.Visibility = Visibility.Visible;
                if (graphScrollViewer != null) graphScrollViewer.Visibility = Visibility.Collapsed;
                if (totalContent != null) totalContent.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MonitorTab_Click 오류: {ex.Message}");
            }
        }
        #endregion

        #region 언어 적용
        public void ApplyLanguage()
        {
            // 언어 적용 (IPVS용 구현 필요)
            System.Diagnostics.Debug.WriteLine("IPVS 언어 적용");
        }
        #endregion

        #region 뒤로가기
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            BackRequested?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        #region 다크모드 전환
        public void SetDarkMode(bool isDark)
        {
            // ViewModel에 다크모드 상태 전달
            if (viewModel != null)
            {
                viewModel.IsDarkMode = isDark;
            }
            
            // ThemeManager로 다크모드 적용 (OPTIC과 동일)
            ThemeManager.UpdateDynamicColors(this, isDark);
            
            // 각 Manager에 다크모드 전파
            dataTableManager?.SetDarkMode(isDark);
            graphManager?.SetDarkMode(isDark);
            monitorManager?.SetDarkMode(isDark);
            judgmentStatusManager?.SetDarkMode(isDark);
            zoneButtonManager?.SetDarkMode(isDark);
            
            // 클래스 변수에 다크모드 상태 저장
            this.isDarkMode = isDark;
            
            System.Diagnostics.Debug.WriteLine($"IPVSPage 다크모드 변경: {isDark}");
        }
        #endregion
        
        #region XAML 이벤트 핸들러들
        private void PathButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var pathWindow = new Common.PathSettingWindow("IPVS_PATHS", isDarkMode);
                pathWindow.Owner = Window.GetWindow(this);
                pathWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Path 버튼 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// RESET 버튼 클릭 이벤트 (ZoneButtonManager로 위임, OPTIC과 동일)
        /// </summary>
        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            zoneButtonManager?.OnResetButtonClick(sender, e);
        }
        
        private void SettingButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var cellIdWindow = new Common.CellIdInputWindow(1, isDarkMode, "IPVS");
                cellIdWindow.Owner = Window.GetWindow(this);
                cellIdWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Setting 버튼 오류: {ex.Message}");
            }
        }
        
        private void TestStartButton_Click(object sender, RoutedEventArgs e)
        {
            StartTest();
        }
        
        private void WadComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            dataTableManager?.OnWadComboBoxSelectionChanged(sender, e);
        }
        #endregion
    }
}














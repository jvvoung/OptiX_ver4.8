using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OptiX.Common
{
    /// <summary>
    /// Zone 버튼 관리 클래스 (OPTIC/IPVS 공통)
    /// 
    /// 역할:
    /// - Zone 버튼 동적 생성 (INI에서 읽은 Zone 개수만큼)
    /// - Zone별 테스트 상태 관리 (완료 여부)
    /// - RESET 버튼 이벤트 처리
    /// - 버튼 스타일 및 색상 관리
    /// - 다크모드 전환 시 버튼 색상 업데이트
    /// 
    /// 사용하는 UI 요소:
    /// - ZoneButtonsPanel (StackPanel)
    /// 
    /// 의존성:
    /// - GlobalDataManager (Zone 개수 읽기)
    /// - CurrentZone 콜백 (Zone 상태 관리)
    /// </summary>
    public class ZoneButtonManager
    {
        private readonly StackPanel zoneButtonsPanel;
        private readonly Action<int> onZoneChanged; // Zone 변경 콜백
        private readonly Func<int> getCurrentZone; // 현재 Zone 가져오기
        private Action onResetRequested; // RESET 콜백 (ViewModel 초기화용)
        private object dataTableManager; // RESET 시 필요 (범용)
        private bool isDarkMode = false;
        
        // Zone별 테스트 상태
        private bool[] zoneTestCompleted;
        private bool[] zoneMeasured;

        /// <summary>
        /// 범용 생성자 (IPVS용)
        /// </summary>
        public ZoneButtonManager(StackPanel zoneButtonsPanel, Action<int> onZoneChanged, Func<int> getCurrentZone, Action onResetRequested = null)
        {
            this.zoneButtonsPanel = zoneButtonsPanel ?? throw new ArgumentNullException(nameof(zoneButtonsPanel));
            this.onZoneChanged = onZoneChanged;
            this.getCurrentZone = getCurrentZone;
            this.onResetRequested = onResetRequested;
        }

        /// <summary>
        /// OPTIC 호환용 생성자
        /// </summary>
        public ZoneButtonManager(StackPanel zoneButtonsPanel, object viewModel, GraphManager graphManager = null)
        {
            this.zoneButtonsPanel = zoneButtonsPanel ?? throw new ArgumentNullException(nameof(zoneButtonsPanel));
            
            // Reflection으로 CurrentZone 속성 접근
            var viewModelType = viewModel.GetType();
            var currentZoneProp = viewModelType.GetProperty("CurrentZone");
            
            if (currentZoneProp != null)
            {
                this.getCurrentZone = () => (int)currentZoneProp.GetValue(viewModel);
                this.onZoneChanged = (zone) => currentZoneProp.SetValue(viewModel, zone);
            }
            
            // Reflection으로 RESET 콜백 설정
            var initJudgmentMethod = viewModelType.GetMethod("InitializeJudgmentCounters");
            var clearGraphMethod = viewModelType.GetMethod("ClearGraphData", new[] { typeof(GraphManager) });
            var updateJudgmentMethod = viewModelType.GetMethod("UpdateJudgmentStatusUI");
            
            if (initJudgmentMethod != null && clearGraphMethod != null && updateJudgmentMethod != null)
            {
                this.onResetRequested = () => {
                    initJudgmentMethod.Invoke(viewModel, null);
                    clearGraphMethod.Invoke(viewModel, new object[] { graphManager });
                    updateJudgmentMethod.Invoke(viewModel, null);
                };
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("⚠️ ViewModel 메서드를 찾을 수 없습니다!");
                System.Diagnostics.Debug.WriteLine($"InitializeJudgmentCounters: {initJudgmentMethod != null}");
                System.Diagnostics.Debug.WriteLine($"ClearGraphData: {clearGraphMethod != null}");
                System.Diagnostics.Debug.WriteLine($"UpdateJudgmentStatusUI: {updateJudgmentMethod != null}");
            }
        }

        /// <summary>
        /// DataTableManager 설정 (RESET 시 필요)
        /// </summary>
        public void SetDataTableManager(object manager)
        {
            this.dataTableManager = manager;
        }

        /// <summary>
        /// 다크모드 상태 설정 및 버튼 갱신
        /// </summary>
        public void SetDarkMode(bool darkMode)
        {
            this.isDarkMode = darkMode;
            
            // 다크모드 변경 시 버튼 즉시 재생성
            try
            {
                CreateZoneButtons();
                System.Diagnostics.Debug.WriteLine($"다크모드 변경으로 Zone 버튼 갱신: {darkMode}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"다크모드 버튼 갱신 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 버튼 업데이트 (다크모드 전환 시)
        /// </summary>
        public void UpdateButtons()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("OpticZoneButtonManager.UpdateButtons() 호출됨");
                
                // 기존 버튼들 재생성
                CreateZoneButtons();
                
                System.Diagnostics.Debug.WriteLine("버튼 업데이트 완료");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"버튼 업데이트 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 리소스에서 Style을 안전하게 가져옴
        /// </summary>
        private Style GetResourceStyle(string resourceKey)
        {
            try
            {
                // 1. ZoneButtonsPanel(Page 레벨)에서 찾기 시도
                if (zoneButtonsPanel?.TryFindResource(resourceKey) is Style style)
                    return style;
                
                // 2. Application에서 찾기 시도
                if (Application.Current.TryFindResource(resourceKey) is Style appStyle)
                    return appStyle;
                
                System.Diagnostics.Debug.WriteLine($"⚠️ 스타일 '{resourceKey}' 못 찾음 - null 반환");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ 스타일 '{resourceKey}' 로드 오류: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Zone 버튼 생성 (OpticPage.xaml.cs에서 복사)
        /// </summary>
        public void CreateZoneButtons()
        {
            try
            {
                if (zoneButtonsPanel == null)
                {
                    System.Diagnostics.Debug.WriteLine("❌ ZoneButtonsPanel을 찾을 수 없습니다!");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("✅ ZoneButtonsPanel 찾음 - 버튼 생성 시작");

                // 기존 버튼들 제거
                zoneButtonsPanel.Children.Clear();

                // Settings에서 MTP_ZONE 개수 읽기
                string zoneCountStr = GlobalDataManager.GetValue("Settings", "MTP_ZONE", "2");
                int zoneCount = int.Parse(zoneCountStr);

                // 스타일 미리 가져오기
                var activeStyle = GetResourceStyle("ActiveZoneButtonStyle");
                var normalStyle = GetResourceStyle("ZoneButtonStyle");

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
                        if (activeStyle != null) zoneButton.Style = activeStyle;
                    }
                    else
                    {
                        if (normalStyle != null) zoneButton.Style = normalStyle;
                    }

                    int currentI = i;
                    zoneButton.Click += (s, e) => {
                        if (s is Button btn && btn.Tag is int zoneIndex)
                        {
                            System.Diagnostics.Debug.WriteLine($"=== Zone 버튼 클릭 ===");
                            System.Diagnostics.Debug.WriteLine($"클릭된 버튼: Zone {currentI} (Tag: {zoneIndex})");
                            System.Diagnostics.Debug.WriteLine($"이전 CurrentZone: {getCurrentZone()}");

                            onZoneChanged(zoneIndex);

                            System.Diagnostics.Debug.WriteLine($"새로운 CurrentZone: {getCurrentZone()}");
                            System.Diagnostics.Debug.WriteLine($"업데이트될 targetZone: {zoneIndex + 1}");

                            // 모든 Zone 버튼 스타일 초기화
                            var normalStyleClick = GetResourceStyle("ZoneButtonStyle");
                            var activeStyleClick = GetResourceStyle("ActiveZoneButtonStyle");

                            foreach (var child in zoneButtonsPanel.Children)
                            {
                                if (child is Button childBtn && normalStyleClick != null)
                                {
                                    childBtn.Style = normalStyleClick;
                                }
                            }

                            // 선택된 버튼 스타일 변경
                            if (activeStyleClick != null) btn.Style = activeStyleClick;

                            // CreateCustomTable() 호출 제거 - 테이블을 다시 그리지 않음
                            System.Diagnostics.Debug.WriteLine($"Zone {zoneIndex + 1} 선택됨. 테이블 재생성 안함.");
                        }
                    };

                    zoneButtonsPanel.Children.Add(zoneButton);
                }

                // 첫 번째 Zone을 기본 선택
                if (zoneButtonsPanel.Children.Count > 0)
                {
                    onZoneChanged(0);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Zone 버튼 생성 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// Zone별 테스트 상태 초기화 (OpticPage.xaml.cs에서 복사)
        /// </summary>
        public void InitializeZoneTestStates()
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

        /// <summary>
        /// 특정 Zone의 테스트 완료 상태 설정 (OpticPage.xaml.cs에서 복사)
        /// </summary>
        public void SetZoneTestCompleted(int zoneIndex, bool completed)
        {
            // null 체크 - 배열이 초기화되지 않았으면 초기화
            if (zoneTestCompleted == null)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ zoneTestCompleted가 null - 자동 초기화");
                InitializeZoneTestStates();
            }

            if (zoneTestCompleted != null && zoneIndex >= 0 && zoneIndex < zoneTestCompleted.Length)
            {
                zoneTestCompleted[zoneIndex] = completed;
                System.Diagnostics.Debug.WriteLine($"Zone {zoneIndex + 1} 테스트 완료 상태: {completed}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"❌ Zone {zoneIndex + 1} 인덱스 범위 초과 (배열 크기: {zoneTestCompleted?.Length ?? 0})");
            }
        }

        /// <summary>
        /// 특정 Zone의 테스트 완료 상태 확인 (OpticPage.xaml.cs에서 복사)
        /// </summary>
        public bool IsZoneTestCompleted(int zoneIndex)
        {
            // null 체크
            if (zoneTestCompleted == null)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ zoneTestCompleted가 null - false 반환");
                return false;
            }

            if (zoneIndex >= 0 && zoneIndex < zoneTestCompleted.Length)
            {
                return zoneTestCompleted[zoneIndex];
            }
            return false;
        }

        /// <summary>
        /// Zone별 테스트 상태 배열 가져오기
        /// </summary>
        public bool[] GetZoneTestCompletedArray()
        {
            return zoneTestCompleted;
        }

        /// <summary>
        /// Zone별 측정 여부 배열 가져오기
        /// </summary>
        public bool[] GetZoneMeasuredArray()
        {
            return zoneMeasured;
        }

        /// <summary>
        /// RESET 버튼 클릭 이벤트 핸들러 (OpticPage.xaml.cs에서 복사)
        /// </summary>
        public void OnResetButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("RESET 버튼 클릭됨");
                
                // 모든 Zone의 테스트 완료 상태 초기화
                if (zoneTestCompleted != null)
                {
                    for (int i = 0; i < zoneTestCompleted.Length; i++)
                    {
                        zoneTestCompleted[i] = false;
                    }
                }
                
                if (zoneMeasured != null)
                {
                    for (int i = 0; i < zoneMeasured.Length; i++)
                    {
                        zoneMeasured[i] = false;
                    }
                }
                
                System.Diagnostics.Debug.WriteLine("Zone 테스트 상태 초기화 완료");

                // UI 스레드에서 초기화 실행
                _ = System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        // RESET 콜백 호출 (ViewModel 초기화)
                        onResetRequested?.Invoke();

                        // DataTableManager를 통해 데이터 테이블 초기화
                        if (dataTableManager != null)
                        {
                            var resetMethod = dataTableManager.GetType().GetMethod("ResetDataTable");
                            resetMethod?.Invoke(dataTableManager, null);
                        }

                        System.Diagnostics.Debug.WriteLine("모든 데이터 초기화 완료");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"데이터 초기화 중 오류: {ex.Message}");
                        // 추가적인 예외 처리 로직
                    }
                }, System.Windows.Threading.DispatcherPriority.Background);

                ShowResetCompleteMessage();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RESET 버튼 클릭 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 데이터 RESET 완료 메시지 표시 (OpticPage.xaml.cs에서 복사)
        /// </summary>
        private void ShowResetCompleteMessage()
        {
            try
            {
                _ = System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(
                        "데이터 RESET이 완료되었습니다.",
                        "RESET 완료",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                    
                    System.Diagnostics.Debug.WriteLine("RESET 완료 팝업 표시됨");
                }, System.Windows.Threading.DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RESET 메시지 표시 오류: {ex.Message}");
            }
        }
    }
}


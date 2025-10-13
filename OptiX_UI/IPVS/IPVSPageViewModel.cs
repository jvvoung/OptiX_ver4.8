using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using OptiX.Common;
using OptiX.DLL;

namespace OptiX.IPVS
{
    public class IPVSPageViewModel : INotifyPropertyChanged
    {
        #region Fields
        private ObservableCollection<DataTableItem> dataItems;
        private int currentZone = 0;
        private int selectedWadIndex = 0;
        private bool isDarkMode = false;
        #endregion

        #region Properties
        public ObservableCollection<DataTableItem> DataItems
        {
            get => dataItems;
            set => SetProperty(ref dataItems, value);
        }
    
        public int CurrentZone
        {
            get => currentZone;
            set => SetProperty(ref currentZone, value);
        }

        public int SelectedWadIndex
        {
            get => selectedWadIndex;
            set => SetProperty(ref selectedWadIndex, value);
        }

        public bool IsDarkMode
        {
            get => isDarkMode;
            set => SetProperty(ref isDarkMode, value);
        }
        #endregion

        #region Constructor
        public IPVSPageViewModel()
        {
            DataItems = new ObservableCollection<DataTableItem>();
            
            // IPVS 데이터 초기화
            InitializeIPVSData();
            
            // Commands 초기화
            TestStartCommand = new RelayCommand(ExecuteTestStart);
            SettingCommand = new RelayCommand(ExecuteSetting);
            PathCommand = new RelayCommand(ExecutePath);
            ZoneButtonCommand = new RelayCommand<int>(ExecuteZoneButton);
            GraphTabCommand = new RelayCommand(ExecuteGraphTab);
            TotalTabCommand = new RelayCommand(ExecuteTotalTab);
            BackCommand = new RelayCommand(ExecuteBack);
            
            // IPVS 시퀀스 캐싱 (한 번만 실행)
            OptiX.DLL.SequenceCacheManager.Instance.LoadIPVSSequence();
        }
        #endregion

        /// <summary>
        /// IPVS 초기 데이터 생성
        /// </summary>
        private void InitializeIPVSData()
        {
            try
            {
                // DataItems 초기화
                DataItems.Clear();
                
                // Zone 개수와 Point 개수 읽기
                int zoneCount = int.Parse(GlobalDataManager.GetValue("Settings", "IPVS_ZONE", "2"));
                int maxPoint = int.Parse(GlobalDataManager.GetValue("IPVS", "MAX_POINT", "5"));
                
                System.Diagnostics.Debug.WriteLine($"IPVS 데이터 초기화: {zoneCount} Zones × {maxPoint} Points = {zoneCount * maxPoint} rows");

                // Zone × Point 조합으로 데이터 아이템 생성
                for (int zone = 1; zone <= zoneCount; zone++)
                {
                    // Cell ID와 Inner ID는 테스트 후에만 표시 (OPTIC과 동일)
                    
                    for (int point = 1; point <= maxPoint; point++)
                    {
                        var item = new DataTableItem
                        {
                            Zone = zone.ToString(),
                            CellId = "",          // 테스트 전에는 비어있음
                            InnerId = "",         // 테스트 전에는 비어있음
                            Point = point.ToString(),
                            X = "0.0",
                            Y = "0.0",
                            L = "0.0",
                            Current = "0.0",
                            Efficiency = "0.0",
                            ErrorName = "",
                            Tact = "0",
                            Judgment = "WAIT"
                        };
                        DataItems.Add(item);
                        System.Diagnostics.Debug.WriteLine($"✓ Zone={zone}, Point={point} 추가");
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"✅ IPVS 데이터 초기화 완료: {DataItems.Count}개 아이템 생성됨");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ IPVS 데이터 초기화 오류: {ex.Message}");
            }
        }

        #region Commands
        public ICommand TestStartCommand { get; }
        public ICommand SettingCommand { get; }
        public ICommand PathCommand { get; }
        public ICommand ZoneButtonCommand { get; }
        public ICommand GraphTabCommand { get; }
        public ICommand TotalTabCommand { get; }
        public ICommand BackCommand { get; }
        #endregion

        #region Command Methods
        private void ExecuteTestStart()
        {
            System.Diagnostics.Debug.WriteLine("IPVS 테스트 시작");
            TestStartRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ExecuteSetting()
        {
            System.Diagnostics.Debug.WriteLine("IPVS 설정 열기");
        }

        private void ExecutePath()
        {
            System.Diagnostics.Debug.WriteLine("IPVS 경로 설정");
        }

        private void ExecuteZoneButton(int zone)
        {
            CurrentZone = zone;
            System.Diagnostics.Debug.WriteLine($"IPVS Zone {zone} 선택");
        }

        private void ExecuteGraphTab()
        {
            System.Diagnostics.Debug.WriteLine("IPVS 그래프 탭 선택");
        }

        private void ExecuteTotalTab()
        {
            System.Diagnostics.Debug.WriteLine("IPVS 전체 탭 선택");
        }

        private void ExecuteBack()
        {
            System.Diagnostics.Debug.WriteLine("IPVS 뒤로가기");
        }
        #endregion

        #region Data Management
        /// <summary>
        /// DLL 결과로 데이터 테이블 업데이트 (DllResultHandler로 위임)
        /// </summary>
        public void UpdateDataTableWithDllResult(Output output, int zoneNumber, IPVSDataTableManager dataTableManager, GraphManager graphManager)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== IPVS UpdateDataTableWithDllResult 시작: Zone {zoneNumber} ===");

                var targetZone = zoneNumber.ToString();

                // IPVS_data 확인
                if (output.IPVS_data == null || output.IPVS_data.Length == 0)
                {
                    System.Diagnostics.Debug.WriteLine("❌ IPVS_data가 비어있음");
                    return;
                }

                // DllResultHandler를 통해 결과 처리
                var handler = new DLL.DllResultHandler();
                string zoneJudgment = handler.ProcessIPVSResult(
                    output,
                    targetZone,
                    dataTableManager,
                    SelectedWadIndex,
                    UpdateJudgmentStatusTable
                );

                // 그래프 데이터 추가 (GraphManager 사용)
                AddGraphDataPoint(zoneNumber, zoneJudgment, graphManager);

                // 데이터 변경 알림
                OnPropertyChanged(nameof(DataItems));
                
                // 테이블 재생성 요청 (UI 업데이트)
                DataTableUpdateRequested?.Invoke(this, EventArgs.Empty);
                
                System.Diagnostics.Debug.WriteLine($"=== IPVS UpdateDataTableWithDllResult 완료: Zone {zoneNumber}, 판정: {zoneJudgment} ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"IPVS UI 업데이트 오류: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 판정 현황 표 업데이트 (이제 JudgmentStatusManager가 카운터 관리)
        /// </summary>
        /// <param name="judgment">새로운 판정 결과 (OK/R/J/PTN)</param>
        private void UpdateJudgmentStatusTable(string judgment)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[IPVS] 판정 결과 전달: {judgment} → JudgmentStatusManager");

                // 판정 결과를 JudgmentStatusManager에게 전달 (이벤트 발생)
                JudgmentStatusUpdateRequested?.Invoke(this, 
                    new JudgmentStatusUpdateEventArgs(judgment));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"판정 현황 표 업데이트 오류: {ex.Message}");
            }
        }
        #endregion

        #region Graph Data (GraphManager로 위임)
        
        /// <summary>
        /// 그래프 데이터 포인트 추가 - GraphManager로 위임
        /// </summary>
        public void AddGraphDataPoint(int zoneNumber, string judgment, GraphManager graphManager)
        {
            try
            {
                // GraphManager를 통해 데이터 추가 (Timestamp 미포함)
                var dataPoints = graphManager.AddDataPoint(zoneNumber, judgment, includeTimestamp: false);
                
                GraphDisplayUpdateRequested?.Invoke(this, new GraphDisplayUpdateEventArgs
                {
                    DataPoints = dataPoints
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"그래프 데이터 포인트 추가 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 그래프 데이터 초기화 - GraphManager로 위임
        /// </summary>
        public void ClearGraphData(GraphManager graphManager)
        {
            try
            {
                graphManager.ClearDataPoints();
                
                GraphDisplayUpdateRequested?.Invoke(this, new GraphDisplayUpdateEventArgs
                {
                    DataPoints = new List<GraphManager.GraphDataPoint>()
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"그래프 데이터 초기화 오류: {ex.Message}");
            }
        }
        #endregion

        #region Events
        public event EventHandler TestStartRequested;
        public event EventHandler<JudgmentStatusUpdateEventArgs> JudgmentStatusUpdateRequested;
        public event EventHandler<GraphDisplayUpdateEventArgs> GraphDisplayUpdateRequested;
        public event EventHandler DataTableUpdateRequested; // 데이터 테이블 재생성 요청
        #endregion

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        #endregion
    }
}

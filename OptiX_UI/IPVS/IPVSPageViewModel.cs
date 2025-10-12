using System;
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
        
        // 판정 카운터
        private int totalCount = 0;
        private int okCount = 0;
        private int rjCount = 0;
        private int ptnCount = 0;
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

        public ObservableCollection<GraphDataPoint> GraphDataPoints { get; set; }
        #endregion

        #region Constructor
        public IPVSPageViewModel()
        {
            DataItems = new ObservableCollection<DataTableItem>();
            GraphDataPoints = new ObservableCollection<GraphDataPoint>();
            
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
            
            // 초기 데이터는 InitializeIPVSData()에서 이미 로드됨
            // LoadInitialData(); // 제거: InitializeIPVSData()와 중복
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
        /// 초기 데이터 로드
        /// </summary>
        // LoadInitialData() 제거됨 - InitializeIPVSData()가 이미 올바른 데이터 생성함

        /// <summary>
        /// DLL 결과로 데이터 테이블 업데이트
        /// ⚠️ 이 메서드는 반드시 UI 스레드에서 호출되어야 합니다!
        /// POINT==1 행만 업데이트, 판정은 [0~6][0] 데이터로 수행
        /// </summary>
        /// <param name="output">DLL Output</param>
        /// <param name="zoneNumber">Zone 번호</param>
        public void UpdateDataTableWithDllResult(Output output, int zoneNumber = -1)
        {
            // zoneNumber가 전달되면 사용, 아니면 CurrentZone 사용
            int actualZone = zoneNumber > 0 ? zoneNumber : CurrentZone;
            System.Diagnostics.Debug.WriteLine($"IPVS UI 업데이트 시작... Zone: {actualZone}");

            var targetZone = actualZone.ToString();
            
            try
            {
                // IPVS_data 확인
                if (output.IPVS_data == null || output.IPVS_data.Length == 0)
                {
                    System.Diagnostics.Debug.WriteLine("❌ IPVS_data가 비어있음");
                    return;
                }

                // Zone별 Cell ID와 Inner ID 가져오기 (IPVS 섹션에서)
                string cellId = GlobalDataManager.GetValue("IPVS", $"CELL_ID_ZONE_{actualZone}", "");
                string innerId = GlobalDataManager.GetValue("IPVS", $"INNER_ID_ZONE_{actualZone}", "");
                System.Diagnostics.Debug.WriteLine($"Zone {targetZone} - Cell ID: '{cellId}', Inner ID: '{innerId}'");

                // TACT 계산
                DateTime zoneSeqStartTime = SeqExecutionManager.GetZoneSeqStartTime(actualZone);
                DateTime zoneSeqEndTime = SeqExecutionManager.GetZoneSeqEndTime(actualZone);
                
                if (zoneSeqEndTime == default(DateTime) || zoneSeqEndTime < zoneSeqStartTime)
                {
                    zoneSeqEndTime = DateTime.Now;
                }
                
                double tactSeconds = (zoneSeqEndTime - zoneSeqStartTime).TotalSeconds;
                string tactValue = tactSeconds.ToString("F3");
                
                System.Diagnostics.Debug.WriteLine($"Zone {targetZone} TACT: {tactValue}초");

                // POINT==1 행만 업데이트 (현재 선택된 WAD의 첫 번째 포인트)
                // IPVS_data는 1차원 배열 [IPVS_DATA_SIZE] (MAX_WAD_COUNT × MAX_POINT_COUNT)
                // 접근: [wadIndex * MAX_POINT_COUNT + pointIndex]
                int dataIndex = SelectedWadIndex * DLL.DllConstants.MAX_POINT_COUNT + 0; // POINT==1이므로 pointIndex=0
                var pattern = output.IPVS_data[dataIndex];
                
                var existingItem = DataItems.FirstOrDefault(item =>
                    item.Zone == targetZone && item.Point == "1");

                if (existingItem != null)
                {
                    existingItem.X = pattern.x.ToString("F2");
                    existingItem.Y = pattern.y.ToString("F2");
                    existingItem.L = pattern.L.ToString("F2");
                    existingItem.Current = pattern.cur.ToString("F3");
                    existingItem.Efficiency = pattern.eff.ToString("F2");
                    existingItem.CellId = cellId;
                    existingItem.InnerId = innerId;
                    existingItem.ErrorName = "";
                    existingItem.Tact = tactValue;

                    System.Diagnostics.Debug.WriteLine($"✅ Zone {targetZone} POINT==1 업데이트 완료");
                }

                // 판정: [0][0] ~ [MAX_WAD_COUNT-1][0] 데이터로 수행 (MAX_WAD_COUNT개 WAD 각도)
                int okCount = 0;
                int totalWad = 0;
                
                for (int wadIdx = 0; wadIdx < DLL.DllConstants.MAX_WAD_COUNT; wadIdx++)
                {
                    int idx = wadIdx * DLL.DllConstants.MAX_POINT_COUNT + 0; // 각 WAD의 첫 번째 포인트
                    if (idx < output.IPVS_data.Length)
                    {
                        totalWad++;
                        int result = output.IPVS_data[idx].result;
                        
                        // IPVSJudgment 클래스 사용 (OPTIC과 동일)
                        string judgment = IPVSJudgment.Instance.GetPointJudgment(result);
                        if (judgment == "OK") okCount++;
                        
                        System.Diagnostics.Debug.WriteLine($"WAD[{wadIdx}][0] (index={idx}) result={result} → {judgment}");
                    }
                }

                string zoneJudgment = (okCount == totalWad) ? "OK" : "R/J";
                System.Diagnostics.Debug.WriteLine($"Zone {targetZone} 전체 판정: {zoneJudgment} (OK: {okCount}/{totalWad})");

                // 판정을 모든 Point 행에 반영
                var zoneItems = DataItems.Where(item => item.Zone == targetZone).ToList();
                foreach (var item in zoneItems)
                {
                    item.Judgment = zoneJudgment;
                }

                // 판정 현황 표 및 그래프 업데이트
                UpdateJudgmentStatusTable(zoneJudgment);
                AddGraphDataPoint(actualZone, zoneJudgment);

                // 데이터 변경 알림
                OnPropertyChanged(nameof(DataItems));
                
                // 테이블 재생성 요청 (UI 업데이트)
                DataTableUpdateRequested?.Invoke(this, EventArgs.Empty);
                
                System.Diagnostics.Debug.WriteLine($"IPVS UI 업데이트 완료");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"IPVS UI 업데이트 오류: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 판정 현황 표 업데이트
        /// </summary>
        private void UpdateJudgmentStatusTable(string judgment)
        {
            totalCount++;
            
            if (judgment == "OK")
                okCount++;
            else if (judgment == "R/J")
                rjCount++;
            else if (judgment == "PTN")
                ptnCount++;

            UpdateJudgmentStatusUI();
        }
        #endregion

        #region Judgment Counters
        public void InitializeJudgmentCounters()
        {
            totalCount = 0;
            okCount = 0;
            rjCount = 0;
            ptnCount = 0;
        }
        
        public void UpdateJudgmentStatusUI()
        {
            this.totalCount = okCount + rjCount + ptnCount;
            string okRate = this.totalCount > 0 ? (okCount / (double)this.totalCount).ToString("0.00") : "0.00";
            string rjRate = this.totalCount > 0 ? (rjCount / (double)this.totalCount).ToString("0.00") : "0.00";
            string ptnRate = this.totalCount > 0 ? (ptnCount / (double)this.totalCount).ToString("0.00") : "0.00";

            JudgmentStatusUpdateRequested?.Invoke(this, new JudgmentStatusUpdateEventArgs { RowName = "Total", Quantity = this.totalCount.ToString(), Rate = "1.00" });
            JudgmentStatusUpdateRequested?.Invoke(this, new JudgmentStatusUpdateEventArgs { RowName = "OK", Quantity = okCount.ToString(), Rate = okRate });
            JudgmentStatusUpdateRequested?.Invoke(this, new JudgmentStatusUpdateEventArgs { RowName = "RJ", Quantity = rjCount.ToString(), Rate = rjRate });
            JudgmentStatusUpdateRequested?.Invoke(this, new JudgmentStatusUpdateEventArgs { RowName = "PTN", Quantity = ptnCount.ToString(), Rate = ptnRate });
        }
        
        public void ClearGraphData()
        {
            GraphDataPoints.Clear();
            GraphDisplayUpdateRequested?.Invoke(this, new GraphDisplayUpdateEventArgs { DataPoints = new System.Collections.Generic.List<GraphDataPoint>() });
        }
        #endregion

        #region Graph Data
        public class GraphDataPoint
        {
            public int ZoneNumber { get; set; }
            public string Judgment { get; set; }
            public int GlobalIndex { get; set; }
        }
        
        public void AddGraphDataPoint(int zoneNumber, string judgment)
        {
            var newPoint = new GraphDataPoint
            {
                ZoneNumber = zoneNumber,
                Judgment = judgment,
                GlobalIndex = GraphDataPoints.Count
            };
            GraphDataPoints.Add(newPoint);
            
            System.Diagnostics.Debug.WriteLine($"그래프 데이터 포인트 추가: Zone{zoneNumber} - {judgment} (총 {GraphDataPoints.Count}개, GlobalIndex: {newPoint.GlobalIndex})");
            
            GraphDisplayUpdateRequested?.Invoke(this, new GraphDisplayUpdateEventArgs
            {
                DataPoints = GraphDataPoints.ToList()
            });
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

    #region Event Arguments
    public class JudgmentStatusUpdateEventArgs : EventArgs
    {
        public string RowName { get; set; }
        public string Quantity { get; set; }
        public string Rate { get; set; }
    }

    public class GraphDisplayUpdateEventArgs : EventArgs
    {
        public System.Collections.Generic.List<IPVSPageViewModel.GraphDataPoint> DataPoints { get; set; }
    }
    #endregion

    #region RelayCommand Classes
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object parameter) => _execute();
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            if (_canExecute == null) return true;
            if (parameter == null) return false;
            
            if (parameter is T typedParam)
                return _canExecute(typedParam);
            
            return false;
        }

        public void Execute(object parameter)
        {
            if (parameter is T typedParam)
                _execute(typedParam);
        }
    }
    #endregion
}

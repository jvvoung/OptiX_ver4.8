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
using OptiX.Models;
using OptiX_UI.Result_LOG;

namespace OptiX.IPVS
{
    public class IPVSPageViewModel : INotifyPropertyChanged
    {
        #region Fields
        private ObservableCollection<DataTableItem> dataItems;
        private int currentZone = 0;
        private bool isDarkMode = false;
        private DispatcherTimer characteristicsTimer;
        private DispatcherTimer ipvsTimer;
        private IPVSPage ipvsPage; // IPVSPage 참조 추가
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

        public bool IsDarkMode
        {
            get => isDarkMode;
            set => SetProperty(ref isDarkMode, value);
        }
        #endregion

        #region Commands
        public ICommand TestStartCommand { get; }
        public ICommand SettingCommand { get; }
        public ICommand PathCommand { get; }
        public ICommand ZoneButtonCommand { get; }
        public ICommand GraphTabCommand { get; }
        public ICommand TotalTabCommand { get; }
        public ICommand BackCommand { get; }
        #endregion

        #region Constructor
        public IPVSPageViewModel(IPVSPage page = null)
        {
            DataItems = new ObservableCollection<DataTableItem>();
            ipvsPage = page; // IPVSPage 참조 설정
            
            // Commands 초기화
            TestStartCommand = new RelayCommand(ExecuteTestStart);
            SettingCommand = new RelayCommand(ExecuteSetting);
            PathCommand = new RelayCommand(ExecutePath);
            ZoneButtonCommand = new RelayCommand<int>(ExecuteZoneButton);
            GraphTabCommand = new RelayCommand(ExecuteGraphTab);
            TotalTabCommand = new RelayCommand(ExecuteTotalTab);
            BackCommand = new RelayCommand(ExecuteBack);
            
            // 초기 데이터 로드
            LoadInitialData();
        }
        #endregion

        #region Command Methods
        private void ExecuteTestStart()
        {
            System.Diagnostics.Debug.WriteLine("IPVS 테스트 시작");
            // IPVS 테스트 시작 로직
        }

        private void ExecuteSetting()
        {
            System.Diagnostics.Debug.WriteLine("IPVS 설정 열기");
            // IPVS 설정 창 열기 로직
        }

        private void ExecutePath()
        {
            System.Diagnostics.Debug.WriteLine("IPVS 경로 설정");
            // IPVS 경로 설정 로직
        }

        private void ExecuteZoneButton(int zone)
        {
            CurrentZone = zone;
            System.Diagnostics.Debug.WriteLine($"IPVS Zone {zone} 선택");
            // Zone 변경 로직
        }

        private void ExecuteGraphTab()
        {
            System.Diagnostics.Debug.WriteLine("IPVS 그래프 탭 선택");
            // 그래프 탭 로직
        }

        private void ExecuteTotalTab()
        {
            System.Diagnostics.Debug.WriteLine("IPVS 전체 탭 선택");
            // 전체 탭 로직
        }

        private void ExecuteBack()
        {
            System.Diagnostics.Debug.WriteLine("IPVS 뒤로 가기");
            // 뒤로 가기 로직
        }
        #endregion

        #region Data Management
        /// <summary>
        /// 초기 데이터 로드
        /// </summary>
        private void LoadInitialData()
        {
            try
            {
                DataItems.Clear();
                
                // IPVS Zone 개수 읽기
                string zoneCountStr = GlobalDataManager.GetValue("Settings", "IPVS_ZONE", "2");
                int zoneCount = int.Parse(zoneCountStr);
                
                for (int zone = 1; zone <= zoneCount; zone++)
                {
                    DataItems.Add(new DataTableItem
                    {
                        Zone = zone.ToString(),
                        InnerId = "", // 초기에는 빈 값
                        CellId = "", // 초기에는 빈 값
                        Category = "IPVS",
                        X = "0.0",
                        Y = "0.0",
                        L = "0.0",
                        Current = "0.0",
                        Efficiency = "0.0",
                        ErrorName = "",
                        Tact = "0",
                        Judgment = "WAIT"
                    });
                }
                
                System.Diagnostics.Debug.WriteLine($"IPVS 초기 데이터 로드 완료: {DataItems.Count}개 항목");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"IPVS 초기 데이터 로드 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// DLL 결과로 데이터 테이블 업데이트
        /// </summary>
        /// <param name="output">DLL에서 받은 출력 데이터</param>
        /// <param name="zoneNumber">Zone 번호</param>
        public void UpdateDataTableWithDllResult(Output output, int zoneNumber = -1)
        {
            try
            {
                // zoneNumber가 전달되면 사용, 아니면 CurrentZone 사용
                int actualZone = zoneNumber > 0 ? zoneNumber : CurrentZone;
                System.Diagnostics.Debug.WriteLine($"IPVS UI 업데이트 시작... 현재 Zone: {CurrentZone}, 전달된 Zone: {zoneNumber}, 실제 사용할 Zone: {actualZone}");

                var targetZone = actualZone.ToString();
                System.Diagnostics.Debug.WriteLine($"업데이트할 Zone: {targetZone}");

                // Zone별 Cell ID와 Inner ID 가져오기
                var (cellId, innerId) = GlobalDataManager.GetZoneInfo(actualZone);
                System.Diagnostics.Debug.WriteLine($"Zone {actualZone} - Cell ID: {cellId}, Inner ID: {innerId}");

                // 해당 Zone의 아이템 찾기 및 업데이트
                var targetItem = DataItems.FirstOrDefault(item => item.Zone == targetZone);
                if (targetItem != null)
                {
                    // DLL 결과를 DataTableItem으로 변환
                    var convertedData = ConvertToDataTableItem(output, targetZone, cellId, innerId);
                    
                    // 기존 아이템 업데이트
                    targetItem.Category = convertedData.Category;
                    targetItem.X = convertedData.X;
                    targetItem.Y = convertedData.Y;
                    targetItem.L = convertedData.L;
                    targetItem.Current = convertedData.Current;
                    targetItem.Efficiency = convertedData.Efficiency;
                    targetItem.CellId = convertedData.CellId;
                    targetItem.InnerId = convertedData.InnerId;
                    targetItem.ErrorName = convertedData.ErrorName;
                    targetItem.Tact = convertedData.Tact;
                    targetItem.Judgment = convertedData.Judgment;

                    System.Diagnostics.Debug.WriteLine($"Zone {actualZone} 아이템 업데이트: {targetItem.Category} - Cell ID: {targetItem.CellId}, Inner ID: {targetItem.InnerId}");
                }

                // UI 업데이트 알림
                OnPropertyChanged(nameof(DataItems));
                
                System.Diagnostics.Debug.WriteLine("IPVS UI 업데이트 완료");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"IPVS UI 업데이트 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// Output을 DataTableItem으로 변환
        /// </summary>
        private DataTableItem ConvertToDataTableItem(Output output, string zone, string cellId, string innerId)
        {
            try
            {
                var data = ConvertToOutputData(output);
                
                return new DataTableItem
                {
                    Zone = zone,
                    InnerId = innerId,
                    CellId = cellId,
                    Category = "IPVS",
                    X = SanitizeValue(data.measure[0].x).ToString("F3"),
                    Y = SanitizeValue(data.measure[0].y).ToString("F3"),
                    L = SanitizeValue(data.measure[0].L).ToString("F3"),
                    Current = SanitizeValue(data.measure[0].cur).ToString("F3"),
                    Efficiency = SanitizeValue(data.measure[0].eff).ToString("F3"),
                    ErrorName = data.result ? "" : "MEASUREMENT_ERROR",
                    Tact = DateTime.Now.ToString("HH:mm:ss"),
                    Judgment = data.result ? "PASS" : "FAIL"
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"IPVS 데이터 변환 오류: {ex.Message}");
                return new DataTableItem
                {
                    Zone = zone,
                    InnerId = innerId,
                    CellId = cellId,
                    Category = "IPVS",
                    X = "0.000",
                    Y = "0.000",
                    L = "0.000",
                    Current = "0.000",
                    Efficiency = "0.000",
                    ErrorName = "CONVERSION_ERROR",
                    Tact = DateTime.Now.ToString("HH:mm:ss"),
                    Judgment = "FAIL"
                };
            }
        }

        /// <summary>
        /// Output 구조체를 OutputData로 변환
        /// </summary>
        private OutputData ConvertToOutputData(Output output)
        {
            var outputData = new OutputData();
            
            // measure 배열 변환
            for (int i = 0; i < 7; i++)
            {
                outputData.measure[i] = new MeasureData
                {
                    x = SanitizeValue(output.measure[i].x),
                    y = SanitizeValue(output.measure[i].y),
                    L = SanitizeValue(output.measure[i].L),
                    cur = SanitizeValue(output.measure[i].cur),
                    eff = SanitizeValue(output.measure[i].eff)
                };
            }
            
            // data 배열 변환
            for (int i = 0; i < 119; i++)
            {
                outputData.data[i] = new PatternData
                {
                    x = SanitizeValue(output.data[i].x),
                    y = SanitizeValue(output.data[i].y),
                    L = SanitizeValue(output.data[i].L),
                    cur = SanitizeValue(output.data[i].cur),
                    eff = SanitizeValue(output.data[i].eff)
                };
            }
            
            // lut 배열 변환
            for (int i = 0; i < 3; i++)
            {
                outputData.lut[i] = new LutData
                {
                    x = SanitizeValue(output.lut[i].x),
                    y = SanitizeValue(output.lut[i].y),
                    L = SanitizeValue(output.lut[i].L),
                    cur = SanitizeValue(output.lut[i].cur),
                    eff = SanitizeValue(output.lut[i].eff)
                };
            }
            
            outputData.result = output.result;
            
            return outputData;
        }

        /// <summary>
        /// 값 정제 (NaN, Infinity 등 처리)
        /// </summary>
        private float SanitizeValue(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return 0.0f;
            
            if (Math.Abs(value) > 1000000.0f)
                return 0.0f;
                
            return value;
        }
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

    #region Supporting Classes
    /// <summary>
    /// IPVS 출력 데이터 구조
    /// </summary>
    public class OutputData
    {
        public PatternData[] data = new PatternData[119];
        public MeasureData[] measure = new MeasureData[7];
        public LutData[] lut = new LutData[3];
        public bool result;
    }

    public class PatternData
    {
        public float x, y, L, cur, eff;
    }

    public class MeasureData
    {
        public float x, y, L, cur, eff;
    }

    public class LutData
    {
        public float x, y, L, cur, eff;
    }

    /// <summary>
    /// IPVS 입력 구조체
    /// </summary>
    public struct Input
    {
        public float test_value;
        public int zone;
    }

    /// <summary>
    /// IPVS 출력 구조체
    /// </summary>
    public struct Output
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 119)]
        public PatternData[] data;
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
        public MeasureData[] measure;
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public LutData[] lut;
        
        public bool result;
    }

    /// <summary>
    /// RelayCommand 구현
    /// </summary>
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

        public bool CanExecute(object parameter) => _canExecute?.Invoke((T)parameter) ?? true;

        public void Execute(object parameter) => _execute((T)parameter);
    }
    #endregion
}


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

namespace OptiX.ViewModels
{
    public class OpticPageViewModel : INotifyPropertyChanged
    {
        #region Fields
        private ObservableCollection<DataTableItem> dataItems;
        public IniFileManager iniManager;
        private int currentZone = 0;
        private bool isDarkMode = false;
        private DispatcherTimer characteristicsTimer;
        private DispatcherTimer ipvsTimer;
        private bool isCharacteristicsHovered = false;
        private bool isIPVSHovered = false;
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
        public OpticPageViewModel()
        {
            DataItems = new ObservableCollection<DataTableItem>();
            iniManager = new IniFileManager(@"D:\OptiX\Recipe\OptiX.ini");
            
            // Commands 초기화
            TestStartCommand = new RelayCommand(ExecuteTestStart);
            SettingCommand = new RelayCommand(ExecuteSetting);
            PathCommand = new RelayCommand(ExecutePath);
            ZoneButtonCommand = new RelayCommand<int>(ExecuteZoneButton);
            GraphTabCommand = new RelayCommand(ExecuteGraphTab);
            TotalTabCommand = new RelayCommand(ExecuteTotalTab);
            BackCommand = new RelayCommand(ExecuteBack);
            
            InitializeTimers();
            LoadSettingsFromIni();
            CreateZoneButtons();
        }
        #endregion

        #region Private Methods
        private void InitializeTimers()
        {
            characteristicsTimer = new DispatcherTimer();
            characteristicsTimer.Interval = TimeSpan.FromMilliseconds(100);
            characteristicsTimer.Tick += (s, e) => CheckCharacteristicsHover();

            ipvsTimer = new DispatcherTimer();
            ipvsTimer.Interval = TimeSpan.FromMilliseconds(100);
            ipvsTimer.Tick += (s, e) => CheckIPVSHover();
        }

        private void LoadSettingsFromIni()
        {
            try
            {
                // 다크모드 설정 로드
                string darkModeStr = iniManager.ReadValue("Theme", "IsDarkMode", "F");
                IsDarkMode = darkModeStr.ToUpper() == "T";
                
                // Zone과 Category 정보 로드하여 초기 데이터 생성
                LoadDataFromIni();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"INI 설정 로드 오류: {ex.Message}");
            }
        }

        private void LoadDataFromIni()
        {
            try
            {
                // MTP 섹션에서 Zone과 Category 읽기
                string zoneCountStr = iniManager.ReadValue("MTP", "Zone", "2");
                string categoriesStr = iniManager.ReadValue("MTP", "Category", "W,WG,R,G,B");

                int zoneCount = int.Parse(zoneCountStr);
                string[] categories = categoriesStr.Split(',').Select(c => c.Trim()).ToArray();

                DataItems.Clear();

                // Zone과 Category에 따라 데이터 생성 (각 카테고리를 개별 행으로)
                for (int zone = 1; zone <= zoneCount; zone++)
                {
                    for (int i = 0; i < categories.Length; i++)
                    {
                        string category = categories[i].Trim();
                        DataItems.Add(new DataTableItem
                        {
                            Zone = zone.ToString(),
                            InnerId = "",
                            CellId = "",
                            Category = category,
                            X = "", 
                            Y = "", 
                            L = "", 
                            Current = "", 
                            Efficiency = "",
                            ErrorName = "", 
                            Tact = "", 
                            Judgment = "",
                            IsFirstInGroup = i == 0,
                            GroupSize = categories.Length
                        });
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"INI에서 읽은 Zone 개수: {zoneCount}, 카테고리: {string.Join(", ", categories)}");
                
                // 생성된 데이터 확인
                System.Diagnostics.Debug.WriteLine("=== 생성된 DataItems ===");
                foreach (var item in DataItems)
                {
                    System.Diagnostics.Debug.WriteLine($"Zone: {item.Zone}, Category: {item.Category}");
                }
                System.Diagnostics.Debug.WriteLine("=== 생성된 DataItems 끝 ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"INI 데이터 로드 오류: {ex.Message}");
            }
        }

        private void CreateZoneButtons()
        {
            // Zone 버튼 생성 로직은 View에서 처리
        }

        private void CheckCharacteristicsHover()
        {
            // 호버 체크 로직
        }

        private void CheckIPVSHover()
        {
            // 호버 체크 로직
        }
        #endregion

        #region Command Methods
        private void ExecuteTestStart()
        {
            try
            {
                // DLL 경로 가져오기
                string dllPath = iniManager.ReadValue("MTP_PATHS", "DLL_FOLDER", "");
                if (string.IsNullOrEmpty(dllPath))
                {
                    MessageBox.Show("DLL 경로가 설정되지 않았습니다. Path 버튼을 눌러 DLL 폴더를 설정해주세요.", "오류",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // DLL 파일 경로
                string fullDllPath = Path.Combine(dllPath, "TestDll.dll");
                if (!File.Exists(fullDllPath))
                {
                    MessageBox.Show($"DLL 파일을 찾을 수 없습니다: {fullDllPath}", "오류",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // DLL 로드
                IntPtr dllHandle = LoadLibrary(fullDllPath);
                if (dllHandle == IntPtr.Zero)
                {
                    int error = Marshal.GetLastWin32Error();
                    MessageBox.Show($"DLL 로딩에 실패했습니다. 오류 코드: {error}\n경로: {fullDllPath}", "오류",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                System.Diagnostics.Debug.WriteLine("DLL 로딩 성공!");

                // 함수 주소 가져오기
                IntPtr funcPtr = GetProcAddress(dllHandle, "test");
                if (funcPtr == IntPtr.Zero)
                {
                    int error = Marshal.GetLastWin32Error();
                    MessageBox.Show($"함수 주소를 가져올 수 없습니다. 오류 코드: {error}", "오류",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    FreeLibrary(dllHandle);
                    return;
                }

                System.Diagnostics.Debug.WriteLine("함수 주소 가져오기 성공!");

                // INI에서 Cell ID와 Inner ID 읽기
                string cellId = iniManager.ReadValue("MTP_PATHS", "CELL_ID", "");
                string innerId = iniManager.ReadValue("MTP_PATHS", "INNER_ID", "");
                
                // INI에서 카테고리 개수 읽기
                string categoriesStr = iniManager.ReadValue("MTP", "Category", "R,G,B");
                string[] categories = categoriesStr.Split(',').Select(c => c.Trim()).ToArray();
                int categoryCount = categories.Length;
                
                // 입력 데이터 준비
                Input input = new Input
                {
                    CELL_ID = cellId,
                    INNER_ID = innerId,
                    total_point = Math.Min(categoryCount, 7), // 최대 7개 카테고리로 제한 (DLL 제한)
                    cur_point = 0
                };

                // 출력 데이터 준비 (1차원 배열 119개 요소)
                Output output = new Output
                {
                    data = new Pattern[119] // 7*17 = 119개 요소
                };
                
                System.Diagnostics.Debug.WriteLine($"출력 데이터 배열 크기: [7,17] (카테고리 개수: {categoryCount})");

                // 디버그: DLL 호출 전
                System.Diagnostics.Debug.WriteLine("DLL 함수 호출 시작...");
                System.Diagnostics.Debug.WriteLine($"입력 데이터 - CELL_ID: {input.CELL_ID}, INNER_ID: {input.INNER_ID}, total_point: {input.total_point}, cur_point: {input.cur_point}");

                // 구조체를 비관리 메모리에 마샬링
                IntPtr inputPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Input)));
                IntPtr outputPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Output)));
                
                int result = 0; // result 변수를 try 블록 밖에서 선언

                try
                {
                    // 입력 구조체를 비관리 메모리에 복사
                    Marshal.StructureToPtr(input, inputPtr, false);
                    Marshal.StructureToPtr(output, outputPtr, false);

                    // 함수 포인터를 델리게이트로 변환
                    TestFunction testFunc = Marshal.GetDelegateForFunctionPointer<TestFunction>(funcPtr);

                    // DLL 함수 호출
                    result = testFunc(inputPtr, outputPtr);

                    System.Diagnostics.Debug.WriteLine($"DLL 함수 호출 완료. 결과: {result}");

                    if (result == 1)
                    {
                        // 출력 구조체를 관리 메모리로 복사
                        Output resultOutput = Marshal.PtrToStructure<Output>(outputPtr);
                        
                        // UI 업데이트
                        UpdateDataTableWithDllResult(resultOutput);
                        
                        MessageBox.Show("테스트가 성공적으로 완료되었습니다!", "성공",
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("테스트 실행 중 오류가 발생했습니다.", "오류",
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                finally
                {
                    // 메모리 해제
                    Marshal.FreeHGlobal(inputPtr);
                    Marshal.FreeHGlobal(outputPtr);
                    FreeLibrary(dllHandle);
                }
            }
            catch (DllNotFoundException)
            {
                MessageBox.Show("TestDll.dll을 찾을 수 없습니다. DLL 경로를 확인해주세요.", "오류",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"테스트 실행 중 오류가 발생했습니다: {ex.Message}", "오류",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteSetting()
        {
            try
            {
                // IPVS페이지와 동일한 방식으로 설정 창 열기 (MVVM 패턴 유지)
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    // 현재 Zone 번호를 Setting 창에 전달 (1-based)
                    int currentZoneNumber = CurrentZone + 1;
                    var settingWindow = new CellIdInputWindow(currentZoneNumber, IsDarkMode); // 다크모드 상태 전달
                    settingWindow.Owner = System.Windows.Application.Current.MainWindow;
                    settingWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;

                    if (settingWindow.ShowDialog() == true)
                    {
                        MessageBox.Show($"Zone {currentZoneNumber} 설정이 저장되었습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"설정 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecutePath()
        {
            try
            {
                // IPVS페이지와 동일한 방식으로 Path 설정 창 열기 (MVVM 패턴 유지)
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var pathWindow = new PathSettingWindow("MTP_PATHS", IsDarkMode); // MTP_PATHS 섹션 사용, 다크모드 상태 전달
                    pathWindow.Owner = System.Windows.Application.Current.MainWindow;
                    pathWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;

                    if (pathWindow.ShowDialog() == true)
                    {
                        MessageBox.Show("Optic 경로 설정이 완료되었습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"경로 설정 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteZoneButton(int zoneIndex)
        {
            System.Diagnostics.Debug.WriteLine($"Zone {zoneIndex + 1} 버튼 클릭됨! 이전 currentZone: {CurrentZone}");
            CurrentZone = zoneIndex;
            System.Diagnostics.Debug.WriteLine($"Zone {zoneIndex + 1} 선택됨. currentZone = {CurrentZone}");

            // Zone 변경만 수행, 테이블 재생성하지 않음
            // UI에서 Zone 버튼 스타일만 변경됨
        }

        private void ExecuteGraphTab()
        {
            // Graph 탭 활성화 로직
            System.Diagnostics.Debug.WriteLine("Graph 탭 클릭됨");
            // TODO: Graph 탭 UI 업데이트 로직 구현
        }

        private void ExecuteTotalTab()
        {
            // Total 탭 활성화 로직
            System.Diagnostics.Debug.WriteLine("Total 탭 클릭됨");
            // TODO: Total 탭 UI 업데이트 로직 구현
        }

        private void ExecuteBack()
        {
            // 뒤로가기 로직 - MainWindow에서 처리
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
                mainWindow?.ShowMainPage();
            });
        }

        private void UpdateDataTableWithDllResult(Output output)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"UI 업데이트 시작... 현재 Zone: {CurrentZone}");

                var targetZone = (CurrentZone + 1).ToString();
                System.Diagnostics.Debug.WriteLine($"업데이트할 Zone: {targetZone}");

                string categoriesStr = iniManager.ReadValue("MTP", "Category", "W,WG,R,G,B");
                string[] categoryNames = categoriesStr.Split(',').Select(c => c.Trim()).ToArray();

                // Zone별로 다른 키를 사용하여 Cell ID와 Inner ID 읽기
                string cellIdKey = $"CELL_ID_ZONE_{targetZone}";
                string innerIdKey = $"INNER_ID_ZONE_{targetZone}";

                string cellId = iniManager.ReadValue("MTP_PATHS", cellIdKey, "");
                string innerId = iniManager.ReadValue("MTP_PATHS", innerIdKey, "");

                System.Diagnostics.Debug.WriteLine($"Zone {targetZone} - Cell ID: {cellId}, Inner ID: {innerId}");

                int availableCategories = 7;
                int categoriesToProcess = Math.Min(categoryNames.Length, availableCategories);

                for (int categoryIndex = 0; categoryIndex < categoriesToProcess; categoryIndex++)
                {
                    int dataIndex = categoryIndex * 17 + 0;

                    if (dataIndex >= output.data.Length) continue;

                    var pattern = output.data[dataIndex];

                    var existingItem = DataItems.FirstOrDefault(item =>
                        item.Zone == targetZone && item.Category == categoryNames[categoryIndex]);

                    if (existingItem != null)
                    {
                        existingItem.X = pattern.x.ToString();
                        existingItem.Y = pattern.y.ToString();
                        existingItem.L = pattern.L.ToString();
                        existingItem.Current = pattern.cur.ToString();
                        existingItem.Efficiency = pattern.eff.ToString();
                        existingItem.CellId = cellId;    // Zone별 Cell ID
                        existingItem.InnerId = innerId;  // Zone별 Inner ID
                        existingItem.ErrorName = "";
                        existingItem.Tact = "0";
                        existingItem.Judgment = "OK";

                        System.Diagnostics.Debug.WriteLine($"Zone {targetZone} 아이템 업데이트: {existingItem.Category} - Cell ID: {cellId}, Inner ID: {innerId}");
                    }
                }

                OnPropertyChanged(nameof(DataItems));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UI 업데이트 중 오류: {ex.Message}");
                MessageBox.Show($"데이터 테이블 업데이트 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region INotifyPropertyChanged
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

        #region P/Invoke Declarations
        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

        [DllImport("kernel32.dll")]
        private static extern bool FreeLibrary(IntPtr hModule);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int TestFunction(IntPtr input, IntPtr output);
        #endregion

        #region Struct Definitions
        [StructLayout(LayoutKind.Sequential)]
        public struct Input
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string CELL_ID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string INNER_ID;
            public int total_point;
            public int cur_point;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Pattern
        {
            public int x;
            public int y;
            public int L;
            public int cur;
            public int eff;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Output
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 119)] // 7*17 = 119개 요소를 1차원으로 마샬링
            public Pattern[] data; // 1차원 배열로 마샬링하되 2차원으로 사용
        }
        #endregion
    }

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

        public bool CanExecute(object parameter) => _canExecute?.Invoke((T)parameter) ?? true;

        public void Execute(object parameter) => _execute((T)parameter);
    }
    #endregion
}

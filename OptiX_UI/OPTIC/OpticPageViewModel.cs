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

namespace OptiX.OPTIC
{
    public class OpticPageViewModel : INotifyPropertyChanged
    {
        #region Fields
        private ObservableCollection<DataTableItem> dataItems;
        private int currentZone = 0;
        private int selectedWadIndex = 0; // 현재 선택된 WAD 인덱스 (0 = 0도, 1 = 15도, ...)
        private bool isDarkMode = false;
        private DispatcherTimer characteristicsTimer;
        private DispatcherTimer ipvsTimer;
        #endregion

        #region Events (View와의 통신용 - 순환 참조 제거)
        /// <summary>
        /// 테스트 시작 요청 이벤트
        /// </summary>
        public event EventHandler TestStartRequested;

        /// <summary>
        /// 판정 현황 업데이트 요청 이벤트
        /// </summary>
        public event EventHandler<JudgmentStatusUpdateEventArgs> JudgmentStatusUpdateRequested;

        /// <summary>
        /// 그래프 표시 업데이트 요청 이벤트
        /// </summary>
        public event EventHandler<GraphDisplayUpdateEventArgs> GraphDisplayUpdateRequested;
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

        #region 시퀀스 캐싱
        private static Queue<string> _cachedSequence = null;
        private static bool _sequenceLoaded = false;
        private static readonly object _sequenceLock = new object();
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
            LoadSequenceFromIni(); // 시퀀스 로드 추가
            CreateZoneButtons();
            InitializeJudgmentCounters();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 시퀀스 INI 파일에서 시퀀스 로드 및 캐싱
        /// </summary>
        private void LoadSequenceFromIni()
        {
            lock (_sequenceLock)
            {
                if (_sequenceLoaded && _cachedSequence != null)
                {
                    System.Diagnostics.Debug.WriteLine("시퀀스가 이미 로드되어 있음 - 캐시된 시퀀스 사용");
                    return;
                }

                try
                {
                    string seqIniPath = @"D:\Project\Recipe\Sequence\Sequence_Optic.ini";
                    System.Diagnostics.Debug.WriteLine($"시퀀스 INI 파일 로드: {seqIniPath}");

                    var seqIniManager = new IniFileManager(seqIniPath);
                    
                    // SEQ_COUNT 읽기
                    string seqCountStr = seqIniManager.ReadValue("SETTING", "SEQ_COUNT", "0");
                    if (!int.TryParse(seqCountStr, out int seqCount) || seqCount <= 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"SEQ_COUNT가 유효하지 않음: {seqCountStr}");
                        return;
                    }

                    // 시퀀스 Queue 생성
                    _cachedSequence = new Queue<string>();
                    
                    // SEQ00부터 SEQ{seqCount-1}까지 읽기
                    for (int i = 0; i < seqCount; i++)
                    {
                        string seqKey = $"SEQ{i:D2}"; // SEQ00, SEQ01, SEQ02, ...
                        string seqValue = seqIniManager.ReadValue("SEQ", seqKey, "");
                        
                        if (!string.IsNullOrEmpty(seqValue))
                        {
                            _cachedSequence.Enqueue(seqValue);
                            System.Diagnostics.Debug.WriteLine($"시퀀스 추가: {seqKey}={seqValue}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"시퀀스 {seqKey}가 비어있음 - 건너뜀");
                        }
                    }

                    _sequenceLoaded = true;
                    System.Diagnostics.Debug.WriteLine($"시퀀스 로드 완료 - 총 {_cachedSequence.Count}개 시퀀스");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"시퀀스 로드 오류: {ex.Message}");
                    _cachedSequence = new Queue<string>();
                    _sequenceLoaded = false;
                }
            }
        }

        /// <summary>
        /// 캐시된 시퀀스의 복사본 반환 (Queue 복사)
        /// </summary>
        public static Queue<string> GetCachedSequence()
        {
            lock (_sequenceLock)
            {
                if (_cachedSequence == null || !_sequenceLoaded)
                {
                    System.Diagnostics.Debug.WriteLine("캐시된 시퀀스가 없음 - 빈 Queue 반환");
                    return new Queue<string>();
                }

                // Queue 복사본 반환 (원본 보호)
                var sequenceCopy = new Queue<string>(_cachedSequence);
                System.Diagnostics.Debug.WriteLine($"캐시된 시퀀스 복사본 반환 - {sequenceCopy.Count}개 시퀀스");
                return sequenceCopy;
            }
        }

        /// <summary>
        /// 판정 현황 카운터 초기화
        /// </summary>
        public void InitializeJudgmentCounters()
        {
            try
            {
                totalCount = 0;
                okCount = 0;
                ngCount = 0;
                ptnCount = 0;
                
                System.Diagnostics.Debug.WriteLine("판정 현황 카운터 초기화 완료");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"판정 현황 카운터 초기화 오류: {ex.Message}");
            }
        }

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
                string darkModeStr = GlobalDataManager.GetValue("Theme", "IsDarkMode", "F");
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
                // Settings 섹션에서 MTP_ZONE과 Category 읽기
                string zoneCountStr = GlobalDataManager.GetValue("Settings", "MTP_ZONE", "2");
                string categoriesStr = GlobalDataManager.GetValue("MTP", "Category", "W,WG,R,G,B");

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
                            InnerId = "", // 초기에는 빈 값
                            CellId = "", // 초기에는 빈 값
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
                // DllManager를 통해 DLL 함수 호출
                if (!DllManager.IsInitialized)
                {
                    MessageBox.Show("DLL이 초기화되지 않았습니다. 메인 설정창에서 DLL 경로를 확인해주세요.", "오류",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 시퀀스 기반 테스트 시작 요청 이벤트 발생
                TestStartRequested?.Invoke(this, EventArgs.Empty);
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
                    
                    var settingWindow = new CellIdInputWindow(currentZoneNumber, IsDarkMode, "MTP"); // OPTIC용 MTP 섹션 사용
                    settingWindow.Owner = System.Windows.Application.Current.MainWindow;
                    settingWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;

                    // Non-Modal로 열기 (메인 프로그램 계속 동작)
                    settingWindow.Show();
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

                    // Non-Modal로 열기 (메인 프로그램 계속 동작)
                    pathWindow.Show();
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

            // SeqExecutionManager에 현재 Zone 설정
            SeqExecutionManager.SetCurrentZone(CurrentZone + 1);

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

        public void UpdateDataTableWithDllResult(Output output, int zoneNumber = -1)
        {
            try
            {
                // zoneNumber가 전달되면 사용, 아니면 CurrentZone 사용
                int actualZone = zoneNumber > 0 ? zoneNumber : CurrentZone;
                System.Diagnostics.Debug.WriteLine($"UI 업데이트 시작... 현재 Zone: {CurrentZone}, 전달된 Zone: {zoneNumber}, 실제 사용할 Zone: {actualZone}");

                var targetZone = actualZone.ToString();
                System.Diagnostics.Debug.WriteLine($"업데이트할 Zone: {targetZone}");

                string categoriesStr = GlobalDataManager.GetValue("MTP", "Category", "W,WG,R,G,B");
                string[] categoryNames = categoriesStr.Split(',').Select(c => c.Trim()).ToArray();

                // Zone별로 다른 키를 사용하여 Cell ID와 Inner ID 읽기
                string cellIdKey = $"CELL_ID_ZONE_{targetZone}";
                string innerIdKey = $"INNER_ID_ZONE_{targetZone}";

                string cellId = GlobalDataManager.GetValue("MTP", cellIdKey, "");
                string innerId = GlobalDataManager.GetValue("MTP", innerIdKey, "");

                System.Diagnostics.Debug.WriteLine($"Zone {targetZone} - Cell ID: {cellId}, Inner ID: {innerId}");
                System.Diagnostics.Debug.WriteLine($"현재 CurrentZone: {CurrentZone}, targetZone: {targetZone}");

                // TACT 계산 (해당 Zone의 SEQ 시작 시간부터 종료 시간까지의 소요 시간)
                DateTime zoneSeqStartTime = SeqExecutionManager.GetZoneSeqStartTime(actualZone);
                DateTime zoneSeqEndTime = SeqExecutionManager.GetZoneSeqEndTime(actualZone);
                
                // Race Condition 방지: 종료 시간이 아직 설정되지 않았으면 현재 시간 사용
                if (zoneSeqEndTime == default(DateTime) || zoneSeqEndTime < zoneSeqStartTime)
                {
                    zoneSeqEndTime = DateTime.Now;
                    System.Diagnostics.Debug.WriteLine($"Zone {targetZone} 종료 시간이 아직 설정 안됨 - 현재 시간 사용");
                }
                
                double tactSeconds = (zoneSeqEndTime - zoneSeqStartTime).TotalSeconds;
                string tactValue = tactSeconds.ToString("F3");
                
                System.Diagnostics.Debug.WriteLine($"Zone {targetZone} TACT 계산: {tactValue}초 (시작: {zoneSeqStartTime:HH:mm:ss.fff}, 종료: {zoneSeqEndTime:HH:mm:ss.fff})");

                // DLL output 구조체를 2차원 배열로 변환 (result 값 추출)
                int[,] resultArray = new int[DLL.DllConstants.MAX_WAD_COUNT, DLL.DllConstants.MAX_PATTERN_COUNT];
                for (int wad = 0; wad < DLL.DllConstants.MAX_WAD_COUNT; wad++)
                {
                    for (int pattern = 0; pattern < DLL.DllConstants.MAX_PATTERN_COUNT; pattern++)
                    {
                        int index = wad * DLL.DllConstants.MAX_PATTERN_COUNT + pattern;
                        if (index < output.data.Length)
                        {
                            resultArray[wad, pattern] = output.data[index].result;
                        }
                    }
                }

                // Zone 전체 판정 수행
                string zoneJudgment = OpticJudgment.Instance.JudgeZoneFromResults(resultArray);
                System.Diagnostics.Debug.WriteLine($"Zone {targetZone} 전체 판정: {zoneJudgment}");

                // 각 Category별로 데이터 업데이트
                int availableCategories = Math.Min(categoryNames.Length, DLL.DllConstants.MAX_PATTERN_COUNT);
                for (int categoryIndex = 0; categoryIndex < availableCategories; categoryIndex++)
                {
                    // 현재 선택된 WAD에서 해당 패턴의 데이터 가져오기
                    int patternIndex = GetPatternArrayIndex(categoryNames[categoryIndex]);
                    int dataIndex = SelectedWadIndex * DLL.DllConstants.MAX_PATTERN_COUNT + patternIndex; // 현재 선택된 WAD의 데이터

                    if (dataIndex >= output.data.Length) continue;

                    var pattern = output.data[dataIndex];
                    int result = pattern.result;

                    // 디버그: 실제 DLL에서 받은 result 값 확인
                    System.Diagnostics.Debug.WriteLine($"Zone {targetZone}, Category {categoryNames[categoryIndex]}, result={result}");

                    // 개별 패턴 판정
                    string patternJudgment = OpticJudgment.Instance.GetPatternJudgment(result);

                    var existingItem = DataItems.FirstOrDefault(item =>
                        item.Zone == targetZone && item.Category == categoryNames[categoryIndex]);

                    if (existingItem != null)
                    {
                        existingItem.X = pattern.x.ToString("F2");
                        existingItem.Y = pattern.y.ToString("F2");
                        existingItem.L = pattern.L.ToString("F2");
                        existingItem.Current = pattern.cur.ToString("F3");
                        existingItem.Efficiency = pattern.eff.ToString("F2");
                        existingItem.CellId = cellId;    // Zone별 Cell ID
                        existingItem.InnerId = innerId;  // Zone별 Inner ID
                        existingItem.ErrorName = "";
                        existingItem.Tact = tactValue;   // 계산된 TACT 값
                        existingItem.Judgment = patternJudgment; // 개별 패턴 판정

                        System.Diagnostics.Debug.WriteLine($"Zone {targetZone} 아이템 업데이트: {existingItem.Category} - Judgment: {patternJudgment}");
                    }
                }

                // Zone 전체 판정을 모든 아이템의 Judgment에 반영 (일관성 유지)
                var zoneItems = DataItems.Where(item => item.Zone == targetZone).ToList();
                foreach (var item in zoneItems)
                {
                    System.Diagnostics.Debug.WriteLine($"Zone {targetZone} 아이템 {item.Category}: {item.Judgment} → {zoneJudgment}");
                    item.Judgment = zoneJudgment;
                }
                System.Diagnostics.Debug.WriteLine($"Zone {targetZone} 전체 판정 적용: {zoneJudgment}");

                OnPropertyChanged(nameof(DataItems));
                
                // 판정 현황 표 업데이트
                UpdateJudgmentStatusTable(zoneJudgment);
                
                // 그래프 영역 업데이트는 OpticPage.xaml.cs에서 처리됨
                // AddGraphDataPoint(actualZone, zoneJudgment); // 중복 제거
                
                // 결과 로그 생성은 DllManager.ExecuteMakeResultLog에서 처리됨
                // CreateResultLogs(output, cellId, innerId); // 중복 제거
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UI 업데이트 중 오류: {ex.Message}");
                MessageBox.Show($"데이터 테이블 업데이트 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Category 이름을 패턴 배열 인덱스로 변환
        /// </summary>
        private int GetPatternArrayIndex(string category)
        {
            switch (category.ToUpper())
            {
                case "W": return 0;
                case "R": return 1;
                case "G": return 2;
                case "B": return 3;
                case "WG": return 4;
                case "WG2": return 5;
                case "WG3": return 6;
                case "WG4": return 7;
                case "WG5": return 8;
                case "WG6": return 9;
                case "WG7": return 10;
                case "WG8": return 11;
                case "WG9": return 12;
                case "WG10": return 13;
                case "WG11": return 14;
                case "WG12": return 15;
                case "WG13": return 16;
                default: return 0;
            }
        }

        /// <summary>
        /// 결과 로그 생성 메서드
        /// </summary>
        private void CreateResultLogs(Output output, string cellId, string innerId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== MAKE_RESULT_LOG 시작 ===");
                System.Diagnostics.Debug.WriteLine($"Cell ID: {cellId}, Inner ID: {innerId}");
                
                var startTime = DateTime.Now.AddSeconds(-10); // 테스트 시작 시간 (예시)
                var endTime = DateTime.Now; // 테스트 종료 시간

                // ResultLogManager를 통해 모든 로그 생성 (OPTIC)
                ResultLogManager.CreateResultLogsForZone(startTime, endTime, cellId, innerId, CurrentZone + 1, output);
                
                System.Diagnostics.Debug.WriteLine("=== MAKE_RESULT_LOG 완료 ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"결과 로그 생성 중 오류: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"스택 트레이스: {ex.StackTrace}");
            }
        }


        #region 판정 현황 관리
        private int totalCount = 0;
        private int okCount = 0;
        private int ngCount = 0;
        private int ptnCount = 0;

        /// <summary>
        /// 판정 현황 표 업데이트
        /// </summary>
        /// <param name="judgment">새로운 판정 결과 (OK/NG/PTN)</param>
        private void UpdateJudgmentStatusTable(string judgment)
        {
            try
            {
                // 카운트 업데이트
                totalCount++;
                
                switch (judgment)
                {
                    case "OK":
                        okCount++;
                        break;
                    case "NG":
                    case "R/J": // R/J도 NG와 동일하게 처리
                        ngCount++;
                        break;
                    case "PTN":
                        ptnCount++;
                        break;
                }

                System.Diagnostics.Debug.WriteLine($"판정 현황 업데이트: Total={totalCount}, OK={okCount}, NG={ngCount}, PTN={ptnCount}");

                // UI 스레드에서 판정 현황 표 업데이트
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    UpdateJudgmentStatusUI();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"판정 현황 표 업데이트 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 판정 현황 표 UI 업데이트 (이벤트 기반)
        /// </summary>
        public void UpdateJudgmentStatusUI()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"판정 현황 표 업데이트: Total={totalCount}, OK={okCount}, NG={ngCount}, PTN={ptnCount}");

                // Total 행 업데이트
                UpdateStatusTableRow("Total", totalCount.ToString(), "1.00");

                // OK 행 업데이트
                double okRate = totalCount > 0 ? (double)okCount / totalCount : 0.0;
                UpdateStatusTableRow("OK", okCount.ToString(), okRate.ToString("F2"));

                // R/J(NG) 행 업데이트
                double ngRate = totalCount > 0 ? (double)ngCount / totalCount : 0.0;
                UpdateStatusTableRow("R/J", ngCount.ToString(), ngRate.ToString("F2"));

                // PTN 행 업데이트
                double ptnRate = totalCount > 0 ? (double)ptnCount / totalCount : 0.0;
                UpdateStatusTableRow("PTN", ptnCount.ToString(), ptnRate.ToString("F2"));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"판정 현황 표 UI 업데이트 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 판정 현황 표의 특정 행 업데이트 (이벤트 발생)
        /// </summary>
        private void UpdateStatusTableRow(string rowName, string quantity, string rate)
        {
            try
            {
                // View에게 업데이트 요청 이벤트 발생
                JudgmentStatusUpdateRequested?.Invoke(this, 
                    new JudgmentStatusUpdateEventArgs(rowName, quantity, rate));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"판정 현황 행 업데이트 오류 ({rowName}): {ex.Message}");
            }
        }
        #endregion

        #region 그래프 영역 관리
        private List<GraphDataPoint> graphDataPoints = new List<GraphDataPoint>();
        
        /// <summary>
        /// 그래프 데이터 포인트들 (공개 접근)
        /// </summary>
        public List<GraphDataPoint> GraphDataPoints => graphDataPoints;

        /// <summary>
        /// 그래프 데이터 포인트 클래스
        /// </summary>
        public class GraphDataPoint
        {
            public int ZoneNumber { get; set; }
            public string Judgment { get; set; }
            public DateTime Timestamp { get; set; }
            public int GlobalIndex { get; set; }  // 전역 인덱스 (0부터 시작)
        }

        /// <summary>
        /// 그래프 데이터 포인트 추가
        /// </summary>
        /// <param name="zoneNumber">Zone 번호</param>
        /// <param name="judgment">판정 결과</param>
        public void AddGraphDataPoint(int zoneNumber, string judgment)
        {
            try
            {
                // 새로운 데이터 포인트 추가
                var newPoint = new GraphDataPoint
                {
                    ZoneNumber = zoneNumber,
                    Judgment = judgment,
                    Timestamp = DateTime.Now,
                    GlobalIndex = graphDataPoints.Count  // 현재 개수를 GlobalIndex로 설정
                };

                graphDataPoints.Add(newPoint);

                // 최대 MAX_GRAPH_DATA_POINTS개까지만 유지 (FIFO)
                if (graphDataPoints.Count > DLL.DllConstants.MAX_GRAPH_DATA_POINTS)
                {
                    graphDataPoints.RemoveAt(0);
                    
                    // GlobalIndex 재조정 (0부터 다시 시작)
                    for (int i = 0; i < graphDataPoints.Count; i++)
                    {
                        graphDataPoints[i].GlobalIndex = i;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"그래프 데이터 포인트 추가: Zone{zoneNumber} - {judgment} (총 {graphDataPoints.Count}개, GlobalIndex: {newPoint.GlobalIndex})");

                // UI 스레드에서 그래프 업데이트
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    UpdateGraphUI();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"그래프 영역 업데이트 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 특정 Zone의 판정 결과 반환
        /// </summary>
        public string GetJudgmentForZone(int zoneNumber)
        {
            try
            {
                // Zone별 판정 결과 찾기 (Zone은 string 타입)
                var zoneData = DataItems?.Where(item => item.Zone == zoneNumber.ToString()).FirstOrDefault();
                return zoneData?.Judgment ?? "";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Zone {zoneNumber} 판정 결과 조회 오류: {ex.Message}");
                return "";
            }
        }

        /// <summary>
        /// 그래프 UI 업데이트
        /// </summary>
        private void UpdateGraphUI()
        {
            try
            {
                // OpticPage 인스턴스를 직접 사용 (생성자에서 받은 참조)
                // View에게 그래프 업데이트 요청 이벤트 발생
                GraphDisplayUpdateRequested?.Invoke(this, 
                    new GraphDisplayUpdateEventArgs(graphDataPoints));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"그래프 UI 업데이트 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 그래프 데이터 초기화
        /// </summary>
        public void ClearGraphData()
        {
            try
            {
                graphDataPoints.Clear();
                System.Diagnostics.Debug.WriteLine("그래프 데이터 초기화 완료");
                
                // UI 스레드에서 그래프 업데이트 (빈 데이터로)
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    UpdateGraphUI();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"그래프 데이터 초기화 오류: {ex.Message}");
            }
        }
        #endregion
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

    #region Event Arguments (View와의 통신용)
    
    /// <summary>
    /// 판정 현황 업데이트 이벤트 인자
    /// </summary>
    public class JudgmentStatusUpdateEventArgs : EventArgs
    {
        public string RowName { get; set; }
        public string Quantity { get; set; }
        public string Rate { get; set; }

        public JudgmentStatusUpdateEventArgs(string rowName, string quantity, string rate)
        {
            RowName = rowName;
            Quantity = quantity;
            Rate = rate;
        }
    }

    /// <summary>
    /// 그래프 표시 업데이트 이벤트 인자
    /// </summary>
    public class GraphDisplayUpdateEventArgs : EventArgs
    {
        public List<OpticPageViewModel.GraphDataPoint> DataPoints { get; set; }

        public GraphDisplayUpdateEventArgs(List<OpticPageViewModel.GraphDataPoint> dataPoints)
        {
            DataPoints = dataPoints;
        }
    }

    #endregion
}


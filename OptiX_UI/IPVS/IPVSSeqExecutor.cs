using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using OptiX.DLL;
using OptiX.Common;

namespace OptiX.IPVS
{
    /// <summary>
    /// IPVS 페이지 SEQ 시퀀스 실행 관리 클래스
    /// 
    /// 역할:
    /// - 테스트 시작/종료 관리
    /// - Zone별 SEQ 비동기 실행
    /// - DLL 함수 호출 순서 관리 (PGTurn, MEASTurn, PGPattern, MEAS, IPVS 테스트)
    /// - Zone별 테스트 완료 상태 관리
    /// - 결과 로그 생성
    /// 
    /// IPVS 특성:
    /// - output.IPVS_data[7][10] 데이터 사용
    /// - Sequence_IPVS.ini 파일에서 SEQ 읽기
    /// </summary>
    public class IPVSSeqExecutor
    {
        private readonly Action<List<IPVSPageViewModel.GraphDataPoint>> updateGraphDisplay;
        private readonly IPVSDataTableManager dataTableManager;
        private readonly IPVSPageViewModel viewModel;
        private bool isTestStarted = false;
        private bool[] zoneTestCompleted;
        private bool[] zoneMeasured;

        public IPVSSeqExecutor(
            Action<List<IPVSPageViewModel.GraphDataPoint>> updateGraphDisplay,
            IPVSDataTableManager dataTableManager,
            IPVSPageViewModel viewModel)
        {
            this.updateGraphDisplay = updateGraphDisplay ?? throw new ArgumentNullException(nameof(updateGraphDisplay));
            this.dataTableManager = dataTableManager ?? throw new ArgumentNullException(nameof(dataTableManager));
            this.viewModel = viewModel;

            // Zone별 상태 배열 초기화
            InitializeZoneArrays();
        }

        #region Public Methods

        /// <summary>
        /// Zone별 테스트 완료 상태 배열 초기화
        /// </summary>
        private void InitializeZoneArrays()
        {
            try
            {
                int zoneCount = int.Parse(GlobalDataManager.GetValue("Settings", "IPVS_ZONE", "2"));
                zoneTestCompleted = new bool[zoneCount];
                zoneMeasured = new bool[zoneCount];
            }
            catch
            {
                zoneTestCompleted = new bool[2];
                zoneMeasured = new bool[2];
            }
        }

        /// <summary>
        /// 테스트 시작
        /// </summary>
        public async void StartTest()
        {
            await StartTestAsync();
        }
        
        public async Task<bool> StartTestAsync()
        {
            try
            {
                if (isTestStarted)
                {
                    System.Diagnostics.Debug.WriteLine("이미 테스트가 실행 중입니다.");
                    return false;
                }

                System.Diagnostics.Debug.WriteLine("IPVS 테스트 시작");
                isTestStarted = true;

                // Zone 개수 읽기
                int zoneCount = int.Parse(GlobalDataManager.GetValue("Settings", "IPVS_ZONE", "2"));
                System.Diagnostics.Debug.WriteLine($"Zone 개수: {zoneCount}");

                // Zone별 비동기 실행
                var tasks = new List<Task>();
                for (int zone = 1; zone <= zoneCount; zone++)
                {
                    int zoneNumber = zone;
                    tasks.Add(ExecuteSeqForZoneAsync(zoneNumber));
                }

                // 모든 Zone 완료 대기
                await Task.WhenAll(tasks);

                // Zone 완료 대기 (finally 블록 실행 대기)
                await Task.Delay(DLL.DllConstants.ZONE_COMPLETION_DELAY_MS);

                System.Diagnostics.Debug.WriteLine("모든 Zone 테스트 완료");
                isTestStarted = false;

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"IPVS 테스트 시작 오류: {ex.Message}");
                isTestStarted = false;
                return false;
            }
        }

        /// <summary>
        /// Zone별 SEQ 실행
        /// </summary>
        private async Task ExecuteSeqForZoneAsync(int zoneNumber)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[Zone {zoneNumber}] IPVS SEQ 실행 시작");

                // Zone SEQ 시작 시간 기록
                SeqExecutionManager.SetZoneSeqStartTime(zoneNumber, DateTime.Now);

                // SEQ 순서 읽기 (Sequence_IPVS.ini)
                var seqOrder = ReadSeqOrder();

                // SEQ 실행
                foreach (var seq in seqOrder)
                {
                    await ExecuteSeqStep(zoneNumber, seq);
                }

                // Zone 완료 플래그 설정
                if (zoneNumber > 0 && zoneNumber <= zoneTestCompleted.Length)
                {
                    zoneTestCompleted[zoneNumber - 1] = true;
                }

                System.Diagnostics.Debug.WriteLine($"[Zone {zoneNumber}] IPVS SEQ 실행 완료");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Zone {zoneNumber}] SEQ 실행 오류: {ex.Message}");
            }
            finally
            {
                // Zone SEQ 종료 시간 기록 (반드시 실행)
                SeqExecutionManager.SetZoneSeqEndTime(zoneNumber, DateTime.Now);
            }
        }

        /// <summary>
        /// SEQ 순서 읽기 (Sequence_IPVS.ini)
        /// </summary>
        private List<string> ReadSeqOrder()
        {
            var seqList = new List<string>();

            try
            {
                // Sequence_IPVS.ini 파일 경로
                string seqIniPath = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Sequence_IPVS.ini"
                );

                if (!System.IO.File.Exists(seqIniPath))
                {
                    System.Diagnostics.Debug.WriteLine($"SEQ 파일 없음: {seqIniPath}");
                    return GetDefaultSeqOrder();
                }

                var seqIniManager = new IniFileManager(seqIniPath);
                string seqCountStr = seqIniManager.ReadValue("SEQ", "SEQ_COUNT", "5");
                int seqCount = int.Parse(seqCountStr);

                for (int i = 1; i <= seqCount; i++)
                {
                    string seqValue = seqIniManager.ReadValue("SEQ", $"SEQ_{i}", "");
                    if (!string.IsNullOrEmpty(seqValue))
                    {
                        seqList.Add(seqValue);
                    }
                }

                System.Diagnostics.Debug.WriteLine($"SEQ 순서 로드 완료: {seqList.Count}개");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SEQ 순서 읽기 오류: {ex.Message}");
                return GetDefaultSeqOrder();
            }

            return seqList.Count > 0 ? seqList : GetDefaultSeqOrder();
        }

        /// <summary>
        /// 기본 SEQ 순서
        /// </summary>
        private List<string> GetDefaultSeqOrder()
        {
            return new List<string>
            {
                "PGTurn",
                "MEASTurn",
                "PGPattern",
                "MEAS",
                "IPVS_TEST"
            };
        }

        /// <summary>
        /// SEQ 단계 실행
        /// </summary>
        private async Task ExecuteSeqStep(int zoneNumber, string seqName)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[Zone {zoneNumber}] SEQ 실행: {seqName}");

                switch (seqName.ToUpper())
                {
                    case "PGTURN":
                        await Task.Run(() => ExecutePGTurn(zoneNumber));
                        break;

                    case "MEASTURN":
                        await Task.Run(() => ExecuteMEASTurn(zoneNumber));
                        break;

                    case "PGPATTERN":
                        await Task.Run(() => ExecutePGPattern(zoneNumber));
                        break;

                    case "MEAS":
                        await Task.Run(() => ExecuteMEAS(zoneNumber));
                        break;

                    case "IPVS_TEST":
                        await Task.Run(() => ExecuteIPVSTest(zoneNumber));
                        break;

                    default:
                        System.Diagnostics.Debug.WriteLine($"알 수 없는 SEQ: {seqName}");
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Zone {zoneNumber}] SEQ 실행 오류 ({seqName}): {ex.Message}");
            }
        }

        /// <summary>
        /// PGTurn 실행
        /// </summary>
        private void ExecutePGTurn(int zoneNumber)
        {
            try
            {
                MonitorLogService.Instance.Log(zoneNumber - 1, $"ENTER PGTurn");
                bool result = DllFunctions.CallPGTurn(zoneNumber);
                MonitorLogService.Instance.Log(zoneNumber - 1, $"Execute PGTurn => {(result ? "OK" : "FAIL")}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PGTurn 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// MEASTurn 실행
        /// </summary>
        private void ExecuteMEASTurn(int zoneNumber)
        {
            try
            {
                MonitorLogService.Instance.Log(zoneNumber - 1, $"ENTER MEASTurn");
                bool result = DllFunctions.CallMeasTurn(zoneNumber);
                MonitorLogService.Instance.Log(zoneNumber - 1, $"Execute MEASTurn => {(result ? "OK" : "FAIL")}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MEASTurn 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// PGPattern 실행
        /// </summary>
        private void ExecutePGPattern(int zoneNumber)
        {
            try
            {
                MonitorLogService.Instance.Log(zoneNumber - 1, $"ENTER PGPattern");
                bool result = DllFunctions.CallPGPattern(zoneNumber);
                MonitorLogService.Instance.Log(zoneNumber - 1, $"Execute PGPattern => {(result ? "OK" : "FAIL")}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PGPattern 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// MEAS 실행
        /// </summary>
        private void ExecuteMEAS(int zoneNumber)
        {
            try
            {
                MonitorLogService.Instance.Log(zoneNumber - 1, $"ENTER MEAS");
                var (measureData, success) = DllFunctions.CallGetdata();
                MonitorLogService.Instance.Log(zoneNumber - 1, $"Execute MEAS => {(success ? "OK" : "FAIL")}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MEAS 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// IPVS 테스트 실행 (한 번만 호출, 운영설비가 모든 포인트 측정)
        /// </summary>
        private void ExecuteIPVSTest(int zoneNumber)
        {
            try
            {
                MonitorLogService.Instance.Log(zoneNumber - 1, $"ENTER IPVS_TEST");

                // Zone별 CELL_ID, INNER_ID 가져오기 (IPVS 섹션에서)
                string cellId = GlobalDataManager.GetValue("IPVS", $"CELL_ID_ZONE_{zoneNumber}", "");
                string innerId = GlobalDataManager.GetValue("IPVS", $"INNER_ID_ZONE_{zoneNumber}", "");

                // MAX_POINT 읽기
                int maxPoint = int.Parse(GlobalDataManager.GetValue("IPVS", "MAX_POINT", "5"));
                System.Diagnostics.Debug.WriteLine($"[Zone {zoneNumber}] IPVS 테스트 시작 (total_point={maxPoint})");

                // IPVS 테스트 실행 (한 번만)
                var input = new Input
                {
                    CELL_ID = cellId,
                    INNER_ID = innerId,
                    total_point = maxPoint,
                    cur_point = DLL.DllConstants.DEFAULT_CURRENT_POINT  // 항상 0
                };

                SeqExecutionManager.SetCurrentZone(zoneNumber);
                var (output, result) = DllFunctions.CallIPVSTestFunction(input);

                if (result)
                {
                    // 결과 저장
                    SeqExecutionManager.SetZoneResult(zoneNumber, output);

                    // ViewModel 업데이트 (UI 스레드에서 실행)
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        viewModel.UpdateDataTableWithDllResult(output, zoneNumber);
                    });

                    // IPVS 결과 로그 생성
                    DateTime startTime = SeqExecutionManager.GetZoneSeqStartTime(zoneNumber);
                    DateTime endTime = SeqExecutionManager.GetZoneSeqEndTime(zoneNumber);
                    if (endTime == default(DateTime) || endTime < startTime)
                    {
                        endTime = DateTime.Now;
                    }
                    
                    ResultLogManager.CreateIPVSResultLogsForZone(startTime, endTime, cellId, innerId, zoneNumber, output);

                    MonitorLogService.Instance.Log(zoneNumber - 1, $"Execute IPVS_TEST => OK");
                }
                else
                {
                    MonitorLogService.Instance.Log(zoneNumber - 1, $"Execute IPVS_TEST => FAIL");
                }
                
                System.Diagnostics.Debug.WriteLine($"[Zone {zoneNumber}] IPVS 테스트 완료");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"IPVS_TEST 오류: {ex.Message}");
                MonitorLogService.Instance.Log(zoneNumber - 1, $"IPVS_TEST failed");
            }
        }

        #endregion
    }
}


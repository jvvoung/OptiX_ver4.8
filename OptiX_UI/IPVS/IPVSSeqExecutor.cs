using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using OptiX.DLL;
using OptiX.Common;
using System.Security.Policy;

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
        private readonly Action<List<GraphManager.GraphDataPoint>> updateGraphDisplay;
        private readonly IPVSDataTableManager dataTableManager;
        private readonly IPVSPageViewModel viewModel;
        private readonly GraphManager graphManager;
        private bool isTestStarted = false;
        private bool[] zoneTestCompleted;
        private bool[] zoneMeasured;

        public IPVSSeqExecutor(
            Action<List<GraphManager.GraphDataPoint>> updateGraphDisplay,
            IPVSDataTableManager dataTableManager,
            IPVSPageViewModel viewModel,
            GraphManager graphManager)
        {
            this.updateGraphDisplay = updateGraphDisplay ?? throw new ArgumentNullException(nameof(updateGraphDisplay));
            this.dataTableManager = dataTableManager ?? throw new ArgumentNullException(nameof(dataTableManager));
            this.viewModel = viewModel;
            this.graphManager = graphManager ?? throw new ArgumentNullException(nameof(graphManager));

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
                    return false;
                }

                Common.ErrorLogger.Log("=== IPVS 테스트 시작 ===", Common.ErrorLogger.LogLevel.INFO);
                isTestStarted = true;

                // Zone 개수 읽기
                int zoneCount = int.Parse(GlobalDataManager.GetValue("Settings", "IPVS_ZONE", "2"));
                Common.ErrorLogger.Log($"Zone 개수: {zoneCount}", Common.ErrorLogger.LogLevel.INFO);

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

                Common.ErrorLogger.Log("=== IPVS 테스트 완료 ===", Common.ErrorLogger.LogLevel.INFO);
                isTestStarted = false;

                return true;
            }
            catch (Exception ex)
            {
                Common.ErrorLogger.LogException(ex, "IPVS 테스트 시작 중 오류");
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
                Common.ErrorLogger.Log($"IPVS SEQ 실행 시작", Common.ErrorLogger.LogLevel.INFO, zoneNumber);

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

                Common.ErrorLogger.Log($"IPVS SEQ 실행 완료", Common.ErrorLogger.LogLevel.INFO, zoneNumber);
            }
            catch (Exception ex)
            {
                Common.ErrorLogger.LogException(ex, "IPVS SEQ 실행 중 오류", zoneNumber);
            }
            finally
            {
                // Zone SEQ 종료 시간 기록 (반드시 실행)
                SeqExecutionManager.SetZoneSeqEndTime(zoneNumber, DateTime.Now);
            }
        }

        /// <summary>
        /// SEQ 순서 읽기 (SequenceCacheManager에서 캐시된 데이터 사용)
        /// </summary>
        private List<string> ReadSeqOrder()
        {
            // SequenceCacheManager에서 캐시된 시퀀스 가져오기 (파일 I/O 없음, 빠름!)
            return SequenceCacheManager.Instance.GetIPVSSequenceList();
        }


        /// <summary>
        /// SEQ 단계 실행 (OPTIC과 동일한 방식)
        /// </summary>
        private async Task ExecuteSeqStep(int zoneNumber, string item)
        {
            try
            {
                // 예: "PGTurn,1" 또는 "MEAS" 또는 "DELAY,50" 같은 항목
                string fnName;
                int? arg = null;

                var parts = item.Split(',');
                fnName = parts[0].Trim();
                if (parts.Length > 1 && int.TryParse(parts[1].Trim(), out int parsed))
                    arg = parsed;
                
                // 함수 진입 로그
                MonitorLogService.Instance.Log(zoneNumber - 1, $"ENTER {fnName}{(arg.HasValue ? "(" + arg.Value + ")" : string.Empty)}");

                // DELAY 처리: 밀리초 지연 (비동기)
                if (string.Equals(fnName, "DELAY", StringComparison.OrdinalIgnoreCase))
                {
                    int delayMs = arg ?? 0;
                    if (delayMs > 0)
                    {
                        MonitorLogService.Instance.Log(zoneNumber - 1, $"DELAY start {delayMs}ms");
                        await Task.Delay(delayMs);  // 비동기 지연
                        MonitorLogService.Instance.Log(zoneNumber - 1, "DELAY end");
                    }
                    return; // 다음 SEQ 항목으로
                }

                // IPVS 테스트는 직접 처리
                if (string.Equals(fnName, "IPVS", StringComparison.OrdinalIgnoreCase))
                {
                    await Task.Run(() => ExecuteIPVSTest(zoneNumber));
                    return;
                }

                // 나머지 함수들은 SeqExecutionManager.ExecuteMapped로 통일 처리
                bool ok = await SeqExecutionManager.ExecuteMappedAsync(fnName, arg, zoneNumber);

                // 실행 결과 로그
                MonitorLogService.Instance.Log(zoneNumber - 1, $"Execute {fnName}({(arg.HasValue ? arg.Value.ToString() : "-")}) => {(ok ? "OK" : "FAIL")}");
                
                if (!ok)
                {
                    Common.ErrorLogger.Log($"{fnName} 실행 실패", Common.ErrorLogger.LogLevel.WARNING, zoneNumber);
                }
            }
            catch (Exception ex)
            {
                Common.ErrorLogger.LogException(ex, $"SEQ 실행 오류 ({item})", zoneNumber);
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

                // Zone별 CELL_ID, INNER_ID 가져오기 (캐시된 IPVS Zone 정보 사용)
                var (cellId, innerId) = GlobalDataManager.GetIPVSZoneInfo(zoneNumber);

                // MAX_POINT 읽기
                int maxPoint = int.Parse(GlobalDataManager.GetValue("IPVS", "MAX_POINT", "5"));
                Common.ErrorLogger.Log($"IPVS 테스트 시작 (total_point={maxPoint}, Cell ID: '{cellId}', Inner ID: '{innerId}')", Common.ErrorLogger.LogLevel.INFO, zoneNumber);

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
                    _ = System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        viewModel.UpdateDataTableWithDllResult(output, zoneNumber, dataTableManager, graphManager);
                    }, System.Windows.Threading.DispatcherPriority.Background);

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
                
                Common.ErrorLogger.Log($"IPVS 테스트 완료", Common.ErrorLogger.LogLevel.INFO, zoneNumber);
            }
            catch (Exception ex)
            {
                Common.ErrorLogger.LogException(ex, "IPVS_TEST 실행 중 오류", zoneNumber);
                MonitorLogService.Instance.Log(zoneNumber - 1, $"IPVS_TEST failed");
            }
        }

        #endregion
    }
}


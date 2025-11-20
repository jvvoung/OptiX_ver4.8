using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using OptiX.OPTIC;
using OptiX.DLL;
using OptiX.Common;

namespace OptiX.OPTIC
{
    /// <summary>
    /// OPTIC 페이지 SEQ 시퀀스 실행 관리 클래스
    /// 
    /// 역할:
    /// - 테스트 시작/종료 관리
    /// - Zone별 SEQ 비동기 실행
    /// - DLL 함수 호출 순서 관리 (PGTurn, MEASTurn, PGPattern, MEAS, MTP)
    /// - Zone별 테스트 완료 상태 관리
    /// - 결과 로그 생성 (EECP, CIM, VALIDATION)
    /// 
    /// 사용하는 DLL 함수:
    /// - PGTurn: Pattern Generator 포트 연결
    /// - MEASTurn: 측정 장비 포트 연결
    /// - PGPattern: 패턴 전송
    /// - MEAS: 측정 데이터 가져오기
    /// - MTP: 메인 테스트 실행
    /// 
    /// 의존성:
    /// - DllManager (DLL 함수 호출)
    /// - GlobalDataManager (INI 설정 읽기)
    /// - OpticPageViewModel (테스트 상태 관리)
    /// 
    /// SEQ 실행 흐름:
    /// 1. INI 파일에서 SEQ 순서 읽기
    /// 2. Zone별로 병렬 실행
    /// 3. 각 Zone마다 SEQ 순서대로 DLL 함수 호출
    /// 4. 모든 Zone 완료 후 결과 로그 생성
    /// </summary>
    public class OpticSeqExecutor
    {
        private readonly Action<List<GraphManager.GraphDataPoint>> updateGraphDisplay;
        private readonly OpticDataTableManager dataTableManager;
        private readonly OpticPageViewModel viewModel;
        private readonly GraphManager graphManager;
        private readonly MonitorManager monitorManager;
        private bool isTestStarted = false;
        private bool[] zoneTestCompleted;
        private bool[] zoneMeasured;

        public OpticSeqExecutor(
            Action<List<GraphManager.GraphDataPoint>> updateGraphDisplay,
            OpticDataTableManager dataTableManager,
            OpticPageViewModel viewModel,
            GraphManager graphManager,
            MonitorManager monitorManager)
        {
            this.updateGraphDisplay = updateGraphDisplay ?? throw new ArgumentNullException(nameof(updateGraphDisplay));
            this.dataTableManager = dataTableManager ?? throw new ArgumentNullException(nameof(dataTableManager));
            this.viewModel = viewModel;
            this.graphManager = graphManager ?? throw new ArgumentNullException(nameof(graphManager));
            this.monitorManager = monitorManager ?? throw new ArgumentNullException(nameof(monitorManager));
        }

        /// <summary>
        /// Zone별 테스트 완료 상태 배열 초기화
        /// </summary>
        public void InitializeZoneTestStates()
        {
            try
            {
                int zoneCount = int.Parse(GlobalDataManager.GetValue("Settings", "MTP_ZONE", "2"));
                zoneTestCompleted = new bool[zoneCount];
                zoneMeasured = new bool[zoneCount];

                for (int i = 0; i < zoneCount; i++)
                {
                    zoneTestCompleted[i] = false;
                    zoneMeasured[i] = false;
                }

                ErrorLogger.Log($"Zone 테스트 상태 초기화 완료 - {zoneCount}개 Zone", ErrorLogger.LogLevel.INFO);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogException(ex, "Zone 테스트 상태 초기화 중 오류");
            }
        }

        //25.10.30 - 중복 실행 방지 개선
        /// <summary>
        /// 테스트 시작 (UI 스레드에서 호출)
        /// </summary>
        public void StartTest()
        {
            //25.10.30 - 이미 실행 중이면 무시 (중복 클릭 방지)
            if (isTestStarted)
            {
                System.Diagnostics.Debug.WriteLine("[OpticSeqExecutor] 이미 테스트 실행 중 - 중복 실행 무시");
                return;
            }
            
            isTestStarted = true;
            Task.Run(() => StartTestAsync());
        }

        /// <summary>
        /// 테스트 비동기 실행
        /// </summary>
        private async Task StartTestAsync()
        {
            try
            {
                ErrorLogger.Log("=== OPTIC 테스트 시작 ===", ErrorLogger.LogLevel.INFO);

                // DLL 초기화 확인
                if (!DllManager.IsInitialized)
                {
                    _ = System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        MessageBox.Show("DLL이 초기화되지 않았습니다. 메인 설정창에서 DLL 경로를 확인해주세요.",
                                      "오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }, System.Windows.Threading.DispatcherPriority.Background);
                    return;
                }

                //25.10.29 - 전체 Zone 컨텍스트 초기화
                SeqExecutionManager.ResetAllZones();

                // Zone 개수 읽기 (올바른 INI 섹션/키 사용)
                int zoneCount = int.Parse(GlobalDataManager.GetValue("Settings", "MTP_ZONE", "2"));
                ErrorLogger.Log($"Zone 개수: {zoneCount}", ErrorLogger.LogLevel.INFO);

                bool isHviMode = GlobalDataManager.IsHviModeEnabled();
                ErrorLogger.Log($"HVI 모드: {(isHviMode ? "활성" : "비활성")}", ErrorLogger.LogLevel.INFO);

                // SequenceCacheManager에서 캐싱된 시퀀스 가져오기
                var cachedSeq = SequenceCacheManager.Instance.GetOpticSequenceCopy();
                if (cachedSeq == null || cachedSeq.Count == 0)
                {
                    ErrorLogger.Log("시퀀스가 로드되지 않음", ErrorLogger.LogLevel.WARNING);
                    _ = System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        MessageBox.Show("시퀀스가 로드되지 않았습니다. Sequence_Optic.ini 파일을 확인해주세요.",
                                      "오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }, System.Windows.Threading.DispatcherPriority.Background);
                    return;
                }

                // Queue를 List로 변환 (모든 Zone에서 같은 시퀀스 사용)
                var orderedSeq = cachedSeq.ToList();
                ErrorLogger.Log($"SEQ 개수: {orderedSeq.Count}", ErrorLogger.LogLevel.INFO);

                // Zone별로 병렬 실행
                var tasks = new List<Task>();
                for (int zoneId = 1; zoneId <= zoneCount; zoneId++)
                {
                    int currentZoneId = zoneId;
                    tasks.Add(Task.Run(() => ExecuteSeqForZoneAsync(currentZoneId, orderedSeq, isHviMode)));
                }

                // 모든 Zone 완료 대기
                await Task.WhenAll(tasks);
                
                // 짧은 지연: 모든 Zone의 finally 블록이 완료될 시간 제공 (Race Condition 방지)
                await Task.Delay(DLL.DllConstants.ZONE_COMPLETION_DELAY_MS);

                // SEQ 종료 시간 설정
                SeqExecutionManager.SetSeqEndTime(DateTime.Now);
                ErrorLogger.Log("=== 모든 Zone OPTIC SEQ 완료 ===", ErrorLogger.LogLevel.INFO);

                if (isHviMode)
                {
                    await FinalizeHviModeAsync(zoneCount);
                }
                else
                {
                    int[] zones = Enumerable.Range(1, zoneCount).ToArray();
                    await FinalizeNormalModeAsync(zones);
                }

                // 그래프 최종 업데이트 (HVI / 일반 공통)
                _ = Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        var graphDataPoints = graphManager?.GetDataPoints();
                        if (graphDataPoints != null && graphDataPoints.Count > 0)
                        {
                            updateGraphDisplay?.Invoke(graphDataPoints);
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorLogger.LogException(ex, "그래프 최종 업데이트 중 오류");
                    }
                }, System.Windows.Threading.DispatcherPriority.Normal);
            }
            finally
            {
                //25.10.30 - 테스트 완료 시 플래그 초기화 (다음 테스트 실행 가능하도록)
                isTestStarted = false;
                System.Diagnostics.Debug.WriteLine("[OpticSeqExecutor] 테스트 종료 - 플래그 초기화");
                
                // 외부 INPUT 데이터 초기화
                viewModel?.ClearExternalInputData();
            }
        }

        private async Task FinalizeNormalModeAsync(int[] zones)
        {
            try
            {
                await CreateAllResultLogs(zones);
                ErrorLogger.Log("=== OPTIC 테스트 완료 ===", ErrorLogger.LogLevel.INFO);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogException(ex, "정상 모드 결과 로그 생성 중 오류");
            }
        }

        //25.10.30 - 모든 Zone 완료 후 EECP/Summary 로그 생성 (데이터 통합)
        private async Task CreateAllResultLogs(int[] zones)
        {
            try
            {
                ErrorLogger.Log($"전체 결과 로그 생성 시작 - {zones.Length}개 Zone", ErrorLogger.LogLevel.INFO);

                // 모든 Zone의 데이터 수집
                var zoneData = new System.Collections.Generic.Dictionary<int, (string cellId, string innerId, Output output)>();
                foreach (int zoneNumber in zones)
                {
                    var storedOutput = SeqExecutionManager.GetStoredZoneResult(zoneNumber);
                    if (storedOutput.HasValue)
                    {
                        var (cellId, innerId) = GlobalDataManager.GetZoneInfo(zoneNumber);
                        zoneData[zoneNumber] = (cellId, innerId, storedOutput.Value);
                    }
                }

                if (zoneData.Count > 0)
                {
                    DateTime startTime = SeqExecutionManager.GetZoneSeqStartTime(zones.FirstOrDefault());
                    DateTime endTime = SeqExecutionManager.GetZoneSeqEndTime(zones.LastOrDefault());

                    ErrorLogger.Log($"전체 결과 로그 생성 시작 (Zone {zoneData.Count}개)", ErrorLogger.LogLevel.INFO);
                    
                    bool success = await Task.Run(() => ResultLogManager.CreateAllResultLogs(
                        startTime,
                        endTime,
                        zoneData
                    ));
                    
                    ErrorLogger.Log($"전체 로그 생성 결과: {success}", ErrorLogger.LogLevel.INFO);
                }
                
                ErrorLogger.Log("전체 결과 로그 생성 완료", ErrorLogger.LogLevel.INFO);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogException(ex, "전체 결과 로그 생성 중 오류");
            }
        }
        
        private Task FinalizeHviModeAsync(int zoneCount)
        {
            try
            {
                ErrorLogger.Log("HVI 모드 최종 처리 시작", ErrorLogger.LogLevel.INFO);

                var outputs = new List<Output>();
                var contexts = new List<ZoneContext>();

                for (int zone = 1; zone <= zoneCount; zone++)
                {
                    try
                    {
                        var context = SeqExecutionManager.GetZoneContext(zone);
                        contexts.Add(context);
                        outputs.Add(context.SharedOutput);
                    }
                    catch (Exception ex)
                    {
                        ErrorLogger.LogException(ex, "HVI 모드 컨텍스트 조회 중 오류", zone);
                    }
                }

                outputs = outputs.Where(o => o.data != null && o.data.Length > 0).ToList();

                if (outputs.Count == 0)
                {
                    ErrorLogger.Log("HVI 모드: 유효한 측정 데이터가 없어 판정을 건너뜁니다.", ErrorLogger.LogLevel.WARNING);
                    return Task.CompletedTask;
                }

                DateTime earliestStart = contexts
                    .Where(c => c != null && c.SeqStartTime != default)
                    .Select(c => c.SeqStartTime)
                    .DefaultIfEmpty(DateTime.Now)
                    .Min();

                DateTime latestEnd = contexts
                    .Where(c => c != null && c.SeqEndTime != default)
                    .Select(c => c.SeqEndTime)
                    .DefaultIfEmpty(DateTime.Now)
                    .Max();

                var combinedOutput = CombineHviOutputs(outputs);

                var zone1Context = SeqExecutionManager.GetZoneContext(1);
                zone1Context.SharedOutput = combinedOutput;
                zone1Context.SeqStartTime = earliestStart;
                zone1Context.SeqEndTime = latestEnd;
                SeqExecutionManager.SetZoneSeqStartTime(1, earliestStart);
                SeqExecutionManager.SetZoneSeqEndTime(1, latestEnd);

                string cellIdForHvi = zone1Context.SharedInput.CELL_ID ?? "";
                string innerIdForHvi = zone1Context.SharedInput.INNER_ID ?? "";
                if (string.IsNullOrWhiteSpace(cellIdForHvi) || string.IsNullOrWhiteSpace(innerIdForHvi))
                {
                    var (fallbackCell, fallbackInner) = GlobalDataManager.GetZoneInfo(1);
                    if (string.IsNullOrWhiteSpace(cellIdForHvi)) cellIdForHvi = fallbackCell;
                    if (string.IsNullOrWhiteSpace(innerIdForHvi)) innerIdForHvi = fallbackInner;
                }

                var handler = new DllResultHandler();
                string categoriesStr = GlobalDataManager.GetValue("MTP", "Category", "W,WG,R,G,B");
                string[] categoryNames = categoriesStr.Split(',').Select(c => c.Trim()).ToArray();
                int selectedWadIndex = viewModel?.SelectedWadIndex ?? 0;

                MonitorLogService.Instance.Log(0, "ENTER JUDGMENT (HVI)");
                string zoneJudgment = handler.ProcessOpticResult(
                    combinedOutput,
                    "1",
                    dataTableManager,
                    selectedWadIndex,
                    categoryNames,
                    null
                );
                MonitorLogService.Instance.Log(0, $"JUDGMENT => {zoneJudgment}");

                double tactSeconds = (latestEnd - earliestStart).TotalSeconds;
                if (tactSeconds < 0) tactSeconds = 0;
                string tactStr = tactSeconds.ToString("F3");
                string errorName = DetermineErrorName(combinedOutput, zoneJudgment);
                var testResult = ZoneTestResult.Create(errorName, tactStr, zoneJudgment);

                for (int zone = 1; zone <= zoneCount; zone++)
                {
                    SeqExecutionManager.StoreZoneTestResult(zone, testResult);
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        for (int zone = 1; zone <= zoneCount; zone++)
                        {
                            dataTableManager?.UpdateZoneFullTestResult(zone.ToString(), testResult);
                        }
                        dataTableManager?.UpdateZoneCellInfo("1", cellIdForHvi, innerIdForHvi);
                        viewModel?.RefreshDataTable();

                        if (!string.IsNullOrEmpty(zoneJudgment))
                        {
                            viewModel?.AddGraphDataPoint(1, zoneJudgment, graphManager);
                            viewModel?.UpdateJudgmentStatusTable(zoneJudgment);
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorLogger.LogException(ex, "HVI 모드 UI 업데이트 중 오류");
                    }
                }, System.Windows.Threading.DispatcherPriority.Send);

                MonitorLogService.Instance.Log(0, "ENTER CIM (HVI)");

                _ = Task.Run(() =>
                {
                    try
                    {
                        bool cimSuccess = ResultLogManager.CreateCIMForZone(
                            earliestStart,
                            latestEnd,
                            cellIdForHvi,
                            innerIdForHvi,
                            1,
                            combinedOutput,
                            testResult
                        );

                        MonitorLogService.Instance.Log(0, $"CIM {(cimSuccess ? "OK" : "FAIL")}");

                        var zoneData = new Dictionary<int, (string cellId, string innerId, Output output)>
                        {
                            { 1, (cellIdForHvi, innerIdForHvi, combinedOutput) }
                        };

                        bool logSuccess = ResultLogManager.CreateAllResultLogs(
                            earliestStart,
                            latestEnd,
                            zoneData
                        );

                        ErrorLogger.Log($"=== OPTIC 테스트 완료 (HVI) / 로그 결과: {logSuccess} ===", ErrorLogger.LogLevel.INFO);
                    }
                    catch (Exception ex)
                    {
                        ErrorLogger.LogException(ex, "HVI 모드 로그 생성 중 오류");
                        MonitorLogService.Instance.Log(0, "PROCESS ERROR");
                    }
                });
            }
            catch (Exception ex)
            {
                ErrorLogger.LogException(ex, "HVI 모드 최종 처리 중 오류");
            }

            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Zone별 SEQ 비동기 실행
        /// </summary>
        private async Task ExecuteSeqForZoneAsync(int zoneId, List<string> orderedSeq, bool isHviMode)
        {
            try
            {
                await ExecuteSeqForZone(zoneId, orderedSeq);

                // 존 완료 표시
                SetZoneTestCompleted(zoneId - 1, true);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogException(ex, $"Zone SEQ 실행 중 오류", zoneId);
            }
            finally
            {
                //25.10.29 - Zone SEQ 종료 - 종료 시간 기록
                SeqExecutionManager.EndZoneSeq(zoneId);
                
                if (isHviMode)
                {
                    await ProcessZoneResultHviBufferingAsync(zoneId);
                }
                else
                {
                    await ProcessZoneResultNormalAsync(zoneId);
                }
            }
        }

        private async Task ProcessZoneResultNormalAsync(int zoneId)
        {
            try
            {
                var (cellId, innerId) = GlobalDataManager.GetZoneInfo(zoneId);
                DateTime startTime = SeqExecutionManager.GetZoneSeqStartTime(zoneId);
                DateTime endTime = SeqExecutionManager.GetZoneSeqEndTime(zoneId);
                Output? storedOutput = SeqExecutionManager.GetStoredZoneResult(zoneId);

                if (!storedOutput.HasValue)
                {
                    return;
                }

                MonitorLogService.Instance.Log(zoneId - 1, "ENTER JUDGMENT");

                var handler = new DllResultHandler();
                string categoriesStr = GlobalDataManager.GetValue("MTP", "Category", "W,WG,R,G,B");
                string[] categoryNames = categoriesStr.Split(',').Select(c => c.Trim()).ToArray();
                int selectedWadIndex = viewModel?.SelectedWadIndex ?? 0;

                string zoneJudgment = handler.ProcessOpticResult(
                    storedOutput.Value,
                    zoneId.ToString(),
                    dataTableManager,
                    selectedWadIndex,
                    categoryNames,
                    null);

                MonitorLogService.Instance.Log(zoneId - 1, $"JUDGMENT => {zoneJudgment}");

                double tactSeconds = (endTime - startTime).TotalSeconds;
                string tactStr = tactSeconds.ToString("F3");
                string errorName = DetermineErrorName(storedOutput.Value, zoneJudgment);

                var testResult = ZoneTestResult.Create(errorName, tactStr, zoneJudgment);
                SeqExecutionManager.StoreZoneTestResult(zoneId, testResult);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        dataTableManager?.UpdateZoneFullTestResult(zoneId.ToString(), testResult);
                        viewModel?.RefreshDataTable();

                        if (!string.IsNullOrEmpty(zoneJudgment))
                        {
                            viewModel?.AddGraphDataPoint(zoneId, zoneJudgment, graphManager);
                            viewModel?.UpdateJudgmentStatusTable(zoneJudgment);
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorLogger.LogException(ex, $"Zone {zoneId} UI 업데이트 중 오류", zoneId);
                    }
                }, System.Windows.Threading.DispatcherPriority.Send);

                MonitorLogService.Instance.Log(zoneId - 1, "ENTER CIM");

                _ = Task.Run(() =>
                {
                    bool success = ResultLogManager.CreateCIMForZone(
                        startTime,
                        endTime,
                        cellId,
                        innerId,
                        zoneId,
                        storedOutput.Value,
                        testResult);

                    MonitorLogService.Instance.Log(zoneId - 1, $"CIM {(success ? "OK" : "FAIL")}");
                });
            }
            catch (Exception ex)
            {
                ErrorLogger.LogException(ex, "Zone 판정/CIM 로그 생성 중 오류", zoneId);
                MonitorLogService.Instance.Log(zoneId - 1, "PROCESS ERROR");
            }
        }

        private Task ProcessZoneResultHviBufferingAsync(int zoneId)
        {
            try
            {
                Output? storedOutput = SeqExecutionManager.GetStoredZoneResult(zoneId);
                if (storedOutput.HasValue)
                {
                    var handler = new DllResultHandler();
                    string categoriesStr = GlobalDataManager.GetValue("MTP", "Category", "W,WG,R,G,B");
                    string[] categoryNames = categoriesStr.Split(',').Select(c => c.Trim()).ToArray();
                    int selectedWadIndex = viewModel?.SelectedWadIndex ?? 0;

                    handler.ProcessOpticResult(
                        storedOutput.Value,
                        zoneId.ToString(),
                        dataTableManager,
                        selectedWadIndex,
                        categoryNames,
                        null,
                        applyZoneJudgment: false);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            viewModel?.RefreshDataTable();
                        }
                        catch (Exception ex)
                        {
                            ErrorLogger.LogException(ex, $"HVI 모드 Zone {zoneId} UI 갱신 중 오류", zoneId);
                        }
                    }, System.Windows.Threading.DispatcherPriority.Send);
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogException(ex, "HVI 모드 Zone 측정 데이터 업데이트 중 오류", zoneId);
            }

            return Task.CompletedTask;
        }

        //25.10.29 - Zone SEQ 실행 메서드 리팩토링 (새로운 API 사용)
        /// <summary>
        /// Zone별 SEQ 실행 (최신 버전 - View와 동일)
        /// </summary>
        private async Task ExecuteSeqForZone(int zoneId, List<string> orderedSeq)
        {
            ErrorLogger.Log($"Zone SEQ 실행 시작", ErrorLogger.LogLevel.INFO, zoneId);
            
            //25.10.29 - Zone SEQ 시작 - 컨텍스트 생성 및 Input 설정
            // 외부 INPUT 데이터 확인 (통신으로부터 - Zone별)
            var (externalCellID, externalInnerID, hasExternalData) = viewModel?.GetExternalInputData(zoneId) ?? ("", "", false);
            
            string cellId, innerId;
            if (hasExternalData)
            {
                // 외부 데이터 사용 (CellID = LotID)
                cellId = externalCellID;
                innerId = externalInnerID;
                System.Diagnostics.Debug.WriteLine($"[OpticSeqExecutor] Zone {zoneId} - 외부 INPUT 사용: CELL_ID={cellId}, INNER_ID={innerId}");
                ErrorLogger.Log($"Zone {zoneId} - 외부 INPUT 사용: CELL_ID={cellId}, INNER_ID={innerId}", ErrorLogger.LogLevel.INFO, zoneId);
            }
            else
            {
                // 기존 방식: GlobalDataManager에서 가져오기
                (cellId, innerId) = GlobalDataManager.GetZoneInfo(zoneId);
                System.Diagnostics.Debug.WriteLine($"[OpticSeqExecutor] Zone {zoneId} - 기본 INPUT 사용: CELL_ID={cellId}, INNER_ID={innerId}");
            }
            
            int totalPoint = DllConstants.DEFAULT_CURRENT_POINT; // MTP는 119 포인트 (기본값)
            
            SeqExecutionManager.StartZoneSeq(zoneId, cellId, innerId, totalPoint, isIPVS: false);
            
            // 시퀀스를 Queue로 변환 (POP 방식으로 진행)
            var sequenceQueue = new Queue<string>(orderedSeq);

            while (sequenceQueue.Count > 0)
            {
                var item = sequenceQueue.Dequeue(); // Queue에서 POP

                // 예: "PGTurn,1" 또는 "MEAS" 같은 항목
                string fnName;
                int? arg = null;

                var parts = item.Split(',');
                fnName = parts[0].Trim();
                if (parts.Length > 1 && int.TryParse(parts[1].Trim(), out int parsed))
                    arg = parsed;
                
                //25.10.30 - 함수 진입 즉시 로그 (MonitorLogService는 이미 비동기 큐 방식이므로 빠름)
                MonitorLogService.Instance.Log(zoneId - 1, $"ENTER {fnName}{(arg.HasValue ? "(" + arg.Value + ")" : string.Empty)}");

                // DELAY 처리: 밀리초 지연 (비동기)
                if (string.Equals(fnName, "DELAY", StringComparison.OrdinalIgnoreCase))
                {
                    int delayMs = arg ?? 0;
                    if (delayMs > 0)
                    {
                        //25.10.30 - Task.Run() 제거 (순서 보장)
                        MonitorLogService.Instance.Log(zoneId - 1, $"DELAY start {delayMs}ms");
                        
                        await Task.Delay(delayMs);  // 비동기 지연으로 UI 스레드 블록 방지
                        
                        //25.10.30 - Task.Run() 제거 (순서 보장)
                        MonitorLogService.Instance.Log(zoneId - 1, "DELAY end");
                    }
                    continue; // 다음 SEQ 항목으로
                }

                // 모든 함수를 ExecuteMapped로 통일 처리 (비동기로 UI 스레드 블록 방지)
                bool ok = await SeqExecutionManager.ExecuteMappedAsync(fnName, arg, zoneId);
                
                //25.10.30 - Task.Run() 제거 (순서 보장)
                MonitorLogService.Instance.Log(zoneId - 1, $"Execute {fnName}({(arg.HasValue ? arg.Value.ToString() : "-")}) => {(ok ? "OK" : "FAIL")}");
                
                if (!ok)
                {
                    ErrorLogger.Log($"{fnName} 실행 실패", ErrorLogger.LogLevel.WARNING, zoneId);
                    //25.10.30 - Task.Run() 제거 (순서 보장)
                    MonitorLogService.Instance.Log(zoneId - 1, $"{fnName} failed");
                    // 실패 정책: 일단 계속 진행
                }
            }
        }

        /// <summary>
        /// 테스트 중지
        /// </summary>
        public void StopTest()
        {
            isTestStarted = false;
            ErrorLogger.Log("테스트 중지 요청", ErrorLogger.LogLevel.INFO);
        }

        /// <summary>
        /// Zone별 테스트 완료 상태 설정
        /// </summary>
        public void SetZoneTestCompleted(int zoneIndex, bool completed)
        {
            // null 체크
            if (zoneTestCompleted == null)
            {
                InitializeZoneTestStates();
            }

            if (zoneTestCompleted != null && zoneIndex >= 0 && zoneIndex < zoneTestCompleted.Length)
            {
                zoneTestCompleted[zoneIndex] = completed;
            }
        }

        /// <summary>
        /// Zone별 테스트 완료 상태 확인
        /// </summary>
        public bool IsZoneTestCompleted(int zoneIndex)
        {
            if (zoneTestCompleted == null)
            {
                return false;
            }

            if (zoneIndex >= 0 && zoneIndex < zoneTestCompleted.Length)
            {
                return zoneTestCompleted[zoneIndex];
            }
            return false;
        }

        /// <summary>
        /// 테스트 시작 상태 확인
        /// </summary>
        public bool IsTestStarted()
        {
            return isTestStarted;
        }

        private Output CombineHviOutputs(IList<Output> outputs)
        {
            var combined = new Output
            {
                data = new Pattern[DllConstants.OPTIC_DATA_SIZE],
                IPVS_data = new Pattern[DllConstants.IPVS_DATA_SIZE],
                measure = new Pattern[DllConstants.MAX_WAD_COUNT],
                lut = new LUTParameter[DllConstants.RGB_CHANNEL_COUNT]
            };

            if (outputs == null || outputs.Count == 0)
            {
                return combined;
            }

            for (int i = 0; i < DllConstants.OPTIC_DATA_SIZE; i++)
            {
                float sumX = 0f, sumY = 0f, sumU = 0f, sumV = 0f, sumL = 0f, sumCur = 0f, sumEff = 0f;
                int maxResult = 0;
                int validCount = 0;

                foreach (var output in outputs)
                {
                    if (output.data == null || output.data.Length <= i) continue;
                    var pattern = output.data[i];
                    sumX += pattern.x;
                    sumY += pattern.y;
                    sumU += pattern.u;
                    sumV += pattern.v;
                    sumL += pattern.L;
                    sumCur += pattern.cur;
                    sumEff += pattern.eff;
                    maxResult = Math.Max(maxResult, pattern.result);
                    validCount++;
                }

                if (validCount > 0)
                {
                    combined.data[i] = new Pattern
                    {
                        x = sumX / validCount,
                        y = sumY / validCount,
                        u = sumU / validCount,
                        v = sumV / validCount,
                        L = sumL / validCount,
                        cur = sumCur / validCount,
                        eff = sumEff / validCount,
                        result = maxResult
                    };
                }
            }

            for (int i = 0; i < DllConstants.IPVS_DATA_SIZE; i++)
            {
                float sumX = 0f, sumY = 0f, sumU = 0f, sumV = 0f, sumL = 0f, sumCur = 0f, sumEff = 0f;
                int maxResult = 0;
                int validCount = 0;

                foreach (var output in outputs)
                {
                    if (output.IPVS_data == null || output.IPVS_data.Length <= i) continue;
                    var pattern = output.IPVS_data[i];
                    sumX += pattern.x;
                    sumY += pattern.y;
                    sumU += pattern.u;
                    sumV += pattern.v;
                    sumL += pattern.L;
                    sumCur += pattern.cur;
                    sumEff += pattern.eff;
                    maxResult = Math.Max(maxResult, pattern.result);
                    validCount++;
                }

                if (validCount > 0)
                {
                    combined.IPVS_data[i] = new Pattern
                    {
                        x = sumX / validCount,
                        y = sumY / validCount,
                        u = sumU / validCount,
                        v = sumV / validCount,
                        L = sumL / validCount,
                        cur = sumCur / validCount,
                        eff = sumEff / validCount,
                        result = maxResult
                    };
                }
            }

            for (int i = 0; i < DllConstants.MAX_WAD_COUNT; i++)
            {
                float sumX = 0f, sumY = 0f, sumU = 0f, sumV = 0f, sumL = 0f, sumCur = 0f, sumEff = 0f;
                int maxResult = 0;
                int validCount = 0;

                foreach (var output in outputs)
                {
                    if (output.measure == null || output.measure.Length <= i) continue;
                    var pattern = output.measure[i];
                    sumX += pattern.x;
                    sumY += pattern.y;
                    sumU += pattern.u;
                    sumV += pattern.v;
                    sumL += pattern.L;
                    sumCur += pattern.cur;
                    sumEff += pattern.eff;
                    maxResult = Math.Max(maxResult, pattern.result);
                    validCount++;
                }

                if (validCount > 0)
                {
                    combined.measure[i] = new Pattern
                    {
                        x = sumX / validCount,
                        y = sumY / validCount,
                        u = sumU / validCount,
                        v = sumV / validCount,
                        L = sumL / validCount,
                        cur = sumCur / validCount,
                        eff = sumEff / validCount,
                        result = maxResult
                    };
                }
            }

            for (int i = 0; i < DllConstants.RGB_CHANNEL_COUNT; i++)
            {
                float sumMaxLumi = 0f, sumMaxIndex = 0f, sumGamma = 0f, sumBlack = 0f;
                int validCount = 0;

                foreach (var output in outputs)
                {
                    if (output.lut == null || output.lut.Length <= i) continue;
                    var lut = output.lut[i];
                    sumMaxLumi += lut.max_lumi;
                    sumMaxIndex += lut.max_index;
                    sumGamma += lut.gamma;
                    sumBlack += lut.black;
                    validCount++;
                }

                if (validCount > 0)
                {
                    combined.lut[i] = new LUTParameter
                    {
                        max_lumi = sumMaxLumi / validCount,
                        max_index = sumMaxIndex / validCount,
                        gamma = sumGamma / validCount,
                        black = sumBlack / validCount
                    };
                }
            }

            return combined;
        }

        /// <summary>
        /// Zone 테스트 결과에서 에러명 판정
        /// </summary>
        private string DetermineErrorName(Output output, string zoneJudgment)
        {
            try
            {
                // OK일 경우 에러 없음
                if (zoneJudgment == "OK")
                {
                    return "";
                }

                // NG/PTN일 경우 상세 에러 분석
                if (output.data == null || output.data.Length == 0)
                {
                    return "NO_DATA";
                }

                // 패턴별 에러 카운트
                int specOutCount = 0;  // result == 1 (NG)
                int patternFailCount = 0;  // result == 2 (PTN)
                
                foreach (var pattern in output.data)
                {
                    if (pattern.result == 1)
                    {
                        specOutCount++;
                    }
                    else if (pattern.result == 2)
                    {
                        patternFailCount++;
                    }
                }

                // 에러 우선순위 판정
                if (patternFailCount > 0)
                {
                    return "PATTERN_FAIL"; // 패턴 불량이 있으면 최우선
                }
                else if (specOutCount > 0)
                {
                    return "SPEC_OUT"; // 스펙 아웃
                }
                else if (zoneJudgment == "NG" || zoneJudgment == "R/J" || zoneJudgment.Contains("R"))
                {
                    return "JUDGMENT_NG"; // 판정 NG/Reject (원인 불명)
                }

                return ""; // 기타
            }
            catch (Exception ex)
            {
                ErrorLogger.LogException(ex, "에러명 판정 중 오류");
                return "ERROR_UNKNOWN";
            }
        }
    }
}




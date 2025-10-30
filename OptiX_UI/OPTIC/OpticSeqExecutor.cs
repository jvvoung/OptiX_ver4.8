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
        private bool isTestStarted = false;
        private bool[] zoneTestCompleted;
        private bool[] zoneMeasured;

        public OpticSeqExecutor(
            Action<List<GraphManager.GraphDataPoint>> updateGraphDisplay,
            OpticDataTableManager dataTableManager,
            OpticPageViewModel viewModel,
            GraphManager graphManager)
        {
            this.updateGraphDisplay = updateGraphDisplay ?? throw new ArgumentNullException(nameof(updateGraphDisplay));
            this.dataTableManager = dataTableManager ?? throw new ArgumentNullException(nameof(dataTableManager));
            this.viewModel = viewModel;
            this.graphManager = graphManager ?? throw new ArgumentNullException(nameof(graphManager));
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
                    tasks.Add(Task.Run(() => ExecuteSeqForZoneAsync(currentZoneId, orderedSeq)));
                }

                // 모든 Zone 완료 대기
                await Task.WhenAll(tasks);
                
                // 짧은 지연: 모든 Zone의 finally 블록이 완료될 시간 제공 (Race Condition 방지)
                await Task.Delay(DLL.DllConstants.ZONE_COMPLETION_DELAY_MS);

                // SEQ 종료 시간 설정
                SeqExecutionManager.SetSeqEndTime(DateTime.Now);
                ErrorLogger.Log("=== 모든 Zone OPTIC SEQ 완료 ===", ErrorLogger.LogLevel.INFO);

                int[] zones = Enumerable.Range(1, zoneCount).ToArray();
                
                //25.10.31 - UI 병목 해결 핵심: 로그 생성을 UI 업데이트보다 뒤로!
                // 1. UI 업데이트 먼저 (즉시 반응)
                // 2. 로그 생성은 백그라운드에서 (UI 블록 없이)
                
                // 단 한 번의 UI 업데이트로 모든 Zone 데이터를 동시에 표시
                _ = Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        if (viewModel != null && dataTableManager != null && graphManager != null)
                        {
                            int lastZone = zones.LastOrDefault();
                            
                            //25.10.31 - 모든 Zone 데이터 업데이트 (동시에!)
                            foreach (int zoneNumber in zones)
                            {
                                var storedOutput = SeqExecutionManager.GetStoredZoneResult(zoneNumber);
                                if (storedOutput.HasValue)
                                {
                                    try
                                    {
                                        bool isLastZone = (zoneNumber == lastZone);
                                        
                                        // 데이터 테이블 업데이트 (마지막 Zone에서만 UI 갱신)
                                        viewModel.UpdateDataTableWithDllResult(storedOutput.Value, zoneNumber, dataTableManager, suppressNotification: !isLastZone);
                                        
                                        // 그래프 데이터 포인트 추가 (원래 방식대로!)
                                        var judgment = viewModel.GetJudgmentForZone(zoneNumber);
                                        if (!string.IsNullOrEmpty(judgment))
                                        {
                                            viewModel.AddGraphDataPoint(zoneNumber, judgment, graphManager);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        ErrorLogger.LogException(ex, $"Zone {zoneNumber} 데이터 업데이트 오류", zoneNumber);
                                    }
                                }
                            }
                            
                            // 모든 Zone 처리 완료 후 그래프 업데이트 (원래 방식대로!)
                            var graphDataPoints = graphManager?.GetDataPoints();
                            if (graphDataPoints != null && graphDataPoints.Count > 0)
                            {
                                updateGraphDisplay?.Invoke(graphDataPoints);
                            }
                            
                            // 테이블 재생성
                            if (dataTableManager != null)
                            {
                                dataTableManager.CreateCustomTable();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorLogger.LogException(ex, "Zone 데이터 표시 중 오류");
                    }
                }, System.Windows.Threading.DispatcherPriority.Normal); // Background → Normal (더 빠른 UI 반응)
                
                //25.10.31 - 로그 생성은 UI 업데이트 후 백그라운드에서 (UI 블록 제거!)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await CreateAllResultLogs(zones);
                        ErrorLogger.Log("=== OPTIC 테스트 완료 ===", ErrorLogger.LogLevel.INFO);
                    }
                    catch (Exception ex)
                    {
                        ErrorLogger.LogException(ex, "결과 로그 생성 중 오류");
                    }
                });
            }
            finally
            {
                //25.10.30 - 테스트 완료 시 플래그 초기화 (다음 테스트 실행 가능하도록)
                isTestStarted = false;
                System.Diagnostics.Debug.WriteLine("[OpticSeqExecutor] 테스트 종료 - 플래그 초기화");
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
        
        /// <summary>
        /// Zone별 SEQ 비동기 실행
        /// </summary>
        private async Task ExecuteSeqForZoneAsync(int zoneId, List<string> orderedSeq)
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
                
                //25.10.31 - Zone 완료 시 CIM 로그 즉시 생성 (대기 없이 병렬 실행!)
                try
                {
                    var (cellId, innerId) = GlobalDataManager.GetZoneInfo(zoneId);
                    DateTime startTime = SeqExecutionManager.GetZoneSeqStartTime(zoneId);
                    DateTime endTime = SeqExecutionManager.GetZoneSeqEndTime(zoneId);
                    Output? storedOutput = SeqExecutionManager.GetStoredZoneResult(zoneId);
                    
                    if (storedOutput.HasValue)
                    {
                        //25.10.31 - await 제거 (fire-and-forget) → Zone 완료 즉시 반환, 로그는 백그라운드에서!
                        _ = Task.Run(() => ResultLogManager.CreateCIMForZone(
                            startTime,
                            endTime,
                            cellId,
                            innerId,
                            zoneId,
                            storedOutput.Value
                        ));
                    }
                }
                catch (Exception ex)
                {
                    ErrorLogger.LogException(ex, "Zone CIM 로그 생성 중 오류", zoneId);
                }
            }
        }

        //25.10.29 - Zone SEQ 실행 메서드 리팩토링 (새로운 API 사용)
        /// <summary>
        /// Zone별 SEQ 실행 (최신 버전 - View와 동일)
        /// </summary>
        private async Task ExecuteSeqForZone(int zoneId, List<string> orderedSeq)
        {
            ErrorLogger.Log($"Zone SEQ 실행 시작", ErrorLogger.LogLevel.INFO, zoneId);
            
            //25.10.29 - Zone SEQ 시작 - 컨텍스트 생성 및 Input 설정
            var (cellId, innerId) = GlobalDataManager.GetZoneInfo(zoneId);
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
    }
}




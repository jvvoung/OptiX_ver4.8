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

        /// <summary>
        /// 테스트 시작 (UI 스레드에서 호출)
        /// </summary>
        public void StartTest()
        {
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
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("DLL이 초기화되지 않았습니다. 메인 설정창에서 DLL 경로를 확인해주세요.",
                                      "오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    });
                    return;
                }

                // SEQ 시작/종료 시간 및 Zone별 시간 초기화
                SeqExecutionManager.ResetSeqStartTime();

                // Zone 개수 읽기 (올바른 INI 섹션/키 사용)
                int zoneCount = int.Parse(GlobalDataManager.GetValue("Settings", "MTP_ZONE", "2"));
                ErrorLogger.Log($"Zone 개수: {zoneCount}", ErrorLogger.LogLevel.INFO);

                // SequenceCacheManager에서 캐싱된 시퀀스 가져오기
                var cachedSeq = SequenceCacheManager.Instance.GetOpticSequenceCopy();
                if (cachedSeq == null || cachedSeq.Count == 0)
                {
                    ErrorLogger.Log("시퀀스가 로드되지 않음", ErrorLogger.LogLevel.WARNING);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("시퀀스가 로드되지 않았습니다. Sequence_Optic.ini 파일을 확인해주세요.",
                                      "오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    });
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

                // 결과 로그 생성
                int[] zones = Enumerable.Range(1, zoneCount).ToArray();
                await CreateAllResultLogs(zones);

                ErrorLogger.Log("=== OPTIC 테스트 완료 ===", ErrorLogger.LogLevel.INFO);

                // 모든 존 완료 후 테이블 렌더링 (UI 스레드에서 - 원본 코드 그대로)
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // 각 Zone의 저장된 DLL 결과를 ViewModel에 전달하여 UI 업데이트
                    if (viewModel != null && dataTableManager != null)
                    {
                        foreach (int zoneNumber in zones)
                        {
                            var storedOutput = SeqExecutionManager.GetStoredZoneResult(zoneNumber);
                            if (storedOutput.HasValue)
                            {
                                viewModel.UpdateDataTableWithDllResult(storedOutput.Value, zoneNumber, dataTableManager);
                                
                                // 그래프 데이터 포인트 추가
                                var judgment = viewModel.GetJudgmentForZone(zoneNumber);
                                if (!string.IsNullOrEmpty(judgment))
                                {
                                    viewModel.AddGraphDataPoint(zoneNumber, judgment, graphManager);
                                }
                            }
                        }
                    }
                    
                    // 모든 Zone 처리 완료 후 그래프 업데이트
                    var graphDataPoints = graphManager?.GetDataPoints();
                    if (graphDataPoints != null && graphDataPoints.Count > 0)
                    {
                        updateGraphDisplay?.Invoke(graphDataPoints);
                    }
                    
                    // 테이블 재생성만 수행 (UpdateDataForWad 호출하지 않음 - 이미 viewModel.UpdateDataTableWithDllResult로 업데이트됨)
                    if (dataTableManager != null)
                    {
                        dataTableManager.CreateCustomTable();
                    }
                });
            }
            catch (Exception ex)
            {
                ErrorLogger.LogException(ex, "OPTIC 테스트 실행 중 오류");
            }
        }

        /// <summary>
        /// Zone별 SEQ 비동기 실행
        /// </summary>
        /// <summary>
        /// Zone별 SEQ 실행 Wrapper (최신 버전 - View와 동일)
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
                // Zone SEQ 종료 시간 설정 (예외 발생 여부와 관계없이 항상 실행)
                SeqExecutionManager.SetZoneSeqEndTime(zoneId, DateTime.Now);
            }
        }

        /// <summary>
        /// Zone별 SEQ 실행 (최신 버전 - View와 동일)
        /// </summary>
        private async Task ExecuteSeqForZone(int zoneId, List<string> orderedSeq)
        {
            ErrorLogger.Log($"Zone SEQ 실행 시작", ErrorLogger.LogLevel.INFO, zoneId);
            
            // 시퀀스를 Queue로 변환 (POP 방식으로 진행)
            var sequenceQueue = new Queue<string>(orderedSeq);
            bool isFirstCommand = true; // 첫 번째 명령어(SEQ00) 감지용

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
                
                // SEQ00(첫 번째 명령어) 시작 시 Zone별 SEQ 시작 시간 설정
                if (isFirstCommand)
                {
                    SeqExecutionManager.SetZoneSeqStartTime(zoneId, DateTime.Now);
                    ErrorLogger.Log($"SEQ00 시작: {fnName}, 시간: {DateTime.Now:HH:mm:ss.fff}", ErrorLogger.LogLevel.DEBUG, zoneId);
                    isFirstCommand = false;
                }
                
                // 함수 진입 즉시 로그 (별도 스레드에서 처리 - UI 지연 없음)
                Task.Run(() =>
                {
                    MonitorLogService.Instance.Log(zoneId - 1, $"ENTER {fnName}{(arg.HasValue ? "(" + arg.Value + ")" : string.Empty)}");
                });

                // DELAY 처리: 밀리초 지연 (비동기)
                if (string.Equals(fnName, "DELAY", StringComparison.OrdinalIgnoreCase))
                {
                    int delayMs = arg ?? 0;
                    if (delayMs > 0)
                    {
                        Task.Run(() =>
                        {
                            MonitorLogService.Instance.Log(zoneId - 1, $"DELAY start {delayMs}ms");
                        });
                        
                        await Task.Delay(delayMs);  // 비동기 지연으로 UI 스레드 블록 방지
                        
                        Task.Run(() =>
                        {
                            MonitorLogService.Instance.Log(zoneId - 1, "DELAY end");
                        });
                    }
                    continue; // 다음 SEQ 항목으로
                }

                // 모든 함수를 ExecuteMapped로 통일 처리 (비동기로 UI 스레드 블록 방지)
                bool ok = await Task.Run(() => SeqExecutionManager.ExecuteMapped(fnName, arg, zoneId));

                // 실행 결과 로그 (별도 스레드에서 처리 - UI 지연 없음)
                Task.Run(() =>
                {
                    MonitorLogService.Instance.Log(zoneId - 1, $"Execute {fnName}({(arg.HasValue ? arg.Value.ToString() : "-")}) => {(ok ? "OK" : "FAIL")}");
                });
                
                if (!ok)
                {
                    ErrorLogger.Log($"{fnName} 실행 실패", ErrorLogger.LogLevel.WARNING, zoneId);
                    Task.Run(() =>
                    {
                        MonitorLogService.Instance.Log(zoneId - 1, $"{fnName} failed");
                    });
                    // 실패 정책: 일단 계속 진행
                }
            }
        }

        /// <summary>
        /// 모든 Zone의 결과 로그 생성
        /// </summary>
        private async Task CreateAllResultLogs(int[] zones)
        {
            try
            {
                ErrorLogger.Log($"결과 로그 생성 시작 - {zones.Length}개 Zone", ErrorLogger.LogLevel.INFO);

                foreach (int zoneNumber in zones)
                {
                    try
                    {
                        var (cellId, innerId) = GlobalDataManager.GetZoneInfo(zoneNumber);
                        
                        DateTime startTime = SeqExecutionManager.GetZoneSeqStartTime(zoneNumber);
                        DateTime endTime = SeqExecutionManager.GetZoneSeqEndTime(zoneNumber);

                        Output? storedOutput = SeqExecutionManager.GetStoredZoneResult(zoneNumber);
                        if (storedOutput.HasValue)
                        {
                            bool logResult = ResultLogManager.CreateResultLogsForZone(
                                startTime,
                                endTime,
                                cellId,
                                innerId,
                                zoneNumber,
                                storedOutput.Value
                            );

                            ErrorLogger.Log($"로그 생성 결과: {logResult}", ErrorLogger.LogLevel.INFO, zoneNumber);
                        }
                        else
                        {
                            ErrorLogger.Log($"저장된 데이터 없음 - 로그 생성 스킵", ErrorLogger.LogLevel.WARNING, zoneNumber);
                        }

                        await Task.Delay(DLL.DllConstants.LOG_GENERATION_DELAY_MS);
                    }
                    catch (Exception ex)
                    {
                        ErrorLogger.LogException(ex, "로그 생성 중 오류", zoneNumber);
                    }
                }

                ErrorLogger.Log("결과 로그 생성 완료", ErrorLogger.LogLevel.INFO);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogException(ex, "결과 로그 생성 전체 오류");
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



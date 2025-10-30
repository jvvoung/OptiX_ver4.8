using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OptiX.Common
{
    //25.10.30 - MonitorLogService 비동기 파일 I/O 개선
    /// <summary>
    /// 존별 실시간 로그를 발행/구독하는 싱글턴 서비스 (비동기 개선 버전)
    /// </summary>
    public sealed class MonitorLogService
    {
        private static readonly Lazy<MonitorLogService> lazy = new Lazy<MonitorLogService>(() => new MonitorLogService());
        public static MonitorLogService Instance => lazy.Value;

        // 최근 로그 캐싱 (윈도우가 늦게 열려도 직전 로그 몇 줄 재생)
        private readonly ConcurrentQueue<(int zoneIndex, string text)> recentLogs = new ConcurrentQueue<(int, string)>();
        private const int MaxCachedLines = 200;

        //25.10.30 - 비동기 파일 쓰기를 위한 큐와 작업자 스레드
        private readonly BlockingCollection<(int zoneIndex, string line)> _logQueue = new BlockingCollection<(int, string)>(1000);
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private Task _writerTask;

        private MonitorLogService() 
        {
            //25.10.30 - 백그라운드 로그 작성 스레드 시작
            _writerTask = Task.Run(() => ProcessLogQueueAsync(), _cts.Token);
        }

        public event Action<int, string> LogReceived;

        //25.10.30 - Log 메서드 비동기 큐 방식으로 변경 (UI 블록 제거)
        public void Log(int zoneIndex, string message)
        {
            string line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
            
            // 메모리 캐시 (빠름)
            recentLogs.Enqueue((zoneIndex, line));
            while (recentLogs.Count > MaxCachedLines && recentLogs.TryDequeue(out _)) { }
            
            // UI 이벤트 발행 (빠름)
            LogReceived?.Invoke(zoneIndex, line);

            //25.10.30 - 파일 쓰기는 큐에 추가만 (즉시 반환, UI 블록 없음!)
            try
            {
                _logQueue.TryAdd((zoneIndex, line), 0);  // Timeout 0 = 큐 가득차면 무시
            }
            catch { /* 큐 문제가 있어도 UI는 계속 */ }
        }

        public (int zoneIndex, string text)[] GetRecentLogs()
        {
            return recentLogs.ToArray();
        }

        private string GetLogFilePath(int zoneIndex)
        {
            var now = DateTime.Now;
            string date = now.ToString("yyMMdd");
            string hour = now.ToString("HH");
            // 예: D:\Project\Log\TraceLog\Monitor\Seq_251007_10_1zone.txt
            string fileName = $"Seq_{date}_{hour}_{zoneIndex + 1}zone.txt";
            return Path.Combine(@"D:\Project\Log\TraceLog\Monitor", fileName);
        }

        //25.10.30 - 백그라운드 로그 큐 처리 메서드 추가
        /// <summary>
        /// 백그라운드에서 로그 큐를 처리하여 파일에 비동기로 기록
        /// </summary>
        private async Task ProcessLogQueueAsync()
        {
            // Zone별 버퍼 (Zone 인덱스 -> StringBuilder)
            var buffers = new System.Collections.Generic.Dictionary<int, StringBuilder>();
            var flushTimer = DateTime.Now;

            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    // 큐에서 로그 가져오기 (100ms 타임아웃)
                    if (_logQueue.TryTake(out var logItem, 100, _cts.Token))
                    {
                        int zoneIndex = logItem.zoneIndex;
                        
                        // Zone별 버퍼에 누적
                        if (!buffers.ContainsKey(zoneIndex))
                        {
                            buffers[zoneIndex] = new StringBuilder();
                        }
                        buffers[zoneIndex].AppendLine(logItem.line);
                    }

                    // 500ms마다 또는 버퍼 크기가 1KB 이상이면 플러시
                    bool shouldFlush = (DateTime.Now - flushTimer).TotalMilliseconds > 500;
                    
                    if (shouldFlush || buffers.Values.Any(b => b.Length > 1000))
                    {
                        await FlushAllBuffersAsync(buffers);
                        flushTimer = DateTime.Now;
                    }
                }
                catch (OperationCanceledException)
                {
                    // 정상 종료
                    break;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MonitorLogService] 로그 처리 오류: {ex.Message}");
                }
            }

            // 종료 시 남은 로그 모두 플러시
            await FlushAllBuffersAsync(buffers);
        }

        //25.10.30 - Zone별 버퍼를 파일에 비동기 쓰기
        /// <summary>
        /// 모든 버퍼를 파일에 비동기로 플러시
        /// </summary>
        private async Task FlushAllBuffersAsync(System.Collections.Generic.Dictionary<int, StringBuilder> buffers)
        {
            foreach (var kvp in buffers.ToArray())
            {
                if (kvp.Value.Length > 0)
                {
                    try
                    {
                        string filePath = GetLogFilePath(kvp.Key);
                        string directory = Path.GetDirectoryName(filePath);
                        
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }
                        
                        //25.10.30 - .NET Framework 호환 비동기 파일 쓰기
                        await Task.Run(() => File.AppendAllText(filePath, kvp.Value.ToString()));
                        
                        kvp.Value.Clear();
                    }
                    catch { /* 파일 문제가 있어도 계속 */ }
                }
            }
        }

        //25.10.30 - 종료 메서드 추가 (App 종료 시 호출)
        /// <summary>
        /// MonitorLogService 종료 (앱 종료 시 호출)
        /// </summary>
        public void Dispose()
        {
            try
            {
                _cts.Cancel();
                _logQueue.CompleteAdding();
                _writerTask?.Wait(1000);  // 최대 1초 대기
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MonitorLogService] 종료 중 오류: {ex.Message}");
            }
        }
    }
}



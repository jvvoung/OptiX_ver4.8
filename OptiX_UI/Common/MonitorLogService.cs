using System;
using System.Collections.Concurrent;
using System.IO;

namespace OptiX.Common
{
    /// <summary>
    /// 존별 실시간 로그를 발행/구독하는 싱글턴 서비스
    /// </summary>
    public sealed class MonitorLogService
    {
        private static readonly Lazy<MonitorLogService> lazy = new Lazy<MonitorLogService>(() => new MonitorLogService());
        public static MonitorLogService Instance => lazy.Value;

        // 최근 로그 캐싱 (윈도우가 늦게 열려도 직전 로그 몇 줄 재생)
        private readonly ConcurrentQueue<(int zoneIndex, string text)> recentLogs = new ConcurrentQueue<(int, string)>();
        private const int MaxCachedLines = 200;

        private MonitorLogService() { }

        public event Action<int, string> LogReceived;

        public void Log(int zoneIndex, string message)
        {
            string line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
            recentLogs.Enqueue((zoneIndex, line));
            while (recentLogs.Count > MaxCachedLines && recentLogs.TryDequeue(out _)) { }
            LogReceived?.Invoke(zoneIndex, line);

            // 파일 로그도 남김 (시간별/존별 파일)
            try
            {
                string filePath = GetLogFilePath(zoneIndex);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                // 파일에는 존 접두사 없이 시간/메시지만 기록
                File.AppendAllText(filePath, $"{line}{Environment.NewLine}");
            }
            catch { /* 파일 문제가 있어도 UI 로그는 계속 */ }
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
    }
}



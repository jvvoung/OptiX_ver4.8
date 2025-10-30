using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OptiX.Common
{
    //25.10.30 - ErrorLogger 비동기 + Zone별 파일 분리 개선
    /// <summary>
    /// Thread-safe 오류 및 예외 로깅 클래스 (비동기 개선 버전)
    /// Zone별 파일 분리 + 비동기 큐 방식으로 UI 블록 없음
    /// 로그 위치: D:\Project\Log\TraceLog\Error
    /// </summary>
    public static class ErrorLogger
    {
        //25.10.30 - 비동기 큐 방식으로 변경
        private static readonly BlockingCollection<LogEntry> _logQueue = new BlockingCollection<LogEntry>(1000);
        private static readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private static Task _writerTask;
        private static string _logDirectory = @"D:\Project\Log\TraceLog\Error";
        
        //25.10.30 - 로그 엔트리 구조체
        private struct LogEntry
        {
            public DateTime Timestamp;
            public LogLevel Level;
            public int? ZoneNumber;
            public string Message;
            public Exception Exception;
            public bool IsException;
        }

        /// <summary>
        /// 로그 레벨 정의
        /// </summary>
        public enum LogLevel
        {
            DEBUG,    // 디버그 정보
            INFO,     // 일반 정보
            WARNING,  // 경고
            ERROR,    // 오류
            CRITICAL  // 치명적 오류
        }

        //25.10.30 - 초기화 메서드 개선 (백그라운드 작업 스레드 시작)
        /// <summary>
        /// ErrorLogger 초기화 (앱 시작 시 호출)
        /// </summary>
        public static void Initialize()
        {
            try
            {
                // 로그 디렉토리 생성
                if (!Directory.Exists(_logDirectory))
                {
                    Directory.CreateDirectory(_logDirectory);
                }

                //25.10.30 - 백그라운드 로그 작성 스레드 시작
                if (_writerTask == null || _writerTask.IsCompleted)
                {
                    _writerTask = Task.Run(() => ProcessLogQueueAsync(), _cts.Token);
                }

                // 초기화 로그 기록
                Log("ErrorLogger 초기화 완료 (비동기 모드)", LogLevel.INFO);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ErrorLogger 초기화 실패: {ex.Message}");
            }
        }

        //25.10.30 - Zone별 로그 파일 경로 가져오기 (파일 분리)
        /// <summary>
        /// Zone별 로그 파일 경로 가져오기
        /// 로그 파일명: Error_YYYYMMDD_HH_ZoneN.log 또는 Error_YYYYMMDD_HH_System.log
        /// </summary>
        private static string GetLogPath(int? zoneNumber, DateTime timestamp)
        {
            string fileName = zoneNumber.HasValue
                ? $"Error_{timestamp:yyyyMMdd_HH}_Zone{zoneNumber}.log"
                : $"Error_{timestamp:yyyyMMdd_HH}_System.log";
            return Path.Combine(_logDirectory, fileName);
        }

        //25.10.30 - Log 메서드 비동기 큐 방식으로 변경 (UI 블록 제거)
        /// <summary>
        /// 일반 로그 기록 (Zone 정보 포함) - 비동기 큐 방식
        /// </summary>
        /// <param name="message">로그 메시지</param>
        /// <param name="level">로그 레벨</param>
        /// <param name="zoneNumber">Zone 번호 (선택)</param>
        public static void Log(string message, LogLevel level = LogLevel.INFO, int? zoneNumber = null)
        {
            try
            {
                //25.10.30 - 로그 엔트리를 큐에 추가만 (즉시 반환, UI 블록 없음)
                var entry = new LogEntry
                {
                    Timestamp = DateTime.Now,
                    Level = level,
                    ZoneNumber = zoneNumber,
                    Message = message,
                    IsException = false
                };
                
                _logQueue.TryAdd(entry, 0);  // Timeout 0 = 큐 가득차면 무시
                
                // 디버그 출력 (개발 중 확인용)
                string zoneTag = zoneNumber.HasValue ? $"[Zone-{zoneNumber}]" : "[System]";
                Debug.WriteLine($"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {zoneTag} {message}");
            }
            catch (Exception ex)
            {
                // 큐 추가 실패해도 앱은 계속 실행
                Debug.WriteLine($"[ErrorLogger] 로그 큐 추가 실패: {ex.Message}");
            }
        }

        //25.10.30 - LogException 메서드 비동기 큐 방식으로 변경
        /// <summary>
        /// 예외 로그 기록 (자동으로 ERROR 레벨, 상세 스택 추적 포함) - 비동기 큐 방식
        /// </summary>
        /// <param name="ex">예외 객체</param>
        /// <param name="contextMessage">발생 컨텍스트 설명</param>
        /// <param name="zoneNumber">Zone 번호 (선택)</param>
        public static void LogException(Exception ex, string contextMessage = "", int? zoneNumber = null)
        {
            try
            {
                //25.10.30 - 예외 로그 엔트리를 큐에 추가만 (즉시 반환, UI 블록 없음)
                var entry = new LogEntry
                {
                    Timestamp = DateTime.Now,
                    Level = LogLevel.ERROR,
                    ZoneNumber = zoneNumber,
                    Message = contextMessage,
                    Exception = ex,
                    IsException = true
                };
                
                _logQueue.TryAdd(entry, 0);  // Timeout 0 = 큐 가득차면 무시
                
                // 디버그 출력 (간략 버전)
                string zoneTag = zoneNumber.HasValue ? $"[Zone-{zoneNumber}]" : "[System]";
                Debug.WriteLine($"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [ERROR] {zoneTag} Exception: {ex.GetType().Name} - {ex.Message}");
            }
            catch (Exception logEx)
            {
                Debug.WriteLine($"[ErrorLogger] 예외 로그 큐 추가 실패: {logEx.Message}");
            }
        }

        /// <summary>
        /// DLL 호출 관련 오류 로그 (추가 정보 포함)
        /// </summary>
        public static void LogDllError(string functionName, string errorMessage, int? zoneNumber = null, int? returnCode = null)
        {
            string msg = $"DLL 호출 실패 - Function: {functionName}, Error: {errorMessage}";
            if (returnCode.HasValue)
                msg += $", ReturnCode: {returnCode.Value}";
            
            Log(msg, LogLevel.ERROR, zoneNumber);
        }

        /// <summary>
        /// 시퀀스 실행 관련 오류 로그
        /// </summary>
        public static void LogSeqError(string seqName, string errorMessage, int zoneNumber)
        {
            Log($"SEQ 실행 실패 - Seq: {seqName}, Error: {errorMessage}", LogLevel.ERROR, zoneNumber);
        }

        /// <summary>
        /// INI 파일 관련 오류 로그
        /// </summary>
        public static void LogIniError(string section, string key, string errorMessage)
        {
            Log($"INI 읽기 실패 - Section: [{section}], Key: {key}, Error: {errorMessage}", LogLevel.ERROR);
        }

        /// <summary>
        /// 파일 I/O 관련 오류 로그
        /// </summary>
        public static void LogFileError(string filePath, string operation, string errorMessage)
        {
            Log($"파일 작업 실패 - Path: {filePath}, Operation: {operation}, Error: {errorMessage}", LogLevel.ERROR);
        }

        //25.10.30 - 백그라운드 로그 큐 처리 메서드 추가
        /// <summary>
        /// 백그라운드에서 로그 큐를 처리하여 파일에 비동기로 기록
        /// </summary>
        private static async Task ProcessLogQueueAsync()
        {
            // Zone별 버퍼 (Zone 번호 -> StringBuilder)
            var buffers = new System.Collections.Generic.Dictionary<string, StringBuilder>();
            var flushTimer = DateTime.Now;

            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    // 큐에서 로그 가져오기 (100ms 타임아웃)
                    if (_logQueue.TryTake(out var entry, 100, _cts.Token))
                    {
                        // Zone별 버퍼 키 생성
                        string bufferKey = entry.ZoneNumber.HasValue 
                            ? $"Zone{entry.ZoneNumber}" 
                            : "System";

                        // 버퍼가 없으면 생성
                        if (!buffers.ContainsKey(bufferKey))
                        {
                            buffers[bufferKey] = new StringBuilder();
                        }

                        // 로그 포맷팅 및 버퍼에 추가
                        string logLine = FormatLogEntry(entry);
                        buffers[bufferKey].AppendLine(logLine);
                    }

                    // 500ms마다 또는 버퍼 크기가 일정 이상이면 플러시
                    bool shouldFlush = (DateTime.Now - flushTimer).TotalMilliseconds > 500;
                    
                    if (shouldFlush || buffers.Values.Any(b => b.Length > 2000))
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
                    Debug.WriteLine($"[ErrorLogger] 로그 처리 오류: {ex.Message}");
                }
            }

            // 종료 시 남은 로그 모두 플러시
            await FlushAllBuffersAsync(buffers);
        }

        //25.10.30 - 로그 엔트리 포맷팅 메서드 추가
        /// <summary>
        /// 로그 엔트리를 문자열로 포맷팅
        /// </summary>
        private static string FormatLogEntry(LogEntry entry)
        {
            string timestamp = entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string zoneTag = entry.ZoneNumber.HasValue ? $"[Zone-{entry.ZoneNumber}]" : "[System]";
            string levelTag = $"[{entry.Level}]";

            if (entry.IsException && entry.Exception != null)
            {
                // 예외 로그 포맷
                var sb = new StringBuilder();
                sb.AppendLine($"[{timestamp}] [ERROR] {zoneTag} ═══ EXCEPTION ═══");
                
                if (!string.IsNullOrEmpty(entry.Message))
                    sb.AppendLine($"[{timestamp}] [ERROR] {zoneTag} Context: {entry.Message}");
                    
                sb.AppendLine($"[{timestamp}] [ERROR] {zoneTag} Type: {entry.Exception.GetType().Name}");
                sb.AppendLine($"[{timestamp}] [ERROR] {zoneTag} Message: {entry.Exception.Message}");
                sb.AppendLine($"[{timestamp}] [ERROR] {zoneTag} StackTrace:");
                sb.AppendLine(entry.Exception.StackTrace);
                sb.AppendLine($"[{timestamp}] [ERROR] {zoneTag} ═══════════════════");
                
                return sb.ToString();
            }
            else
            {
                // 일반 로그 포맷
                return $"[{timestamp}] {levelTag} {zoneTag} {entry.Message}";
            }
        }

        //25.10.30 - Zone별 버퍼를 파일에 비동기 쓰기
        /// <summary>
        /// 모든 버퍼를 파일에 비동기로 플러시
        /// </summary>
        private static async Task FlushAllBuffersAsync(System.Collections.Generic.Dictionary<string, StringBuilder> buffers)
        {
            foreach (var kvp in buffers.ToArray())
            {
                if (kvp.Value.Length > 0)
                {
                    try
                    {
                        // Zone 번호 추출
                        int? zoneNumber = kvp.Key.StartsWith("Zone") 
                            ? int.Parse(kvp.Key.Substring(4)) 
                            : (int?)null;

                        // Zone별 파일 경로
                        string filePath = GetLogPath(zoneNumber, DateTime.Now);
                        
                        // 디렉토리 생성
                        string directory = Path.GetDirectoryName(filePath);
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }
                        
                        //25.10.30 - .NET Framework 호환 비동기 파일 쓰기
                        await Task.Run(() => File.AppendAllText(filePath, kvp.Value.ToString()));
                        
                        kvp.Value.Clear();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ErrorLogger] 파일 쓰기 실패 ({kvp.Key}): {ex.Message}");
                    }
                }
            }
        }

        //25.10.30 - 종료 메서드 추가 (App 종료 시 호출)
        /// <summary>
        /// ErrorLogger 종료 (앱 종료 시 호출)
        /// </summary>
        public static void Dispose()
        {
            try
            {
                _cts.Cancel();
                _logQueue.CompleteAdding();
                _writerTask?.Wait(2000);  // 최대 2초 대기
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ErrorLogger] 종료 중 오류: {ex.Message}");
            }
        }
    }
}


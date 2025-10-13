using System;
using System.Diagnostics;
using System.IO;

namespace OptiX.Common
{
    /// <summary>
    /// Thread-safe 오류 및 예외 로깅 클래스
    /// 멀티스레드 환경에서 안전하게 단일 로그 파일에 기록
    /// 로그 위치: D:\Project\Log\TraceLog\Error
    /// </summary>
    public static class ErrorLogger
    {
        private static readonly object _logLock = new object();
        private static string _logDirectory = @"D:\Project\Log\TraceLog\Error";

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

                // 초기화 로그 기록
                Log("ErrorLogger 초기화 완료", LogLevel.INFO);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ErrorLogger 초기화 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 현재 시간에 맞는 로그 파일 경로 가져오기
        /// 로그 파일명: Error_YYYYMMDD_HH.log (시간별 분리)
        /// </summary>
        private static string GetCurrentLogPath()
        {
            string fileName = $"Error_{DateTime.Now:yyyyMMdd_HH}.log";
            return Path.Combine(_logDirectory, fileName);
        }

        /// <summary>
        /// 일반 로그 기록 (Zone 정보 포함)
        /// </summary>
        /// <param name="message">로그 메시지</param>
        /// <param name="level">로그 레벨</param>
        /// <param name="zoneNumber">Zone 번호 (선택)</param>
        public static void Log(string message, LogLevel level = LogLevel.INFO, int? zoneNumber = null)
        {
            try
            {
                lock (_logLock)
                {
                    // 로그 디렉토리 확인 (최초 실행 시)
                    if (!Directory.Exists(_logDirectory))
                    {
                        Directory.CreateDirectory(_logDirectory);
                    }

                    // 현재 시간에 맞는 로그 파일 경로 가져오기 (시간별 분리)
                    string logPath = GetCurrentLogPath();

                    // 로그 라인 생성
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    string zoneTag = zoneNumber.HasValue ? $"[Zone-{zoneNumber}]" : "[System]";
                    string levelTag = $"[{level}]";

                    string logLine = $"[{timestamp}] {levelTag} {zoneTag} {message}";

                    // 파일에 기록 (시간별 파일에 추가)
                    File.AppendAllText(logPath, logLine + Environment.NewLine);

                    // 디버그 출력 (개발 중 확인용)
                    Debug.WriteLine(logLine);
                }
            }
            catch (Exception ex)
            {
                // 로그 기록 실패해도 앱은 계속 실행
                Debug.WriteLine($"[ErrorLogger] 로그 쓰기 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 예외 로그 기록 (자동으로 ERROR 레벨, 상세 스택 추적 포함)
        /// </summary>
        /// <param name="ex">예외 객체</param>
        /// <param name="contextMessage">발생 컨텍스트 설명</param>
        /// <param name="zoneNumber">Zone 번호 (선택)</param>
        public static void LogException(Exception ex, string contextMessage = "", int? zoneNumber = null)
        {
            try
            {
                lock (_logLock)
                {
                    // 로그 디렉토리 확인 (최초 실행 시)
                    if (!Directory.Exists(_logDirectory))
                    {
                        Directory.CreateDirectory(_logDirectory);
                    }

                    // 현재 시간에 맞는 로그 파일 경로 가져오기 (시간별 분리)
                    string logPath = GetCurrentLogPath();

                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    string zoneTag = zoneNumber.HasValue ? $"[Zone-{zoneNumber}]" : "[System]";

                    // 예외 정보 구성
                    string header = $"[{timestamp}] [ERROR] {zoneTag} ═══ EXCEPTION ═══";
                    string context = string.IsNullOrEmpty(contextMessage) 
                        ? "" 
                        : $"[{timestamp}] [ERROR] {zoneTag} Context: {contextMessage}";
                    string exType = $"[{timestamp}] [ERROR] {zoneTag} Type: {ex.GetType().Name}";
                    string exMsg = $"[{timestamp}] [ERROR] {zoneTag} Message: {ex.Message}";
                    string exStack = $"[{timestamp}] [ERROR] {zoneTag} StackTrace:\n{ex.StackTrace}";
                    string footer = $"[{timestamp}] [ERROR] {zoneTag} ═══════════════════";

                    // 로그 조합
                    string fullLog = header + Environment.NewLine;
                    if (!string.IsNullOrEmpty(context))
                        fullLog += context + Environment.NewLine;
                    fullLog += exType + Environment.NewLine;
                    fullLog += exMsg + Environment.NewLine;
                    fullLog += exStack + Environment.NewLine;
                    fullLog += footer + Environment.NewLine;

                    // 파일에 기록 (시간별 파일에 추가)
                    File.AppendAllText(logPath, fullLog);

                    // 디버그 출력
                    Debug.WriteLine(fullLog);
                }
            }
            catch (Exception logEx)
            {
                Debug.WriteLine($"[ErrorLogger] 예외 로그 쓰기 실패: {logEx.Message}");
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
    }
}


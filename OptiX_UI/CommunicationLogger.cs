using System;
using System.IO;
using System.Threading.Tasks;

namespace OptiX
{
    /// <summary>
    /// 클라이언트-서버 통신 로그를 관리하는 클래스
    /// </summary>
    public class CommunicationLogger
    {
        private static readonly string LogDirectory = @"D:\Project\Log\protocol";
        private static readonly object _lock = new object();

        /// <summary>
        /// 로그 디렉토리가 존재하지 않으면 생성
        /// </summary>
        static CommunicationLogger()
        {
            try
            {
                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"로그 디렉토리 생성 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 오늘 날짜의 로그 파일 경로 생성
        /// </summary>
        private static string GetTodayLogFilePath()
        {
            string today = DateTime.Now.ToString("yyyyMMdd");
            return Path.Combine(LogDirectory, $"protocol_{today}.txt");
        }

        /// <summary>
        /// 현재 시간을 로그 형식으로 반환 (연월일 시분초.밀리초)
        /// </summary>
        private static string GetCurrentTimeStamp()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }

        /// <summary>
        /// 로그 메시지를 파일에 기록
        /// </summary>
        private static void WriteLogToFile(string logMessage)
        {
            try
            {
                lock (_lock)
                {
                    string logFilePath = GetTodayLogFilePath();
                    string timeStamp = GetCurrentTimeStamp();
                    string fullLogMessage = $"[{timeStamp}] {logMessage}";

                    // 파일에 로그 추가 (비동기로 처리하여 UI 블로킹 방지)
                    File.AppendAllText(logFilePath, fullLogMessage + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"로그 기록 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 통신 관련 로그 기록 (호출부에서 로그 내용 직접 작성)
        /// </summary>
        /// <param name="logMessage">로그 메시지 (호출부에서 직접 작성)</param>
        public static void WriteLog(string logMessage)
        {
            WriteLogToFile(logMessage);
        }

        /* 
         * 사용 예시:
         * 
         * // 클라이언트-서버 통신 로그
         * CommunicationLogger.WriteLog("🟢 [CLIENT_CONNECT] 클라이언트 연결 성공 - IP: 127.0.0.1:54321");
         * CommunicationLogger.WriteLog("📥 [MESSAGE_RECEIVED] 클라이언트로부터 메시지 수신 - 메시지: \"PING\"");
         * CommunicationLogger.WriteLog("📤 [MESSAGE_SENT] 클라이언트에게 메시지 전송 - 메시지: \"PONG\"");
         * CommunicationLogger.WriteLog("🔴 [CLIENT_DISCONNECT] 클라이언트 연결 해제 - 사유: 연결 종료");
         * 
         * // DLL과 UI 통신 로그 (향후 추가 예정)
         * CommunicationLogger.WriteLog("🔧 [DLL_CALL] DLL 함수 호출 - 함수명: TestFunction");
         * CommunicationLogger.WriteLog("📊 [DLL_RESULT] DLL 함수 결과 - 반환값: SUCCESS, 처리시간: 150ms");
         * CommunicationLogger.WriteLog("⚠️ [DLL_ERROR] DLL 함수 오류 - 오류코드: -1, 오류메시지: \"Invalid Parameter\"");
         * CommunicationLogger.WriteLog("🔄 [UI_UPDATE] UI 업데이트 완료 - 페이지: OpticPage, 상태: 검사완료");
         */
    }
}

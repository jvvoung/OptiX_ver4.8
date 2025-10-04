using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Collections.Generic;
using System.Linq;

namespace OptiX
{
    /// <summary>
    /// OptiX UI와 외부 운영 프로그램 간의 TCP/IP 통신을 담당하는 서버 클래스
    /// </summary>
    public class CommunicationServer
    {
        #region Fields
        private TcpListener tcpListener;
        private CancellationTokenSource cancellationTokenSource;
        private bool isRunning = false;
        private List<TcpClient> connectedClients = new List<TcpClient>();
        private readonly object clientsLock = new object();
        
        // 이벤트 정의
        public event EventHandler<string> MessageReceived;
        public event EventHandler<bool> ConnectionStatusChanged;
        public event EventHandler<string> LogMessage;
        #endregion

        #region Properties
        public bool IsRunning => isRunning;
        public int ConnectedClientCount 
        { 
            get 
            { 
                lock (clientsLock) 
                { 
                    return connectedClients.Count; 
                } 
            } 
        }
        #endregion

        #region Public Methods
        
        /// <summary>
        /// TCP 서버 시작
        /// </summary>
        /// <param name="ipAddress">서버 IP 주소</param>
        /// <param name="port">서버 포트</param>
        public async Task<bool> StartServerAsync(string ipAddress, int port)
        {
            try
            {
                if (isRunning)
                {
                    LogMessage?.Invoke(this, "⚠️ 서버가 이미 실행 중입니다.");
                    return false;
                }

                // IP 주소 파싱
                IPAddress address;
                if (ipAddress == "127.0.0.1" || ipAddress == "localhost")
                {
                    address = IPAddress.Any; // 모든 인터페이스에서 수신
                }
                else if (!IPAddress.TryParse(ipAddress, out address))
                {
                    LogMessage?.Invoke(this, $"❌ 잘못된 IP 주소: {ipAddress}");
                    return false;
                }

                // TCP 리스너 생성
                tcpListener = new TcpListener(address, port);
                
                // 리스너 시작
                tcpListener.Start();

                isRunning = true;
                cancellationTokenSource = new CancellationTokenSource();

                LogMessage?.Invoke(this, $"🚀 TCP 서버 시작됨: {address}:{port}");
                LogMessage?.Invoke(this, $"📡 TCP 리스너 상태: {tcpListener.Server.IsBound}");
                LogMessage?.Invoke(this, $"🔍 로컬 엔드포인트: {tcpListener.LocalEndpoint}");
                LogMessage?.Invoke(this, $"✅ 서버 바인딩 완료!");
                LogMessage?.Invoke(this, $"🔧 서버 소켓 정보: {tcpListener.Server.LocalEndPoint}");
                LogMessage?.Invoke(this, $"🌐 서버가 정상적으로 포트 {port}에서 수신 대기 중입니다!");
                
                // 통신 로그 기록
                CommunicationLogger.WriteLog($"🚀 [SERVER_START] 서버 시작 - IP: {address}, Port: {port}");
                
                ConnectionStatusChanged?.Invoke(this, true);

                // 클라이언트 연결 대기 시작
                _ = Task.Run(() => AcceptClientsAsync(cancellationTokenSource.Token));

                return true;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke(this, $"❌ 서버 시작 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// TCP 서버 중지
        /// </summary>
        public async Task StopServerAsync()
        {
            try
            {
                if (!isRunning)
                {
                    LogMessage?.Invoke(this, "⚠️ 서버가 실행되지 않았습니다.");
                    return;
                }

                // 취소 토큰 신호
                cancellationTokenSource?.Cancel();

                // TCP 리스너 중지
                tcpListener?.Stop();

                // 연결된 모든 클라이언트 종료
                lock (clientsLock)
                {
                    foreach (var client in connectedClients.ToList())
                    {
                        try
                        {
                            client?.Close();
                        }
                        catch { }
                    }
                    connectedClients.Clear();
                }

                isRunning = false;
                LogMessage?.Invoke(this, "🛑 TCP 서버 중지됨");
                
                // 통신 로그 기록
                CommunicationLogger.WriteLog($"🛑 [SERVER_STOP] 서버 중지 - 사유: 사용자 요청");
                
                ConnectionStatusChanged?.Invoke(this, false);

                await Task.Delay(100); // 안전한 종료를 위한 대기
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke(this, $"❌ 서버 중지 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 모든 연결된 클라이언트에게 메시지 전송
        /// </summary>
        /// <param name="message">전송할 메시지</param>
        public async Task SendMessageToAllClientsAsync(string message)
        {
            if (!isRunning)
            {
                LogMessage?.Invoke(this, "⚠️ 서버가 실행되지 않았습니다.");
                return;
            }

            List<TcpClient> clientsToSend;
            lock (clientsLock)
            {
                clientsToSend = connectedClients.Where(c => c?.Connected == true).ToList();
            }

            if (clientsToSend.Count == 0)
            {
                LogMessage?.Invoke(this, "⚠️ 연결된 클라이언트가 없습니다.");
                return;
            }

            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            var tasks = clientsToSend.Select(async client =>
            {
                try
                {
                    var stream = client.GetStream();
                    await stream.WriteAsync(messageBytes, 0, messageBytes.Length);
                    LogMessage?.Invoke(this, $"📤 모든 클라이언트에게 메시지 전송: {message}");
                }
                catch (Exception ex)
                {
                    LogMessage?.Invoke(this, $"❌ 클라이언트에게 메시지 전송 실패: {ex.Message}");
                    // 연결이 끊어진 클라이언트 제거 (연결 상태 업데이트 포함)
                    RemoveClient(client);
                }
            });

            await Task.WhenAll(tasks);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 클라이언트 연결 대기 및 처리
        /// </summary>
        private async Task AcceptClientsAsync(CancellationToken cancellationToken)
        {
            LogMessage?.Invoke(this, "🔄 클라이언트 연결 대기 시작...");
            
            while (!cancellationToken.IsCancellationRequested && isRunning)
            {
                try
                {
                    LogMessage?.Invoke(this, "⏳ 클라이언트 연결 대기 중...");
                    
                    // 클라이언트 연결 대기
                    var tcpClient = await tcpListener.AcceptTcpClientAsync();
                    
                    lock (clientsLock)
                    {
                        connectedClients.Add(tcpClient);
                    }

                    LogMessage?.Invoke(this, $"✅ 클라이언트 연결됨: {tcpClient.Client.RemoteEndPoint}");
                    LogMessage?.Invoke(this, $"📊 연결된 클라이언트 수: {ConnectedClientCount}");
                    
                    // 통신 로그 기록
                    CommunicationLogger.WriteLog($"🟢 [CLIENT_CONNECT] 클라이언트 연결 성공 - IP: {tcpClient.Client.RemoteEndPoint}");

                    // 연결 상태 변경 이벤트 발생 (클라이언트가 연결되었음을 알림)
                    CommunicationLogger.WriteLog($"🔍 [DEBUG] CommunicationServer - 클라이언트 연결 이벤트 발생 전");
                    ConnectionStatusChanged?.Invoke(this, true);
                    LogMessage?.Invoke(this, "🟢 클라이언트 연결됨 - AUTO MODE 활성화");
                    CommunicationLogger.WriteLog($"🟢 [CONNECTION_STATUS] 클라이언트 연결 - AUTO MODE 활성화");

                    // 클라이언트별 메시지 처리 시작
                    _ = Task.Run(() => HandleClientAsync(tcpClient, cancellationToken));
                }
                catch (ObjectDisposedException)
                {
                    // 서버가 중지된 경우
                    break;
                }
                catch (Exception ex)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        LogMessage?.Invoke(this, $"❌ 클라이언트 연결 대기 중 오류: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 개별 클라이언트 메시지 처리
        /// </summary>
        private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            NetworkStream stream = null;
            try
            {
                stream = client.GetStream();
                byte[] buffer = new byte[4096];

                while (!cancellationToken.IsCancellationRequested && client.Connected)
                {
                    // 메시지 수신 대기
                    LogMessage?.Invoke(this, $"🔍 메시지 수신 대기 중... (클라이언트: {client.Client.RemoteEndPoint}, 연결상태: {client.Connected})");
                    
                    // 수신 타임아웃 설정 (5초)
                    stream.ReadTimeout = 5000;
                    
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    LogMessage?.Invoke(this, $"📊 수신된 바이트 수: {bytesRead}, 클라이언트 연결상태: {client.Connected}");
                    
                    if (bytesRead == 0)
                    {
                        // 클라이언트가 연결을 끊은 경우
                        break;
                    }

                    // 메시지 파싱
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    LogMessage?.Invoke(this, $"📥 클라이언트로부터 메시지 수신: {message}");

                    // 통신 로그 기록 - 메시지 수신 (모든 메시지 기록)
                    CommunicationLogger.WriteLog($"📥 [MESSAGE_RECEIVED] 수신메시지: \"{message}\" | 길이: {bytesRead}");

                    // 명령 처리
                    string response = ProcessCommand(message);
                    
                    // 응답 전송
                    if (!string.IsNullOrEmpty(response))
                    {
                        byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                        await stream.WriteAsync(responseBytes, 0, responseBytes.Length, cancellationToken);
                        LogMessage?.Invoke(this, $"📤 클라이언트에게 응답 전송: {response}");
                        
                    // 통신 로그 기록 - 메시지 전송
                    CommunicationLogger.WriteLog($"📤 [MESSAGE_SENT] 전송메시지: \"{response}\"");
                    }

                    // 메시지 수신 이벤트 발생
                    MessageReceived?.Invoke(this, message);
                }
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    LogMessage?.Invoke(this, $"❌ 클라이언트 처리 중 오류: {ex.Message}");
                }
            }
            finally
            {
                // 클라이언트 연결 정리
                RemoveClient(client);
                stream?.Close();
                client?.Close();
            }
        }

        /// <summary>
        /// 클라이언트 목록에서 제거
        /// </summary>
        private void RemoveClient(TcpClient client)
        {
            lock (clientsLock)
            {
                connectedClients.Remove(client);
            }
            
            LogMessage?.Invoke(this, $"🔌 클라이언트 연결 해제: {client.Client.RemoteEndPoint}");
            LogMessage?.Invoke(this, $"📊 연결된 클라이언트 수: {ConnectedClientCount}");
            
            // 통신 로그 기록
            CommunicationLogger.WriteLog($"🔴 [CLIENT_DISCONNECT] 클라이언트 연결 해제 - IP: {client.Client.RemoteEndPoint} - 사유: 연결 종료");
            
            // 연결 상태 변경 이벤트 발생 (클라이언트가 연결 해제되었음을 알림)
            CommunicationLogger.WriteLog($"🔍 [DEBUG] CommunicationServer - 클라이언트 연결 해제 이벤트 발생 전");
            ConnectionStatusChanged?.Invoke(this, false);
            LogMessage?.Invoke(this, "🔴 클라이언트 연결 해제됨 - AUTO MODE 해제");
            CommunicationLogger.WriteLog($"🔴 [CONNECTION_STATUS] 클라이언트 연결 해제 - AUTO MODE 해제");
        }

        /// <summary>
        /// 클라이언트 명령 처리
        /// </summary>
        private string ProcessCommand(string command)
        {
            try
            {
                string cmd = command.Trim().ToUpper();
                
                // 명령 처리 시작 로그
                CommunicationLogger.WriteLog($"🔍 [COMMAND_PROCESS] 명령 처리 시작 - 원본: \"{command}\" | 처리용: \"{cmd}\"");

                switch (cmd)
                {
                    case "PING":
                        CommunicationLogger.WriteLog($"✅ [COMMAND_SUCCESS] PING 명령 처리 완료");
                        return "PONG";

                    case "TEST_START":
                        // TODO: 검사 시작 로직 구현
                        CommunicationLogger.WriteLog($"✅ [COMMAND_SUCCESS] TEST_START 명령 처리 완료");
                        return "TEST_START_OK";

                    case "TEST_STOP":
                        // TODO: 검사 중지 로직 구현
                        CommunicationLogger.WriteLog($"✅ [COMMAND_SUCCESS] TEST_STOP 명령 처리 완료");
                        return "TEST_STOP_OK";

                    case "GET_STATUS":
                        // TODO: 상태 조회 로직 구현
                        CommunicationLogger.WriteLog($"✅ [COMMAND_SUCCESS] GET_STATUS 명령 처리 완료");
                        return "STATUS_RUNNING";

                    default:
                        LogMessage?.Invoke(this, $"⚠️ 알 수 없는 명령: {command}");
                        
                        // 통신 로그 기록 - 알 수 없는 명령
                        CommunicationLogger.WriteLog($"⚠️ [UNKNOWN_COMMAND] 수신메시지: \"{command}\" - 정의되지 않은 명령입니다");
                        
                        return "UNKNOWN_COMMAND";
                }
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke(this, $"❌ 명령 처리 중 오류: {ex.Message}");
                
                // 통신 로그 기록 - 명령 처리 오류
                CommunicationLogger.WriteLog($"❌ [COMMAND_ERROR] 명령 처리 중 오류 발생 - 명령: \"{command}\" - 오류: {ex.Message}");
                
                return "ERROR";
            }
        }

        #endregion

        #region IDisposable Implementation
        
        public void Dispose()
        {
            try
            {
                StopServerAsync().Wait(5000); // 5초 대기
            }
            catch { }
            
            cancellationTokenSource?.Dispose();
            tcpListener = null;
        }
        
        #endregion
    }
}

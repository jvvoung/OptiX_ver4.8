using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace OptiXClient
{
    class SimpleServerTest
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🧪 간단한 서버 테스트 (포트 7777)");
            Console.WriteLine("================================");

            try
            {
                // TCP 리스너 생성 (포트 7777)
                TcpListener listener = new TcpListener(IPAddress.Any, 7777);
                listener.Start();

                Console.WriteLine($"🚀 서버 시작: 0.0.0.0:7777");
                Console.WriteLine($"📡 서버 바인딩 상태: {listener.Server.IsBound}");
                Console.WriteLine($"🔍 로컬 엔드포인트: {listener.LocalEndpoint}");
                Console.WriteLine($"🔧 서버 소켓 정보: {listener.Server.LocalEndPoint}");
                Console.WriteLine("🌐 서버가 정상적으로 포트 7777에서 수신 대기 중입니다!");

                while (true)
                {
                    Console.WriteLine("⏳ 클라이언트 연결 대기 중...");
                    
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    Console.WriteLine($"✅ 클라이언트 연결됨: {client.Client.RemoteEndPoint}");

                    // 클라이언트 처리
                    _ = Task.Run(() => HandleClient(client));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 서버 오류: {ex.Message}");
                Console.WriteLine($"❌ 스택 트레이스: {ex.StackTrace}");
            }
        }

        static async Task HandleClient(TcpClient client)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[4096];

                while (client.Connected)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"📥 받은 메시지: {message}");

                    // 응답 전송
                    string response = "PONG";
                    byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                    await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                    Console.WriteLine($"📤 응답 전송: {response}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 클라이언트 처리 오류: {ex.Message}");
            }
            finally
            {
                client.Close();
                Console.WriteLine("🔌 클라이언트 연결 종료");
            }
        }
    }
}

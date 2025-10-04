using System;
using System.Threading.Tasks;

namespace OptiXClient
{
    class TestServer
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🧪 OptiX 간단한 서버 테스트");
            Console.WriteLine("================================");

            SimpleServer server = new SimpleServer();

            try
            {
                await server.StartServerAsync("127.0.0.1", 8888);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 서버 실행 오류: {ex.Message}");
            }

            Console.WriteLine("아무 키나 누르면 종료...");
            Console.ReadKey();
            server.StopServer();
        }
    }
}


using System;
using System.Drawing;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace OptiXClient
{
    public partial class MainForm : Form
    {
        private TcpClient tcpClient;
        private NetworkStream stream;
        private bool isConnected = false;
        private List<string> messageHistory = new List<string>();

        // UI 컨트롤들
        private TextBox txtMessage;
        private Button btnSend;
        private TextBox txtMessageHistory;
        private Label lblStatus;
        private Label lblServerInfo;

        public MainForm()
        {
            InitializeComponent();
            InitializeConnection();
        }

        private void InitializeComponent()
        {
            // 폼 설정
            this.Text = "OptiX 운영 프로그램 (클라이언트)";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(248, 250, 252);

            // 서버 정보 라벨
            lblServerInfo = new Label
            {
                Text = "서버: 127.0.0.1:7777",
                Location = new Point(20, 20),
                Size = new Size(200, 25),
                Font = new Font("맑은 고딕", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            this.Controls.Add(lblServerInfo);

            // 상태 라벨
            lblStatus = new Label
            {
                Text = "상태: 연결 안됨",
                Location = new Point(20, 50),
                Size = new Size(200, 25),
                Font = new Font("맑은 고딕", 9),
                ForeColor = Color.FromArgb(239, 68, 68)
            };
            this.Controls.Add(lblStatus);

            // 메시지 입력 텍스트박스
            txtMessage = new TextBox
            {
                Location = new Point(20, 90),
                Size = new Size(500, 30),
                Font = new Font("맑은 고딕", 10),
                PlaceholderText = "서버에 보낼 메시지를 입력하세요 (예: TEST_START)",
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            txtMessage.KeyPress += TxtMessage_KeyPress;
            this.Controls.Add(txtMessage);

            // 전송 버튼
            btnSend = new Button
            {
                Text = "SEND",
                Location = new Point(540, 90),
                Size = new Size(100, 30),
                Font = new Font("맑은 고딕", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(139, 92, 246),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSend.FlatAppearance.BorderSize = 0;
            btnSend.Click += BtnSend_Click;
            btnSend.MouseEnter += (s, e) => btnSend.BackColor = Color.FromArgb(124, 58, 237);
            btnSend.MouseLeave += (s, e) => btnSend.BackColor = Color.FromArgb(139, 92, 246);
            this.Controls.Add(btnSend);

            // 메시지 히스토리 텍스트박스
            txtMessageHistory = new TextBox
            {
                Location = new Point(20, 140),
                Size = new Size(740, 400),
                Font = new Font("Consolas", 9),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.FromArgb(241, 245, 249),
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(txtMessageHistory);

            // 초기 메시지 추가
            AddMessageToHistory("🚀 OptiX 클라이언트 시작됨");
            AddMessageToHistory("📡 서버 연결 시도 중...");
            InitializeConnection();
        }

        private void InitializeConnection()
        {
            Task.Run(async () =>
            {
                await TryConnectWithRetry();
            });
        }

        private async Task TryConnectWithRetry()
        {
            int maxRetries = 100; // 최대 100번 재시도
            int retryCount = 0;
            int retryDelay = 2000; // 2초마다 재시도
            bool infiniteRetry = false; // 무한 재시도 옵션

            while ((infiniteRetry || retryCount < maxRetries) && !isConnected)
            {
                try
                {
                    retryCount++;
                    
                    if (retryCount == 1)
                    {
                        AddMessageToHistory("📡 서버 연결 시도 중...");
                    }
                    else
                    {
                        AddMessageToHistory($"🔄 서버 재연결 시도 중... ({retryCount}/{maxRetries})");
                    }

                    tcpClient = new TcpClient();
                    await tcpClient.ConnectAsync("127.0.0.1", 7777);
                    stream = tcpClient.GetStream();
                    isConnected = true;

                    this.Invoke(new Action(() =>
                    {
                        lblStatus.Text = "상태: 연결됨";
                        lblStatus.ForeColor = Color.FromArgb(34, 197, 94);
                        AddMessageToHistory("✅ 서버 연결 성공!");
                        AddMessageToHistory("📡 서버: 127.0.0.1:7777");
                        AddMessageToHistory("💡 메시지를 입력하고 SEND 버튼을 클릭하세요");
                    }));

                    // 서버로부터 응답 수신 대기
                    await ListenForResponses();
                    break; // 성공하면 루프 종료
                }
                catch (Exception ex)
                {
                    this.Invoke(new Action(() =>
                    {
                        lblStatus.Text = "상태: 연결 실패";
                        lblStatus.ForeColor = Color.FromArgb(239, 68, 68);
                        
                        if (retryCount == 1)
                        {
                            AddMessageToHistory($"❌ 서버 연결 실패: {ex.Message}");
                            AddMessageToHistory("🔍 OptiX UI 서버가 실행 중인지 확인해주세요");
                            AddMessageToHistory("🔍 서버 포트가 7777인지 확인해주세요");
                        }
                        else if (retryCount % 10 == 0) // 10번마다 한 번씩 메시지 출력
                        {
                            if (retryCount >= maxRetries)
                            {
                                AddMessageToHistory($"⏳ 서버 연결 대기 중... (무한 재시도 모드)");
                            }
                            else
                            {
                                AddMessageToHistory($"⏳ 서버 연결 대기 중... ({retryCount}/{maxRetries})");
                            }
                        }
                    }));

                    // 연결 실패 시 리소스 정리
                    try
                    {
                        tcpClient?.Close();
                        stream?.Close();
                    }
                    catch { }

                    // 재시도 전 대기
                    if (retryCount < maxRetries || infiniteRetry)
                    {
                        await Task.Delay(retryDelay);
                    }
                    
                    // 100번 시도 후 무한 재시도 모드로 전환
                    if (retryCount >= maxRetries && !infiniteRetry)
                    {
                        infiniteRetry = true;
                        this.Invoke(new Action(() =>
                        {
                            AddMessageToHistory($"🔄 {maxRetries}번 시도 완료. 무한 재시도 모드로 전환합니다...");
                        }));
                    }
                }
            }

            // 무한 재시도 모드가 아닌 경우에만 메시지 표시
            if (!isConnected && !infiniteRetry && retryCount >= maxRetries)
            {
                this.Invoke(new Action(() =>
                {
                    AddMessageToHistory($"❌ 최대 재시도 횟수({maxRetries}) 초과. 수동으로 다시 시도해주세요.");
                }));
            }
        }

        private async Task ListenForResponses()
        {
            byte[] buffer = new byte[4096];
            while (isConnected && tcpClient?.Connected == true)
            {
                try
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        this.Invoke(new Action(() =>
                        {
                            AddMessageToHistory($"📥 서버 응답: {response}");
                            
                            // 서버 종료 메시지 처리
                            if (response == "SERVER_SHUTDOWN")
                            {
                                isConnected = false;
                                lblStatus.Text = "상태: 서버 종료됨";
                                lblStatus.ForeColor = Color.FromArgb(239, 68, 68);
                                AddMessageToHistory("🔌 서버가 종료되었습니다.");
                            }
                        }));
                    }
                    else
                    {
                        // 연결이 끊어진 경우
                        this.Invoke(new Action(() =>
                        {
                            lblStatus.Text = "상태: 연결 끊김";
                            lblStatus.ForeColor = Color.FromArgb(239, 68, 68);
                            AddMessageToHistory("🔌 서버 연결이 끊어졌습니다. 재연결 시도 중...");
                        }));
                        
                        isConnected = false;
                        break; // ListenForResponses 루프 종료
                    }
                }
                catch (Exception ex)
                {
                    this.Invoke(new Action(() =>
                    {
                        AddMessageToHistory($"❌ 응답 수신 오류: {ex.Message}");
                        
                        // 연결 끊김 처리
                        isConnected = false;
                        lblStatus.Text = "상태: 연결 끊김";
                        lblStatus.ForeColor = Color.FromArgb(239, 68, 68);
                        AddMessageToHistory("🔌 서버 연결이 끊어졌습니다.");
                        AddMessageToHistory("🔄 자동 재연결 시도 중...");
                    }));
                    break;
                }
            }

            // ListenForResponses 종료 시 자동 재연결 시도
            if (!isConnected)
            {
                this.Invoke(new Action(() =>
                {
                    AddMessageToHistory("🔄 자동 재연결을 시작합니다...");
                }));
                
                // 잠시 대기 후 재연결 시도
                await Task.Delay(3000);
                await TryConnectWithRetry();
            }
        }

        private void TxtMessage_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                SendMessage();
            }
        }

        private void BtnSend_Click(object sender, EventArgs e)
        {
            SendMessage();
        }

        private void SendMessage()
        {
            if (!isConnected || stream == null)
            {
                AddMessageToHistory("❌ 서버에 연결되지 않았습니다");
                return;
            }

            string message = txtMessage.Text.Trim();
            if (string.IsNullOrEmpty(message))
            {
                AddMessageToHistory("⚠️ 메시지를 입력해주세요");
                return;
            }

            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                AddMessageToHistory($"📤 전송 시도: {message} (바이트 수: {data.Length}, 연결상태: {isConnected})");
                
                // 전송 전 연결 상태 재확인
                if (!isConnected || stream == null || !tcpClient.Connected)
                {
                    AddMessageToHistory($"❌ 연결 상태 오류 - 연결상태: {isConnected}, 스트림: {(stream != null ? "있음" : "없음")}, TCP: {(tcpClient?.Connected ?? false)}");
                    return;
                }
                
                stream.Write(data, 0, data.Length);
                stream.Flush(); // 버퍼 강제 플러시
                
                AddMessageToHistory($"✅ 전송 완료: {message} (실제 전송된 바이트: {data.Length})");
                txtMessage.Clear();
                txtMessage.Focus();
            }
            catch (Exception ex)
            {
                AddMessageToHistory($"❌ 전송 실패: {ex.Message}");
            }
        }

        private void AddMessageToHistory(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string fullMessage = $"[{timestamp}] {message}";
            
            messageHistory.Add(fullMessage);
            txtMessageHistory.AppendText(fullMessage + Environment.NewLine);
            txtMessageHistory.SelectionStart = txtMessageHistory.Text.Length;
            txtMessageHistory.ScrollToCaret();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            isConnected = false;
            stream?.Close();
            tcpClient?.Close();
            base.OnFormClosing(e);
        }
    }
}

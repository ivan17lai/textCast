using Fleck;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace text_cast
{
    public partial class Form1 : Form
    {
        private WebSocketServer server;
        private List<IWebSocketConnection> clients = new List<IWebSocketConnection>();
        private HttpListener httpListener;
        private Thread httpThread;

        public Form1()
        {
            InitializeComponent();
            StartWebSocketServer();
            StartHttpServer();
        }

        private void StartWebSocketServer()
        {
            string localIP = GetLocalIPAddress();
            string websocketAddress = $"ws://{localIP}:8080";

            server = new WebSocketServer(websocketAddress);
            server.Start(socket =>
            {
                socket.OnOpen = () =>
                {
                    clients.Add(socket);
                    Console.WriteLine($"New client connected. [{socket.ConnectionInfo.ClientIpAddress}]");
                };
                socket.OnClose = () =>
                {
                    clients.Remove(socket);
                    Console.WriteLine("Client disconnected.");
                };
                socket.OnMessage = message =>
                {
                    Console.WriteLine("Received: " + message);
                };
            });

            label1.Text = $"WebSocket: {websocketAddress} \n HTTP: http://{localIP}:8081";
            Console.WriteLine($"WebSocket Server started at {websocketAddress}");
        }

        private void StartHttpServer()
        {
            string localIP = GetLocalIPAddress();
            string httpAddress = $"http://{localIP}:8081/";

            httpListener = new HttpListener();
            httpListener.Prefixes.Add(httpAddress);
            httpListener.Start();

            httpThread = new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        var context = httpListener.GetContext();
                        var response = context.Response;

                        string html = GetHtmlPage(localIP);
                        byte[] buffer = Encoding.UTF8.GetBytes(html);

                        response.ContentLength64 = buffer.Length;
                        response.OutputStream.Write(buffer, 0, buffer.Length);
                        response.OutputStream.Close();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("HTTP Server Error: " + ex.Message);
                    }
                }
            });

            httpThread.IsBackground = true;
            httpThread.Start();

            label1.Text = $"HTTP Server started at {httpAddress}";

            Console.WriteLine($"HTTP Server started at {httpAddress}");
        }

        private string GetHtmlPage(string ip)
        {
            return $@"
            <!DOCTYPE html>
            <html lang='zh-TW'>
            <head>
                <meta charset='UTF-8'>
                <title>WebSocket 測試</title>
            </head>
            <body>
                <h2>WebSocket 測試</h2>
                <p id='output'>等待數據...</p>
                <script>
                    let ws = new WebSocket('ws://{ip}:8080');
                    ws.onmessage = function(event) {{
                        document.getElementById('output').innerText = '收到: ' + event.data;
                    }};
                    ws.onopen = function() {{
                        console.log('已連接 WebSocket 伺服器');
                    }};
                    ws.onclose = function() {{
                        console.log('WebSocket 連線關閉');
                    }};
                </script>
            </body>
            </html>";
        }

        private string GetLocalIPAddress()
        {
            string localIP = "127.0.0.1"; // 預設為本機
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                    break;
                }
            }
            return localIP;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            string text = textBox1.Text;
            foreach (var client in clients)
            {
                client.Send(text);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            server.Dispose();
            httpListener.Stop();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }
    }
}

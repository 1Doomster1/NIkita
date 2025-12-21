using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NIkita;

namespace Server
{
    class Server
    {
        private static TcpListener server;
        private static TcpClient client;
        private static NetworkStream stream;
        private static bool isRunning = false;
        private static CancellationTokenSource cts = new CancellationTokenSource();

        public async Task StartServerAsync()
        {
            if (isRunning) return;
            int port = GetPortFromUser();

            server = new TcpListener(IPAddress.Any, port);
            server.Start();

            isRunning = true;
            Console.WriteLine($"Сервер запущен на порту {port}");
            Console.WriteLine("Ожидание подключения клиента...");
            Console.WriteLine("Для остановки сервера 'с'");


            while (!cts.Token.IsCancellationRequested)
            {
                client = await server.AcceptTcpClientAsync();
                stream = client.GetStream();

                Console.WriteLine("\nКлиент подключен");
                Console.WriteLine($"Адрес клиента: {((IPEndPoint)client.Client.RemoteEndPoint).Address}");
                Console.WriteLine("Для отключения введите '/disconnect'");
                Console.WriteLine();
                await ClientHandler();
            }
        }

        static int GetPortFromUser()
        {
            int port = 9000;

            Console.Write($"Введите порт сервера [по умолчанию {port}]: ");
            string input = Console.ReadLine();

            if (!string.IsNullOrWhiteSpace(input) && int.TryParse(input, out int userPort) && userPort > 0 && userPort <= 65535)
                return userPort;

            Console.WriteLine($"Используется порт по умолчанию: {port}");
            return port;
        }

        static async Task ClientHandler()
        {
            var receiveTask = Task.Run(async () =>
            {
                byte[] buffer = new byte[1024];

                while (client.Connected && !cts.Token.IsCancellationRequested)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);

                    if (bytesRead == 0)
                    {
                        Console.WriteLine("\nКлиент отключился");
                        break;
                    }

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"Клиент: {message}");

                    if (message.Trim().ToLower() == "/disconnect")
                    {
                        Console.WriteLine("Клиент запросил отключение");
                        StopServer();
                        break;
                    }
                }
            });

            var sendTask = Task.Run(async () =>
            {
                while (client.Connected && !cts.Token.IsCancellationRequested)
                {
                    string message = Console.ReadLine();

                    if (cts.Token.IsCancellationRequested)
                        break;

                    if (string.IsNullOrWhiteSpace(message))
                        continue;

                    byte[] data = Encoding.UTF8.GetBytes(message);
                    await stream.WriteAsync(data, 0, data.Length, cts.Token);
                    await stream.FlushAsync();

                    if (message.Trim().ToLower() == "/disconnect")
                    {
                        Console.WriteLine("Отправка команды отключения клиенту");
                        break;
                    }
                }
            });

            await Task.WhenAny(receiveTask, sendTask);
            DisconnectClient();
        }

        static void DisconnectClient()
        {
            if (stream != null)
                stream.Close();
            if (client != null)
                client.Close();
            Console.WriteLine("Ожидание нового подключения");
        }

        static public void StopServer()
        {
            isRunning = false;

            if (stream != null)
                stream.Close();

            if (client != null)
                client.Close();

            if (server != null)
                server.Stop();

            Console.WriteLine("Сервер остановлен");
        }
    }
}
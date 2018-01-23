using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;

namespace ChatClient
{
    class Program
    {
        static string userName;
        private const string host = "192.168.1.220";
        private const int port = 8888;
        static TcpClient client;
        static NetworkStream stream;

        static void Main(string[] args)
        {
            Console.Write("Enter your name: ");
            userName = Console.ReadLine();
            client = new TcpClient();
            try
            {
                if (null != host && 0 != host.Length)
                {
                    client.Connect(host, port);                                         // Подключение клиента
                    stream = client.GetStream();                                        // Получаем поток
                }

                string message = userName;
                byte[] data = Encoding.Unicode.GetBytes(message);
                stream.Write(data, 0, data.Length);

                // запускаем новый поток для получения данных
                Thread receiveThread = new Thread(new ThreadStart(ReceiveMessage));
                if (null != receiveThread)
                {
                    receiveThread.Start();                                                  //старт потока
                    Console.WriteLine("Welcome back, {0}", userName);
                    SendMessage();
                }
                else
                {
                    Console.WriteLine("Connection Lost");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Disconnect();
            }
        }


        // отправка сообщений
        static void SendMessage()
        {
            Console.WriteLine("Enter your message: ");

            while (true)
            {
                string message = Console.ReadLine();
                if (null != message && 0 != message.Length)
                {
                    byte[] data = Encoding.Unicode.GetBytes(message);
                    stream.Write(data, 0, data.Length);
                }
            }

        }


        // получение сообщений
        static void ReceiveMessage()
        {
            while (true)
            {
                try
                {
                    byte[] data = new byte[64];                                    // буфер для получаемых данных
                    StringBuilder builder = new StringBuilder();
                    int bytes = 0;
                    do
                    {
                        bytes = stream.Read(data, 0, data.Length);
                        builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                    }
                    while (stream.DataAvailable);

                    string message = builder.ToString();
                    Console.WriteLine(message);                                   //вывод сообщения
                }
                catch
                {
                    Console.WriteLine("Connection is interrupted!");            //соединение было прервано
                    Console.ReadLine();
                    Disconnect();
                }
            }
        }

        static void Disconnect()
        {
            if (stream != null)
                stream.Close();                                                  //отключение потока
            if (client != null)
                Console.WriteLine("\nSee you later!");
                client.Close();                                                  //отключение клиента
            Environment.Exit(0);                                                 //завершение процесса
        }

    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace ChatServer
{
//********************************************************
    public class ClientObject
    {
        String userName;
        TcpClient client;
        ServerObject server;                                                        //Объект сервера

        protected internal String Id
        {
            get;
            private set;
        }
        
        protected internal NetworkStream Stream 
        { 
            get; 
            private set; 
        }

        public ClientObject(TcpClient tcpClient, ServerObject serverObject)
        {
            Id = Guid.NewGuid().ToString();
            client = tcpClient;
            server = serverObject;
            serverObject.AddConnection(this);
        }

        public void Process()
        {
            try
            {
                Stream = client.GetStream();
                String message = GetMessage();                                      //Получаем имя пользователя
                userName = message;

                message = userName + " вошёл в чат";
                server.BroadcastMessage(message, this.Id);                          //Посылаем сообщение о входе в чат всем подключенным пользователям
                Console.WriteLine(message);

                while (true)                                                         //В бесконечном цикле получаем сообщения от клиента
                {
                    try
                    {
                        message = GetMessage();
                        message = String.Format("{0}: {1}", userName, message);
                        Console.WriteLine(message);
                        server.BroadcastMessage(message, this.Id);
                    }
                    catch
                    {
                        message = String.Format("{0}: покинул чат", userName);
                        Console.WriteLine(message);
                        server.BroadcastMessage(message, this.Id);
                        break;
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }
            finally
            {
                server.RemoveConnection(this.Id);
                Close();
            }
        }

        // чтение входящего сообщения и преобразование в строку
        private string GetMessage()
        {
            byte[] data = new byte[64]; // буфер для получаемых данных
            StringBuilder builder = new StringBuilder();
            int bytes = 0;
            do
            {
                bytes = Stream.Read(data, 0, data.Length);
                builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
            }
            while (Stream.DataAvailable);
 
            return builder.ToString();
        }
 
        // закрытие подключения
        protected internal void Close()
        {
            if (Stream != null)
                Stream.Close();
            if (client != null)
                client.Close();
        }
    }
//***********************************************


//++++++++++++++++++++++++++++++++++++++++++++++
    public class ServerObject
    {
        static TcpListener tcpListener;                                             //Сервер для прослушивания 
        List<ClientObject> clients = new List<ClientObject>();                      //Все подключения

        protected internal void AddConnection(ClientObject clientObject)
        {
            clients.Add(clientObject);
        }

        protected internal void RemoveConnection(String id)
        {
            //Получаем по id закрытое подключение
            ClientObject client = clients.FirstOrDefault(c => c.Id == id);
            
            //И удаляем его из списка подключений
            if (client != null)
            {
                clients.Remove(client);
            }

        }

        //Прослушивание входящих сообщений
        protected internal void Listen()
        {
            try
            {
                tcpListener = new TcpListener(IPAddress.Any, 8888);
                tcpListener.Start();
                Console.WriteLine("Сервер запущен. Ожидание подключений... ");

                while (true)
                {
                    TcpClient tcpClient = tcpListener.AcceptTcpClient();

                    ClientObject clientObject = new ClientObject(tcpClient, this);
                    Thread clientThread = new Thread(new ThreadStart(clientObject.Process));
                    clientThread.Start();
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
                Disconnect();                                                                   //Отключение связи
            }
        }


        //Трансляция сообщения подключённым клиентам
        protected internal void BroadcastMessage(String message, String id)
        {
            byte[] data = Encoding.Unicode.GetBytes(message);
            for (int i = 0; i < clients.Count; i++)
            {
                if (clients[i].Id != id)                                                        //Если id клиента не равно id отправляющего
                {
                    clients[i].Stream.Write(data, 0, data.Length);                              //Передача данных
                }
            }
        }


        //Отключение всех клиентов
        protected internal void Disconnect()
        {
            tcpListener.Stop();                                                                 //Остановка сервера

            for (int i = 0; i < clients.Count; i++)
            {
                clients[i].Close();                                                             //Отключение клиента
            }

            Environment.Exit(0);                                                                //Завершение процесса
        }

    }
//+++++++++++++++++++++++++++++++++++++++++++++



//-----------------------------------------------
    class Program
    {
        static ServerObject server;                                                             // сервер
        static Thread listenThread;                                                             // потока для прослушивания
        static void Main(string[] args)
        {
            try
            {
                server = new ServerObject();
                listenThread = new Thread(new ThreadStart(server.Listen));
                listenThread.Start();                                                           //старт потока
            }
            catch (Exception ex)
            {
                server.Disconnect();
                Console.WriteLine(ex.Message);
            }
        }
    }
//----------------------------------------------

}

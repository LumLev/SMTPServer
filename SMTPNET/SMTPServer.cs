using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using SMTPNET.Models;

namespace SMTPNET
{
    public class SMTPServer
    {
        private Socket listener; 
        private string _path;
        public SMTPServer(string ipAdressOrHost = "0.0.0.0", int port = 25, int maxConcurrent = 5)
        {
            listener = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            
            _path = Path.Combine(AppContext.BaseDirectory, "emails");
            if (!Directory.Exists(_path)) {  Directory.CreateDirectory(_path); }


            listener.Bind(IPEndPoint.Parse($"{ipAdressOrHost}:{port}"));
            IasyncQueue = new ConcurrentQueue<IAsyncResult>();
            listener.Listen(maxConcurrent);
            
        }

        internal ConcurrentQueue<IAsyncResult> IasyncQueue;
        public bool OnGoing;
        internal void HandleConnections()
        {
            OnGoing = true;
            while (IasyncQueue.Count > 0)
            {
                if (IasyncQueue.Count > 1)
                {
                    Parallel.ForEach(IasyncQueue, que => 
                    { 
                       if (IasyncQueue.TryDequeue(out que!))
                        { AcceptClient(que); }
                    });
                }
                else 
                {
                    IAsyncResult? que;
                    IasyncQueue.TryDequeue(out que);
                    if (que is not null) { AcceptClient(que); }
                }
            }
            OnGoing = false;
        }


        internal void AddToQueue(IAsyncResult begunsocket)
        {
            IasyncQueue.Enqueue(begunsocket);
            if (OnGoing is not true)
            { HandleConnections(); }
            StartAcceptMail();
        }


        public void StartAcceptMail()
        {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Listening: {listener.RemoteEndPoint}");
                        Console.ForegroundColor = ConsoleColor.Magenta;
            IAsyncResult acceptingsocket = listener.BeginAccept(null,null);
            AddToQueue(acceptingsocket);
        }
        public void AcceptClient(IAsyncResult client)
        {
            Socket IncomingClient = listener.EndAccept(client);
            string? IncomingIP = IncomingClient.RemoteEndPoint?.ToString()?.Split(":")[0];
            if (IncomingIP is not null)
            {
                var clienthost = Dns.GetHostEntry(IncomingIP);
                        Console.WriteLine($"Accepted HostName: {clienthost.HostName}");
                        Console.WriteLine($"RemoteEndPoint: {IncomingClient.RemoteEndPoint}");
                        Console.ForegroundColor = ConsoleColor.Yellow;
                SMTPResponse sreq = new(IncomingClient, _path, clienthost.HostName);
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine(sreq.DataResult.ToString());
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine($"========= Finished Receiving From: {sreq.Sender} =========");
            }
        }
    }
}


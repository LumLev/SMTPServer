using System.Collections;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using SMTPNET.Models;

namespace SMTPNET
{
    public class SMTPServer
    {

        private TcpListener listener;
           
        private string _path;

        public SMTPServer(int port)
        {
            listener = new TcpListener(IPEndPoint.Parse($"0.0.0.0:{port}"));
            _path = Path.Combine(AppContext.BaseDirectory, "emails");
            if (!Directory.Exists(_path))
            {
                Directory.CreateDirectory(_path);
            }
            listener.Start(5);
            IASYNCS = new Collection<IAsyncResult>();
        }

        internal Collection<IAsyncResult> IASYNCS;


        public bool OnGoing;

        internal void HandleConnections()
        {
            while(IASYNCS.Count > 0)
            {
                OnGoing = true;
                AcceptClient(IASYNCS[0]);
                IASYNCS.RemoveAt(0);
            }
            OnGoing = false;
            StartAcceptMail();
        }

        internal void AddToQueue(IAsyncResult begunsocket)
        {
            IASYNCS.Add(begunsocket);
            if (OnGoing is not true)
            { HandleConnections(); }
            else
            { StartAcceptMail(); } 
        }
       
        public void StartAcceptMail()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Listening: {listener.LocalEndpoint}");
            Console.ForegroundColor = ConsoleColor.Magenta;
            IAsyncResult acceptingsocket = listener.BeginAcceptSocket(null,null);
            AddToQueue(acceptingsocket);
        }


        public void AcceptClient(IAsyncResult client)
        {
            Socket IncomingClient = listener.EndAcceptSocket(client);
            string? IncomingIP = IncomingClient.RemoteEndPoint?.ToString()?.Split(":")[0];
            if (IncomingIP is not null)
            {
                var clienthost = Dns.GetHostEntry(IncomingIP);
                Console.WriteLine($"Accepted HostName: {clienthost.HostName}");
            }
            

            Console.WriteLine($"RemoteEndPoint: {IncomingClient.RemoteEndPoint}");
            
            Console.ForegroundColor = ConsoleColor.Yellow;
            SMTPRequest sreq = new(IncomingClient, _path);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(sreq.dataresult.ToString());
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"========= Finished Receiving From: {sreq.Sender} =========");
        }
    }
}








//Console.ForegroundColor = ConsoleColor.Green;
//Console.WriteLine($"Listening: {listener.LocalEndpoint}");
//Console.ForegroundColor = ConsoleColor.Magenta;
//Socket IncomingClient = listener.AcceptSocket();
//Console.WriteLine($"Accepted: {IncomingClient.RemoteEndPoint}");
//Console.ForegroundColor = ConsoleColor.Yellow;
//SMTPRequest sreq = new(IncomingClient, _path);
//Console.ForegroundColor = ConsoleColor.Cyan;
//Console.WriteLine(sreq.dataresult.ToString());
//Console.ForegroundColor = ConsoleColor.Magenta;
//Console.WriteLine($"========= Finished Receiving From: {sreq.Sender} =========");

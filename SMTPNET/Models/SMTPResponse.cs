using System.ComponentModel;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace SMTPNET.Models
{
    public ref struct SMTPResponse
    {
        private const int readCount = 512000;
        private const string lineTerminator = "\r\n";
        private const string dataTerminator = "\r\n.\r\n";

        private ReadOnlySpan<char> _pathfile;

        public ReadOnlySpan<char> DataResult;

        public ReadOnlySpan<char> Sender;

        private Socket TheSocket;
        private StatusEnum status;

        public ReadOnlySpan<char> HostDnsName;

        public bool GracefulFinish;

        public SMTPResponse(Socket incomingSocket, string basepath, string hostDnsName)
        {
            HostDnsName = hostDnsName;
            _pathfile = Path.Combine(basepath, $"{DateTime.Now.ToString("yyyyMMdd.hhmmss.FFFFFFF")}.email");
           // DataResult = "";
            TheSocket = incomingSocket;
            status = StatusEnum.Connected;
            Start();
        }

        internal int DisconnectCounter = 0;

        /// <summary>
        /// Starts the 
        /// </summary>
        public void Start()
        {
            WriteLine("220 Welcome <mail.yourdomain.tld> SMTP Server");
            
            while (TheSocket.Connected)
            {
                if (DisconnectCounter > 2) { TheSocket.Disconnect(false); break; }
                ReadOnlySpan<char> data = Read(lineTerminator);
                if (data.Length < 1) { DisconnectCounter++; break; }
                else
                {
                    if (data.StartsAs("QUIT"))
                    {
                        WriteLine("221 Good bye");
                        TheSocket.Disconnect(false);
                        break;
                    }
                    else
                    {
                        switch (data[0..4])
                        {
                            case "QUIT":
                                WriteLine("221 Good bye");
                                TheSocket.Disconnect(false);
                                break;
                            case "RSET":
                                WriteLine("250 OK");
                                status = StatusEnum.Identified;
                                Sender = "";
                                DataResult = "";
                                break;
                            case "HELO":
                            case "EHLO":
                                if (status is StatusEnum.Connected)
                                {
                                    WriteGreeting(data.Slice(0, 4));
                                    status = StatusEnum.Identified;
                                }
                                else { DisconnectCounter++; WriteCommand(SmtpResponseCode.BadSequenceOfCommands);  }
                                break;
                            case "MAIL":
                                if (status is StatusEnum.Identified)
                                {
                                   
                                    status = StatusEnum.Mail;
                                    Sender = data.GetFirstTag();
                                    if (HostDnsName.EndsWith(Sender[(Sender.IndexOf('@') + 1)..]))
                                    { WriteLine("250 OK"); }
                                    else
                                    {
                                        WriteCommand(SmtpResponseCode.DnsError);
                                        WriteLine("QUIT");
                                        TheSocket.Disconnect(false);
                                    }
                                    
                                   
                                } 
                                else { DisconnectCounter++; WriteCommand(SmtpResponseCode.BadSequenceOfCommands);  }
                                break;
                            case "RCPT":
                                if (status is StatusEnum.Recipient || status is StatusEnum.Mail)
                                {
                                    status = StatusEnum.Recipient;
                                    WriteLine("250 OK");
                                } 
                                else { DisconnectCounter++; WriteCommand(SmtpResponseCode.BadSequenceOfCommands); }
                                break;
                            case "DATA":
                                if (status is not StatusEnum.Recipient)
                                { DisconnectCounter++; WriteCommand(SmtpResponseCode.BadSequenceOfCommands); }
                                else
                                {
                                    status = StatusEnum.Data;
                                    WriteLine("354 Start mail input; end with <CRLF>.<CRLF>");
                                    GracefulFinish = SaveData();
                                    WriteLine("250 OK");
                                    WriteLine("QUIT");
                                    TheSocket.Disconnect(false);
                                }
                                break;
                            default:
                                WriteLine(" Command not implemented");
                                DisconnectCounter++;
                                break;
                        }
                    }
                }
            }
        }


        private ReadOnlySpan<char> Read(string terminator)
        {
            Span<byte> bytes = stackalloc byte[64];
            Span<char> chars = new char[64];
            bool? spin = null;
            while (spin is not true)
            {
                var count = TheSocket.Receive(bytes);
                if (count == 0) { break; }
                if (bytes [(count-2) .. count].EndsAs(terminator)) { break; }
                if (spin is null) { spin = false; }
                else if (spin is false) {  spin = true;}
            }
            Encoding.UTF8.GetChars(bytes, chars);
            ConsoleWrite(chars.ToString());
            return chars;
        }


        private bool SaveData()
        {
            Span<byte> bytes = stackalloc byte[2048];
            using (Stream fs = File.OpenWrite(_pathfile.ToString()))
            {
             //   int total = 0;
            reread:              
                int count = TheSocket.Receive(bytes);
                if (count == 0) { return false; }
                else
                {
                    ReadOnlySpan<char> theread = Encoding.UTF8.GetString(bytes[0..count]);

                    Console.WriteLine(theread.ToString());
                    fs.Write(bytes); 

                    if (theread.EndsAs("\r\n.\r\n")) { return true; }
                    else
                    { goto reread; }
                } 
            }
        }

        private void WriteCommand(SmtpResponseCode smtpResponseCode)
        {
            WriteLine($"${(int)smtpResponseCode} ${smtpResponseCode}");
        }


        private void WriteLine(string data)
        {
            string message =  data + lineTerminator;
            TheSocket.Send(message.ToUTF8());
            ConsoleWrite(data);
        }


        private void WriteGreeting(ReadOnlySpan<char> id)
        {
            WriteLine($"250 Hello {id}");
        }

        private static void ConsoleWrite(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                throw new Exception("Zero length data found.");
            }

            data = data.Trim();
            // If multiline message trim only first line
            if (data.IndexOf(Environment.NewLine) > 0)
            {
                data = string.Concat(data.Substring(0, data.IndexOf(Environment.NewLine)), " ...");
            }

            Console.WriteLine("{0:HH:mm:ss.FFF}: {1}", DateTime.Now, data);
        }


    }
}

using System.ComponentModel;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace SMTPNET.Models
{
    public ref struct SMTPRequest
    {
        private const int readCount = 512000;
        private const string lineTerminator = "\r\n";
        private const string dataTerminator = "\r\n.\r\n";

        private ReadOnlySpan<char> _pathfile;
        public ReadOnlySpan<char> dataresult;

        public ReadOnlySpan<char> Sender;

        private Socket socket;
        private StatusEnum status;

        public bool GracefulFinish;

        public SMTPRequest(Socket incomingSocket, string basepath)
        {
            _pathfile = Path.Combine(basepath, $"{DateTime.Now.ToString("yyyyMMdd.hhmmss.FFFFFFF")}.email");
            dataresult = "";
            socket = incomingSocket;
            status = StatusEnum.Connected;
            Start();
        }

        internal int NullCounter;

        /// <summary>
        /// Starts the 
        /// </summary>
        public void Start()
        {
            WriteLine("220 Welcome <mail.yourdomain.tld> SMTP Server");
            
            while (socket.Connected)
            {
                ReadOnlySpan<char> data = Read(lineTerminator);
                if (data == null) 
                {
                    NullCounter++;
                    if (NullCounter == 3) { socket.Dispose(); }
                    break;
                }
                else
                {
                    if (data.StartsAs("QUIT"))
                    {
                        WriteLine("221 Good bye");
                        socket.Dispose();
                        break;
                    }
                    else
                    {
                        switch (data[0..4])
                        {
                            case "QUIT":
                                WriteLine("221 Good bye");
                                socket.Dispose();
                                break;
                            case "HELO":
                            case "EHLO":
                                WriteGreeting(data.Slice(0, 4));
                                status = StatusEnum.Identified;
                                break;
                            case "MAIL":
                                status = StatusEnum.Mail;
                                WriteOk();
                                this.Sender = data.GetFirstTag();
                                break;
                            case "RCPT":
                                status = StatusEnum.Recipient;
                                WriteOk();
                                break;
                            case "DATA":
                                WriteSendData();
                                GracefulFinish = SaveData();
                                this.WriteOk();
                                this.WriteLine("QUIT");
                                socket.Dispose();
                                break;
                            default:
                                WriteError(ErrorEnum.CommandNotImplemented, "Command not implemented");
                                socket.Dispose();
                                break;
                        }
                    }
                }
            }
        }


        internal void WriteCmdUnkownAndDispose()
        {
            WriteError(ErrorEnum.CommandNotImplemented, "Command not implemented");
            socket.Dispose();
        }


        private ReadOnlySpan<char> Read(string terminator)
        {
            Span<byte> bytes = stackalloc byte[64];
            Span<char> chars = new char[64];
            bool? spin = null;
            while (spin is not true)
            {
                var count = socket.Receive(bytes);
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
                int count = socket.Receive(bytes);
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



        private void WriteLine(string data)
        {
            string message =  data + lineTerminator;
            socket.Send(message.ToUTF8());
            ConsoleWrite(data);
        }

        private void WriteOk()
        {
            WriteLine("250 OK");
        }

        private void WriteError(ErrorEnum error, ReadOnlySpan<char> description)
        {
            WriteLine($"{error} - {description}");
        }

        private void WriteGreeting(ReadOnlySpan<char> id)
        {
            WriteLine($"250 Hello {id}");
        }

        private void WriteSendData()
        {
            WriteLine("354 Start mail input; end with <CRLF>.<CRLF>");
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

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

        public SMTPRequest(Socket incomingSocket, string basepath)
        {
            _pathfile = Path.Combine(basepath, $"{DateTime.Now.ToString("yyyyMMdd.hhmmss.FFFFFFF")}.email");
            dataresult = "";
            socket = incomingSocket;
            status = StatusEnum.Connected;
            Start();
        }

        internal int NullCounter;
        public void Start()
        {
            WriteLine($"220 Welcome <mail.wifiorders.com.de> SMTP Server. {status}");
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
                                dataresult = Read(dataTerminator);

                                using (var fs = File.CreateText(_pathfile.ToString()))
                                {
                                    fs.Write(dataresult);
                                }
                                WriteOk();
                                WriteLine("QUIT");
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

        #region Read/Write
        private ReadOnlySpan<char> Read(string terminator)
        {
            var bytes = new byte[readCount];
            var data = new StringBuilder();

            while (true)
            {
                var count = socket.Receive(bytes);
                if (count == 0) { break; }
                var dataString = Encoding.UTF8.GetString(bytes, 0, count);
                data.Append(dataString);
                if (dataString.EndsWith(terminator)) { break; }
            }

            Debug(data.ToString());
            return data.ToString();
        }

        private void WriteLine(string data)
        {
            data += lineTerminator;
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            socket.Send(bytes);
            Debug(data);
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
        #endregion


        private static void Debug(string data)
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

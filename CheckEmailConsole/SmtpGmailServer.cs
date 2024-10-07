using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;

namespace CheckEmailConsole
{
    public class SmtpGmailServer : IDisposable
    {
        private const string hostName = "smtp.gmail.com";
        private const int hostPort = 587;
        private TcpClient _tcpClient;
        private SslStream _sslStream;

        public SmtpGmailServer()
        {
            var buffer = new byte[1024];
            var str = "";

            _tcpClient = new TcpClient();
            _tcpClient.Connect(hostName, hostPort);
            Console.WriteLine(" Smtp server:");
            _tcpClient.Client.Receive(buffer);
            str = Encoding.UTF8.GetString(buffer);
            checkResponse(str, '2');
            executeCommand($"EHLO {hostName}", '2');
            executeCommand("STARTTLS", '2');

            _sslStream = new SslStream(_tcpClient.GetStream());

            _sslStream.AuthenticateAsClient(hostName);

            var auth = _sslStream.IsAuthenticated;
            Console.WriteLine("     IsAuthenticated:" + auth);
            if (!auth)
            {
                throw new Exception("Authentication failed");
            }
        }

        public string ExecuteVRFY(string mail)
        {
            return executeCommand($"VRFY {mail}", '2', true);
        }
        public string ExecuteEHLO()
        {
            return executeCommand("EHLO gmail.com", '2', true);
        }

        public string ExecuteAUTHLOGIN(string senderEmail, string senderPassword)
        {
            var str = executeCommand("AUTH LOGIN", '3', true);

            str = sendString(senderEmail, true);
            checkResponse(str, '3');
            str = sendString(senderPassword, true);
            checkResponse(str, '2');

            return str;
        }

        public string ExecuteMAILFROM(string senderEmail)
        {
            var str = executeCommand($"MAIL FROM: <{senderEmail}>", '2', true);

            return str;
        }

        public string ExecuteRCPTTO(string receiverEmail)
        {
            var str = executeCommand($"RCPT TO: <{receiverEmail}>", '2', true);

            return str;
        }

        public string ExecuteDATA(string subject, string body)
        {
            var str = executeCommand($"DATA", '3', true);

            sendStringWithoutResponse($"Subject: {subject}");
            sendStringWithoutResponse($"{body}");
            str = sendString(".");
            checkResponse(str, '2');

            return str;
        }

        public string ExecuteQuit()
        {
            var str = executeCommand($"quit", '2', true);

            return str;
        }

        private string sendString(string data, bool convertToBase64 = false)
        {
            var buffer = new byte[1024];
            var str = "";

            data = convertToBase64 ? Convert.ToBase64String(Encoding.UTF8.GetBytes(data)) : data;
            _sslStream.Write(Encoding.UTF8.GetBytes($"{data}\r\n"));
            _sslStream.Read(buffer, 0, buffer.Length);
            str = Encoding.UTF8.GetString(buffer);


            Console.WriteLine("     " + data);
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("     " + str.Substring(0, str.IndexOf('\0') + 1));
            Console.ForegroundColor = ConsoleColor.White;

            return str.Substring(0, str.IndexOf('\0') + 1);
        }
        private void sendStringWithoutResponse(string data, bool convertToBase64 = false)
        {
            data = convertToBase64 ? Convert.ToBase64String(Encoding.UTF8.GetBytes(data)) : data;
            _sslStream.Write(Encoding.UTF8.GetBytes($"{data}\r\n"));
            Console.WriteLine("     " + data);
        }

        private string executeCommand(string command, char firstNumberOfCode, bool withSsl = false)
        {
            var buffer = new byte[1024];
            var str = "";

            if (withSsl)
            {
                _sslStream.Write(Encoding.UTF8.GetBytes($"{command}\r\n"));
                _sslStream.Read(buffer, 0, buffer.Length);
                str = Encoding.UTF8.GetString(buffer);
            }
            else
            {
                _tcpClient.Client.Send(Encoding.UTF8.GetBytes($"{command}\r\n"));
                _tcpClient.Client.Receive(buffer);
                str = Encoding.UTF8.GetString(buffer);
            }
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine("     " + command);
            checkResponse(str, firstNumberOfCode);


            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("     " + str.Substring(0, str.IndexOf('\0') + 1).Replace("\n", "\n     "));
            Console.ForegroundColor = ConsoleColor.White;

            return str.Substring(0, str.IndexOf('\0') + 1);
        }

        private string[] checkResponse(string str, char firstNumberOfCode)
        {
            var responseLines = str.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            if (responseLines[0][0] != firstNumberOfCode)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("     " + str.Substring(0, str.IndexOf('\0') + 1));
                Console.ForegroundColor = ConsoleColor.White;
                throw new Exception(str);
            }

            return responseLines;
        }

        public void Dispose()
        {
            ExecuteQuit();
            _tcpClient.Dispose();
            _sslStream.Dispose();

            GC.SuppressFinalize(this);
        }
        ~SmtpGmailServer()
        {
            Dispose();
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using DnsClient;


namespace CheckEmailConsole
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                const string inputTextFile = "input.txt";
                var recipients = new List<string>();


                using (var streamReader = new StreamReader(inputTextFile))
                {
                    var line = "";

                    while ((line = streamReader.ReadLine()) != null)
                    {
                        recipients.Add(line);
                    }
                }

                foreach (var recipient in recipients)
                {
                    var success = false;
                    var response = checkEmail(recipient, out success);

                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write(" email: ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(recipient);
                    Console.ForegroundColor = success ? ConsoleColor.Green : ConsoleColor.Red;
                    Console.Write(" repsonse: ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(response);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine();
                    Console.WriteLine("///////////////////////////////////////////////////////////////////////////////");
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.Read();
        }

        static string checkEmail(string email, out bool success)
        {
            var startDomainIndex = email.IndexOf('@') + 1;
            var domain = email.Substring(startDomainIndex, email.Length - startDomainIndex);

            var iPAddres = IPAddress.Parse("8.8.8.8");
            var lookup = new LookupClient(new LookupClientOptions(iPAddres));

            var result = lookup.Query(domain, QueryType.ANY);
            var anyRecords = result.Answers.ToList();

            if (anyRecords.Count <= 0)
            {
                success = false;

                return "dns have no any-record";
            }

            result = lookup.Query(domain, QueryType.MX);
            var mxRecords = result.Answers.ToList();

            if (mxRecords.Count <= 0)
            {
                success = false;

                return "dns have no mx-record";
            }

            const string senderEmail = "";
            const string senderPass = "";

            var response = "";

            using (var stmpGmailServer = new SmtpGmailServer())
            {
                try
                {
                    stmpGmailServer.ExecuteAUTHLOGIN(senderEmail, senderPass);
                    stmpGmailServer.ExecuteEHLO();
                    stmpGmailServer.ExecuteMAILFROM(senderEmail);
                    stmpGmailServer.ExecuteRCPTTO(email);
                    response = stmpGmailServer.ExecuteDATA("Test", "It works");
                    stmpGmailServer.ExecuteQuit();
                }
                catch (Exception ex)
                {
                    success = false;

                    return ex.Message;
                }

            }

            success = true;

            return response;
        }
    }
}

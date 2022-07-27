using System.Net;
using TFTP.TFTPLogic;
using System.Text.RegularExpressions;

namespace TFTP.TFTPApp
{
    internal class Program
    {
        static void Main(string[] s_args)
        {
            // put all args into a list because it's easier to work with.
            List<string> args = new(s_args);
            string filename = string.Empty; // name of the file that we will get from\put onto a server
            IPEndPoint server = new(IPAddress.None, 0); // self-explanatory, i guess
            bool address, file; // checks for setting server address and filename
            TFTPClient client; // self-explanatory, i guess

            address = file = false;

            if (args.Count == 0)
            {
                Console.WriteLine("No arguments were provided. Use -h (or --help) to get help.");
                return;
            }

            do // work with arguments one-by-one
            {
                switch (args[0])
                {
                    case "-h": case "--help":
                        Console.WriteLine("TFTP command-line client.");
                        Console.WriteLine("Arguments:");
                        Console.WriteLine("\t-h, --help");
                        Console.WriteLine("\t\tDisplays this message.\n");
                        Console.WriteLine("\t-s [ip/hostname](:port), --server [ip/hostname](:port)");
                        Console.WriteLine("\t\tSpecifies server to connect. Required. Default port is 69.\n");
                        Console.WriteLine("\t-g, --get");
                        Console.WriteLine("\t\tGet a file from a server. Required if --put isn't used.\n");
                        Console.WriteLine("\t-p, --put");
                        Console.WriteLine("\t\tPut a file on a server. Required if --get isn't used.\n");
                        Console.WriteLine("\t-f [filename], --file [filename]");
                        Console.WriteLine("\t\tSpecifies file to get/put. Required.\n");
                        return;

                    case "-s": case "--server":
                        if (address) break; // if server address was already set, we skip that

                        var temp = args[1].Split(":");
                        server.Address = Regex.IsMatch(temp[0], @"(?:\d{1,3}\.){3}\d{1,3}") 
                            ? IPAddress.Parse(temp[0]) : Dns.GetHostAddresses(temp[0]).First(); // parsing either an ip or a hostname

                        if (temp.Length == 2) // if we using non-default host
                            server.Port = int.Parse(temp[1]); // set it here

                        address = true;
                        // remove processed argument
                        args.RemoveRange(0, 2);
                        break;

                    case "-f": case "--file":
                        if (file) break; // if filename was already specified, we skip that
                        filename = args[1];
                        file = true;
                        // remove processed argument
                        args.RemoveRange(0, 2);
                        break;

                    case "-g": case "--get":
                        if (address && file) // if we set both flags
                        { // get a connection
                            client = new(server);
                            client.GetFile(filename);
                            // remove processed argument
                            args.RemoveAt(0);
                        }
                        else // otherwise
                        { // put that argument at the end
                            var tmp = args[0];
                            args.RemoveAt(0);
                            args.Add(tmp);
                        }
                        break;

                    case "-p": case "--put":
                        if (address && file) // if we set both flags
                        { // get a connection
                            client = new(server);
                            client.PutFile(filename);
                            // remove processed argument
                            args.RemoveAt(0);
                        }
                        else // otherwise
                        { // put that argument at the end
                            var tmp = args[0];
                            args.RemoveAt(0);
                            args.Add(tmp);
                        }
                        break;
                }
                
            } while (args.Count != 0);
        }
    }
}
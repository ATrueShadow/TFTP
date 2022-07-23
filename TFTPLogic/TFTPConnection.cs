using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;

namespace TFTP.TFTPLogic
{
    public class TFTPClient
    {
        private const int DefaultPort = 69;
        private IPEndPoint remoteEndPoint;

        public TFTPClient(IPEndPoint remoteEndPoint)
        {
            this.remoteEndPoint = remoteEndPoint;
        }

        public TFTPClient(IPAddress address, int port)
        {
            remoteEndPoint = new(address, port);
        }

        public TFTPClient(IPAddress address)
        {
            remoteEndPoint = new(address, DefaultPort);
        }

        public TFTPClient(string hostname, int port)
        {
            remoteEndPoint = new(Dns.GetHostAddresses(hostname).First(), port);
        }

        public TFTPClient(string hostname)
        {
            remoteEndPoint = new(Dns.GetHostAddresses(hostname).First(), DefaultPort);
        }

        public void GetFile(string filename)
        {
            // check for existing file
            if (File.Exists(Directory.GetCurrentDirectory() + "\\" + filename))
            {
                Console.WriteLine($"File {filename} already exists. Do you want to replace it? (y, n)\n(If transmission won't be successful, file will be corrupted!)");
                switch (Console.ReadLine()?.ToLower())
                {
                    case "y": case "yes": 
                        File.Delete(Directory.GetCurrentDirectory() + "\\" + filename);
                        break;
                    case "n": case "no": return;
                }
            }

            FileStream file = File.Create(Directory.GetCurrentDirectory() + "\\" + filename);

            // opcode for read request
            byte[] RRQ = { 00, 01 };
            // opcode for acknowledgment
            byte[] ACK = { 00, 04 };

            // just a zero. how the fuck do i concat a single byte without this??
            byte[] zero = { 0 };
            byte[] receive; // for receiving data. TODO: Change the name to smth more representing.

            // opening a udp socket
            UdpClient udpClient = new(remoteEndPoint.Port);

            // send out a tftp read request udp packet in format [ Opcode | Filename | 0 | Mode | 0 ]
            // super cringe, kill me
            udpClient.Send(RRQ
                .Concat(Encoding.UTF8.GetBytes(filename))
                .Concat(zero)
                .Concat(Encoding.UTF8.GetBytes("octet"))
                .Concat(zero)
                .ToArray(), remoteEndPoint);

            do
            {
                // receiving data packet
                receive = udpClient.Receive(ref remoteEndPoint);

                // Check for error opcode and handle it if present.
                if (receive[1] == 5)
                {
                    switch (receive[3]) // Error codes taken from RFC 1350, page 10.
                    {
                        case 0:
                            Console.WriteLine($"Undefined error: {Encoding.UTF8.GetString(receive[4..])}");
                            break;
                        case 1:
                            Console.WriteLine($"File {filename} not found or file already being accessed.");
                            break;
                        case 2:
                            Console.WriteLine($"Could not open {filename}: Access violation.");
                            break;
                        case 3:
                            Console.WriteLine("Disk is full.");
                            break;
                        case 4:
                            Console.WriteLine("Illegal TFTP operation.");
                            break;
                        case 5:
                            Console.WriteLine("Unknown transfer ID.");
                            break;
                        case 6: // unlikely to be called in rrq, but whatever
                            Console.WriteLine($"{filename} already exists.");
                            break;
                        case 7: // what.
                            Console.WriteLine("No such user.");
                            break;
                    }

                    file.Close();
                    return;
                }

                // TODO: change that shit from console output to file output.
                // Console.WriteLine(Encoding.UTF8.GetString(receive[4..]));

                // write to a file
                file.Write(receive[4..]);

                // just a simple animation
                Console.SetCursorPosition(0, Console.CursorTop);
                switch (receive[3] % 46)
                {
                    case 0:
                        Console.Write($"Receiving {filename}...  [=     ]");
                        break;
                    case 9:
                        Console.Write($"Receiving {filename}...  [ ==   ]");
                        break;
                    case 18:
                        Console.Write($"Receiving {filename}...  [  ==  ]");
                        break;
                    case 27:
                        Console.Write($"Receiving {filename}...  [   == ]");
                        break;
                    case 36:
                        Console.Write($"Receiving {filename}...  [    ==]");
                        break;
                    case 45:
                        Console.Write($"Receiving {filename}...  [     =]");
                        break;
                }
                

                // sending ack. format: [ Opcode | Block no. ]
                udpClient.Send(ACK
                    .Concat(receive[2..4])
                    .ToArray(), remoteEndPoint);

                    // if length isn't 516 bytes - that transmission is the last, so stop receiving.
            } while (receive.Length == 516);
            Console.WriteLine();

            // safely closing the socket and filestream
            udpClient.Close();
            file.Close();
        }
    }
}
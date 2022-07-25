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
            byte[] data; // for receiving data.

            // opening a udp socket
            UdpClient udpClient = new(remoteEndPoint.Port);

            // send out a tftp read request udp packet in format [ Opcode (2b) | Filename (str) | 0 | Mode (str) | 0 ]
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
                data = udpClient.Receive(ref remoteEndPoint);

                // Check for error opcode and handle it if present.
                if (data[1] == 5)
                {
                    switch (data[3]) // Error codes taken from RFC 1350, page 10.
                    {
                        case 0:
                            Console.WriteLine($"Undefined error: {Encoding.UTF8.GetString(data[4..])}");
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

                // write to a file
                file.Write(data);

                // just a simple animation
                Console.SetCursorPosition(0, Console.CursorTop);
                switch (data[3] % 46)
                {
                    case 0:
                        Console.Write($"Receiving {filename}...  [==    ]");
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
                        Console.Write($"Receiving {filename}...  [      ]");
                        break;
                }
                
                // sending ack. format: [ Opcode (2b) | Block no. (2b) ]
                udpClient.Send(ACK
                    .Concat(data[2..4])
                    .ToArray(), remoteEndPoint);

                    // if length isn't 516 bytes - that transmission is the last, so stop receiving.
            } while (data.Length == 516);
            Console.WriteLine();

            // safely closing the socket and filestream
            udpClient.Close();
            file.Close();
        }

        public void PutFile(string filename)
        {
            // checking for a file
            if (!File.Exists(Directory.GetCurrentDirectory() + "\\" + filename))
            {
                Console.WriteLine($"{filename} not found. Aborted.");
                return;
            }

            // opening a file
            // FileStream file = File.OpenRead(Directory.GetCurrentDirectory() + "\\" + filename);
            BinaryReader file = new(File.OpenRead(Directory.GetCurrentDirectory() + "\\" + filename));

            // opcode for write request
            byte[] WRQ = { 00, 02 };
            // opcode for data
            byte[] DATA = { 00, 03 };

            // zero. again.
            byte[] zero = { 0 };
            byte[] data = new byte[516]; // for sending data.
            byte[] reply; // for replies from the server
            byte[] blockNo = { 00, 00 }; // keeping number of blocks sent
            int readBytes = 0; // keeping track of how much bytes are sent

            // opening a udp socket
            UdpClient udpClient = new(remoteEndPoint.Port);

            // send out a tftp write packet. format: [ Opcode (2b) | Filename (str) | 0 | Mode (str) | 0 ]
            // here we go again. ugh
            udpClient.Send(WRQ
                .Concat(Encoding.UTF8.GetBytes(filename))
                .Concat(zero)
                .Concat(Encoding.UTF8.GetBytes("octet"))
                .Concat(zero)
                .ToArray(), remoteEndPoint);

            do
            {
                // receiving reply from the server
                reply = udpClient.Receive(ref remoteEndPoint);

                // Check for error opcode and handle it if present.
                if (reply[1] == 5)
                {
                    switch (reply[3]) // Error codes taken from RFC 1350, page 10.
                    {
                        case 0:
                            Console.WriteLine($"Undefined error: {Encoding.UTF8.GetString(reply[4..])}");
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
                        case 6:
                            Console.WriteLine($"{filename} already exists.");
                            break;
                        case 7: // what.
                            Console.WriteLine("No such user.");
                            break;
                    }

                    udpClient.Close();
                    file.Close();
                    return;
                } 
                
                if (reply[1] == 4) // checking if the server sent their ack
                {
                    //if (reply[2..3].Equals(blockNo))
                    //{
                        // counting up number of blocks
                        // ughhhhh.
                        blockNo[1]++;
                        if (blockNo[1] == byte.MinValue)
                            blockNo[0]++;
                    //}
                    //else // rewind that shit if server isn't ready
                    //{
                    //    file.Position -= readBytes;
                    //}

                    // reading the file (max 516 bytes)
                    // TODO: why this shit skips 4 bytes every time a new block starts???? but wireshark shows everything??? tf??
                    data = file.ReadBytes(516);
                    
                    // sending data packet. format: [ Opcode (2b) | Block No. (2b) ]
                    // oh wow, that shit again
                    udpClient.Send(DATA
                        .Concat(blockNo)
                        .Concat(data)
                        .ToArray(), remoteEndPoint);
                }

                    // if we send less than 516 bytes of data - that's the last transmission
            } while (data.Length == 516);

            // safely closing the socket and file
            udpClient.Close();
            file.Close();
        }
    }
}
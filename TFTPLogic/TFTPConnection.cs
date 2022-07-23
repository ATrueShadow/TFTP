using System.Net;
using System.Net.Sockets;
using System.Text;

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

                // TODO: change that shit from console output to file output. Also, error handling.
                Console.WriteLine(Encoding.UTF8.GetString(receive[4..]));

                // sending ack. format: [ Opcode | Block no. ]
                udpClient.Send(ACK
                    .Concat(receive[2..4])
                    .ToArray(), remoteEndPoint);

                    // if length isn't 516 bytes - that transmission is last, so stop receiving.
            } while (receive[1] == 3 && receive.Length == 516);

            // safely closing the socket
            udpClient.Close();
        }
    }
}
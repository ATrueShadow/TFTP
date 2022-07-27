using TFTP.TFTPLogic;
using System.Net;

namespace TFTP.TFTPClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            // test
            TFTPLogic.TFTPClient client = new(IPAddress.Parse("192.168.1.20"), 1069);
            // client.GetFile("linuxmint-20.3-mate-64bit.iso");
            client.PutFile("pepeEnough.jpg");
        }
    }
}
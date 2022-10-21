using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using UnityEngine.UI;

public class ClientTCP : MonoBehaviour
{
    Socket clientSocket;
    Thread listenThread;
    public void ConnectToServer()
    {
        Debug.Log("attempt connect");
        clientSocket = new Socket(AddressFamily.InterNetwork,
            SocketType.Stream, ProtocolType.Tcp);
        IPEndPoint ipep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8008);
        clientSocket.Connect(ipep);
    }
    public void SendM()
    {
        byte[] data = new byte[32];
        data = Encoding.ASCII.GetBytes("pito");
        IPEndPoint ipep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8008);
        clientSocket.SendTo(data, data.Length, SocketFlags.None, ipep);
    }
}

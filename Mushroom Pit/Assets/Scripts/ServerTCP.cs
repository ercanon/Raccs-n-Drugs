using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Text;
using UnityEngine.UI;

public class ServerTCP : MonoBehaviour
{
    Socket serverSocket;
    Thread listenThread;
    Thread receiveThread;
    List<Socket> clients;
    Socket clientSocket;
    //
    public InputField ServerIP;
    public InputField Port;
    public InputField Message;
    public Text ChatBox;
    public void OpenServer()
    {
        serverSocket = new Socket(AddressFamily.InterNetwork,
            SocketType.Stream, ProtocolType.Tcp);
		IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(ServerIP.text ?? "127.0.0.1"), int.Parse(Port.text ?? "8008"));
        serverSocket.Bind(ipep);
        listenThread = new Thread(Listening);
        listenThread.Start();
		ChatBox.text += "server opened and listening at (" + ipep.Address + ")[" + ipep.Port + "]";
    }
    void Listening()
    {
        serverSocket.Listen(1);
        Socket newClient = serverSocket.Accept();
		ChatBox.text += "client deceived {" + newClient.RemoteEndPoint + "}";
        clientSocket = newClient;
        receiveThread = new Thread(Receiving);
        receiveThread.Start();
    }
    void Receiving()
    {
        byte[] data = new byte[32];
        clientSocket.Receive(data);
        Debug.Log(Encoding.ASCII.GetString(data));
    }
}

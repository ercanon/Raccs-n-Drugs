using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class ServerUDP : MonoBehaviour
{
    int recv;
    byte[] data;
    IPEndPoint ipep;
    IPEndPoint sender;
    Socket server;
    EndPoint remote;

    // Start is called before the first frame update
    void Start()
    {
        Thread thread = new Thread(Connection);

        data = new byte[1024];
        ipep = new IPEndPoint(IPAddress.Any, 9050);
        sender = new IPEndPoint(IPAddress.Any, 0);
        server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        server.Bind(ipep);
        Debug.Log("Waiting for a client...");
        remote = (EndPoint)(sender);

        thread.Start();
    }

    // Update is called once per frame
    public void Connection()
    {
        recv = server.ReceiveFrom(data, ref remote);

        Debug.Log("Message received from " + remote.ToString() + ":");
        Debug.Log(Encoding.ASCII.GetString(data, 0, recv));

        DataTestMessage();
        server.SendTo(data, data.Length, SocketFlags.None, remote);

        while(true)
        {
            data = new byte[1024];
            recv = server.ReceiveFrom(data, ref remote);

            Debug.Log(Encoding.ASCII.GetString(data, 0, recv));
            server.SendTo(data, data.Length, SocketFlags.None, remote);
        }
    }

    void DataTestMessage()
    {
        data = Encoding.ASCII.GetBytes("Test working on Server.");
    }
}

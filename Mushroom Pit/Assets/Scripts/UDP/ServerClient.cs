using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class ServerClient : MonoBehaviour
{
    int recv;
    byte[] data ;
    IPEndPoint ipep;
    IPEndPoint sender;
    EndPoint remote;
    Thread thread;
    Thread threadClient;

    // Server
    Socket newsock;

    // Client
    Socket server;

    // temporal
    string input, stringData;

    public void Server()
    {
        data = new byte[1024];
        ipep = new IPEndPoint(IPAddress.Any, 8194);
        newsock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        newsock.Bind(ipep);
        Debug.Log("Waiting for a client...");

        sender = new IPEndPoint(IPAddress.Any, 0);
        remote = (EndPoint)(sender);

        thread = new Thread(WaitingToWork);
        thread.Start();
    }

    void WaitingToWork()
    {
        recv = newsock.ReceiveFrom(data, ref remote);

        Debug.Log("Message received from {0}: " + remote.ToString());
        Debug.Log(Encoding.ASCII.GetString(data, 0, recv));
        
        string welcome = "Welcome to my test server";
        data = Encoding.ASCII.GetBytes(welcome);
        newsock.SendTo(data, data.Length, SocketFlags.None, remote);
        while (true)
        {
            data = new byte[1024];
            recv = newsock.ReceiveFrom(data, ref remote);
        
            Debug.Log(Encoding.ASCII.GetString(data, 0, recv));
            newsock.SendTo(data, recv, SocketFlags.None, remote);
        }
    }

    public void Client()
    {
        data = new byte[1024];
        ipep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8194);
        server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        string welcome = "Hello, are you there?";
        data = Encoding.ASCII.GetBytes(welcome);
        server.SendTo(data, data.Length, SocketFlags.None, ipep);

        threadClient = new Thread(ClientHear);
        threadClient.Start();
    }

    void ClientHear()
    {
        sender = new IPEndPoint(IPAddress.Any, 0);
        remote = (EndPoint)sender;

        data = new byte[1024];
        int recv = server.ReceiveFrom(data, ref remote);

        Debug.Log("Message received from {0}: " + remote.ToString());
        Debug.Log(Encoding.ASCII.GetString(data, 0, recv));

        while (true)
        {
            input = Console.ReadLine();
            if (input == "exit")
                break;
            server.SendTo(Encoding.ASCII.GetBytes(input), remote);
            data = new byte[1024];
            recv = server.ReceiveFrom(data, ref remote);
            stringData = Encoding.ASCII.GetString(data, 0, recv);
            Debug.Log(stringData);
        }
        Debug.Log("Stopping client");
        server.Close();
    }
}

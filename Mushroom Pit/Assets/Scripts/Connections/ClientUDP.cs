using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class ClientUDP : MonoBehaviour
{
    Socket server;
    int recv;
    byte[] data;
    EndPoint remote;
    IPEndPoint sender;
    IPEndPoint ipep;
    string input, stringData;
    Thread thread;

    // Start is called before the first frame update
    void Start()
    {
        thread = new Thread(Receive);
    }

    public void StartUDP()
    {
        data = new byte[1024];
        ipep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9050); // Provisional el parse.

        server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        server.SendTo(data, data.Length, SocketFlags.None, ipep);

        sender = new IPEndPoint(IPAddress.Any, 0);
        remote = (EndPoint)sender;

        data = new byte[1024];
        recv = server.ReceiveFrom(data, ref remote);

        Debug.Log("Message received from " + remote.ToString() + ":");
        Debug.Log(Encoding.ASCII.GetString(data, 0, recv));

    }

    public void Receive()
    {
        while(true)
        {
            input = Console.ReadLine(); //idk no se lo que hace
            if (input == "exit") break;
            server.SendTo(Encoding.ASCII.GetBytes(input), remote);
            data = new byte[1024];
            recv = server.ReceiveFrom(data, ref remote);
            stringData = Encoding.ASCII.GetString(data, 0, recv);
            Debug.Log(stringData);
        }
    }

    void DataTestMessage()
    {
        data = Encoding.ASCII.GetBytes("Test working on Client.");
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("Stopping client");
            server.Close();
            Application.Quit();
        }
    }
}

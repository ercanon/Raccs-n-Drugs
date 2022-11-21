using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine.UI;
using System.IO;

public class ServerClient : MonoBehaviour
{
    int recv;
    byte[] data;
    IPEndPoint ipep;
    IPEndPoint sender;
    EndPoint remote;
    Thread thread;
    Thread threadClient;

    // Serializer
    static MemoryStream stream;
    bool connectedS, loggedC;
    float x, y, z, dx, dy, dz;

    // Canvas Connection
    enum Profile { server, client }; Profile profile;
    public InputField userName, serverIP, enterMessage;
    public Text serverPort, chatBox;
    string textSend;
    public Button startB, createB, joinB, disconnectB;

    // Server
    Socket newsock;
    private Vector3 newPosRaccoon;
    private Vector3 newPosRaccoon2;
    public bool posChanged = false;
    public GameObject Racoon2;

    // Client
    Socket server;

    // SerializeContent
    [SerializeField] bool startBool;
    Transform racoon;
    Transform racoonc;

    private void Awake()
    {
        serverIP.interactable = false;

        //racoon = GameObject.FindGameObjectsWithTag("Player")[0].GetComponent<Transform>();
        //racoonc = GameObject.FindGameObjectsWithTag("Player")[1].GetComponent<Transform>();
    }

    public void ChangeProfile(int prof)
    {
        profile = (Profile)prof;

        if (profile == Profile.server)
        {
            serverIP.interactable = false;
            startB.interactable = true;
            createB.interactable = true;
            joinB.interactable = false;
            disconnectB.interactable = false;
        }
        else if (profile == Profile.client)
        {
            serverIP.interactable = true;
            startB.interactable = false;
            createB.interactable = false;
            joinB.interactable = true;
            disconnectB.interactable = true;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return)) textSend = enterMessage.text;

        if ((connectedS || loggedC))
        {
            Serialize();
        }

        if (startBool)
        {
            LaunchGame();
        }

        if (textSend != "")
        {
            chatBox.text += textSend + "\n";
            textSend = "";
        }

        if (profile == Profile.client && posChanged)
        {
            GameObject.FindGameObjectsWithTag("Player")[0].GetComponent<RacoonBehaviour>().enabled = false;
            GameObject.FindGameObjectsWithTag("Player")[0].transform.position = newPosRaccoon;
            posChanged = false;

        }

        if (profile == Profile.server && posChanged)
        {
            GameObject.FindGameObjectsWithTag("Player")[1].GetComponent<RacoonBehaviour>().enabled = false;
            GameObject.FindGameObjectsWithTag("Player")[1].transform.position = newPosRaccoon;
            posChanged = false;
        }
    }

    /*----- HOST/SERVER -----*/

    public void Server()
    {
        data = new byte[1024];
        ipep = new IPEndPoint(IPAddress.Any, int.Parse(serverPort.text));
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

        connectedS = true;

        Debug.Log("Message received from {0}: " + remote.ToString());
        Debug.Log(Encoding.ASCII.GetString(data, 0, recv));
        
        string welcome = "Welcome to my test server";
        data = Encoding.ASCII.GetBytes(welcome);
        newsock.SendTo(data, data.Length, SocketFlags.None, remote);

        while (true)
        {
            data = new byte[1024];
            recv = newsock.ReceiveFrom(data, ref remote);

            Deserialize();
        }
    }

    /*----- CLIENT -----*/

    public void Client()
    {
        data = new byte[1024];
        ipep = new IPEndPoint(IPAddress.Parse(serverIP.text), int.Parse(serverPort.text));
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

        loggedC = true;

        while (true)
        {
            data = new byte[1024];
            recv = server.ReceiveFrom(data, ref remote);

            Deserialize();
        }
    }

    /*----- FUNCTIONS -----*/
    void Serialize()
    {
        stream = new MemoryStream();
        BinaryWriter write = new BinaryWriter(stream);

        write.Write(startBool);
        Vector3 xd;
        if (profile == Profile.server)
            xd = GameObject.FindGameObjectsWithTag("Player")[0].transform.position;
        else
            xd = GameObject.FindGameObjectsWithTag("Player")[1].transform.position;
        write.Write(xd.x);
        write.Write(xd.y);
        write.Write(xd.z);

       // if (connectedS) newsock.SendTo(stream.ToArray(), stream.ToArray().Length, SocketFlags.None, remote);
        server.SendTo(stream.ToArray(), stream.ToArray().Length, SocketFlags.None, remote);
    }

    void Deserialize()
    {
        stream = new MemoryStream(data);
        BinaryReader reader = new BinaryReader(stream);
        stream.Seek(0, SeekOrigin.Begin);
        Debug.Log("this is boolean state: "+startBool);
        startBool = reader.ReadBoolean();
        x = reader.ReadSingle();
        y = reader.ReadSingle();
        z = reader.ReadSingle();
        newPosRaccoon = new Vector3((float)x, (float)y, (float)z);
        posChanged = true;

    }

    /*----- FUNCTIONS -----*/
    public void StartB() { startBool = true; }

    void LaunchGame()
    {
        GameObject.Find("Level").GetComponent<GameplayScript>().enabled = true;
        GameObject.Find("UI").SetActive(false);
        //startBool = false;
    }
}

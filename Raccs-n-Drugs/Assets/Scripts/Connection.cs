using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class Connection : MonoBehaviour
{
	/*---------------------VARIABLES-------------------*/
	enum Protocol { UDP, TCP }; private Protocol protocol;
	enum Profile { server, client }; private Profile profile;
	enum TypeData { start, posList, chat, cocainePositions, raccsTransform, raccsCharge };
	
	private Socket socketHost;
	private Socket socket;
	private Thread ServerWaiting;
	private Thread ServerGather;
	private Thread ClientListen;

	private EndPoint remote;
	private List<EndPoint> clients;
	private List<byte[]> pendingData;
	private string log;

	public GameObject menuSong;
	public GameObject gameplaySong;

	[SerializeField] private GameplayScript gameplay;

	[Space]
	[Header("Text Dynamics")]
	[SerializeField] private InputField enterUserName;
	[SerializeField] private InputField serverIPInput;
	[SerializeField] private InputField enterServerPort;
	[SerializeField] private InputField enterMessage;
	[SerializeField] private Text ChatBox;
	[SerializeField] private GameObject tutorialWindow;
	[Space]
	[Header("Buttons Dynamics")]
	[SerializeField] private Button startGameButton;
	[SerializeField] private Button createGameButton;
	[SerializeField] private Button joinGameButton;
	[SerializeField] private Button disconnectButton;



	/*---------------------CONFIG-------------------*/
	void Reset(int prot, int prof)
	{
		if (socket != null)
		{
			if (remote != null)
			{
				socket.Shutdown(SocketShutdown.Both);
				socket.Close();
			}
			socket = null;
		}

		if (pendingData != null)
			pendingData.Clear();
		pendingData = new List<byte[]>();

		if (ClientListen != null)
		{
			ClientListen.Abort();
			ClientListen = null;
		}

		if (profile == Profile.server)
		{
			if (socketHost != null)
			{
				socketHost.Shutdown(SocketShutdown.Both);
				socketHost.Close();
				socketHost = null;

				startGameButton.interactable = false;
			}

			if (clients != null)
				clients.Clear();
			clients = new List<EndPoint>();

			if (ServerGather != null)
			{
				if (protocol == Protocol.TCP)
				{
					ServerWaiting.Abort();
					ServerWaiting = null;
				}

				ServerGather.Abort();
				ServerGather = null;
			}
		}

		log = null;
		ChatBox.text = null;
		remote = null;
		enterServerPort.text = "";
		serverIPInput.text = "";


		gameplay.Reset();

		protocol = (Protocol)prot;
		profile = (Profile)prof;
	}
	
	void Awake()
	{
		enterUserName.text = "Player" + (int)Random.Range(1, 100);
		gameplay.connect = this;
		Reset((int)protocol, (int)profile);
	}
	
	public void ChangeProtocol(int val)
	{
		Reset(val, (int)profile);
	}
	
	public void ChangeProfile(int val)
	{
		Reset((int)protocol, val);

		switch (profile)
		{
			case Profile.client:
				createGameButton.interactable = false;
				joinGameButton.interactable = true;
				disconnectButton.interactable = true;
				serverIPInput.interactable = true;
				break;
			case Profile.server:
				createGameButton.interactable = true;
				joinGameButton.interactable = false;
				disconnectButton.interactable = false;
				serverIPInput.interactable = false;
				break;
			default:
				break;
		}
	}

	private string TellIP()
	{
		foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
		{
			if (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
			{
				foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
				{
					if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
					{
						return ip.Address.ToString();
					}
				}
			}
		}
		return null;
	}

	//Under Development
	private string SearchServers()
	{
		foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
			foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
				{
				}

		return null;
	}



	/*---------------------TEXT-------------------*/
	void Update()
	{
		if (log != null) { ChatBox.text += "\n"; ChatBox.text += log; log = null; } // ChatBox.text += "\n" -> Made for a jump before the message. Only happen 1 time.
		
		if (Input.GetKeyDown(KeyCode.Return))
		{
			SendMessage();
			enterMessage.text = "";
		}

		while(pendingData.Count > 0)
		{
			Deserialize(pendingData[0]);
			pendingData.RemoveAt(0);
		}
	}
	
	void customLog(string data, string sender, bool nl = true)
	{
		string message = data.TrimEnd('\0');
		log += "[" + sender + "] ";
		log += message; 
		if (nl) log += '\n';
	}



	/*---------------------HOST-------------------*/
	public void CreateGame()
	{
		if (socketHost != null)
        {
			customLog("Cannot create another server!", "Error");
			return;
		}

		if (protocol == Protocol.TCP)
			socketHost = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		else if (protocol == Protocol.UDP)
			socketHost = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

		if (enterServerPort.text == "")
			enterServerPort.text = 666.ToString();

		IPEndPoint ipep = new IPEndPoint(IPAddress.Any, int.Parse(enterServerPort.text));
		socketHost.Bind(ipep);

		if (protocol == Protocol.TCP)
		{
			ServerWaiting = new Thread(WaitingClients);
			ServerWaiting.Start();

			socketHost.Listen(4);
		}

		customLog(enterUserName.text + "'s game available at " + TellIP(), "Server");
		startGameButton.interactable = true;

		ServerGather = new Thread(GatherAndBroadcast);
		ServerGather.Start();

		serverIPInput.text = "127.0.0.1";
		JoinGame();
	}

	void WaitingClients()
	{
		try
		{
			Socket pendingClient = socketHost.Accept();
			clients.Add(pendingClient.RemoteEndPoint);
		}
		catch (SocketException e)
		{
			customLog(e.ToString(), "Error");
		}
	}

	void GatherAndBroadcast()
	{
		while (true)
		{
			byte[] data = new byte[1024];
			int recv = 0;

			if (protocol == Protocol.TCP)
			{
				if (socketHost.Connected && socketHost.IsBound)
				{
					socketHost.Receive(data);
					socketHost.Send(data);
				}
			}
			else if (protocol == Protocol.UDP)
			{
				EndPoint sender = new IPEndPoint(IPAddress.Any, 0);
				recv = socketHost.ReceiveFrom(data, ref sender);

				if (!clients.Contains(sender))
				{
					clients.Add(sender);
					SendData(Serialize((int)TypeData.posList), socketHost, sender);
					socketHost.SendTo(data, recv, SocketFlags.None, sender);
				}

				foreach (EndPoint reciber in clients)
				{
					if (sender.ToString() != reciber.ToString())
						socketHost.SendTo(data, recv, SocketFlags.None, reciber);
				}
			}
		}
	}

	void Broadcast(TypeData tData)
	{
		byte[] data = new byte[1024];
		data = Serialize((int)tData, enterMessage.text, enterUserName.text);

		if (protocol == Protocol.TCP)
			socketHost.Send(data);
		else if (protocol == Protocol.UDP)
			foreach (EndPoint reciber in clients)
				socketHost.SendTo(data, data.Length, SocketFlags.None, reciber);
	}



	/*---------------------CLIENT-------------------*/
	public void JoinGame()
	{
		if (remote != null)
		{
			customLog("You have already joined!", "Error");
			return;
		}

		if (serverIPInput.text == "")
		{
			customLog("System needs a Server IP to join the game!", "Error");
			return;
		}

		if (socket == null)
		{
			if (protocol == Protocol.TCP)
				socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			else if (protocol == Protocol.UDP)
				socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		}

		if (enterServerPort.text == "")
			enterServerPort.text = 666.ToString();
			
		remote = new IPEndPoint(IPAddress.Parse(serverIPInput.text), int.Parse(enterServerPort.text));

		try
		{
			if (protocol == Protocol.TCP)
				socket.Connect(remote);

			SendData(Serialize((int)TypeData.chat, enterUserName.text + " joined the server!", "Server"), socket, remote);
		}
		catch (SocketException e)
		{
			customLog(e.Message, "Error");
			remote = null;
			return;
		}

		ClientListen = new Thread(Listen);
		ClientListen.Start();
	}
	
	void Listen()
	{
		while (true)
		{
			byte[] data = new byte[1024];
			if (protocol == Protocol.TCP)
				socket.Receive(data);
			else if (protocol == Protocol.UDP)
				socket.ReceiveFrom(data, ref remote);

			if (data != null)
				pendingData.Add(data);
		}
	}

	public void Disconnect()
	{
		Reset((int)protocol, (int)profile);
	}

	public void SendMessage()
	{
		SendData(Serialize((int)TypeData.chat, enterMessage.text, enterUserName.text), socket, remote);
		customLog(enterMessage.text, enterUserName.text);
	}



	/*---------------------SERIALIZATION-------------------*/
	private byte[] Serialize(int type, string message = "", string sender = "")
	{
		MemoryStream stream = new MemoryStream();
		BinaryWriter writer = new BinaryWriter(stream);

		writer.Write(type);
		switch (type)
		{
			case 0:	//LaunchGame
				writer.Write(clients.Count);
				break;
			case 1: //Position List Racoon
				writer.Write(clients.Count-1);
				break;
			case 2:	//Chat
				writer.Write(message);
				writer.Write(sender);
				break;
			case 3: //CocainePosition	
				writer.Write(gameplay.cocaineList.Count);
				for (int i = 0; i < gameplay.cocaineList.Count; i++)
				{
					Vector3 pos = gameplay.cocaineList[i].transform.position;
					writer.Write(pos.x);
					writer.Write(pos.y);
					writer.Write(pos.z);
				}
				break;
			case 4: //raccsTransform
				int rListPos = gameplay.posRaccList;
				if (rListPos < 4)
				{
					Vector3 pos = gameplay.raccsList[rListPos].transform.position;
					writer.Write(pos.x);
					writer.Write(pos.y);
					writer.Write(pos.z);

					Vector3 rot = gameplay.raccsList[rListPos].transform.rotation.eulerAngles;
					writer.Write(rot.x);
					writer.Write(rot.y);
					writer.Write(rot.z);

					writer.Write(rListPos);
				}
				break;
			case 5: //RaccsCharge
				writer.Write(gameplay.posRaccList);
				break;
			default:
				return null;
		}

		return stream.ToArray();
	}

	private void Deserialize(byte[] data)
	{
		MemoryStream stream = new MemoryStream(data);
		BinaryReader reader = new BinaryReader(stream);
		stream.Seek(0, SeekOrigin.Begin);

		int type = reader.ReadInt32();
		switch (type)
		{
			case 0: //LaunchGame
				gameplay.LaunchGame(reader.ReadInt32());
				break;
			case 1:	//Position List Racoon
				gameplay.posRaccList = reader.ReadInt32();
				break;
			case 2:	//Chat
				customLog(reader.ReadString(), reader.ReadString());
				break;
			case 3: //CocainePosition
				int maxCocaine = reader.ReadInt32();
				for (int i = 0; i < maxCocaine; i++)
				{
					float xC = reader.ReadSingle();
					float yC = reader.ReadSingle();
					float zC = reader.ReadSingle();

					gameplay.UpdateCocaine(new Vector3(xC, yC, zC), maxCocaine, i == 0 ? true : false);
				}
				break;
			case 4: //raccsTransform
				float xRP = reader.ReadSingle();
				float yRP = reader.ReadSingle();
				float zRP = reader.ReadSingle();

				float xRR = reader.ReadSingle();
				float yRR = reader.ReadSingle();
				float zRR = reader.ReadSingle();

				gameplay.UpdateRacoon(new Vector3(xRP, yRP, zRP), new Vector3(xRR, yRR, zRR), reader.ReadInt32());
				break;
			case 5: //RaccsCharge
				gameplay.ChargeRacoon(reader.ReadInt32());
				break;
			default:
				customLog("Package is corrupted", "Error");
				break;
		}
	}

	private int SendData(byte[] data, Socket sender, EndPoint receiver)
	{
		if (protocol == Protocol.TCP)
			 return sender.Send(data);
		else if (protocol == Protocol.UDP)
			return sender.SendTo(data, data.Length, SocketFlags.None, receiver);
		return 0;
	}

	public int SendClientData(int type)
	{
		byte[] data = Serialize(type);
		return SendData(data, socket, remote);
	}



	/*---------------------GAME-------------------*/
	public void LaunchGame()
    {
		SendClientData((int)TypeData.start);
		gameplay.LaunchGame(clients.Count);
		menuSong.SetActive(false);
		gameplaySong.SetActive(true);
	}

	public void OpenInfo()
	{
		bool set = tutorialWindow.activeSelf;
		enterMessage.interactable = set;
		tutorialWindow.SetActive(set != true);
	}
}

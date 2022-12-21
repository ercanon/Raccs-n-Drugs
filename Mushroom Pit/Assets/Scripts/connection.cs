using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Text;
using UnityEngine.UI;
using System.Net.NetworkInformation;
using System.IO;

public class connection : MonoBehaviour
{
	/*---------------------VARIABLES-------------------*/
	enum Protocol { UDP, TCP }; private Protocol protocol;
	enum Profile { server, client }; private Profile profile;
	enum TypeData { start, posList, chat, raccsPositions, CocainePositions }; private TypeData typeData;
	
	Socket socketHost;
	Socket socket;
	Thread ServerWaiting;
	Thread ServerGather;
	Thread ClientListen;

	[HideInInspector]
	EndPoint remote;
	//Dictionary<EndPoint, Socket> clients;
	List<EndPoint> clients;
	List<byte[]> pendingData;

	public InputField enterUserName;
	public Text enterServerIP;
	public Text enterServerPort;
	public Text ChatBox;
	public InputField enterMessage;
	string log;
	private GameplayScript gameplay;



	/*---------------------CONFIG-------------------*/
	void Reset(int prot, int prof)
	{
		protocol = (Protocol)prot;
		profile = (Profile)prof;

		if (socketHost != null)
		{
			socketHost.Shutdown(SocketShutdown.Both);
			socketHost.Close();
		}
		if (socket != null)
		{
			socket.Shutdown(SocketShutdown.Both);
			socket.Close();
		}

		if (clients != null)
			clients.Clear();
		clients = new List<EndPoint>();

		if (pendingData != null)
			pendingData.Clear();
		pendingData = new List<byte[]>();

		log = null;
		remote = (new IPEndPoint(IPAddress.Any, 0));

		gameplay.Reset();
	}
	
	void Awake()
	{
		enterUserName.text = "Player" + (int)Random.Range(1, 100);
		gameplay = GameObject.Find("Level").GetComponent<GameplayScript>();
		gameplay.conect = this;

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
				GameObject.Find("StartGame").GetComponent<Button>().interactable = false;
				GameObject.Find("CreateGame").GetComponent<Button>().interactable = false;
				GameObject.Find("JoinGame").GetComponent<Button>().interactable = true;
				GameObject.Find("Disconnect").GetComponent<Button>().interactable = true;
				GameObject.Find("ServerIP").GetComponent<InputField>().interactable = true;
				break;
			case Profile.server:
				GameObject.Find("StartGame").GetComponent<Button>().interactable = true;
				GameObject.Find("CreateGame").GetComponent<Button>().interactable = true;
				GameObject.Find("JoinGame").GetComponent<Button>().interactable = false;
				GameObject.Find("Disconnect").GetComponent<Button>().interactable = false;
				GameObject.Find("ServerIP").GetComponent<InputField>().interactable = false;
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
		if (protocol == Protocol.TCP)
			socketHost = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		else if (protocol == Protocol.UDP)
			socketHost = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

		IPEndPoint ipep = new IPEndPoint(IPAddress.Any, int.Parse(enterServerPort.text));
		socketHost.Bind(ipep);
		customLog(enterUserName.text + "'s game available at " + TellIP(), "Server");

		if (protocol == Protocol.TCP)
		{
			ServerWaiting = new Thread(WaitingPlayers);
			ServerWaiting.Start();
		}

		ServerGather = new Thread(GatherAndBroadcast);
		ServerGather.Start();

		JoinGame(true);
	}

	void WaitingPlayers()
	{
		socketHost.Listen(1);
		Socket newClient = socketHost.Accept();
		customLog("client deceived " + newClient.RemoteEndPoint.ToString(), "Server");
	}

	void GatherAndBroadcast()
	{		
		while (true)
		{
			byte[] data = new byte[1024];
			int recv = 0;

			if (protocol == Protocol.TCP)
			{
				recv = socketHost.Receive(data);
				if (recv != 0) 
					socketHost.Send(data);
			}
			else if (protocol == Protocol.UDP)
			{
				EndPoint sender = new IPEndPoint(IPAddress.Any, 0);
				recv = socketHost.ReceiveFrom(data, ref sender);
				if (clients.Contains(sender) == false)
				{
					clients.Add(sender);
					SendData(Serialize((int)TypeData.posList), socketHost, sender);
					customLog("client deceived " + sender.ToString(), "Server");
					socketHost.SendTo(data, recv, SocketFlags.None, sender);
				}

				foreach (EndPoint reciber in clients)
				{
					if (sender.ToString() != reciber.ToString())
						socketHost.SendTo(data, recv, SocketFlags.None, reciber);
				}
			}



			/*
			foreach (var r in clients)
			{
				byte[] data = new byte[1024];
				int recv = 0;
				if (protocol == Protocol.TCP && r.Value != null)
					recv = socketHost.Receive(data);
				else if (protocol == Protocol.UDP)
				{
					EndPoint client = r.Key;
					recv = socketHost.ReceiveFrom(data, ref client);
				}

				if (recv == 0)
				{
					customLog("client" + r.Key.ToString() + "disconnected", "Server");
					clients.Remove(r.Key);
				}
				else
				{
					foreach (var s in clients)
					{
						if (r.Key != s.Key)
						{
							if (protocol == Protocol.TCP && r.Value != null)
								s.Value.Send(data);
							else if (protocol == Protocol.UDP)
								socketHost.SendTo(data, recv, SocketFlags.None, s.Key);
						}
					}
				}
			}
			*/
		}
	}



	/*---------------------CLIENT-------------------*/
	public void JoinGame(bool isHost = false)
	{
		if (remote.ToString() != new IPEndPoint(IPAddress.Any, 0).ToString())
		{
			customLog("cannot join again", "Local");
			return;
		}

		if (socket == null)
		{
			if (protocol == Protocol.TCP)
				socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			else if (protocol == Protocol.UDP)
				socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		}

		if (isHost)
			remote = new IPEndPoint(IPAddress.Parse("127.0.0.1"), int.Parse(enterServerPort.text));
		else
			remote = new IPEndPoint(IPAddress.Parse(enterServerIP.text), int.Parse(enterServerPort.text));

		try
		{
			if (protocol == Protocol.TCP)
				socket.Connect(remote);
			else if (protocol == Protocol.UDP)
				SendData(Serialize((int)TypeData.chat, enterUserName.text + " joined the server!", "Server"), socket, remote);
		}
		catch (SocketException e)
		{
			customLog(e.Message, "Error");
			remote = (new IPEndPoint(IPAddress.Any, 0));
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



	/*---------------------CHAT-------------------*/
	public void SendMessage()
	{
		SendData(Serialize((int)TypeData.chat, enterMessage.text, enterUserName.text), socket, remote);
		customLog(enterMessage.text, enterUserName.text);

		/*
		//Profile.server:
		if (clients.Count > 0)
			foreach (var r in clients)
			{
				byte[] data = new byte[1024];
				data = Serialize((int)Serial.chat, enterMessage.text, enterUserName.text);

				if (protocol == Protocol.TCP)
					r.Value.Send(data);
				else if (protocol == Protocol.UDP)
					socket.SendTo(data, data.Length, SocketFlags.None, r.Key);
			}

		customLog(enterMessage.text, enterUserName.text);
		*/
	}



	/*---------------------SERIALIZATION-------------------*/
	private byte[] Serialize(int type, string message = "", string sender = "")
	{
		MemoryStream stream = new MemoryStream();
		BinaryWriter writer = new BinaryWriter(stream);

		writer.Write(type);
		switch (type)
		{
			case 0:	//Start
				writer.Write(clients.Count);
				break;
			case 1: //Position List Racoon
				writer.Write(clients.Count-1);
				break;
			case 2:	//Chat
				writer.Write(message);
				writer.Write(sender);
				break;
			case 3: //RaccsPosition
				int rListPos = gameplay.posRacoonList;
				if (rListPos < 4)
				{
					Vector3 pos = gameplay.racoonList[rListPos].transform.position;
					writer.Write(pos.x);
					writer.Write(pos.y);
					writer.Write(pos.z);
					writer.Write(rListPos);
				}
				break;
			case 4: //CocainePosition	
				writer.Write(gameplay.cocaineList.Count);
				for (int i = 0; i < gameplay.cocaineList.Count; i++)
				{
					Vector3 pos = gameplay.cocaineList[i].transform.position;
					writer.Write(pos.x);
					writer.Write(pos.y);
					writer.Write(pos.z);
				}
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
			case 0: //Start
				gameplay.LaunchGame(reader.ReadInt32());
				break;
			case 1:	//Position List Racoon
				gameplay.posRacoonList = reader.ReadInt32();
				break;
			case 2:	//Chat
				customLog(reader.ReadString(), reader.ReadString());
				break;
			case 3: //RaccsPosition
				float xR = reader.ReadSingle();
				float yR = reader.ReadSingle();
				float zR = reader.ReadSingle();

				gameplay.UpdateRacoon(new Vector3(xR, yR, zR), reader.ReadInt32());
				break;
			case 4: //CocainePosition
				int maxCocaine = reader.ReadInt32();
				for (int i = 0; i < maxCocaine; i++)
				{
					float xC = reader.ReadSingle();
					float yC = reader.ReadSingle();
					float zC = reader.ReadSingle();

					gameplay.UpdateCocaine(new Vector3(xC, yC, zC), maxCocaine, i + 1 >= maxCocaine ? true : false);
				}
				break;
			default:
				customLog("No package has been send", "Error");
				break;
		}
	}

	public int SendData(byte[] data, Socket sender, EndPoint receiver)
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

		if (protocol == Protocol.TCP)
			return socket.Send(data);
		else if (protocol == Protocol.UDP)
			return socket.SendTo(data, data.Length, SocketFlags.None, remote);
		return 0;
	}

	/*---------------------GAME-------------------*/
	public void LaunchGame()
    {
		gameplay.LaunchGame(clients.Count);
		SendClientData((int)TypeData.start);
	}
}

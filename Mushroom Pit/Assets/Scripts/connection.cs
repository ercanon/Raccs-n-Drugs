using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Text;
using UnityEngine.UI;
using System.IO;

public class connection : MonoBehaviour
{
	/*---------------------VARIABLES-------------------*/
	enum Protocol { UDP, TCP }; private Protocol protocol;
	enum Profile { server, client }; private Profile profile;
	enum TypeData { start, posList, chat, position }; private TypeData typeData;
	Socket socketHost;
	Socket socket;
	Thread ServerWaiting;
	Thread ServerGather;
	Thread ClientListen;

	//Check for global use
	EndPoint remote;
	[HideInInspector]
	Dictionary<EndPoint, Socket> clients;

	public InputField enterUserName;
	public Text enterServerIP;
	public Text enterServerPort;
	public Text ChatBox;
	public InputField enterMessage;
	string log;
	bool gameStart;

	[HideInInspector]
	public GameplayScript gameManager;

	public GameObject racoon;
	private List<GameObject> racoonList;
	private int posRacoonList;



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
		clients = new Dictionary<EndPoint, Socket>();

		if (racoonList != null)
			racoonList.Clear();
		racoonList = new List<GameObject>();
		posRacoonList = 0;

		log = null;
		remote = (EndPoint)(new IPEndPoint(IPAddress.Any, 0));
	}
	
	void Awake()
	{
		Reset((int)protocol, (int)profile);

		enterUserName.text = "Player" + (int)Random.Range(1, 100000);
		gameManager = null;
		gameStart = false;
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

	public int PosKeyDict(EndPoint key)
    {
		if (clients.ContainsKey(key))
		{
			int i = 0;
			foreach (EndPoint k in clients.Keys)
			{
				if (key == k)
					return i;
				i++;
			}
		}
		return -1;
    }

	public int PosValueDict(Socket value)
	{
		if (clients.ContainsValue(value))
		{
			int i = 0;
			foreach (Socket s in clients.Values)
			{
				if (value == s)
					return i;
				i++;
			}
		}
		return -1;
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

		if(gameStart)
			SendData(Serialize((int)TypeData.position), socket, remote);
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
		customLog(enterUserName.text + "'s game available at " + socketHost.LocalEndPoint, "Server");

		ServerWaiting = new Thread(WaitingPlayers);
		ServerWaiting.Start();

		ServerGather = new Thread(GatherAndBroadcast);
		ServerGather.Start();

		JoinGame(true);
	}
	
	void WaitingPlayers()
	{
		switch (protocol)
		{
			case Protocol.TCP:
				{
					socketHost.Listen(4);
					Socket newClient = socketHost.Accept();
					clients.Add(newClient.RemoteEndPoint, newClient);
					customLog("client deceived " + newClient.RemoteEndPoint.ToString(), "Server");
					break;
				}
			case Protocol.UDP:
				{
					IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
					remote = (EndPoint)(sender);

					byte[] data = new byte[1024];
					int recv = socketHost.ReceiveFrom(data, ref remote);
					clients.Add(remote, null);
					customLog("client deceived " + remote.ToString(), "Server");

					socketHost.SendTo(data, recv, SocketFlags.None, remote);
					break;
				}
			default:
				break;
		}
	}

	void GatherAndBroadcast()
	{		
		while (true)
		{
			if (clients.Count > 0)
			{
				foreach (KeyValuePair<EndPoint, Socket> r in clients)
				{
					byte[] data = new byte[1024];
					int recv = 0;
					if (protocol == Protocol.TCP)
						recv = r.Value.Receive(data);
					else if (protocol == Protocol.UDP)
					{
						EndPoint client = r.Key;
						recv = socketHost.ReceiveFrom(data, ref client);
					}

					if (recv == 0)
					{
						customLog("client disconnected", "Server");
						clients.Remove(r.Key);
					}
					else
					{
						foreach (KeyValuePair<EndPoint, Socket> s in clients)
						{
							if (r.Key != s.Key)
							{
								if (protocol == Protocol.TCP)
									s.Value.Send(data);
								else if (protocol == Protocol.UDP)
									socketHost.SendTo(data, recv, SocketFlags.None, s.Key);
							}
						}
					}
				}
			}
		}
	}



	/*---------------------CLIENT-------------------*/
	public void JoinGame(bool isHost = false)
	{
		if (socket != null)
		{
			customLog("cannot join again", "Local");
			return;
		}

		if (protocol == Protocol.TCP)
			socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		else if (protocol == Protocol.UDP)
			socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

		IPEndPoint ipep = null;
		if (isHost)
			ipep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), int.Parse(enterServerPort.text));
		else
			ipep = new IPEndPoint(IPAddress.Parse(enterServerIP.text), int.Parse(enterServerPort.text));

		if (protocol == Protocol.TCP)
		{
			try
			{
				socket.Connect(ipep);
			}
			catch (SocketException e)
			{
				customLog(e.Message, "Error");
				return;
			}
		}
		else if (protocol == Protocol.UDP)
		{
			IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
			remote = (EndPoint)sender;
		}

		SendData(Serialize((int)TypeData.chat, enterUserName.text + " joined the server!", "Server"), socket, ipep);

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
				Deserialize(data);
		}
	}

	public void Disconnect()
	{
		Reset((int)protocol, (int)profile);
	}



	/*---------------------CHAT-------------------*/
	void SendMessage()
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
				writer.Write(gameStart);
				break;
			case 1: //Position List Racoon
				writer.Write(clients.Keys.Count-1);
				break;
			case 2:	//Chat
				writer.Write(message);
				writer.Write(sender);
				break;
			case 3: //Position
				if (posRacoonList < 4)
				{
					writer.Write(posRacoonList);
					writer.Write(racoonList[posRacoonList].transform.position.x);
					writer.Write(racoonList[posRacoonList].transform.position.y);
					writer.Write(racoonList[posRacoonList].transform.position.z);
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
				if (gameStart == false && reader.ReadBoolean())
					LaunchGame();
				break;
			case 1:	//Position List Racoon
				posRacoonList = reader.ReadInt32();
				break;
			case 2:	//Chat
				customLog(reader.ReadString(), reader.ReadString());
				break;
			case 3: //Position
				int posSend = reader.ReadInt32();
				float x = reader.ReadSingle();
				float y = reader.ReadSingle();
				float z = reader.ReadSingle();

				if (racoonList.Count > 0 && posSend != posRacoonList)
						racoonList[posSend].transform.position = new Vector3(x, y, z);
				break;
		}
	}

	private void SendData(byte[] data, Socket sender, EndPoint receiver)
	{
		if (protocol == Protocol.TCP)
			sender.Send(data);
		else if (protocol == Protocol.UDP)
			sender.SendTo(data, data.Length, SocketFlags.None, receiver);
	}



	/*---------------------GAME-------------------*/
	public void LaunchGame()
	{
		SendData(Serialize((int)TypeData.start), socket, remote);

		GameObject.Find("Level").GetComponent<GameplayScript>().enabled = true;
		GameObject.Find("UI").SetActive(false);
		gameStart = true;

		Transform[] pos = GameObject.Find("RacoonSpawn").GetComponentsInChildren<Transform>();
		for (int i = 0; i <= clients.Count; i++)
		{
			if (i >= 4)
				break;
			GameObject rac = Instantiate(racoon, pos[i + 1].position, pos[i + 1].rotation);
			rac.GetComponent<RacoonBehaviour>().ChangeState(1);
			racoonList.Add(rac);
		}
	}
}

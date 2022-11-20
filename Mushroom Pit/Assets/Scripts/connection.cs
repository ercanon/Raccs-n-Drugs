using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Text;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class connection : MonoBehaviour
{
	enum Protocol { UDP, TCP }; private Protocol protocol;
	enum Profile { server, client }; private Profile profile;
	Socket socketServer;
	Socket socketClient;
	Thread threadServer;
	Thread threadServerR;
	Thread threadClient;

	//Check for global use
	EndPoint remote;
	List<Socket> TCPclients;
	List<EndPoint> UDPclients;

	public InputField enterUserName;
	public Text enterServerIP;
	public Text enterServerPort;
	public Text ChatBox;
	public InputField enterMessage;
	string log;

	void Reset(int prot, int prof)
	{
		protocol = (Protocol)prot;
		profile = (Profile)prof;

		if (socketServer != null)
		{
			socketServer.Shutdown(SocketShutdown.Both);
			socketServer.Close();
		}
		if (socketClient != null)
		{
			socketClient.Shutdown(SocketShutdown.Both);
			socketClient.Close();
		}

		if (TCPclients != null)
			TCPclients.Clear();
		TCPclients = new List<Socket>();

		if (UDPclients != null)
			UDPclients.Clear();
		UDPclients = new List<EndPoint>();

		log = null;
		remote = (EndPoint)(new IPEndPoint(IPAddress.Any, 0));
	}
	void Awake()
	{
		Reset((int)protocol, (int)profile);

		enterUserName.text = "Player" + (int)Random.Range(1, 100000);
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

	/*---------------------TEXT-------------------*/
	void Update()
	{
		if (log != null) { ChatBox.text += "\n"; ChatBox.text += log; log = null; } // ChatBox.text += "\n" -> Made for a jump before the message. Only happen 1 time.

		if (Input.GetKeyDown(KeyCode.Return))
		{
			SendM();
			enterMessage.text = "";
		}

		if (Input.GetKeyDown(KeyCode.F1)) SceneManager.LoadScene(1);
	}
	void customLog(string x, bool nl = true)
	{
		string shrt = x.TrimEnd('\0');
		log += shrt; //if (nl) log += '\n';
	}

	/*---------------------HOST-------------------*/
	public void CreateGame()
	{
		if (protocol == Protocol.TCP)
			socketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		else if (protocol == Protocol.UDP)
			socketServer = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

		IPEndPoint ipep = new IPEndPoint(IPAddress.Any, int.Parse(enterServerPort.text));
		socketServer.Bind(ipep);
		customLog(enterUserName.text + "'s game available at " + socketServer.LocalEndPoint);

		threadServer = new Thread(WaitingPlayers);
		threadServer.Start();

		threadServerR = new Thread(GatherM);
		threadServerR.Start();

		if (protocol == Protocol.UDP)
		{
			IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
			remote = (EndPoint)(sender);
		}
	}
	void WaitingPlayers()
	{
		switch (protocol)
		{
			case Protocol.TCP:
				{
					socketServer.Listen(4);
					Socket newClient = socketServer.Accept();
					TCPclients.Add(newClient);
					customLog("client deceived " + TCPclients[^1].RemoteEndPoint.ToString());
					break;
				}
			case Protocol.UDP:
				{
					byte[] data = new byte[1024];
					int recv = socketServer.ReceiveFrom(data, ref remote);
					UDPclients.Add(remote);
					customLog("client deceived " + remote.ToString());

					socketServer.SendTo(data, recv, SocketFlags.None, remote);
					string msg = Encoding.UTF8.GetString(data, 0, recv);
					customLog(msg);
					break;
				}
			default:
				break;
		}
	}

	void GatherM()
	{
		switch (protocol)
		{
			case Protocol.TCP:
				{
					while (true)
					{
						if (TCPclients.Count > 0)
							foreach (Socket c in TCPclients)
							{
								byte[] data = new byte[1024];
								int recv = c.Receive(data);
								if (recv == 0)
								{
									customLog("client disconnected");
									TCPclients.Remove(c);
								}
								else
								{
									foreach (Socket s in TCPclients)
										c.Send(data);

									string msg = Encoding.UTF8.GetString(data, 0, recv);
									customLog(msg);
								}
							}
					}
					break;
				}
			case Protocol.UDP:
				{
					while (true)
					{
						if (UDPclients.Count > 0)
							foreach (EndPoint c in UDPclients)
							{
								EndPoint client = c;
								byte[] data = new byte[1024];
								int recv = socketServer.ReceiveFrom(data, ref client);
								if (recv == 0)
								{
									customLog("client disconnected");
									UDPclients.Remove(c);
								}
								else
								{
									foreach (EndPoint s in UDPclients)
										socketServer.SendTo(data, recv, SocketFlags.None, s);

									string msg = Encoding.UTF8.GetString(data, 0, recv);
									customLog(msg);
								}
							}
					}
					break;
				}
			default:
				break;
		}
	}

	/*---------------------CLIENT-------------------*/
	public void JoinGame()
	{
		switch (protocol)
		{
			case Protocol.TCP:
				{
					if (socketClient != null)
					{
						customLog("cannot join again");
						return;
					}

					socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
					IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(enterServerIP.text), int.Parse(enterServerPort.text));

					try
					{
						socketClient.Connect(ipep);
					}
					catch (SocketException e)
					{
						customLog(e.Message);
						return;
					}
					byte[] data = new byte[1024];
					data = Encoding.UTF8.GetBytes(enterUserName.text + " joined the server!");
					socketClient.Send(data);

					threadClient = new Thread(HearServer);
					threadClient.Start();
					break;
				}
			case Protocol.UDP:
				{
					if (socketClient != null)
					{
						customLog("cannot join again");
						return;
					}

					socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
					IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(enterServerIP.text), int.Parse(enterServerPort.text));

					byte[] data = new byte[1024];
					data = Encoding.UTF8.GetBytes(enterUserName.text + " joined the server!");
					socketClient.SendTo(data, data.Length, SocketFlags.None, ipep);

					IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
					remote = (EndPoint)sender;

					threadClient = new Thread(HearServer);
					threadClient.Start();
					break;
				}
			default:
				break;
		}
	}
	void HearServer()
	{
		while (true)
		{
			byte[] data = new byte[1024];
			int recv = 0;
			if (protocol == Protocol.TCP)
				recv = socketClient.Receive(data);
			else if (protocol == Protocol.UDP)
				recv = socketClient.ReceiveFrom(data, ref remote);

			string msg = Encoding.UTF8.GetString(data, 0, recv);
			customLog(msg);
		}
	}
	public void Disconnect()
	{
		Reset((int)protocol, (int)profile);
	}

	/*---------------------CHAT-------------------*/
	void SendM()
	{
		//No working with UDP -> not true. Is working
		switch (profile)
		{
			case Profile.server:
				{
					switch (protocol)
					{
						case Protocol.TCP:
							{
								if (TCPclients.Count > 0)
									foreach (Socket c in TCPclients)
									{
										byte[] data = new byte[1024];
										data = Encoding.UTF8.GetBytes(enterMessage.text);
										c.Send(data);
									}
								break;
							}
						case Protocol.UDP:
							{
								if (UDPclients.Count > 0)
									foreach (EndPoint c in UDPclients)
									{
										byte[] data = new byte[1024];
										data = Encoding.UTF8.GetBytes(enterMessage.text);
										socketServer.SendTo(data, data.Length, SocketFlags.None, c);
									}
								break;
							}
						default:
							break;
					}

					customLog(enterMessage.text);
					break;
				}
			case Profile.client:
				{
					byte[] data = new byte[1024];
					data = Encoding.UTF8.GetBytes(enterMessage.text);

					if (protocol == Protocol.TCP)
						socketClient.Send(data);
					else if (protocol == Protocol.UDP)
						socketClient.SendTo(data, data.Length, SocketFlags.None, remote);
					break;
				}
			default:
				break;
		}
	}

	/*---------------------STARTGAME-------------------*/
	public void Start()
	{

	}
}

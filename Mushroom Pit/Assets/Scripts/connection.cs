using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Text;
using UnityEngine.UI;

public class connection : MonoBehaviour
{
	enum Protocol { TCP, UDP }; private Protocol protocol;
	enum Profile { server, client }; private Profile profile;
	Socket socketServer;
	Socket socketClient;
	Thread threadServer;
	Thread threadServerR;
	Thread threadClient;
	EndPoint remote;

	//Check for global use
	List<Socket> clients;

	public Text enterUserName;
	public Text enterServerIP;
	public Text enterServerPort;
	public Text ChatBox;
	public Text enterMessage;
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

		if (clients != null) 
			clients.Clear();
		clients = new List<Socket>();
		log = null;
		remote = (EndPoint)(new IPEndPoint(IPAddress.Any, 0));
	}
	void Awake()
	{
		Reset((int)profile, (int)protocol);
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
				GameObject.Find("CreateGame").GetComponent<Button>().interactable = false;
				GameObject.Find("JoinGame").GetComponent<Button>().interactable = true;
				GameObject.Find("Disconnect").GetComponent<Button>().interactable = true;
				break;
			case Profile.server:
				GameObject.Find("CreateGame").GetComponent<Button>().interactable = true;
				GameObject.Find("JoinGame").GetComponent<Button>().interactable = false;
				GameObject.Find("Disconnect").GetComponent<Button>().interactable = false;
				break;
			default:
				break;
		}
	}

	/*---------------------TEXT-------------------*/
	void Update()
	{
		if (log != null) { ChatBox.text += log; log = null; }
	}
	void customLog(string x, bool nl = true)
	{
		string shrt = x.TrimEnd('\0');
		log += shrt; if (nl) log += '\n';
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

		if (protocol == Protocol.TCP)
		{
			threadServerR = new Thread(GatherM);
			threadServerR.Start();
		}
		else if (protocol == Protocol.UDP)
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
					socketServer.Listen(2);
					Socket newClient = socketServer.Accept();
					clients.Add(newClient);
					customLog("client deceived " + clients[^1].RemoteEndPoint);
					break;
				}
			case Protocol.UDP:
				{
					byte[] data = new byte[1024];
					int recv = socketServer.ReceiveFrom(data, ref remote);

					customLog("client deceived " + remote.ToString());
					socketServer.SendTo(data, data.Length, SocketFlags.None, remote);

					while (true)
					{
						data = new byte[1024];
						recv = socketServer.ReceiveFrom(data, ref remote);
						string msg = Encoding.UTF8.GetString(data, 0, recv);
						customLog(msg);

						socketServer.SendTo(data, recv, SocketFlags.None, remote);
					}
					break;
				}
			default:
				break;
		}
	}

	void GatherM()
	{
		while (true)
		{
			if (clients.Count > 0)
				foreach (Socket c in clients)
				{
					byte[] data = new byte[1024];
					int recv = c.Receive(data);
					if (recv == 0)
					{
						customLog("client disconnected");
						clients.Remove(c);
					}
					else
					{
						c.Send(data);
						string msg = Encoding.UTF8.GetString(data, 0, recv);
						customLog(msg);
					}
				}
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
		switch (protocol)
		{
			case Protocol.TCP:
				{
					while (true)
					{
						byte[] data = new byte[1024];
						int recv = socketClient.Receive(data);
						string msg = Encoding.UTF8.GetString(data, 0, recv);
						customLog(msg);
					}
					break;
				}

			case Protocol.UDP:
				{
					while (true)
					{
						byte[] data = new byte[1024];
						int recv = socketClient.ReceiveFrom(data, ref remote);
						string msg = Encoding.UTF8.GetString(data, 0, recv);
						customLog(msg);
					}
					break;
				}
		}
	}
	public void Disconnect()
	{
		socketServer.Shutdown(SocketShutdown.Both);
		socketServer.Close();
	}

	/*---------------------CHAT-------------------*/
	public void SendM()
	{
		//No working with UDP
		switch (profile)
		{
			case Profile.server:
				{
					if (clients.Count > 0)
						foreach (Socket c in clients)
						{
							byte[] data = new byte[1024];
							data = Encoding.UTF8.GetBytes(enterMessage.text);
							c.Send(data);
						}
					customLog(enterMessage.text);
					break;
				}
			case Profile.client:
				{
					byte[] data = new byte[1024];
					data = Encoding.UTF8.GetBytes(enterMessage.text);
					socketClient.Send(data);
					break;
				}
			default:
				break;
		}
	}
}

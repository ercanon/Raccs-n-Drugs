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
	List<Socket> clients;
	public Text enterUserName;
	public Text enterServerIP;
	public Text enterServerPort;
	public Text ChatBox;
	public Text enterMessage;
	string log;
	EndPoint remote;

	void Reset()
	{
		protocol = Protocol.TCP;
		profile = Profile.server;
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
		if (clients != null) clients.Clear();
		clients = new List<Socket>();
		log = null;
		remote = (EndPoint)(new IPEndPoint(IPAddress.Any, 0));
	}
	void Awake()
	{
		Reset();
	}
	public void ChangeProtocol(int val)
	{
		Reset();
		protocol = (Protocol)val;
	}
	public void ChangeProfile(int val)
	{
		Reset();
		profile = (Profile)val;

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
		else if (protocol == Protocol.TCP)
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
					customLog(remote.ToString() + " spoke");
					socketServer.SendTo(data, data.Length, SocketFlags.None, remote);
					while (true)
					{
						recv = socketServer.ReceiveFrom(data, ref remote);
						string msg = Encoding.UTF8.GetString(data, 0, recv);
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
					IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
					remote = (EndPoint)sender;

					byte[] data = new byte[1024];
					data = Encoding.UTF8.GetBytes(enterUserName.text + " joined the server!");
					socketClient.Send(data, data.Length, SocketFlags.None);

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
			int recv = socketClient.Receive(data);
			string msg = Encoding.UTF8.GetString(data, 0, recv);
			customLog(msg);
		}
	}
	public void Disconnect()
	{
		try
    	{
        	socketClient.Close();
			socketClient = null;
			
			//byte[] data = new byte[1024];
			//data = Encoding.UTF8.GetBytes(enterMessage.text);
			//socketClient.Send(data);
        	customLog(enterUserName.text + " has been disconnected");
    	}
		catch(SocketException e)
    	{
			customLog(e.Message);
    	}

	}

	/*---------------------CHAT-------------------*/
	public void SendM()
	{
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
					break;
					customLog(enterMessage.text);
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

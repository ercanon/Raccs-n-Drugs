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
	byte[] data;
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
		data = new byte[1024];//memleak?
		remote = (EndPoint)(new IPEndPoint(IPAddress.Any, 0));
	}
	void Awake()
	{
		Reset();
	}
	public void ChangeProtocol(int val)
	{
		protocol = (Protocol)val;
		Reset();
	}
	public void ChangeProfile(int val)
	{
		profile = (Profile)val;
		Reset();
	}
	void Update()
	{
		if (log != null) { ChatBox.text += log; log = null; }
	}
	void customLog(string x, bool nl = true)
	{
		log += x; if (nl) log += '\n';
	}
	public void CreateGame()
	{
		if (protocol == Protocol.TCP) socketServer =
				new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		else if (protocol == Protocol.UDP) socketServer =
				new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
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
	}
	void WaitingPlayers()
	{
		if (protocol == Protocol.TCP)
		{
			socketServer.Listen(2);
			Socket newClient = socketServer.Accept();
			clients.Add(newClient);
			customLog("client deceived " + clients[^1].RemoteEndPoint);
			data = Encoding.UTF8.GetBytes("u joined server!");
			newClient.Send(data, data.Length, SocketFlags.None);
		}
		if (protocol == Protocol.UDP)
		{
			int recv = socketServer.ReceiveFrom(data, ref remote);
			customLog(remote.ToString() + " spoke");
			socketServer.SendTo(data, data.Length, SocketFlags.None, remote);
			while (true)
			{
				recv = socketServer.ReceiveFrom(data, ref remote);
				string msg = Encoding.UTF8.GetString(data, 0, recv);
				socketServer.SendTo(data, recv, SocketFlags.None, remote);
			}
		}
	}
	void GatherM()
	{
		while (true)
		{
			if (clients.Count > 0)
				foreach (Socket c in clients)
				{
					int recv = c.Receive(data);
					if (recv == 0) customLog("client disconnected");
					else
					{
						string msg = Encoding.UTF8.GetString(data, 0, recv);
						customLog(msg);
					}
				}
		}
	}
	public void JoinGame()
	{
		socketClient = new Socket(AddressFamily.InterNetwork,
			SocketType.Stream, ProtocolType.Tcp);
		IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(enterServerIP.text), int.Parse(enterServerPort.text));
		try
		{
			socketClient.Connect(ipep);
			customLog("joined successfully");
		}
		catch (SocketException e)
		{
			customLog(e.Message);
			return;
		}
		data = Encoding.UTF8.GetBytes(enterUserName.text);
		socketClient.Send(data, data.Length, SocketFlags.None);
		threadClient = new Thread(HearServer);
		threadClient.Start();
	}
	void HearServer()
	{
		while (true)
		{
			int recv = socketClient.Receive(data);
			string msg = Encoding.UTF8.GetString(data, 0, recv);
			customLog(msg);
		}
	}
}

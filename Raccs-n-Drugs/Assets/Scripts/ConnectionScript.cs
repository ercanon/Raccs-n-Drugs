using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class ConnectionScript : MonoBehaviour
{
	/*---------------------VARIABLES-------------------*/
	enum Protocol { UDP, TCP }; private Protocol protocol;
	enum Profile { host, client }; private Profile profile;
	enum TypeData { start, posList, chat, cocainePositions, raccsTransform, raccsCharge, userReady };
	
	private Socket socketHost;
	private Socket socket;
	private Thread ServerWaiting;
	private Thread ServerGather;
	private Thread ClientListen;

	private EndPoint remote;
	private List<EndPoint> clients;
	private int clientsReady;
	private List<byte[]> pendingData;

	[SerializeField] private GameplayScript gameplay;
	[SerializeField] private LobbyScript uiScript;



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

		if (profile == Profile.host)
		{
			if (socketHost != null)
			{
				socketHost.Shutdown(SocketShutdown.Both);
				socketHost.Close();
				socketHost = null;
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

		remote = null;
		clientsReady = 0;

		uiScript.Reset();
		gameplay.Reset();

		protocol = (Protocol)prot;
		profile = (Profile)prof;
	}
	
	void Awake()
	{
		uiScript.connect = this;
		gameplay.connect = this;
		Reset((int)Protocol.UDP, (int)Profile.client);
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


	public void ChangeProfile(int profile)
    {
		Reset((int)protocol, profile);
	}

	public void ChangeProtocol(int protocol)
	{
		Reset(protocol, (int)profile);
	}


	/*---------------------SERIALIZATION-------------------*/
	void Update()
	{
		while (pendingData.Count > 0)
		{
			Deserialize(pendingData[0]);
			pendingData.RemoveAt(0);
		}
	}

	private byte[] Serialize(int type, string message = "", string sender = "")
	{
		MemoryStream stream = new MemoryStream();
		BinaryWriter writer = new BinaryWriter(stream);

		writer.Write(type);
		switch (type)
		{
			case 0: //LaunchGame
				writer.Write(clients.Count);
				break;
			case 1: //Position List Racoon
				writer.Write(clients.Count - 1);
				break;
			case 2: //Chat
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
			case 6: //UserReady
				writer.Write(clientsReady++);
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
			case 1: //Position List Racoon
				gameplay.posRaccList = reader.ReadInt32();
				break;
			case 2: //Chat
				uiScript.customLog(reader.ReadString(), reader.ReadString());
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
			case 6: //UserReady
				clientsReady = reader.ReadInt32();
				uiScript.customLog(clientsReady.ToString() + "/" + clients.Count.ToString() + "users are ready!", "Server");
				break;
			default:
				uiScript.customLog("Package is corrupted", "Error");
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



	/*---------------------HOST-------------------*/
	public void CreateGame(InputField IPInput, InputField portInput, string userName)
	{
		if (socketHost != null)
        {
			uiScript.customLog("Cannot create another server!", "Error");
			return;
		}

		if (protocol == Protocol.TCP)
			socketHost = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		else if (protocol == Protocol.UDP)
			socketHost = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

		if (portInput.text == "")
			portInput.text = 666.ToString();
					  
		IPEndPoint ipep = new IPEndPoint(IPAddress.Any, int.Parse(portInput.text));
		socketHost.Bind(ipep);

		if (protocol == Protocol.TCP)
		{
			ServerWaiting = new Thread(WaitingClients);
			ServerWaiting.Start();

			socketHost.Listen(4);
		}

		uiScript.customLog(userName + "'s game available at " + TellIP(), "Server");

		ServerGather = new Thread(GatherAndBroadcast);
		ServerGather.Start();

		IPInput.text = "127.0.0.1";
		JoinGame(IPInput, portInput, userName);
	}

	private void WaitingClients()
	{
		try
		{
			Socket pendingClient = socketHost.Accept();
			clients.Add(pendingClient.RemoteEndPoint);
		}
		catch (SocketException e)
		{
			uiScript.customLog(e.ToString(), "Error");
		}
	}

	private void GatherAndBroadcast()
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

	void Broadcast(TypeData tData, string message = "", string user = "")
	{
		byte[] data = new byte[1024];
		data = Serialize((int)tData, message, user);

		if (protocol == Protocol.TCP)
			socketHost.Send(data);
		else if (protocol == Protocol.UDP)
			foreach (EndPoint reciber in clients)
				socketHost.SendTo(data, data.Length, SocketFlags.None, reciber);
	}



	/*---------------------CLIENT-------------------*/
	public void JoinGame(InputField IPInput, InputField portInput, string userName)
	{
		if (remote != null)
		{
			uiScript.customLog("You have already joined!", "Error");
			return;
		}

		if (socket == null)
		{
			if (protocol == Protocol.TCP)
				socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			else if (protocol == Protocol.UDP)
				socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		}

		if (portInput.text == "")
			portInput.text = 666.ToString();
			
		remote = new IPEndPoint(IPAddress.Parse(IPInput.text), int.Parse(portInput.text));

		try
		{
			if (protocol == Protocol.TCP)
				socket.Connect(remote);

			SendData(Serialize((int)TypeData.chat, userName + " joined the server!", "Server"), socket, remote);
		}
		catch (SocketException e)
		{
			uiScript.customLog(e.Message, "Error");
			remote = null;
			return;
		}

		ClientListen = new Thread(Listen);
		ClientListen.Start();
	}
	
	private void Listen()
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


	/*---------------------GAME-------------------*/
	public void LaunchGame()
    {
		if (profile == Profile.host && clients.Count == clientsReady-1)
		{
			SendClientData((int)TypeData.start);
			gameplay.LaunchGame(clients.Count);
		}
		else
			SendClientData((int)TypeData.userReady);
	}
}

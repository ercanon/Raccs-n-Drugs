using UnityEngine;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine.UI;
using System.Net.NetworkInformation;
using System.IO;

public class bruh : MonoBehaviour
{
	Socket socket;
	Thread thread;
	int port = 9000;
	int maxClients = 4;
	Vector3 me;
	string localIP;
	Dictionary<EndPoint, Vector3> peers;
	byte[] packet = null;
	public Text chat;
	public InputField entry;
	//
	public GameObject racoon;
	public GameObject cocaine;
	bool transition;
	//
	private string chatHistory = null;
	private void Log(string data) { chatHistory += data + '\n'; }
	Vector3 spawnRand() { return new Vector3(Random.Range(-9, -5), 0, Random.Range(7, 15)); }
	Vector3 spawnRand0() { return new Vector3(-5, 0, 7); }
	private void Reset()
	{
		socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		//socket.Blocking = false;
		socket.Bind(new IPEndPoint(IPAddress.Any, port));
		thread = new Thread(Listen);
		thread.Start();
		peers = new Dictionary<EndPoint, Vector3>();
		me = spawnRand();
	}
	public void PingX()
	{
		if (entry.text != null)
		{
			byte[] msg = Encoding.ASCII.GetBytes("yo!");
			EndPoint target = new IPEndPoint(IPAddress.Parse(entry.text), port);
			socket.SendTo(msg, msg.Length, SocketFlags.None, target);
			entry.text = null;
		}
	}
	private void Listen()
	{
		while (true)
		{
			byte[] data = new byte[1024];
			EndPoint who = new IPEndPoint(IPAddress.Any, 0);
			int rf = socket.ReceiveFrom(data, ref who);
			if (rf > 0)
			{
				if (peers.ContainsKey(who) == false)
				{
					if (peers.Count < maxClients)
					{
						peers.Add(who, spawnRand0());
						Log("[" + who + "] joined");
						Broadcast(Encoding.ASCII.GetBytes("join " + who));
					}
				}
				else
				{
					if (peers.Count < maxClients)
					{
						var maybeIP = Encoding.ASCII.GetString(data);
						var filter = new Regex(@"join (\d{1,}(?:\.\d{1,}){3})$").Match(maybeIP);
						if (filter.Success)
						{
							IPAddress ip = IPAddress.Parse(filter.Groups[1].Value);
							IPEndPoint new_who = new IPEndPoint(ip, port);
							if (peers.ContainsKey(new_who) == false)
							{
								peers.Add(new_who, spawnRand0());
								Log("[" + who + "] joined");
							}
							continue;
						}
					}
					packet = new byte[1024];
					data.CopyTo(packet, 0);
				}
			}
		}
	}
	private void Broadcast(byte[] data)
	{
		foreach (var who in peers)
			socket.SendTo(data, data.Length, SocketFlags.None, who.Key);
	}
	private byte[] Serialize()
	{
		MemoryStream stream = new MemoryStream();
		BinaryWriter writer = new BinaryWriter(stream);
		writer.Write(transition);
		writer.Write(localIP);
		writer.Write(me.x);
		writer.Write(me.y);
		writer.Write(me.z);
		return stream.ToArray();
	}
	private void Deserialize(byte[] data)
	{
		MemoryStream stream = new MemoryStream(data);
		BinaryReader reader = new BinaryReader(stream);
		stream.Seek(0, SeekOrigin.Begin);
		bool begun = reader.ReadBoolean();
		string ip = reader.ReadString();
		float x = reader.ReadSingle();
		float y = reader.ReadSingle();
		float z = reader.ReadSingle();
		Debug.Log(ip);
		EndPoint who = new IPEndPoint(IPAddress.Parse(ip), port);
		if (peers.ContainsKey(who))
		{
			var target = peers[who];
			target.Set(x, y, z);
		}
		if (transition == false && begun)
		{
			StartGame();
		}
	}
	private void Awake()
	{
		Reset();
		localIP = TellIP();
		Log(TellIP());
		transition = false;
	}
	public void StartGame()
	{
		transition = true;
		Instantiate(racoon, me, Quaternion.identity);
		foreach (var player in peers)
		{
			Instantiate(racoon, player.Value, Quaternion.identity);
		}
		GameObject.Find("UI").SetActive(false);
	}
	private void Update()
	{
		if (packet != null)
		{
			Deserialize(packet);
			packet = null;
		}
		Broadcast(Serialize());
		if (chatHistory != null)
		{
			chat.text += chatHistory;
			chatHistory = null;
		}
		//
		if (transition)
		{
			var cam_now = GameObject.Find("Main Camera").transform;
			var cam_target = GameObject.Find("CameraGamePosition").transform;

			cam_now.position = Vector3.Lerp(cam_now.position, cam_target.position, 2f * Time.deltaTime);
			cam_now.rotation = Quaternion.Lerp(cam_now.rotation, cam_target.rotation, 2f * Time.deltaTime);

			if (Vector3.Distance(cam_now.position, cam_target.position) < 0.15f)
				transition = false;
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
						return "[" + ni.Name + "] " + ip.Address.ToString();
					}
				}
			}
		}
		return null;
	}
}

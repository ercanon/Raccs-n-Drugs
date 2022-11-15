using UnityEngine;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine.UI;
//manlaig
public class server : MonoBehaviour
{
	[Tooltip("touch and die")]
	[SerializeField] int port = 8008;
	[Tooltip("there can only be one king")]
	[SerializeField] int maxClients = 4;
	Socket socket;
	Thread thread;
	int idAssignIndex = -1;
	Dictionary<EndPoint, int> clients = new Dictionary<EndPoint, int>();
	Dictionary<EndPoint, float> pings = new Dictionary<EndPoint, float>();
	//
	List<string> history;
	public Text chat;
	public InputField entry;
	void Awake()
	{
		IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, port);
		socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		socket.Bind(endPoint);
		//socket.Blocking = false;
		thread = new Thread(Listen);
		thread.Start();
	}
	void Update()
	{
		//
	}
	void Listen()
	{
		byte[] packet = new byte[1024];
		EndPoint who = new IPEndPoint(IPAddress.Any, port);
		int rf = socket.ReceiveFrom(packet, ref who);
		if (rf > 0) onPackedReceived(packet, who);
	}
	private void OnConnect(EndPoint who)
	{
		if (clients.Count < maxClients)
		{
			clients.Add(who, ++idAssignIndex);
			Broadcast(MessageToData("$who joined the game!"));
		}
		else Debug.Log("cannot override maxClients, sorry");
	}
	private void OnDisconnect(EndPoint who)
	{
		if (clients.ContainsKey(who))
		{
			clients.Remove(who);
			Broadcast(MessageToData("$who leaved the game!"));
		}
		else Debug.Log("x, d.");
	}
	private void HandlePing(EndPoint who)
	{
		float last = Time.time;
		if (last - pings[who] > 5.0f)
		{
			OnDisconnect(who);
			Broadcast(MessageToData("$who timeouted ._."));
		}
		else pings[who] = last;
	}
	private byte[] MessageToData(string str)
	{
		return Encoding.UTF8.GetBytes(str);
	}
	private string DataToMessage(byte[] data)
	{
		return Encoding.UTF8.GetString(data);
	}
	private void Broadcast(byte[] data)
	{
		foreach (KeyValuePair<EndPoint, int> i in clients)
			Debug.Log(i.Key);// SendPacket(data, i.Key);
	}
	private void SendPacket(byte[] data, EndPoint who)
	{
		socket.SendTo(data, who);
	}
	private void onPackedReceived(byte[] data, EndPoint who)
	{
		byte[] prefix = { data[0], data[1] };
		string input = DataToMessage(prefix);
		switch (input)
		{
			case "j;":
				OnConnect(who);
				break;
			case "l;":
				OnDisconnect(who);
				break;
			case "p;":
				HandlePing(who);
				break;
			default:
				Broadcast(data);
				break;
		}
		Debug.Log(DataToMessage(data));
	}
	public void onEntryReceived()
	{
		byte[] data = MessageToData(entry.text);
		onPackedReceived(data, socket.LocalEndPoint);
		entry.text = null;
	}
	private void ParseConnectionAttempt(string data, ref IPAddress ip, ref int port, ref string name)
	{
		Regex filter = new Regex(@"j; (\d{3}(?:\.\d{1,}){3}):(\d{4}) as (\w{3,})$");//j; 127.0.0.1:8008 as Name
		var result = filter.Match(data);
		ip = IPAddress.Parse(result.Groups[0].Value);
		port = int.Parse(result.Groups[1].Value);
		name = result.Groups[2].Value;//.ToString();
		Debug.Log(ip + " " + port + " " + name);
	}
}

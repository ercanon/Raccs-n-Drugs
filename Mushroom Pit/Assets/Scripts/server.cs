using UnityEngine;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine.UI;
using System.Net.NetworkInformation;
//manlaig
public class server : MonoBehaviour
{
	[Tooltip("touch and die")]
	[SerializeField] int port = 8194;
	[Tooltip("there can only be one king")]
	[SerializeField] int maxClients = 4;
	Socket socket;
	Thread thread;
	string myname = null;
	EndPoint host = null;
	Dictionary<EndPoint, string> clients = new Dictionary<EndPoint, string>();
	public Text chat;
	public InputField entry;
	public void Reset()
	{

	}
	void Log(string data)
	{
		chat.text += data;// + '\n';
	}
	void Awake()
	{
		socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		IPEndPoint ep = new IPEndPoint(IPAddress.Any, port);
		socket.Bind(ep);
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
		if (rf > 0) UInput(packet, who);
	}
	private void IConnect(EndPoint where)
	{
		socket.SendTo(MessageToData("x; " + myname), where);
	}
	private void UConnect(EndPoint who, string name)
	{
		if (clients.Count < maxClients)
		{
			if (clients.ContainsKey(who) == false)
			{
				clients.Add(who, name);
				Broadcast(who + " as " + name + " joined the game!");
			}
			else Log(who + " as " + name + " attempted to join, but he's already in, STUPID?");
		}
		else Log(who + " as " + name + " attempted to join, but we're full, SAD");
	}
	private void IDisconnect()
	{
		if (host != null) socket.SendTo(MessageToData("x; " + myname), host);
		else Log("u alone monk!");
	}
	private void UDisconnect(EndPoint who)
	{
		if (clients.Remove(who)) Broadcast(clients[who] + " leaved the game!");
		else Broadcast(who + " attempted to leave, though it was never part of this, stayed alone nonetheless");
	}
	private void HandlePing() { }
	private byte[] MessageToData(string str)
	{
		return Encoding.UTF8.GetBytes(str);
	}
	private string DataToMessage(byte[] data)
	{
		return Encoding.UTF8.GetString(data);
	}
	private void Broadcast(string data)
	{
		Broadcast(MessageToData(data));
	}
	private void Broadcast(byte[] data)
	{
		foreach (var i in clients)
			SendPacket(data, i.Key);
	}
	private void SendPacket(byte[] data, EndPoint who)
	{
		socket.SendTo(data, who);
	}
	private void UInput(byte[] data, EndPoint who)
	{
		string input = DataToMessage(data);
		switch (input.Substring(0, 2))
		{
			case "x;":
				UDisconnect(who);
				var filter = new Regex(@"x; (\w{3,})$").Match(entry.text);
				if (filter.Success)
				{
					string urname = filter.Groups[0].Value;
					UConnect(who, urname);
				}
				else Log("perhaps, just perhaps, u monk?");
				break;
			default:
				Broadcast(input);
				Log(input);
				break;
		}
		//entry.text = null;
	}
	public void IInput()
	{
		if (entry.text.Length < 2) return;
		switch (entry.text.Substring(0, 2))
		{
			case "x;":
				IDisconnect();
				var filter = new Regex(@"x; (\d{3}(?:\.\d{1,}){3}):(\d{4})$").Match(entry.text);
				if (filter.Success)
				{
					IPAddress ip = IPAddress.Parse(filter.Groups[0].Value);
					int port = int.Parse(filter.Groups[1].Value);
					IConnect(new IPEndPoint(ip, port));
				}
				else Log("perhaps learn how to write? or internal error");
				break;
			case "p;":
				Log(TellIP());
				break;
			default:
				Broadcast(entry.text);
				Log(entry.text);
				break;
		}
		//entry.text = null;
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
						return ni.Name + " " + ip.Address.ToString();
					}
				}
			}
		}
		return null;
	}
}

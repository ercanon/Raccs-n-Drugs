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
	string myname = null;
	EndPoint host = null;
	Dictionary<EndPoint, string> clients = new Dictionary<EndPoint, string>();
	public Text chat;
	public InputField entry;
	public void Reset()
	{

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
		if (rf > 0) onPackedReceived(packet, who);
	}
	private void IConnect(EndPoint where)
	{
		socket.SendTo(MessageToData("j; " + myname), where);
	}
	private void UConnect(EndPoint who, string name)
	{
		if (clients.Count < maxClients)
		{
			if (clients.ContainsKey(who))
			{
				chat.text += who + " as " + name + " attempted to join, but he's already in, STUPID?";
			}
			else
			{
				clients.Add(who, name);
				Broadcast(who + " as " + name + " joined the game!");
			}
		}
		else
		{
			chat.text += who + " as " + name + " attempted to join, but we're full, SAD";
		}
	}
	private void IDisconnect()
	{
		if (host != null)
		{
			socket.SendTo(MessageToData("l; " + myname), host);
		}
		else
		{
			chat.text += "u alone monk!";
		}
	}
	private void UDisconnect(EndPoint who, string name)
	{
		if (clients.ContainsKey(who))
		{
			clients.Remove(who);
			Broadcast(clients[who] + " leaved the game!");
		}
		else
		{
			Broadcast(who + " attempted to leave, though it was never part of this, stayed alone nonetheless");
		}
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
	private void OnPackedReceived(byte[] data, EndPoint who)
	{
		string prefix = entry.text.Substring(0, 2);
		switch (prefix)
		{
			case "j;":
				var filter = new Regex(@"j; (\w{3,})$").Match(entry.text);
				string urname = filter.Groups[0].Value;
				UConnect(who, urname);
				break;
			case "l;":
				UDisconnect(who, urname);
				break;
			default:
				Broadcast(entry.text);
				chat.text += entry.text;
				break;
		}
		//entry.text = null;
	}
	public void IInput()
	{
		string prefix = entry.text.Substring(0, 2);
		switch (prefix)
		{
			case "j;":
				var filter = new Regex(@"j; (\d{3}(?:\.\d{1,}){3}):(\d{4})$");
				var result = filter.Match(entry.text);
				IPAddress ip = IPAddress.Parse(result.Groups[0].Value);
				int port = int.Parse(result.Groups[1].Value);
				IPEndPoint where = new IPEndPoint(ip, port);
				IConnect(where);
				break;
			case "l;":
				IDisconnect();
				break;
			default:
				Broadcast(entry.text);
				chat.text += entry.text;
				break;
		}
		//entry.text = null;
	}
	private void TellIP()
	{
		//foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
		//{
		//	if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
		//	{
		//		Console.WriteLine(ni.Name);
		//		foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
		//		{
		//			if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
		//			{
		//				Console.WriteLine(ip.Address.ToString());
		//			}
		//		}
		//	}
		//}
	}
}

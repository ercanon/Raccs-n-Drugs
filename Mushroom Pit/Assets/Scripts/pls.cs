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
public class pls : MonoBehaviour
{
	Socket socket;
	Thread thread;
	int port = 9000;
	int maxClients = 4;
	string myname = "defaultName";
	EndPoint host = null;
	Dictionary<EndPoint, string> clients = new Dictionary<EndPoint, string>();
	public Text chat;
	public InputField entry;
	public void Reset()
	{
		//
	}
	void Log(string data) { chat.text += data + '\n'; }
	void Log(byte[] data) { Log(B2S(data)); }
	void Awake()
	{
		socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
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
		while (true)
		{
			byte[] data = new byte[1024];
			EndPoint who = new IPEndPoint(IPAddress.Any, 0);
			int rf = socket.ReceiveFrom(data, ref who);
			if (rf > 0)
			{
				var message = B2S(data);
				if (host == null)/*server*/
				{
					if (clients.ContainsKey(who) == false)
					{
						if (clients.Count <= maxClients)
						{
							clients.Add(who, message);
							Send("admitted;", who);
							Broadcast("welcome " + message);
						}
						else
						{
							Send("rejected;", who);
						}
					}
					else
					{
						if (message == "leave;")
						{
							Broadcast(who + " leaves");
							clients.Remove(who);
							Send("left;", who);
						}
						else
						{
							Log(data);
							Broadcast(data);
						}
					}
				}
				else/*client*/
				{
					if (message == "admitted;")
					{
						host = who;
					}
					if (message == "rejected;")
					{
						Log("cannot join, server might be full");
					}
					if (message == "left;")
					{
						host = null;
						Log("u finnnallly left");
					}
					else
					{
						Log(message);
					}
				}
			}
		}
	}
	private byte[] S2B(string data) { return Encoding.ASCII.GetBytes(data); }
	private string B2S(byte[] data) { return Encoding.ASCII.GetString(data); }
	private void Send(byte[] data, EndPoint who)
	{
		socket.SendTo(data, data.Length, SocketFlags.None, who);
	}
	private void Send(string data, EndPoint who)
	{
		Send(S2B(data), who);
	}
	private void Broadcast(byte[] data)
	{
		foreach (var i in clients)
			Send(data, i.Key);
	}
	private void Broadcast(string data)
	{
		Broadcast(S2B(data));
	}
	public void Entry()
	{
		if (entry.text.Length == 0) return;
		if (host == null)/*none*/
		{
			var filter = new Regex(@"join (\d{1,}(?:\.\d{1,}){3})$").Match(entry.text);
			if (filter.Success)
			{
				IPAddress ip = IPAddress.Parse(filter.Groups[1].Value);
				IPEndPoint where = new IPEndPoint(ip, port);
				Send(myname, where);
			}
			else if (entry.text == "create;")
			{
				socket.Bind(new IPEndPoint(IPAddress.Any, port));
				Log("game created, u are server!");
			}
			else if (entry.text == "ip;")
					{
				Log(TellIP());
			}
			else Log(entry.text);
		}
		else/*client*/
		{
			if (entry.text == "leave;")
			{
				Send(entry.text, host);
			}
			else if (entry.text == "ping;")
			{
				//
			}
			else Send(entry.text, host);
		}
		
		entry.text = null;
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

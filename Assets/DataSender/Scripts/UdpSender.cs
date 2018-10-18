using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class UdpSender : MonoBehaviour
{
    public string sendTextData = "test";
    public string remoteIp = "localhost";
    public int remotePort = 8888;

    Socket udp;
    IPEndPoint remote;

    [ContextMenu("send test")]
    void Send()
    {
        Send(sendTextData);
    }

    public void Send<T>(T data, IPEndPoint remote = null)
    {
        var text = JsonUtility.ToJson(data);
        if (remote == null)
            remote = this.remote;
        Send(text, remote);
    }

    public void Send(string text, IPEndPoint remote = null)
    {
        sendTextData = text;
        var data = Encoding.UTF8.GetBytes(sendTextData);
        Send(data, remote);
    }

    public void Send(byte[] data, IPEndPoint remote = null)
    {
        if (remote == null)
            remote = this.remote;
        udp.SendTo(data, remote);
    }

    void Start()
    {
        udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        remote = new IPEndPoint(FindFromHostName(remoteIp), remotePort);
    }

    private void OnDestroy()
    {
        udp.Close();
        udp = null;
    }

    public IPAddress FindFromHostName(string hostname)
    {
        var addresses = Dns.GetHostAddresses(hostname);
        IPAddress address = IPAddress.None;
        for (var i = 0; i < addresses.Length; i++)
        {
            if (addresses[i].AddressFamily == AddressFamily.InterNetwork)
            {
                address = addresses[i];
                break;
            }
        }
        return address;
    }
}

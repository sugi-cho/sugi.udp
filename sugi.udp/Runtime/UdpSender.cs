using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace sugi.cc.udp
{
    public class UdpSender : MonoBehaviour
    {
        public static void Send(string text, IPEndPoint remote)
        {
            var data = Encoding.UTF8.GetBytes(text);
            Send(data, remote);
        }
        public static void Send(byte[] data, IPEndPoint remote)
        {
            UdpSocket.SendTo(data, remote);
        }
        public static IPAddress FindFromHostName(string hostname)
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

        public string remoteIp = "localhost";
        public bool useBroadCast;
        public int remotePort = 8888;
        IPEndPoint remote;

        static Socket UdpSocket
        {
            get
            {
                if (_udp == null)
                {
                    _udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    _udp.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
                    Application.quitting += CloseSocket;
                }
                return _udp;
            }
        }
        static Socket _udp;
        static void CloseSocket()
        {
            if (_udp != null)
            {
                _udp.Close();
                _udp = null;
            }
        }

        public void CreateRemoteEP(string ip, int port)
        {
            remoteIp = ip;
            remotePort = port;

            if (useBroadCast)
                remote = new IPEndPoint(IPAddress.Broadcast, remotePort);
            else
                remote = new IPEndPoint(FindFromHostName(remoteIp), port);
        }

        public void Send(string text)
        {
            Send(text, remote);
        }
        public void Send(byte[] data)
        {
            Send(data, remote);
        }

        void Start()
        {
            CreateRemoteEP(remoteIp, remotePort);
        }
    }
}
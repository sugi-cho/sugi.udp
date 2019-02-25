using System.Collections.Generic;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.Events;

public abstract class UdpServer : MonoBehaviour
{
    public int localPort = 8888;
    public int receiveLimit = 10;
    public string errorMsg;

    Socket udp;
    Thread reader;
    byte[] receiveBuffer;

    public void StartServer(int port)
    {
        StopServer();

        udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        localPort = port;
        udp.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        udp.Bind(new IPEndPoint(IPAddress.Any, localPort));

        reader = new Thread(Reader);
        reader.Start();
    }

    public void StopServer()
    {
        if (udp != null)
        {
            udp.Close();
            udp = null;
        }
        if (reader != null)
        {
            reader.Abort();
            reader.Join();
            reader = null;
        }
    }

    void Start()
    {
        receiveBuffer = new byte[1 << 16];
        StartServer(localPort);
    }

    void OnApplicationQuit()
    {
        StopServer();
    }

    void Reader()
    {
        var clientEndpoint = new IPEndPoint(IPAddress.Any, 0);
        while (udp != null && udp.IsBound)
        {
            try
            {
                var fromendpoint = (EndPoint)clientEndpoint;
                var length = udp.ReceiveFrom(receiveBuffer, ref fromendpoint);
                var fromipendpoint = fromendpoint as IPEndPoint;
                if (length == 0 || fromipendpoint == null)
                    continue;

                OnReadPacket(receiveBuffer, length, fromipendpoint);
            }
            catch (System.Exception e) { OnRaiseError(e); }
        }
    }

    protected abstract void OnReadPacket(byte[] buffer, int length, IPEndPoint source);

    protected virtual void OnRaiseError(System.Exception e)
    {
        errorMsg = e.ToString();
    }
}

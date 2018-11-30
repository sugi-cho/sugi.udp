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
        if (reader != null)
            reader.Abort();
        if (udp != null)
            udp.Close();
        udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        localPort = port;
        udp.Bind(new IPEndPoint(IPAddress.Any, localPort));

        reader = new Thread(Reader);
        reader.Start();
    }

    void Start()
    {
        receiveBuffer = new byte[1 << 16];
        StartServer(localPort);
    }


    private void OnDestroy()
    {
        udp.Close();
        udp = null;
        reader.Abort();
        reader = null;
    }

    void Reader()
    {
        var clientEndpoint = new IPEndPoint(IPAddress.Any, 0);
        while (udp != null)
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

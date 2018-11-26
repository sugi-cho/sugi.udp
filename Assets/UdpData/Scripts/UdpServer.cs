using System.Collections.Generic;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.Events;

public class UdpServer : MonoBehaviour
{
    public int localPort = 8888;
    public BytesEvent onDataReceived;
    public int receiveLimit = 10;
    public string errorMsg;

    Socket udp;
    Thread reader;
    byte[] receiveBuffer;
    Queue<byte[]> receivedDataQueue = new Queue<byte[]>();

    void Start()
    {
        udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        udp.Bind(new IPEndPoint(IPAddress.Any, localPort));
        receiveBuffer = new byte[1 << 16];

        reader = new Thread(Reader);
        reader.Start();
    }

    protected virtual void Update()
    {
        lock (receivedDataQueue)
            while (0 < receivedDataQueue.Count)
                onDataReceived.Invoke(receivedDataQueue.Dequeue());
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

                if (receiveLimit <= 0 || receivedDataQueue.Count < receiveLimit)
                {
                    var data = new byte[length];
                    System.Buffer.BlockCopy(receiveBuffer, 0, data, 0, length);
                    OnReadPacket(data, fromipendpoint);
                }
            }
            catch (System.Exception e) { OnRaiseError(e); }
        }
    }

    protected virtual void OnReadPacket(byte[] data, IPEndPoint source)
    {
        lock (receivedDataQueue)
            receivedDataQueue.Enqueue(data);
    }

    protected virtual void OnRaiseError(System.Exception e)
    {
        errorMsg = e.Message;
    }


    [System.Serializable]
    public class BytesEvent : UnityEvent<byte[]> { }
}

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

    Socket udp;
    Thread reader;
    byte[] receiveBuffer;
    Queue<byte[]> receivedDataQueue;

    void Start()
    {
        udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        udp.Bind(new IPEndPoint(IPAddress.Any, localPort));
        receiveBuffer = new byte[1 << 16];
        receivedDataQueue = new Queue<byte[]>();

        reader = new Thread(Reader);
        reader.Start();
    }

    private void Update()
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
        while (udp != null)
        {
            try
            {
                var clientEndpoint = new IPEndPoint(IPAddress.Any, 0);
                var fromendpoint = (EndPoint)clientEndpoint;
                var length = udp.ReceiveFrom(receiveBuffer, ref fromendpoint);
                if (length == 0 || (clientEndpoint = fromendpoint as IPEndPoint) == null)
                    continue;
                if (receiveLimit <= 0 || receivedDataQueue.Count < receiveLimit)
                {
                    var data = new byte[length];
                    System.Buffer.BlockCopy(receiveBuffer, 0, data, 0, length);
                    receivedDataQueue.Enqueue(data);
                }
            }
            catch { }
        }
    }

    [System.Serializable]
    public class BytesEvent : UnityEvent<byte[]> { }
}

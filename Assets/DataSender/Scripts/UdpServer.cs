using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.Events;

public class UdpServer : MonoBehaviour
{
    public int localPort = 8888;
    public string receivedText;
    public TextEvent onTextDataReceived;
    public int receiveLimit = 10;

    Socket udp;
    Thread reader;
    byte[] receiveBuffer;
    Queue<string> receivedTextQueue;
    
    void Start()
    {
        udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        udp.Bind(new IPEndPoint(IPAddress.Any, localPort));
        receiveBuffer = new byte[1 << 16];
        receivedTextQueue = new Queue<string>();

        reader = new Thread(Reader);
        reader.Start();
    }

    private void Update()
    {
        while (0 < receivedTextQueue.Count)
        {
            var text = receivedTextQueue.Dequeue();
            onTextDataReceived.Invoke(text);
        }
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
                receivedText = Encoding.UTF8.GetString(receiveBuffer, 0, length);
                if (receiveLimit <= 0 || receivedTextQueue.Count < receiveLimit)
                    receivedTextQueue.Enqueue(receivedText);
            }
            catch { }
        }
    }

    [System.Serializable]
    public class TextEvent : UnityEvent<string> { }
}

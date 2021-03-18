using System.Collections.Generic;
using System.Net;
using System.Text;
using UnityEngine.Events;

namespace sugi.cc.udp
{
    public class TextDataServer : UdpServer
    {
        public TextDataEvent onDataReceived;
        Queue<string> receivedDataQueue = new Queue<string>();

        protected override void OnReadPacket(byte[] buffer, int length, IPEndPoint source)
        {
            var text = Encoding.UTF8.GetString(buffer, 0, length);
            if (0 < receiveLimit && receivedDataQueue.Count < receiveLimit)
                lock (receivedDataQueue)
                    receivedDataQueue.Enqueue(text);
        }
        protected void Update()
        {
            lock (receivedDataQueue)
                while (0 < receivedDataQueue.Count)
                    onDataReceived.Invoke(receivedDataQueue.Dequeue());
        }

        [System.Serializable]
        public class TextDataEvent : UnityEvent<string> { }
    }
}
using System.Collections.Generic;
using System.Net;
using UnityEngine.Events;

namespace sugi.cc.udp
{
    public class RawDataServer : UdpServer
    {
        public RawDataEvent onDataReceived;
        Queue<byte[]> receivedDataQueue = new Queue<byte[]>();

        protected override void OnReadPacket(byte[] buffer, int length, IPEndPoint source)
        {
            var data = new byte[length];
            System.Buffer.BlockCopy(buffer, 0, data, 0, length);
            if (0 < receiveLimit && receivedDataQueue.Count < receiveLimit)
                lock (receivedDataQueue)
                    receivedDataQueue.Enqueue(data);
        }
        protected void Update()
        {
            lock (receivedDataQueue)
                while (0 < receivedDataQueue.Count)
                    onDataReceived.Invoke(receivedDataQueue.Dequeue());
        }

        [System.Serializable]
        public class RawDataEvent : UnityEvent<byte[]> { }
    }
}
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.Events;

using ilda.digital;

namespace sugi.cc.udp.idn
{
    public class IDNServer : UdpServer
    {
        [SerializeField] IDNDataEvent onDataReceived;
        Queue<IDNData> receivedDataQueue = new Queue<IDNData>();
        private void Reset()
        {
            localPort = IDNData.IDNVAL_HELLO_UDP_PORT;
        }
        protected override void OnReadPacket(byte[] buffer, int length, IPEndPoint source)
        {
            if (0 < receiveLimit && receivedDataQueue.Count < receiveLimit)
            {
                var idn = IDNData.Parse(buffer);
                lock (receivedDataQueue)
                    receivedDataQueue.Enqueue(idn);
            }
        }
        private void Update()
        {
            lock (receivedDataQueue)
                while (0 < receivedDataQueue.Count)
                {
                    var data = receivedDataQueue.Dequeue();
                    onDataReceived.Invoke(data);
                }
        }

        [System.Serializable]
        public class IDNDataEvent : UnityEvent<IDNData> { }
    }
}
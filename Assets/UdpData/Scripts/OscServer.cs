using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;
using UnityEngine.Events;
using Osc;

public class OscServer : UdpServer
{
    public OscMsgEvent onDataReceived;
    public List<OscAddressEventPair> oscAddressEvents;

    Parser oscParser = new Parser();
    Queue<Message> receivedDataQueue = new Queue<Message>();

    protected override void OnReadPacket(byte[] buffer, int length, IPEndPoint source)
    {
        if (0 < receiveLimit && receivedDataQueue.Count < receiveLimit)
        {
            oscParser.FeedData(buffer, length);
            while (0 < oscParser.MessageCount)
            {
                var msg = oscParser.PopMessage();
                lock (receivedDataQueue)
                    receivedDataQueue.Enqueue(msg);
            }
        }
    }

    protected void Update()
    {
        lock (receivedDataQueue)
            while (0 < receivedDataQueue.Count)
            {
                var msg = receivedDataQueue.Dequeue();
                onDataReceived.Invoke(msg);
                var address = msg.path;
                var events = oscAddressEvents.Where(pair => pair.oscAddress == address).Select(pair => pair.oscEvent);
                foreach (var e in events)
                    e.Invoke(msg.data);
            }
    }

    [System.Serializable]
    public class OscMsgEvent : UnityEvent<Message> { }
    [System.Serializable]
    public class OscDataEvent : UnityEvent<object[]> { }
    [System.Serializable]
    public struct OscAddressEventPair
    {
        public string label;
        public string oscAddress;
        public OscDataEvent oscEvent;
    }
}

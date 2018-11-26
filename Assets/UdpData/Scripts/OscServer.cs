using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.Events;
using Osc;

public class OscServer : UdpServer {

    public OscMsgEvent onOsc;

    Parser oscParser = new Parser();
    Queue<Message> receivedOscMsgQueue = new Queue<Message>();

    protected override void Update()
    {
        base.Update();
        lock (receivedOscMsgQueue)
            while (0 < receivedOscMsgQueue.Count)
                onOsc.Invoke(receivedOscMsgQueue.Dequeue());
    }

    protected override void OnReadPacket(byte[] data, IPEndPoint source)
    {
        base.OnReadPacket(data, source);
        oscParser.FeedData(data, data.Length);
        while(0 < oscParser.MessageCount)
        {
            var msg = oscParser.PopMessage();
            lock (receivedOscMsgQueue)
                receivedOscMsgQueue.Enqueue(msg);
        }
    }

    [System.Serializable]
    public class OscMsgEvent : UnityEvent<Message> { }
}

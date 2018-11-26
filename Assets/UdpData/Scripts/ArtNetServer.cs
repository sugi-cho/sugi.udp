using System.Collections.Generic;
using System.Net;
using UnityEngine.Events;
using ArtNet.IO;
using ArtNet.Packets;

public class ArtNetServer : UdpServer {

    public ArtNetEvent onArtNet;

    Queue<ArtNetPacket> receivedArtNetQueue = new Queue<ArtNetPacket>();

    private void Reset()
    {
        localPort = 6454;
    }

    protected override void Update()
    {
        base.Update();
        lock (receivedArtNetQueue)
            while (0 < receivedArtNetQueue.Count)
                onArtNet.Invoke(receivedArtNetQueue.Dequeue());
    }

    protected override void OnReadPacket(byte[] data,  IPEndPoint source)
    {
        base.OnReadPacket(data, source);
        var recieveState = new ArtNetRecieveData(data);
        var artNetPacket = ArtNetPacket.Create(recieveState);

        lock (receivedArtNetQueue)
            receivedArtNetQueue.Enqueue(artNetPacket);
    }

    [System.Serializable]
    public class ArtNetEvent : UnityEvent<ArtNetPacket> { }
}

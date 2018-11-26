using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ArtNet.Packets;
using ArtNet.Enums;

public class FetchArtNet : MonoBehaviour {

    public byte[] dmxData;

    public void OnArtNet(ArtNetPacket packet)
    {
        if (packet.OpCode == ArtNetOpCodes.Dmx)
            dmxData = ((ArtNetDmxPacket)packet).DmxData;
    }
}

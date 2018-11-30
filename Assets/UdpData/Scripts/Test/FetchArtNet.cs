using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ArtNet.Packets;
using ArtNet.Enums;

public class FetchArtNet : MonoBehaviour {

    public byte[] dmxData;
    public ArtNetOpCodes opCode;
    public string protocol;

    public void OnArtNet(ArtNetPacket packet)
    {
        opCode = packet.OpCode;
        protocol = packet.Protocol;

        if (opCode == ArtNetOpCodes.Dmx)
            dmxData = ((ArtNetDmxPacket)packet).DmxData;
    }
}

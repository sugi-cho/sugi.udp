using UnityEngine;
using ArtNet.Packets;
using ArtNet.Enums;

namespace sugi.cc.udp.artnet
{
    public class FetchArtNet : MonoBehaviour
    {

        public ArtNetOpCodes opCode;
        public string protocol;
        public short universe;
        public byte[] dmxData;

        public void OnArtNet(ArtNetPacket packet)
        {
            opCode = packet.OpCode;
            protocol = packet.Protocol;

            if (opCode == ArtNetOpCodes.Dmx)
            {
                var dmxPacket = (ArtNetDmxPacket)packet;
                universe = dmxPacket.Universe;
                dmxData = dmxPacket.DmxData;
            }
        }
    }
}
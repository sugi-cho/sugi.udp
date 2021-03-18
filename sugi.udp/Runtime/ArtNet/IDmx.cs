using UnityEngine;

namespace sugi.cc.udp.artnet
{
    public interface IDmxDevice
    {
        void Initialize(short universe, int channel);
        int NumChannels { get; }
    }
    public interface IDmxInitialize
    {
        void DmxInitialize(Texture dmxBuffer);
    }
    public interface IDmxUpdate
    {
        short Universe { get; }
        void DmxUpdate(byte[] dmx);
    }
}
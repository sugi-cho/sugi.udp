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
    public interface IDmxOutputModule
    {
        int StartChannel { get; }
        int NumChannels { get; }
        void SetChannel(int channel);
        void SetDmx(ref byte[] dmx);
    }
}
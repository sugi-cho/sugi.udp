using UnityEngine;
using sugi.cc.udp.artnet;

[RequireComponent(typeof(Light))]
public class SimpleDMXLight : MonoBehaviour, IDmxDevice, IDmxUpdate
{
    public short Universe => universe;
    public int NumChannels => 4;

    [SerializeField] short universe;
    [SerializeField] int channel;
    public void Initialize(short universe, int channel)
    {
        this.universe = universe;
        this.channel = channel;
    }

    new Light light;
    int ChRed => channel + 0;
    int ChGreen => channel + 1;
    int ChBlueB => channel + 2;
    int ChDimmer => channel + 3;

    public void DmxUpdate(byte[] dmx)
    {
        var color = light.color;

        color.r = dmx[ChRed] / 255f;
        color.g = dmx[ChGreen] / 255f;
        color.b = dmx[ChBlueB] / 255f;

        light.color = color;
        light.intensity = dmx[ChDimmer] / 255f * 2f;
    }

    private void Start()
    {
        light = GetComponent<Light>();
    }
}

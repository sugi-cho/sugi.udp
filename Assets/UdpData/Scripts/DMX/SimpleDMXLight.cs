using UnityEngine;

[RequireComponent(typeof(Light))]
public class SimpleDMXLight : DMXDevice
{
    public override int NumChannels
    {
        get
        {
            return 4;
        }
    }
    public float whitePower = 0.5f;

    new Light light;
    int channelR;
    int channelG;
    int channelB;
    int channelW;

    private void Reset()
    {
        channelR = 0;
        channelG = 1;
        channelB = 2;
        channelW = 3;
    }

    protected override void OnDmxData()
    {
        var color = light.color;

        color.r = dmxData[channelR] / 256f;
        color.g = dmxData[channelG] / 256f;
        color.b = dmxData[channelB] / 256f;
        color += Color.white * whitePower * dmxData[channelW] / 256f;

        light.color = color;
    }

    private void Start()
    {
        light = GetComponent<Light>();
    }
}

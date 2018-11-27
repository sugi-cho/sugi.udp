using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Threading;

public class SendMassDataTest : MonoBehaviour
{

    public UdpSender sender;

    public int numData = 100;
    public int sendFPS = 60;
    public int dataSize = 530;

    public bool send;

    Thread dataSender;
    byte[] data;

    // Use this for initialization
    void Start()
    {
        data = new byte[dataSize];
    }

    private void OnDestroy()
    {
        if (dataSender != null)
            dataSender.Abort();
        dataSender = null;
    }

    void Update()
    {
        var ms = (Time.time * 1000).ToString("0000000000");
        var bytes = System.Text.Encoding.UTF8.GetBytes(ms);
        System.Buffer.BlockCopy(bytes, 0, data, 0, bytes.Length);

        if (send)
            for (var i = 0; i < numData; i++)
                sender.Send(data);
    }
}

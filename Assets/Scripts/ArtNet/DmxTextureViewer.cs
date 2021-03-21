using UnityEngine;

using sugi.cc.udp.artnet;

public class DmxTextureViewer : MonoBehaviour, IDmxInitialize
{
    public string propName = "_DmxTes";
    void IDmxInitialize.DmxInitialize(Texture dmxBuffer)
    {
        GetComponent<Renderer>().material.SetTexture(propName, dmxBuffer);
    }
}

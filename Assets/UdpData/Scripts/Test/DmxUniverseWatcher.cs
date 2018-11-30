using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DmxUniverseWatcher : MonoBehaviour {
    
    public byte[] dmxData;

    public void GetDmxData(byte[] dmxData)
    {
        this.dmxData = dmxData;
    }
}

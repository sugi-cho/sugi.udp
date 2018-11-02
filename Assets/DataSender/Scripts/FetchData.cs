using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FetchData : MonoBehaviour {

    public byte[] dataFetched;

    public void OnUdpData(byte[] data)
    {
        dataFetched = data;
    }
}

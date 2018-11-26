using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FetchOsc : MonoBehaviour {

    public string oscMsg;
    public void OnOsc(Osc.Message msg)
    {
        oscMsg = msg.ToString();
    }
}

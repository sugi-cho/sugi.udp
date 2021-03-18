using UnityEngine;

namespace sugi.cc.udp.osc
{
    public class FetchOsc : MonoBehaviour
    {

        public string oscMsg;
        public void OnOsc(Osc.Message msg)
        {
            oscMsg = msg.ToString();
        }
    }
}
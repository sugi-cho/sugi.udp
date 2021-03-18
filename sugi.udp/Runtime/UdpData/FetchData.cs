using UnityEngine;

namespace sugi.cc.udp
{
    public class FetchData : MonoBehaviour
    {

        public byte[] dataFetched;

        public void OnUdpData(byte[] data)
        {
            dataFetched = data;
        }
    }
}
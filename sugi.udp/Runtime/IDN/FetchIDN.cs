using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using ilda.digital;

namespace sugi.cc.udp.idn
{
    public class FetchIDN : MonoBehaviour
    {
        [SerializeField] IDNData idn;

        public void OnIdn(IDNData data) => idn = data;

        private void OnDrawGizmosSelected()
        {
            if (idn != null && idn.Command == IDNData.IDNCMD.CNLMSG)
            {
                var samples = idn.FrameSamples;
                for (var i = 0; i < samples.Length - 1; i++)
                {
                    Gizmos.color = samples[i].color;
                    var from = samples[i].position / ushort.MaxValue;
                    var to = samples[i + 1].position / ushort.MaxValue;
                    Gizmos.DrawLine(from, to);
                }
            }
        }
    }
}
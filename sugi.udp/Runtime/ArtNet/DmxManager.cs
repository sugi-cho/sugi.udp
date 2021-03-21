using System.Linq;
using System.Net;
using UnityEngine;

using ArtNet.Packets;

namespace sugi.cc.udp.artnet
{
    [RequireComponent(typeof(ArtNetServer))]
    public class DmxManager : MonoBehaviour
    {
        const int Port = 6454;

        public ArtNetServer server;

        IDmxInitialize[] dmxInitialize;
        IDmxUpdate[] dmxUpdate;

        [Header("send dmx")]
        public bool useBroadcast;
        public string remoteIP = "localhost";
        IPEndPoint remote;

        [Header("send/recieved DMX data for debug")]
        [SerializeField] ArtNetDmxPacket dmxToSend;

        [SerializeField] Texture2D dmxDataTex;
        byte[] dmxDataTexRawData;
        byte[][] dmx;

        public void Send(short universe, byte[] dmxData)
        {
            dmxToSend.Universe = universe;
            System.Buffer.BlockCopy(dmxData, 0, dmxToSend.DmxData, 0, dmxData.Length);
            UdpSender.Send(dmxToSend.ToArray(), remote);
        }

        public void OnDmxData(short universe, byte[] dmxData)
        {
            if (dmx[universe] == null)
                dmx[universe] = new byte[512];
            System.Buffer.BlockCopy(dmxData, 0, dmx[universe], 0, 512);
            System.Buffer.BlockCopy(dmxData, 0, dmxDataTexRawData, universe * 512, 512);
        }

        void OnEnable()
        {
            dmxToSend.DmxData = new byte[512];
            if (!useBroadcast)
                remote = new IPEndPoint(UdpSender.FindFromHostName(remoteIP), Port);
            else
                remote = new IPEndPoint(IPAddress.Broadcast, Port);

            CreateInitialize();
            CreateUpdate();
        }
        void CreateInitialize()
        {
            dmxDataTex = new Texture2D(512, 512, TextureFormat.R8, false);
            dmxDataTexRawData = new byte[512 * 512];

            dmxInitialize = FindObjectsOfType<GameObject>().SelectMany(go => go.GetComponents<IDmxInitialize>()).ToArray();
            foreach (var i in dmxInitialize)
                i.DmxInitialize(dmxDataTex);
        }
        void CreateUpdate()
        {
            dmx = new byte[512][];
            dmxUpdate = FindObjectsOfType<GameObject>().SelectMany(go => go.GetComponents<IDmxUpdate>()).ToArray();
        }

        private void Update()
        {
            for(var i = 0; i < dmxUpdate.Length; i++)
            {
                var data = dmx[dmxUpdate[i].Universe];
                if (data != null)   
                    dmxUpdate[i].DmxUpdate(data);
            }

            dmxDataTex.LoadRawTextureData(dmxDataTexRawData);
            dmxDataTex.Apply();
        }
    }
}
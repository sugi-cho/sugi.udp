using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine.Events;
using ArtNet.IO;
using ArtNet.Packets;

namespace sugi.cc.udp.artnet
{
    public class ArtNetServer : UdpServer
    {
        public ArtNetEvent onDataReceived;
        public UniverseDmxDataEvent onDmxData;
        public DmxMergeMode dmxMergeMode;

        byte[][] dmx;

        Queue<ArtNetPacket> receivedDataQueue = new Queue<ArtNetPacket>();
        Queue<short> updateUniverseQueue = new Queue<short>();

        private void Reset()
        {
            localPort = 6454;
        }

        ArtNetRecieveData recieveState;
        protected override void OnReadPacket(byte[] buffer, int length, IPEndPoint source)
        {
            if (recieveState == null)
                recieveState = new ArtNetRecieveData();
            recieveState.buffer = buffer;
            recieveState.DataLength = length;

            var artNetPacket = ArtNetPacket.Create(recieveState);

            if (0 < receiveLimit && receivedDataQueue.Count < receiveLimit)
            {
                lock (receivedDataQueue)
                    receivedDataQueue.Enqueue(artNetPacket);
                if (artNetPacket.OpCode == ArtNet.Enums.ArtNetOpCodes.Dmx)
                {
                    var dmxPacket = (ArtNetDmxPacket)artNetPacket;
                    var universe = dmxPacket.Universe;
                    var dmxData = dmxPacket.DmxData;
                    var mergedDmxData = DmxMerge.GetMergedDmxData(universe, dmxData, dmxMergeMode);

                    if (dmx == null)
                        dmx = new byte[512][];
                    if (dmx[universe] == null)
                        dmx[universe] = new byte[512];
                    System.Buffer.BlockCopy(mergedDmxData, 0, dmx[universe], 0, mergedDmxData.Length);

                    if (!updateUniverseQueue.Contains(universe))
                        lock (updateUniverseQueue)
                            updateUniverseQueue.Enqueue(universe);
                }
            }
        }

        protected void Update()
        {
            lock (receivedDataQueue)
                while (0 < receivedDataQueue.Count)
                    onDataReceived.Invoke(receivedDataQueue.Dequeue());
            lock (updateUniverseQueue)
            {
                while (0 < updateUniverseQueue.Count)
                {
                    var universe = updateUniverseQueue.Dequeue();
                    var dmxData = dmx[universe];
                    onDmxData.Invoke(universe, dmxData);
                }
                if (dmxMergeMode == DmxMergeMode.HTP)
                    DmxMerge.ClearForHTP();
            }
        }

        public enum DmxMergeMode
        {
            HTP,
            LTP,
        }

        class DmxMerge
        {
            static Dictionary<int, DmxMerge> UniverseMergeMap = new Dictionary<int, DmxMerge>();
            static byte[] zeroDmxData = Enumerable.Repeat((byte)0, 512).ToArray();
            byte[] mergedDmxData = new byte[512];

            public static byte[] GetMergedDmxData(int universe, byte[] dmxData, DmxMergeMode mergeMode)
            {
                if (!UniverseMergeMap.ContainsKey(universe))
                    UniverseMergeMap.Add(universe, new DmxMerge());
                var merger = UniverseMergeMap[universe];

                if (mergeMode == DmxMergeMode.HTP)
                    return merger.GetMergedDmxHTP(dmxData);
                else
                    return merger.GetMergedDmxLTP(dmxData);
            }

            public static void ClearForHTP()
            {
                lock (UniverseMergeMap)
                    foreach (var merge in UniverseMergeMap.Values)
                        merge.ClearDmxData();
            }
            void ClearDmxData()
            {
                System.Buffer.BlockCopy(zeroDmxData, 0, mergedDmxData, 0, 512);
            }

            byte[] GetMergedDmxHTP(byte[] dmxData)
            {
                lock (mergedDmxData)
                {
                    for (var i = 0; i < mergedDmxData.Length; i++)
                        dmxData[i] = System.Math.Max(mergedDmxData[i], dmxData[i]);
                    System.Buffer.BlockCopy(dmxData, 0, mergedDmxData, 0, dmxData.Length);
                    return mergedDmxData;
                }
            }

            byte[] GetMergedDmxLTP(byte[] dmxData)
            {
                lock (mergedDmxData)
                {
                    System.Buffer.BlockCopy(dmxData, 0, mergedDmxData, 0, dmxData.Length);

                    return mergedDmxData;
                }
            }
        }

        [System.Serializable]
        public class ArtNetEvent : UnityEvent<ArtNetPacket> { }
        [System.Serializable]
        public class UniverseDmxDataEvent : UnityEvent<short, byte[]> { }
    }
}
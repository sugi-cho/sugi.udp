using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine.Events;
using ArtNet.IO;
using ArtNet.Packets;

public class ArtNetServer : UdpServer
{
    public ArtNetEvent onDataReceived;
    public List<DmxUniverseEventPair> dmxUniverseEvents;
    public DmxMergeMode dmxMergeMode;

    Queue<ArtNetPacket> receivedDataQueue = new Queue<ArtNetPacket>();
    Dictionary<int, byte[]> universeDmxDataMap = new Dictionary<int, byte[]>();
    Queue<int> updateUniverseQueue = new Queue<int>();

    private void Reset()
    {
        localPort = 6454;
    }

    protected override void OnReadPacket(byte[] buffer, int length, IPEndPoint source)
    {
        var recieveState = new ArtNetRecieveData();
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
                var mergedDmxData = DmxMerge.GetMergedDmxData(universe, dmxData, dmxMergeMode, source);

                if (!universeDmxDataMap.ContainsKey(universe))
                    universeDmxDataMap.Add(universe, Enumerable.Repeat((byte)0, 512).ToArray());
                System.Buffer.BlockCopy(mergedDmxData, 0, universeDmxDataMap[universe], 0, mergedDmxData.Length);

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
                var events = dmxUniverseEvents.Where(pair => pair.universe == universe).Select(pair => pair.dmxEvent);
                var dmxData = universeDmxDataMap[universe];
                foreach (var e in events)
                    e.Invoke(dmxData);
                if (dmxMergeMode == DmxMergeMode.HTP)
                    DmxMerge.ClearForHTP();
            }
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
        byte[] mergedDmxData = new byte[512];
        Dictionary<IPEndPoint, byte[]> fromEPDmxDataMap = new Dictionary<IPEndPoint, byte[]>();

        public static byte[] GetMergedDmxData(int universe, byte[] dmxData, DmxMergeMode mergeMode, IPEndPoint fromEndPoint = null)
        {
            if (!UniverseMergeMap.ContainsKey(universe))
                UniverseMergeMap.Add(universe, new DmxMerge());
            var merger = UniverseMergeMap[universe];

            if (mergeMode == DmxMergeMode.HTP)
                return merger.GetMergedDmxHTP(dmxData);
            else
                return merger.GetMergedDmxLTP(dmxData, fromEndPoint);
        }

        public static void ClearForHTP()
        {
            foreach (var merge in UniverseMergeMap.Values)
                merge.ClearDmxData();
        }
        void ClearDmxData()
        {
            lock (mergedDmxData)
                for (var i = 0; i < mergedDmxData.Length; i++)
                    mergedDmxData[i] = 0;
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

        byte[] GetMergedDmxLTP(byte[] dmxData, IPEndPoint fromEndPoint)
        {
            lock (mergedDmxData)
            {
                bool[] changeFlg;
                if (!fromEPDmxDataMap.ContainsKey(fromEndPoint))
                {
                    fromEPDmxDataMap.Add(fromEndPoint, new byte[512]);
                    changeFlg = Enumerable.Repeat(true, 512).ToArray();
                }
                else
                {
                    var prevData = fromEPDmxDataMap[fromEndPoint];
                    changeFlg = prevData.Select((data, idx) => data != dmxData[idx]).ToArray();
                }
                for (var i = 0; i < mergedDmxData.Length; i++)
                    if (changeFlg[i])
                        mergedDmxData[i] = dmxData[i];
                System.Buffer.BlockCopy(dmxData, 0, fromEPDmxDataMap[fromEndPoint], 0, dmxData.Length);

                return mergedDmxData;
            }
        }
    }

    [System.Serializable]
    public class ArtNetEvent : UnityEvent<ArtNetPacket> { }
    [System.Serializable]
    public class DmxDataEvent : UnityEvent<byte[]> { }
    [System.Serializable]
    public struct DmxUniverseEventPair
    {
        public string label;
        public int universe;
        public DmxDataEvent dmxEvent;
    }
}

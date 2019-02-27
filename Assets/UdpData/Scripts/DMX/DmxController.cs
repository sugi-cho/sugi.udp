using System.Collections.Generic;
using System.Net;
using System.Linq;
using UnityEngine;

using ArtNet.Packets;

[RequireComponent(typeof(ArtNetServer))]
public class DmxController : MonoBehaviour
{
    public static DmxController Instance { get { if (_i == null) _i = FindObjectOfType<DmxController>(); return _i; } }
    static DmxController _i;
    const int Port = 6454;

    public ArtNetServer server;

    [Header("send dmx")]
    public bool useBroadcast;
    public string remoteIP = "localhost";
    IPEndPoint remote;

    [Header("dmx devices")]
    [SerializeField] List<DMXDevice> deviceList;
    Dictionary<int, List<DMXDevice>> universeDeviceListMap;

    [Header("send/recieved DMX data for debug")]
    [SerializeField] ArtNetDmxPacket dmxToSend;
    byte[] dmxDataToSend;

    Dictionary<int, byte[]> universeDmxMap { get { return server.UniverseDmxDataMap; } }
    public bool debugMode;

    Texture2D dmxDataTex;
    byte[] dmxDataTexRawData;

    public void AddDevice(DMXDevice device)
    {
        if (universeDeviceListMap == null)
            universeDeviceListMap = new Dictionary<int, List<DMXDevice>>();

        var universe = device.universe;
        if (!universeDeviceListMap.ContainsKey(universe))
            universeDeviceListMap.Add(universe, new List<DMXDevice>() { device });
        else if (!universeDeviceListMap[universe].Contains(device))
            universeDeviceListMap[universe].Add(device);
    }

    public void Send(short universe, byte[] dmxData)
    {
        dmxToSend.Universe = universe;
        System.Buffer.BlockCopy(dmxData, 0, dmxToSend.DmxData, 0, dmxData.Length);
        UdpSender.Send(dmxToSend.ToArray(), remote);
    }

    public void OnDmxData(int universe, byte[] dmxData)
    {
        var deviceList = universeDeviceListMap[universe];
        foreach (var d in deviceList)
            d.SetData(dmxData.Skip(d.startChannel).Take(d.NumChannels).ToArray());

        System.Buffer.BlockCopy(dmxData, 0, dmxDataTexRawData, (511 - universe) * 512, 512);
        texLoaded = false;
    }

    void Start()
    {
        //Build Dictionary
        foreach (var d in deviceList)
            AddDevice(d);

        dmxToSend.DmxData = new byte[512];

        if (!useBroadcast)
            remote = new IPEndPoint(UdpSender.FindFromHostName(remoteIP), Port);
        else
            remote = new IPEndPoint(IPAddress.Broadcast, Port);

        dmxDataTex = new Texture2D(512, 512, TextureFormat.R8, false);
        dmxDataTexRawData = new byte[512 * 512];
    }

    // デバッグでArtNetを送信するための処理
    #region send artNet to debug device
    Rect windowRect = new Rect(0, 0, 768, 1024);
    int selectingUnivIdx;
    int selectingDevice;
    bool controlAllDevices;
    bool showDmxDataTex;
    bool texLoaded;
    private void OnGUI()
    {
        if (!debugMode)
            return;
        windowRect = GUI.Window(GetInstanceID(), windowRect, OnGUIWindow, "DMX Controller");
    }
    void OnGUIWindow(int id)
    {
        if (dmxDataToSend == null || dmxDataToSend.Length < 512)
            dmxDataToSend = new byte[512];

        if (showDmxDataTex = GUILayout.Toggle(showDmxDataTex, "show DMX data texture?"))
        {
            if (!texLoaded)
            {
                dmxDataTex.LoadRawTextureData(dmxDataTexRawData);
                dmxDataTex.Apply();
                texLoaded = true;
            }
            GUILayout.Label(dmxDataTex);
        }

        GUILayout.Space(8);

        GUILayout.Label("Select Universe:");

        var univDeviceListPairs = universeDeviceListMap.OrderBy(pair => pair.Key);
        var idx = GUILayout.SelectionGrid(
            selectingUnivIdx,
            univDeviceListPairs.Select(u => string.Format("univ{0,000}:({1}devices)", u.Key, u.Value.Count)).ToArray(), 5);
        if (idx != selectingUnivIdx)
        {
            selectingUnivIdx = idx;
            selectingDevice = 0;
        }
        var univs = univDeviceListPairs.Select(pair => pair.Key).ToArray();
        var univ = univs[selectingUnivIdx];
        var deviceList = universeDeviceListMap[univ];
        if (universeDmxMap.ContainsKey(univ))
            System.Buffer.BlockCopy(universeDmxMap[univ], 0, dmxDataToSend, 0, 512);

        GUILayout.Space(8);

        controlAllDevices = GUILayout.Toggle(controlAllDevices, "control all devices in Universe");
        if (!controlAllDevices)
        {
            GUILayout.Label("Select Device:");
            selectingDevice = GUILayout.SelectionGrid(selectingDevice, deviceList.Select(d => d.name).ToArray(), 5);
        }
        else
            selectingDevice = 0;
        var device = deviceList[selectingDevice];

        if (device.OnGUIFunc())
        {
            if (controlAllDevices)
            {
                for (var i = 0; i < 512 / device.NumChannels; i++)
                    System.Buffer.BlockCopy(device.GetChangedData(), 0, dmxDataToSend, i * device.NumChannels, device.NumChannels);
            }
            else
                System.Buffer.BlockCopy(device.GetChangedData(), 0, dmxDataToSend, device.startChannel, device.NumChannels);
            Send((short)univ, dmxDataToSend);
        }
        GUI.DragWindow();
    }
    #endregion
}

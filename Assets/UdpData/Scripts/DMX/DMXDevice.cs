using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public abstract class DMXDevice : MonoBehaviour
{
    public string modelName;
    public byte[] dmxData;
    protected byte[] dmxToSend;

    byte[] preDmxData;
    bool[] changeFlg;
    protected bool HasChanged(int idx) { return changeFlg[idx]; }

    public bool autoSetToController;
    public int universe;
    public int startChannel;
    public abstract int NumChannels { get; }

    public void SetData(byte[] dmxData)
    {
        this.dmxData = dmxData;
        if (preDmxData == null)
            preDmxData = new byte[dmxData.Length];

        changeFlg = dmxData.Select((b, i) => b != preDmxData[i]).ToArray();
        OnDmxData();
        System.Buffer.BlockCopy(dmxData, 0, preDmxData, 0, dmxData.Length);
    }

    protected abstract void OnDmxData();

    // デバッグでArtNetを送信するための処理
    #region send artNet to debug device
    //値に変更があったか(bool)を返す
    protected Vector2 scrollPos;
    public virtual bool OnGUIFunc()
    {
        if (dmxData == null || dmxData.Length < NumChannels)
            dmxData = new byte[NumChannels];
        if (dmxToSend == null || dmxToSend.Length < NumChannels)
            dmxToSend = new byte[NumChannels];
        System.Buffer.BlockCopy(dmxData, 0, dmxToSend, 0, dmxData.Length);
        GUILayout.BeginVertical("box");
        scrollPos = GUILayout.BeginScrollView(scrollPos);
        GUILayout.Label(name);
        var changeFlg = false;
        for(var i=0;i < dmxToSend.Length; i++)
        {
            dmxToSend[i] = (byte)GUILayout.HorizontalSlider(dmxToSend[i], 0, 255);
            if (dmxToSend[i] != dmxData[i])
                changeFlg = true;
        }
        GUILayout.EndScrollView();
        GUILayout.EndVertical();
        return changeFlg;
    }
    public virtual byte[] GetChangedData() { return dmxToSend; }
    #endregion

    [System.Serializable]
    public class DmxEventTable
    {
        public byte minDmx;
        public byte maxDmx;
        public UnityEvent onDmx;

        public static UnityEvent PickEvent(DmxEventTable[] dmxEventTables, byte val)
        {
            var dmxEventTable = dmxEventTables.Where(det => det.minDmx <= val && val <= det.maxDmx).FirstOrDefault();
            return dmxEventTable.onDmx;
        }
    }
}

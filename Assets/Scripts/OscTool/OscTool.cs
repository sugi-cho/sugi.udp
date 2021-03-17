using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Osc;

using sugi.cc.udp;
using sugi.cc.udp.osc;

public class OscTool : MonoBehaviour
{
    public OscServer server;
    public UdpSender sender;
    public bool view;

    Rect windowRect;
    Vector2 scrollPos;

    string remoteIp;
    int remotePort;
    int localPort;
    int logCapacity;

    private void Start()
    {
        var w = Screen.width;
        var h = Screen.height;
        windowRect = Rect.MinMaxRect(w * 0.1f, h * 0.1f, w * 0.9f, h * 0.9f);

        remoteIp = sender.remoteIp;
        remotePort = sender.remotePort;
        localPort = server.localPort;
        logCapacity = server.logCapacity;
    }

    private void OnGUI()
    {
        if (view)
            windowRect = GUI.Window(GetInstanceID(), windowRect, OnGUIWindow, "OSC Tool");
    }

    string oscPath = "/address";
    string[] sortTypes = new[] { "date time", "path", "sourceIP" };
    string[] paramTypes = System.Enum.GetNames(typeof(DataType));
    int sortType;
    int intParam;
    float floatParam;
    bool boolParam;
    List<OscParam> oscParamList;

    void OnGUIWindow(int idx)
    {
        if (oscParamList == null)
            oscParamList = new List<OscParam>();

        GUILayout.BeginVertical("box");
        GUILayout.BeginVertical("box");
        OscParam paramForRemove = null;
        oscPath = GUILayout.TextField(oscPath);
        for (var i = 0; i < oscParamList.Count; i++)
        {
            var oscParam = oscParamList[i];
            GUILayout.BeginHorizontal();
            oscParam.type = (DataType)GUILayout.SelectionGrid((int)oscParam.type, paramTypes, paramTypes.Length);
            var param = GUILayout.TextField(oscParam.param, GUILayout.MinWidth(80));
            switch (oscParam.type)
            {
                case DataType.String:
                    oscParam.param = param;
                    break;
                case DataType.Int:
                    int.TryParse(oscParam.param, out intParam);
                    int.TryParse(param, out intParam);
                    oscParam.param = intParam.ToString();
                    break;
                case DataType.Float:
                    float.TryParse(oscParam.param, out floatParam);
                    float.TryParse(param, out floatParam);
                    oscParam.param = floatParam.ToString();
                    break;
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Remove"))
                paramForRemove = oscParam;
            GUILayout.EndHorizontal();
        }
        if (paramForRemove != null)
            oscParamList.Remove(paramForRemove);

        if (GUILayout.Button("Add Osc Data"))
            oscParamList.Add(new OscParam());
        GUILayout.EndVertical();

        GUILayout.Space(8);

        GUILayout.BeginHorizontal();
        if (SetIpField("Remote IP: ", ref remoteIp) || SetPortField("Remote Port", ref remotePort))
            sender.CreateRemoteEP(remoteIp, remotePort);
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Send OSC"))
        {
            var osc = new MessageEncoder(oscPath);
            foreach (var oscParam in oscParamList)
                switch (oscParam.type)
                {
                    case DataType.String:
                        osc.Add(oscParam.GetStringParam());
                        break;
                    case DataType.Int:
                        osc.Add(oscParam.GetIntParam());
                        break;
                    case DataType.Float:
                        osc.Add(oscParam.GetFloatParam());
                        break;
                }
            sender.Send(osc.Encode());
        }
        GUILayout.EndVertical();

        GUILayout.BeginVertical("box");
        GUILayout.Label("OSC Log");

        GUILayout.BeginHorizontal();
        if (SetPortField("Local Port: ", ref localPort))
            server.StartServer(localPort);
        GUILayout.Label("log capacity: ");
        if (int.TryParse(GUILayout.TextField(server.logCapacity.ToString()), out logCapacity))
            server.logCapacity = logCapacity;

        GUILayout.EndHorizontal();
        sortType = GUILayout.SelectionGrid(sortType, sortTypes, 3);
        var ordered = server.OscLog.OrderBy(log =>
        {
            switch (sortType)
            {
                case 0: return log.date;
                case 1: return log.oscMsg;
                case 2: return log.source;
            }
            return log.oscMsg;
        });

        scrollPos = GUILayout.BeginScrollView(scrollPos);
        lock (server.OscLog)
            foreach (var oscLog in ordered)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(oscLog.date);
                GUILayout.FlexibleSpace();
                GUILayout.Label(oscLog.oscMsg);
                GUILayout.FlexibleSpace();
                GUILayout.Label(oscLog.source);
                GUILayout.EndHorizontal();
            }

        GUILayout.EndScrollView();
        GUILayout.EndVertical();

        GUI.DragWindow();
    }

    bool SetIpField(string label, ref string ip)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label);
        var str = GUILayout.TextField(ip);
        GUILayout.EndHorizontal();

        if (str == ip)
            return false;
        ip = str;
        return true;
    }
    bool SetPortField(string label, ref int port)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label);
        var str = GUILayout.TextField(port.ToString());
        GUILayout.EndHorizontal();

        int i;
        if (int.TryParse(str, out i))
            if (i != port)
            {
                port = i;
                return true;
            }

        return false;
    }

    public class OscParam
    {
        public DataType type;
        public string param = "";

        public string GetStringParam()
        {
            return param;
        }
        public int GetIntParam()
        {
            return int.Parse(param);
        }
        public float GetFloatParam()
        {
            return float.Parse(param);
        }
    }
    public enum DataType
    {
        String = 0,
        Int,
        Float,
    }
}

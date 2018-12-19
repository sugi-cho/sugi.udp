using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using FileUtility;

[RequireComponent(typeof(AsyncRecordPlayer))]
public class AsyncRecorderApp : MonoBehaviour
{

    public int windowWidth = 768;
    public int windowHeight = 480;
    Rect windowRect;

    string fileName = "data";
    const string fileExtension = ".udp";
    AsyncRecordPlayer recorder;

    string error = "";

    string remoteIp;
    int remotePort;
    int localPort;

    public UdpSender sender;
    public UdpServer server;

    string parentFolder;
    bool hide;
    #region cll methods via osc
    public void RecStartOsc(object[] data)
    {
        var filePath = Path.Combine(parentFolder, fileName + fileExtension);
        if (!File.Exists(filePath))
            recorder.StartRecording();
    }

    public void PlayStartOsc(object[] data)
    {
        fileName = (string)data[0];
        var startTime = (float)data[1];
        var filePath = Path.Combine(parentFolder, fileName + fileExtension);
        if (File.Exists(filePath))
            recorder.Play(filePath, startTime);
    }

    public void StopOsc(object[] data)
    {
        recorder.Stop();
    }
    #endregion

    public void OnError(System.Exception e)
    {
        error = e.ToString();
    }

    // Use this for initialization
    void Start()
    {
        remoteIp = sender.remoteIp;
        remotePort = sender.remotePort;
        localPort = server.localPort;

        var x = Screen.width / 2f - windowWidth / 2f;
        var y = Screen.height / 2f - windowHeight / 2f;
        windowRect = new Rect(new Vector2(x, y), new Vector2(windowWidth, windowHeight));

        recorder = GetComponent<AsyncRecordPlayer>();

        parentFolder = Path.Combine(Application.streamingAssetsPath, "udpData");
        if (!Directory.Exists(parentFolder))
            Directory.CreateDirectory(parentFolder);
    }


    private void OnGUI()
    {
        windowRect = GUI.Window(GetInstanceID(), windowRect, OnGUIWindow, "UDP Recorder");
    }

    void OnGUIWindow(int id)
    {
        if (hide = GUILayout.Toggle(hide, "Hide GUI"))
            return;

        GUILayout.Space(4);

        GUILayout.Label("FolderPath:");
        if (GUILayout.Button(parentFolder))
            OpenInFileBrowser.Open(parentFolder);

        GUILayout.BeginHorizontal();
        GUILayout.Label("FileName: ");
        fileName = GUILayout.TextField(fileName, GUILayout.Width(320));
        GUILayout.Label(fileExtension);
        GUILayout.EndHorizontal();

        GUILayout.Space(16);

        var filePath = Path.Combine(parentFolder, fileName + fileExtension);
        var exist = File.Exists(filePath);

        if (exist && !recorder.playing && !recorder.recording)
        {
            recorder.playFilePath = filePath;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("/play"))
                recorder.Play();
            if (GUILayout.Button("create playData"))
                recorder.CreatePlayData();
            GUILayout.EndHorizontal();
        }
        else if (!exist && !recorder.recording)
        {
            recorder.recordFilePath = filePath;
            if (GUILayout.Button("/record"))
                recorder.StartRecording();
        }

        if ((recorder.playing || recorder.recording))
        {
            if (GUILayout.Button("/stop"))
            {
                if (recorder.recording)
                {
                    var array = fileName.Split('_');
                    var num = 0;
                    if (0 < array.Length && int.TryParse(array.Last(), out num))
                    {
                        array[array.Length - 1] = (num + 1).ToString();
                        fileName = string.Join("_", array);
                    }
                    else
                        fileName += "_0";
                }
                recorder.Stop();
            }
        }
        else
        {
            GUILayout.Space(16);
            GUILayout.BeginVertical("box");
            if (SetIpField("Remote IP: ", ref remoteIp) || SetPortField("Remote Port", ref remotePort))
                sender.CreateRemoteEP(remoteIp, remotePort);
            GUILayout.Space(8);
            if (SetPortField("Local Port: ", ref localPort))
                server.StartServer(localPort);
            GUILayout.EndVertical();
        }

        if (0 < error.Length)
        {
            var color = GUI.color;
            GUI.color = Color.red;
            GUILayout.Label(error);
            GUI.color = color;
        }

        GUILayout.Space(16);
        if (recorder.playing || recorder.recording)
        {
            var fileSize = (float)recorder.fileSize;
            var unit = "B";
            if (1024 < fileSize)
            {
                fileSize /= 1024;
                unit = "KB";
            }
            if (1024 < fileSize)
            {
                fileSize /= 1024;
                unit = "MB";
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label(recorder.playing ? "playing: " : "recording: ");
            GUILayout.Label(recorder.time.ToString(), GUILayout.Width(200));
            GUILayout.FlexibleSpace();
            GUILayout.Label("file size: ");
            GUILayout.Label(fileSize.ToString(".000") + unit);
            GUILayout.EndHorizontal();
        }

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
}

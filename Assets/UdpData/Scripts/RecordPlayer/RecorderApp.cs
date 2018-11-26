using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using FileUtility;

[RequireComponent(typeof(UdpRecordPlayer))]
public class RecorderApp : MonoBehaviour
{

    public int windowWidth = 640;
    public int windowHeight = 768;
    Rect windowRect;

    string fileName = "data";
    const string fileExtension = ".udp";
    UdpRecordPlayer recorder;

    string error = "";

    public void OnError(System.Exception e)
    {
        error = e.ToString();
    }

    // Use this for initialization
    void Start()
    {
        var x = Screen.width / 2f - windowWidth / 2f;
        var y = Screen.height / 2f - windowHeight / 2f;
        windowRect = new Rect(new Vector2(x, y), new Vector2(windowWidth, windowHeight));

        recorder = GetComponent<UdpRecordPlayer>();
    }


    private void OnGUI()
    {
        windowRect = GUI.Window(GetInstanceID(), windowRect, OnGUIWindow, "UDP Recorder");
    }

    void OnGUIWindow(int id)
    {
        var parent = Path.Combine(Application.streamingAssetsPath, "udpData");
        if (!Directory.Exists(parent))
            Directory.CreateDirectory(parent);

        GUILayout.Label("Folder Path:");
        if (GUILayout.Button(parent))
            OpenInFileBrowser.Open(parent);

        GUILayout.BeginHorizontal();
        GUILayout.Label("FileName: ");
        fileName = GUILayout.TextField(fileName, GUILayout.Width(320));
        GUILayout.Label(fileExtension);
        GUILayout.EndHorizontal();

        GUILayout.Space(16);

        var filePath = Path.Combine(parent, fileName + fileExtension);
        var exist = File.Exists(filePath);

        if (exist && !recorder.playing)
        {
            recorder.playFilePath = filePath;
            if (GUILayout.Button("/play"))
                recorder.Play();
        }
        else if (!exist && !recorder.recording)
        {
            recorder.recordFilePath = filePath;
            if (GUILayout.Button("/record"))
                recorder.StartRecording();
        }

        if ((recorder.playing || recorder.recording) && GUILayout.Button("/stop"))
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

        if(0 < error.Length)
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
            if(1024 < fileSize)
            {
                fileSize /= 1024;
                unit = "KB";
            }
            if(1024 < fileSize)
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
}

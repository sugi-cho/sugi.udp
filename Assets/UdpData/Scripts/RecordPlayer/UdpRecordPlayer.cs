using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using UnityEngine;

public class UdpRecordPlayer : MonoBehaviour
{
    public UdpSender sender;
    Queue<TimeDataPair> recordQueue = new Queue<TimeDataPair>();
    public string recordFilePath = "udpRec.data";
    public string playFilePath = "recordedUdp/recorded.udp";

    Thread recorder;
    Thread player;
    float startTime;
    public float time;

    public bool recording { get; private set; }
    public bool playing { get; private set; }
    public long fileSize { get; private set; }
    public string exception;

    [Header("field for debug")]
    public TimeDataPair[] playData;

    private void OnDestroy()
    {
        Stop();
    }

    public void EnqueueData(byte[] data)
    {
        if (!recording)
            return;

        recordQueue.Enqueue(new TimeDataPair() { time = Time.time - startTime, data = data });
    }
    public void CreatePlayData()
    {
        var list = new List<TimeDataPair>();
        if (File.Exists(playFilePath))
        {
            var fileData = File.ReadAllBytes(playFilePath);
            using (var stream = new MemoryStream(fileData))
            using (var reader = new BinaryReader(stream))
            {
                var enl = false;
                while (!enl)
                {
                    try
                    {
                        var time = reader.ReadSingle();
                        var count = reader.ReadInt32();
                        var data = reader.ReadBytes(count);
                        list.Add(new TimeDataPair() { time = time, data = data });
                    }
                    catch(EndOfStreamException)
                    {
                        enl = true;
                    }
                }
            }
            playData = list.ToArray();
        }
    }

    [ContextMenu("Start Recording")]
    public void StartRecording()
    {
        recording = true;
        startTime = Time.time;
        recordQueue.Clear();

        if (recorder != null)
            recorder.Abort();
        recorder = new Thread(RecordLoop);
        recorder.Start();
    }
    [ContextMenu("Play RecordedData")]
    public void Play()
    {
        playing = true;
        startTime = Time.time;
        time = 0;

        if (player != null)
            player.Abort();
        player = new Thread(PlayLoop);
        player.Start();
    }

    public void Play(string filePath, float startTime)
    {
        playing = true;
        startTime = Time.time - startTime;
        time = startTime;

        playFilePath = filePath;
        if (!File.Exists(filePath))
            return;

        if (player != null)
            player.Abort();
        player = new Thread(PlayLoop);
        player.Start();
    }

    [ContextMenu("Stop Rec-Play")]
    public void Stop()
    {
        recording = false;
        playing = false;
        if (recorder != null)
            recorder.Abort();
        recorder = null;
        if (player != null)
            player.Abort();
        player = null;
    }

    private void Update()
    {
        if (playing || recording)
            time = Time.time - startTime;
    }

    void RecordLoop()
    {
        using (var stream = new FileStream(recordFilePath, FileMode.Create))
        using (var writer = new BinaryWriter(stream))
        {
            while (recording)
                try
                {
                    lock (recordQueue)
                        while (0 < recordQueue.Count)
                        {
                            var pair = recordQueue.Dequeue();
                            if (pair.data != null)
                            {
                                writer.Write(pair.time);
                                writer.Write(pair.data.Length);
                                writer.Write(pair.data);
                                fileSize = stream.Length;
                            }
                        }
                }
                catch (System.Exception e)
                {
                    exception = e.ToString();
                }

            writer.Close();
            stream.Close();
        }
    }

    void PlayLoop()
    {
        var recordedData = File.ReadAllBytes(playFilePath);
        using (var stream = new MemoryStream(recordedData))
        using (var reader = new BinaryReader(stream))
        {
            fileSize = stream.Length;
            while (playing)
            {
                try
                {
                    var nextTime = reader.ReadSingle();
                    var count = reader.ReadInt32();
                    if (0 < count)
                    {
                        var data = reader.ReadBytes(count);
                        while (time < nextTime && playing)
                        {

                        }
                        sender.Send(data);
                    }
                    else
                        playing = false;
                }
                catch (EndOfStreamException)
                {
                    playing = false;
                    reader.Close();
                    stream.Close();
                    return;
                }
                catch (System.Exception e)
                {
                    exception = e.ToString();
                }
            }
            reader.Close();
            stream.Close();
        }
    }

    [System.Serializable]
    public struct TimeDataPair
    {
        public float time;
        public byte[] data;
    }
}

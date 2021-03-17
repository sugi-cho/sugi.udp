using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.IO;
using UnityEngine;

using sugi.cc.udp;

public class AsyncRecordPlayer : UdpServer
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

    protected override void OnReadPacket(byte[] buffer, int length, IPEndPoint source)
    {
        if (recording)
        {
            var data = new byte[length];
            System.Buffer.BlockCopy(buffer, 0, data, 0, length);
            lock (recordQueue)
            {
                if (0 < receiveLimit &&  recordQueue.Count < receiveLimit)
                    recordQueue.Enqueue(new TimeDataPair() { time = GetCurrentTime() - startTime, data = data });
            }
        }
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
                    catch (EndOfStreamException)
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
        Stop();

        recording = true;
        startTime = GetCurrentTime();

        lock(recordQueue)
            recordQueue.Clear();

        recorder = new Thread(RecordLoop);
        recorder.Start();
    }

    [ContextMenu("Play RecordedData")]
    public void Play()
    {
        Stop();

        playing = true;
        startTime = GetCurrentTime();
        time = 0;

        player = new Thread(PlayLoop);
        player.Start();
    }

    [ContextMenu("Stop Rec-Play")]
    public void Stop()
    {
        recording = false;
        playing = false;
        if (recorder != null)
        {
            recorder.Abort();
            recorder.Join();
            recorder = null;
        }
        if (player != null)
        {
            player.Abort();
            player.Join();
            player = null;
        }
    }

    public void Play(string filePath, float playStartTime)
    {
        Stop();

        playing = true;
        startTime = GetCurrentTime() - playStartTime;
        time = playStartTime;

        playFilePath = filePath;
        if (!File.Exists(filePath))
            return;

        player = new Thread(PlayLoop);
        player.Start();
    }

    private void Update()
    {
        if (playing || recording)
            time = GetCurrentTime() - startTime;
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
                    {
                        if (0 < recordQueue.Count)
                        {
                            var pair = recordQueue.Dequeue();
                            if (pair.data != null)
                            {
                                writer.Write(pair.time);
                                writer.Write(pair.data.Length);
                                writer.Write(pair.data);
                            }
                        }
                    }
                    fileSize = stream.Length;
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
            var startTime = time;
            while (playing)
            {
                try
                {
                    var nextTime = reader.ReadSingle();
                    var count = reader.ReadInt32();
                    while(nextTime < startTime) {
                        reader.ReadBytes(count);
                        nextTime = reader.ReadSingle();
                        count = reader.ReadInt32();
                    }


                    if (0 < count)
                    {
                        var data = reader.ReadBytes(count);
                        {
                            while (time < nextTime && playing)
                            {

                            }
                            sender.Send(data);
                        }
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

    static float GetCurrentTime()
    {
        return (float)Environment.TickCount / 1000.0f;
    }

    [System.Serializable]
    public struct TimeDataPair
    {
        public float time;
        public byte[] data;
    }
}

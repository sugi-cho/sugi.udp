using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using UnityEngine;

public class UdpRecordPlayer : MonoBehaviour
{
    public UdpSender sender;
    Queue<byte[]> recordQueue = new Queue<byte[]>();
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


    private void OnDestroy()
    {
        Stop();
    }

    public void EnqueueData(byte[] data)
    {
        if (!recording)
            return;
        recordQueue.Enqueue(data);
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
        fileSize = 0;
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream))
        {
            while (recording)
                try
                {
                    lock (recordQueue)
                        while (0 < recordQueue.Count)
                        {
                            var data = recordQueue.Dequeue();
                            if (data != null)
                            {
                                writer.Write(time);
                                writer.Write(data.Length);
                                writer.Write(data);
                                fileSize = stream.Length;
                            }
                        }
                    Thread.Sleep(0);
                }
                catch (System.Exception e)
                {
                    exception = e.ToString();
                }
            stream.Flush();
            File.WriteAllBytes(recordFilePath, stream.GetBuffer());

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
                    var data = reader.ReadBytes(count);
                    while (time < nextTime && playing)
                    {
                        Thread.Sleep(0);
                    }
                    sender.Send(data);
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
}

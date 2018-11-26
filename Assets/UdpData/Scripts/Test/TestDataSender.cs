using UnityEngine;

public class TestDataSender : MonoBehaviour
{
    public UdpSender sender;
    public TestData dataForSend;
    public TestData receivedData;

    public void ReceiveData(byte[] data)
    {
        var json = System.Text.Encoding.UTF8.GetString(data);
        JsonUtility.FromJsonOverwrite(json, receivedData);
    }

    [ContextMenu("send test data")]
    public void SendData()
    {
        sender.Send(dataForSend);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            SendData();
    }

    [System.Serializable]
    public class TestData
    {
        public string name;
        public int intData;
        public float[] values;
        public Vector2Int[] pixelPoses;
    }
}

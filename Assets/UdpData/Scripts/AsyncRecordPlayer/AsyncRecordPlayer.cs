using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class AsyncRecordPlayer : UdpServer {

    protected override void OnReadPacket(byte[] buffer, int length, IPEndPoint source)
    {
        //ここでUDPレコードの処理をする
    }

}

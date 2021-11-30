using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        NetManager.AddEventListener(NetManager.NetEvent.ConnectSucc, OnConnectSucc);
        NetManager.AddEventListener(NetManager.NetEvent.ConnectFail, OnConnectFail);
        NetManager.AddEventListener(NetManager.NetEvent.Close, OnConnectClose);
    }

    private void OnConnectClose(string err)
    {
        Debug.Log("OnConnectClose");
    }

    private void OnConnectFail(string err)
    {
        Debug.Log($"OnConnectFail: {err}");
    }

    private void OnConnectSucc(string err)
    {
        Debug.Log("OnConnectSucc");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnConnectClick()
    {
        NetManager.Connect("127.0.0.1", 8888);
    }
    public void OnCloseClick()
    {
        NetManager.Close();
    }

    public void OnMoveClick()
    {
        MsgMove msg = new MsgMove();
        msg.x = 120;
        msg.y = 123;
        msg.z = -6;

        NetManager.Send(msg);
    }
}

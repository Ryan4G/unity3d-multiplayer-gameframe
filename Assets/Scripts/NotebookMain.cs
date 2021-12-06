using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NotebookMain : MonoBehaviour
{

    public InputField idInput;
    public InputField pwInput;
    public InputField textInput;

    // Start is called before the first frame update
    void Start()
    {
        NetManager.AddEventListener(NetManager.NetEvent.ConnectSucc, OnConnectSucc);
        NetManager.AddEventListener(NetManager.NetEvent.ConnectFail, OnConnectFail);
        NetManager.AddEventListener(NetManager.NetEvent.Close, OnConnectClose);

        NetManager.AddMsgListener("MsgLogin", OnMsgLogin);
        NetManager.AddMsgListener("MsgRegister", OnMsgRegister);
        NetManager.AddMsgListener("MsgKick", OnMsgKick);
        NetManager.AddMsgListener("MsgGetText", OnMsgGetText);
        NetManager.AddMsgListener("MsgSaveText", OnMsgSaveText);
    }

    private void OnMsgSaveText(MsgBase msgBase)
    {
        MsgSaveText msg = (MsgSaveText)msgBase;

        if (msg.result == 1)
        {
            Debug.Log("Save Text Successful");
        }
        else
        {
            Debug.Log("Save Text fail");
        }
    }

    private void OnMsgGetText(MsgBase msgBase)
    {
        MsgGetText msg = (MsgGetText)msgBase;

        textInput.text = msg.text;
    }

    private void OnMsgKick(MsgBase msgBase)
    {
        Debug.Log("Your account was logon on other mechine...");
    }

    private void OnMsgRegister(MsgBase msgBase)
    {
        MsgRegister msg = (MsgRegister)msgBase;

        if (msg.result == 1)
        {
            Debug.Log("Register Successful");
        }
        else
        {
            Debug.Log("Register fail");
        }
    }

    private void OnMsgLogin(MsgBase msgBase)
    {
        MsgLogin msg = (MsgLogin)msgBase;

        if (msg.result == 1)
        {
            Debug.Log("Login Successful");

            MsgGetText msgGetText = new MsgGetText();

            NetManager.Send(msgGetText);
        }
        else
        {
            Debug.Log("Login fail");
        }
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
        NetManager.Update();
    }

    public void OnConnectClick()
    {
        NetManager.Connect("127.0.0.1", 8888);
    }

    public void OnCloseClick()
    {
        NetManager.Close();
    }

    public void OnRegisterClick()
    {
        MsgRegister msg = new MsgRegister();
        msg.id = idInput.text;
        msg.pw = pwInput.text;

        NetManager.Send(msg);
    }

    public void OnLoginClick()
    {
        MsgLogin msg = new MsgLogin();
        msg.id = idInput.text;
        msg.pw = pwInput.text;

        NetManager.Send(msg);
    }

    public void OnSaveClick()
    {
        MsgSaveText msg = new MsgSaveText();
        msg.text = textInput.text;

        NetManager.Send(msg);
    }
}

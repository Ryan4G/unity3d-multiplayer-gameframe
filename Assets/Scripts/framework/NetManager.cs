using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class NetManager
{
    static Socket socket;

    static ByteArray readBuff = new ByteArray();

    static Queue<ByteArray> writeQueue = new Queue<ByteArray>();

    static bool isConnecting = false;

    static bool isClosing = false;

    static List<MsgBase> msgList = new List<MsgBase>();

    static int msgCount = 0;

    readonly static int MAX_MESSAGE_FIRE = 10;

    public enum NetEvent
    {
        ConnectSucc = 1,
        ConnectFail = 2,
        Close = 3,
    }

    public delegate void EventListener(string err);

    private static Dictionary<NetEvent, EventListener> eventListeners = new Dictionary<NetEvent, EventListener>();

    public static void AddEventListener(NetEvent netEvent, EventListener listener)
    {
        if (eventListeners.ContainsKey(netEvent))
        {
            eventListeners[netEvent] += listener;
        }
        else
        {
            eventListeners[netEvent] = listener;
        }
    }

    public static void RemoveEventListener(NetEvent netEvent, EventListener listener)
    {
        if (eventListeners.ContainsKey(netEvent))
        {
            eventListeners[netEvent] -= listener;

            if (eventListeners[netEvent] == null)
            {
                eventListeners.Remove(netEvent);
            }
        }
    }

    public static void FireEvent(NetEvent netEvent, string err)
    {
        if (eventListeners.ContainsKey(netEvent))
        {
            eventListeners[netEvent].Invoke(err);
        }
    }

    public static void Connect(string ip, int port)
    {
        if (socket != null && socket.Connected)
        {
            Debug.Log("Connect fail, already connected!");
            return;
        }

        if (isConnecting)
        {
            Debug.Log("Connect fail, isConnecting");
            return;
        }

        InitState();

        socket.NoDelay = true;

        isConnecting = true;

        socket.BeginConnect(ip, port, ConnectCallback, socket);
    }

    private static void ConnectCallback(IAsyncResult ar)
    {
        try
        {
            Socket socket = ar.AsyncState as Socket;
            socket.EndConnect(ar);

            Debug.Log("Socket Connect succ ");
            FireEvent(NetEvent.ConnectSucc, "");
            isConnecting = false;

            socket.BeginReceive(readBuff.bytes, readBuff.writeIdx, readBuff.remain, 0, ReceiveCallback, socket);

        }
        catch (SocketException ex)
        {
            Debug.Log($"Socket Connect fail {ex} ");
            FireEvent(NetEvent.ConnectFail, ex.ToString());
            isConnecting = false;
        }
    }

    private static void ReceiveCallback(IAsyncResult ar)
    {
        try
        {
            Socket socket = ar.AsyncState as Socket;

            int count = socket.EndReceive(ar);

            if (count == 0)
            {
                Close();
                return;
            }

            readBuff.writeIdx += count;

            OnRecevieData();

            if (readBuff.remain < 8)
            {
                readBuff.MoveBytes();
                readBuff.Resize(readBuff.length * 2);
            }

            socket.BeginReceive(readBuff.bytes, readBuff.writeIdx, readBuff.remain, 0, ReceiveCallback, socket);
        }
        catch (SocketException ex)
        {
            Debug.Log($"Socket Receive fail : {ex}");
        }
    }

    private static void OnReceiveData()
    {
        // only length bytes
        if (readBuff.length <= 2)
        {
            return;
        }

        short bodyLength = readBuff.ReadInt16();

        // package is not completed
        if (readBuff.length < bodyLength)
        {
            readBuff.readIdx -= 2;
            return;
        }

        int nameCount = 0;
        string protoName = MsgBase.DecodeName(readBuff.bytes, readBuff.readIdx, out nameCount);

        if (string.IsNullOrEmpty(protoName))
        {
            Debug.Log("OnReceiveData MsgBase.DecodeName fail");
            return;
        }

        readBuff.readIdx += nameCount;

        int bodyCount = bodyLength - nameCount;
        MsgBase msgBase = MsgBase.Decode(protoName, readBuff.bytes, readBuff.readIdx, bodyCount);

        readBuff.readIdx += bodyCount;
        readBuff.CheckAndMoveBytes();

        lock (msgList)
        {
            msgList.Add(msgBase);
        }

        msgCount++;

        if (readBuff.length > 2)
        {
            // work utill return
            OnReceiveData();
        }
    }

    private static void InitState()
    {
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        readBuff = new ByteArray();

        writeQueue = new Queue<ByteArray>();

        isConnecting = false;

        isClosing = false;

        msgList = new List<MsgBase>();

        msgCount = 0;
    }

    public static void Close()
    {
        if (socket == null || !socket.Connected)
        {
            return;
        }

        if (isConnecting)
        {
            return;
        }

        if (writeQueue.Count > 0)
        {
            isClosing = true;
        }
        else
        {
            socket.Close();
            FireEvent(NetEvent.Close, "");
        }
    }

    public static void Send(MsgBase msg)
    {
        if (socket == null || !socket.Connected)
        {
            return;
        }

        if (isConnecting)
        {
            return;
        }

        if (isClosing)
        {
            return;
        }

        byte[] nameBytes = MsgBase.EncodeName(msg);
        byte[] bodyBytes = MsgBase.Encode(msg);

        int len = nameBytes.Length + bodyBytes.Length;
        byte[] sendBytes = new byte[2 + len];

        sendBytes[0] = (byte)(len % 256);
        sendBytes[1] = (byte)(len / 256);

        Array.Copy(nameBytes, 0, sendBytes, 2, nameBytes.Length);
        Array.Copy(bodyBytes, 0, sendBytes, 2 + nameBytes.Length, bodyBytes.Length);

        ByteArray ba = new ByteArray(sendBytes);
        int count = 0;

        lock (writeQueue)
        {
            writeQueue.Enqueue(ba);
            count = writeQueue.Count;
        }

        if (count == 1)
        {
            socket.BeginSend(sendBytes, 0, sendBytes.Length, 0, SendCallback, socket);
        }
    }

    private static void SendCallback(IAsyncResult ar)
    {
        try
        {
            Socket socket = ar.AsyncState as Socket;

            if (socket == null || !socket.Connected)
            {
                return;
            }

            var count = socket.EndSend(ar);

            ByteArray ba;

            lock (writeQueue)
            {
                ba = writeQueue.Peek();
            }

            ba.readIdx += count;

            // send package completed
            if (ba.length == 0)
            {
                lock (writeQueue)
                {
                    writeQueue.Dequeue();

                    ba = writeQueue.Peek();
                }
            }

            // if ba.length != 0 or queue is not empty
            if (ba != null)
            {
                socket.BeginSend(ba.bytes, ba.readIdx, ba.length, 0, SendCallback, socket);
            }
            else if (isClosing)
            {
                socket.Close();
            }

            Debug.Log($"Socket Send {count} bytes");
        }
        catch (SocketException ex)
        {
            Debug.Log($"Socket Send Failed: {ex}");
        }
    }
}

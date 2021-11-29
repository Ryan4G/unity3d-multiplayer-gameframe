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

    static bool isClosing = false;

    public delegate void MsgListener(string str);

    // listening list
    private static Dictionary<string, MsgListener> listeners = new Dictionary<string, MsgListener>();

    // message queue
    private static Queue<string> msgQueue = new Queue<string>();

    public static void AddListener(string msgName, MsgListener listener)
    {
        if (listeners.ContainsKey(msgName))
        {
            listeners[msgName] = listener;
        }
        else
        {
            listeners.Add(msgName, listener);
        }
    }

    public static string GetDesc()
    {
        if (socket == null)
        {
            return "";
        }

        if (!socket.Connected)
        {
            return "";
        }

        return socket.LocalEndPoint.ToString();
    }

    public static void Connect(string ip, int port)
    {
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        socket.BeginConnect(ip, port, ConnectCallback, socket);
    }

    public static void Close()
    {
        if (writeQueue.Count > 0)
        {
            isClosing = true;
        }
        else
        {
            if (socket != null && socket.Connected)
            {
                socket.Close();
            }
        }
    }

    private static void ConnectCallback(IAsyncResult ar)
    {
        try
        {
            Socket socket = ar.AsyncState as Socket;
            socket.EndConnect(ar);
            Debug.Log("Socket Connect Successful");

            socket.BeginReceive(readBuff.bytes, readBuff.writeIdx, readBuff.remain, 0, ReceiveCallback, socket);
        }
        catch (SocketException ex)
        {
            Debug.Log($"Socket Connect Failed: {ex}");
        }
    }

    private static void ReceiveCallback(IAsyncResult ar)
    {
        try
        {
            Socket socket = ar.AsyncState as Socket;

            var count = socket.EndReceive(ar);

            readBuff.writeIdx += count;

            OnReceiveData();

            if (readBuff.remain < 8)
            {
                readBuff.MoveBytes();
                readBuff.Resize(readBuff.length * 2);
            }

            socket.BeginReceive(readBuff.bytes, readBuff.writeIdx, readBuff.remain, 0, ReceiveCallback, socket);
        }
        catch (SocketException ex)
        {
            Debug.Log($"Socket Receive Failed: {ex}");
        }
    }

    public static void Send(string sendStr)
    {
        if (socket == null)
        {
            return;
        }

        if (!socket.Connected)
        {
            return;
        }

        if (isClosing)
        {
            return;
        }

        byte[] bodyBytes = System.Text.Encoding.UTF8.GetBytes(sendStr);

        // add length bytes into package
        short len = (short)bodyBytes.Length;
        byte[] lenBytes = BitConverter.GetBytes(len);

        // length bytes default writen with little endian 
        if (!BitConverter.IsLittleEndian)
        {
            lenBytes.Reverse();
        }

        byte[] sendBytes = lenBytes.Concat(bodyBytes).ToArray();

        ByteArray ba = new ByteArray(sendBytes);
        var count = 0;

        // avoid multi theads problem
        lock (writeQueue)
        {
            writeQueue.Enqueue(ba);
            count = writeQueue.Count;
        }

        // if queue only has one package , send it immediately
        if (count == 1)
        {
            socket.BeginSend(ba.bytes, ba.readIdx, ba.length, 0, SendCallback, socket);
        }
    }

    private static void SendCallback(IAsyncResult ar)
    {
        try
        {
            Socket socket = ar.AsyncState as Socket;

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

    public static void Update()
    {
        if (msgQueue.Count == 0)
        {
            return;
        }

        var msgStr = msgQueue.Dequeue();

        if (string.IsNullOrEmpty(msgStr))
        {
            return;
        }

        var split = msgStr.Split('|');
        var msgName = split[0];
        var msgArgs = split[1];

        Debug.Log($"NetManager -> Dequeue -> {msgName} -> {msgArgs}");

        if (listeners.ContainsKey(msgName))
        {
            listeners[msgName](msgArgs);
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

        byte[] stringBytes = new byte[bodyLength];
        readBuff.Read(stringBytes, 0, bodyLength);

        string recvStr = System.Text.Encoding.UTF8.GetString(stringBytes);

        msgQueue.Enqueue(recvStr);

        if (readBuff.length > 2)
        {
            // work utill return
            OnReceiveData();
        }
    }
}

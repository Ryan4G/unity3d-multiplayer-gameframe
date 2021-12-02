using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestProtobuf : MonoBehaviour
{
    public static byte[] Encode(ProtoBuf.IExtensible msgBase)
    {
        using (var memory = new System.IO.MemoryStream())
        {
            ProtoBuf.Serializer.Serialize(memory, msgBase);
            return memory.ToArray();
        }
    }

    public static ProtoBuf.IExtensible Decode(string protoName, byte[] bytes, int offset, int count)
    {
        using (var memory = new System.IO.MemoryStream(bytes, offset, count))
        {
            System.Type t = System.Type.GetType(protoName);
            return (ProtoBuf.IExtensible)ProtoBuf.Serializer.NonGeneric.Deserialize(t, memory);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        proto.BattleMsg.MsgMove msgMove = new proto.BattleMsg.MsgMove();
        msgMove.x = 214;
        byte[] bs = Encode(msgMove);
        Debug.Log(System.BitConverter.ToString(bs));

        ProtoBuf.IExtensible m = Decode(msgMove.ToString(), bs, 0, bs.Length);
        proto.BattleMsg.MsgMove m2 = (proto.BattleMsg.MsgMove)m;
        Debug.Log(m2.x);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

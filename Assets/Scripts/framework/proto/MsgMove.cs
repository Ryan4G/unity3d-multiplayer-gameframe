using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MsgMove : MsgBase
{
    public MsgMove()
    {
        protoName = nameof(MsgMove);
    }

    public int x = 0;
    public int y = 0;
    public int z = 0;
}

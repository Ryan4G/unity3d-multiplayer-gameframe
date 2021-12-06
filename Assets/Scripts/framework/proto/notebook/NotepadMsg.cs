using System.Collections;
using System.Collections.Generic;


public class MsgGetText : MsgBase
{
    public string text = "";

    public MsgGetText()
    {
        protoName = nameof(MsgGetText);
    }
}

public class MsgSaveText : MsgBase
{
    public string text = "";

    /// <summary>
    /// 0-Fail 1-Success
    /// </summary>
    public int result = 0;

    public MsgSaveText()
    {
        protoName = nameof(MsgSaveText);
    }
}
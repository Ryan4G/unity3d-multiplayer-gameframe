using System.Collections;
using System.Collections.Generic;


public class MsgRegister : MsgBase
{
    public string id = "";

    public string pw = "";

    /// <summary>
    /// 0-Fail 1-Success
    /// </summary>
    public int result = 0;

    public MsgRegister()
    {
        protoName = nameof(MsgRegister);
    }
}

public class MsgLogin : MsgBase
{
    public string id = "";

    public string pw = "";

    /// <summary>
    /// 0-Fail 1-Success
    /// </summary>
    public int result = 0;

    public MsgLogin()
    {
        protoName = nameof(MsgLogin);
    }
}

public class MsgKick : MsgBase
{
    public int reason = 0;

    public MsgKick()
    {
        protoName = nameof(MsgKick);
    }
}
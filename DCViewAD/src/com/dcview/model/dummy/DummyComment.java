package com.dcview.model.dummy;

import com.dcview.model.IComment;

public class DummyComment implements IComment
{
    @Override
    public String GetNick()
    {
        return "nick";  //To change body of implemented methods use File | Settings | File Templates.
    }

    @Override
    public String GetText()
    {
        return "text";  //To change body of implemented methods use File | Settings | File Templates.
    }
}

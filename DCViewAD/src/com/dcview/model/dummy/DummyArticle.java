package com.dcview.model.dummy;

import com.dcview.model.IArticle;
import com.dcview.model.IComment;
import com.dcview.model.IPage;
import com.dcview.model.IPreviewArticle;

import java.util.ArrayList;

class PageComment implements IPage<IComment>
{
    @Override
    public ArrayList<IComment> GetNext()
    {
        return null;  //To change body of implemented methods use File | Settings | File Templates.
    }
}

public class DummyArticle implements IArticle, IPreviewArticle
{
    int id;

    public DummyArticle(int id)
    {
        this.id = id;
    }

    @Override
    public IArticle GetArticle()
    {
        return this;
    }

    @Override
    public String GetTitle()
    {
        return String.format("Title %05d", id);
    }

    @Override
    public String GetText()
    {
        return String.format("Text Text %05d", id);  //To change body of implemented methods use File | Settings | File Templates.
    }

    @Override
    public IPage<IComment> GetComments()
    {
        return new PageComment();
    }
}

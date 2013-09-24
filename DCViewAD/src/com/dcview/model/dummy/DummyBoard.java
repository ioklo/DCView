package com.dcview.model.dummy;

import com.dcview.model.IBoard;
import com.dcview.model.IPage;
import com.dcview.model.IPreviewArticle;

import java.util.ArrayList;

class PreviewArticlePage implements IPage<IPreviewArticle>
{
    int latestID = 45234;
    int page = 0;

    @Override
    public ArrayList<IPreviewArticle> GetNext()
    {
        ArrayList<IPreviewArticle> articles = new ArrayList<IPreviewArticle>();

        for(int t = 0; t < 20; t++)
            articles.add(new DummyArticle(latestID - page * 20 - t));

        page++;

        return articles;
    }
}

public class DummyBoard implements IBoard
{
    @Override
    public IPage<IPreviewArticle> GetArticles()
    {
        return new PreviewArticlePage();
    }
}

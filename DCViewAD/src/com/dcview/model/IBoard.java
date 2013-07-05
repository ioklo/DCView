package com.dcview.model;

import java.util.ArrayList;

public interface IBoard
{
    IPage<IPreviewArticle> GetArticles();
}

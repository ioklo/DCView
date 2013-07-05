package com.dcview.model;

public interface IArticle
{
    String GetTitle();
    String GetText();
    IPage<IComment> GetComments();
}

package com.dcview.model;

public interface IPreviewArticle
{
    // async operation
    IArticle GetArticle();

    String GetName();
    String GetTitle();
}

package com.dcview.model;

public interface IPreviewArticle
{
    // async operation
    IArticle GetArticle();

    String GetTitle();
    String GetText();
}

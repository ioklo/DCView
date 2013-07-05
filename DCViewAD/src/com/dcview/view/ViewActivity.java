package com.dcview.view;

import android.app.Activity;
import android.os.Bundle;
import com.dcview.model.IBoard;

// 글 목록과 글 내용, 댓글을 볼 수 있는 Activity
public class ViewActivity extends Activity
{
    IBoard board; // 글 목록과 내용을 가져올 board

    public void onCreate(Bundle savedInstanceState)
    {
        super.onCreate(savedInstanceState);
    }
}
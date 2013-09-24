package com.dcview.view;

import android.app.Activity;
import android.app.AlertDialog;
import android.content.Context;
import android.os.AsyncTask;
import android.os.Bundle;
import android.view.View;
import android.view.ViewGroup;
import android.widget.AdapterView;
import android.widget.ArrayAdapter;
import android.widget.ListView;
import android.widget.TextView;
import com.dcview.R;
import com.dcview.model.IBoard;
import com.dcview.model.IPage;
import com.dcview.model.IPreviewArticle;
import com.dcview.model.dummy.DummyArticle;
import com.dcview.model.dummy.DummyBoard;

import java.util.ArrayList;
import java.util.List;

class ArticleListItemAdapter extends ArrayAdapter<IPreviewArticle>
{
    public ArticleListItemAdapter(Context context)
    {
        super(context, R.layout.article_item, R.id.name);
    }

    @Override
    public View getView(int position, View convertView, ViewGroup parent)
    {
        View view = super.getView(position, convertView, parent);

        TextView nameView = (TextView)view.findViewById(R.id.name);
        TextView titleView = (TextView)view.findViewById(R.id.title);

        IPreviewArticle article = this.getItem(position);
        nameView.setText(article.GetName());
        titleView.setText(article.GetTitle());

        return view;
    }
}

// 글 목록과 글 내용, 댓글을 볼 수 있는 Activity
public class ViewActivity extends Activity
{
    IPage<IPreviewArticle> pages;

    ArticleListItemAdapter articleList;
    ListView articleListView;

    public ViewActivity()
    {
        DummyBoard board = new DummyBoard();
        pages = board.GetArticles();
    }


    public void onCreate(Bundle savedInstanceState)
    {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.view_activity);

        articleListView = (ListView)findViewById(R.id.listView);

        articleList = new ArticleListItemAdapter(this);
        articleListView.setAdapter(articleList);

        articleListView.setOnItemClickListener(new AdapterView.OnItemClickListener()
        {
            @Override
            public void onItemClick(AdapterView<?> parent, View view, int position, long id)
            {
                new AlertDialog.Builder(ViewActivity.this)
                        .setMessage("hi")
                        .create()
                        .show();
                //To change body of implemented methods use File | Settings | File Templates.
            }
        });

        NextPage();
    }

    private void NextPage()
    {
        // async operation으로 만듭니다.
        AsyncTask<Void, Integer, List<IPreviewArticle>> task = new AsyncTask<Void, Integer, List<IPreviewArticle>>()
        {
            @Override
            protected List<IPreviewArticle> doInBackground(Void... params)
            {
                return pages.GetNext();
            }

            @Override
            protected void onPostExecute(List<IPreviewArticle> result)
            {
                articleList.addAll(result);
            }

            @Override
            protected void onCancelled(List<IPreviewArticle> result)
            {

            }
        };

        task.execute();
    }
}
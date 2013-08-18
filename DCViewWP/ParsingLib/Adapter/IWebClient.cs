using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCView.Adapter
{
    public interface IWebClient
    {   
        void SetHeader(string name, string data);
        Task<string> DownloadStringAsyncTask(Uri uri);
        Task<string> UploadStringAsyncTask(Uri uri, string method, string data);
        event Action<object, long, long, int> DownloadProgressChanged;
    }
}

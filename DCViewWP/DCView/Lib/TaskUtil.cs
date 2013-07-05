using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCView.Lib
{
    public static class TaskUtil
    {
        static public T GetResult<T>(this Task<T> task)
        {
            task.Wait();
            return task.Result;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DCView.Util
{
    // 데이터의 값을 n개까지 유지하는 캐시
    public class MRUCache<Key, Value>
    {
        struct TimeWith<T>
        {
            public T value;
            public int time;
        }
        
        Dictionary<Key, TimeWith<Value>> dict = new Dictionary<Key, TimeWith<Value>>();
        Queue<TimeWith<Key>> keyQueue = new Queue<TimeWith<Key>>();

        int maxLength;
        int curTime = 0;

        public MRUCache(int maxLength)
        {
            this.maxLength = maxLength;
        }

        public void Add(Key key, Value value)
        {
            lock (this)
            {
                keyQueue.Enqueue(new TimeWith<Key>() { value = key, time = curTime });
                dict[key] = new TimeWith<Value>() { value = value, time = curTime };
                curTime++;

                if (keyQueue.Count > maxLength)
                {
                    var timeWithKey = keyQueue.Dequeue();
                    if (dict[timeWithKey.value].time == timeWithKey.time) // 업데이트 되었는지 확인하고 안되었다면
                        dict.Remove(timeWithKey.value); // 제거
                }
            }
        }
        
        public bool TryGetValue(Key key, out Value value)
        {
            lock (this)
            {
                TimeWith<Value> timeWithValue;
                if (!dict.TryGetValue(key, out timeWithValue))
                {
                    value = default(Value);
                    return false;
                }

                value = timeWithValue.value;
                return true;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DCView.Adapter
{
    public interface ISettings
    {
        bool TryGetValue<T>(string key, out T value);
        object this[string key] { get; set; }
        void Add(string key, object value);
        bool Remove(string key);
        void Save();
    }
}

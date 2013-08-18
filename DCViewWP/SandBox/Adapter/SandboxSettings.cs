using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DCView.Adapter;

namespace SandBox
{
    class SandboxSettings : ISettings
    {
        Dictionary<string, object> settings = new Dictionary<string, object>();

        public static SandboxSettings Instance { get; private set; }
        static SandboxSettings()
        {
            Instance = new SandboxSettings();
        }

        public bool TryGetValue<T>(string key, out T value)
        {
            try
            {
                value = (T)settings[key];
                return true;
            }
            catch
            {
                value = default(T);
                return false;
            }
        }

        public object this[string key]
        {
            get
            {
                return settings[key];
            }
            set
            {
                settings[key] = value;
            }
        }

        public bool Remove(string key)
        {
            settings[key] = null;
            return true;
        }

        public void Save()
        {            
        }

        public void Add(string key, object value)
        {
            settings[key] = value;
        }
    }
}

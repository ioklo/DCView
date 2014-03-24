using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;

namespace DCView.Adapter
{
    class WPSetting : ISettings
    {
        private IsolatedStorageSettings settings;
        private WPSetting(IsolatedStorageSettings settings) { this.settings = settings; }

        public static WPSetting Instance { get; private set; }
        static WPSetting()
        {
            Instance = new WPSetting(IsolatedStorageSettings.ApplicationSettings);
        }

        public bool TryGetValue<T>(string key, out T value)
        {
            return settings.TryGetValue(key, out value);
        }

        public void Save()
        {
            settings.Save();
        }

        public void Add(string key, object value)
        {
            if (settings.Contains(key))
                settings[key] = value;
            else
                settings.Add(key, value);
        }

        public bool Remove(string key)
        {
            return settings.Remove(key);
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
    }
}

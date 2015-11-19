using System;
using System.Collections.Generic;
using System.Text;
using Windows.Storage;

namespace QMunicate
{
    public class SettingsManager
    {

        private static SettingsManager instance = new SettingsManager();

        public static SettingsManager Instance
        {
            get
            {
                return instance;
            }
        }

        private readonly object thisLock = new object();

        public void WriteToSettings<T>(string settingsKey, T value)
        {
            var settings = ApplicationData.Current.LocalSettings;
            lock (thisLock)
            {
                if (!settings.Values.Keys.Contains(settingsKey))
                {
                    settings.Values.Add(new KeyValuePair<string, object>(settingsKey, value));
                }
                else
                {
                    settings.Values[settingsKey] = value;
                }
            }
        }

        public T ReadFromSettings<T>(string settingsKey)
        {
            var settings = ApplicationData.Current.LocalSettings;
            object value;
            lock (thisLock)
            {
                settings.Values.TryGetValue(settingsKey, out value);
            }

            return value == null ? default(T) : (T) value;
        }

        public void DeleteFromSettings(string settingsKey)
        {
            var settings = ApplicationData.Current.LocalSettings;
            lock (thisLock)
            {
                if (settings.Values.Keys.Contains(settingsKey))
                {
                    settings.Values.Remove(settingsKey);
                }
            }
        }
    }
}

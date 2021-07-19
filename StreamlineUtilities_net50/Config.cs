using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;

namespace StreamlineUtilities
{
    // START ConfigManager Class --------------------------------------------------------------------------------------------------------
    public static class Config
    {
        private static ReaderWriterLockSlim configLocker = new ReaderWriterLockSlim();

        public static string Read(string key)
        {
            configLocker.EnterReadLock();
            string value = safeRead(key);
            configLocker.ExitReadLock();
            return value;
        }

        private static string safeRead(string key)
        {
            if (key == null)
            {
                return "";
            }

            ConfigurationManager.RefreshSection("appSettings");

            try
            {
                return ConfigurationManager.AppSettings[key] ?? "";
            }
            catch (Exception Ex)
            {
                LogStreamline.Exception("Error reading config. Key: " + key, Ex);
                return "";
            }
        }

        public static bool Update(string key, string value)
        {
            configLocker.EnterWriteLock();
            bool success = safeUpdate(key, value);
            configLocker.ExitWriteLock();
            return success;
        }

        private static bool safeUpdate(string key, string value)
        {
            if (key == null || value == null)
            {
                return false;
            }

            ConfigurationManager.RefreshSection("appSettings");
            Configuration config = null;

            try
            {
                config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            }
            catch (Exception Ex)
            {
                LogStreamline.Exception("Error loading config file for writing. Key: " + key + " | Value: " + value, Ex);
                return false;
            }

            try
            {
                if (ConfigurationManager.AppSettings[key] != null)
                {
                    config.AppSettings.Settings[key].Value = value;
                }
                else
                {
                    config.AppSettings.Settings.Add(key, value);
                }
            }
            catch (Exception Ex)
            {
                LogStreamline.Exception("Error staging data for writing to config file. Key: " + key + " | Value: " + value, Ex);
                return false;
            }

            config.AppSettings.SectionInformation.ForceSave = true;

            try
            {
                config.Save(ConfigurationSaveMode.Modified);
            }
            catch (Exception Ex)
            {
                LogStreamline.Exception("Error saving data to config file. Key: " + key + " | Value: " + value, Ex);
                return false;
            }

            ConfigurationManager.RefreshSection("appSettings");
            return true;
        }
    }
    // END ConfigManager Class ----------------------------------------------------------------------------------------------------------
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace StreamlineUtilities
{
    // START INI_Class ------------------------------------------------------------------------------------------------------------------
    public class INIKeyValuePair
    {
        public string Key { get; set; } = "";
        public string Value { get; set; } = "";
    }

    public class INISection
    {
        public string Name { get; set; } = "";
        public INIKeyValuePair[] Section { get; set; } = new INIKeyValuePair[0];
    }

    public static class INI
    {
        private static ReaderWriterLockSlim iniLocker = new ReaderWriterLockSlim();

        public static bool? ReadBool(string file, string section, string key)
        {
            string value = Read(file, section, key);
            bool parsed;
            if (bool.TryParse(value, out parsed) == false)
            {
                return null;
            }

            return parsed;
        }

        public static int? ReadInt(string file, string section, string key)
        {
            string value = Read(file, section, key);
            int parsed;
            if (int.TryParse(value, out parsed) == false)
            {
                return null;
            }

            return parsed;
        }

        public static string Read(string file, string section, string key)
        {
            if (string.IsNullOrEmpty(file) || section == null || string.IsNullOrEmpty(key) || File.Exists(file) == false)
            {
                return "";
            }

            iniLocker.EnterReadLock();
            string value = safeReadKey(file, section, key);
            iniLocker.ExitReadLock();
            return value;
        }

        private static string safeReadKey(string file, string section, string key)
        {
            List<INISection> iniSectionList = safeReadFile(file);
            if (iniSectionList == null || iniSectionList.Count <= 0)
            {
                return "";
            }

            int sectionIndex = iniSectionList.FindIndex(s => s.Name.ToLower() == section.ToLower());
            if (sectionIndex > -1)
            {
                // Checks list for a matching key. Returns empty string if not found.
                INIKeyValuePair iniKeyValuePair = iniSectionList[sectionIndex].Section.FirstOrDefault(k => k.Key.ToLower() == key.ToLower());
                if (iniKeyValuePair == null)
                {
                    return "";
                }

                return iniKeyValuePair.Value;
            }

            return "";
        }

        public static INISection[] ReadFile(string file)
        {
            if (string.IsNullOrEmpty(file) || File.Exists(file) == false)
            {
                return new INISection[0];
            }

            iniLocker.EnterReadLock();
            INISection[] iniSectionArray = safeReadFile(file).ToArray();
            iniLocker.ExitReadLock();
            return iniSectionArray;
        }

        private static List<INISection> safeReadFile(string file)
        {
            string[] iniFile = null;
            try
            {
                iniFile = File.ReadAllLines(file); // Read the file to a List<string>
            }
            catch (Exception Ex)
            {
                LogUtilities.Exception("Error reading INI file: " + file, Ex);
                return new List<INISection>();
            }

            if (iniFile == null || iniFile.Length <= 0)
            {
                return new List<INISection>();
            }

            List<INISection> iniSectionList = new List<INISection>();
            List<INIKeyValuePair> currentKeyValuePair = new List<INIKeyValuePair>();
            int currentIndex = -1;
            for (int i = 0; i < iniFile.Length; i++)
            {
                string line = iniFile[i].Trim();

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    if (currentIndex > -1 && iniSectionList.Count > 0 && currentKeyValuePair.Count > 0)
                    {
                        iniSectionList[currentIndex].Section = currentKeyValuePair.ToArray();
                        currentKeyValuePair.Clear();
                    }

                    iniSectionList.Add(new INISection() { Name = line.TrimStart('[').TrimEnd(']'), });
                    currentIndex++;
                    continue;
                }

                if (line.Contains("="))
                {
                    string[] parts = line.Split('=');
                    if (parts.Length > 2)
                    {
                        continue;
                    }

                    if (iniSectionList.Count <= 0)
                    {
                        currentIndex++;
                        iniSectionList.Add(new INISection() { Name = "", });
                    }

                    currentKeyValuePair.Add(new INIKeyValuePair() { Key = parts[0], Value = parts[1] });
                }

                // This tells us we have reached the end of the file and the last set of Key Value pairs should be written to the appropriate section.
                if (i == iniFile.Length - 1)
                {
                    iniSectionList[currentIndex].Section = currentKeyValuePair.ToArray();
                    currentKeyValuePair.Clear();
                }
            }

            return iniSectionList;
        }

        public static bool Write(string file, string section, string key, string value, bool create, bool backup)
        {
            if (string.IsNullOrEmpty(file) || string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
            {
                return false;
            }

            INISection[] iniSectionList = new INISection[]
            {
                new INISection()
                {
                    Name = section,
                    Section =  new INIKeyValuePair[] { new INIKeyValuePair(){ Key = key, Value = value}, },
                },
            };

            iniLocker.EnterWriteLock();
            bool success = safeWrite(file, iniSectionList, false, create, backup);
            iniLocker.ExitWriteLock();
            return success;
        }

        public static bool MultiWrite(string file, INISection[] iniSectionList, bool overwrite, bool create, bool backup)
        {
            if (string.IsNullOrEmpty(file) || iniSectionList == null)
            {
                return false;
            }

            iniLocker.EnterWriteLock();
            bool success = false;
            if (iniSectionList.Length <= 0 && create)
            {
                success = writeAllLines(file, "", backup);
            }
            else
            {
                success = safeWrite(file, iniSectionList, overwrite, create, backup);
            }
            iniLocker.ExitWriteLock();
            return success;
        }

        private static bool safeWrite(string file, INISection[] iniSectionList, bool overwrite, bool create, bool backup)
        {
            // File does not exists and the create paramater is false.
            if (File.Exists(file) == false && create == false)
            {
                LogUtilities.LogEntry("INI write failure. File does not exist: " + file);
                return false;
            }

            // File does not exists and the create paramater is true indicating we want to create a new file.
            if (File.Exists(file) == false && create)
            {
                if (writeAllLines(file, convertSectionList(iniSectionList), backup))
                {
                    return true;
                }

                return false;
            }

            // We just want to overwrite the existing data.
            if (overwrite)
            {
                if (writeAllLines(file, convertSectionList(iniSectionList), backup))
                {
                    return true;
                }

                return false;
            }

            // Below is if we want to just append the existing data in the file.
            // Converts the INISection[] to a List<INISection> so we can append data as needed.
            List<INISection> fileINISectionList = safeReadFile(file).ToList();

            // If the file is empty, just write directly to it.
            if (fileINISectionList.Count <= 0)
            {
                if (writeAllLines(file, convertSectionList(iniSectionList), backup))
                {
                    return true;
                }

                return false;
            }

            // Reads each INISection from the paramater so we can append what was read from the file.
            foreach (INISection iniSection in iniSectionList)
            {
                // Searches file INISection list for a match to what we want to add. A match will result in appending the data.
                int matchedFileINISectionIndex = fileINISectionList.FindIndex(s => s.Name.ToLower() == iniSection.Name.ToLower());
                if (matchedFileINISectionIndex > -1) // Matching section found.
                {
                    fileINISectionList[matchedFileINISectionIndex].Section = mergeINIKeyValuePairList(iniSection.Section, fileINISectionList[matchedFileINISectionIndex].Section);
                }
                else
                {
                    // Section does not exist so we can add this one.
                    fileINISectionList.Add(iniSection);
                }
            }

            if (writeAllLines(file, convertSectionList(fileINISectionList.ToArray()), backup))
            {
                return true;
            }

            return false;
        }

        private static string convertSectionList(INISection[] iniSectionList)
        {
            string allText = "";
            INISection blankSection = new INISection();

            foreach (INISection iniSection in iniSectionList)
            {
                // If we have multiple blank sections, we want to merge those. These will be added back in the end if any exist.
                if (iniSection.Name.Length <= 0)
                {
                    blankSection.Section = mergeINIKeyValuePairList(blankSection.Section, iniSection.Section);
                    continue;
                }

                allText = allText + "[" + iniSection.Name + "]" + Environment.NewLine;
                foreach (INIKeyValuePair iniKeyValuePair in iniSection.Section)
                {
                    if (string.IsNullOrEmpty(iniKeyValuePair.Key))
                    {
                        continue;
                    }

                    if (iniKeyValuePair.Value == null)
                    {
                        iniKeyValuePair.Value = "";
                    }

                    allText = allText + iniKeyValuePair.Key + "=" + iniKeyValuePair.Value + Environment.NewLine;
                }

                allText = allText + Environment.NewLine;
            }

            if (blankSection.Section.Length > 0)
            {
                string blankSectionText = "";
                foreach (INIKeyValuePair iniKeyValuePair in blankSection.Section)
                {
                    if (string.IsNullOrEmpty(iniKeyValuePair.Key))
                    {
                        continue;
                    }

                    if (iniKeyValuePair.Value == null)
                    {
                        iniKeyValuePair.Value = "";
                    }

                    blankSectionText = blankSectionText + iniKeyValuePair.Key + "=" + iniKeyValuePair.Value + Environment.NewLine;
                }

                allText = blankSectionText + Environment.NewLine + allText;
            }

            return allText;
        }

        // Merges 2 INIKeyValuePair[] with value priority with the primary list.
        private static INIKeyValuePair[] mergeINIKeyValuePairList(INIKeyValuePair[] primary, INIKeyValuePair[] secondary)
        {
            // The secondary list is what gets overwritten so we are assinging it to a list if we need to append the number of items.
            List<INIKeyValuePair> workingList = secondary.ToList();
            foreach (INIKeyValuePair iniKeyValuePair in primary)
            {
                // Checks if the secondary list contains the key pair.
                int matchedIndex = workingList.FindIndex(p => p.Key.ToLower() == iniKeyValuePair.Key.ToLower());
                if (matchedIndex > -1)
                {
                    workingList[matchedIndex].Value = iniKeyValuePair.Value;
                }
                else
                {
                    workingList.Add(new INIKeyValuePair() { Key = iniKeyValuePair.Key, Value = iniKeyValuePair.Value });
                }
            }

            return workingList.ToArray();
        }

        private static bool writeAllLines(string file, string allText, bool backup)
        {
            string tempFile = "";

            try
            {
                tempFile = string.Format(@"{0}\{1}_Temp.ini", Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file));
                File.WriteAllText(tempFile, allText);
            }
            catch (Exception Ex)
            {
                LogUtilities.Exception("Error creating temp INI file. File: " + tempFile, Ex);
                return false;
            }

            if (backup)
            {
                string backFile = "";

                try
                {
                    backFile = string.Format(@"{0}\{1}_Back.ini", Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file));

                    if (File.Exists(file))
                    {
                        File.Copy(file, backFile, true);
                    }
                }
                catch (Exception Ex)
                {
                    LogUtilities.Exception("Error creating backup INI file. File: " + backFile, Ex);
                }
            }

            try
            {
                File.Copy(tempFile, file, true);
                File.Delete(tempFile);
            }
            catch (Exception Ex)
            {
                LogUtilities.Exception("Error overwriting original INI file. File: " + file, Ex);
                return false;
            }

            return true;
        }
    }
    // END INI_Class --------------------------------------------------------------------------------------------------------------------
}

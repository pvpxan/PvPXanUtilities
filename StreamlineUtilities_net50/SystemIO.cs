using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace StreamlineUtilities
{
    // START SystemIO Class -------------------------------------------------------------------------------------------------------------
    public enum PathType
    {
        Invalid,
        File,
        Directory,
    }

    public class OutputResult
    {
        public string Filepath { get; set; } = "";
        public bool Success { get; set; } = true;
    }

    // Safely wrapped System.IO stuff that is commonly used.
    public static class SystemIO
    {
        public static PathType GetPathType(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return PathType.Invalid;
            }

            if (File.Exists(path))
            {
                return PathType.File;
            }

            if (Directory.Exists(path))
            {
                return PathType.Directory;
            }

            return PathType.Invalid;
        }

        public static bool Delete(string file)
        {
            if (File.Exists(file))
            {
                try
                {
                    File.Delete(file);
                    return true;
                }
                catch (Exception Ex)
                {
                    LogStreamline.Exception("Error deleting file: " + file, Ex);
                    return false;
                }
            }

            return false;
        }

        public static bool Copy(string fileSource, string fileTarget, bool overwrite)
        {
            try
            {
                File.Copy(fileSource, fileTarget, overwrite);
                return true;
            }
            catch (Exception Ex)
            {
                LogStreamline.Exception("Error copying source file: " + fileSource + " to target desitnation path: " + fileTarget, Ex);
                return false;
            }
        }

        public static bool CreateDirectory(string directory)
        {
            try
            {
                Directory.CreateDirectory(directory);
                return true;
            }
            catch (Exception Ex)
            {
                LogStreamline.Exception("Error creating directory: : " + directory, Ex);
                return false;
            }
        }

        public static OutputResult[] CopyDirectory(string sourceDirectory, string targetDirectory)
        {
            List<OutputResult> output = new List<OutputResult>();

            try
            {
                DirectoryInfo diSource = new DirectoryInfo(sourceDirectory);
                DirectoryInfo diTarget = new DirectoryInfo(targetDirectory);

                foreach (OutputResult result in copyDirecoryWork(diSource, diTarget))
                {
                    output.Add(result);
                }
            }
            catch (Exception Ex)
            {
                LogStreamline.Exception("Error processing Copy method.", Ex);
            }

            return output.ToArray();
        }

        private static List<OutputResult> copyDirecoryWork(DirectoryInfo source, DirectoryInfo target)
        {
            List<OutputResult> output = new List<OutputResult>();

            try
            {
                Directory.CreateDirectory(target.FullName);

                // Copy each file into the new directory.
                foreach (FileInfo fi in source.GetFiles())
                {
                    OutputResult fileResult = new OutputResult();

                    try
                    {
                        fileResult.Filepath = fi.FullName;

                        fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
                    }
                    catch (Exception Ex)
                    {
                        fileResult.Success = false;

                        LogStreamline.Exception("Failed to copy file: " + fi.FullName, Ex);
                    }

                    output.Add(fileResult);
                }

                // Copy each subdirectory using recursion.
                foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
                {
                    try
                    {
                        DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);

                        foreach (OutputResult result in copyDirecoryWork(diSourceSubDir, nextTargetSubDir))
                        {
                            output.Add(result);
                        }
                    }
                    catch (Exception Ex)
                    {
                        LogStreamline.Exception("Error with creating subdirectory: " + diSourceSubDir.Name, Ex);
                    }
                }
            }
            catch (Exception Ex)
            {
                LogStreamline.Exception("Error processing Copy method.", Ex);
            }

            return output;
        }
    }
    // END SystemIO Class ---------------------------------------------------------------------------------------------------------------
}

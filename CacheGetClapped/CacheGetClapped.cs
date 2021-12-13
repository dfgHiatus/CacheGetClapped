using HarmonyLib;
using NeosModLoader;
using FrooxEngine;
using System.IO;
using System;

namespace ModNameGoesHere
{
    public class CacheGetClapped : NeosMod
    {
        public override string Name => "CacheGetClapped";
        public override string Author => "dfgHiatus";
        public override string Version => "1.0.0";
        public override string Link => "https://github.com/dfgHiatus/CacheGetClapped/";

        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony("net.dfgHiatus.CacheGetClapped");
            harmony.PatchAll();
        }
		
        [HarmonyPatch(typeof(Engine), "Shutdown")]
        public class ShutdownPatch
        {
            // Should run before we run the rest of shutdown
            public static void Prefix()
            {
                int MaxDaysToKeep = 3;

                long CacheFileSize = 0;
                long CacheOldFileSize = 0;
                int CacheFileQuantity = 0;
                int CacheOldQuantity = 0;

                long DataFileSize = 0;
                long DataOldFileSize = 0;
                int DataFileQuantity = 0;
                int DataOldQuantity = 0;

                long CombinedFileSize = 0;
                long CombinedOldFileSize = 0;
                int CombinedFileQuantity = 0;
                int CombinedOldFileQuantity = 0;

                // Multiply by negative 1 to get the Date Offset
                MaxDaysToKeep *= -1;

                // C:/Users/<Username>/AppData/Local/Temp/Solirax/NeosVR if null
                string CachePath = Engine.Current.CachePath + "/Cache";
                if (Directory.Exists(CachePath))
                {
                    Debug("CachePath found at " + CachePath);
                }
                else
                {
                    Error("Could not find CachePath. Aborting");
                    throw new DirectoryNotFoundException("Could not find CachePath. Aborting");
                }

                // C:/Users/<Username>/AppData/LocalLow/Solirax/NeosVR if null
                string DataPath = Engine.Current.DataPath + "/Assets";
                if (Directory.Exists(DataPath))
                {
                    Debug("DataPath found at " + DataPath);
                }
                else
                {
                    Error("Could not find DataPath. Aborting");
                    throw new DirectoryNotFoundException("Could not find DataPath. Aborting");
                }

                // Local
                DirectoryInfo di_1 = new DirectoryInfo(CachePath);
                foreach (FileInfo file in di_1.EnumerateFiles()) // Only files, no dirs
                {
                    CacheFileSize += file.Length;
                    CacheFileQuantity++;

                    // Delete if the file hasn't been accessed in more than 3 days
                    if (file.LastAccessTime < DateTime.Now.AddDays(MaxDaysToKeep))
                    {
                        file.Delete();
                        CacheOldFileSize += file.Length;
                        CacheOldQuantity++;
                    }
                }

                // LocalLow
                DirectoryInfo di_2 = new DirectoryInfo(DataPath);
                foreach (FileInfo file in di_2.EnumerateFiles())
                {
                    DataFileSize += file.Length;
                    DataFileQuantity++;

                    if (file.LastAccessTime < DateTime.Now.AddDays(MaxDaysToKeep))
                    {
                        file.Delete();
                        DataOldFileSize += file.Length;
                        DataOldQuantity++;
                    }
                }

                CombinedFileSize = CacheFileSize + DataFileSize;
                CombinedOldFileSize = CacheOldFileSize + DataOldFileSize;
                CombinedFileQuantity = CacheFileQuantity + DataFileQuantity;
                CombinedOldFileQuantity = CacheOldQuantity + DataOldQuantity;

                Debug("");
                Debug("BEGIN CACHE-GET-CLAPPED DIAGNOSTICS:");
                Debug("");
                Debug("CACHE FOLDER INFO:");
                Debug("Number of unique cache files: " + CacheFileQuantity);
                Debug("Size of Neos cache folder: " + BytesToString(CacheFileSize));
                Debug("");
                Debug("DATA FOLDER INFO");
                Debug("Number of unique data files: " + DataFileQuantity);
                Debug("Size of Neos data folder: " + BytesToString(DataFileSize));
                Debug("");
                Debug("TOTAL INFO:");
                Debug("Total number of files: " + CombinedFileQuantity);
                Debug("Total size of both folders: " + BytesToString(CombinedFileSize));
                Debug("Total number of files over 3 days old: " + CombinedOldFileQuantity);
                Debug("Total size of files over 3 days old: " + BytesToString(CombinedOldFileSize));
                Debug("");
                Debug(string.Format(
                    "Deleted {0} or approximately {1}% of the Neos Cache successfully",
                    BytesToString(CombinedOldFileSize),
                    Ratio(CombinedOldFileQuantity, CombinedFileQuantity)));
                Debug("Neos Cache is now " + BytesToString(CombinedFileSize - CombinedOldFileSize));
                Debug("");
                Debug("END DIAGNOSTICS");
                Debug("");
            }

            public static string BytesToString(long byteCount)
            {
                string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
                if (byteCount == 0)
                    return "0" + suf[0];
                long bytes = Math.Abs(byteCount);
                int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
                double num = Math.Round(bytes / Math.Pow(1024, place), 1);
                return (Math.Sign(byteCount) * num).ToString() + suf[place];
            }

            public static string Ratio(long old, long total)
            {
                return (1.0f * old / total).ToString();
            }
        }
    }
}
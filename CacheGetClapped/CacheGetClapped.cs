using HarmonyLib;
using NeosModLoader;
using FrooxEngine;
using System.IO;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace CacheGetClappedMod
{
    public class CacheGetClapped : NeosMod
    {
        [AutoRegisterConfigKey]
        public static ModConfigurationKey<int> MAX_DAYS_KEY = new ModConfigurationKey<int>("max_days_to_keep", "Maximum days to keep cached files", () => 21);

        [AutoRegisterConfigKey]
        public static ModConfigurationKey<bool> IS_ENABLED = new ModConfigurationKey<bool>("is_enabled", "A toggle for the user, if the mod should run", () => true);

        [AutoRegisterConfigKey]
        public static ModConfigurationKey<int> MAX_SIZE_KEY = new ModConfigurationKey<int>("max_size_of_cache", "The maximum size of the cache in MB, before the oldest cache files are removed", () => -1);

        [AutoRegisterConfigKey]
        public static ModConfigurationKey<int> TARGET_SIZE_KEY = new ModConfigurationKey<int>("target_size_of_cache", "The new target size of the cache in MB, after the maximum size has been exceeded", () => -1);

        public static ModConfiguration config;

        public override string Name => "CacheGetClapped";
        public override string Author => "dfgHiatus";
        public override string Version => "1.0.4";
        public override string Link => "https://github.com/dfgHiatus/CacheGetClapped/";

        public override void OnEngineInit()
        {
            config = GetConfiguration();
            Harmony harmony = new Harmony("net.dfgHiatus.CacheGetClapped");
            harmony.PatchAll();
        }
		
        [HarmonyPatch(typeof(Engine), "Shutdown")]
        public class ShutdownPatch
        {
            public static void Prefix()
            {
                if (!config.GetValue(IS_ENABLED))
                {
                    return;
                }

                // TODO: Expose MaxDaysToKeep as setting
                int CacheFileQuantity = 0;
                int CacheOldFileQuantity = 0;
                long CacheFileSize = 0;
                long CacheOldFileSize = 0;

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

                int configTime = config.GetValue(MAX_DAYS_KEY);
                configTime *= -1;
                DirectoryInfo CacheDirectory = new DirectoryInfo(CachePath);  
                DateTime NewestCachedFileAccessTime = CacheDirectory.GetFiles().OrderByDescending(f => f.LastWriteTime).First()
                                                     .LastAccessTime.AddDays(configTime);

                foreach (FileInfo file in CacheDirectory.EnumerateFiles())
                {
                    CacheFileSize += file.Length;
                    CacheFileQuantity++;

                    if (file.LastAccessTime < NewestCachedFileAccessTime)
                    { 
                        CacheOldFileSize += file.Length;
                        CacheOldFileQuantity++;
                        file.Delete();
                    }
                }

                long MaxSize = config.GetValue(MAX_SIZE_KEY) * 1000000;
                long TargetSize = config.GetValue(TARGET_SIZE_KEY) * 1000000;
                bool shouldDoSizeCleanup = MaxSize > 0 && TargetSize > 0;

                if (CacheFileSize - CacheOldFileSize > MaxSize && shouldDoSizeCleanup)
                {
                    var files = CacheDirectory.GetFiles().OrderBy(f => f.LastWriteTime);
                    foreach (FileInfo file in files)
                    {
                        CacheOldFileSize += file.Length;
                        CacheOldFileQuantity++;
                        file.Delete();

                        if (CacheFileSize - CacheOldFileSize < TargetSize)
                            break;
                    }
                }

                Debug("");
                Debug("BEGIN CACHE-GET-CLAPPED DIAGNOSTICS:");
                Debug("");
                Debug("CACHE FOLDER INFO:");
                Debug("Number of unique cached files: " + CacheFileQuantity);
                Debug("Size of Neos cache folder: " + BytesToString(CacheFileSize));
                Debug("");
                Debug(string.Format(
                    "Deleted {0} files or approximately {1}% of the Neos Cache successfully",
                    BytesToString(CacheOldFileSize),
                    Ratio(CacheOldFileQuantity, CacheFileQuantity)));
                Debug("Neos Cache is now " + BytesToString(CacheFileSize - CacheOldFileSize));
                Debug("");
                Debug("END DIAGNOSTICS");
                Debug("");
            }

	        // https://stackoverflow.com/questions/281640/how-do-i-get-a-human-readable-file-size-in-bytes-abbreviation-using-net
            public static string BytesToString(long byteCount)
            {
                //Longs run out around EB
                string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; 
                if (byteCount == 0)
                    return "0" + suf[0];
                long bytes = Math.Abs(byteCount);
                int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
                double num = Math.Round(bytes / Math.Pow(1024, place), 1);
                return (Math.Sign(byteCount) * num).ToString() + suf[place];
            }

            public static string Ratio(long old, long total)
            {
                return float.IsNaN(1.0f * old / total) ? "0" : (1.0f * old / total).ToString();
            }
        }
    }
}

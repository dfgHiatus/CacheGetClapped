using HarmonyLib;
using NeosModLoader;
using FrooxEngine;
using System.IO;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace CacheGetClappedMod
{
    public class CacheGetClapped : NeosMod
    {
        [AutoRegisterConfigKey]
        public static ModConfigurationKey<bool> IS_ENABLED = new ModConfigurationKey<bool>("is_enabled", "Enabled (Will run on close if true)", () => true);

        [AutoRegisterConfigKey]
        public static ModConfigurationKey<float> MAX_DAYS_KEY = new ModConfigurationKey<float>("max_days_to_keep", "Maximum number of days to keep cached files", () => 21f);

        [AutoRegisterConfigKey]
        public static ModConfigurationKey<float> MAX_SIZE_KEY = new ModConfigurationKey<float>("max_size_of_cache", "Maximum size of the cache in gigabytes (GB) before triggering a cleanup", () => -1f);

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

                float configTime = config.GetValue(MAX_DAYS_KEY);
                bool shouldDoDayCleanup = configTime >= 0;

                configTime *= -1;
                DirectoryInfo CacheDirectory = new DirectoryInfo(CachePath);  
                DateTime NewestCachedFileAccessTime = CacheDirectory.GetFiles().OrderByDescending(f => f.LastWriteTime).First()
                                                     .LastAccessTime.AddDays(configTime);

                if (shouldDoDayCleanup)
                {
                    _ = Parallel.ForEach(CacheDirectory.EnumerateFiles(), (FileInfo file) =>
                    {
                        Interlocked.Add(ref CacheFileSize, file.Length);
                        Interlocked.Increment(ref CacheFileQuantity);

                        if (file.LastAccessTime < NewestCachedFileAccessTime)
                        {
                            Interlocked.Add(ref CacheOldFileSize, file.Length);
                            Interlocked.Increment(ref CacheOldFileQuantity);
                            file.Delete();
                        }
                    });
                }
                
                long MaxSize = (long)(config.GetValue(MAX_SIZE_KEY) * 1_000_000_000);
                bool shouldDoSizeCleanup = MaxSize >= 0;

                if (CacheFileSize - CacheOldFileSize > MaxSize && shouldDoSizeCleanup)
                {
                    var files = CacheDirectory.GetFiles().OrderBy(f => f.LastWriteTime);
                    foreach (FileInfo file in files)
                    {
                        CacheOldFileSize += file.Length;
                        CacheOldFileQuantity++;
                        file.Delete();

                        if (CacheFileSize - CacheOldFileSize < MaxSize)
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

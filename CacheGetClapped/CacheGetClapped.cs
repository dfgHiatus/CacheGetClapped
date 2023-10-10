using FrooxEngine;
using ResoniteModLoader;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CacheGetClappedMod;

public class CacheGetClapped : ResoniteMod
{
    [AutoRegisterConfigKey]
    public readonly static ModConfigurationKey<bool> IS_ENABLED = 
        new("is_enabled", "Run on close", () => true);

    [AutoRegisterConfigKey]
    public readonly static ModConfigurationKey<float> MAX_DAYS_KEY = 
        new("max_days_to_keep", "Maximum number of days to keep cached files", () => 21f);

    [AutoRegisterConfigKey]
    public readonly static ModConfigurationKey<float> MAX_SIZE_KEY = 
        new("max_size_of_cache", "(Optional) Maximum cache size in gigabytes (GB). Leave at -1 to disable", () => -1f);

    public static ModConfiguration config;
    private static readonly string CachePath = Path.Combine(Engine.Current.CachePath, "Cache");

    public override string Name => "CacheGetClapped";
    public override string Author => "dfgHiatus";
    public override string Version => "2.0.0";
    public override string Link => "https://github.com/dfgHiatus/CacheGetClapped/";

    public override void OnEngineInit()
    {
        config = GetConfiguration();
        Engine.Current.OnShutdown += OnShutdown;
    }
		
    private static void OnShutdown()
    {
        if (!config.GetValue(IS_ENABLED)) return;

        if (!Directory.Exists(CachePath))
            throw new DirectoryNotFoundException("Could not find CachePath. Aborting");

        Debug($"CachePath found at {CachePath}");
        int CacheFileQuantity = 0;
        int CacheOldFileQuantity = 0;
        long CacheFileSize = 0;
        long CacheOldFileSize = 0;

        float configTime = config.GetValue(MAX_DAYS_KEY);
        long MaxSize = (long)(config.GetValue(MAX_SIZE_KEY) * 1_000_000_000);

        DirectoryInfo CacheDirectory = new DirectoryInfo(CachePath);

        /* --- Get directory size and file count ---
        * Order does not matter, so we can parallelize this operation
        * (Provided we interlock CacheFileSize/CacheFileQuantity)
        */
        Parallel.ForEach(CacheDirectory.EnumerateFiles(), (FileInfo file) =>
        {
            Interlocked.Add(ref CacheFileSize, file.Length);
            Interlocked.Increment(ref CacheFileQuantity);
        });

        /* --- Cache file cleanup by file age ---
        * The order of the files deleted here does not matter as well,
        * so we can parallelize this operation too. Parallel.ForEach
        * *should* be safe for concurrent file deletions, at least
        * according to MSDN/StackOverflow. My tests corroborate this,
        * though part of me remains skeptical.
        * Don't forget to interlock CacheOldFileSize/CacheOldFileQuantity!
        */
        if (configTime >= 0)
        {
            DateTime DateThreshold = DateTime.Now.AddDays(configTime * -1f);
            Parallel.ForEach(CacheDirectory.EnumerateFiles(), (FileInfo file) =>
            {
                if (file.LastAccessTime < DateThreshold)
                {
                    Interlocked.Add(ref CacheOldFileSize, file.Length);
                    Interlocked.Increment(ref CacheOldFileQuantity);
                    file.Delete();
                }
            });
        }

        /* --- Cache file cleanup by total size threshold ---
         * If the user has specified a maximum cache size AND
         * the above operation did not already delete enough files,
         * start deleting the oldest files until we are under the
         * desired size threshold. This *cannot* be parallelized,
         * as we need to check the size of the cache folder after.
         */
        if (MaxSize >= 0)
        {
            var files = CacheDirectory.EnumerateFiles().OrderBy(f => f.LastWriteTime);
            foreach (FileInfo file in files)
            {
                if (CacheFileSize - CacheOldFileSize < MaxSize)
                    break;
                CacheOldFileSize += file.Length;
                CacheOldFileQuantity++;
                file.Delete();
            }
        }

        var bTs = BytesToString(CacheFileSize);

        Debug("");
        Debug("BEGIN CACHE-GET-CLAPPED DIAGNOSTICS:");
        Debug("");
        Debug("CACHE FOLDER INFO:");
        Debug($"Number of unique cached files: {CacheFileQuantity}");
        Debug($"Size of Resonite cache folder: {bTs}");
        Debug("");
        Debug($"Deleted {bTs} files or approximately {Ratio(CacheOldFileQuantity, CacheFileQuantity)}% of the Resonite Cache successfully");
        Debug("Resonite Cache is now " + BytesToString(CacheFileSize - CacheOldFileSize));
        Debug("");
        Debug("END DIAGNOSTICS");
        Debug("");
    }

	// https://stackoverflow.com/questions/281640/how-do-i-get-a-human-readable-file-size-in-bytes-abbreviation-using-net
    private static string BytesToString(long byteCount)
    {
        // Longs run out around EB
        string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; 
        if (byteCount == 0)
            return "0" + suf[0];
        long bytes = Math.Abs(byteCount);
        int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
        double num = Math.Round(bytes / Math.Pow(1024, place), 1);
        return (Math.Sign(byteCount) * num).ToString() + suf[place];
    }

    private static string Ratio(long old, long total)
    {
        return float.IsNaN(1.0f * old / total) ? "0" : (1.0f * old / total).ToString();
    }
}

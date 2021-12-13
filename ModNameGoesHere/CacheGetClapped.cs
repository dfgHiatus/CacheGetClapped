using HarmonyLib;
using NeosModLoader;
using FrooxEngine;
using System.IO;
using System;
using System.Linq;

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
            // public static bool Prefix()
            public static void Postfix()
            {
                long sizeDeleted = 0;

                // Del LocalLow
                string cachePathOne = Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData), "LocalLow/Solirax/NeosVR/Assets");
                System.IO.DirectoryInfo di_1 = new DirectoryInfo(cachePathOne);

                foreach (FileInfo file in di_1.EnumerateFiles())
                {
                    sizeDeleted += file.Length;
                    file.Delete();
                }
                foreach (DirectoryInfo dir in di_1.EnumerateDirectories())
                {
                    sizeDeleted += GetDirectorySize(dir);
                    dir.Delete(true);
                }

                // Del Local
                string cachePathTwo = Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData), "Local/Temp/Solirax/NeosVR/Cache");
                System.IO.DirectoryInfo di_2 = new DirectoryInfo(cachePathOne);

                foreach (FileInfo file in di_2.EnumerateFiles())
                {
                    sizeDeleted += file.Length;
                    file.Delete();
                }
                foreach (DirectoryInfo dir in di_2.EnumerateDirectories())
                {
                    sizeDeleted += GetDirectorySize(dir);
                    dir.Delete(true);
                }
            }

            public static long GetDirectorySize(DirectoryInfo dir)
            {
                return dir.EnumerateFiles("*.*", SearchOption.AllDirectories).Sum(fi => fi.Length);
            }
        }
    }
}
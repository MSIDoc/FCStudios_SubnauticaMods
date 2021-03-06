﻿using System;
using System.Reflection;
using FCSCommon.Utilities;
using FCSDemo.Buildables;
using FCSDemo.Configuration;
using FCSDemo.Model;
using Harmony;
using QModManager.API.ModLoading;

namespace FCSDemo
{
    [QModCore]
    public class QPatch
    {
        internal static ConfigFile Configuration { get; private set; } = new ConfigFile();

        [QModPatch]
        public static void Patch()
        {
            QuickLogger.Info($"Started patching. Version: {QuickLogger.GetAssemblyVersion(Assembly.GetExecutingAssembly())}");

#if DEBUG
            QuickLogger.DebugLogsEnabled = true;
            QuickLogger.Debug("Debug logs enabled");
#endif

            var harmony = HarmonyInstance.Create("com.fcsdemo.fcstudios");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Configuration = Mod.LoadConfiguration();

            foreach (ModEntry modEntry in Configuration.Config.Prefabs)
            {
                QuickLogger.Info($"Added Prefab {modEntry.ClassID}");
                modEntry.Prefab = FCSDemoModel.GetPrefabs(modEntry.PrefabName);
                var prefab = new FCSDemoBuidable(modEntry);
                prefab.Patch();
            }

            try
            {
                QuickLogger.Info("Finished patching");
            }
            catch (Exception ex)
            {
                QuickLogger.Error(ex);
            }
        }
    }
}

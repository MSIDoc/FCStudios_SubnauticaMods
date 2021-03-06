﻿using ExStorageDepot.Buildable;
using ExStorageDepot.Configuration;
using FCSCommon.Utilities;
using System;
using System.IO;
using System.Reflection;
using FCSTechFabricator;
using FCSTechFabricator.Components;
using FCSTechFabricator.Craftables;
using HarmonyLib;
using QModManager.API.ModLoading;
using SMLHelper.V2.Utility;


namespace ExStorageDepot
{
    [QModCore]
    public static class QPatch
    {
        internal static Config Config { get; private set; }

        [QModPatch]
        public static void Patch()
        {
            QuickLogger.Info("Started patching. Version: " + QuickLogger.GetAssemblyVersion(Assembly.GetExecutingAssembly()));

#if DEBUG
            QuickLogger.DebugLogsEnabled = true;
            QuickLogger.Debug("Debug logs enabled");
#endif

            try
            {
                Config = Mod.LoadConfiguration();

                AddTechFabricatorItems();
                
                ExStorageDepotBuildable.PatchHelper();

                var harmony = new Harmony("com.exstoragedepot.fcstudios");
                harmony.PatchAll(Assembly.GetExecutingAssembly());

                QuickLogger.Info("Finished patching");
            }
            catch (Exception ex)
            {
                QuickLogger.Error(ex);
            }
        }


        private static void AddTechFabricatorItems()
        {
            var icon = ImageUtils.LoadSpriteFromFile(Path.Combine(Mod.GetAssetPath(), $"{Mod.ClassID}.png"));
            var craftingTab = new CraftingTab(Mod.ExStorageTabID, Mod.ModFriendly, icon);

            var exStorageKit = new FCSKit(Mod.ExStorageKitClassID, Mod.ModFriendly, craftingTab, Mod.ExStorageIngredients);
            exStorageKit.Patch(FcTechFabricatorService.PublicAPI, FcAssetBundlesService.PublicAPI);
        }
    }
}

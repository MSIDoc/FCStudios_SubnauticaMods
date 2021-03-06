﻿using FCSCommon.Utilities;
using SMLHelper.V2.Utility;
using System;
using System.Collections;
using System.IO;
using FCS_DeepDriller.Mono.MK2;
using FCSCommon.Extensions;
using Oculus.Newtonsoft.Json;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Options;
using UnityEngine;

namespace FCS_DeepDriller.Configuration
{
    internal static class Mod
    {
        internal static LootDistributionData LootDistributionData { get; set; }

        internal const string ModClassID = "FCS_DeepDriller";
        internal const string ModFriendlyName = "FCS Deep Driller";
        internal const string ModFolderName = "FCS_DeepDriller";
        internal const string ModDecription = "Let's dig down to the deep down deep dark!";
        internal const string SaveDataFilename = "DeepDrillerSaveData.json";
        internal const string BundleName = "fcsdeepdrillermk2modbundle";

        internal const string DeepDrillerGameObjectName = "DeepDriller";
        internal const string MaterialBaseName = "AlterraDeepDrillerMK2";
        internal const string SandSpawnableClassID = "Sand_DD";
        internal const string DeepDrillerKitClassID = "DeepDrillerKit_DD";
        internal const string DeepDrillerKitFriendlyName = "Deep Driller";

        internal const string DeepDrillerTabID = "DD";
        internal static string MODFOLDERLOCATION => GetModPath();
        
        private static ModSaver _saveObject;

        private static DeepDrillerSaveData _deepDrillerSaveData;
        private static TechType _exStorageTechType;
        private static TechType _sandBagTechType;


        internal static event Action<DeepDrillerSaveData> OnDeepDrillerDataLoaded;

        internal static TechType GetSandBagTechType()
        {
            if (_sandBagTechType == TechType.None)
            {
                _sandBagTechType = SandSpawnableClassID.ToTechType();
            }

            return _sandBagTechType;
        }


#if SUBNAUTICA
        internal static TechData DeepDrillerKitIngredients => new TechData
        {
            craftAmount = 1,
            Ingredients =
            {
                new Ingredient(TechType.MapRoomHUDChip, 1),
                new Ingredient(TechType.Titanium, 2),
                new Ingredient(TechType.AdvancedWiringKit, 1),
                new Ingredient(TechType.Lubricant, 1),
                new Ingredient(TechType.VehicleStorageModule, 1),
            }
        };
#elif BELOWZERO
        internal static RecipeData DeepDrillerKitIngredients => new RecipeData
        {
            craftAmount = 1,
            Ingredients =
            {
                new Ingredient(TechType.MapRoomHUDChip, 1),
                new Ingredient(TechType.Titanium, 2),
                new Ingredient(TechType.AdvancedWiringKit, 1),
                new Ingredient(TechType.ExosuitDrillArmModule, 1),
                new Ingredient(TechType.Lubricant, 1),
                new Ingredient(TechType.VehicleStorageModule, 1),
            }
        };
#endif


        #region Deep Driller
        internal static void SaveDeepDriller()
        {
            if (!IsSaving())
            {
                _saveObject = new GameObject().AddComponent<ModSaver>();

                DeepDrillerSaveData newSaveData = new DeepDrillerSaveData();

                var drills = GameObject.FindObjectsOfType<FCSDeepDrillerController>();

                foreach (var drill in drills)
                {
                    drill.Save(newSaveData);
                }

                _deepDrillerSaveData = newSaveData;

                ModUtils.Save<DeepDrillerSaveData>(_deepDrillerSaveData, SaveDataFilename, GetSaveFileDirectory(), OnSaveComplete);
            }
        }

        internal static void LoadDeepDrillerData()
        {
            QuickLogger.Info("Loading Save Data...");
            ModUtils.LoadSaveData<DeepDrillerSaveData>(SaveDataFilename, GetSaveFileDirectory(), (data) =>
            {
                _deepDrillerSaveData = data;
                QuickLogger.Info("Save Data Loaded");
                OnDeepDrillerDataLoaded?.Invoke(_deepDrillerSaveData);
            });
        }

        internal static DeepDrillerSaveData GetDeepDrillerSaveData()
        {
            return _deepDrillerSaveData ?? new DeepDrillerSaveData();
        }

        internal static DeepDrillerSaveDataEntry GetDeepDrillerSaveData(string id)
        {
            LoadDeepDrillerData();

            var saveData = GetDeepDrillerSaveData();

            foreach (var entry in saveData.Entries)
            {
                if (entry.Id == id)
                {
                    return entry;
                }
            }

            return new DeepDrillerSaveDataEntry() { Id = id };
        }
        #endregion

        internal static void OnSaveComplete()
        {
            _saveObject.StartCoroutine(SaveCoroutine());
        }

        private static IEnumerator SaveCoroutine()
        {
            while (SaveLoadManager.main != null && SaveLoadManager.main.isSaving)
            {
                yield return null;
            }
            GameObject.DestroyImmediate(_saveObject.gameObject);
            _saveObject = null;
        }

        internal static bool IsSaving()
        {
            return _saveObject != null;
        }

        private static string GetQModsPath()
        {
            return Path.Combine(Environment.CurrentDirectory, "QMods");
        }
        
        private static string GetModPath()
        {
            return Path.Combine(GetQModsPath(), ModFolderName);
        }
        internal static string GetAssetFolder()
        {
            return Path.Combine(GetModPath(), "Assets");
        }

        private static string GetConfigPath()
        {
            return Path.Combine(GetModPath(), "config.json");
        }

        internal static string GetSaveFileDirectory()
        {
            return Path.Combine(SaveUtils.GetCurrentSaveDataDir(), ModClassID);
        }

        internal static bool IsConfigAvailable()
        {
            return File.Exists(GetConfigPath());
        }

        private static void CreateModConfiguration()
        {
            var config = new DeepDrillerCfg();

            var saveDataJson = JsonConvert.SerializeObject(config, Formatting.Indented);

            File.WriteAllText(Path.Combine(MODFOLDERLOCATION, GetConfigPath()), saveDataJson);
        }

        internal static void SaveModConfiguration()
        {
            try
            {
                var saveDataJson = JsonConvert.SerializeObject(QPatch.Configuration, Formatting.Indented);

                File.WriteAllText(Path.Combine(MODFOLDERLOCATION, GetConfigPath()), saveDataJson);
            }
            catch (Exception e)
            {
                QuickLogger.Error($"{e.Message}\n{e.StackTrace}");
            }
        }

        private static DeepDrillerCfg LoadConfigurationData()
        {
            try
            {
                // == Load Configuration == //
                string configJson = File.ReadAllText(GetConfigPath().Trim());

                JsonSerializerSettings settings = new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore
                };

                // == LoadData == //
                return JsonConvert.DeserializeObject<DeepDrillerCfg>(configJson, settings);
            }
            catch (Exception e)
            {
                QuickLogger.Error($"Failed to load configuration loading Defaults: \nError: {e.Message} | StackTrace: {e.StackTrace}");
                return new DeepDrillerCfg();
            }
        }

        internal static DeepDrillerCfg LoadConfiguration()
        {
            if (!IsConfigAvailable())
            {
                CreateModConfiguration();
            }

            return LoadConfigurationData();
        }

        internal static TechType ExStorageTechType()
        {
            if (_exStorageTechType == TechType.None)
            {
                _exStorageTechType = "ExStorageDepot".ToTechType();
            }

            return _exStorageTechType;
        }
    }

    internal class Options : ModOptions
    {
        private const string HardCoreModeID = "DD_HardCore";
        private const string DrillExstorageRangeID = "DD_ExstorageRange";
        private bool _hardCoreMode;
        private float _drillExStorageRange;

        internal Options() : base("Deep Driller Settings")
        {
            ToggleChanged += OnToggleChanged;
            SliderChanged += OnSliderChanged;
            _hardCoreMode = QPatch.Configuration.HardCoreMode;
            _drillExStorageRange = QPatch.Configuration.DrillExStorageRange;
        }

        private void OnSliderChanged(object sender, SliderChangedEventArgs e)
        {
            switch (e.Id)
            {
                case DrillExstorageRangeID:
                    _drillExStorageRange = QPatch.Configuration.DrillExStorageRange = e.Value;
                    break;
            }

            Mod.SaveModConfiguration();
        }

        internal void OnToggleChanged(object sender, ToggleChangedEventArgs e)
        {
            
            switch (e.Id)
            {
                case HardCoreModeID:
                    _hardCoreMode = QPatch.Configuration.HardCoreMode = e.Value;
                    break;
            }

            Mod.SaveModConfiguration();
        }


        public override void BuildModOptions()
        {
            AddToggleOption(HardCoreModeID, "Hard Core Mode", _hardCoreMode);
            AddSliderOption(DrillExstorageRangeID,"Drill ExStorage Search Range",0f,QPatch.Configuration.DrillExStorageMaxRange, _drillExStorageRange);
        }
    }
}

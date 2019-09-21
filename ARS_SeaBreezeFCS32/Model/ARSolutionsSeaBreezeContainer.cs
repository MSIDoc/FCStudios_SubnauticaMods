﻿using ARS_SeaBreezeFCS32.Buildables;
using ARS_SeaBreezeFCS32.Enum;
using ARS_SeaBreezeFCS32.Interfaces;
using ARS_SeaBreezeFCS32.Mono;
using FCSCommon.Objects;
using FCSCommon.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARS_SeaBreezeFCS32.Model
{
    internal class ARSolutionsSeaBreezeContainer : IFridgeContainer
    {
        public bool IsFull { get; }
        public int NumberOfItems => FridgeItems.Count;
        private const int ContainerWidth = 6;
        private const int ContainerHeight = 8;
        private readonly ItemsContainer _fridgeContainer = null;
        private readonly ChildObjectIdentifier _containerRoot = null;
        public Dictionary<TechType, int> TrackedItems { get; } = new Dictionary<TechType, int>();
        private readonly Func<bool> _isConstructed;
        private bool _isFridgeOpen = false;
        private FridgeCoolingState _coolingState;
        private const float Rate = 11.0f;

        public List<EatableEntities> FridgeItems { get; } = new List<EatableEntities>();

        #region Constructor
        internal ARSolutionsSeaBreezeContainer(ARSolutionsSeaBreezeController mono)
        {
            _isConstructed = () => { return mono.IsConstructed; };

            if (_containerRoot == null)
            {
                QuickLogger.Debug("Initializing StorageRoot");
                var storageRoot = new GameObject("StorageRoot");
                storageRoot.transform.SetParent(mono.transform, false);
                _containerRoot = storageRoot.AddComponent<ChildObjectIdentifier>();
            }

            if (_fridgeContainer == null)
            {
                QuickLogger.Debug("Initializing Container");

                _fridgeContainer = new ItemsContainer(ContainerWidth, ContainerHeight, _containerRoot.transform,
                    ARSSeaBreezeFCS32Buildable.StorageLabel(), null);

                _fridgeContainer.isAllowedToAdd += IsAllowedToAdd;
                _fridgeContainer.isAllowedToRemove += IsAllowedToRemove;

                _fridgeContainer.onAddItem += OnAddItemEvent;
                _fridgeContainer.onRemoveItem += OnRemoveItemEvent;

                _fridgeContainer.onAddItem += mono.OnAddItemEvent;
                _fridgeContainer.onRemoveItem += mono.OnRemoveItemEvent;
            }
        }

        #endregion

        private void OnAddItemEvent(InventoryItem item)
        {
            var techType = item.item.GetTechType();

            if (TrackedItems.ContainsKey(techType))
            {
                TrackedItems[techType] = TrackedItems[techType] + 1;
            }
            else
            {
                TrackedItems.Add(techType, 1);
            }

            CoolItem(item);
            QuickLogger.Debug($"Fridge Item Count: {FridgeItems.Count}", true);
        }
        private void OnRemoveItemEvent(InventoryItem item)
        {
            var techType = item.item.GetTechType();

            //Remove completely if 1

            if (TrackedItems.ContainsKey(techType))
            {
                if (TrackedItems[techType] != 1)
                {
                    TrackedItems[techType] = TrackedItems[techType] - 1;
                }
                else
                {
                    TrackedItems.Remove(techType);
                }
            }

            var eat = item.item.GetComponent<Eatable>();

            var prefabId = item.item.GetComponent<PrefabIdentifier>().Id;

            var f = eat.GetFoodValue();
            var w = eat.GetWaterValue();


            EatableEntities match = FindMatch(prefabId);

            if (_coolingState == FridgeCoolingState.Cooling)
            {
                var eatable = item.item.GetComponent<Eatable>();

                if (match != null)
                {
                    QuickLogger.Debug($"Match Found", true);

                    eatable.kDecayRate = match.KDecayRate;
                    eatable.timeDecayStart = DayNightCycle.main.timePassedAsFloat;
                    eatable.foodValue = f;
                    eatable.waterValue = w;
                    eatable.decomposes = match.Decomposes;
                    QuickLogger.Debug($"Match Found", true);
                    FridgeItems.Remove(match);
                    QuickLogger.Debug($"Match Removed", true);
                    QuickLogger.Debug($"Decay Reset {eatable.kDecayRate}", true);
                    QuickLogger.Debug($"Decomposes Reset {eatable.decomposes}", true);
                }
            }
            else
            {
                foreach (EatableEntities eatableEntity in FridgeItems)
                {
                    FridgeItems.Remove(match);
                    break;
                }
            }

            FridgeItems.Remove(match);

            QuickLogger.Debug($"Fridge Item Count: {FridgeItems.Count}", true);
        }

        private EatableEntities FindMatch(string prefabId)
        {
            foreach (EatableEntities eatableEntity in FridgeItems)
            {
                if (eatableEntity.PrefabID == prefabId)
                {
                    return eatableEntity;
                }
            }

            return null;
        }

        internal void DecayItems()
        {
            QuickLogger.Debug($"Cooling State: {_coolingState.ToString()}", true);
            if (_coolingState == FridgeCoolingState.NotCooling) return;

            foreach (InventoryItem inventoryItem in _fridgeContainer)
            {
                var prefabId = inventoryItem.item.gameObject.GetComponent<PrefabIdentifier>().Id;
                var eatable = inventoryItem.item.gameObject.GetComponent<Eatable>();

                foreach (EatableEntities eatableEntity in FridgeItems)
                {
                    if (eatableEntity.PrefabID != prefabId) continue;
                    QuickLogger.Debug($"Match before: F:{eatable.foodValue} || W:{eatable.waterValue} || DR:{eatable.kDecayRate} || TDS:{eatable.timeDecayStart} || D:{eatable.decomposes}", true);
                    eatable.timeDecayStart = DayNightCycle.main.timePassedAsFloat;
                    eatable.kDecayRate = eatableEntity.KDecayRate;
                    eatable.decomposes = eatableEntity.Decomposes;
                    QuickLogger.Debug($"SavedF:{eatableEntity.FoodValue} || SavedW:{eatableEntity.WaterValue}", true);
                    eatable.foodValue = eatableEntity.FoodValue;
                    eatable.waterValue = eatableEntity.WaterValue;
                    QuickLogger.Debug($"Match after: F:{eatable.foodValue} || W:{eatable.waterValue} || DR:{eatable.kDecayRate} || TDS:{eatable.timeDecayStart} || D:{eatable.decomposes}", true);
                    //QuickLogger.Debug($"Decaying {inventoryItem.item.name}|| Decompose: {eatable.decomposes} || DRate: {eatable.kDecayRate}", true);
                    break;
                }
            }

            _coolingState = FridgeCoolingState.NotCooling;
        }

        internal void CoolItems()
        {
            QuickLogger.Debug($"Cooling State: {_coolingState.ToString()}", true);

            if (_coolingState == FridgeCoolingState.Cooling) return;

            foreach (InventoryItem inventoryItem in _fridgeContainer)
            {
                var eatable = inventoryItem.item.GetComponent<Eatable>();

                var curF = eatable.GetFoodValue();
                var curW = eatable.GetWaterValue();

                var prefabId = inventoryItem.item.gameObject.GetComponent<PrefabIdentifier>().Id;

                if (FindMatch(prefabId).Decomposes)
                {
                    eatable.decomposes = false;
                    eatable.timeDecayStart = DayNightCycle.main.timePassedAsFloat;
                    //Add set the food and water so it doesnt reset
                    eatable.foodValue = curF;
                    eatable.waterValue = curW;
                }

                QuickLogger.Debug($"Cooling {inventoryItem.item.name}|| Decompose: {eatable.decomposes} || DRate: {eatable.kDecayRate}", true);
            }
            _coolingState = FridgeCoolingState.Cooling;
        }

        private void CoolItem(InventoryItem item)
        {
            var eatable = item.item.GetComponent<Eatable>();
            var curF = eatable.GetFoodValue();
            var curW = eatable.GetWaterValue();
            var prefabId = item.item.GetComponent<PrefabIdentifier>().Id;

            QuickLogger.Debug($"F {eatable.foodValue} || W {eatable.waterValue} || KD {eatable.kDecayRate} || D {eatable.decomposes}", true);

            //Store Data about the item
            var eatableEntity = new EatableEntities();
            eatableEntity.Initialize(item.item);
            FridgeItems.Add(eatableEntity);

            if (FindMatch(prefabId).Decomposes && _coolingState == FridgeCoolingState.Cooling)
            {
                eatable.decomposes = false;
                eatable.timeDecayStart = DayNightCycle.main.timePassedAsFloat;
                eatable.foodValue = curF;
                eatable.waterValue = curW;
                QuickLogger.Debug($"Cooling", true);
            }

            QuickLogger.Debug($"Tracker Count = {FridgeItems.Count}", true);
        }

        private bool IsAllowedToRemove(Pickupable pickupable, bool verbose)
        {
            return true;
        }

        private bool IsAllowedToAdd(Pickupable pickupable, bool verbose)
        {
            bool flag = false;
            if (pickupable != null)
            {
                TechType techType = pickupable.GetTechType();

                QuickLogger.Debug(techType.ToString());

                if (pickupable.GetComponent<Eatable>() != null)
                    flag = true;

                //if (ARSolutionsSeabreezeConfiguration.EatableEntities.ContainsKey(techType))
                //    flag = true;
            }

            QuickLogger.Debug($"Adding Item {flag} || {verbose}");

            if (!flag && verbose)
                ErrorMessage.AddMessage("[Alterra Refrigeration] Food items allowed only.");
            return flag;
        }

        public void OpenStorage()
        {
            QuickLogger.Debug($"Storage Button Clicked", true);

            if (!_isConstructed.Invoke())
                return;

            Player main = Player.main;
            PDA pda = main.GetPDA();
            Inventory.main.SetUsedStorage(_fridgeContainer, false);
            pda.Open(PDATab.Inventory, null, OnFridgeClose, 4f);
            _isFridgeOpen = true;
        }

        private void OnFridgeClose(PDA pda)
        {
            _isFridgeOpen = false;
        }

        public int GetTechTypeAmount(TechType techType)
        {
            return _fridgeContainer.GetCount(techType);
        }

        internal void LoadFoodItems(IEnumerable<EatableEntities> savedDataFridgeContainer)
        {
            FridgeItems.Clear();

            foreach (EatableEntities eatableEntities in savedDataFridgeContainer)
            {
                QuickLogger.Debug($"Adding entity {eatableEntities.Name}");

                var food = GameObject.Instantiate(CraftData.GetPrefabForTechType(eatableEntities.TechType));
                food.gameObject.GetComponent<PrefabIdentifier>().Id = eatableEntities.PrefabID;

                var eatable = food.gameObject.GetComponent<Eatable>();
                eatable.foodValue = eatableEntities.FoodValue;
                eatable.waterValue = eatableEntities.WaterValue;

                var item = new InventoryItem(food.gameObject.GetComponent<Pickupable>().Pickup(false));

                _fridgeContainer.UnsafeAdd(item);
                QuickLogger.Debug($"Load Item {item.item.name}|| Decompose: {eatable.decomposes} || DRate: {eatable.kDecayRate}");
            }
        }

        internal bool GetOpenState()
        {
            return _isFridgeOpen;
        }

        internal IEnumerable<EatableEntities> GetSaveData()
        {
            foreach (EatableEntities eatableEntity in FridgeItems)
            {
                eatableEntity.SaveData();
                yield return eatableEntity;
            }
        }
    }
}

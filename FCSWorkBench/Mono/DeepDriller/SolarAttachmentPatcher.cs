﻿using FCSCommon.Utilities;
using FCSTechFabricator.Abstract_Classes;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
using SMLHelper.V2.Utility;
using System.Collections.Generic;
using UnityEngine;

namespace FCSTechFabricator.Mono.DeepDriller
{
    public partial class SolarAttachmentBuildable : FCSTechFabricatorItem
    {
        private TechGroup GroupForPDA = TechGroup.Resources;
        private TechCategory CategoryForPDA = TechCategory.AdvancedMaterials;
        private GameObject _prefab;


        public SolarAttachmentBuildable() : base("SolarAttachment_DD", "Solar Attachment")
        {

        }

        public override GameObject GetGameObject()
        {
            GameObject prefab = GameObject.Instantiate<GameObject>(this._prefab);
            prefab.name = this.PrefabFileName;
            return prefab;
        }

        private TechData GetBlueprintRecipe()
        {
            // Create and associate recipe to the new TechType
            var customFabRecipe = new TechData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>()
                {
                    new Ingredient(TechType.Copper, 1),
                    new Ingredient(TechType.Glass, 2),
                    new Ingredient(TechType.WiringKit, 1),
                    new Ingredient(TechType.Titanium, 1),
                }
            };
            return customFabRecipe;
        }

        public override void Register()
        {
            if (QPatch.SolarModule)
            {
                _prefab = QPatch.SolarModule;

                if (this.IsRegistered == false)
                {

                    ClassID_I = this.ClassID;

                    //Create a new TechType
                    this.TechType = TechTypeHandler.AddTechType(ClassID, PrefabFileName, "This specially made attachment allows you to run your deep driller off solar power", new Atlas.Sprite(ImageUtils.LoadTextureFromFile($"./QMods/FCSTechFabricator/Assets/{ClassID}.png")));

                    CraftDataHandler.SetTechData(TechType, GetBlueprintRecipe());

                    CraftDataHandler.AddToGroup(this.GroupForPDA, this.CategoryForPDA, this.TechType);


                    QuickLogger.Debug($"Class Id = {ClassID_I}");

                    FriendlyName_I = this.PrefabFileName;

                    TechTypeID = TechType;

                    // Make the object drop slowly in water
                    var wf = _prefab.AddComponent<WorldForces>();
                    wf.underwaterGravity = 0;
                    wf.underwaterDrag = 20f;
                    wf.enabled = true;

                    // Add fabricating animation
                    var fabricatingA = _prefab.AddComponent<VFXFabricating>();
                    fabricatingA.localMinY = -0.1f;
                    fabricatingA.localMaxY = 0.6f;
                    fabricatingA.posOffset = new Vector3(0f, 0f, 0f);
                    fabricatingA.eulerOffset = new Vector3(0f, 0f, 0f);
                    fabricatingA.scaleFactor = 1.0f;

                    // Set proper shaders (for crafting animation)
                    Shader marmosetUber = Shader.Find("MarmosetUBER");
                    var renderer = _prefab.GetComponentInChildren<Renderer>();
                    renderer.material.shader = marmosetUber;

                    // Update sky applier
                    var applier = _prefab.GetComponent<SkyApplier>();
                    if (applier == null)
                        applier = _prefab.AddComponent<SkyApplier>();
                    applier.renderers = new Renderer[] { renderer };
                    applier.anchorSky = Skies.Auto;

                    // We can pick this item
                    var pickupable = _prefab.AddComponent<Pickupable>();
                    pickupable.isPickupable = true;
                    pickupable.randomizeRotationWhenDropped = true;

                    // Add the new TechType to Hand Equipment type.
                    CraftDataHandler.SetEquipmentType(TechType, EquipmentType.PowerCellCharger);

                    PrefabIdentifier prefabID = _prefab.AddComponent<PrefabIdentifier>();
                    prefabID.ClassId = this.ClassID;

                    var techTag = this._prefab.AddComponent<TechTag>();
                    techTag.type = this.TechType;

                    _prefab.AddComponent<FCSTechFabricatorTag>();

                    PrefabHandler.RegisterPrefab(this);

                    this.IsRegistered = true;
                }
            }
            else
            {
                QuickLogger.Error("Failed to get the SolarModulePrefab");
            }
        }

    }
}
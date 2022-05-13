﻿using System.Collections.Generic;
using UnityEngine;

namespace CrabsProfit
{
    using static STRINGS.ITEMS.INDUSTRIAL_PRODUCTS;
    public class BabyCrabFreshWaterShellConfig : IEntityConfig
    {
        public const string ID = "BabyCrabFreshWaterShell";
        public static readonly Tag TAG = TagManager.Create(ID, BABY_CRAB_SHELL.VARIANT_FRESH_WATER.NAME);

        public string[] GetDlcIds() => DlcManager.AVAILABLE_ALL_VERSIONS;

        public GameObject CreatePrefab()
        {
            var mass = (float)CrabsProfitOptions.Instance.CrabFreshWater_Shell_Mass / (float)CrabsProfitOptions.Instance.BabyCrabFreshWater_Mass_Divider;
            var go = EntityTemplates.CreateLooseEntity(
                id: ID,
                name: BABY_CRAB_SHELL.VARIANT_FRESH_WATER.NAME,
                desc: BABY_CRAB_SHELL.VARIANT_FRESH_WATER.DESC,
                mass: mass,
                unitMass: true,
                anim: Assets.GetAnim("fresh_crabshells_small_kanim"),
                initialAnim: "object",
                sceneLayer: Grid.SceneLayer.Front,
                collisionShape: EntityTemplates.CollisionShape.RECTANGLE,
                width: 0.9f,
                height: 0.6f,
                isPickupable: true,
                sortOrder: 0,
                element: SimHashes.Creature,
                additionalTags: new List<Tag> { GameTags.IndustrialIngredient, GameTags.Organics });
            go.AddOrGet<EntitySplitter>();
            go.AddOrGet<SimpleMassStatusItem>();
            EntityTemplates.CreateAndRegisterCompostableFromPrefab(go);
            CrabFreshWaterShellConfig.AddRecipe(ID, mass);
            return go;
        }

        public void OnPrefabInit(GameObject inst) { }
        public void OnSpawn(GameObject inst) { }
    }
}

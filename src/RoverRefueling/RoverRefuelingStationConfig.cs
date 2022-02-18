﻿using TUNING;
using UnityEngine;

namespace RoverRefueling
{
    public class RoverRefuelingStationConfig : IBuildingConfig
    {
        public const string ID = "RoverRefuelingStation";
        // todo: определиться с рейтами
        public const float CHARGE_TIME = 0.1f * Constants.SECONDS_PER_CYCLE;
        public const float CHARGE_MASS = 100f;
        public const float MINIMUM_MASS = 0.1f * CHARGE_MASS;
        public const int NUM_USES = 3;
        public const float CAPACITY = NUM_USES * CHARGE_MASS;

        public override string[] GetDlcIds() => DlcManager.AVAILABLE_EXPANSION1_ONLY;
        public override BuildingDef CreateBuildingDef()
        {
            var def = BuildingTemplates.CreateBuildingDef(
                id: ID,
                width: 2,
                height: 3,
                anim: "oxygen_mask_station_kanim",
                hitpoints: BUILDINGS.HITPOINTS.TIER1,
                construction_time: BUILDINGS.CONSTRUCTION_TIME_SECONDS.TIER2,
                construction_mass: BUILDINGS.CONSTRUCTION_MASS_KG.TIER3,
                construction_materials: MATERIALS.RAW_METALS,
                melting_point: BUILDINGS.MELTING_POINT_KELVIN.TIER1,
                build_location_rule: BuildLocationRule.OnFloor,
                decor: BUILDINGS.DECOR.BONUS.TIER1,
                noise: NOISE_POLLUTION.NOISY.TIER0);
            def.OverheatTemperature = BUILDINGS.OVERHEAT_TEMPERATURES.HIGH_2;
            def.LogicInputPorts = LogicOperationalController.CreateSingleInputPortList(CellOffset.none);
            def.InputConduitType = ConduitType.Liquid;
            def.UtilityInputOffset = CellOffset.none;
            def.PermittedRotations = PermittedRotations.FlipH;
            def.ViewMode = OverlayModes.LiquidConduits.ID;
            GeneratedBuildings.RegisterWithOverlay(OverlayScreen.LiquidVentIDs, ID);
            return def;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            var prefabID = go.GetComponent<KPrefabID>();
            prefabID.AddTag(RoomConstraints.ConstraintTags.IndustrialMachinery);
            prefabID.AddTag(GameTags.NotRocketInteriorBuilding);
            var storage = BuildingTemplates.CreateDefaultStorage(go, false);
            storage.SetDefaultStoredItemModifiers(Storage.StandardSealedStorage);
            Prioritizable.AddRef(go);
            var md = go.AddComponent<ManualDeliveryKG>();
            md.SetStorage(storage);
            md.requestedItemTag = GameTags.CombustibleLiquid;
            md.capacity = CAPACITY;
            md.refillMass = CHARGE_MASS;
            md.choreTypeIDHash = Db.Get().ChoreTypes.Fetch.IdHash;
            md.Pause(true, "");
            var consumer = go.AddOrGet<ConduitConsumer>();
            consumer.conduitType = ConduitType.Liquid;
            consumer.consumptionRate = ConduitFlow.MAX_LIQUID_MASS;
            consumer.capacityKG = CAPACITY;
            consumer.capacityTag = GameTags.CombustibleLiquid;
            consumer.forceAlwaysSatisfied = true;
            consumer.wrongElementResult = ConduitConsumer.WrongElementResult.Dump;
            go.AddOrGet<RoverRefuelingStation>();
        }

        public override void DoPostConfigureUnderConstruction(GameObject go)
        {
            go.GetComponent<KPrefabID>().prefabSpawnFn += (inst) =>
                inst.GetComponent<Constructable>().requireMinionToWork = false;
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            go.GetComponent<RequireInputs>().SetRequirements(false, false);
            go.AddOrGet<LogicOperationalController>();
            SymbolOverrideControllerUtil.AddToPrefab(go);
            go.AddOrGet<SymbolOverrideController>().ApplySymbolOverridesByAffix(Assets.GetAnim("beer_for_robots_kanim"), "beer_");
        }
    }
}

﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using STRINGS;
using UnityEngine;
using HarmonyLib;
using PeterHan.PLib.Core;
using PeterHan.PLib.Detours;

namespace SanchozzONIMods.Shared
{
    // пачти для ManualDeliveryKG
    // копирование настроек - вкл/выкл ручную доставку
    // исправление тоолтипа для этой кнопки, ушоб было видно доставку чего отключаем.
    // исправление последствий косяка в системе событий клеев
    // - что обработчики вызыватся многократно если есть несколько подписаных однотипных компонентов 
    // - просто отписываемся если этот компонент не первый.
    public static class ManualDeliveryKGPatch
    {
        private static readonly EventSystem.IntraObjectHandler<ManualDeliveryKG> OnCopySettingsDelegate =
            new EventSystem.IntraObjectHandler<ManualDeliveryKG>((component, data) => component.OnCopySettings(data));

        private static EventSystem.IntraObjectHandler<ManualDeliveryKG> OnRefreshUserMenuDelegate;

        private const string PATCH_KEY = "Patch.ManualDeliveryKG.OnCopySettings";

        public static readonly IDetouredField<ManualDeliveryKG, bool> userPaused =
            PDetours.DetourField<ManualDeliveryKG, bool>("userPaused");

        public static void Patch(Harmony harmony)
        {
            if (!PRegistry.GetData<bool>(PATCH_KEY))
            {
                OnRefreshUserMenuDelegate = Traverse.Create<ManualDeliveryKG>()
                    .Field<EventSystem.IntraObjectHandler<ManualDeliveryKG>>(nameof(OnRefreshUserMenuDelegate)).Value;
                harmony.Patch(typeof(ManualDeliveryKG), nameof(OnSpawn),
                    postfix: new HarmonyMethod(typeof(ManualDeliveryKGPatch), nameof(OnSpawn)));
                harmony.Patch(typeof(ManualDeliveryKG), nameof(OnCleanUp),
                    prefix: new HarmonyMethod(typeof(ManualDeliveryKGPatch), nameof(OnCleanUp)));
                harmony.PatchTranspile(typeof(ManualDeliveryKG), "OnRefreshUserMenu",
                    transpiler: new HarmonyMethod(typeof(ManualDeliveryKGPatch), nameof(Transpiler)));
                PRegistry.PutData(PATCH_KEY, true);
            }
        }

        private static void OnSpawn(ManualDeliveryKG __instance)
        {
            if (__instance.allowPause)
            {
                if (__instance.GetComponents<ManualDeliveryKG>().ToList().IndexOf(__instance) > 0)
                    __instance.Unsubscribe((int)GameHashes.RefreshUserMenu, OnRefreshUserMenuDelegate);
                else
                    __instance.Subscribe((int)GameHashes.CopySettings, OnCopySettingsDelegate);
            }
        }

        private static void OnCleanUp(ManualDeliveryKG __instance)
        {
            if (__instance.allowPause)
                __instance.Unsubscribe((int)GameHashes.CopySettings, OnCopySettingsDelegate);
        }

        private static void OnCopySettings(this ManualDeliveryKG @this, object data)
        {
            if (@this.allowPause)
            {
                // правильное копирование, если компонентов несколько
                int index = @this.GetComponents<ManualDeliveryKG>().ToList().IndexOf(@this);
                var others = ((GameObject)data).GetComponents<ManualDeliveryKG>();
                if (others != null && index >= 0 && index < others.Length && others[index] != null)
                {
                    bool paused = userPaused.Get(others[index]);
                    userPaused.Set(@this, paused);
                    @this.Pause(paused, "OnCopySettings");
                }
            }
        }

        private static string ResolveTooltip(string tooltip, ManualDeliveryKG manualDelivery)
        {
            return $"{tooltip}\n{string.Format(BUILDING.STATUSITEMS.WAITINGFORMATERIALS.LINE_ITEM_UNITS, manualDelivery.requestedItemTag.ProperName())}";
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method)
        {
            var instructionsList = instructions.ToList();
            string methodName = method.DeclaringType.FullName + "." + method.Name;

            var Tooltip1 = typeof(UI.USERMENUACTIONS.MANUAL_DELIVERY)
                .GetFieldSafe(nameof(UI.USERMENUACTIONS.MANUAL_DELIVERY.TOOLTIP), true);
            var Tooltip2 = typeof(UI.USERMENUACTIONS.MANUAL_DELIVERY)
                .GetFieldSafe(nameof(UI.USERMENUACTIONS.MANUAL_DELIVERY.TOOLTIP_OFF), true);
            var Resolver = typeof(ManualDeliveryKGPatch).GetMethodSafe(nameof(ResolveTooltip), true, PPatchTools.AnyArguments);

            bool result = false;
            if (Tooltip1 != null && Tooltip2 != null && Resolver != null)
            {
                for (int i = 0; i < instructionsList.Count(); i++)
                {
                    var instruction = instructionsList[i];
                    if (instruction.opcode == OpCodes.Ldsfld && (instruction.operand is FieldInfo info) && (info == Tooltip1 || info == Tooltip2))
                    {
                        i++;
                        instructionsList.Insert(++i, new CodeInstruction(OpCodes.Ldarg_0));
                        instructionsList.Insert(++i, new CodeInstruction(OpCodes.Call, Resolver));
                        result = true;
#if DEBUG
                            PUtil.LogDebug($"'{methodName}' Transpiler injected");
#endif
                    }
                }
            }
            if (!result)
            {
                PUtil.LogWarning($"Could not apply Transpiler to the '{methodName}'");
            }
            return instructionsList;
        }
    }
}

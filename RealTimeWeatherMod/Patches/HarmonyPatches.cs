using System;
using System.Reflection;
using HarmonyLib;
using Bulbul;
using TMPro;
using ChillWithYou.EnvSync.Utils;
using ChillWithYou.EnvSync.Core;

namespace ChillWithYou.EnvSync.Patches
{
    [HarmonyPatch(typeof(UnlockItemService), "Setup")]
    internal static class UnlockServicePatch
    {
        static void Postfix(UnlockItemService __instance)
        {
            ChillEnvPlugin.UnlockItemServiceInstance = __instance;
            ChillEnvPlugin.TryInitializeOnce(__instance);
        }
    }

    [HarmonyPatch(typeof(EnviromentController), "Setup")]
    internal static class EnvControllerPatch
    {
        static void Postfix(EnviromentController __instance)
        {
            EnvRegistry.Register(__instance.EnvironmentType, __instance);
        }
    }

    [HarmonyPatch(typeof(FacilityEnviroment), "Setup")]
    internal static class FacilityEnvPatch
    {
        static void Postfix(FacilityEnviroment __instance)
        {
            try
            {
                FieldInfo field = typeof(FacilityEnviroment).GetField("_windowViewService", BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                {
                    object service = field.GetValue(__instance);
                    if (service != null)
                    {
                        ChillEnvPlugin.WindowViewServiceInstance = service;
                        ChillEnvPlugin.ChangeWeatherMethod = service.GetType().GetMethod("ChangeWeatherAndTime", BindingFlags.Instance | BindingFlags.Public);
                        if (ChillEnvPlugin.ChangeWeatherMethod != null)
                            ChillEnvPlugin.Log?.LogInfo("✅ Successfully captured WindowViewService.ChangeWeatherAndTime");
                    }
                }
            }
            catch (Exception ex) { ChillEnvPlugin.Log?.LogError($"Failed to capture Service: {ex}"); }
        }
    }

    [HarmonyPatch(typeof(CurrentDateAndTimeUI), "UpdateDateAndTime")]
    internal static class DateUIPatch
    {
        static void Postfix(CurrentDateAndTimeUI __instance)
        {
            if (ChillEnvPlugin.Cfg_ShowWeatherOnUI.Value && !string.IsNullOrEmpty(ChillEnvPlugin.UIWeatherString))
            {
                try
                {
                    var field = typeof(CurrentDateAndTimeUI).GetField("_dateText", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (field != null)
                    {
                        var textMesh = field.GetValue(__instance) as TextMeshProUGUI;
                        if (textMesh != null)
                            textMesh.text += $" | {ChillEnvPlugin.UIWeatherString}";
                    }
                }
                catch { }
            }

            if (ChillEnvPlugin.Cfg_DetailedTimeSegments.Value)
            {
                try
                {
                    var timeFormat = SaveDataManager.Instance.SettingData.TimeFormat.Value;
                    if (timeFormat.ToString() != "AMPM") return;

                    int hour = DateTime.Now.Hour;
                    string timeSegment = "Evening";

                    if (hour >= 0 && hour < 5) timeSegment = "Midnight";
                    else if (hour >= 5 && hour < 7) timeSegment = "Early Morning";
                    else if (hour >= 7 && hour < 11) timeSegment = "Morning";
                    else if (hour >= 11 && hour < 13) timeSegment = "Noon";
                    else if (hour >= 13 && hour < 18) timeSegment = "Afternoon";
                    else if (hour >= 18 && hour < 19) timeSegment = "Evening";
                    else if (hour >= 19 && hour <= 23) timeSegment = "Night";

                    var amPmField = typeof(CurrentDateAndTimeUI).GetField("_amPmText", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (amPmField != null)
                    {
                        var localizationBehaviour = amPmField.GetValue(__instance);
                        if (localizationBehaviour != null)
                        {
                            var textProp = localizationBehaviour.GetType().GetProperty("Text");
                            if (textProp != null)
                            {
                                var tmpro = textProp.GetValue(localizationBehaviour) as TextMeshProUGUI;
                                if (tmpro != null)
                                {
                                    tmpro.text = timeSegment;
                                }
                            }
                        }
                    }
                }
                catch { }
            }
        }
    }

    [HarmonyPatch(typeof(EnviromentController), "OnClickButtonMainIcon")]
    internal static class UserInteractionPatch
    {
        public static bool IsSimulatingClick = false;
        static void Prefix(EnviromentController __instance)
        {
            if (!IsSimulatingClick)
            {
                EnvironmentType type = __instance.EnvironmentType;
                if (!SceneryAutomationSystem.UserInteractedMods.Contains(type))
                {
                    SceneryAutomationSystem.UserInteractedMods.Add(type);
                    ChillEnvPlugin.Log?.LogInfo($"[User Interaction] User took over {type}, stopping auto-management.");
                }
                
                if (SceneryAutomationSystem._autoEnabledMods.Contains(type))
                {
                    SceneryAutomationSystem._autoEnabledMods.Remove(type);
                    ChillEnvPlugin.Log?.LogDebug($"[User Interaction] Removed {type} from managed list");
                }
                
                if (type == EnvironmentType.Whale && SceneryAutomationSystem.IsWhaleSystemTriggered)
                {
                    SceneryAutomationSystem.IsWhaleSystemTriggered = false;
                    ChillEnvPlugin.Log?.LogInfo("[Whale Easter Egg] User manually closed system-triggered whale, flag cleared");
                }
            }
        }
    }
}

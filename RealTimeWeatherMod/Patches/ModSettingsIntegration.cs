using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using BepInEx;

namespace ChillWithYou.EnvSync.Patches
{
    public class ModSettingsIntegration : MonoBehaviour
    {
        private static bool _settingsRegistered = false;

        private void Start()
        {
            StartCoroutine(RegisterSettingsWhenReady());
        }

        private IEnumerator RegisterSettingsWhenReady()
        {
            yield return null;

            float timeout = 10f;
            float elapsed = 0f;

            while (elapsed < timeout)
            {
                if (TryRegisterSettings())
                {
                    ChillEnvPlugin.Log?.LogInfo("✅ MOD settings successfully registered to game interface");
                    yield break;
                }

                yield return new WaitForSeconds(0.5f);
                elapsed += 0.5f;
            }

            ChillEnvPlugin.Log?.LogWarning("⚠️ ModSettingsManager not found, settings interface unavailable (iGPU Savior may not be installed)");
        }

        private bool TryRegisterSettings()
        {
            if (_settingsRegistered) return true;

            try
            {
                // Get ModSettingsManager
                Type managerType = Type.GetType("ModShared.ModSettingsManager, iGPU Savior");
                if (managerType == null)
                {
                    return false;
                }

                var instanceProp = managerType.GetProperty("Instance",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (instanceProp == null)
                {
                    ChillEnvPlugin.Log?.LogError("❌ ModSettingsManager.Instance property does not exist");
                    return false;
                }

                object managerInstance = instanceProp.GetValue(null);
                if (managerInstance == null)
                {
                    return false;
                }

                var isInitializedProp = managerType.GetProperty("IsInitialized");
                if (isInitializedProp == null)
                {
                    ChillEnvPlugin.Log?.LogError("❌ ModSettingsManager.IsInitialized property does not exist");
                    return false;
                }

                bool isInitialized = (bool)isInitializedProp.GetValue(managerInstance);
                if (!isInitialized)
                {
                    return false;
                }

                // ========== Register Settings (using new API) ==========

                bool allSuccess = true;
                if (!AddToggleSafe(managerInstance, managerType,
                    "Enable Weather API Sync",
                    ChillEnvPlugin.Cfg_EnableWeatherSync.Value,
                    (value) =>
                    {
                        ChillEnvPlugin.Cfg_EnableWeatherSync.Value = value;
                        ChillEnvPlugin.Instance.Config.Save();
                        ChillEnvPlugin.Log?.LogInfo($"[Settings] Weather API Sync: {value}");
                    }))
                {
                    allSuccess = false;
                }

                if (!AddToggleSafe(managerInstance, managerType,
                    "Show Weather on Date Bar",
                    ChillEnvPlugin.Cfg_ShowWeatherOnUI.Value,
                    (value) =>
                    {
                        ChillEnvPlugin.Cfg_ShowWeatherOnUI.Value = value;
                        ChillEnvPlugin.Instance.Config.Save();
                        ChillEnvPlugin.Log?.LogInfo($"[Settings] Show Weather Info: {value}");
                    }))
                {
                    allSuccess = false;
                }

                if (!AddToggleSafe(managerInstance, managerType,
                    "Show Detailed Time Segments (Midnight/Morning/etc.)",
                    ChillEnvPlugin.Cfg_DetailedTimeSegments.Value,
                    (value) =>
                    {
                        ChillEnvPlugin.Cfg_DetailedTimeSegments.Value = value;
                        ChillEnvPlugin.Instance.Config.Save();
                        ChillEnvPlugin.Log?.LogInfo($"[Settings] Detailed Time Segments: {value}");
                    }))
                {
                    allSuccess = false;
                }

                if (!AddToggleSafe(managerInstance, managerType,
                    "Enable Seasonal Easter Eggs & Ambient Sounds",
                    ChillEnvPlugin.Cfg_EnableEasterEggs.Value,
                    (value) =>
                    {
                        ChillEnvPlugin.Cfg_EnableEasterEggs.Value = value;
                        ChillEnvPlugin.Instance.Config.Save();
                        ChillEnvPlugin.Log?.LogInfo($"[Settings] Seasonal Easter Eggs: {value}");
                    }))
                {
                    allSuccess = false;
                }

                if (allSuccess)
                {
                    ChillEnvPlugin.Log?.LogInfo("✅ All settings successfully added");

                    // Fix layout offset issues
                    FixContentLayout(managerInstance, managerType);
                }

                _settingsRegistered = true;
                return true;
            }
            catch (Exception ex)
            {
                ChillEnvPlugin.Log?.LogError($"❌ Settings registration failed: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        private bool AddToggleSafe(object managerInstance, Type managerType,
            string label, bool defaultValue, Action<bool> callback)
        {
            try
            {
                // Get new AddToggle method (3 parameters: label, defaultValue, callback)
                var addToggleMethod = managerType.GetMethod("AddToggle", new Type[] {
                    typeof(string),
                    typeof(bool),
                    typeof(Action<bool>)
                });

                if (addToggleMethod == null)
                {
                    ChillEnvPlugin.Log?.LogError("❌ ModSettingsManager.AddToggle(string, bool, Action<bool>) method does not exist");
                    return false;
                }

                // Wrap callback to catch exceptions
                Action<bool> safeCallback = (value) =>
                {
                    try
                    {
                        callback?.Invoke(value);
                    }
                    catch (Exception ex)
                    {
                        ChillEnvPlugin.Log?.LogError($"❌ Callback exception for setting '{label}': {ex.Message}");
                    }
                };

                object result = addToggleMethod.Invoke(managerInstance, new object[] {
                    label,
                    defaultValue,
                    safeCallback
                });

                // Debug: Check returned GameObject
                GameObject toggleObj = result as GameObject;
                if (toggleObj != null)
                {
                    ChillEnvPlugin.Log?.LogInfo($"✅ Setting added: '{label}' → GameObject: {toggleObj.name}, Active: {toggleObj.activeSelf}");
                }
                else
                {
                    ChillEnvPlugin.Log?.LogInfo($"✅ Setting added: '{label}'");
                }

                return true;
            }
            catch (Exception ex)
            {
                ChillEnvPlugin.Log?.LogError($"❌ Failed to add setting '{label}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Fix content layout - resolve -145 offset issue
        /// </summary>
        private void FixContentLayout(object managerInstance, Type managerType)
        {
            try
            {
                // Get ModContentParent
                var contentParentProp = managerType.GetProperty("ModContentParent");
                if (contentParentProp == null)
                {
                    ChillEnvPlugin.Log?.LogWarning("⚠️ Unable to get ModContentParent property, skipping layout fix");
                    return;
                }

                GameObject contentParent = contentParentProp.GetValue(managerInstance) as GameObject;
                if (contentParent == null)
                {
                    ChillEnvPlugin.Log?.LogWarning("⚠️ ModContentParent is null, skipping layout fix");
                    return;
                }

                ConfigureContentLayout(contentParent);
                ChillEnvPlugin.Log?.LogInfo("✅ Content layout fixed");
            }
            catch (Exception ex)
            {
                ChillEnvPlugin.Log?.LogWarning($"⚠️ Layout fix failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Configure content layout - fix offset and alignment issues
        /// </summary>
        static void ConfigureContentLayout(GameObject content)
        {
            // 1. Force reset Content's RectTransform
            var rect = content.GetComponent<RectTransform>();
            if (rect != null)
            {
                // Key: Anchor Min=(0,1), Max=(1,1) -> horizontal stretch, top alignment
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(1, 1);
                rect.pivot = new Vector2(0.5f, 1f); // Pivot at top center
                rect.anchoredPosition = Vector2.zero; // Reset position
                rect.sizeDelta = new Vector2(0, 0); // Width auto-fit (controlled by parent), height controlled by Fitter
                rect.localScale = Vector3.one;
            }

            // 2. Configure vertical layout group
            var vGroup = content.GetComponent<VerticalLayoutGroup>() ?? content.AddComponent<VerticalLayoutGroup>();
            vGroup.spacing = 16f;
            // Padding: Left=60 (space for title), Right=40
            vGroup.padding = new RectOffset(60, 40, 20, 20);
            vGroup.childAlignment = TextAnchor.UpperLeft; // Force top-left alignment
            vGroup.childControlHeight = false;
            vGroup.childControlWidth = true;
            vGroup.childForceExpandHeight = false;
            vGroup.childForceExpandWidth = true;   // Force children to fill width

            // 3. Configure size fitter
            var fitter = content.GetComponent<ContentSizeFitter>() ?? content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        }
    }
}

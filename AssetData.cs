using ICities;
using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using System.IO;
using ColossalFramework.Packaging;
using ColossalFramework.UI;

namespace MovableBridge {
    // Whole point of this logic is to strip custom effects from assets on save and reattach them on load.
    // That way the asset remains compatible even when the mod is disabled!

    // Saving in asset editor
    [HarmonyPatch(typeof(SaveAssetPanel), "SaveRoutine")]
    public static class SaveRoutinePatch {
        public static void Prefix(string mapName) {
            AssetData.OnPreSaveAsset(mapName);
        }
    }

    // Loading in asset editor
    [HarmonyPatch(typeof(LoadAssetPanel), "OnLoad")]
    public static class OnLoadPatch {
        public static void Postfix(LoadAssetPanel __instance, UIListBox ___m_SaveList) {
            try {
                // Taken from LoadAssetPanel.OnLoad
                var selectedIndex = ___m_SaveList.selectedIndex;
                var getListingMetaDataMethod = typeof(LoadSavePanelBase<CustomAssetMetaData>).GetMethod(
                    "GetListingMetaData", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var listingMetaData = (CustomAssetMetaData)getListingMetaDataMethod.Invoke(__instance, new object[] { selectedIndex });


                // Taken from LoadingManager.LoadCustomContent
                if (listingMetaData.userDataRef != null) {
                    AssetDataWrapper.UserAssetData userAssetData = listingMetaData.userDataRef.Instantiate() as AssetDataWrapper.UserAssetData;
                    if (userAssetData == null) {
                        userAssetData = new AssetDataWrapper.UserAssetData();
                    }
                    AssetData.OnAssetLoadedImpl(listingMetaData.name, ToolsModifierControl.toolController.m_editPrefabInfo, userAssetData.Data);
                }
            } catch (Exception e) {
                Debug.LogError(e);
            }
        }
    }

    public class AssetData : AssetDataExtensionBase {
        private const int kVersion = 0;
        private const string kDataKey = "MovableBridgeAIData";

        private static MovableBridgeAIData m_Data;

        public static void OnPreSaveAsset(string assetName) {
            var prefab = ToolsModifierControl.toolController.m_editPrefabInfo;
            m_Data = StripCustomAI(prefab);
        }

        public override void OnAssetSaved(string name, object asset, out Dictionary<string, byte[]> userData) {
            if (m_Data == null) {
                userData = null;
                return;
            }

            Debug.Log($"Saving MovableBridgeAIData for {name}");

            using (var stream = new MemoryStream()) {
                using (var writer = new PackageWriter(stream)) {
                    writer.Write(kVersion);
                    m_Data.Write(writer);
                }

                userData = new Dictionary<string, byte[]>
                {
                    { kDataKey, stream.ToArray() }
                };
            }

            var prefab = ToolsModifierControl.toolController.m_editPrefabInfo;
            ApplyCustomAI(prefab, m_Data);
            m_Data = null;
        }

        public override void OnAssetLoaded(string name, object asset, Dictionary<string, byte[]> userData) {
            if (asset is PrefabInfo prefab && prefab.editorCategory == "MovableBridge") {
                OnAssetLoadedImpl(name, prefab, userData);
            }
        }

        public static void OnAssetLoadedImpl(string name, PrefabInfo prefab, Dictionary<string, byte[]> userData) {
            if (!userData.TryGetValue(kDataKey, out var bytes)) {
                return;
            }

            Debug.Log($"Found MovableBridgeAIData for {name}");

            MovableBridgeAIData data;
            using (var stream = new MemoryStream(bytes)) {
                using (var reader = new PackageReader(stream)) {
                    reader.ReadInt32(); // version

                    data = new MovableBridgeAIData();
                    data.Read(reader);
                }
            }

            ApplyCustomAI(prefab, data);
        }

        private static MovableBridgeAIData StripCustomAI(PrefabInfo prefab) {
            if (prefab is BuildingInfo buildingInfo && buildingInfo.m_buildingAI is MovableBridgeAI customAI) {
                if (prefab.editorCategory != "MovableBridge") {
                    throw new Exception("Missing 'MovableBridge' editorCategory!");
                }

                PlayerBuildingAI vanillaAI = buildingInfo.gameObject.AddComponent<PlayerBuildingAI>();
                vanillaAI.CopyFrom(customAI);

                var data = new MovableBridgeAIData();
                data.CopyFrom(customAI);

                UnityEngine.Object.DestroyImmediate(customAI);

                buildingInfo.m_buildingAI = vanillaAI;
                vanillaAI.m_info = buildingInfo;

                UnityEngine.Debug.Log("Stripped " + data.ToString());

                return data;
            } else {
                return null;
            }
        }

        private static void ApplyCustomAI(PrefabInfo prefab, MovableBridgeAIData data) {
            if (prefab is BuildingInfo buildingInfo && buildingInfo.m_buildingAI is PlayerBuildingAI existingAI) {
                UnityEngine.Debug.Log("Creating MovableBridgeAI and applying " + data.ToString());

                if (existingAI is MovableBridgeAI customAI) {
                    UnityEngine.Debug.Log("Applying " + data.ToString());
                    data.CopyTo(customAI);
                } else {
                    UnityEngine.Debug.Log("Creating MovableBridgeAI and applying " + data.ToString());
                    customAI = buildingInfo.gameObject.AddComponent<MovableBridgeAI>();
                    data.CopyTo(customAI);
                    customAI.CopyFrom(existingAI);
                    UnityEngine.Object.DestroyImmediate(existingAI);
                    
                    buildingInfo.m_buildingAI = customAI;
                    customAI.m_info = buildingInfo;
                    customAI.InitializePrefab();
                }
            }
        }
    }

    public class MovableBridgeAIData {
        public int m_PreOpeningDuration = 2;
        public int m_OpeningDuration = 1;
        public int m_ClosingDuration = 1;
        public float m_BridgeClearance = 4f;

        public override string ToString() {
            return $"{nameof(m_PreOpeningDuration)}: {m_PreOpeningDuration}, {nameof(m_OpeningDuration)}: {m_OpeningDuration}, {nameof(m_ClosingDuration)}: {m_ClosingDuration}, {nameof(m_BridgeClearance)}: {m_BridgeClearance}";
        }

        public void CopyFrom(MovableBridgeAI ai) {
            m_PreOpeningDuration = ai.m_PreOpeningDuration;
            m_OpeningDuration = ai.m_OpeningDuration;
            m_ClosingDuration = ai.m_ClosingDuration;
            m_BridgeClearance = ai.m_BridgeClearance;
        }

        public void CopyTo(MovableBridgeAI ai) {
            ai.m_PreOpeningDuration = m_PreOpeningDuration;
            ai.m_OpeningDuration = m_OpeningDuration;
            ai.m_ClosingDuration = m_ClosingDuration;
            ai.m_BridgeClearance = m_BridgeClearance;
        }

        public void Read(PackageReader reader) {
            m_PreOpeningDuration = reader.ReadInt32();
            m_OpeningDuration = reader.ReadInt32();
            m_ClosingDuration = reader.ReadInt32();
            m_BridgeClearance = reader.ReadSingle();
        }

        public void Write(PackageWriter writer) {
            writer.Write(m_PreOpeningDuration);
            writer.Write(m_OpeningDuration);
            writer.Write(m_ClosingDuration);
            writer.Write(m_BridgeClearance);
        }
    }
}

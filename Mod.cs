﻿using CitiesHarmony.API;
using ICities;

// ReSharper disable InconsistentNaming

namespace MovableBridge {
    public class Mod : IUserMod {
        public string Name => "Movable Bridge Mod - Fixed";
        public string Description => "Functional movable bridges for trains, cars and pedestrians";

        public void OnEnabled() {
            HarmonyHelper.DoOnHarmonyReady(Patcher.Patch);
        }

        public void OnDisabled() {
            if (HarmonyHelper.IsHarmonyInstalled) Patcher.Unpatch();
        }

        public static bool IsInGame {
            get {
                var updateMode = SimulationManager.instance.m_metaData.m_updateMode;
                return updateMode == SimulationManager.UpdateMode.NewGameFromMap ||
                       updateMode == SimulationManager.UpdateMode.NewGameFromScenario ||
                       updateMode == SimulationManager.UpdateMode.LoadGame;
            }
        }

        public static bool IsInAssetEditor {
            get {
                var updateMode = SimulationManager.instance.m_metaData.m_updateMode;
                return updateMode == SimulationManager.UpdateMode.NewAsset ||
                       updateMode == SimulationManager.UpdateMode.LoadAsset;
            }
        }
    }
}

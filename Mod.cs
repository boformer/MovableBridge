using CitiesHarmony.API;
using ICities;

// ReSharper disable InconsistentNaming

namespace MovableBridge {
    public class Mod : IUserMod {
        public string Name => "Movable Bridge Mod";
        public string Description => "Functional movable bridges for cars and pedestrians";

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
    }
}

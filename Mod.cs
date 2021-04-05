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
    }
}

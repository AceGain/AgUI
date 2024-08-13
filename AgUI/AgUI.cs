using HarmonyLib;
using System.Reflection;

public class AgUI : IModApi
{
    public void InitMod(Mod _modInstance)
    {
        this.HarmonyPatch();
    }

    private void HarmonyPatch()
    {
        Log.Out("AceGame RepairLimit Harmony Patch: {0}", base.GetType().ToString());
        Harmony harmony = new Harmony(base.GetType().ToString());
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }
}

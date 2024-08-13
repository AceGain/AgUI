using HarmonyLib;

namespace HarmonyInject
{
    public class Patch_XUiC_TargetBar
    {
        [HarmonyPatch(typeof(XUiC_TargetBar))]
        [HarmonyPatch(nameof(XUiC_TargetBar.GetBindingValue))]
        public class Point_GetBindingValue
        {
            [HarmonyPostfix]
            public static void Postfix(XUiC_TargetBar __instance, ref bool __result, ref string value, string bindingName)
            {
                if (!__result)
                {
                    switch (bindingName)
                    {
                        case "noboss_sprite":
                            value = Point_GetBindingValue.GetIcon(__instance.Target);
                            __result = true;
                            break;
                        default:
                            __result = false;
                            break;
                    }
                }
            }

            public static string GetIcon(EntityAlive target)
            {
                if (target == null)
                {
                    return "";
                }
                if (TryGetCustomIconAtlas(target, out string value))
                {
                    return value;
                }
                if (target is EntityZombie)
                {
                    return "ui_game_symbol_zombie";
                }
                if (target is EntityVehicle)
                {
                    EntityClass.list[target.entityClass].Properties.Values.TryGetValue(EntityClass.PropMapIcon, out value);
                    return value;
                }
                if (target is EntityAnimal)
                {
                    EntityClass.list[target.entityClass].Properties.Values.TryGetValue(EntityClass.PropTrackerIcon, out value);
                    return value;
                }
                if (target is EntityEnemyAnimal)
                {
                    EntityClass.list[target.entityClass].Properties.Values.TryGetValue(EntityClass.PropTrackerIcon, out value);
                    return value;
                }
                if (target is EntityTrader)
                {
                    return "ui_game_symbol_map_trader";
                }
                return "ui_game_symbol_other";
            }

            public static bool TryGetCustomIconAtlas(EntityAlive _target, out string _result)
            {
                _result = default;
                if (_target == null)
                {
                    return false;
                }
                string classNameByXml = EntityClass.list[_target.entityClass].entityClassName;
                if (string.IsNullOrEmpty(classNameByXml))
                {
                    return false;
                }
                bool hasManager = ModManager.atlasManagers.TryGetValue("UIAtlas", out var atlasManager);
                if (!hasManager)
                {
                    return false;
                }
                classNameByXml = "ag_ui_game_symbol_" + classNameByXml;
                bool hasAtlas = atlasManager.Manager.atlasesForSprites.TryGetValue(classNameByXml, out var baseAtlas);
                if (!hasAtlas)
                {
                    return false;
                }
                if (baseAtlas == null)
                {
                    return false;
                }
                _result = classNameByXml;
                return true;
            }
        }
    }
}
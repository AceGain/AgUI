using HarmonyLib;

namespace HarmonyInject
{
    public class Patch_XUiC_WindowSelector
    {
        [HarmonyPatch(typeof(XUiC_WindowSelector))]
        [HarmonyPatch(nameof(XUiC_WindowSelector.OnOpen))]
        public class Point_OnOpen
        {
            [HarmonyPostfix]
            public static void Postfix(XUiC_WindowSelector __instance)
            {
                __instance.xui.GetWindow("agWindowMiniMap").IsVisible = false;
            }
        }

        [HarmonyPatch(typeof(XUiC_WindowSelector))]
        [HarmonyPatch(nameof(XUiC_WindowSelector.OnClose))]
        public class Point_OnClose
        {
            [HarmonyPostfix]
            public static void Postfix(XUiC_WindowSelector __instance)
            {
                if (__instance.xui.FindWindowGroupByName("toolbelt").IsOpen)
                {
                    __instance.xui.GetWindow("agWindowMiniMap").IsVisible = true;
                }
            }
        }
    }
}

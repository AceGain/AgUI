using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonyInject
{
    public class Patch_XUiC_WindowNonPagingHeader
    {
        [HarmonyPatch(typeof(XUiC_WindowNonPagingHeader))]
        [HarmonyPatch(nameof(XUiC_WindowNonPagingHeader.OnOpen))]
        public class Point_OnOpen
        {
            [HarmonyPostfix]
            public static void Postfix(XUiC_WindowNonPagingHeader __instance)
            {
                __instance.xui.GetWindow("agWindowMiniMap").IsVisible = false;
            }
        }

        [HarmonyPatch(typeof(XUiC_WindowNonPagingHeader))]
        [HarmonyPatch(nameof(XUiC_WindowNonPagingHeader.OnClose))]
        public class Point_OnClose
        {
            [HarmonyPostfix]
            public static void Postfix(XUiC_WindowNonPagingHeader __instance)
            {
                if (__instance.xui.FindWindowGroupByName("toolbelt").IsOpen)
                {
                    __instance.xui.GetWindow("agWindowMiniMap").IsVisible = true;
                }
            }
        }
    }
}

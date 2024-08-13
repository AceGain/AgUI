using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonyInject
{
    public class Patch_XUiC_MainMenu
    {
        [HarmonyPatch(typeof(XUiC_MainMenu))]
        [HarmonyPatch(nameof(XUiC_MainMenu.Open))]
        public class Point_XUiC_MainMenu
        {
            [HarmonyPrefix]
            public static bool Prefix(XUiC_MainMenu __instance, XUi _xuiInstance)
            {
                _xuiInstance.playerUI.windowManager.Open(XUiC_MainMenu.ID, true, false, true);
                //XUiC_MainMenu.shownNewsScreenOnce = true;
                return false;
            }
        }
    }
}

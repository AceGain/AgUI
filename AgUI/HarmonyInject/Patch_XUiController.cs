using HarmonyLib;

namespace HarmonyInject
{
    public class Patch_XUiController
    {
        [HarmonyPatch(typeof(XUiController))]
        [HarmonyPatch(nameof(XUiController.Init))]
        public class Point_Init
        {
            [HarmonyReversePatch]
            public static void Reverse(XUiController __instance) { }
        }

        [HarmonyPatch(typeof(XUiController))]
        [HarmonyPatch(nameof(XUiController.OnOpen))]
        public class Point_OnOpen
        {
            [HarmonyReversePatch]
            public static void Reverse(XUiController __instance) { }
        }

        [HarmonyPatch(typeof(XUiController))]
        [HarmonyPatch(nameof(XUiController.OnClose))]
        public class Point_OnClose
        {
            [HarmonyReversePatch]
            public static void Reverse(XUiController __instance) { }
        }

        [HarmonyPatch(typeof(XUiController))]
        [HarmonyPatch(nameof(XUiController.OnCursorSelected))]
        public class Point_OnCursorSelected
        {
            [HarmonyReversePatch]
            public static void Reverse(XUiController __instance) { }
        }

        [HarmonyPatch(typeof(XUiController))]
        [HarmonyPatch(nameof(XUiController.OnCursorUnSelected))]
        public class Point_OnCursorUnSelected
        {
            [HarmonyReversePatch]
            public static void Reverse(XUiController __instance) { }
        }

        [HarmonyPatch(typeof(XUiController))]
        [HarmonyPatch(nameof(XUiController.Update))]
        public class Point_Update
        {
            [HarmonyReversePatch]
            public static void Reverse(XUiController __instance, float _dt) { }
        }

        [HarmonyPatch(typeof(XUiController))]
        [HarmonyPatch(nameof(XUiController.Cleanup))]
        public class Point_Cleanup
        {
            [HarmonyReversePatch]
            public static void Reverse(XUiController __instance) { }
        }
    }
}
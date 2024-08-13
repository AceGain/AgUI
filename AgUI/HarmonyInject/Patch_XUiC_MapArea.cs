using HarmonyLib;
using UnityEngine;


namespace HarmonyInject
{
    public class Patch_XUiC_MapArea
    {

        [HarmonyPatch(typeof(XUiC_MapArea))]
        [HarmonyPatch(nameof(XUiC_MapArea.Update))]
        public class Point_Update
        {
            [HarmonyPrefix]
            public static void Prefix(XUiC_MapArea __instance)
            {
                //Log.Out("==========地图位置：{0}", __instance.mapMiddlePosPixel.ToString());
            }
        }

        [HarmonyPatch(typeof(XUiC_MapArea))]
        [HarmonyPatch(nameof(XUiC_MapArea.positionMap))]
        public class Point_PositionMap
        {
            //[HarmonyPrefix]
            //public static void Prefix(XUiC_MapArea __instance)
            //{

            //}

            [HarmonyPostfix]
            public static void Postfix(XUiC_MapArea __instance)
            {
                //Log.Out("==========mapMiddlePosPixel:{0};mapMiddlePosChunks:{1};mapScrollTextureOffset:{2}", __instance.mapMiddlePosPixel, __instance.mapMiddlePosChunks, __instance.mapScrollTextureOffset);
                //Log.Out("==========mapScale:{0};mapPos:{1};mapBGPos:{2};zoomScale:{3}", __instance.mapScale, __instance.mapPos, __instance.mapBGPos, __instance.zoomScale);
            }
        }

        [HarmonyPatch(typeof(XUiC_MapArea))]
        [HarmonyPatch(nameof(XUiC_MapArea.updateMapSection))]
        public class Point_UpdateMapSection
        {
            [HarmonyPostfix]
            public static void Postfix(XUiC_MapArea __instance, int mapStartX, int mapStartZ, int mapEndX, int mapEndZ, int drawnMapStartX, int drawnMapStartZ, int drawnMapEndX, int drawnMapEndZ)
            {
                //Log.Out("==========地图位置：{0}", __instance.mapMiddlePosPixel.ToString());
                //Log.Out("==========玩家位置：{0}", __instance.localPlayer.GetPosition().ToString());
                //Log.Out("==========地图比例：{0}；地图位置：{1}；地图背景：{2}；滚动偏移：{3}", __instance.mapScale, __instance.mapPos.ToString(), __instance.mapBGPos.ToString(), __instance.mapScrollTextureOffset.ToString());
                //Log.Out("==========地图位置：mapStartX={0}，mapEndX={1}，mapStartZ={2}，mapEndZ={3}，", mapStartX, mapEndX, mapStartZ, mapEndZ);
                //Log.Out("==========地图绘制：drawnMapStartX={0}，drawnMapEndX={1}，drawnMapStartZ={2}，drawnMapEndZ={3}，", drawnMapStartX, drawnMapEndX, drawnMapStartZ, drawnMapEndZ);
            }
        }

        [HarmonyPatch(typeof(XUiC_MapArea))]
        [HarmonyPatch(nameof(XUiC_MapArea.DragMap))]
        public class Point_DragMap
        {
            [HarmonyPrefix]
            public static void Prefix(XUiC_MapArea __instance, Vector2 delta)
            {
                Log.Out("--------------------------------------------------------------");
                Log.Out("==========位置增量：{0}", delta.ToString());
                Log.Out("==========地图变更前位置：{0}", __instance.mapMiddlePosPixel.ToString());
                Log.Out("==========玩家位置：{0}", __instance.localPlayer.GetPosition().ToString());
            }

            [HarmonyPostfix]
            public static void Postfix(XUiC_MapArea __instance)
            {
                Log.Out("==========地图变更后位置：{0}", __instance.mapMiddlePosPixel.ToString());
            }
        }

        [HarmonyPatch(typeof(XUiC_MapArea))]
        [HarmonyPatch(nameof(XUiC_MapArea.updateMapForScroll))]
        public class Point_updateMapForScroll
        {
            [HarmonyPrefix]
            public static void Prefix(XUiC_MapArea __instance, int deltaChunksX, int deltaChunksZ)
            {
                Log.Out("==========地图增量：{0},{1}", deltaChunksX,deltaChunksZ);
            }
        }


        [HarmonyPatch(typeof(XUiC_MapArea))]
        [HarmonyPatch(nameof(XUiC_MapArea.Init))]
        public class Point_Init
        {
            [HarmonyPrefix]
            public static void Prefix(XUiC_MapArea __instance)
            {
                //__instance.zoomScale = 0.5f;
            }

        }

        [HarmonyPatch(typeof(XUiC_MapArea))]
        [HarmonyPatch(nameof(XUiC_MapArea.OnOpen))]
        public class Point_OnOpen
        {
            [HarmonyPrefix]
            public static void Prefix(XUiC_MapArea __instance)
            {
                //Log.Out("====================OnOpen Prefix Start");
                //Log.Out("====================地图比例大小：{0}", __instance.mapScale);
                //Log.Out("====================地图滚动大小：{0}", __instance.mapScrollTextureOffset.ToString());
                //Log.Out("====================地图展示位置：{0}", __instance.mapPos.ToString());
                //Log.Out("====================地图背景位置：{0}", __instance.mapBGPos.ToString());
                //Log.Out("====================OnOpen Prefix End");
            }

            [HarmonyPostfix]
            public static void Postfix(XUiC_MapArea __instance)
            {
                //Log.Out("====================OnOpen Postfix Start");
                //Log.Out("====================地图比例大小：{0}", __instance.mapScale);
                //Log.Out("====================地图滚动大小：{0}", __instance.mapScrollTextureOffset.ToString());
                //Log.Out("====================地图展示位置：{0}", __instance.mapPos.ToString());
                //Log.Out("====================地图背景位置：{0}", __instance.mapBGPos.ToString());
                //Log.Out("====================OnOpen Postfix End");
            }

        }

        [HarmonyPatch(typeof(XUiC_MapArea))]
        [HarmonyPatch(nameof(XUiC_MapArea.updateMapSection))]
        public class Point_updateMapSection
        {
            [HarmonyPrefix]
            public static void Prefix(XUiC_MapArea __instance, int mapStartX, int mapStartZ, int mapEndX, int mapEndZ, int drawnMapStartX, int drawnMapStartZ, int drawnMapEndX, int drawnMapEndZ)
            {
                //Log.Out("====================OnOpen Prefix Start");
                //Log.Out("====================地图展示位置：StartX={0},EndX={1},StartZ={2},EndZ={3}", mapStartX, mapEndX, mapStartZ, mapEndZ);
                //Log.Out("====================地图绘制位置：DrawnStartX={0},DrawnEndX={1},DrawnStartZ={2},DrawnEndZ={3}", drawnMapStartX, drawnMapEndX, drawnMapStartZ, drawnMapEndZ);
                //Log.Out("====================OnOpen Prefix End");
            }

        }

    }
}

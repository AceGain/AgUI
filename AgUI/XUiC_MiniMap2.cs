using Audio;
using GUI_2;
using HarmonyInject;
using InControl;
using Platform;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using static UIPopupList;

public class XUiC_MiniMap2 : XUiC_MapArea
{
    public Vector3 playerLastPos;
    public override void Init()
    {
        Patch_XUiController.Point_Init.Reverse(this);
        if (mapTexture == null)
        {
            mapTexture = new Texture2D(2048, 2048, TextureFormat.RGBA32, mipChain: false);
            mapTexture.name = "XUiC_MapArea.mapTexture";
        }
        NativeArray<Color32> rawTextureData = mapTexture.GetRawTextureData<Color32>();
        Color32 value = new Color32(0, 0, 0, byte.MaxValue);
        for (int i = 0; i < rawTextureData.Length; i += 4)
        {
            rawTextureData[i] = value;
        }
        XUiController childById = GetChildById("mapViewTexture");
        xuiTexture = (XUiV_Texture)childById.ViewComponent;
        transformSpritesParent = GetChildById("clippingPanel").ViewComponent.UiTransform;
        mapView = GetChildById("mapView");
        //mapView.OnDrag += onMapDragged;
        //mapView.OnScroll += onMapScrolled;
        //mapView.OnPress += onMapPressedLeft;
        //mapView.OnRightPress += onMapPressed;
        //mapView.OnHover += onMapHover;
        zoomScale = 1f;
        targetZoomScale = 1f;
        base.xui.LoadData("Prefabs/MapSpriteEntity", (GameObject o) =>
        {
            prefabMapSprite = o;
        });
        base.xui.LoadData("Prefabs/MapSpriteStartPoint", (GameObject o) =>
        {
            prefabMapSpriteStartPoint = o;
        });
        base.xui.LoadData("Prefabs/MapSpritePrefab", (GameObject o) =>
        {
            prefabMapSpritePrefab = o;
        });
        base.xui.LoadData("Prefabs/MapSpriteEntitySpawner", (GameObject o) =>
        {
            prefabMapSpriteEntitySpawner = o;
        });
        initFOWChunkMaskColors();
        //GetChildById("playerIcon").OnPress += onPlayerIconPressed;
        //uiLblPlayerPos = (XUiV_Label)GetChildById("playerPos").ViewComponent;
        //uiLblCursorPos = (XUiV_Label)GetChildById("cursorPos").ViewComponent;
        //GetChildById("bedrollIcon").OnPress += onBedrollIconPressed;
        //uiLblBedrollPos = (XUiV_Label)GetChildById("bedrollPos").ViewComponent;
        //GetChildById("waypointIcon").OnPress += onWaypointIconPressed;
        //uiLblMapMarkerDistance = (XUiV_Label)GetChildById("waypointDistance").ViewComponent;
        //switchStaticMap = (XUiV_Button)GetChildById("switchStaticMap").ViewComponent;
        //GetChildById("switchStaticMap").OnPress += (XUiController _sender, int _args) =>
        //{
        //    showStaticData = !showStaticData;
        //    switchStaticMap.Selected = showStaticData;
        //    cbxStaticMapType.ViewComponent.IsVisible = showStaticData;
        //};
        //cbxStaticMapType = GetChildByType<XUiC_ComboBoxEnum<EStaticMapOverlay>>();
        //cbxStaticMapType.OnValueChanged += CbxStaticMapType_OnValueChanged;
        bShouldRedrawMap = true;
        this.InitMiniMap();
        //kilometers = Localization.Get("xuiKilometers");
        crosshair = GetChildById("crosshair").ViewComponent as XUiV_Sprite;
        NavObjectManager.Instance.OnNavObjectRemoved += Instance_OnNavObjectRemoved;
        //if (GameManager.Instance.IsEditMode() && !PrefabEditModeManager.Instance.IsActive())
        //{
        //    showStaticData = true;
        //    switchStaticMap.Selected = true;
        //    cbxStaticMapType.Value = EStaticMapOverlay.Biomes;
        //}
        mapView.ViewComponent.IsSnappable = false;
        childById.ViewComponent.IsSnappable = false;
        crosshair.isVisible = true;
    }

    public override void OnOpen()
    {
        Patch_XUiController.Point_OnOpen.Reverse(this);
        closeAllPopups();
        //base.xui.playerUI.windowManager.OpenIfNotOpen("windowpaging", _bModal: false);
        if (!isOpen)
        {
            isOpen = true;
            localPlayer = base.xui.playerUI.entityPlayer;
            //bFowMaskEnabled = !GameManager.Instance.IsEditMode();
            //switchStaticMap.IsVisible = SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer && (GameManager.Instance.IsEditMode() || GamePrefs.GetBool(EnumGamePrefs.DebugMenuEnabled));
            //cbxStaticMapType.ViewComponent.IsVisible = switchStaticMap.IsVisible && showStaticData;
            initExistingWaypoints(localPlayer.Waypoints);
            this.InitMiniMap();
            positionMiniMap();
            PositionMiniMapAt(localPlayer.GetPosition());
            base.xui.playerUI.GetComponentInParent<LocalPlayerCamera>().PreRender += OnPreRender;
            base.xui.calloutWindow.ClearCallouts(XUiC_GamepadCalloutWindow.CalloutType.Menu);
            base.xui.calloutWindow.AddCallout(UIUtils.ButtonIcon.RightStickLeftRightUpDown, "igcoMapMoveNoHold", XUiC_GamepadCalloutWindow.CalloutType.Menu);
            base.xui.calloutWindow.AddCallout(UIUtils.ButtonIcon.FaceButtonSouth, "igcoMapWaypoint", XUiC_GamepadCalloutWindow.CalloutType.Menu);
            base.xui.calloutWindow.AddCallout(UIUtils.ButtonIcon.RightTrigger, "igcoMapZoomIn", XUiC_GamepadCalloutWindow.CalloutType.Menu);
            base.xui.calloutWindow.AddCallout(UIUtils.ButtonIcon.LeftTrigger, "igcoMapZoomOut", XUiC_GamepadCalloutWindow.CalloutType.Menu);
            base.xui.calloutWindow.AddCallout(UIUtils.ButtonIcon.FaceButtonEast, "igcoExit", XUiC_GamepadCalloutWindow.CalloutType.Menu);
            base.xui.calloutWindow.EnableCallouts(XUiC_GamepadCalloutWindow.CalloutType.Menu);
            //base.xui.FindWindowGroupByName("windowpaging").GetChildByType<XUiC_WindowSelector>()?.SetSelected("map");
            crosshair.IsVisible = true;
            windowGroup.isEscClosable = false;
        }
    }

    public override void OnClose()
    {
        Patch_XUiController.Point_OnClose.Reverse(this);
        //base.xui.playerUI.windowManager.CloseIfOpen("windowpaging");
        if (isOpen)
        {
            isOpen = false;
            bShouldRedrawMap = false;
            if (bMapCursorSet)
            {
                SetMapCursor(_customCursor: false);
                base.xui.currentToolTip.ToolTip = string.Empty;
            }
            base.xui.playerUI.GetComponentInParent<LocalPlayerCamera>().PreRender -= OnPreRender;
            base.xui.calloutWindow.DisableCallouts(XUiC_GamepadCalloutWindow.CalloutType.Menu);
            base.xui.playerUI.CursorController.Locked = false;
            SoftCursor.SetCursor(CursorControllerAbs.ECursorType.Default);
            closestMouseOverNavObject = null;
        }
    }

    public override void OnCursorSelected()
    {
        Patch_XUiController.Point_OnCursorSelected.Reverse(this);
        crosshair.IsVisible = PlatformManager.NativePlatform.Input.CurrentInputStyle != PlayerInputManager.InputStyle.Keyboard;
    }

    public override void OnCursorUnSelected()
    {
        Patch_XUiController.Point_OnCursorUnSelected.Reverse(this);
        crosshair.IsVisible = false;
    }

    public override void Update(float _dt)
    {
        Patch_XUiController.Point_Update.Reverse(this, _dt);
        if (!windowGroup.isShowing || !XUi.IsGameRunning() || base.xui.playerUI.entityPlayer == null)
        {
            return;
        }
        if (!bMapInitialized)
        {
            this.InitMiniMap();
        }
        if (base.xui.playerUI.playerInput.GUIActions.LastDeviceClass == InputDeviceClass.Controller && !base.xui.GetWindow("mapAreaSetWaypoint").IsVisible)
        {
            Vector2 vector = -base.xui.playerUI.playerInput.GUIActions.Camera.Vector;
            if (vector.sqrMagnitude > 0f)
            {
                DragMiniMap(vector * 500f * _dt);
            }
        }

        if (!base.xui.playerUI.entityPlayer.IsMoveStateStill())
        {
            Vector3 playerCurrentPos = localPlayer.GetPosition();
            if (!playerCurrentPos.Equals(playerLastPos))
            {
                Vector2 delta = new Vector2(playerLastPos.x - playerCurrentPos.x, playerLastPos.z - playerCurrentPos.z);
                DragMiniMap(delta);
                playerLastPos = playerCurrentPos;
            }
        }

        //this.UpdateMiniMapOverlay();
        if (bShouldRedrawMap)
        {
            UpdateFullMiniMap();
            bShouldRedrawMap = false;
        }
        if (timeToRedrawMap > 0f)
        {
            timeToRedrawMap -= _dt;
            if (timeToRedrawMap <= 0f)
            {
                bShouldRedrawMap = true;
            }
        }
        if (localPlayer.ChunkObserver.mapDatabase.IsNetworkDataAvail())
        {
            timeToRedrawMap = 0.5f;
            localPlayer.ChunkObserver.mapDatabase.ResetNetworkDataAvail();
        }
        //uiLblPlayerPos.Text = ValueDisplayFormatters.WorldPos(base.xui.playerUI.entityPlayer.GetPosition());
        //Vector3 pos = screenPosToWorldPos(base.xui.playerUI.CursorController.GetScreenPosition());
        //uiLblCursorPos.Text = ValueDisplayFormatters.WorldPos(pos);
        //uiLblCursorPos.UiTransform.gameObject.SetActive(bMouseOverMap);
        //uiLblBedrollPos.Text = ((localPlayer.SpawnPoints.Count > 0) ? ValueDisplayFormatters.WorldPos(localPlayer.SpawnPoints[0].ToVector3()) : string.Empty);
        //string text = string.Empty;
        //if (localPlayer.markerPosition != Vector3i.zero)
        //{
        //    text = string.Format("{0} {1}", ((localPlayer.position - localPlayer.markerPosition.ToVector3()).magnitude / 1000f).ToCultureInvariantString("0.0"), kilometers);
        //}
        //uiLblMapMarkerDistance.Text = text;
        float num = 5f * base.xui.playerUI.playerInput.GUIActions.TriggerAxis.Value;
        if (num != 0f)
        {
            targetZoomScale = Utils.FastClamp(targetZoomScale + num * _dt, 0.7f, 6.15f);
        }
        zoomScale = Mathf.Lerp(zoomScale, targetZoomScale, 5f * _dt);
        positionMiniMap();
        this.updateMiniMapObjects();
        UpdateWaypointSelection();
        if (base.xui.playerUI.playerInput.GUIActions.Cancel.WasPressed || base.xui.playerUI.playerInput.PermanentActions.Cancel.WasPressed)
        {
            XUiV_Window window = base.xui.GetWindow("mapAreaSetWaypoint");
            if (window.IsVisible)
            {
                window.IsVisible = false;
                base.xui.GetWindow("mapAreaChooseWaypoint").IsVisible = false;
                base.xui.GetWindow("mapAreaEnterWaypointName").IsVisible = false;
                base.xui.playerUI.CursorController.SetNavigationTargetLater(GetChildById("MapView").ViewComponent);
            }
            else
            {
                base.xui.playerUI.windowManager.CloseAllOpenWindows(null, _fromEsc: true);
            }
        }
    }


    public void UpdateMiniMapOverlay()
    {
        //base.updateMapOverlay();

        //if (showStaticData == (staticWorldTexture != null))
        //{
        //    return;
        //}
        //bShouldRedrawMap = true;
        //if (!showStaticData)
        //{
        //    staticWorldTexture = null;
        //}
        //else
        //{
        //    if (staticWorldTexture != null)
        //    {
        //        return;
        //    }
        //    World world = GameManager.Instance.World;
        //    if (!(world.ChunkCache.ChunkProvider is ChunkProviderGenerateWorldFromRaw chunkProviderGenerateWorldFromRaw))
        //    {
        //        return;
        //    }
        //    IBiomeProvider biomeProvider = chunkProviderGenerateWorldFromRaw.GetBiomeProvider();
        //    WorldDecoratorPOIFromImage poiFromImage = chunkProviderGenerateWorldFromRaw.poiFromImage;
        //    if (biomeProvider == null || poiFromImage == null)
        //    {
        //        return;
        //    }
        //    int splat3Width = poiFromImage.splat3Width;
        //    int splat3Height = poiFromImage.splat3Height;
        //    int num = splat3Width / 2;
        //    int num2 = splat3Height / 2;
        //    staticWorldWidth = splat3Width;
        //    staticWorldHeight = splat3Height;
        //    staticMapLeft = -staticWorldWidth / 2;
        //    staticMapRight = staticWorldWidth / 2 - 1;
        //    staticMapBottom = -staticWorldHeight / 2;
        //    staticMapTop = staticWorldHeight / 2 - 1;
        //    staticWorldTexture = new Color32[poiFromImage.m_Poi.DimX * poiFromImage.m_Poi.DimY];
        //    Color32 color = new Color32(0, 0, 0, byte.MaxValue);
        //    for (int i = 0; i < splat3Height; i++)
        //    {
        //        int num3 = i * splat3Width;
        //        ReadOnlySpan<ushort> span;
        //        using (chunkProviderGenerateWorldFromRaw.heightData.GetReadOnlySpan(num3, splat3Width, out span))
        //        {
        //            for (int j = 0; j < splat3Width; j++)
        //            {
        //                color.r = 0;
        //                color.g = 0;
        //                color.b = 0;
        //                byte value = poiFromImage.m_Poi.colors.GetValue(j, i);
        //                PoiMapElement poiForColor;
        //                if (value == 1)
        //                {
        //                    color.r = (color.b = byte.MaxValue);
        //                }
        //                else if (value == 2)
        //                {
        //                    color.r = byte.MaxValue;
        //                }
        //                else if (value == 3)
        //                {
        //                    color.g = byte.MaxValue;
        //                }
        //                else if (value == 4)
        //                {
        //                    color.b = byte.MaxValue;
        //                }
        //                else if (value != 0 && (poiForColor = world.Biomes.getPoiForColor(value)) != null && poiForColor.m_BlockValue.Block.blockMaterial.IsLiquid)
        //                {
        //                    color.b = byte.MaxValue;
        //                }
        //                else
        //                {
        //                    byte b = (byte)((float)(int)span[j] * 0.0038910506f);
        //                    color.r = (color.g = (color.b = b));
        //                    //if (cbxStaticMapType.Value == EStaticMapOverlay.Biomes)
        //                    //{
        //                    //    BiomeDefinition biomeAt = biomeProvider.GetBiomeAt(j - num, i - num2);
        //                    //    if (biomeAt != null)
        //                    //    {
        //                    //        color.r = (byte)Mathf.LerpUnclamped((int)color.r, (biomeAt.m_uiColor >> 16) & 0xFFu, 0.25f);
        //                    //        color.g = (byte)Mathf.LerpUnclamped((int)color.g, (biomeAt.m_uiColor >> 8) & 0xFFu, 0.25f);
        //                    //        color.b = (byte)Mathf.LerpUnclamped((int)color.b, biomeAt.m_uiColor & 0xFFu, 0.25f);
        //                    //    }
        //                    //}
        //                    //else if (cbxStaticMapType.Value == EStaticMapOverlay.Radiation)
        //                    //{
        //                    //    float radiationAt = biomeProvider.GetRadiationAt(j - num, i - num2);
        //                    //    if (radiationAt < 0.5f)
        //                    //    {
        //                    //        continue;
        //                    //    }
        //                    //    if (radiationAt < 1.5f)
        //                    //    {
        //                    //        color.r = (byte)Mathf.LerpUnclamped((int)color.r, 0f, 0.25f);
        //                    //        color.g = (byte)Mathf.LerpUnclamped((int)color.g, 255f, 0.25f);
        //                    //        color.b = (byte)Mathf.LerpUnclamped((int)color.b, 0f, 0.25f);
        //                    //    }
        //                    //    else if (radiationAt < 2.5f)
        //                    //    {
        //                    //        color.r = (byte)Mathf.LerpUnclamped((int)color.r, 0f, 0.25f);
        //                    //        color.g = (byte)Mathf.LerpUnclamped((int)color.g, 0f, 0.25f);
        //                    //        color.b = (byte)Mathf.LerpUnclamped((int)color.b, 255f, 0.25f);
        //                    //    }
        //                    //    else
        //                    //    {
        //                    //        color.r = (byte)Mathf.LerpUnclamped((int)color.r, 255f, 0.25f);
        //                    //        color.g = (byte)Mathf.LerpUnclamped((int)color.g, 0f, 0.25f);
        //                    //        color.b = (byte)Mathf.LerpUnclamped((int)color.b, 0f, 0.25f);
        //                    //    }
        //                    //}
        //                }
        //                staticWorldTexture[num3 + j] = color;
        //            }
        //        }
        //    }
        //}
    }

    public void InitMiniMap()
    {
        if (!(base.xui.playerUI.entityPlayer == null))
        {
            localPlayer = base.xui.playerUI.entityPlayer;
            bMapInitialized = true;
            xuiTexture.Texture = mapTexture;
            xuiTexture.Size = new Vector2i(712, 712);
        }
    }


    /* updateFullMap */
    public void UpdateFullMiniMap()
    {
        int mapStartX = (int)mapMiddlePosChunks.x - 1024;
        int mapEndX = (int)mapMiddlePosChunks.x + 1024;
        int mapStartZ = (int)mapMiddlePosChunks.y - 1024;
        int mapEndZ = (int)mapMiddlePosChunks.y + 1024;
        UpdateMiniMapSection(mapStartX, mapStartZ, mapEndX, mapEndZ, 0, 0, 2048, 2048);
        mapScrollTextureOffset.x = 0f;
        mapScrollTextureOffset.y = 0f;
        mapScrollTextureChunksOffsetX = 0;
        mapScrollTextureChunksOffsetZ = 0;
        positionMiniMap();
        mapTexture.Apply();
        SendMapPositionToServer();
    }


    /* updateMapForScroll */
    public void UpdateMiniMapForScroll(int deltaChunksX, int deltaChunksZ)
    {
        if (deltaChunksX != 0)
        {
            int num = Mathf.Abs(deltaChunksX);
            int num2 = mapScrollTextureChunksOffsetX * 16;
            int value = (mapScrollTextureChunksOffsetX + deltaChunksX) * 16;
            value = Utils.WrapInt(value, 0, 2048);
            int num3;
            int num4;
            if (deltaChunksX > 0)
            {
                if (num2 == 2048)
                {
                    num2 = 0;
                }
                num3 = (int)mapMiddlePosChunks.x + 1024;
                num4 = num3 - num * 16;
            }
            else
            {
                if (num2 == 0)
                {
                    num2 = 2048;
                }
                int num5 = num2;
                num2 = value;
                value = num5;
                num4 = (int)mapMiddlePosChunks.x - 1024;
                num3 = num4 + num * 16;
            }
            int index = (mapScrollTextureChunksOffsetZ + deltaChunksZ) * 16;
            index = Utils.WrapIndex(index, 2048);
            int drawnMapEndZ = Utils.WrapIndex(index - 1, 2048);
            int num6 = (int)mapMiddlePosChunks.y - 1024;
            int mapEndZ = num6 + 2048;
            UpdateMiniMapSection(num4, num6, num3, mapEndZ, num2, index, value, drawnMapEndZ);
        }
        if (deltaChunksZ != 0)
        {
            int num7 = Mathf.Abs(deltaChunksZ);
            int index = mapScrollTextureChunksOffsetZ * 16;
            int drawnMapEndZ = (mapScrollTextureChunksOffsetZ + deltaChunksZ) * 16;
            drawnMapEndZ = Utils.WrapInt(drawnMapEndZ, 0, 2048);
            int mapEndZ;
            int num6;
            if (deltaChunksZ > 0)
            {
                if (index == 2048)
                {
                    index = 0;
                }
                mapEndZ = (int)mapMiddlePosChunks.y + 1024;
                num6 = mapEndZ - num7 * 16;
            }
            else
            {
                if (index == 0)
                {
                    index = 2048;
                }
                int num8 = index;
                index = drawnMapEndZ;
                drawnMapEndZ = num8;
                num6 = (int)mapMiddlePosChunks.y - 1024;
                mapEndZ = num6 + num7 * 16;
            }
            int num2 = (mapScrollTextureChunksOffsetX + deltaChunksX) * 16;
            num2 = Utils.WrapIndex(num2, 2048);
            int value = Utils.WrapIndex(num2 - 1, 2048);
            int num4 = (int)mapMiddlePosChunks.x - 1024;
            int num3 = num4 + 2048;
            UpdateMiniMapSection(num4, num6, num3, mapEndZ, num2, index, value, drawnMapEndZ);
        }
        mapScrollTextureOffset.x += (float)(deltaChunksX * 16) / (float)mapTexture.width;
        mapScrollTextureOffset.y += (float)(deltaChunksZ * 16) / (float)mapTexture.width;
        mapScrollTextureOffset.x = Utils.WrapFloat(mapScrollTextureOffset.x, 0f, 1f);
        mapScrollTextureOffset.y = Utils.WrapFloat(mapScrollTextureOffset.y, 0f, 1f);
        mapScrollTextureChunksOffsetX += deltaChunksX;
        mapScrollTextureChunksOffsetZ += deltaChunksZ;
        mapScrollTextureChunksOffsetX = Utils.WrapIndex(mapScrollTextureChunksOffsetX, 128);
        mapScrollTextureChunksOffsetZ = Utils.WrapIndex(mapScrollTextureChunksOffsetZ, 128);
        positionMiniMap();
        mapTexture.Apply();
        SendMapPositionToServer();
    }


    /* updateMapSection */
    public void UpdateMiniMapSection(int mapStartX, int mapStartZ, int mapEndX, int mapEndZ, int drawnMapStartX, int drawnMapStartZ, int drawnMapEndX, int drawnMapEndZ)
    {
        Vector3 position = localPlayer.GetPosition();
        mapStartX = (int)position.x - 256;
        mapStartZ = (int)position.z - 256;
        mapEndX = (int)position.x + 256;
        mapEndZ = (int)position.z + 256;
        drawnMapStartX = 768;
        drawnMapStartZ = 768;

        IMapChunkDatabase mapDatabase = localPlayer.ChunkObserver.mapDatabase;
        bool flag = showStaticData && staticWorldTexture != null;
        NativeArray<Color32> rawTextureData = mapTexture.GetRawTextureData<Color32>();
        int num = mapStartZ;
        int num2 = drawnMapStartZ;
        while (num < mapEndZ)
        {
            int num3 = mapStartX;
            int num4 = drawnMapStartX;
            while (num3 < mapEndX)
            {
                int num5 = World.toChunkXZ(num3);
                int num6 = World.toChunkXZ(num);
                if (flag)
                {
                    int num7 = num5 << 4;
                    int num8 = num6 << 4;
                    for (int i = 0; i < 256; i++)
                    {
                        int num9 = i / 16;
                        int num10 = i % 16;
                        int num11 = (num2 + num9) * 2048;
                        int num12 = num4 + num10;
                        int index = num11 + num12;
                        int num13 = num7 + num10;
                        int num14 = num8 + num9;
                        if (num13 < staticMapLeft || num13 > staticMapRight || num14 < staticMapBottom || num14 > staticMapTop)
                        {
                            rawTextureData[index] = new Color32(0, 0, 0, 0);
                            continue;
                        }
                        int num15 = num13 - staticMapLeft + (num14 - staticMapBottom) * staticWorldWidth;
                        Color32 color = staticWorldTexture[num15];
                        if (color.a > 0)
                        {
                            rawTextureData[index] = new Color32(color.r, color.g, color.b, byte.MaxValue);
                        }
                        else
                        {
                            rawTextureData[index] = new Color32(0, 0, 0, 0);
                        }
                    }
                }
                else
                {
                    long chunkKey = WorldChunkCache.MakeChunkKey(num5, num6);
                    ushort[] mapColors = mapDatabase.GetMapColors(chunkKey);
                    if (mapColors == null)
                    {
                        for (int j = 0; j < 256; j++)
                        {
                            int num16 = (num2 + j / 16) * 2048;
                            int index2 = num4 + j % 16 + num16;
                            rawTextureData[index2] = new Color32(0, 0, 0, 0);
                        }
                    }
                    else
                    {

                        //byte[] array = fowChunkMaskAlphas[num17];
                        byte[] array = fowChunkMaskAlphas[4];
                        //if (!bFowMaskEnabled)
                        //{
                        //    array = fowChunkMaskAlphas[4];
                        //}
                        for (int k = 0; k < 256; k++)
                        {
                            int num18 = k / 16;
                            int num19 = k % 16;
                            int num20 = (num2 + num18) * 2048;
                            int num21 = num4 + num19;
                            int index3 = num20 + num21;
                            int num22 = num18 * 16;
                            byte b = array[num22 + num19];
                            Color32 color2 = Utils.FromColor5To32(mapColors[k]);
                            rawTextureData[index3] = new Color32(color2.r, color2.g, color2.b, (b < byte.MaxValue) ? b : byte.MaxValue);
                        }
                    }
                }
                num3 += 16;
                num4 = Utils.WrapIndex(num4 + 16, 2048);
            }
            num += 16;
            num2 = Utils.WrapIndex(num2 + 16, 2048);
        }
    }



    /* positionMap */
    public void positionMiniMap()
    {
        float num = (2048f - 256f * zoomScale) / 2f;
        mapScale = 256f * zoomScale / 2048f;
        float num2 = (num + (mapMiddlePosPixel.x - mapMiddlePosChunks.x)) / 2048f;
        float num3 = (num + (mapMiddlePosPixel.y - mapMiddlePosChunks.y)) / 2048f;
        mapPos = new Vector3(num2 + mapScrollTextureOffset.x, num3 + mapScrollTextureOffset.y, 0f);
        mapBGPos.x = (num + mapMiddlePosPixel.x) / 2048f;
        mapBGPos.y = (num + mapMiddlePosPixel.y) / 2048f;
    }


    /* 不适用代码 */
    //public void onMapDragged(XUiController _sender, EDragType _dragType, Vector2 _mousePositionDelta)
    //{
    //    if (UICamera.currentKey == KeyCode.Mouse0)
    //    {
    //        if (base.xui.playerUI.playerInput.GUIActions.LastDeviceClass != InputDeviceClass.Controller)
    //        {
    //            DragMap(_mousePositionDelta);
    //        }
    //        closeAllPopups();
    //    }
    //}


    public void DragMiniMap(Vector2 delta)
    {
        mapMiddlePosPixel -= delta;
        mapMiddlePosPixel = GameManager.Instance.World.ClampToValidWorldPosForMap(mapMiddlePosPixel);
        int num = 0;
        int num2 = 0;
        while (mapMiddlePosChunks.x - mapMiddlePosPixel.x >= 16f)
        {
            mapMiddlePosChunks.x -= 16f;
            num--;
        }
        while (mapMiddlePosChunks.x - mapMiddlePosPixel.x <= -16f)
        {
            mapMiddlePosChunks.x += 16f;
            num++;
        }
        while (mapMiddlePosChunks.y - mapMiddlePosPixel.y >= 16f)
        {
            mapMiddlePosChunks.y -= 16f;
            num2--;
        }
        while (mapMiddlePosChunks.y - mapMiddlePosPixel.y <= -16f)
        {
            mapMiddlePosChunks.y += 16f;
            num2++;
        }
        if (num != 0 || num2 != 0)
        {
            updateMapForScroll(num, num2);
        }
    }

    /* 不适用代码 */
    //public void onMapScrolled(XUiController _sender, float _delta)
    //{
    //    float num = 6f;
    //    if (InputUtils.ShiftKeyPressed)
    //    {
    //        num = 5f * zoomScale;
    //    }
    //    float min = 0.7f;
    //    float max = 6.15f;
    //    targetZoomScale = Utils.FastClamp(zoomScale - _delta * num, min, max);
    //    if (_delta < 0f)
    //    {
    //        Manager.PlayInsidePlayerHead("map_zoom_in");
    //    }
    //    else
    //    {
    //        Manager.PlayInsidePlayerHead("map_zoom_out");
    //    }
    //    closeAllPopups();
    //}



    /* updateMapObjects */
    public virtual void updateMiniMapObjects()
    {
        World world = GameManager.Instance.World;
        navObjectsOnMapAlive.Clear();
        mapObjectsOnMapAlive.Clear();
        updateNavObjectList();
        foreach (KeyValuePair<int, NavObject> item in keyToNavObject.Dict)
        {
            if (!navObjectsOnMapAlive.Contains(item.Key))
            {
                keyToNavObject.MarkToRemove(item.Key);
                keyToNavSprite.MarkToRemove(item.Key);
            }
        }
        foreach (KeyValuePair<long, MapObject> item2 in keyToMapObject.Dict)
        {
            if (!mapObjectsOnMapAlive.Contains(item2.Key))
            {
                keyToMapObject.MarkToRemove(item2.Key);
                keyToMapSprite.MarkToRemove(item2.Key);
            }
        }
        keyToNavObject.RemoveAllMarked((int _key) =>
        {
            keyToNavObject.Remove(_key);
        });
        keyToNavSprite.RemoveAllMarked((int _key) =>
        {
            UnityEngine.Object.Destroy(keyToNavSprite[_key]);
            keyToNavSprite.Remove(_key);
        });
        keyToMapObject.RemoveAllMarked((long _key) =>
        {
            keyToMapObject.Remove(_key);
        });
        keyToMapSprite.RemoveAllMarked((long _key) =>
        {
            UnityEngine.Object.Destroy(keyToMapSprite[_key]);
            keyToMapSprite.Remove(_key);
        });
        localPlayer.selectedSpawnPointKey = -1L;
    }

    /* 不适应代码 */
    //public void UpdateWaypointSelection()
    //{
    //    Vector2 screenPosition = base.xui.playerUI.CursorController.GetScreenPosition();
    //    GameObject gameObject = null;
    //    float num = float.MaxValue;
    //    closestMouseOverNavObject = null;
    //    foreach (NavObject navObject in NavObjectManager.Instance.NavObjectList)
    //    {
    //        if (navObject == null || navObject.NavObjectClass == null || (navObject.TrackedEntity != null && navObject.TrackedEntity.entityId == GameManager.Instance.World.GetPrimaryPlayerId()))
    //        {
    //            continue;
    //        }
    //        GameObject gameObject2 = keyToNavSprite[navObject.Key];
    //        if (gameObject2 != null)
    //        {
    //            Vector3 b = base.xui.playerUI.uiCamera.cachedCamera.WorldToScreenPoint(gameObject2.transform.position);
    //            float num2 = Vector3.Distance(screenPosition, b);
    //            if (num2 <= 20f && (closestMouseOverNavObject == null || (closestMouseOverNavObject != null && num2 < num)))
    //            {
    //                closestMouseOverNavObject = navObject;
    //                num = num2;
    //                gameObject = gameObject2;
    //            }
    //        }
    //    }
    //    if (closestMouseOverNavObject != null)
    //    {
    //        if (selectMapSprite != gameObject)
    //        {
    //            if (selectMapSprite != null)
    //            {
    //                selectMapSprite.transform.localScale = Vector3.one;
    //            }
    //            selectMapSprite = gameObject;
    //            selectMapSprite.transform.localScale = Vector3.one * 1.5f;
    //        }
    //    }
    //    else if (selectMapSprite != null)
    //    {
    //        selectMapSprite.transform.localScale = Vector3.one;
    //        selectMapSprite = null;
    //    }
    //}



    /* onMapPressed调用，不适用代码 */
    //public void teleportPlayerOnMap(Vector3 _screenPosition)
    //{
    //    Vector3 vector = screenPosToWorldPos(_screenPosition);
    //    localPlayer.Teleport(new Vector3(vector.x, 180f, vector.z));
    //}

    /* 不适用代码 */
    //public void onMapPressedLeft(XUiController _sender, int _mouseButton)
    //{
    //    closeAllPopups();
    //    if (closestMouseOverNavObject != null)
    //    {
    //        closestMouseOverNavObject.hiddenOnCompass = !closestMouseOverNavObject.hiddenOnCompass;
    //        if (closestMouseOverNavObject.hiddenOnCompass)
    //        {
    //            GameManager.ShowTooltip(GameManager.Instance.World.GetPrimaryPlayer(), Localization.Get("compassWaypointHiddenTooltip"));
    //        }
    //        if (closestMouseOverNavObject.NavObjectClass.NavObjectClassName == "quick_waypoint")
    //        {
    //            base.xui.playerUI.entityPlayer.navMarkerHidden = closestMouseOverNavObject.hiddenOnCompass;
    //            return;
    //        }
    //        Waypoint waypointForNavObject = base.xui.playerUI.entityPlayer.Waypoints.GetWaypointForNavObject(closestMouseOverNavObject);
    //        if (waypointForNavObject != null)
    //        {
    //            waypointForNavObject.hiddenOnCompass = closestMouseOverNavObject.hiddenOnCompass;
    //        }
    //    }
    //    else if (PlatformManager.NativePlatform.Input.CurrentInputStyle != PlayerInputManager.InputStyle.Keyboard)
    //    {
    //        OpenWaypointPopup();
    //    }
    //}


    /* 不适用代码 */
    //public void onMapPressed(XUiController _sender, int _mouseButton)
    //{
    //    if (PlatformManager.NativePlatform.Input.CurrentInputStyle != PlayerInputManager.InputStyle.Keyboard)
    //    {
    //        return;
    //    }
    //    closeAllPopups();
    //    _ = base.xui.playerUI.uiCamera;
    //    if (InputUtils.ControlKeyPressed)
    //    {
    //        if (GameStats.GetBool(EnumGameStats.IsTeleportEnabled) || GamePrefs.GetBool(EnumGamePrefs.DebugMenuEnabled))
    //        {
    //            this.TeleportPlayerOnMiniMap(base.xui.playerUI.CursorController.GetScreenPosition());
    //        }
    //    }
    //    else
    //    {
    //        OpenWaypointPopup();
    //    }
    //}


    /* 不适用代码 */
    //public void onMapHover(XUiController _sender, bool _isOver)
    //{
    //    bMouseOverMap = _isOver;
    //}

    /* 不适用代码 */
    //public void onPlayerIconPressed(XUiController _sender, int _mouseButton)
    //{
    //    PositionMapAt(localPlayer.GetPosition());
    //    closeAllPopups();
    //}

    /* 不适用代码 */
    //public void onBedrollIconPressed(XUiController _sender, int _mouseButton)
    //{
    //    if (localPlayer.SpawnPoints.Count != 0)
    //    {
    //        PositionMapAt(localPlayer.SpawnPoints[0].ToVector3());
    //        closeAllPopups();
    //    }
    //}

    /* 无调用代码 */
    //public void OnSetWaypoint()
    //{
    //    localPlayer.navMarkerHidden = false;
    //    localPlayer.markerPosition = World.worldToBlockPos(screenPosToWorldPos(nextMarkerMousePosition, needY: true));
    //    closeAllPopups();
    //}

    //public void OnWaypointEntryChosen(string _iconName)
    //{
    //    currentWaypointIconChosen = _iconName;
    //}

    /* 无调用代码 */
    //public void OnWaypointCreated(string _name)
    //{
    //    Waypoint w = new Waypoint();
    //    w.pos = World.worldToBlockPos(screenPosToWorldPos(nextMarkerMousePosition, needY: true));
    //    w.icon = currentWaypointIconChosen;
    //    w.name.Update(_name, PlatformManager.MultiPlatform.User.PlatformUserId);
    //    base.xui.playerUI.entityPlayer.Waypoints.Collection.Add(w);
    //    closeAllPopups();
    //    ((XUiC_MapWaypointList)base.Parent.GetChildById("waypointList")).UpdateWaypointsList();
    //    w.navObject = NavObjectManager.Instance.RegisterNavObject("waypoint", w.pos.ToVector3(), w.icon);
    //    w.navObject.IsActive = false;
    //    GeneratedTextManager.GetDisplayText(w.name, (string _filtered) =>
    //    {
    //        w.navObject.name = _filtered;
    //    }, _runCallbackIfReadyNow: true, _checkBlockState: false, GeneratedTextManager.TextFilteringMode.FilterWithSafeString);
    //    w.navObject.usingLocalizationId = w.bUsingLocalizationId;
    //    selectWaypoint(w);
    //    Manager.PlayInsidePlayerHead("ui_waypoint_add");
    //}

    /* 不适用代码 */
    //public void CbxStaticMapType_OnValueChanged(XUiController _sender, EStaticMapOverlay _oldvalue, EStaticMapOverlay _newvalue)
    //{
    //    staticWorldTexture = null;
    //}

    //public void CreateVehicleLastKnownWaypoint(EntityVehicle _vehicle)
    //{
    //    Waypoint waypoint = new Waypoint();
    //    waypoint.pos = World.worldToBlockPos(_vehicle.position);
    //    waypoint.icon = _vehicle.GetMapIcon();
    //    waypoint.ownerId = _vehicle.GetVehicle().OwnerId;
    //    waypoint.name.Update(Localization.Get(_vehicle.EntityName), PlatformManager.MultiPlatform.User.PlatformUserId);
    //    waypoint.entityId = _vehicle.entityId;
    //    EntityPlayer entityPlayer = base.xui.playerUI.entityPlayer;
    //    if (!entityPlayer.Waypoints.ContainsWaypoint(waypoint))
    //    {
    //        entityPlayer.Waypoints.Collection.Add(waypoint);
    //        if (waypoint.CanBeViewedBy(PlatformManager.InternalLocalUserIdentifier))
    //        {
    //            ((XUiC_MapWaypointList)base.Parent.GetChildById("waypointList")).UpdateWaypointsList();
    //            waypoint.navObject = NavObjectManager.Instance.RegisterNavObject("waypoint", waypoint.pos.ToVector3(), waypoint.icon);
    //            waypoint.navObject.IsActive = false;
    //            waypoint.navObject.OverrideSpriteName = _vehicle.GetMapIcon();
    //            waypoint.navObject.name = waypoint.name.Text;
    //            waypoint.navObject.usingLocalizationId = waypoint.bUsingLocalizationId;
    //        }
    //    }
    //}


    /* 不适用代码 */
    //public void onWaypointIconPressed(XUiController _sender, int _mouseButton)
    //{
    //    _ = base.xui.playerUI.entityPlayer;
    //    if (localPlayer.markerPosition == Vector3i.zero)
    //    {
    //        Manager.PlayInsidePlayerHead("ui_denied");
    //    }
    //    else
    //    {
    //        Manager.PlayInsidePlayerHead("ui_waypoint_delete");
    //    }
    //    localPlayer.markerPosition = Vector3i.zero;
    //}


    /* PositionMapAt */
    public void PositionMiniMapAt(Vector3 _worldPos)
    {
        int num = (int)_worldPos.x;
        int num2 = (int)_worldPos.z;
        mapMiddlePosChunks = new Vector2(World.toChunkXZ(num - 1024) * 16 + 1024, World.toChunkXZ(num2 - 1024) * 16 + 1024);
        mapMiddlePosPixel = mapMiddlePosChunks;
        mapMiddlePosPixel = GameManager.Instance.World.ClampToValidWorldPosForMap(mapMiddlePosPixel);
        UpdateFullMiniMap();
    }

    public override void Cleanup()
    {
        Patch_XUiController.Point_Cleanup.Reverse(this);
        UnityEngine.Object.Destroy(mapTexture);
    }
}
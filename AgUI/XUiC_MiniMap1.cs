using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;

public class XUiC_MiniMap1 : XUiController
{
    public GameObject prefabMapSprite;

    public EntityPlayer localPlayer;

    public XUiV_Panel mapView;
    public XUiV_Texture mapViewTexture;
    public XUiV_Panel mapViewClip;
    public XUiV_Sprite mapViewCross;

    public Texture2D mapTexture;

    public Vector3 playerLastPos;

    public Vector2 mapPos;
    public Vector2 mapBGPos;

    public Vector2 mapMiddlePosPixel;
    public Vector2 mapMiddlePosChunks;
    public Vector2 mapMiddlePosChunksToServer;

    public Vector2 mapScrollTextureOffset;
    public int mapScrollTextureChunksOffsetX;
    public int mapScrollTextureChunksOffsetZ;

    public Transform mapViewClipTransform;

    public DictionarySave<long, MapObject> keyToMapObject = new DictionarySave<long, MapObject>();
    public DictionarySave<int, NavObject> keyToNavObject = new DictionarySave<int, NavObject>();
    public DictionarySave<int, GameObject> keyToNavSprite = new DictionarySave<int, GameObject>();
    public DictionarySave<long, GameObject> keyToMapSprite = new DictionarySave<long, GameObject>();

    public HashSetLong navObjectsOnMapAlive = new HashSetLong();
    public HashSetLong mapObjectsOnMapAlive = new HashSetLong();

    public bool isOpen;
    public bool bMapInitialized;
    public bool bShouldRedrawMap;
    public float timeToRedrawMap;

    public int mapScreenSize = 256;

    public int mapSize = 2048;
    public int mapSizeZoom = 256;
    public float mapZoomScale = 1f;
    // mapScale = mapSizeZoom * mapZoomScale / mapSize
    public float mapScale = 0.125f;

    // factor = mapScreenSize / mapSizeZoom
    public float factorWorldToMapPos = 1f;

    public Vector2i spriteMiddlePos = new Vector2i(128, 128);
    public int spriteSizeZoom = 30;
    // spriteScaleZoom = 1f / (mapZoomScale * 2f)
    public float spriteScaleZoom = 0.5f;

    public override void Init()
    {
        base.Init();

        base.xui.LoadData("Prefabs/MapSpriteEntity", (GameObject o) =>
        {
            prefabMapSprite = o;
        });

        mapView = base.GetChildById("mapView").viewComponent as XUiV_Panel;
        mapViewTexture = base.GetChildById("mapViewTexture").viewComponent as XUiV_Texture;
        mapViewClip = base.GetChildById("mapViewClip").ViewComponent as XUiV_Panel;
        mapViewCross = base.GetChildById("mapViewCross").viewComponent as XUiV_Sprite;

        mapViewClipTransform = mapViewClip.UiTransform;
        if (mapTexture == null)
        {
            mapTexture = new Texture2D(mapSize, mapSize, TextureFormat.RGBA32, mipChain: false);
            mapTexture.name = "XUiC_MiniMap.mapTexture";
        }

        bShouldRedrawMap = true;
        mapView.IsSnappable = false;
        mapViewTexture.IsSnappable = false;

        InitMap();

        NavObjectManager.Instance.OnNavObjectRemoved += Instance_OnNavObjectRemoved;

    }

    /* DONE */
    public void InitMap()
    {
        if (base.xui.playerUI.entityPlayer != null)
        {
            localPlayer = base.xui.playerUI.entityPlayer;
            bMapInitialized = true;
            mapViewTexture.Texture = mapTexture;
            mapViewTexture.Size = new Vector2i(mapScreenSize, mapScreenSize);
        }
    }

    /* DONE */
    public void Instance_OnNavObjectRemoved(NavObject newNavObject)
    {
        UnityEngine.Object.Destroy(keyToNavSprite[newNavObject.Key]);
        keyToNavObject.Remove(newNavObject.Key);
        keyToNavSprite.Remove(newNavObject.Key);
    }

    public override void OnOpen()
    {
        base.OnOpen();
        if (isOpen)
        {
            return;
        }
        if (GameManager.Instance.World.GetWorldExtent(out Vector3i _minSize, out Vector3i _maxSize))
        {
            Log.Out("OnOpen==========地图尺寸：_minSize={0},_maxSize={1}", _minSize, _maxSize);
        }
        isOpen = true;
        localPlayer = base.xui.playerUI.entityPlayer;
        if (!bMapInitialized)
        {
            InitMap();
        }
        playerLastPos = localPlayer.GetPosition();
        UpdateMapAtPos(playerLastPos);
        UpdateMapPos();
        base.xui.playerUI.GetComponentInParent<LocalPlayerCamera>().PreRender += OnPreRender;
    }

    /* DONE */
    public void OnPreRender(LocalPlayerCamera _localPlayerCamera)
    {
        Shader.SetGlobalVector("_MainMapPosAndScale", new Vector4(mapPos.x, mapPos.y, mapScale, mapScale));
        Shader.SetGlobalVector("_MainMapBGPosAndScale", new Vector4(mapBGPos.x, mapBGPos.y, mapScale, mapScale));
    }

    public override void Update(float _dt)
    {
        base.Update(_dt);
        if (!windowGroup.isShowing || !XUi.IsGameRunning() || base.xui.playerUI.entityPlayer == null)
        {
            return;
        }
        if (!bMapInitialized)
        {
            InitMap();
        }
        if (bShouldRedrawMap)
        {
            playerLastPos = localPlayer.GetPosition();
            UpdateMapAtPos(playerLastPos);
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
        if (!base.xui.playerUI.entityPlayer.IsMoveStateStill())
        {
            Vector3 playerCurrentPos = localPlayer.GetPosition();
            if (!playerCurrentPos.Equals(playerLastPos))
            {

                UpdateMapOnMove(playerCurrentPos);
                playerLastPos = playerCurrentPos;
            }
        }
        UpdateMapPos();
        UpdateMapCross();
        UpdateMapObjects();
    }

    public void UpdateMapAtPos(Vector3 worldPos)
    {
        int worldPosX = (int)worldPos.x;
        int worldPosZ = (int)worldPos.z;

        int mapMiddlePosChunkX = World.toChunkXZ(worldPosX);
        int mapMiddlePosChunkZ = World.toChunkXZ(worldPosZ);
        mapMiddlePosChunks = new Vector2(mapMiddlePosChunkX * 16, mapMiddlePosChunkZ * 16);
        mapMiddlePosPixel = GameManager.Instance.World.ClampToValidWorldPosForMap(mapMiddlePosChunks);

        int startX = (int)mapMiddlePosChunks.x - 1024;
        int endX = (int)mapMiddlePosChunks.x + 1024;
        int startZ = (int)mapMiddlePosChunks.y - 1024;
        int endZ = (int)mapMiddlePosChunks.y + 1024;

        UpdateMapSection(startX, startZ, endX, endZ, 0, 0, 2048, 2048);

        mapScrollTextureOffset.x = 0f;
        mapScrollTextureOffset.y = 0f;
        mapScrollTextureChunksOffsetX = 0;
        mapScrollTextureChunksOffsetZ = 0;

        mapTexture.Apply();
        bShouldRedrawMap = false;
        SendMapPositionToServer();
    }

    public void UpdateMapOnMove(Vector3 worldPos)
    {
        Vector2 delta = new Vector2(playerLastPos.x - worldPos.x, playerLastPos.z - worldPos.z);

        mapMiddlePosPixel -= delta;
        mapMiddlePosPixel = GameManager.Instance.World.ClampToValidWorldPosForMap(mapMiddlePosPixel);

        int deltaChunksX = 0;
        int deltaChunksZ = 0;
        while (mapMiddlePosChunks.x - mapMiddlePosPixel.x >= 16f)
        {
            mapMiddlePosChunks.x -= 16f;
            deltaChunksX--;
        }
        while (mapMiddlePosChunks.x - mapMiddlePosPixel.x <= -16f)
        {
            mapMiddlePosChunks.x += 16f;
            deltaChunksX++;
        }
        while (mapMiddlePosChunks.y - mapMiddlePosPixel.y >= 16f)
        {
            mapMiddlePosChunks.y -= 16f;
            deltaChunksZ--;
        }
        while (mapMiddlePosChunks.y - mapMiddlePosPixel.y <= -16f)
        {
            mapMiddlePosChunks.y += 16f;
            deltaChunksZ++;
        }

        if (deltaChunksX != 0 || deltaChunksZ != 0)
        {
            Log.Out("=============================================");
            Log.Out("======先前数据：worldPos={0}，mapMiddlePosPixel={1}，mapMiddlePosChunks={2}", worldPos, mapMiddlePosPixel, mapMiddlePosChunks);
            Log.Out("======累计滚动：mapScrollTextureOffset={0}，mapScrollTextureChunksOffsetX={1},mapScrollTextureChunksOffsetZ={2}", mapScrollTextureOffset, mapScrollTextureChunksOffsetX, mapScrollTextureChunksOffsetZ);
            Log.Out("======滚动更新：deltaChunksX={0}，deltaChunksZ={1}", deltaChunksX, deltaChunksZ);
            UpdateMapByScroll(worldPos, deltaChunksX, deltaChunksZ);
            Log.Out("=============================================");
        }
    }

    public void UpdateMapByScroll(Vector3 worldPos, int deltaChunksX, int deltaChunksZ)
    {
        int worldPosX = (int)worldPos.x;
        int worldPosZ = (int)worldPos.z;

        if (deltaChunksX != 0)
        {
            int deltaChunksXAbs = Mathf.Abs(deltaChunksX);
            int drawnStartX = mapScrollTextureChunksOffsetX * 16;
            int drawnEndX = (mapScrollTextureChunksOffsetX + deltaChunksX) * 16;
            drawnEndX = Utils.WrapInt(drawnEndX, 0, 2048);
            int endX;
            int startX;
            if (deltaChunksX > 0)
            {
                if (drawnStartX == 2048)
                {
                    drawnStartX = 0;
                }
                endX = (int)mapMiddlePosChunks.x + 1024;
                startX = endX - deltaChunksXAbs * 16;
            }
            else
            {
                if (drawnStartX == 0)
                {
                    drawnStartX = 2048;
                }
                int num5 = drawnStartX;
                drawnStartX = drawnEndX;
                drawnEndX = num5;
                startX = (int)mapMiddlePosChunks.x - 1024;
                endX = startX + deltaChunksXAbs * 16;
            }
            int drawnStartZ = (mapScrollTextureChunksOffsetZ + deltaChunksZ) * 16;
            drawnStartZ = Utils.WrapIndex(drawnStartZ, 2048);
            int drawnEndZ = Utils.WrapIndex(drawnStartZ - 1, 2048);
            int startZ = (int)mapMiddlePosChunks.y - 1024;
            int endZ = startZ + 2048;
            Log.Out("======X轴滚动更新：startX={0}，endX={1}，startZ={2}，endZ={3}", startX, endX, startZ, endZ);
            Log.Out("======X轴滚动绘制：drawnStartX={0}，drawnEndX={1}，drawnStartZ={2}，drawnEndZ={3}", drawnStartX, drawnEndX, drawnStartZ, drawnEndZ);
            UpdateMapSection(startX, startZ, endX, endZ, drawnStartX, drawnStartZ, drawnEndX, drawnEndZ);
        }
        if (deltaChunksZ != 0)
        {
            int deltaChunksZAbs = Mathf.Abs(deltaChunksZ);
            int drawnStartZ = mapScrollTextureChunksOffsetZ * 16;
            int drawnEndZ = (mapScrollTextureChunksOffsetZ + deltaChunksZ) * 16;
            drawnEndZ = Utils.WrapInt(drawnEndZ, 0, 2048);
            int endZ;
            int startZ;
            if (deltaChunksZ > 0)
            {
                if (drawnStartZ == 2048)
                {
                    drawnStartZ = 0;
                }
                endZ = (int)mapMiddlePosChunks.y + 1024;
                startZ = endZ - deltaChunksZAbs * 16;
            }
            else
            {
                if (drawnStartZ == 0)
                {
                    drawnStartZ = 2048;
                }
                int num8 = drawnStartZ;
                drawnStartZ = drawnEndZ;
                drawnEndZ = num8;
                startZ = (int)mapMiddlePosChunks.y - 1024;
                endZ = startZ + deltaChunksZAbs * 16;
            }
            int drawnStartX = (mapScrollTextureChunksOffsetX + deltaChunksX) * 16;
            drawnStartX = Utils.WrapIndex(drawnStartX, 2048);
            int drawnEndX = Utils.WrapIndex(drawnStartX - 1, 2048);
            int startX = (int)mapMiddlePosChunks.x - 1024;
            int endX = startX + 2048;
            Log.Out("======Z轴滚动更新：startX={0}，endX={1}，startZ={2}，endZ={3}", startX, endX, startZ, endZ);
            Log.Out("======Z轴滚动绘制：drawnStartX={0}，drawnEndX={1}，drawnStartZ={2}，drawnEndZ={3}", drawnStartX, drawnEndX, drawnStartZ, drawnEndZ);
            UpdateMapSection(startX, startZ, endX, endZ, drawnStartX, drawnStartZ, drawnEndX, drawnEndZ);
        }

        mapScrollTextureOffset.x += (float)(deltaChunksX * 16) / (float)mapTexture.width;
        mapScrollTextureOffset.y += (float)(deltaChunksZ * 16) / (float)mapTexture.width;
        mapScrollTextureOffset.x = Utils.WrapFloat(mapScrollTextureOffset.x, 0f, 1f);
        mapScrollTextureOffset.y = Utils.WrapFloat(mapScrollTextureOffset.y, 0f, 1f);
        mapScrollTextureChunksOffsetX += deltaChunksX;
        mapScrollTextureChunksOffsetZ += deltaChunksZ;
        mapScrollTextureChunksOffsetX = Utils.WrapIndex(mapScrollTextureChunksOffsetX, 128);
        mapScrollTextureChunksOffsetZ = Utils.WrapIndex(mapScrollTextureChunksOffsetZ, 128);

        mapTexture.Apply();
        bShouldRedrawMap = false;
        SendMapPositionToServer();
    }

    public void UpdateMapSection(int startX, int startZ, int endX, int endZ, int drawnStartX, int drawnStartZ, int drawnEndX, int drawnEndZ)
    {
        IMapChunkDatabase mapDatabase = localPlayer.ChunkObserver.mapDatabase;
        NativeArray<Color32> rawTextureData = mapTexture.GetRawTextureData<Color32>();
        int currentX = startX;
        int drwanCurrentX = drawnStartX;
        while (currentX < endX)
        {
            int currentZ = startZ;
            int drwanCurrentZ = drawnStartZ;
            while (currentZ < endZ)
            {
                int currentChunkX = World.toChunkXZ(currentX);
                int currentChunkZ = World.toChunkXZ(currentZ);
                long currentChunkKey = WorldChunkCache.MakeChunkKey(currentChunkX, currentChunkZ);
                ushort[] currentChunkColors = mapDatabase.GetMapColors(currentChunkKey);
                if (currentChunkColors == null)
                {
                    for (int j = 0; j < 256; j++)
                    {
                        int rawCurrentX = drwanCurrentX + j % 16;
                        int rawCurrentZ = (drwanCurrentZ + j / 16) * mapSize;
                        int rawCurrentIndex = rawCurrentX + rawCurrentZ;
                        rawTextureData[rawCurrentIndex] = new Color32(0, 0, 0, 0);
                    }
                }
                else
                {
                    for (int k = 0; k < 256; k++)
                    {
                        int rawCurrentX = drwanCurrentX + k % 16;
                        int rawCurrentZ = (drwanCurrentZ + k / 16) * mapSize;
                        int rawCurrentIndex = rawCurrentZ + rawCurrentX;
                        Color32 rawCurrentColor = Utils.FromColor5To32(currentChunkColors[k]);
                        rawTextureData[rawCurrentIndex] = new Color32(rawCurrentColor.r, rawCurrentColor.g, rawCurrentColor.b, byte.MaxValue);
                    }
                }
                currentZ += 16;
                drwanCurrentZ = Utils.WrapIndex(drwanCurrentZ + 16, 2048);
            }
            currentX += 16;
            drwanCurrentX = Utils.WrapIndex(drwanCurrentX + 16, 2048);
        }
    }

    /* DONE */
    public void UpdateMapPos()
    {
        float num = (mapSize - mapSizeZoom * mapZoomScale) / 2f;
        float num2 = (num + (mapMiddlePosPixel.x - mapMiddlePosChunks.x)) / mapSize;
        float num3 = (num + (mapMiddlePosPixel.y - mapMiddlePosChunks.y)) / mapSize;
        mapPos = new Vector2(num2 + mapScrollTextureOffset.x, num3 + mapScrollTextureOffset.y);
        mapBGPos = new Vector2((num + mapMiddlePosPixel.x) / mapSize, (num + mapMiddlePosPixel.y) / mapSize);
    }

    /* DONE */
    public void UpdateMapObjects()
    {
        World world = GameManager.Instance.World;
        navObjectsOnMapAlive.Clear();
        mapObjectsOnMapAlive.Clear();
        UpdateNavObjectList();
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

    /* DONE */
    public void UpdateNavObjectList()
    {
        List<NavObject> navObjectList = NavObjectManager.Instance.NavObjectList;
        navObjectsOnMapAlive.Clear();
        for (int i = 0; i < navObjectList.Count; i++)
        {
            NavObject navObject = navObjectList[i];
            int key = navObject.Key;
            if (navObject.HasRequirements && navObject.NavObjectClass.IsOnMap(navObject.IsActive))
            {
                NavObjectMapSettings currentMapSettings = navObject.CurrentMapSettings;
                GameObject gameObject = null;
                UISprite uISprite = null;
                if (!keyToNavObject.ContainsKey(key))
                {
                    gameObject = mapViewClipTransform.gameObject.AddChild(prefabMapSprite);
                    uISprite = gameObject.transform.Find("Sprite").GetComponent<UISprite>();
                    string spriteName = navObject.GetSpriteName(currentMapSettings);
                    uISprite.atlas = base.xui.GetAtlasByName(((UnityEngine.Object)uISprite.atlas).name, spriteName);
                    uISprite.spriteName = spriteName;
                    uISprite.depth = currentMapSettings.Layer;
                    keyToNavObject[key] = navObject;
                    keyToNavSprite[key] = gameObject;
                }
                else
                {
                    gameObject = keyToNavSprite[key];
                }

                //if (navObject.trackedEntity is EntityPlayer entityPlayer)
                //{
                //    if (!string.Equals(localPlayer.PlayerDisplayName, entityPlayer.PlayerDisplayName))
                //    {
                //        UILabel component = gameObject.transform.Find("Name").GetComponent<UILabel>();
                //        component.text = entityPlayer.PlayerDisplayName;
                //        component.font = base.xui.GetUIFontByName("ReferenceFont");
                //        component.fontSize = 15;
                //        component.gameObject.SetActive(value: true);
                //        component.color = (navObject.UseOverrideColor ? navObject.OverrideColor : currentMapSettings.Color);
                //    }
                //}

                uISprite = gameObject.transform.Find("Sprite").GetComponent<UISprite>();
                Vector3 vector = currentMapSettings.IconScaleVector * spriteScaleZoom;
                uISprite.width = (int)((float)spriteSizeZoom * vector.x);
                uISprite.height = (int)((float)spriteSizeZoom * vector.y);
                uISprite.color = (navObject.hiddenOnCompass ? Color.grey : (navObject.UseOverrideColor ? navObject.OverrideColor : currentMapSettings.Color));
                uISprite.gameObject.transform.localEulerAngles = new Vector3(0f, 0f, 0f - navObject.Rotation.y);
                gameObject.transform.localPosition = WorldPosToMapPos(navObject.GetPosition() + Origin.position);
                if (currentMapSettings.AdjustCenter)
                {
                    gameObject.transform.localPosition += new Vector3(uISprite.width / 2, uISprite.height / 2, 0f);
                }

                navObjectsOnMapAlive.Add(key);
            }
        }
    }

    /* DONE */
    public Vector3 WorldPosToMapPos(Vector3 _worldPos)
    {
        return new Vector3((_worldPos.x - mapMiddlePosPixel.x) * factorWorldToMapPos / mapZoomScale + (float)spriteMiddlePos.x, (_worldPos.z - mapMiddlePosPixel.y) * factorWorldToMapPos / mapZoomScale - (float)spriteMiddlePos.y, 0f);
    }

    public void UpdateMapCross()
    {
        mapViewCross.sprite.gameObject.transform.localEulerAngles = new Vector3(0f, 0f, 0f - localPlayer.rotation.y);
    }

    /* DONE */
    public void SendMapPositionToServer()
    {
        if (GameManager.Instance.World.IsRemote() && !mapMiddlePosChunksToServer.Equals(mapMiddlePosChunks))
        {
            mapMiddlePosChunksToServer = mapMiddlePosChunks;
            SingletonMonoBehaviour<ConnectionManager>.Instance.SendToServer(NetPackageManager.GetPackage<NetPackageMapPosition>().Setup(localPlayer.entityId, new Vector2i(Utils.Fastfloor(mapMiddlePosChunks.x), Utils.Fastfloor(mapMiddlePosChunks.y))));
        }
    }

    /* DONE */
    public override void OnClose()
    {
        base.OnClose();
        if (isOpen)
        {
            isOpen = false;
            bShouldRedrawMap = false;
            base.xui.playerUI.GetComponentInParent<LocalPlayerCamera>().PreRender -= OnPreRender;
        }
    }

    /* DONE */
    public override void Cleanup()
    {
        base.Cleanup();
        UnityEngine.Object.Destroy(mapTexture);
    }

}

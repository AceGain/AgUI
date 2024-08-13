using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;

public class XUiC_MiniMap : XUiController
{
    public GameObject prefabMapSprite;

    public EntityPlayer localPlayer;

    public XUiV_Panel mapView;
    public XUiV_Texture mapViewTexture;
    public XUiV_Panel mapViewClip;
    public XUiV_Sprite mapViewCross;

    public Texture2D mapTexture;

    public Vector2 mapPos;
    public Vector2 mapBGPos;

    public Vector2 mapMiddlePosPixel;
    public Vector2 mapMiddlePosChunks;
    public Vector2 mapMiddlePosChunksToServer;

    public Vector3 worldPosLast;

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

    public Vector3i worldMinSize;
    public Vector3i worldMaxSize;

    public int mapScreenSize = 296;

    public int mapVisualRange = 144;
    public int mapVisualRangePositive = 80;
    public int mapVisualRangeNegative = 64;

    public int mapSize = 2048;
    public int mapSizeZoom = 272;
    public float mapZoomScale = 1f;
    // mapScale = mapSizeZoom * mapZoomScale / mapSize
    public float mapScale = 0.1328125f;

    // factor = mapScreenSize / mapSizeZoom
    public float factorWorldToMapPos = 1.0882353f;

    public Vector2i spriteMiddlePos = new Vector2i(148, 148);
    public int spriteSizeZoom = 10;
    // spriteScaleZoom = 1f / (mapZoomScale * 2f)
    public float spriteScaleZoom = 1f;

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
            //Log.Out("OnOpen==========地图尺寸：_minSize={0},_maxSize={1}", _minSize, _maxSize);
            worldMinSize = _minSize;
            worldMaxSize = _maxSize;
        }
        isOpen = true;
        localPlayer = base.xui.playerUI.entityPlayer;
        if (!bMapInitialized)
        {
            InitMap();
        }
        worldPosLast = localPlayer.GetPosition();
        UpdateMapAtPos(worldPosLast);
        bShouldRedrawMap = false;
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
            worldPosLast = localPlayer.GetPosition();
            UpdateMapAtPos(worldPosLast);
            bShouldRedrawMap = false;
            UpdateMapPos();
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
            if (!playerCurrentPos.Equals(worldPosLast))
            {

                UpdateMapOnMove(playerCurrentPos);
                bShouldRedrawMap = false;
                worldPosLast = playerCurrentPos;
                UpdateMapPos();
            }
        }
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

        //TODO 修改中心位置
        int startX = (int)mapMiddlePosChunks.x - mapSize / 2;
        int endX = (int)mapMiddlePosChunks.x + mapSize / 2;
        int startZ = (int)mapMiddlePosChunks.y - mapSize / 2;
        int endZ = (int)mapMiddlePosChunks.y + mapSize / 2;

        //Log.Out("======定点更新：startX={0}，endX={1}，startZ={2}，endZ={3}", startX, endX, startZ, endZ);
        UpdateMapSection(startX, startZ, endX, endZ, 0, 0, mapSize, mapSize);

        mapScrollTextureOffset = Vector2.zero;
        mapScrollTextureChunksOffsetX = 0;
        mapScrollTextureChunksOffsetZ = 0;

        mapTexture.Apply();
        SendMapPositionToServer();
    }

    public void UpdateMapOnMove(Vector3 worldPos)
    {
        if (worldPos.x < worldMinSize.x || worldPos.x > worldMaxSize.x)
        {
            return;
        }

        if (worldPos.z < worldMinSize.z || worldPos.z > worldMaxSize.z)
        {
            return;
        }

        Vector2 delta = new Vector2(worldPosLast.x - worldPos.x, worldPosLast.z - worldPos.z);

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

        int deltaChunksXAbs = Mathf.Abs(deltaChunksX);
        int deltaChunksZAbs = Mathf.Abs(deltaChunksZ);
        if (deltaChunksXAbs > 9 || deltaChunksZAbs > 9)
        {
            bShouldRedrawMap = true;
            return;
        }

        if (deltaChunksX != 0 || deltaChunksZ != 0)
        {
            //Log.Out("=============================================");
            //Log.Out("======先前数据：worldPos={0}，mapMiddlePosPixel={1}，mapMiddlePosChunks={2}", worldPos, mapMiddlePosPixel, mapMiddlePosChunks);
            //Log.Out("======累计滚动：mapScrollTextureOffset={0}，mapScrollTextureChunksOffsetX={1},mapScrollTextureChunksOffsetZ={2}", mapScrollTextureOffset, mapScrollTextureChunksOffsetX, mapScrollTextureChunksOffsetZ);
            //Log.Out("======滚动更新：deltaChunksX={0}，deltaChunksZ={1}", deltaChunksX, deltaChunksZ);
            UpdateMapByScroll(deltaChunksX, deltaChunksZ);
            //Log.Out("=============================================");
        }
    }

    public void UpdateMapByScroll(int deltaChunksX, int deltaChunksZ)
    {
        int mapMiddlePosChunksX = (int)mapMiddlePosChunks.x;
        int mapMiddlePosChunksZ = (int)mapMiddlePosChunks.y;

        if (deltaChunksX != 0)
        {
            int deltaChunksXAbs = Mathf.Abs(deltaChunksX);

            int outStartX;
            int outEndX;
            int outDrawnStartX;
            int outDrawnEndX;

            int inStartX;
            int inEndX;
            int inDrawnStartX;
            int inDrawnEndX;

            if (deltaChunksX > 0)
            {
                outEndX = mapMiddlePosChunksX + mapSize / 2;
                outStartX = outEndX - deltaChunksXAbs * 16;

                outDrawnStartX = mapScrollTextureChunksOffsetX * 16;
                outDrawnStartX = outDrawnStartX == mapSize ? 0 : outDrawnStartX;
                outDrawnEndX = outDrawnStartX + deltaChunksX * 16;

                inEndX = mapMiddlePosChunksX + mapVisualRangePositive;
                inStartX = inEndX - mapVisualRange;

                inDrawnStartX = outDrawnStartX + mapSize / 2 - mapVisualRangeNegative + deltaChunksXAbs * 16;
                inDrawnStartX = Utils.WrapIndex(inDrawnStartX, mapSize);
                inDrawnStartX = inDrawnStartX == mapSize ? 0 : inDrawnStartX;
                inDrawnEndX = inDrawnStartX + mapVisualRange;
            }
            else
            {
                outStartX = mapMiddlePosChunksX - mapSize / 2;
                outEndX = outStartX + deltaChunksXAbs * 16;

                outDrawnEndX = mapScrollTextureChunksOffsetX * 16;
                outDrawnEndX = outDrawnEndX == 0 ? mapSize : outDrawnEndX;
                outDrawnStartX = outDrawnEndX + deltaChunksX * 16;

                inStartX = mapMiddlePosChunksX - mapVisualRangeNegative;
                inEndX = inStartX + mapVisualRange;

                inDrawnEndX = outDrawnEndX - mapSize / 2 + mapVisualRangePositive - deltaChunksXAbs * 16;
                inDrawnEndX = Utils.WrapIndex(inDrawnEndX, mapSize);
                inDrawnEndX = inDrawnEndX == 0 ? mapSize : inDrawnEndX;
                inDrawnStartX = inDrawnEndX - mapVisualRange;
            }

            int outStartZ = mapMiddlePosChunksZ - mapSize / 2;
            int outEndZ = outStartZ + mapSize;

            int outDrawnEndZ = mapScrollTextureChunksOffsetZ * 16;
            outDrawnEndZ = Utils.WrapIndex(outDrawnEndZ, mapSize);
            outDrawnEndZ = outDrawnEndZ == 0 ? mapSize : outDrawnEndZ;
            int outDrawnStartZ = Utils.WrapIndex(outDrawnEndZ - mapSize, mapSize);

            //更新外层地图
            //Log.Out("======X轴外层-滚动更新：outStartX={0}，outEndX={1}，outStartZ={2}，outEndZ={3}", outStartX, outEndX, outStartZ, outEndZ);
            //Log.Out("======X轴外层-滚动绘制：outDrawnStartX={0}，outDrawnEndX={1}，outDrawnStartZ={2}，outDrawnEndZ={3}", outDrawnStartX, outDrawnEndX, outDrawnStartZ, outDrawnEndZ);
            UpdateMapSection(outStartX, outStartZ, outEndX, outEndZ, outDrawnStartX, outDrawnStartZ, outDrawnEndX, outDrawnEndZ);

            int inStartZ = mapMiddlePosChunksZ - mapVisualRangeNegative;
            int inEndZ = inStartZ + mapVisualRange;

            int inDrawnEndZ = mapScrollTextureChunksOffsetZ * 16 + mapVisualRangePositive + mapSize / 2;
            inDrawnEndZ = Utils.WrapIndex(inDrawnEndZ, mapSize);
            inDrawnEndZ = inDrawnEndZ == 0 ? mapSize : inDrawnEndZ;
            int inDrawnStartZ = inDrawnEndZ - mapVisualRange;

            //更新内层地图
            //Log.Out("======X轴内层-滚动更新：inStartX={0}，inEndX={1}，inStartZ={2}，inEndZ={3}", inStartX, inEndX, inStartZ, inEndZ);
            //Log.Out("======X轴内层-滚动绘制：inDrawnStartX={0}，inDrawnEndX={1}，inDrawnStartZ={2}，inDrawnEndZ={3}", inDrawnStartX, inDrawnEndX, inDrawnStartZ, inDrawnEndZ);
            UpdateMapSection(inStartX, inStartZ, inEndX, inEndZ, inDrawnStartX, inDrawnStartZ, inDrawnEndX, inDrawnEndZ);
        }

        if (deltaChunksZ != 0)
        {
            int deltaChunksZAbs = Mathf.Abs(deltaChunksZ);

            int outStartZ;
            int outEndZ;
            int outDrawnStartZ;
            int outDrawnEndZ;

            int inStartZ;
            int inEndZ;
            int inDrawnStartZ;
            int inDrawnEndZ;

            if (deltaChunksZ > 0)
            {
                outEndZ = mapMiddlePosChunksZ + mapSize / 2;
                outStartZ = outEndZ - deltaChunksZAbs * 16;

                outDrawnStartZ = mapScrollTextureChunksOffsetZ * 16;
                outDrawnStartZ = outDrawnStartZ == mapSize ? 0 : outDrawnStartZ;
                outDrawnEndZ = outDrawnStartZ + deltaChunksZ * 16;

                inEndZ = mapMiddlePosChunksZ + mapVisualRangePositive;
                inStartZ = inEndZ - mapVisualRange;

                inDrawnStartZ = outDrawnStartZ + mapSize / 2 - mapVisualRangeNegative + deltaChunksZAbs * 16;
                inDrawnStartZ = Utils.WrapIndex(inDrawnStartZ, mapSize);
                inDrawnStartZ = inDrawnStartZ == mapSize ? 0 : inDrawnStartZ;
                inDrawnEndZ = inDrawnStartZ + mapVisualRange;
            }
            else
            {
                outStartZ = mapMiddlePosChunksZ - mapSize / 2;
                outEndZ = outStartZ + deltaChunksZAbs * 16;

                outDrawnEndZ = mapScrollTextureChunksOffsetZ * 16;
                outDrawnEndZ = outDrawnEndZ == 0 ? mapSize : outDrawnEndZ;
                outDrawnStartZ = outDrawnEndZ + deltaChunksZ * 16;

                inStartZ = mapMiddlePosChunksZ - mapVisualRangeNegative;
                inEndZ = inStartZ + mapVisualRange;

                inDrawnEndZ = outDrawnEndZ - mapSize / 2 + mapVisualRangePositive - deltaChunksZAbs * 16;
                inDrawnEndZ = Utils.WrapIndex(inDrawnEndZ, mapSize);
                inDrawnEndZ = inDrawnEndZ == 0 ? mapSize : inDrawnEndZ;
                inDrawnStartZ = inDrawnEndZ - mapVisualRange;
            }

            int outStartX = mapMiddlePosChunksX - mapSize / 2;
            int outEndX = outStartX + mapSize;

            int outDrawnEndX = mapScrollTextureChunksOffsetX * 16;
            outDrawnEndX = Utils.WrapIndex(outDrawnEndX, mapSize);
            outDrawnEndX = outDrawnEndX == 0 ? mapSize : outDrawnEndX;
            int outDrawnStartX = Utils.WrapIndex(outDrawnEndX - mapSize, mapSize);

            //更新外层地图
            //Log.Out("======Z轴外层-滚动更新：outStartX={0}，outEndX={1}，outStartZ={2}，outEndZ={3}", outStartX, outEndX, outStartZ, outEndZ);
            //Log.Out("======Z轴外层-滚动绘制：outDrawnStartX={0}，outDrawnEndX={1}，outDrawnStartZ={2}，outDrawnEndZ={3}", outDrawnStartX, outDrawnEndX, outDrawnStartZ, outDrawnEndZ);
            UpdateMapSection(outStartX, outStartZ, outEndX, outEndZ, outDrawnStartX, outDrawnStartZ, outDrawnEndX, outDrawnEndZ);

            int inStartX = mapMiddlePosChunksX - mapVisualRangeNegative;
            int inEndX = inStartX + mapVisualRange;

            int inDrawnEndX = mapScrollTextureChunksOffsetX * 16 + mapVisualRangePositive + mapSize / 2;
            inDrawnEndX = Utils.WrapIndex(inDrawnEndX, mapSize);
            inDrawnEndX = inDrawnEndX == 0 ? mapSize : inDrawnEndX;
            int inDrawnStartX = inDrawnEndX - mapVisualRange;

            //更新内层地图
            //Log.Out("======Z轴内层-滚动更新：inStartX={0}，inEndX={1}，inStartZ={2}，inEndZ={3}", inStartX, inEndX, inStartZ, inEndZ);
            //Log.Out("======Z轴内层-滚动绘制：inDrawnStartX={0}，inDrawnEndX={1}，inDrawnStartZ={2}，inDrawnEndZ={3}", inDrawnStartX, inDrawnEndX, inDrawnStartZ, inDrawnEndZ);
            UpdateMapSection(inStartX, inStartZ, inEndX, inEndZ, inDrawnStartX, inDrawnStartZ, inDrawnEndX, inDrawnEndZ);
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
        SendMapPositionToServer();
    }

    /* DONE */
    public void UpdateMapSection(int startX, int startZ, int endX, int endZ, int drawnStartX, int drawnStartZ, int drawnEndX, int drawnEndZ)
    {
        IMapChunkDatabase mapDatabase = base.xui.playerUI.entityPlayer.ChunkObserver.mapDatabase;
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
                drwanCurrentZ = Utils.WrapIndex(drwanCurrentZ + 16, drawnEndZ);
            }
            currentX += 16;
            drwanCurrentX = Utils.WrapIndex(drwanCurrentX + 16, drawnEndX);
        }
    }

    /* DONE */
    public void UpdateMapSectionRender(int startX, int startZ, int endX, int endZ, int drawnStartX, int drawnStartZ, int drawnEndX, int drawnEndZ)
    {
        IMapChunkDatabase mapDatabase = base.xui.playerUI.entityPlayer.ChunkObserver.mapDatabase;
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
                        rawTextureData[rawCurrentIndex] = new Color32(0, 229, 255, byte.MaxValue);
                    }
                    //mapMaskChunks.Add(currentX, currentZ);
                }
                else
                {
                    for (int k = 0; k < 256; k++)
                    {
                        int rawCurrentX = drwanCurrentX + k % 16;
                        int rawCurrentZ = (drwanCurrentZ + k / 16) * mapSize;
                        int rawCurrentIndex = rawCurrentZ + rawCurrentX;
                        Color32 rawCurrentColor = Utils.FromColor5To32(currentChunkColors[k]);
                        rawTextureData[rawCurrentIndex] = new Color32(0, 229, 255, byte.MaxValue);
                    }
                }
                currentZ += 16;
                drwanCurrentZ = Utils.WrapIndex(drwanCurrentZ + 16, drawnEndZ);
            }
            currentX += 16;
            drwanCurrentX = Utils.WrapIndex(drwanCurrentX + 16, drawnEndX);
        }
    }

    public void UpdateMapPos()
    {
        float mapPosBase = (mapSize - mapSizeZoom * mapZoomScale) / 2f;
        float mapPosBaseX = (mapPosBase + (mapMiddlePosPixel.x - mapMiddlePosChunks.x)) / mapSize;
        float mapPosBaseZ = (mapPosBase + (mapMiddlePosPixel.y - mapMiddlePosChunks.y)) / mapSize;
        mapPos = new Vector2(mapPosBaseX + mapScrollTextureOffset.x, mapPosBaseZ + mapScrollTextureOffset.y);
        mapBGPos = new Vector2((mapPosBase + mapMiddlePosPixel.x) / mapSize, (mapPosBase + mapMiddlePosPixel.y) / mapSize);
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
                if (navObject.trackedEntity is EntityPlayer)
                {
                    uISprite.width = (int)((float)(spriteSizeZoom + 5) * vector.x);
                    uISprite.height = (int)((float)(spriteSizeZoom + 5) * vector.y);
                    uISprite.depth = 99;
                }
                else
                {
                    uISprite.width = (int)((float)spriteSizeZoom * vector.x);
                    uISprite.height = (int)((float)spriteSizeZoom * vector.y);
                }
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

using System.Collections.Generic;
using UnityEngine;

public class XUiC_PocketMirror : XUiController
{
    public XUiController previewFrame;

    public XUiV_Texture textPreview;

    public EntityPlayer ep;

    public RuntimeAnimatorController animationController;

    public float atlasResolutionScale;

    public RenderTextureSystem renderTextureSystem = new RenderTextureSystem();

    public bool isDirty;

    public bool isPreviewDirty;

    public EntityPlayer player;

    public List<DisplayInfoEntry> displayInfoEntries;

    public float updateTime;

    public GameObject previewSDCSObj;

    public SDCSUtils.TransformCatalog transformCatalog;

    public override void Init()
    {
        base.Init();
        previewFrame = GetChildById("playerPreviewSDCS");
        previewFrame.OnPress += PreviewFrame_OnPress;
        previewFrame.OnHover += PreviewFrame_OnHover;
        textPreview = (XUiV_Texture)GetChildById("playerPreviewSDCS").ViewComponent;
        isDirty = true;
        XUiM_PlayerEquipment.HandleRefreshEquipment += XUiM_PlayerEquipment_HandleRefreshEquipment;
        base.xui.playerUI.OnUIShutdown += HandleUIShutdown;
        base.xui.OnShutdown += HandleUIShutdown;
    }

    public void HandleUIShutdown()
    {
        base.xui.playerUI.OnUIShutdown -= HandleUIShutdown;
        base.xui.OnShutdown -= HandleUIShutdown;
        XUiM_PlayerEquipment.HandleRefreshEquipment -= XUiM_PlayerEquipment_HandleRefreshEquipment;
    }

    public void PreviewFrame_OnHover(XUiController _sender, bool _isOver)
    {
        renderTextureSystem.RotateTarget(Time.deltaTime * 10f);
    }

    public void PreviewFrame_OnPress(XUiController _sender, int _mouseButton)
    {
        if (base.xui.dragAndDrop.CurrentStack != ItemStack.Empty)
        {
            ItemStack itemStack = base.xui.PlayerEquipment.EquipItem(base.xui.dragAndDrop.CurrentStack);
            if (base.xui.dragAndDrop.CurrentStack != itemStack)
            {
                base.xui.dragAndDrop.CurrentStack = itemStack;
                base.xui.dragAndDrop.PickUpType = XUiC_ItemStack.StackLocationTypes.Equipment;
            }
        }
    }

    public void XUiM_PlayerEquipment_HandleRefreshEquipment(XUiM_PlayerEquipment _playerEquipment)
    {
    }

    public override void Update(float _dt)
    {
        if (GameManager.Instance == null || GameManager.Instance.World == null)
        {
            return;
        }
        if (ep == null)
        {
            ep = base.xui.playerUI.entityPlayer;
        }
        if (Time.time > updateTime)
        {
            updateTime = Time.time + 0.25f;
            RefreshBindings(isDirty);
        }
        if (isDirty)
        {
            if (player == null)
            {
                return;
            }
            isDirty = false;
            RefreshBindings();
        }
        if (isPreviewDirty)
        {
            MakePreview();
        }
        textPreview.Texture = renderTextureSystem.RenderTex;
        if (previewSDCSObj != null)
        {
            previewSDCSObj.transform.localEulerAngles = new Vector3(0f, 180f, 0f);
        }
        base.Update(_dt);
    }

    public override void OnOpen()
    {
        base.OnOpen();
        isDirty = true;
        isPreviewDirty = true;
        player = base.xui.playerUI.entityPlayer;
        if (previewFrame != null)
        {
            previewFrame.OnPress -= PreviewFrame_OnPress;
            previewFrame.OnHover -= PreviewFrame_OnHover;
        }
        previewFrame = GetChildById("previewFrameSDCS");
        previewFrame.OnPress += PreviewFrame_OnPress;
        previewFrame.OnHover += PreviewFrame_OnHover;
        textPreview = (XUiV_Texture)GetChildById("playerPreviewSDCS").ViewComponent;
        if (renderTextureSystem.ParentGO == null)
        {
            renderTextureSystem.Create("playermirror", new GameObject(), new Vector3(0f, -0.5f, 3f), new Vector3(0f, -0.2f, 7.5f), new Vector2i(300, 600), _isAA: true);
            Log.Out("---------------当前窗口尺寸：{0}", renderTextureSystem.cam.pixelRect.ToString());
        }
        displayInfoEntries = UIDisplayInfoManager.Current.GetCharacterDisplayInfo();
        if (player as EntityPlayerLocal != null && player.emodel as EModelSDCS != null)
        {
            XUiM_PlayerEquipment.HandleRefreshEquipment += XUiM_PlayerEquipment_HandleRefreshEquipment1;
        }
    }

    public void XUiM_PlayerEquipment_HandleRefreshEquipment1(XUiM_PlayerEquipment playerEquipment)
    {
        if (base.IsOpen)
        {
            MakePreview();
        }
    }

    public override void OnClose()
    {
        base.OnClose();
        XUiM_PlayerEquipment.HandleRefreshEquipment -= XUiM_PlayerEquipment_HandleRefreshEquipment1;
        SDCSUtils.DestroyViz(previewSDCSObj);
        renderTextureSystem.Cleanup();
    }

    public void MakePreview()
    {
        if (!(ep == null) && !(ep.emodel == null) && ep.emodel is EModelSDCS eModelSDCS)
        {
            isPreviewDirty = false;
            SDCSUtils.CreateVizUI(eModelSDCS.Archetype, ref previewSDCSObj, ref transformCatalog, ep);
            Utils.SetLayerRecursively(previewSDCSObj, 11);
            Transform transform = previewSDCSObj.transform;
            transform.SetParent(renderTextureSystem.ParentGO.transform, worldPositionStays: false);
            transform.localPosition = new Vector3(0.022f, -2.9f, 12f);
            transform.localEulerAngles = new Vector3(0f, 180f, 0f);
            renderTextureSystem.SetOrtho(enabled: true, 0.95f);
        }
    }
}

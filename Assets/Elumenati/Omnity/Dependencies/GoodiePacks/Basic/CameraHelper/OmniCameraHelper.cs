using System.Xml.XPath;
using UnityEngine;

#if USE_OMNITYDLLSOURCE
namespace DLLExports {
#else
#endif

public class OmniCameraHelper : OmnityTabbedFeature {
    static public OmnityPluginsIDs _myOmnityPluginsID = OmnityPluginsIDs.OmniCameraHelper;
    override public OmnityPluginsIDs myOmnityPluginsID { get { return _myOmnityPluginsID; } }

    static public OmniCameraHelper OmniCreateSingleton(GameObject go) {
        return GetSingleton<OmniCameraHelper>(ref singleton, go);
    }

    private void Awake() {
        if (VerifySingleton<OmniCameraHelper>(ref singleton)) {
            Omnity.onReloadEndCallbackPriority.AddLoaderFunction(PriorityEventHandler.Ordering.Order_OmniCameraHelper, CoroutineLoader);
            Omnity.onSaveCompleteCallback += Save;
        }
    }

    override public string BaseName {
        get {
            return "CameraHelper";
        }
    }

    public Camera _targetCamera = null;

    public Camera targetCamera {
        get {
            if (_targetCamera == null) {
                _targetCamera = GetComponent<Camera>();
            }
            return _targetCamera;
        }
        set {
            _targetCamera = value;
        }
    }

    public enum UpdateOn {
        Default,
        Manual,
        StartUp,
        OnUpdate,
        OnUpdateClipPlane,// update only if clip planes don't match,
    };

    [System.NonSerialized]
    static public System.Func<Omnity, int> fnCullingMaskForFinalPassCameraFlat = null;

    [System.NonSerialized]
    static public System.Func<Omnity, int> fnCullingMaskForRenderChannelCamera = null;

    private const bool copyClearFlagsDefault = true;
    private const bool copyCullingMaskDefault = true;
    private const bool copyBackgroundColorDefault = true;
    private const bool copyDepthDefault = true;
    private const bool copyRenderingPathDefault = true;
    private const bool copyClippingPlanesDefault = true;
    private const bool copyOcclusionCullingDefault = true;
    private const bool copyHDRDefault = true;
    private const bool copyMSAADefault = true;
    private const bool premaskFinalPassCameraDefault = true;
    private const UpdateOn updateOnDefault = UpdateOn.Default;

    public bool copyClearFlags = copyClearFlagsDefault;
    public bool copyBackgroundColor = copyBackgroundColorDefault;
    public bool copyCullingMask = copyCullingMaskDefault;
    public bool copyDepth = copyDepthDefault;
    public bool copyRenderingPath = copyRenderingPathDefault;
    public bool copyClippingPlanes = copyClippingPlanesDefault;
    public bool copyOcclusionCulling = copyOcclusionCullingDefault;
    public bool copyHDR = copyHDRDefault;
    public bool copyMSAA = copyMSAADefault;

    public bool premaskFinalPassCamera = true;
    public UpdateOn updateOn = UpdateOn.Default;

    public override void ReadXMLDelegate(XPathNavigator nav) {
        copyClearFlags = OmnityHelperFunctions.ReadElementBoolDefault(nav, ".//copyClearFlags", copyClearFlagsDefault);
        copyBackgroundColor = OmnityHelperFunctions.ReadElementBoolDefault(nav, ".//copyBackgroundColor", copyBackgroundColorDefault);
        copyCullingMask = OmnityHelperFunctions.ReadElementBoolDefault(nav, ".//copyCullingMask", copyCullingMaskDefault);
        copyDepth = OmnityHelperFunctions.ReadElementBoolDefault(nav, ".//copyDepth", copyDepthDefault);
        copyRenderingPath = OmnityHelperFunctions.ReadElementBoolDefault(nav, ".//copyRenderingPath", copyRenderingPathDefault);
        copyClippingPlanes = OmnityHelperFunctions.ReadElementBoolDefault(nav, ".//copyClippingPlanes", copyClippingPlanesDefault);
        copyOcclusionCulling = OmnityHelperFunctions.ReadElementBoolDefault(nav, ".//copyOcclusionCulling", copyOcclusionCullingDefault);
        copyHDR = OmnityHelperFunctions.ReadElementBoolDefault(nav, ".//copyHDR", copyHDRDefault);
        copyMSAA = OmnityHelperFunctions.ReadElementBoolDefault(nav, ".//copyMSAA", copyMSAADefault);
        
        updateOn = OmnityHelperFunctions.ReadElementEnumDefault<UpdateOn>(nav, ".//updateOn", updateOnDefault);
        fpcMode = OmnityHelperFunctions.ReadElementEnumDefault<FinalPassCameraMode>(nav, ".//fpcMode", fpcModeDefault);

        premaskFinalPassCamera = OmnityHelperFunctions.ReadElementBoolDefault(nav, ".//premaskFinalPassCamera", premaskFinalPassCameraDefault);
        try {
            CopyCameraSettings(Omnity.anOmnity, targetCamera);
        } catch (System.Exception e) {
            Debug.LogError(e.Message);
        }
    }

    /// <param name="xmlWriter">The XML writer.</param>
    override public void WriteXMLDelegate(System.Xml.XmlTextWriter xmlWriter) {
        xmlWriter.WriteElementString("copyClearFlags", copyClearFlags.ToString());
        xmlWriter.WriteElementString("copyBackgroundColor", copyBackgroundColor.ToString());
        xmlWriter.WriteElementString("copyCullingMask", copyCullingMask.ToString());
        xmlWriter.WriteElementString("copyDepth", copyDepth.ToString());
        xmlWriter.WriteElementString("copyRenderingPath", copyRenderingPath.ToString());
        xmlWriter.WriteElementString("copyClippingPlanes", copyClippingPlanes.ToString());
        xmlWriter.WriteElementString("copyOcclusionCulling", copyOcclusionCulling.ToString());
        xmlWriter.WriteElementString("copyHDR", copyHDR.ToString());
        xmlWriter.WriteElementString("copyMSAA", copyMSAA.ToString());
        xmlWriter.WriteElementString("updateOn", updateOn.ToString());
        xmlWriter.WriteElementString("premaskFinalPassCamera", premaskFinalPassCamera.ToString());
        xmlWriter.WriteElementString("fpcMode", fpcMode.ToString());
    }

    static private OmniCameraHelper singleton = null;

    public const OmnityPluginsIDs pluginID = OmnityPluginsIDs.OmniCameraHelper;

    private void OnDestroy() {
        singleton = null;
        Omnity.onReloadEndCallbackPriority.RemoveLoaderFunction(PriorityEventHandler.Ordering.Order_OmniCameraHelper, CoroutineLoader);
        Omnity.onSaveCompleteCallback -= Save;
    }

    /// <summary>
    /// Overload this with the gui layout calls.
    /// </summary>
    override public void MyGuiCallback(Omnity anOmnity) {
        bool wasChanged = false;
        wasChanged |= OmnityHelperFunctions.BoolInputResetWasChanged("copyClearFlags", ref copyClearFlags, copyClearFlagsDefault);
        wasChanged |= OmnityHelperFunctions.BoolInputResetWasChanged("copyBackgroundColor", ref copyBackgroundColor, copyBackgroundColorDefault);
        wasChanged |= OmnityHelperFunctions.BoolInputResetWasChanged("copyCullingMask", ref copyCullingMask, copyCullingMaskDefault);
        wasChanged |= OmnityHelperFunctions.BoolInputResetWasChanged("copyDepth", ref copyDepth, copyDepthDefault);
        wasChanged |= OmnityHelperFunctions.BoolInputResetWasChanged("copyRenderingPath", ref copyRenderingPath, copyRenderingPathDefault);
        wasChanged |= OmnityHelperFunctions.BoolInputResetWasChanged("copyClippingPlanes", ref copyClippingPlanes, copyClippingPlanesDefault);
        wasChanged |= OmnityHelperFunctions.BoolInputResetWasChanged("copyOcclusionCulling", ref copyOcclusionCulling, copyOcclusionCullingDefault);
        wasChanged |= OmnityHelperFunctions.BoolInputResetWasChanged("copyHDR", ref copyHDR, copyHDRDefault);
        wasChanged |= OmnityHelperFunctions.BoolInputResetWasChanged("copyMSAA", ref copyMSAA, copyMSAADefault);
        wasChanged |= OmnityHelperFunctions.BoolInputResetWasChanged("premaskFinalPassCamera (recommended)", ref premaskFinalPassCamera, premaskFinalPassCameraDefault);

        wasChanged |= OmnityHelperFunctions.EnumInputResetWasChanged<UpdateOn>("camHelpUpdate", "updateOn", ref updateOn, updateOnDefault, 1);
        wasChanged |= OmnityHelperFunctions.EnumInputResetWasChanged<FinalPassCameraMode>("fpcMode", "Final Pass Camera Mode", ref fpcMode, fpcModeDefault, 1);

        if (wasChanged) {
            switch (updateOn) {
                case UpdateOn.Default:
                    CopyCameraSettings(anOmnity, targetCamera);
                    break;

                case UpdateOn.Manual:
                    break;

                case UpdateOn.StartUp:
                    CopyCameraSettings(anOmnity, targetCamera);
                    break;

                case UpdateOn.OnUpdate:
                    CopyCameraSettings(anOmnity, targetCamera);
                    break;

                case UpdateOn.OnUpdateClipPlane:// update only if clip planes don't match,
                    CopyCameraSettings(anOmnity, targetCamera);
                    break;

                default:
                    Debug.Log("unknown " + updateOn);
                    break;
            }
        }

        SaveLoadGUIButtons(anOmnity);
    }

    /// <summary>
    /// Copy the Camera's Settings to Omnity
    /// </summary>
    /// <param name="anOmnity">An omnity.</param>
    /// <param name="hostCamera">The host camera.</param>
    public void CopyCameraSettings(Omnity anOmnity, Camera hostCamera = null) {
        if (hostCamera == null) {
            hostCamera = targetCamera;
        }

        if (hostCamera != null) {
            for (int i = 0; i < anOmnity.cameraArray.Length; i++) {
                var targetCameraC = anOmnity.cameraArray[i];
                var targetCamera = targetCameraC.myCamera;
                if (targetCamera == null) {
                    return;
                }
                if (copyClearFlags) {
                    targetCamera.clearFlags = hostCamera.clearFlags;
                }

                if (copyBackgroundColor) {
                    targetCamera.backgroundColor = hostCamera.backgroundColor;
                }
                if (copyCullingMask) {
                    int finalPassCameraMaskForFlat = 0;
                    if (fnCullingMaskForRenderChannelCamera != null) {
                        finalPassCameraMaskForFlat = fnCullingMaskForRenderChannelCamera(anOmnity);
                    } else {
                        finalPassCameraMaskForFlat = hostCamera.cullingMask;
                    }
                    if (premaskFinalPassCamera) {
                        int mask = 1 << 31 | 1 << 30;
                        mask = ~mask;

                        targetCamera.cullingMask = targetCameraC.cullingMask = finalPassCameraMaskForFlat & mask;
                    } else {
                        targetCamera.cullingMask = targetCameraC.cullingMask = finalPassCameraMaskForFlat;
                    }
                }
                if (copyDepth) {
                    targetCamera.depth = hostCamera.depth;
                }
                if (copyRenderingPath) {
                    targetCamera.renderingPath = hostCamera.renderingPath;
                }
                if (copyClippingPlanes) {
                    if (targetCamera.orthographic) {
                        targetCameraC.omnityPerspectiveMatrix.nearOrtho = targetCamera.nearClipPlane = hostCamera.nearClipPlane;
                        targetCameraC.omnityPerspectiveMatrix.farOrtho = targetCamera.farClipPlane = hostCamera.farClipPlane;
                    } else {
                        targetCameraC.omnityPerspectiveMatrix.near = targetCamera.nearClipPlane = hostCamera.nearClipPlane;
                        targetCameraC.omnityPerspectiveMatrix.far = targetCamera.farClipPlane = hostCamera.farClipPlane;
                    }
                }
                if (copyOcclusionCulling) {
                    targetCamera.useOcclusionCulling = hostCamera.useOcclusionCulling;
                }
                if (copyHDR) {
                    OmnityPlatformDefines.CopyHDRSettingsFromTo(hostCamera, targetCamera);
                }
                if (copyMSAA) {
                    OmnityPlatformDefines.CopyMSAASettingsFromTo(hostCamera, targetCamera);
                }
            }
            if (fpcMode != FinalPassCameraMode.ApplyToNone) {
                ApplyToFinalPassCameraIndex(anOmnity, fpcMode, hostCamera);
            }
        }
    }

    private void CopyCameraSettingsHelper(FinalPassCamera targetCameraC, Camera hostCamera, Omnity anOmnity) {
        var targetCamera = targetCameraC.myCamera;
        if (targetCamera == null) {
            return;
        }
        if (copyClearFlags) {
            targetCamera.clearFlags = hostCamera.clearFlags;
        }

        if (copyBackgroundColor) {
            targetCamera.backgroundColor = hostCamera.backgroundColor;
        }
        if (copyCullingMask) {
            int finalPassCameraMaskForFlat = 0;
            if (fnCullingMaskForFinalPassCameraFlat != null) {
                finalPassCameraMaskForFlat = fnCullingMaskForFinalPassCameraFlat(anOmnity);
            } else {
                finalPassCameraMaskForFlat = hostCamera.cullingMask;
            }

            if (premaskFinalPassCamera) {
                int mask = 1 << 31 | 1 << 30;
                mask = ~mask;
                targetCamera.cullingMask = targetCameraC.cullingMask = finalPassCameraMaskForFlat & mask;
            } else {
                targetCamera.cullingMask = targetCameraC.cullingMask = finalPassCameraMaskForFlat;
            }
        }
        if (copyDepth) {
            targetCamera.depth = hostCamera.depth;
        }
        if (copyRenderingPath) {
            targetCamera.renderingPath = hostCamera.renderingPath;
        }
        if (copyClippingPlanes) {
            if (targetCamera.orthographic) {
                targetCameraC.omnityPerspectiveMatrix.nearOrtho = targetCamera.nearClipPlane = hostCamera.nearClipPlane;
                targetCameraC.omnityPerspectiveMatrix.farOrtho = targetCamera.farClipPlane = hostCamera.farClipPlane;
            } else {
                targetCameraC.omnityPerspectiveMatrix.near = targetCamera.nearClipPlane = hostCamera.nearClipPlane;
                targetCameraC.omnityPerspectiveMatrix.far = targetCamera.farClipPlane = hostCamera.farClipPlane;
            }
        }
        if (copyOcclusionCulling) {
            targetCamera.useOcclusionCulling = hostCamera.useOcclusionCulling;
        }
        if (copyHDR) {
            OmnityPlatformDefines.CopyHDRSettingsFromTo(hostCamera, targetCamera);
        }
        if (copyMSAA) {
            OmnityPlatformDefines.CopyMSAASettingsFromTo(hostCamera, targetCamera);
        }
    }

    /// <summary>
    /// Copy the Camera's Settings to Omnity
    /// </summary>
    /// <param name="anOmnity">An omnity.</param>
    /// <param name="hostCamera">The host camera.</param>
    public void ApplyToPerspectiveCamera(Omnity anOmnity, Omnity.PerspectiveCameraDelegate func) {
        if (anOmnity.PluginEnabled(pluginID)) {
            for (int i = 0; i < anOmnity.cameraArray.Length; i++) {
                func(anOmnity.cameraArray[i]);
            }
        }
    }

    public void ApplyToFinalPassCameraIndex(Omnity anOmnity, FinalPassCameraMode mode, Camera cam) {
        switch (mode) {
            case FinalPassCameraMode.ApplyToIndex0:
                if (anOmnity.finalPassCameras.Length > 0) {
                    CopyCameraSettingsHelper(anOmnity.finalPassCameras[0], cam, anOmnity);
                }
                break;

            case FinalPassCameraMode.ApplyToIndex1:
                if (anOmnity.finalPassCameras.Length > 1) {
                    CopyCameraSettingsHelper(anOmnity.finalPassCameras[1], cam, anOmnity);
                }
                break;

            case FinalPassCameraMode.ApplyToIndex2:
                if (anOmnity.finalPassCameras.Length > 2) {
                    CopyCameraSettingsHelper(anOmnity.finalPassCameras[2], cam, anOmnity);
                }
                break;

            case FinalPassCameraMode.ApplyToIndex3:
                if (anOmnity.finalPassCameras.Length > 3) {
                    CopyCameraSettingsHelper(anOmnity.finalPassCameras[3], cam, anOmnity);
                }
                break;

            case FinalPassCameraMode.ApplyToIndex4:
                if (anOmnity.finalPassCameras.Length > 4) {
                    CopyCameraSettingsHelper(anOmnity.finalPassCameras[4], cam, anOmnity);
                }
                break;

            case FinalPassCameraMode.ApplyToLast:
                if (anOmnity.finalPassCameras.Length > 0) {
                    CopyCameraSettingsHelper(anOmnity.finalPassCameras[anOmnity.finalPassCameras.Length - 1], cam, anOmnity);
                }
                break;

            case FinalPassCameraMode.ApplyToNone:
                break;

            case FinalPassCameraMode.ApplyToAll:
                for (int i = 0; i < anOmnity.finalPassCameras.Length; i++) {
                    CopyCameraSettingsHelper(anOmnity.finalPassCameras[i], cam, anOmnity);
                }
                break;

            case FinalPassCameraMode.ApplyToAnythingWithTheWord_Flat:
                for (int i = 0; i < anOmnity.finalPassCameras.Length; i++) {
                    if (anOmnity.finalPassCameras[i].name.ContainsCaseInsensitiveSimple("flat")) {
                        CopyCameraSettingsHelper(anOmnity.finalPassCameras[i], cam, anOmnity);
                    }
                }
                break;

            default:
                Debug.LogError("Unknown mode" + mode);
                break;
        }
    }

    // Use this for initialization
    private void Start() {
    }

    // Update is called once per frame
    override public void Update() {
        base.Update();
        Omnity anOmnity = Omnity.anOmnity;
        if (anOmnity == null || !anOmnity.PluginEnabled(pluginID)) {
            return;
        }
        switch (updateOn) {
            case UpdateOn.Default:
                if (ClipPlanesMisMatch(anOmnity)) {
                    CopyCameraSettings(anOmnity, targetCamera);
                }
                break;

            case UpdateOn.Manual:
                break;

            case UpdateOn.StartUp:
                break;

            case UpdateOn.OnUpdate:
                CopyCameraSettings(anOmnity, targetCamera);
                break;

            case UpdateOn.OnUpdateClipPlane:// update only if clip planes don't match,
                if (ClipPlanesMisMatch(anOmnity)) {
                    CopyCameraSettings(anOmnity, targetCamera);
                }
                break;

            default:
                Debug.Log("unknown " + updateOn);
                break;
        }
    }

    public enum FinalPassCameraMode {
        ApplyToAnythingWithTheWord_Flat = -4,
        ApplyToAll = -3,
        ApplyToLast = -2,
        ApplyToNone = -1,
        ApplyToIndex0 = 0,
        ApplyToIndex1 = 1,
        ApplyToIndex2 = 2,
        ApplyToIndex3 = 3,
        ApplyToIndex4 = 4,
    }

    private const FinalPassCameraMode fpcModeDefault = FinalPassCameraMode.ApplyToAnythingWithTheWord_Flat;
    private FinalPassCameraMode fpcMode = fpcModeDefault;

    public override void Unload() {
        base.Unload();
    }

    public bool ClipPlanesMisMatch(Omnity anOmnity, float nearClipPlane, float farClipPlane) {
        if (anOmnity != null) {
            if (anOmnity.cameraArray.Length > 0) {
                // assume we have equal cameras so lets check only one
                // if the we have more exotic configurations this will fail
                if (nearClipPlane != anOmnity.cameraArray[0].omnityPerspectiveMatrix.near || farClipPlane != anOmnity.cameraArray[0].omnityPerspectiveMatrix.far) {
                    return true;
                }
            } else {
                // assume we have only one flat pass camera and it is flat so lets check that
                // if the we have more exotic configurations this will fail
                if (anOmnity.finalPassCameras.Length > 0) {
                    if (nearClipPlane != anOmnity.finalPassCameras[0].omnityPerspectiveMatrix.near || farClipPlane != anOmnity.finalPassCameras[0].omnityPerspectiveMatrix.far) {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public bool ClipPlanesMisMatch(Omnity anOmnity) {
        if (targetCamera != null) {
            return ClipPlanesMisMatch(anOmnity, targetCamera.nearClipPlane, targetCamera.farClipPlane);
        } else {
            return false;
        }
    }

    internal static void RegisterTargetCamera(Camera _targetCamera) {
        Get().targetCamera = _targetCamera;
    }

    public static OmniCameraHelper Get() {
        return singleton;
    }
}

#if USE_OMNITYDLLSOURCE
}
#endif
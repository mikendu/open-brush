using System.Xml.XPath;
using UnityEngine;

public class StereoCombinerManager : OmnityTabbedFeature {
    static public OmnityPluginsIDs _myOmnityPluginsID = OmnityPluginsIDs.StereoCombiner;
    override public OmnityPluginsIDs myOmnityPluginsID { get { return _myOmnityPluginsID; } }

    static public StereoCombinerManager OmniCreateSingleton(GameObject go) {
        return GetSingleton<StereoCombinerManager>(ref singleton, go);
    }

    static private StereoCombinerManager singleton = null;

    static public StereoCombinerManager Get() {
        return singleton;
    }

    private void Awake() {
        if (VerifySingleton<StereoCombinerManager>(ref singleton)) {
            if (stereoCombinerShader == null) {
                stereoCombinerShader = Shader.Find("Elumenati/StereoCombiner");
                if (stereoCombinerShader == null) {
                    Debug.LogError("Couldn't connect stereo combiner shader");//Please make sure the shader is located in a resource folder.
                }
            }
            Omnity.onReloadEndCallbackPriority.AddLoaderFunction(PriorityEventHandler.Ordering.StereoCombinerManager, CoroutineLoader);
            Omnity.onSaveCompleteCallback += Save;
        }
    }

    private static int combinerLayer {
        get {
            return 31;
        }
    }

    private static string combinerParentName {
        get {
            return "@@StereoCombiner";
        }
    }

    public Camera GetCombinerCamera() {
        GameObject p = FindOrGetCombinerParent(Omnity.anOmnity);
        if (p == null || c == null) {
            Debug.LogError("Error Stereo Combiner Camera Missing");
        }
        return c;
    }

    private Camera c;

    static public float? _CustomOthroSize = null;

    static public float CustomOthroSize {
        get {
            if (
                Omnity.anOmnity.pluginIDs.Contains((int)OmnityPluginsIDs.SDMCalibration)
                && _CustomOthroSize != null
                && OmnitySDMManager.Get().stereo) {
                return _CustomOthroSize.GetValueOrDefault();
            } else {
                return Omnity.anOmnity.windowInfo.fullscreenInfo.goalWindowPosAndRes.height * .5f;
            }
        }
        set {
            _CustomOthroSize = value;
            if (Omnity.anOmnity.pluginIDs.Contains((int)OmnityPluginsIDs.SDMCalibration)
          && _CustomOthroSize != null
          && OmnitySDMManager.Get().stereo) {
                if (Get() != null) {
                    Get().GetCombinerCamera().orthographicSize = value;
                }
            }
        }
    }

    private GameObject FindOrGetCombinerParent(Omnity anOmnity) {
        GameObject combiner = GameObject.Find(combinerParentName);
        if (combiner == null) {
            combiner = new GameObject(combinerParentName);
            combiner.transform.localPosition = new Vector3(1024, 1024, 1024);

            GameObject ccCamera = new GameObject("StereoCombinerCamera");
            ccCamera.transform.parent = combiner.transform;
            ccCamera.transform.localPosition = Vector3.zero;

            c = ccCamera.AddComponent<Camera>();
            c.orthographic = true;
            c.nearClipPlane = -1;
            c.farClipPlane = 1;
            c.depth = cameraDepthOmnityFinalPass;

            c.orthographicSize = CustomOthroSize;
            //c.orthographicSize = anOmnity.windowInfo.fullscreenInfo.goalWindowPosAndRes.width * .5f;

            c.cullingMask = 1 << 31;
            c.clearFlags = CameraClearFlags.Color;
            c.backgroundColor = Color.black;
            combiner.layer = 31;

            combiner.transform.parent = anOmnity.omnityTransform;
        } else {
        }
        return combiner;
    }

    public void LinkCombiners(Omnity anOmnity) {
        foreach (LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroupStereo> group in linkedFinalPassCameraGroupsPM) {
            ChangedStereo(anOmnity, group, true);
        }
    }

    static public StereoEyeMode eyeMode = StereoEyeMode.NORMAL;

    public enum StereoEyeMode {
        NORMAL = 0,
        SWAPPED = 1,
        MONO = 2,
    }

    private void SetTransform(Transform t, Omnity anOmnity, Rect normalizedRec) {
        //        Debug.LogWarning("Set transform " + normalizedRec.ToString());
        float w = normalizedRec.width * anOmnity.windowInfo.fullscreenInfo.goalWindowPosAndRes.width;
        float h = normalizedRec.height * anOmnity.windowInfo.fullscreenInfo.goalWindowPosAndRes.height;
        float x = normalizedRec.x * anOmnity.windowInfo.fullscreenInfo.goalWindowPosAndRes.width;
        float y = (normalizedRec.y) * anOmnity.windowInfo.fullscreenInfo.goalWindowPosAndRes.height;
        x += w / 2.0f;
        y += h / 2.0f;
        float offsetX = anOmnity.windowInfo.fullscreenInfo.goalWindowPosAndRes.width / 2.0f;
        float offsetY = anOmnity.windowInfo.fullscreenInfo.goalWindowPosAndRes.height / 2.0f;
        t.localPosition = new Vector3(x - offsetX, y - offsetY, 0);
        t.localScale = new Vector3(w * .1f, 1 * .1f, h * .1f);
        t.localEulerAngles = new Vector3(90, 180, 0);

        //        Debug.LogWarning("t.localScale " + t.localScale);
    }

    public void ChangedStereo(Omnity anOmnity, LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroupStereo> newgroup, bool removeIfFalse) {
        if (newgroup.extraSettings.combinerPlane == null) {
            newgroup.extraSettings.combinerPlane = GameObject.Find(combinerParentName + "/" + newgroup.name);
        }
        if (!removeIfFalse) {
            if (newgroup.extraSettings.combinerPlane != null) {
                GameObject.Destroy(newgroup.extraSettings.combinerPlane);
            }
            return;
        }

        //        newgroup.linked
        if (newgroup.extraSettings.m == null) {
            newgroup.extraSettings.m = new Material(stereoCombinerShader);
        }
        Material m = newgroup.extraSettings.m;

        if (newgroup.extraSettings.combinerPlane == null) {
            newgroup.extraSettings.combinerPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            GameObject.Destroy(newgroup.extraSettings.combinerPlane.GetComponent<Collider>());
            newgroup.extraSettings.combinerPlane.name = newgroup.name;
            newgroup.extraSettings.combinerPlane.layer = combinerLayer;
            newgroup.extraSettings.combinerPlane.transform.parent = FindOrGetCombinerParent(anOmnity).transform;
        }

        if (newgroup.linked.Count == 0) {
            Debug.LogError("Group " + newgroup.name + " needs two cameras (with renderTextureSettings.enabled == 2) for stereo ");
        } else if (newgroup.linked.Count == 1 || eyeMode == StereoEyeMode.MONO) { // Mono
            if (eyeMode == StereoEyeMode.NORMAL || eyeMode == StereoEyeMode.SWAPPED) {
                Debug.LogError("Group " + newgroup.name + " needs two cameras (with renderTextureSettings.enabled == 2) for stereo ");
            }
            m.SetTexture("_Left", newgroup.linked[0].renderTextureSettings.rt);
            m.SetTexture("_Right", newgroup.linked[0].renderTextureSettings.rt);
        } else {
            if (newgroup.linked[1].renderTextureSettings.enabled == false) {
                Debug.LogError("Render texture is not enabled on camera " + newgroup.linked[1].name);
            }
            if (newgroup.linked[0].renderTextureSettings.enabled == false) {
                Debug.LogError("Render texture is not enabled on camera " + newgroup.linked[0].name);
            }
            if (eyeMode == StereoEyeMode.NORMAL) {
                m.SetTexture("_Left", newgroup.linked[0].renderTextureSettings.rt);
                m.SetTexture("_Right", newgroup.linked[1].renderTextureSettings.rt);
            } else {
                m.SetTexture("_Left", newgroup.linked[1].renderTextureSettings.rt);
                m.SetTexture("_Right", newgroup.linked[0].renderTextureSettings.rt);
            }
        }
        for (int i = 0; i < newgroup.linked.Count; i++) {
            var fpc = newgroup.linked[i];
            if (fpc.myCamera != null) {
                //   Debug.LogWarning("fpc.myCamera.targetTexture.depth " + fpc.myCamera.targetTexture.depth);
                //   Debug.LogWarning("Setting zero depth");
                if (fpc.myCamera.targetTexture.depth != 0) {
                    fpc.myCamera.targetTexture.depth = 0;
                    Debug.LogWarning("fpc.myCamera.targetTexture.depth != 0");
                } else {
                }
            } else {
                Debug.LogError("fpc.myCamera == null, problem with disabling depth texture.");
            }
        }

        SetTransform(newgroup.extraSettings.combinerPlane.transform, anOmnity, newgroup.extraSettings.normalizedViewportRect);
        m.SetFloat("_ViewportOffset", newgroup.extraSettings.offset);
        newgroup.extraSettings.combinerPlane.GetComponent<Renderer>().sharedMaterial = m;
        newgroup.extraSettings.needsRefresh = false;
    }

    private static int cameraDepthOmnityFinalPass = 2;
    private static Shader stereoCombinerShader = null;

    public override void Unload() {
        base.Unload();
        linkedFinalPassCameraGroupsPM.Clear();
    }

    override public string BaseName {
        get {
            return "StereoCombinerSetting";
        }
    }

    public override void ReadXMLDelegate(XPathNavigator nav) {
        linkedFinalPassCameraGroupsPM = LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroupStereo>.ReadXMLAll(nav, Omnity.anOmnity);
    }

    /// <param name="xmlWriter">The XML writer.</param>
    override public void WriteXMLDelegate(System.Xml.XmlTextWriter xmlWriter) {
        foreach (var group in linkedFinalPassCameraGroupsPM) {
            group.WriteXML(xmlWriter);
        }
    }

    override public System.Collections.IEnumerator PostLoad() {
        LinkCombiners(Omnity.anOmnity);
        yield break;
    }

    public System.Collections.Generic.List<LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroupStereo>> linkedFinalPassCameraGroupsPM = new System.Collections.Generic.List<LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroupStereo>>();

    public override void MyGuiCallback(Omnity anOmnity) {
        if (!anOmnity.PluginEnabled(OmnityPluginsIDs.StereoCombiner)) {
            return;
        }

        GUILayout.BeginHorizontal();
        GUILayout.Label("Stereo Combiner Configuration requirements, this must be run in DX11 and Unity Full Screen.");
        GUILayout.Label("It works best if you set Omnity.anOmnity.windowInfo.fullscreenInfo to be Application Default and Omnity.anOmnity.windowInfo.fullscreenInfo.applyOnLoad == false");
        OmnityHelperFunctions.BR();
        GUILayout.Label("Stereo Displays");
        OmnityHelperFunctions.BR();
        LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroupStereo>.DrawGUIAll(anOmnity, linkedFinalPassCameraGroupsPM);
        OmnityHelperFunctions.BR();
        if (GUILayout.Button("Add Display", GUILayout.Width(300))) {
            LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroupStereo> newgroup = new LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroupStereo>();
            linkedFinalPassCameraGroupsPM.Add(newgroup);
            StereoCombinerManager.Get().ChangedStereo(anOmnity, newgroup, true);
        }
        GUILayout.EndHorizontal();
        SaveLoadGUIButtons(anOmnity);

        for (int i = 0; i < linkedFinalPassCameraGroupsPM.Count; i++) {
            if (linkedFinalPassCameraGroupsPM[i].extraSettings.needsRefresh) {
                StereoCombinerManager.Get().ChangedStereo(anOmnity, linkedFinalPassCameraGroupsPM[i], true);
            }
        }
    }
}

[System.Serializable]
public class LinkedFinalPassCameraGroupStereo : LinkedFinalPassCameraGroup_ExtraSettingsInterface {
    public Rect normalizedViewportRect = new Rect(0, 0, 1, 1);

    public float offset = 0;

    [System.NonSerialized]
    public Material m;

    public void WriteExtraSettings(System.Xml.XmlTextWriter xmlWriter) {
        xmlWriter.WriteElementString("normalizedViewportRect", normalizedViewportRect.ToString("R"));
        xmlWriter.WriteElementString("offset", offset.ToString("R"));
    }

    public void ReadExtraSettings(System.Xml.XPath.XPathNavigator currentGroup) {
        normalizedViewportRect = OmnityHelperFunctions.ReadElementRectDefault(currentGroup, ".//normalizedViewportRect", normalizedViewportRect);
        offset = OmnityHelperFunctions.ReadElementFloatDefault(currentGroup, ".//offset", offset);
    }

    public void DrawGUIExtra(Omnity anOmnity) {
        normalizedViewportRect = OmnityHelperFunctions.RectInputReset("PositionOnDisplay", normalizedViewportRect, new Rect(0, 0, 1, 1), ref anOmnity.myOmnityGUI.myOmnity.usePixel, ref viewportRectMouseState, anOmnity);
        if (OmnityHelperFunctions.FloatInputResetWasChanged("offset", ref offset, 0)) {
            needsRefresh = true;
        }
        GUILayout.Label("Hint: set offset to zero or viewportrect.x");
    }

    public bool needsRefresh = false;

    private int? viewportRectMouseState = null;

    public void RemoveOrKeepGroupCallBack<T>(Omnity anOmnity, LinkedFinalPassCameraGroupBase<T> _linkedFinalPassCameraGroupBase, bool keepThis, bool linkSizeChanged) where T : LinkedFinalPassCameraGroup_ExtraSettingsInterface, new() {
        LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroupStereo> linkedFinalPassCameraGroupBase = _linkedFinalPassCameraGroupBase as LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroupStereo>;
        if (keepThis) {
            StereoCombinerManager.Get().ChangedStereo(anOmnity, linkedFinalPassCameraGroupBase, true);
        } else {
            StereoCombinerManager.Get().ChangedStereo(anOmnity, linkedFinalPassCameraGroupBase, false);
        }
    }

    [System.NonSerialized]
    public GameObject combinerPlane = null;
}

#if USE_OMNITYDLLSOURCE
}
#endif
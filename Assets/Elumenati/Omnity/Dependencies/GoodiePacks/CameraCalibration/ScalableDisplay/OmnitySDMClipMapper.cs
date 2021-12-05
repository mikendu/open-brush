using OmnityMono;
using OmnityMono.OmnitySDMNamespace;
using System.Xml.XPath;
using UnityEngine;

public class OmnitySDMClipMapper : OmnityTabbedFeature {
    static public OmnityPluginsIDs _myOmnityPluginsID = OmnityPluginsIDs.ClipMapperSDM;
    override public OmnityPluginsIDs myOmnityPluginsID { get { return _myOmnityPluginsID; } }

    static public OmnitySDMClipMapper OmniCreateSingleton(GameObject go) {
        return GetSingleton<OmnitySDMClipMapper>(ref singleton, go);
    }

    static public OmnitySDMClipMapper singleton = null;

    private void Awake() {
        if (VerifySingleton<OmnitySDMClipMapper>(ref singleton)) {
            Omnity.onReloadEndCallbackPriority.AddLoaderFunction(PriorityEventHandler.Ordering.SDMClipMapper, CoroutineLoader);
            Omnity.onReloadStartCallback += (anOmnity) => {
                this.enabled = false;
                mediaSubclips.Clear();
            };
            onNewTextureCallback += MyOnNewTextureCallback;
            Omnity.onSaveCompleteCallback += Save;
        }
    }

    public System.Action<Texture> onNewTextureCallback = delegate {
    };

    public System.Func<Texture> getLatestTextureCallback = () => {
        Debug.LogError("Replace with callback Playlist.tex");
        return null;
    };

    public System.Func<bool> getLatestFlipY = () => {
        Debug.LogError("Replace with callback Playlist.tex");
        return false;
    };

    public static OmnitySDMClipMapper Get() {
        return singleton;
    }

    public override void Unload() {
        base.Unload();
        screenShapes.Clear();
    }

    override public System.Collections.IEnumerator PostLoad() {
        ReconnectScreens(Omnity.anOmnity, getLatestTextureCallback());
        yield break;
    }

    private void MyOnNewTextureCallback(Texture tex) {
        Omnity anOmnity = Omnity.anOmnity;

        if (!anOmnity.PluginEnabled(OmnityPluginsIDs.ClipMapperSDM)) {
            this.enabled = false;
            return;
        } else {
            this.enabled = true;
            ReconnectTextures(Omnity.anOmnity, tex);
        }
    }

    public const FilterMode defaultFilterMode = FilterMode.Point;
    public const TextureWrapMode defaultTextureWrapMode = TextureWrapMode.Clamp;
    public FilterMode textureFilterMode = defaultFilterMode;
    public TextureWrapMode textureWrapMode = defaultTextureWrapMode;
    private System.Collections.Generic.List<ScreenShape> screenShapes = new System.Collections.Generic.List<ScreenShape>();

    public void ReconnectScreens(Omnity anOmnity, Texture tt) {
        if (!anOmnity.PluginEnabled(OmnityPluginsIDs.ClipMapperSDM)) {
            return;
        }
        if (tt != null) {
            tt.filterMode = textureFilterMode;
            tt.wrapMode = textureWrapMode;
        }

        foreach (var ss in screenShapes) {
            ss.startEnabled = false;
            if (ss.renderer != null) {
                ss.renderer.enabled = false;
            }
        }
        int ssIndex = 0;
        foreach (var subclip in mediaSubclips) {
            if (ssIndex >= screenShapes.Count) {
                screenShapes.Add(new ScreenShape {
                    transformInfo = myTransformInfo
                });
                screenShapes[ssIndex].finalPassShaderType = FinalPassShaderType.EdgeBlendVertexColor;

                screenShapes[ssIndex].sphereParams.thetaStart = equirectangularRaycastThetaPhi.x;
                screenShapes[ssIndex].sphereParams.thetaEnd = equirectangularRaycastThetaPhi.y;

                screenShapes[ssIndex].sphereParams.phiStart = equirectangularRaycastThetaPhi.z;
                screenShapes[ssIndex].sphereParams.phiEnd = equirectangularRaycastThetaPhi.w;

                screenShapes[ssIndex].startEnabled = true;
                screenShapes[ssIndex].SpawnScreenShape(anOmnity);
                screenShapes[ssIndex].trans.gameObject.name = "SDM Screen " + ssIndex;
            } else {
                screenShapes[ssIndex].sphereParams.thetaStart = equirectangularRaycastThetaPhi.x;
                screenShapes[ssIndex].sphereParams.thetaEnd = equirectangularRaycastThetaPhi.y;

                screenShapes[ssIndex].sphereParams.phiStart = equirectangularRaycastThetaPhi.z;
                screenShapes[ssIndex].sphereParams.phiEnd = equirectangularRaycastThetaPhi.w;
                screenShapes[ssIndex].sphereParams.flagRegenerateScreen = true;

                if (screenShapes[ssIndex].trans == null) {
                    screenShapes[ssIndex].SpawnScreenShape(anOmnity);
                } else {
                    screenShapes[ssIndex].SpawnOrUpdateScreen(screenShapes[ssIndex].trans.gameObject, false);
                }
            }

            screenShapes[ssIndex].renderer.sharedMaterial.mainTexture = tt;
            screenShapes[ssIndex].renderer.enabled = true;
            LinkGroupScreenShapes_SDMMediaClip ssdmSubclip = (LinkGroupScreenShapes_SDMMediaClip)subclip;
            screenShapes[ssIndex].renderer.sharedMaterial.mainTextureOffset = new Vector2(ssdmSubclip.sourceClip.x, ssdmSubclip.sourceClip.y);
            screenShapes[ssIndex].renderer.sharedMaterial.mainTextureScale = new Vector2(ssdmSubclip.sourceClip.width, ssdmSubclip.sourceClip.height);

            screenShapes[ssIndex].trans.gameObject.layer = ssdmSubclip.layer;

            ssIndex++;
        }

        foreach (var c in mediaSubclips) {
            c.Apply_var(anOmnity);
        }
        RefreshShadersShapes(anOmnity);
    }

    public void ReconnectTextures(Omnity anOmnity, Texture tt) {
        if (!anOmnity.PluginEnabled(OmnityPluginsIDs.ClipMapperSDM)) {
            return;
        }
        if (tt != null) {
            tt.filterMode = textureFilterMode;
            tt.wrapMode = textureWrapMode;
        }

        for (int i = 0; i < screenShapes.Count; i++) {
            var ss = screenShapes[i];
            ss.startEnabled = false;
            if (ss.renderer != null) {
                ss.renderer.enabled = false;
            }
        }

        for (int ssIndex = 0; ssIndex < mediaSubclips.Count; ssIndex++) {
            if (ssIndex >= screenShapes.Count) {
                Debug.LogError("Error, too many subclips for number of screen shape... probably you have need to remove one subclip or add a second screen shape for stereo");
                continue;
            }
            var subclip = mediaSubclips[ssIndex];

            if (screenShapes[ssIndex].renderer == null) {
                Debug.LogError("MAKE SURE SCREENSHAPE IS FINISHED LOADING");
            } else {
                screenShapes[ssIndex].renderer.sharedMaterial.mainTexture = tt;
                screenShapes[ssIndex].renderer.enabled = true;
                LinkGroupScreenShapes_SDMMediaClip ssdmSubclip = (LinkGroupScreenShapes_SDMMediaClip)subclip;
                screenShapes[ssIndex].renderer.sharedMaterial.mainTextureOffset = new Vector2(ssdmSubclip.sourceClip.x, ssdmSubclip.sourceClip.y);
                screenShapes[ssIndex].renderer.sharedMaterial.mainTextureScale = new Vector2(ssdmSubclip.sourceClip.width, ssdmSubclip.sourceClip.height);

                screenShapes[ssIndex].trans.gameObject.layer = ssdmSubclip.layer;
            }
        }

        for (int ssIndex = 0; ssIndex < mediaSubclips.Count; ssIndex++) {
            if (ssIndex >= screenShapes.Count) {
                Debug.LogError("Error, too many subclips for number of screen shape... probably you have need to remove one subclip or add a second screen shape for stereo");
                continue;
            }
            var subclip = mediaSubclips[ssIndex];
            subclip.Apply_var(anOmnity);
        }
        RefreshShaders(anOmnity);
    }

    public System.Collections.Generic.List<LinkedScreenShapeGroupBase> mediaSubclips = new System.Collections.Generic.List<LinkedScreenShapeGroupBase>();

    override public string BaseName {
        get {
            return "SDMClipMapper";
        }
    }

    public override void ReadXMLDelegate(XPathNavigator nav) {
        mediaSubclips = LinkGroupScreenShapes_SDMMediaClip.ReadXMLAll<LinkGroupScreenShapes_SDMMediaClip>(nav, Omnity.anOmnity);

        System.Xml.XPath.XPathNodeIterator mySSIterator = nav.Select("(.//shaderKeyword)");
        listOfActiveKeyWords.Clear();
        while (mySSIterator.MoveNext()) {
            string s = mySSIterator.Current.InnerXml.Trim();
            listOfActiveKeyWords.Add(s);
        }
        myTransformInfo.position = OmnityHelperFunctions.ReadElementVector3Default(nav, "(.//position)", Vector3.zero);
        myTransformInfo.localEulerAngles = OmnityHelperFunctions.ReadElementVector3Default(nav, "(.//localEulerAngles)", Vector3.zero);

        equirectangularRaycastThetaPhi = OmnityHelperFunctions.ReadElementVector4Default(nav, "(.//equirectangularRaycastThetaPhi)", new Vector4(-90, 90, 45, -45));

        textureFilterMode = OmnityHelperFunctions.ReadElementEnumDefault<FilterMode>(nav, "(.//textureFilterMode)", textureFilterMode);
        textureWrapMode = OmnityHelperFunctions.ReadElementEnumDefault<TextureWrapMode>(nav, "(.//textureWrapMode)", textureWrapMode);
        RefreshShadersShapes(Omnity.anOmnity);
    }

    /// <param name="xmlWriter">The XML writer.</param>
    override public void WriteXMLDelegate(System.Xml.XmlTextWriter xmlWriter) {
        foreach (var group in mediaSubclips) {
            group.WriteXML(xmlWriter);
        }

        foreach (var keyword in listOfActiveKeyWords) {
            xmlWriter.WriteElementString("shaderKeyword", keyword);
        }
        xmlWriter.WriteElementString("equirectangularRaycastThetaPhi", equirectangularRaycastThetaPhi.ToString());
        xmlWriter.WriteElementString("textureFilterMode", textureFilterMode.ToString());
        xmlWriter.WriteElementString("textureWrapMode", textureWrapMode.ToString());

        xmlWriter.WriteElementString("position", myTransformInfo.position.ToString("R"));
        xmlWriter.WriteElementString("localEulerAngles", myTransformInfo.localEulerAngles.ToString("R"));
    }

    #region EQUIRECTANGULARRAYCAST

    public Vector4 equirectangularRaycastThetaPhi = new Vector4(-90, 90, 45, -45);

    #endregion EQUIRECTANGULARRAYCAST

    private System.Collections.Generic.List<System.Collections.Generic.List<string>> listOfPotentialKeywords = new System.Collections.Generic.List<System.Collections.Generic.List<string>>{
   new System.Collections.Generic.List<string> {"USE_UV", "USE_EQUIRECTANGULAR_RAYCAST2", "USE_EQUIRECTANGULAR_RAYCAST", "USE_PROJECTIVEPERSPECTIVEMAPPING"}};

    public System.Collections.Generic.List<string> listOfActiveKeyWords = new System.Collections.Generic.List<string>();

    private string expanderHash = OmnityHelperFunctions.OmnityUniqueHash.uniqueHash;

    public override void MyGuiCallback(Omnity anOmnity) {
        if (!anOmnity.PluginEnabled(OmnityPluginsIDs.ClipMapperSDM)) {
            return;
        }
        bool changed = false;

        OmnityHelperFunctionsGUI.VerticalLayout(GUI.skin.box, () => {
            OmnityHelperFunctionsGUI.HorizontalLayout(GUI.skin.box, () => {
                if (OmnityHelperFunctions.BeginExpanderSimple(expanderHash + "key", "Keywords", "Keywords")) {
                    foreach (var keywordSet in listOfPotentialKeywords) {
                        foreach (var keyword in keywordSet) {
                            if (listOfActiveKeyWords.Contains(keyword)) {
                                if (GUILayout.Button("[" + keyword + "]")) {
                                    listOfActiveKeyWords.Remove(keyword);
                                    changed = true;
                                }
                            } else {
                                if (GUILayout.Button(keyword)) {
                                    foreach (var keywordSet2 in listOfPotentialKeywords) {
                                        foreach (var keyword2 in keywordSet2) {
                                            if (keyword == keyword2) {
                                                foreach (var keyword3 in keywordSet2) {
                                                    listOfActiveKeyWords.Remove(keyword3);
                                                }
                                            }
                                        }
                                    }
                                    listOfActiveKeyWords.Add(keyword);
                                    changed = true;
                                }
                            }
                            OmnityHelperFunctions.BR();
                        }
                        OmnityHelperFunctions.BR();
                    }
                }
                OmnityHelperFunctions.EndExpanderSimple();
            });

            textureFilterMode = OmnityHelperFunctions.EnumInputReset<FilterMode>(expanderHash + "filter", "textureFilterMode", textureFilterMode, defaultFilterMode, 1);
            textureWrapMode = OmnityHelperFunctions.EnumInputReset<TextureWrapMode>(expanderHash + "textureWrapMode", "textureWrapMode", textureWrapMode, defaultTextureWrapMode, 1);
            changed |= OmnityHelperFunctions.Vector3InputResetWasChanged("position", ref myTransformInfo.position, new Vector3(0, 0, 0));
            changed |= OmnityHelperFunctions.Vector3InputResetWasChanged("localEulerAngles", ref myTransformInfo.localEulerAngles, new Vector3(0, 0, 0));
            //        Vector4 equirectangularRaycastThetaPhi_new = OmnityHelperFunctions.Vector4InputReset("equirectangularRaycastThetaPhi", equirectangularRaycastThetaPhi,
            //           new Vector4(-90, 90, 45, -45));

            Vector4 equirectangularRaycastThetaPhi_old = equirectangularRaycastThetaPhi;
            OmnityHelperFunctions.DrawFOVWidget(expanderHash + "screen", "Mapping", ref equirectangularRaycastThetaPhi.x,
                ref equirectangularRaycastThetaPhi.y,
                ref equirectangularRaycastThetaPhi.z,
                ref equirectangularRaycastThetaPhi.w,
                new Vector3(0, -180, -90),
                new Vector3(0, 180, 90),
                new Vector3(0, 90, 45),
                new Vector3(0, -90, -45));

            if (equirectangularRaycastThetaPhi_old != equirectangularRaycastThetaPhi) {
                changed = true;
            }

            if (changed) {
                RefreshShadersShapes(anOmnity);
            }

            int count = mediaSubclips.Count;
            LinkGroupScreenShapes_SDMMediaClip.DrawGUIAll<LinkGroupScreenShapes_SDMMediaClip>(anOmnity, mediaSubclips);
            if (mediaSubclips.Count > count) {
                LinkGroupScreenShapes_SDMMediaClip msc = (LinkGroupScreenShapes_SDMMediaClip)mediaSubclips[mediaSubclips.Count - 1];
                msc.layer = count;
                msc.Apply_var(anOmnity);
            }

            SaveLoadGUIButtons(anOmnity);
        });
    }

    private void RefreshShaders(Omnity anOmnity) {
        for (int ssIndex = 0; ssIndex < mediaSubclips.Count; ssIndex++) {
            if (ssIndex < screenShapes.Count) {
                SDMClipMapperExtensions.ActivateRenderer(screenShapes[ssIndex].renderer, OmnitySDMClipMapper.Get().getLatestFlipY(), ((LinkGroupScreenShapes_SDMMediaClip)mediaSubclips[ssIndex]).sourceClip);
            }
        }
    }

    private void RefreshShadersShapes(Omnity anOmnity) {
        int ssIndex = 0;
        foreach (var subclip in mediaSubclips) {
            if (ssIndex < screenShapes.Count) {
                screenShapes[ssIndex].sphereParams.thetaStart = equirectangularRaycastThetaPhi.x;
                screenShapes[ssIndex].sphereParams.thetaEnd = equirectangularRaycastThetaPhi.y;

                screenShapes[ssIndex].sphereParams.phiStart = -equirectangularRaycastThetaPhi.z;
                screenShapes[ssIndex].sphereParams.phiEnd = -equirectangularRaycastThetaPhi.w;
                screenShapes[ssIndex].sphereParams.flagRegenerateScreen = true;
                if (screenShapes[ssIndex].trans == null) {
                    screenShapes[ssIndex].SpawnScreenShape(anOmnity);
                } else {
                    screenShapes[ssIndex].SpawnOrUpdateScreen(screenShapes[ssIndex].trans.gameObject, false);
                }

                LinkGroupScreenShapes_SDMMediaClip subclipssdm = (LinkGroupScreenShapes_SDMMediaClip)subclip;
                if (OmnitySDMClipMapper.Get().getLatestFlipY()) {
                    screenShapes[ssIndex].renderer.sharedMaterial.mainTextureScale = new Vector2(subclipssdm.sourceClip.width, -subclipssdm.sourceClip.height);
                    screenShapes[ssIndex].renderer.sharedMaterial.mainTextureOffset = new Vector2(subclipssdm.sourceClip.x, 1 - subclipssdm.sourceClip.y);
                } else {
                    screenShapes[ssIndex].renderer.sharedMaterial.mainTextureScale = new Vector2(subclipssdm.sourceClip.width, subclipssdm.sourceClip.height);
                    screenShapes[ssIndex].renderer.sharedMaterial.mainTextureOffset = new Vector2(subclipssdm.sourceClip.x, subclipssdm.sourceClip.y);
                }
            }
            if (ssIndex < screenShapes.Count) {
                screenShapes[ssIndex].trans.gameObject.transform.localEulerAngles = screenShapes[ssIndex].transformInfo.localEulerAngles;
                screenShapes[ssIndex].trans.gameObject.transform.localPosition = screenShapes[ssIndex].transformInfo.position;
                screenShapes[ssIndex].trans.gameObject.transform.localScale = Vector3.one;
            }
            ssIndex++;
        }
    }

    public OmnityTransformInfo myTransformInfo = new OmnityTransformInfo();
}

[System.Serializable]
public class LinkGroupScreenShapes_SDMMediaClip : LinkedScreenShapeGroupBase {
    public Rect sourceClip = new Rect(0, 0, 1, 1);
    public int layer = 0;

    public override bool showScreenShapes {
        get {
            return false;
        }
    }

    public ScreenShape proxy = null;

    override public void WriteXML_var(System.Xml.XmlTextWriter xmlWriter) {
        xmlWriter.WriteStartElement("LinkGroupScreenShapes_SDMMediaClip");
        xmlWriter.WriteElementString("name", name);
        xmlWriter.WriteElementString("sourceClip", sourceClip.ToString("R"));
        xmlWriter.WriteElementString("layer", layer.ToString());
        xmlWriter.WriteEndElement();
    }

    override public void ReadXML_var(System.Xml.XPath.XPathNavigator currentGroup, LinkedScreenShapeGroupBase _newGroup) {
        LinkGroupScreenShapes_SDMMediaClip newGroup = (LinkGroupScreenShapes_SDMMediaClip)_newGroup;
        newGroup.sourceClip = OmnityHelperFunctions.ReadElementRectDefault(currentGroup, ".//sourceClip", newGroup.sourceClip);
        newGroup.layer = OmnityHelperFunctions.ReadElementIntDefault(currentGroup, ".//layer", newGroup.layer);
    }

    private int? viewportRectMouseState = null;
    private static bool usePixel = false;

    override public void DrawGUI_variable(Omnity anOmnity) {

        #region VaribleSelection

        if (OmnityHelperFunctions.BeginExpanderSimple(expanderHash + "LV", "sourceClip", "sourceClip")) {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            Texture t = OmnitySDMClipMapper.singleton.getLatestTextureCallback();
            if (t != null) {
                Rect newSourceClipNormalized = OmnityHelperFunctions.RectInputReset("sourceClip", sourceClip, new Rect(0, 0, 1, 1), ref usePixel, ref viewportRectMouseState, anOmnity, t.width, t.height);
                if (newSourceClipNormalized != sourceClip || viewportRectMouseState != null) {
                    sourceClip = newSourceClipNormalized;
                    Apply_var(anOmnity);
                }
            } else {
                GUILayout.Label("Warning: SDMClipMapper.singleton.getLatestTextureCallback() returned null");
            }

            OmnityHelperFunctions.BR();
            OmnityHelperFunctions.BR();
            if (OmnityHelperFunctions.IntInputResetWasChanged("layer", ref layer, 0)) {
                if (proxy != null) {
                    proxy.layer = layer;
                    if (proxy.trans != null) {
                        proxy.trans.gameObject.layer = layer;
                    }
                }
            }
            GUILayout.EndHorizontal();
        }
        OmnityHelperFunctions.EndExpanderSimple();

        #endregion VaribleSelection
    }

    override public void Apply_var(Omnity anOmnity) {
    }
}
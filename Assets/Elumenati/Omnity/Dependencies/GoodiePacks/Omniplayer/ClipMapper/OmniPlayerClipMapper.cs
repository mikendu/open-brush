using System.Xml.XPath;
using UnityEngine;

public class OmniPlayerClipMapper : OmnityTabbedFeature {
    static public OmnityPluginsIDs _myOmnityPluginsID = OmnityPluginsIDs.OmniPlayerClipMapper;
    override public OmnityPluginsIDs myOmnityPluginsID { get { return _myOmnityPluginsID; } }

    static public OmniPlayerClipMapper OmniCreateSingleton(GameObject go) {
        return GetSingleton<OmniPlayerClipMapper>(ref singleton, go);
    }

    static public OmniPlayerClipMapper singleton = null;

    private void Awake() {
        if (VerifySingleton<OmniPlayerClipMapper>(ref singleton)) {
            Omnity.onReloadEndCallbackPriority.AddLoaderFunction(PriorityEventHandler.Ordering.Order_OmniplayerClipmapper, CoroutineLoader);
            onNewTextureCallback += (Texture tex) => {
                Omnity anOmnity = gameObject.GetComponent<Omnity>();
                if (!anOmnity.PluginEnabled(OmnityPluginsIDs.OmniPlayerClipMapper)) {
                    this.enabled = false;
                    return;
                } else {
                    this.enabled = true;
                }
                ReconnectTextures(Omnity.anOmnity, tex);
            };
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

    public static OmniPlayerClipMapper Get() {
        return singleton;
    }

    public const FilterMode defaultFilterMode = FilterMode.Point;
    public const TextureWrapMode defaultTextureWrapMode = TextureWrapMode.Clamp;

    public FilterMode textureFilterMode = defaultFilterMode;
    public TextureWrapMode textureWrapMode = defaultTextureWrapMode;

    private void ReconnectTextures(Omnity anOmnity, Texture tt) {
        if (!anOmnity.PluginEnabled(OmnityPluginsIDs.OmniPlayerClipMapper)) {
            return;
        }
        if (tt != null) {
            tt.filterMode = textureFilterMode;
            tt.wrapMode = textureWrapMode;
        }
        for (int i = 0; i < anOmnity.screenShapes.Length; i++) {
            ScreenShape s = anOmnity.screenShapes[i];
            if (s.startEnabled) {
                if (s.renderer) {
                    s.renderer.sharedMaterial.mainTexture = tt;
                }
            }
        }
        for (int ic = 0; ic < mediaSubClips.Count; ic++) {
            mediaSubClips[ic].Apply_var(anOmnity);
        }
    }

    override public System.Collections.IEnumerator PostLoad() {
        ReconnectTextures(Omnity.anOmnity, getLatestTextureCallback());
        yield break;
    }

    public System.Collections.Generic.List<LinkedScreenShapeGroupBase> mediaSubClips = new System.Collections.Generic.List<LinkedScreenShapeGroupBase>();

    override public string BaseName {
        get {
            return "OmniPlayerClipMapper";
        }
    }

    public override void ReadXMLDelegate(XPathNavigator nav) {
        mediaSubClips = LinkGroupScreenShapes_MediaClip.ReadXMLAll<LinkGroupScreenShapes_MediaClip>(nav, Omnity.anOmnity);

        System.Xml.XPath.XPathNodeIterator mySSIterator = nav.Select("(.//shaderKeyword)");
        listOfActiveKeyWords.Clear();
        while (mySSIterator.MoveNext()) {
            string s = mySSIterator.Current.InnerXml.Trim();
            listOfActiveKeyWords.Add(s);
        }

        rotateAmount = OmnityHelperFunctions.ReadElementFloatDefault(nav, "(.//rotateAmount)", 0);

        equirectangularRaycastThetaPhi = OmnityHelperFunctions.ReadElementVector4Default(nav, "(.//equirectangularRaycastThetaPhi)", new Vector4(-90, 90, 45, -45));

        textureFilterMode = OmnityHelperFunctions.ReadElementEnumDefault<FilterMode>(nav, "(.//textureFilterMode)", textureFilterMode);
        textureWrapMode = OmnityHelperFunctions.ReadElementEnumDefault<TextureWrapMode>(nav, "(.//textureWrapMode)", textureWrapMode);
        RefreshShaders();
    }

    #region EQUIRECTANGULARRAYCAST

    public Vector4 equirectangularRaycastThetaPhi = new Vector4(-90, 90, 45, -45);
    public float rotateAmount = 0;

    #endregion EQUIRECTANGULARRAYCAST

    private System.Collections.Generic.List<System.Collections.Generic.List<string>> listOfPotentialKeywords = new System.Collections.Generic.List<System.Collections.Generic.List<string>>{
   new System.Collections.Generic.List<string> {"USE_UV", "USE_EQUIRECTANGULAR_RAYCAST2", "USE_EQUIRECTANGULAR_RAYCAST", "USE_PROJECTIVEPERSPECTIVEMAPPING"}};

    public System.Collections.Generic.List<string> listOfActiveKeyWords = new System.Collections.Generic.List<string>();

    /// <param name="xmlWriter">The XML writer.</param>
    override public void WriteXMLDelegate(System.Xml.XmlTextWriter xmlWriter) {
        foreach (var group in mediaSubClips) {
            group.WriteXML(xmlWriter);
        }
        foreach (var keyword in listOfActiveKeyWords) {
            xmlWriter.WriteElementString("shaderKeyword", keyword);
        }
        xmlWriter.WriteElementString("equirectangularRaycastThetaPhi", equirectangularRaycastThetaPhi.ToString());
        xmlWriter.WriteElementString("rotateAmount", rotateAmount.ToString());
        xmlWriter.WriteElementString("textureFilterMode", textureFilterMode.ToString());
        xmlWriter.WriteElementString("textureWrapMode", textureWrapMode.ToString());
    }

    private string expanderHash = OmnityHelperFunctions.OmnityUniqueHash.uniqueHash;

    public override void MyGuiCallback(Omnity anOmnity) {
        if (!anOmnity.PluginEnabled(OmnityPluginsIDs.OmniPlayerClipMapper)) {
            return;
        }
        GUILayout.BeginHorizontal();
        bool changed = false;

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

        OmnityHelperFunctions.BR();
        textureFilterMode = OmnityHelperFunctions.EnumInputReset<FilterMode>(expanderHash + "filter", "textureFilterMode", textureFilterMode, defaultFilterMode, 1);

        OmnityHelperFunctions.BR();
        textureWrapMode = OmnityHelperFunctions.EnumInputReset<TextureWrapMode>(expanderHash + "textureWrapMode", "textureWrapMode", textureWrapMode, defaultTextureWrapMode, 1);

        float rotateAmount_new = OmnityHelperFunctions.FloatInputReset("rotateAmount",
            rotateAmount, 0);
        if (rotateAmount != rotateAmount_new) {
            rotateAmount = rotateAmount_new;
            changed = true;
        }
        OmnityHelperFunctions.BR();
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
            RefreshShaders();
        }

        OmnityHelperFunctions.BR();

        LinkGroupScreenShapes_MediaClip.DrawGUIAll<LinkGroupScreenShapes_MediaClip>(anOmnity, mediaSubClips);
        OmnityHelperFunctions.BR();
        if (GUILayout.Button("Add SubClip", GUILayout.Width(300))) {
            LinkGroupScreenShapes_MediaClip newgroup = new LinkGroupScreenShapes_MediaClip();
            mediaSubClips.Add(newgroup);
            newgroup.Apply_var(anOmnity);
        }
        GUILayout.EndHorizontal();
        SaveLoadGUIButtons(anOmnity);
    }

    private void RefreshShaders() {
        foreach (var group in mediaSubClips) {
            foreach (var ss in group.linked) {
                ss.shaderKeywordsAdditional = listOfActiveKeyWords;
                ss.SetShaderKeywords();
                if (ss.renderer) {
                    if (ss.renderer.sharedMaterial.HasProperty("equirectangularRaycastThetaPhi")) {
                        ss.renderer.sharedMaterial.SetVector("equirectangularRaycastThetaPhi", equirectangularRaycastThetaPhi);
                    }
                    if (ss.renderer.sharedMaterial.HasProperty("rotateAmount")) {
                        ss.renderer.sharedMaterial.SetFloat("rotateAmount", rotateAmount);
                    }
                }
            }
        }
    }
}

[System.Serializable]
public class LinkGroupScreenShapes_MediaClip : LinkedScreenShapeGroupBase {
    public Rect sourceClip = new Rect(0, 0, 1, 1);

    override public void WriteXML_var(System.Xml.XmlTextWriter xmlWriter) {
        xmlWriter.WriteElementString("sourceClip", sourceClip.ToString("R"));
    }

    override public void ReadXML_var(System.Xml.XPath.XPathNavigator currentGroup, LinkedScreenShapeGroupBase _newGroup) {
        LinkGroupScreenShapes_MediaClip newGroup = (LinkGroupScreenShapes_MediaClip)_newGroup;
        newGroup.sourceClip = OmnityHelperFunctions.ReadElementRectDefault(currentGroup, ".//sourceClip", newGroup.sourceClip);
    }

    private int? viewportRectMouseState = null;
    private static bool usePixel = false;

    override public void DrawGUI_variable(Omnity anOmnity) {

        #region VaribleSelection

        if (OmnityHelperFunctions.BeginExpanderSimple(expanderHash + "LV", "sourceClip", "sourceClip")) {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            Texture t = OmniPlayerClipMapper.singleton.getLatestTextureCallback();
            if (t != null) {
                Rect newSourceClipNormalized = OmnityHelperFunctions.RectInputReset("sourceClip", sourceClip, new Rect(0, 0, 1, 1), ref usePixel, ref viewportRectMouseState, anOmnity, t.width, t.height);
                if (newSourceClipNormalized != sourceClip || viewportRectMouseState != null) {
                    sourceClip = newSourceClipNormalized;
                    Apply_var(anOmnity);
                }
            } else {
                GUILayout.Label("Warning: OmniPlayerClipMapper.singleton.getLatestTextureCallback() returned null");
            }

            OmnityHelperFunctions.BR();
            GUILayout.EndHorizontal();
        }
        OmnityHelperFunctions.EndExpanderSimple();

        #endregion VaribleSelection
    }

    override public void Apply_var(Omnity anOmnity) {
        for (int i = 0; i < linked.Count; i++) {
            var v = linked[i];

            if (v.renderer == null) {
                Debug.LogWarning("Skipping");
                continue;
            }
            if (OmniPlayerClipMapper.Get().getLatestFlipY()) {
                v.renderer.sharedMaterial.mainTextureScale = new Vector2(sourceClip.width, -sourceClip.height);
                v.renderer.sharedMaterial.mainTextureOffset = new Vector2(sourceClip.x, 1 - sourceClip.y);
            } else {
                v.renderer.sharedMaterial.mainTextureScale = new Vector2(sourceClip.width, sourceClip.height);
                v.renderer.sharedMaterial.mainTextureOffset = new Vector2(sourceClip.x, sourceClip.y);
            }
        }
    }
}
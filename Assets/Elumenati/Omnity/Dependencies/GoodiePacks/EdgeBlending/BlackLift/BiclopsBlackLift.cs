using OmnityMono;
using System.Xml.XPath;
using UnityEngine;

public class OmniBiclopsBlackLiftManager : OmnityTabbedFeature {
    static public OmnityPluginsIDs _myOmnityPluginsID = OmnityPluginsIDs.OmnityBiclopsBlackLift;
    override public OmnityPluginsIDs myOmnityPluginsID { get { return _myOmnityPluginsID; } }

    static public OmniBiclopsBlackLiftManager OmniCreateSingleton(GameObject go) {
        return GetSingleton<OmniBiclopsBlackLiftManager>(ref singleton, go);
    }

    

    private static OmniBiclopsBlackLiftManager singleton;

    static public OmniBiclopsBlackLiftManager Get() {
        return singleton;
    }

    private void Awake() {
        if (VerifySingleton<OmniBiclopsBlackLiftManager>(ref singleton)) {
            currentBlender = null;
            Omnity.onReloadEndCallbackPriority.AddLoaderFunction(PriorityEventHandler.Ordering.Order_BiclopsBlackLift, CoroutineLoader);
            Omnity.onLoadInMemoryConfigStart += (Omnity anOmnity) => {
                if (!anOmnity.PluginEnabled(myOmnityPluginsID)) {
                    enabled = false;
                }
            };
            Omnity.onSaveCompleteCallback += Save;
        }
    }

    public override void Unload() {
        base.Unload();
        currentBlender = null;
        blenders.Clear();
    }

    public override void ReadXMLDelegate(XPathNavigator nav) {
        blenders = LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroup_ExtraSettings_BiclopsBlackLift>.ReadXMLAll(nav, Omnity.anOmnity);
        foreach (var b in blenders) {
            b.extraSettings.parent = b;
        }
    }

    public override System.Collections.IEnumerator PostLoad() {
        LinkCombiners(Omnity.anOmnity);
        yield break;
    }

    private void LinkCombiners(Omnity anOmnity) {
        // Relink();
        foreach (var group in blenders) {
            group.extraSettings.Relink();
        }
    }

    /// <param name="xmlWriter">The XML writer.</param>
    override public void WriteXMLDelegate(System.Xml.XmlTextWriter xmlWriter) {
        foreach (var group in blenders) {
            group.WriteXML(xmlWriter);
        }
    }

    override public string BaseName {
        get {
            return "BiclopsBlackLiftSettings";
        }
    }

    static public LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroup_ExtraSettings_BiclopsBlackLift> currentBlender = null;
    static public System.Collections.Generic.List<LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroup_ExtraSettings_BiclopsBlackLift>> blenders = new System.Collections.Generic.List<LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroup_ExtraSettings_BiclopsBlackLift>>();

    private void Hide() {
        currentBlender = null;

    }

    override public void OnCloseGUI(Omnity anOmnity) {
        Hide();
    }

    public override void MyGuiCallback(Omnity anOmnity) {
        if (!anOmnity.pluginIDs.Contains((int)myOmnityPluginsID)) {
            return;
        }
        if (blenders.Count == 0) {
            currentBlender = null;
        }
        Tabs(anOmnity);
        GUILayout.BeginHorizontal();


        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (currentBlender != null) {
            currentBlender.extraSettings.DrawGUIExtra(anOmnity);
            OmnityHelperFunctions.BR();
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Projector", GUILayout.Width(300))) {
            var b = new LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroup_ExtraSettings_BiclopsBlackLift>();
            b.extraSettings.parent = b;
            blenders.Add(b);
        }
        GUILayout.EndHorizontal();
        SaveLoadGUIButtons(anOmnity);

        GUILayout.BeginHorizontal();
        LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroup_ExtraSettings_BiclopsBlackLift>.DrawGUIAll(anOmnity, blenders);
        GUILayout.EndHorizontal();
    }

    public void Tabs(Omnity anOmnity) {
        GUILayout.BeginHorizontal();

        foreach (LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroup_ExtraSettings_BiclopsBlackLift> bb in blenders) {
            if (GUILayout.Button(currentBlender == bb ? "[" + bb.name + "]" : bb.name)) {
                Hide();
                currentBlender = bb;
                // bb.extraSettings.EditingMesh = true;
            }
        }
        GUILayout.EndHorizontal();
    }

    // Use this for initialization
    private void Start() {
    }
    override public void Update() {
        base.Update();
        if (currentBlender != null) {
            currentBlender.extraSettings.Relink();
        }
    }
}

[System.Serializable]
public class LinkedFinalPassCameraGroup_ExtraSettings_BiclopsBlackLift : LinkedFinalPassCameraGroup_ExtraSettingsInterface {

    //    public LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroup_ExtraSettings_BiclopsBlackLift> parent;
    public LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroup_ExtraSettings_BiclopsBlackLift> parent;

    public LinkedFinalPassCameraGroup_ExtraSettings_BiclopsBlackLift() {
        Reset();
    }

    public void RemoveOrKeepGroupCallBack<T>(Omnity anOmnity, LinkedFinalPassCameraGroupBase<T> _linkedFinalPassCameraGroupBase, bool keepThis, bool linkSizeChanged) where T : LinkedFinalPassCameraGroup_ExtraSettingsInterface, new() {
        //        LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroup_ExtraSettingsInterface> linkedFinalPassCameraGroupBase = _linkedFinalPassCameraGroupBase as LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroup_ExtraSettingsInterface>;
        if (keepThis) {
        } else {
        }
        if (linkSizeChanged) {
        }
    }

    ~LinkedFinalPassCameraGroup_ExtraSettings_BiclopsBlackLift() {
    }


    public int screenNumber = 0;

    public float blackLevelBump= .2f;

    public bool editing {
        get {
            return (OmniBiclopsBlackLiftManager.currentBlender != null && OmniBiclopsBlackLiftManager.currentBlender.extraSettings == this && OmniBiclopsBlackLiftManager.Get().enabled);
        }
    }

    public bool blendEnabled = true;

    /// UNKNOWN

    public void _WriteXML(System.Xml.XmlTextWriter xmlWriter) {
        xmlWriter.WriteStartElement("BiclopsBlackLift");
        xmlWriter.WriteElementString("blendEnabled", blendEnabled.ToString());
        xmlWriter.WriteElementString("screenNumber", screenNumber.ToString());
        xmlWriter.WriteElementString("blackLevelBump", blackLevelBump.ToString());
        
        xmlWriter.WriteEndElement();
    }

    public void _ReadXML(System.Xml.XPath.XPathNavigator nav) {
        blendEnabled = OmnityHelperFunctions.ReadElementBoolDefault(nav, ".//blendEnabled", blendEnabled);
        screenNumber = OmnityHelperFunctions.ReadElementIntDefault(nav, ".//screenNumber", 0);
        blackLevelBump = OmnityHelperFunctions.ReadElementFloatDefault(nav, ".//blackLevelBump", 0.2f);
    }

    private void Reset() {
        blendEnabled = true;
        screenNumber = 0;
        blackLevelBump = .2f;
        SetSecondaryMatricies();
    }

    public void WriteExtraSettings(System.Xml.XmlTextWriter xmlWriter) {
        _WriteXML(xmlWriter);
    }

    public void ReadExtraSettings(System.Xml.XPath.XPathNavigator currentgroup) {
        Reset();
        _ReadXML(currentgroup);
        Relink();
    }

    public void DrawGUIExtra(Omnity anOmnity) {
        GUI.skin.box.VerticalLayout(() => {
            GUI.skin.box.VerticalLayout(() => {
                blendEnabled = OmnityHelperFunctions.BoolInputReset("blendEnabled", blendEnabled, true);
            });

            GUILayout.Label("You should have two screenshapes for the sphere and two final pass cameras for the projectors.  Create one group for each projector and enter the index of the other projector's screen shape. For example projector Top Projector should be linked to Top projector and Sreenshape 1 and Bottom Projector should be linked to screen Shape 0.  They need to be criss crossed.");
            if (OmnityHelperFunctions.IntInputResetWasChanged("screenNumber", ref screenNumber, 0)) {
                screenNumber = Mathf.Clamp(screenNumber, 0, anOmnity.screenShapes.Length);
            }
            blackLevelBump = OmnityHelperFunctions.FloatInputReset("blackLevelBump", blackLevelBump, .2f);
        });
    }
        
    // Update is called once per frame
    public void _Update() {
        if (!Omnity.anOmnity.pluginIDs.Contains((int)OmnityPluginsIDs.OmnityBiclopsBlackLift)) {
            return;
        }
        if (Omnity.anyGuiShowing) {
            Relink();
        }
    }


    public void Relink() {
        SetSecondaryMatricies();
    }

    private Matrix4x4 GetMVP(Camera camera, Transform model) {
        var M = model.localToWorldMatrix;
        var V = camera.worldToCameraMatrix;
        var P = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true);
        Matrix4x4 MVP = P * V * M;
        return MVP;
    }

    private void SetSecondaryMatricies() {
        if (parent == null)
            return;
        try {
            for (int i = 0; i < parent.linked.Count; i++) {
                SetSecondaryMatrix(Omnity.anOmnity.screenShapes[screenNumber], parent.linked[i]);
            }
        } catch (System.Exception e) {
            Debug.LogError(e.Message);
        }
    }

    private void SetSecondaryMatrix(ScreenShape screenshape, FinalPassCamera fpc) {
        try {
            var material = screenshape.trans.GetComponent<Renderer>().sharedMaterial;
            var matrix = GetMVP(fpc.myCamera, screenshape.trans);
            if (material != null) {
                material.SetMatrix("_SecondMatrix", matrix);
                material.SetFloat("_blackLevelBump", blackLevelBump);
            }
        } catch (System.Exception e) {
            Debug.LogError("Error setting second matrix for black level lift " + e.Message);
        }
    }

}

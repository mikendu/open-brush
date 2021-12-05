using System.Collections;
using System.Xml;
using System.Xml.XPath;
using UnityEngine;

public class GlobeUniformity : OmnityTabbedFeature {
    static public OmnityPluginsIDs _myOmnityPluginsID = OmnityPluginsIDs.GlobeUniformity;
    override public OmnityPluginsIDs myOmnityPluginsID { get { return _myOmnityPluginsID; } }

    static public GlobeUniformity OmniCreateSingleton(GameObject go) {
        return GetSingleton<GlobeUniformity>(ref singleton, go);
    }

    static public GlobeUniformity singleton = null;

    public static GlobeUniformity Get() {
        return singleton;
    }

    public override void Unload() {
        base.Unload();
        foreach (var g in groups) {
            g.linked.Clear();
        }
        groups.Clear();
    }

    private void Awake() {
        if (VerifySingleton<GlobeUniformity>(ref singleton)) {
            Omnity.onReloadEndCallbackPriority.AddLoaderFunction(PriorityEventHandler.Ordering.Order_GlobeUniformity, CoroutineLoader);
            Omnity.onSaveCompleteCallback += Save;
        }
    }

    // Use this for initialization
    private void Start() {
    }

    public System.Collections.Generic.List<LinkedScreenShapeGroupBase> groups = new System.Collections.Generic.List<LinkedScreenShapeGroupBase>();

    public override IEnumerator PostLoad() {
        yield return null;
        yield return null;
        yield return null;
        yield return null;
        yield return null;
        yield return null;
        RefreshShaders(Omnity.anOmnity);
        yield break;
    }

    override public void OnCloseGUI(Omnity anOmnity) {
        if (!anOmnity.PluginEnabled(OmnityPluginsIDs.GlobeUniformity)) {
            return;
        }
    }

    public override void MyGuiCallback(Omnity anOmnity) {
        if (!anOmnity.PluginEnabled(OmnityPluginsIDs.GlobeUniformity)) {
            return;
        }
        GUILayout.BeginHorizontal();

        LinkedScreenShapeGroupGlobeUniformityNorm.DrawGUIAll<LinkedScreenShapeGroupGlobeUniformityNorm>(anOmnity, groups);

        GUILayout.EndHorizontal();

        SaveLoadGUIButtons(anOmnity);
    }

    private void RefreshShaders(Omnity anOmnity) {
        foreach (var group in groups) {
            // try {
            LinkedScreenShapeGroupGlobeUniformityNorm g = (LinkedScreenShapeGroupGlobeUniformityNorm)group;
            g.Apply_var(anOmnity);
            // } catch (System.Exception e) {
            //      Debug.LogError(e.Message);
            //    }
        }
    }

    public override void ReadXMLDelegate(XPathNavigator nav) {
        groups = LinkedScreenShapeGroupGlobeUniformityNorm.ReadXMLAll<LinkedScreenShapeGroupGlobeUniformityNorm>(nav, Omnity.anOmnity);
    }

    public override void WriteXMLDelegate(XmlTextWriter xmlWriter) {
        foreach (var group in groups) {
            group.WriteXML(xmlWriter);
        }
    }

    public override string BaseName { get { return "GlobeUniformity"; } }
}

[System.Serializable]
public class LinkedScreenShapeGroupGlobeUniformityNorm : LinkedScreenShapeGroupBase {
    private float intensityAtApex = 1f;
    private float intensityAtEquator = 1f;
    private float intensityAtSummit = .25f;
    private float equatorR = .5f;

    override public void WriteXML_var(System.Xml.XmlTextWriter xmlWriter) {
        xmlWriter.WriteElementString("intensityAtApex", intensityAtApex.ToString("R"));
        xmlWriter.WriteElementString("intensityAtEquator", intensityAtEquator.ToString("R"));
        xmlWriter.WriteElementString("intensityAtSummit", intensityAtSummit.ToString("R"));
        xmlWriter.WriteElementString("equatorR", equatorR.ToString("R"));
    }

    override public void ReadXML_var(System.Xml.XPath.XPathNavigator currentgroup, LinkedScreenShapeGroupBase _newGroup) {
        LinkedScreenShapeGroupGlobeUniformityNorm newGroup = (LinkedScreenShapeGroupGlobeUniformityNorm)_newGroup;
        newGroup.intensityAtApex = OmnityHelperFunctions.ReadElementFloatDefault(currentgroup, ".//intensityAtApex", newGroup.intensityAtApex);
        newGroup.intensityAtEquator = OmnityHelperFunctions.ReadElementFloatDefault(currentgroup, ".//intensityAtEquator", newGroup.intensityAtEquator);
        newGroup.intensityAtSummit = OmnityHelperFunctions.ReadElementFloatDefault(currentgroup, ".//intensityAtSummit", newGroup.intensityAtSummit);
        newGroup.equatorR = OmnityHelperFunctions.ReadElementFloatDefault(currentgroup, ".//equatorR", newGroup.equatorR);
    }

    override public void DrawGUI_variable(Omnity anOmnity) {
        intensityAtApex = OmnityHelperFunctions.FloatInputResetSlider("intensityAtApex", intensityAtApex, 0, 1, 3);
        intensityAtEquator = OmnityHelperFunctions.FloatInputResetSlider("intensityAtEquator", intensityAtEquator, 0, 1, 3);
        intensityAtSummit = OmnityHelperFunctions.FloatInputResetSlider("intensityAtSummit", intensityAtSummit, 0, 1, 3);
        equatorR = OmnityHelperFunctions.FloatInputResetSlider("equatorR", equatorR, 0, .5f, 1);

        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("Defaults");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Disabled")) {
            intensityAtApex = 1;
            intensityAtEquator = 1;
            intensityAtSummit = 1;
            equatorR = .5f;
        }
        if (GUILayout.Button("Subtle")) {
            intensityAtApex = 1;
            intensityAtEquator = 1;
            intensityAtSummit = .25f;
            equatorR = .7f;
        }

        if (GUILayout.Button("Over Drive")) {
            intensityAtApex = 1.5f;
            intensityAtEquator = 1;
            intensityAtSummit = .125f;
            equatorR = .6f;
        }
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
        Apply_var(anOmnity);
    }

    override public void Apply_var(Omnity anOmnity) {
        foreach (var v in linked) {
            if (v.renderer != null) {
                OmnityMono.GlobeUniformityHelper.Apply_var(v.renderer, intensityAtApex, intensityAtEquator, intensityAtSummit, equatorR);
            } else {
                Debug.Log(v.name);
            }
        }
    }
}
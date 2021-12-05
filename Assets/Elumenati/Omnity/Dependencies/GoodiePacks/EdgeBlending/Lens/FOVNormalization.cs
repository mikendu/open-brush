using OmnityMono;
using System.Xml.XPath;
using UnityEngine;

public class FOVNormalization : OmnityTabbedFeature {
    static public OmnityPluginsIDs _myOmnityPluginsID = OmnityPluginsIDs.FOVNormalization;
    override public OmnityPluginsIDs myOmnityPluginsID { get { return _myOmnityPluginsID; } }

    static public FOVNormalization OmniCreateSingleton(GameObject go) {
        return GetSingleton<FOVNormalization>(ref singleton, go);
    }

    static public FOVNormalization singleton = null;

    public static FOVNormalization Get() {
        return singleton;
    }

    private void Awake() {
        if (VerifySingleton<FOVNormalization>(ref singleton)) {
            Omnity.onReloadEndCallbackPriority.AddLoaderFunction(PriorityEventHandler.Ordering.Order_FOVNormalization, CoroutineLoader);
            Omnity.onSaveCompleteCallback += Save;
        }
    }

    // Use this for initialization
    private void Start() {
    }

    public System.Collections.Generic.List<LinkedScreenShapeGroupBase> groups = new System.Collections.Generic.List<LinkedScreenShapeGroupBase>();

    public override void ReadXMLDelegate(XPathNavigator nav) {
        groups = LinkedScreenShapeGroupFOVNorm.ReadXMLAll<LinkedScreenShapeGroupFOVNorm>(nav, Omnity.anOmnity);
        try {
            RefreshShaders(Omnity.anOmnity);
        } catch (System.Exception e) {
            Debug.LogError(e.Message);
        }
    }

    /// <param name="xmlWriter">The XML writer.</param>
    override public void WriteXMLDelegate(System.Xml.XmlTextWriter xmlWriter) {
        foreach (var group in groups) {
            group.WriteXML(xmlWriter);
        }
    }

    public override string BaseName {
        get {
            return "FOVNormalization";
        }
    }

    public override void MyGuiCallback(Omnity anOmnity) {
        if (!anOmnity.PluginEnabled(OmnityPluginsIDs.FOVNormalization)) {
            return;
        }
        GUILayout.BeginHorizontal();

        LinkedScreenShapeGroupFOVNorm.DrawGUIAll<LinkedScreenShapeGroupFOVNorm>(anOmnity, groups);
        /*
        if (GUILayout.Button("Add Renderer", GUILayout.Width(300))) {
            LinkedScreenShapeGroupFOVNorm newGroup = new LinkedScreenShapeGroupFOVNorm();
            newGroup.linked.AddRange(Omnity.anOmnity.screenShapes);
            groups.Add(newGroup);
            newGroup.Apply_var(anOmnity);
        }
        */
        GUILayout.EndHorizontal();
        SaveLoadGUIButtons(anOmnity);
    }

    private void RefreshShaders(Omnity anOmnity) {
        foreach (var group in groups) {
            try {
                LinkedScreenShapeGroupFOVNorm g = (LinkedScreenShapeGroupFOVNorm)group;
                g.Apply_var(anOmnity);
            } catch (System.Exception e) {
                Debug.LogError(e.Message);
            }
        }
    }
}

[System.Serializable]
public class LinkedScreenShapeGroupFOVNorm : LinkedScreenShapeGroupBase {
    public float fov = 180.0f;
    private const float _LensGammaBangThetaDefault = 1;

    //      return 0.2798*R* R *R* R *R* R  - 0.6875*R* R *R* R *R  + 0.7351*R* R *R* R  - 0.3472*R* R *R  + 0.0977*R* R  + 0.9221*R ;

    private float _LensGamma = _LensGammaBangThetaDefault;
    private float _Lens6 = FOVNormalizationExtensions._Lens6BangThetaDefault;
    private float _Lens5 = FOVNormalizationExtensions._Lens5BangThetaDefault;
    private float _Lens4 = FOVNormalizationExtensions._Lens4BangThetaDefault;
    private float _Lens3 = FOVNormalizationExtensions._Lens3BangThetaDefault;
    private float _Lens2 = FOVNormalizationExtensions._Lens2BangThetaDefault;
    private float _Lens1 = FOVNormalizationExtensions._Lens1BangThetaDefault;

    override public void WriteXML_var(System.Xml.XmlTextWriter xmlWriter) {
        xmlWriter.WriteElementString("fov", fov.ToString("R"));
        xmlWriter.WriteElementString("_Lens1", _Lens1.ToString("R"));
        xmlWriter.WriteElementString("_Lens2", _Lens2.ToString("R"));
        xmlWriter.WriteElementString("_Lens3", _Lens3.ToString("R"));
        xmlWriter.WriteElementString("_Lens4", _Lens4.ToString("R"));
        xmlWriter.WriteElementString("_Lens5", _Lens5.ToString("R"));
        xmlWriter.WriteElementString("_Lens6", _Lens6.ToString("R"));
        xmlWriter.WriteElementString("_LensGamma", _LensGamma.ToString("R"));
    }

    override public void ReadXML_var(System.Xml.XPath.XPathNavigator currentgroup, LinkedScreenShapeGroupBase _newGroup) {
        LinkedScreenShapeGroupFOVNorm newGroup = (LinkedScreenShapeGroupFOVNorm)_newGroup;
        newGroup.fov = OmnityHelperFunctions.ReadElementFloatDefault(currentgroup, ".//fov", newGroup.fov);
        newGroup._Lens1 = OmnityHelperFunctions.ReadElementFloatDefault(currentgroup, ".//_Lens1", FOVNormalizationExtensions._Lens1BangThetaDefault);
        newGroup._Lens2 = OmnityHelperFunctions.ReadElementFloatDefault(currentgroup, ".//_Lens2", FOVNormalizationExtensions._Lens2BangThetaDefault);
        newGroup._Lens3 = OmnityHelperFunctions.ReadElementFloatDefault(currentgroup, ".//_Lens3", FOVNormalizationExtensions._Lens3BangThetaDefault);
        newGroup._Lens4 = OmnityHelperFunctions.ReadElementFloatDefault(currentgroup, ".//_Lens4", FOVNormalizationExtensions._Lens4BangThetaDefault);
        newGroup._Lens5 = OmnityHelperFunctions.ReadElementFloatDefault(currentgroup, ".//_Lens5", FOVNormalizationExtensions._Lens5BangThetaDefault);
        newGroup._Lens6 = OmnityHelperFunctions.ReadElementFloatDefault(currentgroup, ".//_Lens6", FOVNormalizationExtensions._Lens6BangThetaDefault);
        newGroup._LensGamma = OmnityHelperFunctions.ReadElementFloatDefault(currentgroup, ".//_LensGamma", _LensGammaBangThetaDefault);
    }

    override public void DrawGUI_variable(Omnity anOmnity) {
        bool wasChanged = OmnityHelperFunctions.FloatInputResetSliderWasChanged("fov", ref fov, 0, 180.0f, 360);
        wasChanged |= OmnityHelperFunctions.FloatInputResetSliderWasChanged("Lens1", ref _Lens1, -2, 1, 2);
        wasChanged |= OmnityHelperFunctions.FloatInputResetSliderWasChanged("Lens2", ref _Lens2, -2, 0, 2);
        wasChanged |= OmnityHelperFunctions.FloatInputResetSliderWasChanged("Lens3", ref _Lens3, -2, 0, 2);
        wasChanged |= OmnityHelperFunctions.FloatInputResetSliderWasChanged("Lens4", ref _Lens4, -2, 0, 2);
        wasChanged |= OmnityHelperFunctions.FloatInputResetSliderWasChanged("Lens5", ref _Lens5, -2, 0, 2);
        wasChanged |= OmnityHelperFunctions.FloatInputResetSliderWasChanged("Lens6", ref _Lens6, -2, 0, 2);
        wasChanged |= OmnityHelperFunctions.FloatInputResetSliderWasChanged("_LensGamma", ref _LensGamma, .001f, 1, 3);
        if (OmnityHelperFunctions.Button("Set !Theta (default)")) {
            SetBangTheta();
            wasChanged = true;
        }
        if (OmnityHelperFunctions.Button("Set F-Theta ")) {
            SetFTheta();
            wasChanged = true;
        }
        if (wasChanged) {
            Apply_var(anOmnity);
        }
    }

    private void SetFTheta() {
        _Lens6 = FOVNormalizationExtensions._Lens6FThetaDefault;
        _Lens5 = FOVNormalizationExtensions._Lens5FThetaDefault;
        _Lens4 = FOVNormalizationExtensions._Lens4FThetaDefault;
        _Lens3 = FOVNormalizationExtensions._Lens3FThetaDefault;
        _Lens2 = FOVNormalizationExtensions._Lens2FThetaDefault;
        _Lens1 = FOVNormalizationExtensions._Lens1FThetaDefault;
        _LensGamma = 1;
    }

    private void SetBangTheta() {
        _Lens6 = FOVNormalizationExtensions._Lens6BangThetaDefault;
        _Lens5 = FOVNormalizationExtensions._Lens5BangThetaDefault;
        _Lens4 = FOVNormalizationExtensions._Lens4BangThetaDefault;
        _Lens3 = FOVNormalizationExtensions._Lens3BangThetaDefault;
        _Lens2 = FOVNormalizationExtensions._Lens2BangThetaDefault;
        _Lens1 = FOVNormalizationExtensions._Lens1BangThetaDefault;
        _LensGamma = _LensGammaBangThetaDefault;
    }

    override public void Apply_var(Omnity anOmnity) {
        foreach (var v in linked) {
            v.renderer.sharedMaterial.SetFloat("_LensFOVDegrees", fov);
            v.renderer.sharedMaterial.SetFloat("_Lens6", _Lens6);
            v.renderer.sharedMaterial.SetFloat("_Lens5", _Lens5);
            v.renderer.sharedMaterial.SetFloat("_Lens4", _Lens4);
            v.renderer.sharedMaterial.SetFloat("_Lens3", _Lens3);
            v.renderer.sharedMaterial.SetFloat("_Lens2", _Lens2);
            v.renderer.sharedMaterial.SetFloat("_Lens1", _Lens1);
            v.renderer.sharedMaterial.SetFloat("_LensGamma", _LensGamma);
        }
    }
}
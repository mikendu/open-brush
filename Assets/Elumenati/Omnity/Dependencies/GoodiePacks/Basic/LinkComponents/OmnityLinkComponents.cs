using System.Collections.Generic;
using System.Xml.XPath;
using UnityEngine;

public class OmnityLinkComponents : OmnityTabbedFeature {
    static private OmnityLinkComponents singleton = null;

    static public OmnityPluginsIDs _myOmnityPluginsID = OmnityPluginsIDs.LinkComponents;
    override public OmnityPluginsIDs myOmnityPluginsID { get { return _myOmnityPluginsID; } }

    static public OmnityLinkComponents OmniCreateSingleton(GameObject go) {
        return GetSingleton<OmnityLinkComponents>(ref singleton, go);
    }

    static public OmnityLinkComponents Get() {
        return singleton;
    }

    private void Awake() {
        if (VerifySingleton<OmnityLinkComponents>(ref singleton)) {
            if (singleton == null) {
                singleton = this;
            } else if (singleton != this) {
                Debug.LogError("ERROR THERE SHOULD ONLY BE ONE of " + this.GetType().ToString() + " per scene");
                enabled = false;
                return;
            }

            Omnity.onReloadEndCallbackPriority.AddLoaderFunction(PriorityEventHandler.Ordering.Order_OmnilinkClipMapper, CoroutineLoader);
            Omnity.onLoadInMemoryConfigStart += MyLoad;
            Omnity.onSaveCompleteCallback += Save;
        }
    }

    private void MyLoad(Omnity anOmnity) {
        if (anOmnity.PluginEnabled(OmnityPluginsIDs.LinkComponents)) {
            anOmnity.StartCoroutine(Load(anOmnity));
        }
    }

    private void OnDestroy() {
        singleton = null;
        Omnity.onReloadEndCallbackPriority.RemoveLoaderFunction(PriorityEventHandler.Ordering.Order_OmnilinkClipMapper, CoroutineLoader);
        Omnity.onSaveCompleteCallback -= Save;
        Omnity.onLoadInMemoryConfigStart -= MyLoad;
    }

    public System.Collections.Generic.List<LinkedFinalPassCameraGroup> linkedFinalPassCameraGroups = new System.Collections.Generic.List<LinkedFinalPassCameraGroup>();
    public System.Collections.Generic.List<LinkedScreenShapeGroupBase> linkedLinkedScreenShapeGroups = new System.Collections.Generic.List<LinkedScreenShapeGroupBase>();

    override public string BaseName {
        get {
            return "linkHelper";
        }
    }

    /// <param name="xmlWriter">The XML writer.</param>
    override public void WriteXMLDelegate(System.Xml.XmlTextWriter xmlWriter) {
        foreach (var group in linkedFinalPassCameraGroups) {
            group.WriteXML(xmlWriter);
        }
        foreach (var group in linkedLinkedScreenShapeGroups) {
            group.WriteXML(xmlWriter);
        }
    }

    public override void ReadXMLDelegate(XPathNavigator nav) {
        linkedFinalPassCameraGroups = LinkedFinalPassCameraGroup.ReadXMLAll(nav, Omnity.anOmnity);
        foreach (var group in linkedFinalPassCameraGroups) {
            group.Apply(Omnity.anOmnity);
        }
        linkedLinkedScreenShapeGroups = LinkGroupScreenShapes_ExtraSettings.ReadXMLAll<LinkGroupScreenShapes_ExtraSettings>(nav, Omnity.anOmnity);
        foreach (var group in linkedLinkedScreenShapeGroups) {
            group.Apply_var(Omnity.anOmnity);
        }
    }

    private string expanderHash = OmnityHelperFunctions.OmnityUniqueHash.uniqueHash;

    /// <summary>
    /// Overload this with the gui layout calls.
    /// </summary>
    override public void MyGuiCallback(Omnity anOmnity) {
        if (!anOmnity.PluginEnabled(OmnityPluginsIDs.LinkComponents)) {
            return;
        }
        GUILayout.BeginHorizontal();
        if (OmnityHelperFunctions.BeginExpanderSimple(expanderHash + "FPC", "Linked \"FinalPassCameras\"", "Linked \"FinalPassCameras\"")) {
            GUILayout.BeginVertical();
            LinkedFinalPassCameraGroup.DrawGUIAll(anOmnity, linkedFinalPassCameraGroups);
            GUILayout.EndVertical();
        }
        OmnityHelperFunctions.EndExpanderSimple();
        OmnityHelperFunctions.BR();
        if (OmnityHelperFunctions.BeginExpanderSimple(expanderHash + "SS", "Linked ScreenShapes", "Linked ScreenShapes")) {
            GUILayout.BeginVertical();
            LinkedScreenShapeGroupBase.DrawGUIAll<LinkedScreenShapeGroupBase>(anOmnity, linkedLinkedScreenShapeGroups);
            GUILayout.EndVertical();
        }
        OmnityHelperFunctions.EndExpanderSimple();

        SaveLoadGUIButtons(anOmnity);
    }

    /// <summary>
    /// Called when the GUI closes
    /// </summary>
    override public void OnCloseGUI(Omnity anOmnity) {
        if (!anOmnity.PluginEnabled(OmnityPluginsIDs.LinkComponents)) {
            return;
        }
    }
}

[System.Serializable]
abstract public class OmnityLinkGroup<T> {
    public string name = "Group";
    public System.Collections.Generic.List<T> linked = new System.Collections.Generic.List<T>();

    abstract public void WriteXML(System.Xml.XmlTextWriter xmlWriter);
}

[System.Serializable]
public class LinkedFinalPassCameraGroup : OmnityLinkGroup<FinalPassCamera> {
    public bool linkTransform = false;
    public bool linkOmnityPerspectiveMatrix = false;

    override public void WriteXML(System.Xml.XmlTextWriter xmlWriter) {
        xmlWriter.WriteStartElement("LinkedFinalPassCameraGroup");
        xmlWriter.WriteElementString("name", name);
        xmlWriter.WriteElementString("linkTransform", linkTransform.ToString());
        xmlWriter.WriteElementString("linkOmnityPerspectiveMatrix ", linkOmnityPerspectiveMatrix.ToString());
        foreach (FinalPassCamera fpc in linked) {
            WriteXMLFPC(xmlWriter, fpc);
        }
        xmlWriter.WriteEndElement();
    }

    public void WriteXMLFPC(System.Xml.XmlTextWriter xmlWriter, FinalPassCamera fpc) {
        xmlWriter.WriteStartElement("FinalPassCamera");
        xmlWriter.WriteElementString("name", fpc.name);
        xmlWriter.WriteEndElement();
    }

    static public System.Collections.Generic.List<LinkedFinalPassCameraGroup> ReadXMLAll(System.Xml.XPath.XPathNavigator nav, Omnity anOmnity) {
        System.Collections.Generic.List<LinkedFinalPassCameraGroup> allgroups = new List<LinkedFinalPassCameraGroup>();
        System.Xml.XPath.XPathNodeIterator OmnityInfos = nav.Select("(.//LinkedFinalPassCameraGroup)");
        while (OmnityInfos.MoveNext()) {
            System.Xml.XPath.XPathNavigator OmnityInfo = OmnityInfos.Current;
            LinkedFinalPassCameraGroup newGroup = new LinkedFinalPassCameraGroup();
            newGroup.name = OmnityHelperFunctions.ReadElementStringDefault(OmnityInfo, ".//name", newGroup.name);
            newGroup.linkTransform = OmnityHelperFunctions.ReadElementBoolDefault(OmnityInfo, ".//linkTransform", newGroup.linkTransform);
            newGroup.linkOmnityPerspectiveMatrix = OmnityHelperFunctions.ReadElementBoolDefault(OmnityInfo, ".//linkOmnityPerspectiveMatrix", newGroup.linkOmnityPerspectiveMatrix);

            System.Xml.XPath.XPathNodeIterator myPerspectiveCameraArrayIterator = OmnityInfo.Select("(.//FinalPassCamera)");

            while (myPerspectiveCameraArrayIterator.MoveNext()) {
                string newname = OmnityHelperFunctions.ReadElementStringDefault(myPerspectiveCameraArrayIterator.Current, ".//name", null);
                if (anOmnity.HasFPCByName(newname)) {
                    newGroup.linked.Add(anOmnity.FindFPCByName(newname));
                }
            }
            allgroups.Add(newGroup);
        }
        return allgroups;
    }

    private string expanderHash = OmnityHelperFunctions.OmnityUniqueHash.uniqueHash;

    static public bool FPCOnGUIProxy(FinalPassCamera fpc, bool isMain, float w, bool isButton, bool isChecked) {
        if (fpc.renderTextureSettings.enabled) {
            return fpc.renderTextureSettings.OnGUI(isMain, w, isButton);
        } else {
            if (isButton) {
                return OmnityHelperFunctions.BoolInputReset(fpc.name, isChecked, false);
            } else {
                if (isChecked) {
                    GUILayout.Label("[" + fpc.name + "]");
                } else {
                    GUILayout.Label(fpc.name);
                }
                return false;
            }
        }
    }

    public bool DrawGUI(Omnity anOmnity) {
        bool clickedRemove = false;
        if (OmnityHelperFunctions.BeginExpanderSimple(expanderHash, name, name)) {
            GUILayout.BeginHorizontal();
            clickedRemove = GUILayout.Button("Remove", GUILayout.Width(100));
            GUILayout.FlexibleSpace();
            GUILayout.Label("Name : ");
            name = GUILayout.TextField(name, GUILayout.Width(300));
            OmnityHelperFunctions.BR();
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical();

            #region VaribleSelection

            if (OmnityHelperFunctions.BeginExpanderSimple(expanderHash + "LV", "Linked Variables", "Linked Variables")) {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                linkTransform = OmnityHelperFunctions.BoolInput("linkTransform", linkTransform);
                OmnityHelperFunctions.BR();
                OmnityHelperFunctions.BR();
                GUILayout.FlexibleSpace();
                linkOmnityPerspectiveMatrix = OmnityHelperFunctions.BoolInput("linkOmnityPerspectiveMatrix", linkOmnityPerspectiveMatrix);
                OmnityHelperFunctions.BR();
                GUILayout.EndHorizontal();
            }
            OmnityHelperFunctions.EndExpanderSimple();

            #endregion VaribleSelection

            #region GroupSelection

            if (OmnityHelperFunctions.BeginExpanderSimple(expanderHash + "FPCSL", "Linked FinalPassCameras", "Linked FinalPassCameras")) {
                GUILayout.BeginHorizontal();
                foreach (var fpc in anOmnity.finalPassCameras) {
                    GUILayout.FlexibleSpace();
                    if (linked.Contains(fpc)) {
                        FPCOnGUIProxy(fpc, false, 128, false, true);
                        if (!OmnityHelperFunctions.BoolInput(fpc.name, true, 300)) {
                            linked.Remove(fpc);
                        }
                    } else {
                        FPCOnGUIProxy(fpc, false, 64, false, false);
                        if (OmnityHelperFunctions.BoolInput(fpc.name, false, 300)) {
                            linked.Add(fpc);
                        }
                    }
                    OmnityHelperFunctions.BR();
                }
                GUILayout.EndHorizontal();
            } else {
                GUILayout.BeginHorizontal();
                foreach (var link in linked) {
                    link.renderTextureSettings.OnGUI(false, 128);
                }
                GUILayout.EndHorizontal();
            }
            OmnityHelperFunctions.EndExpanderSimple();

            #endregion GroupSelection

            GUILayout.EndVertical();
        } else {
            GUILayout.BeginHorizontal();
            foreach (var link in linked) {
                link.renderTextureSettings.OnGUI(false, 128);
            }
            GUILayout.EndHorizontal();
        }
        OmnityHelperFunctions.EndExpanderSimple();

        return !clickedRemove;
    }

    public void Apply(Omnity anOmnity) {
        for (int i = 1; i < linked.Count; i++) {
            if (linkTransform) {
                linked[i].transformInfo = linked[0].transformInfo;
            }
            if (linkOmnityPerspectiveMatrix) {
                linked[i].omnityPerspectiveMatrix = linked[0].omnityPerspectiveMatrix;
            }
        }
    }

    public static void DrawGUIAll(Omnity anOmnity, System.Collections.Generic.List<LinkedFinalPassCameraGroup> linkedFinalPassCameraGroups) {
        GUILayout.Label("Final Pass Cameras Groups");
        OmnityHelperFunctions.BR();
        LinkedFinalPassCameraGroup lfpcgToRemove = null;
        foreach (var fpclinkgroup in linkedFinalPassCameraGroups) {
            if (!fpclinkgroup.DrawGUI(anOmnity)) {
                lfpcgToRemove = fpclinkgroup;
            }
            OmnityHelperFunctions.BR();
        }

        if (lfpcgToRemove != null) {
            linkedFinalPassCameraGroups.Remove(lfpcgToRemove);
        }
        if (GUILayout.Button("Add Link Group", GUILayout.Width(300))) {
            linkedFinalPassCameraGroups.Add(new LinkedFinalPassCameraGroup());
        }
    }
};

[System.Serializable]
public class LinkGroupScreenShapes_ExtraSettings : LinkedScreenShapeGroupBase {

    // vars
    public bool linkTransform = false;

    public bool linkSphereParams = false;

    override public void WriteXML_var(System.Xml.XmlTextWriter xmlWriter) {
        xmlWriter.WriteElementString("linkTransform", linkTransform.ToString());
        xmlWriter.WriteElementString("linkSphereParams", linkSphereParams.ToString());
    }

    override public void ReadXML_var(System.Xml.XPath.XPathNavigator currentgroup, LinkedScreenShapeGroupBase _newGroup) {
        LinkGroupScreenShapes_ExtraSettings newGroup = (LinkGroupScreenShapes_ExtraSettings)_newGroup;
        newGroup.linkTransform = OmnityHelperFunctions.ReadElementBoolDefault(currentgroup, ".//linkTransform", newGroup.linkTransform);
        newGroup.linkSphereParams = OmnityHelperFunctions.ReadElementBoolDefault(currentgroup, ".//linkSphereParams", newGroup.linkSphereParams);
    }

    override public void DrawGUI_variable(Omnity anOmnity) {

        #region VaribleSelection

        if (OmnityHelperFunctions.BeginExpanderSimple(expanderHash + "LV", "Linked Variables", "Linked Variables")) {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            linkTransform = OmnityHelperFunctions.BoolInput("linkTransform", linkTransform);
            OmnityHelperFunctions.BR();
            GUILayout.FlexibleSpace();
            linkSphereParams = OmnityHelperFunctions.BoolInput("linkSphereParams", linkSphereParams);
            OmnityHelperFunctions.BR();
            GUILayout.EndHorizontal();
        }
        OmnityHelperFunctions.EndExpanderSimple();

        #endregion VaribleSelection
    }

    override public void Apply_var(Omnity anOmnity) {
        for (int i = 1; i < linked.Count; i++) {
            if (linkTransform) {
                linked[i].transformInfo = linked[0].transformInfo;
            }
            if (linkSphereParams) {
                linked[i].sphereParams = linked[0].sphereParams;
                linked[i].planeParams = linked[0].planeParams;
            }
        }
    }
}
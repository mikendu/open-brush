using UnityEngine;

public interface LinkedFinalPassCameraGroup_ExtraSettingsInterface {

    void WriteExtraSettings(System.Xml.XmlTextWriter xmlWriter);

    void ReadExtraSettings(System.Xml.XPath.XPathNavigator currentGroup);

    void DrawGUIExtra(Omnity anOmnity);

    void RemoveOrKeepGroupCallBack<T>(Omnity anOmnity, LinkedFinalPassCameraGroupBase<T> _linkedFinalPassCameraGroupBase, bool keepThis, bool linkSizeChanged) where T : LinkedFinalPassCameraGroup_ExtraSettingsInterface, new();
}

[System.Serializable]
public class LinkedFinalPassCameraGroupBase<T> : OmnityLinkGroup<FinalPassCamera> where T : LinkedFinalPassCameraGroup_ExtraSettingsInterface, new() {
    public T extraSettings = new T();

    static public string XMLTagName {
        get {
            return "LinkedFinalPassCameraGroup";
        }
    }

    static public string XMLTagName2 {
        get {
            return "LinkedFinalPassCameraGroupSetting";
        }
    }

    override public void WriteXML(System.Xml.XmlTextWriter xmlWriter) {
        xmlWriter.WriteStartElement(XMLTagName);
        xmlWriter.WriteElementString("name", name);
        extraSettings.WriteExtraSettings(xmlWriter);

        foreach (FinalPassCamera ss in linked) {
            WriteXMLSS(xmlWriter, ss);
        }
        xmlWriter.WriteEndElement();
    }

    public void WriteXMLSS(System.Xml.XmlTextWriter xmlWriter, FinalPassCamera ss) {
        xmlWriter.WriteStartElement("FinalPassCamera");
        xmlWriter.WriteElementString("name", ss.name);
        xmlWriter.WriteEndElement();
    }

    static public System.Collections.Generic.List<LinkedFinalPassCameraGroupBase<T>> ReadXMLAll(System.Xml.XPath.XPathNavigator nav, Omnity anOmnity) {
        System.Collections.Generic.List<LinkedFinalPassCameraGroupBase<T>> allgroups = new System.Collections.Generic.List<LinkedFinalPassCameraGroupBase<T>>();
        System.Xml.XPath.XPathNodeIterator group = nav.Select("(.//" + XMLTagName + ")");
        while (group.MoveNext()) {
            System.Xml.XPath.XPathNavigator currentgroup = group.Current;

            LinkedFinalPassCameraGroupBase<T> newGroup = new LinkedFinalPassCameraGroupBase<T>();

            newGroup.name = OmnityHelperFunctions.ReadElementStringDefault(currentgroup, ".//name", newGroup.name);
            newGroup.extraSettings.ReadExtraSettings(currentgroup);

            System.Xml.XPath.XPathNodeIterator mySSIterator = currentgroup.Select("(.//FinalPassCamera)");

            while (mySSIterator.MoveNext()) {
                string newname = OmnityHelperFunctions.ReadElementStringDefault(mySSIterator.Current, ".//name", null);
                if (anOmnity.HasFPCByName(newname)) {
                    newGroup.linked.Add(anOmnity.FindFPCByName(newname));
                }
            }
            allgroups.Add(newGroup as LinkedFinalPassCameraGroupBase<T>);
        }
        return allgroups;
    }

    static public bool FPCOnGUIProxy(FinalPassCamera fpc, bool isMain, float w, bool isButton, bool isChecked) {
        if (fpc.renderTextureSettings.enabled) {
            return fpc.renderTextureSettings.OnGUI(isMain, w, isButton);
        } else {
            if (isButton) {
                if (isChecked) {
                    return GUILayout.Button("[" + fpc.name + "]");
                } else {
                    return GUILayout.Button(fpc.name);
                }
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

    virtual public bool DrawGUI(Omnity anOmnity) {
        bool clickedRemove = false;
        if (OmnityHelperFunctions.BeginExpanderSimple(expanderHash, name, name)) {
            GUILayout.BeginHorizontal();
            clickedRemove = GUILayout.Button("Remove", GUILayout.Width(100));
            GUILayout.FlexibleSpace();
            GUILayout.Label("Name : ");
            name = GUILayout.TextField(name, GUILayout.Width(300));
            GUILayout.EndHorizontal();

            #region GroupSelection

            if (OmnityHelperFunctions.BeginExpanderSimple(expanderHash + "CAMS", "Linked FinalPassCameras", "Linked FinalPassCameras")) {
                GUILayout.BeginHorizontal();
                foreach (var ss in anOmnity.finalPassCameras) {
                    GUILayout.FlexibleSpace();
                    if (linked.Contains(ss)) {
                        if (FPCOnGUIProxy(ss, false, 256, true, true)) {
                            linked.Remove(ss);
                            linkSizeChanged = true;
                        }
                    } else {
                        if (FPCOnGUIProxy(ss, false, 128, true, false)) {
                            linked.Add(ss);
                            linkSizeChanged = true;
                        }
                    }
                    GUILayout.Label(ss.name, GUILayout.Width(300));
                    OmnityHelperFunctions.BR();
                }
                GUILayout.EndHorizontal();
            } else {
                foreach (var ss in anOmnity.finalPassCameras) {
                    if (linked.Contains(ss)) {
                        FPCOnGUIProxy(ss, false, 128, false, true);
                    }
                }
            }
            OmnityHelperFunctions.EndExpanderSimple();

            #endregion GroupSelection

            #region EXtrasettings

            if (OmnityHelperFunctions.BeginExpanderSimple(expanderHash + "Settings", "Settings", "Settings")) {
                extraSettings.DrawGUIExtra(anOmnity);
            }
            OmnityHelperFunctions.EndExpanderSimple();

            #endregion EXtrasettings
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
    public static bool linkSizeChanged = false;
    public static void DrawGUIAll(Omnity anOmnity, System.Collections.Generic.List<LinkedFinalPassCameraGroupBase<T>> linkedLinkedFinalPassCameraGroupPMs) {
        LinkedFinalPassCameraGroupBase<T> toRemove = null;
        foreach (LinkedFinalPassCameraGroupBase<T> group in linkedLinkedFinalPassCameraGroupPMs) {
            if (!group.DrawGUI(anOmnity)) {
                toRemove = group;
                group.extraSettings.RemoveOrKeepGroupCallBack(anOmnity, group, false, linkSizeChanged);
            } else {
                group.extraSettings.RemoveOrKeepGroupCallBack(anOmnity, group, true,linkSizeChanged);
            }
            linkSizeChanged = false;
           OmnityHelperFunctions.BR();
        }
        if (toRemove != null) {
            linkedLinkedFinalPassCameraGroupPMs.Remove(toRemove);
        }
    }

    private string expanderHash = OmnityHelperFunctions.OmnityUniqueHash.uniqueHash;
};

[System.Serializable]
public class LinkedScreenShapeGroupBase : OmnityLinkGroup<ScreenShape> {

    virtual public void WriteXML_var(System.Xml.XmlTextWriter xmlWriter) {
    }

    virtual public void ReadXML_var(System.Xml.XPath.XPathNavigator currentgroup, LinkedScreenShapeGroupBase newGroup) {
    }

    virtual public void DrawGUI_variable(Omnity anOmnity) {
    }

    virtual public void Apply_var(Omnity anOmnity) {
    }

    override public void WriteXML(System.Xml.XmlTextWriter xmlWriter) {
        xmlWriter.WriteStartElement(XMLTAG);
        xmlWriter.WriteElementString("name", name);
        WriteXML_var(xmlWriter);
        for (int i = 0; i < linked.Count; i++) {
            WriteXMLSS(xmlWriter, linked[i]);
        }
        xmlWriter.WriteEndElement();
    }

    public void WriteXMLSS(System.Xml.XmlTextWriter xmlWriter, ScreenShape ss) {
        xmlWriter.WriteStartElement("ScreenShape");
        xmlWriter.WriteElementString("name", ss.name);
        xmlWriter.WriteEndElement();
    }

    public virtual bool showScreenShapes {
        get {
            return true;
        }
    }

    static public string XMLTAG {
        get {
            return "LinkedScreenShapeGroup";
        }
    }

    static public System.Collections.Generic.List<LinkedScreenShapeGroupBase> ReadXMLAll<T>(System.Xml.XPath.XPathNavigator nav, Omnity anOmnity) where T : LinkedScreenShapeGroupBase, new() {
        System.Collections.Generic.List<LinkedScreenShapeGroupBase> allgroups = new
            System.Collections.Generic.List<LinkedScreenShapeGroupBase>();
        System.Xml.XPath.XPathNodeIterator group = nav.Select("(.//" + XMLTAG + ")");
        while (group.MoveNext()) {
            System.Xml.XPath.XPathNavigator currentgroup = group.Current;
            T newGroup = new T();
            newGroup.name = OmnityHelperFunctions.ReadElementStringDefault(currentgroup, ".//name", newGroup.name);

            newGroup.ReadXML_var(currentgroup, newGroup);

            System.Xml.XPath.XPathNodeIterator mySSIterator = currentgroup.Select("(.//ScreenShape)");

            while (mySSIterator.MoveNext()) {
                string newname = OmnityHelperFunctions.ReadElementStringDefault(mySSIterator.Current, ".//name", null);
                if (anOmnity.HasScreenShapeByName(newname)) {
                    newGroup.linked.Add(anOmnity.FindScreenShapeByName(newname));
                }
            }
            allgroups.Add(newGroup);
        }
        return allgroups;
    }

    protected string expanderHash = OmnityHelperFunctions.OmnityUniqueHash.uniqueHash;

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
            DrawGUI_variable(anOmnity);

            #region GroupSelection

            if (showScreenShapes) {
                if (OmnityHelperFunctions.BeginExpanderSimple(expanderHash + "LSS", "Linked ScreenShapes", "Linked ScreenShapes")) {
                    GUILayout.BeginHorizontal();
                    foreach (var ss in anOmnity.screenShapes) {
                        GUILayout.FlexibleSpace();
                        if (linked.Contains(ss)) {
                            if (!OmnityHelperFunctions.BoolInput(ss.name, true)) {
                                linked.Remove(ss);
                            }
                        } else {
                            if (OmnityHelperFunctions.BoolInput(ss.name, false)) {
                                linked.Add(ss);
                            }
                        }
                        OmnityHelperFunctions.BR();
                    }
                    GUILayout.EndHorizontal();
                }
                OmnityHelperFunctions.EndExpanderSimple();
            }

            #endregion GroupSelection

            GUILayout.EndVertical();
        }
        OmnityHelperFunctions.EndExpanderSimple();
        return !clickedRemove;
    }

    public static void DrawGUIAll<T>(Omnity anOmnity, System.Collections.Generic.List<LinkedScreenShapeGroupBase> linkedLinkedScreenShapeGroups) where T : LinkedScreenShapeGroupBase, new() {
        OmnityHelperFunctionsGUI.VerticalLayout(GUI.skin.box, () => {
            GUILayout.Label("ScreenShape Groups");
            LinkedScreenShapeGroupBase lfpcgToRemove = null;
            foreach (var fpclinkgroup in linkedLinkedScreenShapeGroups) {
                OmnityHelperFunctionsGUI.VerticalLayout(GUI.skin.box, () => {
                    if (!fpclinkgroup.DrawGUI(anOmnity)) {
                        lfpcgToRemove = fpclinkgroup;
                    }
                });
            }

            if (lfpcgToRemove != null) {
                linkedLinkedScreenShapeGroups.Remove(lfpcgToRemove);
            }
            if (GUILayout.Button("Add", GUILayout.Width(300))) {
                linkedLinkedScreenShapeGroups.Add(new T());
            }
        });
    }
};

static public partial class OmnityExtensionFunctions {

    #region FinalPassCamera

    static public FinalPassCamera FindFPCByName(this Omnity anOmnity, string name) {
        if (name != null && name != "") {
            foreach (FinalPassCamera fpc in anOmnity.finalPassCameras) {
                if (fpc.name == name) {
                    return fpc;
                }
            }
        }
        return null;
    }

    static public bool HasFPCByName(this Omnity anOmnity, string name) {
        if (name != null && name != "") {
            foreach (FinalPassCamera fpc in anOmnity.finalPassCameras) {
                if (fpc.name == name) {
                    return true;
                }
            }
        }
        return false;
    }

    #endregion FinalPassCamera

    #region ScreenShape

    static public ScreenShape FindScreenShapeByName(this Omnity anOmnity, string name) {
        if (anOmnity != null && name != null && name != "") {
            foreach (ScreenShape ss in anOmnity.screenShapes) {
                if (ss.name == name) {
                    return ss;
                }
            }
        }
        return null;
    }

    static public bool HasScreenShapeByName(this Omnity anOmnity, string name) {
        return anOmnity.FindScreenShapeByName(name) != null;
    }

    #endregion ScreenShape

    #region PerspectiveCamera

    static public PerspectiveCamera FindPerspectiveCameraByName(this Omnity anOmnity, string name) {
        if (anOmnity != null && name != null && name != "") {
            foreach (PerspectiveCamera pc in anOmnity.cameraArray) {
                if (pc.name == name) {
                    return pc;
                }
            }
        }
        return null;
    }

    static public bool HasPerspectiveCameraByName(this Omnity anOmnity, string name) {
        return anOmnity.FindPerspectiveCameraByName(name) != null;
    }

    #endregion PerspectiveCamera

    public static bool IsInLayerMask(int layer, LayerMask mask) {
        return ((mask.value & (1 << layer)) != 0);
    }
}
using OmnityMono;
using System.Xml.XPath;
using UnityEngine;

public class OmniSimpleWarpManager : OmnityTabbedFeature {
    static public OmnityPluginsIDs _myOmnityPluginsID = OmnityPluginsIDs.OmnitySimpleWarp;
    override public OmnityPluginsIDs myOmnityPluginsID { get { return _myOmnityPluginsID; } }

    static public OmniSimpleWarpManager OmniCreateSingleton(GameObject go) {
        return GetSingleton<OmniSimpleWarpManager>(ref singleton, go);
    }

    private static OmniSimpleWarpManager singleton;

    static public OmniSimpleWarpManager Get() {
        return singleton;
    }

    private void Awake() {
        if (VerifySingleton<OmniSimpleWarpManager>(ref singleton)) {
            currentBlender = null;
            Omnity.onReloadEndCallbackPriority.AddLoaderFunction(PriorityEventHandler.Ordering.Order_SimpleWarp, CoroutineLoader);
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
        blenders = LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroup_ExtraSettings_SimpleWarp>.ReadXMLAll(nav, Omnity.anOmnity);
        foreach (var b in blenders) {
            b.extraSettings.parent = b;
        }
    }

    public override System.Collections.IEnumerator PostLoad() {
        LinkCombiners(Omnity.anOmnity);
        yield break;
    }

    private void LinkCombiners(Omnity anOmnity) {
        Relink();
    }

    /// <param name="xmlWriter">The XML writer.</param>
    override public void WriteXMLDelegate(System.Xml.XmlTextWriter xmlWriter) {
        foreach (var group in blenders) {
            group.WriteXML(xmlWriter);
        }
    }

    override public string BaseName {
        get {
            return "SimpleWarpSettings";
        }
    }

    static public LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroup_ExtraSettings_SimpleWarp> currentBlender = null;
    static public System.Collections.Generic.List<LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroup_ExtraSettings_SimpleWarp>> blenders = new System.Collections.Generic.List<LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroup_ExtraSettings_SimpleWarp>>();

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

        foreach (LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroup_ExtraSettings_SimpleWarp> bb in blenders) {
            foreach (var b in bb.linked) {
                if(b==null|| b.myCamera == null) { continue; }
                var h = b.myCamera.GetComponent<OmniWarpCameraHelper>();
                if (h != null) {
                    h.enabled = bb.extraSettings.blendEnabled;
                }
            }
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Blend", GUILayout.Width(300))) {
            var b = new LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroup_ExtraSettings_SimpleWarp>();
            b.extraSettings.parent = b;
            blenders.Add(b);
        }
        GUILayout.EndHorizontal();
        SaveLoadGUIButtons(anOmnity);

        GUILayout.BeginHorizontal();
        LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroup_ExtraSettings_SimpleWarp>.DrawGUIAll(anOmnity, blenders);
        GUILayout.EndHorizontal();
    }

    public void Tabs(Omnity anOmnity) {
        GUILayout.BeginHorizontal();

        foreach (LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroup_ExtraSettings_SimpleWarp> bb in blenders) {
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
        if (needRelink) {
            Relink();
            needRelink = false;
        }
        if (currentBlender != null) {
            currentBlender.extraSettings._Update();
        }
    }

    public void Relink() {
        foreach (var fpc in Omnity.anOmnity.finalPassCameras) {
            var c = fpc.myCamera;
            if (c != null) {
                var list = c.gameObject.GetComponents<OmniWarpCameraHelper>();
                foreach (var l in list) {
                    GameObject.Destroy(l);
                }
            }
        }
        foreach (LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroup_ExtraSettings_SimpleWarp> group in blenders) {
            //    ChangedBlend(anOmnity, group, true);
            foreach (var v in group.linked) {
                var cameraHelper = v.myCameraTransform.gameObject.AddComponent<OmniWarpCameraHelper>();
                cameraHelper.Apply(group.extraSettings);
            }
        }
    }

    static public bool needRelink = false;
}

[System.Serializable]
public class LinkedFinalPassCameraGroup_ExtraSettings_SimpleWarp : LinkedFinalPassCameraGroup_ExtraSettingsInterface {

    //    public LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroup_ExtraSettings_SimpleWarp> parent;
    public LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroup_ExtraSettings_SimpleWarp> parent;

    public LinkedFinalPassCameraGroup_ExtraSettings_SimpleWarp() {
        Reset();
    }

    public void RemoveOrKeepGroupCallBack<T>(Omnity anOmnity, LinkedFinalPassCameraGroupBase<T> _linkedFinalPassCameraGroupBase, bool keepThis, bool linkSizeChanged) where T : LinkedFinalPassCameraGroup_ExtraSettingsInterface, new() {
        //        LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroup_ExtraSettingsInterface> linkedFinalPassCameraGroupBase = _linkedFinalPassCameraGroupBase as LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroup_ExtraSettingsInterface>;
        if (keepThis) {
        } else {
            OmniSimpleWarpManager.needRelink = true;
        }
        if (linkSizeChanged) {
            OmniSimpleWarpManager.needRelink = true;
        }
    }

    ~LinkedFinalPassCameraGroup_ExtraSettings_SimpleWarp() {
        OmniSimpleWarpManager.needRelink = true;
    }

    public Vector3[,] grid = new Vector3[2, 2];

    public int gridW = 6;
    public int gridH = 6;
    public float gamma = 1.0f;
    static public float ColumnAdjustRate = .1f;
    public int divisions = 6;

    public bool flip = false;

    public bool editing {
        get {
            return (OmniSimpleWarpManager.currentBlender != null && OmniSimpleWarpManager.currentBlender.extraSettings == this && OmniSimpleWarpManager.Get().enabled);
        }
    }

    public int editIndexX = 1;
    public int editIndexY = 1;

    public bool blendEnabled = true;
    public Material _blendMaterialMultiply = null;

    public Material blendMaterial {
        get {
            if (_blendMaterialMultiply == null) {
                _blendMaterialMultiply = OmnityMono.FinalPassSetup.ApplySettings(OmnityMono.FinalPassShaderType.OmniSimpleWarp);
            }
            return _blendMaterialMultiply;
        }
    }

    private Material _lineMaterial = null;

    public Material lineMaterial {
        get {
            if (_lineMaterial == null) {
                _lineMaterial = OmnityMono.FinalPassSetup.ApplySettings(OmnityMono.FinalPassShaderType.LineShader);
                _lineMaterial.hideFlags = HideFlags.HideAndDontSave;
                _lineMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
            }
            return _lineMaterial;
        }
    }

    /// UNKNOWN

    public void _WriteXML(System.Xml.XmlTextWriter xmlWriter) {
        xmlWriter.WriteStartElement("SimpleWarp");
        xmlWriter.WriteElementString("blendEnabled", blendEnabled.ToString());
        xmlWriter.WriteElementString("gamma", gamma.ToString("F4"));

        xmlWriter.WriteElementString("gridH", gridH.ToString());
        xmlWriter.WriteElementString("gridW", gridW.ToString());
        xmlWriter.WriteElementString("divisions", divisions.ToString());
        xmlWriter.WriteElementString("flip", flip.ToString());

        for (int iY = 0; iY < gridH; iY++) {
            for (int iX = 0; iX < gridW; iX++) {
                xmlWriter.WriteElementString("grid_" + iX + "_" + iY, grid[iX, iY].ToString("F4"));
            }
        }

        xmlWriter.WriteEndElement();
    }

    public void _ReadXML(System.Xml.XPath.XPathNavigator nav) {
        gamma = OmnityHelperFunctions.ReadElementFloatDefault(nav, ".//gamma", gamma);
        blendEnabled = OmnityHelperFunctions.ReadElementBoolDefault(nav, ".//blendEnabled", blendEnabled);
        flip = OmnityHelperFunctions.ReadElementBoolDefault(nav, ".//flip", flip);

        gridH = OmnityHelperFunctions.ReadElementIntDefault(nav, ".//gridH", 4);
        gridW = OmnityHelperFunctions.ReadElementIntDefault(nav, ".//gridW ", 4);
        divisions = OmnityHelperFunctions.ReadElementIntDefault(nav, ".//divisions", 6);
        UpdateGrid(gridW, gridH);

        for (int iY = 0; iY < gridH; iY++) {
            for (int iX = 0; iX < gridW; iX++) {
                grid[iX, iY] = OmnityHelperFunctions.ReadElementVector3Default(nav, ".//grid_" + iX + "_" + iY, grid[iX, iY]);
            }
        }
    }

    private void Reset() {
        gamma = 1;
        blendEnabled = true;
        divisions = 6;

        UpdateGrid(gridW, gridH);
    }

    private void UpdateGrid(int NewGridW, int NewGridH) {
        var newgrid = new Vector3[NewGridW, NewGridH];
        for (int iX = 0; iX < NewGridW; iX++) {
            float fX = (iX - 1) / (float)(NewGridW - 3);
            for (int iY = 0; iY < NewGridH; iY++) {
                float fY = (iY - 1) / (float)(NewGridH - 3);

                newgrid[iX, iY] = new Vector3(fX, fY, 1);
            }
        }
        gridH = NewGridH;
        gridW = NewGridW;
        grid = newgrid;

        editIndexX = gridW / 2;
        editIndexY = gridH / 2;
        selection.Clear();
    }

    public System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<int, int>> selection = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<int, int>>();

    public void WriteExtraSettings(System.Xml.XmlTextWriter xmlWriter) {
        _WriteXML(xmlWriter);
    }

    public void ReadExtraSettings(System.Xml.XPath.XPathNavigator currentgroup) {
        Reset();
        _ReadXML(currentgroup);
    }

    public enum MovementType {
        Selection = 0,
        MoveNode = 1,
        AdjustBrightness = 2
    }

    public void DrawGUIExtra(Omnity anOmnity) {
        GUI.skin.box.VerticalLayout(() => {
            GUI.skin.box.VerticalLayout(() => {
                blendEnabled = OmnityHelperFunctions.BoolInputReset("blendEnabled", blendEnabled, true);
            });

            GUI.skin.box.HorizontalLayout(() => {
                GUI.skin.box.VerticalLayout(() => {
                    GUILayout.Label("Move Cursor (arrows)");
                    GUI.skin.box.HorizontalLayout(() => {
                        if (UnityEngine.GUILayout.Button("^")) {
                            editIndexY += 1;
                            editIndexY = Mathf.Clamp(editIndexY, 0, grid.GetLength(1) - 1);
                        }
                    });
                    GUI.skin.box.HorizontalLayout(() => {
                        if (UnityEngine.GUILayout.Button("<")) {
                            editIndexX -= 1;
                            editIndexX = Mathf.Clamp(editIndexX, 0, grid.GetLength(0) - 1);
                        }

                        if (UnityEngine.GUILayout.Button(">")) {
                            editIndexX += 1;

                            editIndexX = Mathf.Clamp(editIndexX, 0, grid.GetLength(0) - 1);
                        }
                    });
                    GUI.skin.box.HorizontalLayout(() => {
                        if (UnityEngine.GUILayout.Button("v")) {
                            editIndexY -= 1;
                            editIndexY = Mathf.Clamp(editIndexY, 0, grid.GetLength(1) - 1);
                        }
                    });

                    GUI.skin.box.HorizontalLayout(() => {
                        if (UnityEngine.GUILayout.Button("Add/Remove to selection (space)")) {
                            AddRemoveToSelection();
                        }
                        if (UnityEngine.GUILayout.Button("Clear Selection")) {
                            selection.Clear();
                        }
                    });
                });

                GUI.skin.box.VerticalLayout(() => {
                    GUILayout.Label("Warp (Shift + Arrows)");
                    GUI.skin.box.HorizontalLayout(() => {
                        if (UnityEngine.GUILayout.RepeatButton("^")) {
                            Nudge(new Vector3(0, Time.deltaTime * .03f, 0));
                        }
                    });
                    GUI.skin.box.HorizontalLayout(() => {
                        if (UnityEngine.GUILayout.RepeatButton("<")) {
                            Nudge(new Vector3(-Time.deltaTime * .03f, 0, 0));
                        }

                        if (UnityEngine.GUILayout.RepeatButton(">")) {
                            Nudge(new Vector3(Time.deltaTime * .03f, 0, 0));
                        }
                    });
                    GUI.skin.box.HorizontalLayout(() => {
                        if (UnityEngine.GUILayout.RepeatButton("v")) {
                            Nudge(new Vector3(0, -Time.deltaTime * .03f, 0));
                        }
                    });

                    GUILayout.Label("Shade (Alt+ Up/Down)");
                    GUI.skin.box.HorizontalLayout(() => {
                        if (UnityEngine.GUILayout.RepeatButton("Darken")) {
                            Nudge(new Vector3(0, 0, -Time.deltaTime));
                        }
                        if (UnityEngine.GUILayout.RepeatButton("Lighten")) {
                            Nudge(new Vector3(0, 0, Time.deltaTime));
                        }
                    });
                });
            });
            GUI.skin.box.VerticalLayout(() => {
                int ih = gridH - 2;
                int iW = gridW - 2;

                if (OmnityHelperFunctions.IntInputResetWasChanged("Grid W", ref iW, 4)) {
                    iW = Mathf.Clamp(iW, 2, 20);
                    UpdateGrid(iW + 2, ih + 2);
                }
                if (OmnityHelperFunctions.IntInputResetWasChanged("Grid H", ref ih, 4)) {
                    ih = Mathf.Clamp(ih, 2, 20);
                    UpdateGrid(iW + 2, ih + 2);
                }
                divisions = Mathf.Clamp(OmnityHelperFunctions.IntInputReset("Refinement", divisions, 6), 2, 20);
            });
            GUI.skin.box.VerticalLayout(() => {
                gamma = OmnityHelperFunctions.FloatInputResetSlider("gamma", gamma, 0.5f, 1, 2f);
                flip = OmnityHelperFunctions.BoolInputReset("flip", flip, false);
            });
            GUI.skin.box.VerticalLayout(() => {
                if (GUILayout.Button("Reset This EdgeBlend")) {
                    Reset();
                }
            });
        });
    }

    public void Nudge(Vector3 vec) {
        if (selection.Count > 0) {
            foreach (var p in selection) {
                grid[p.Key, p.Value] += vec;
                grid[p.Key, p.Value].z = Mathf.Clamp01(grid[p.Key, p.Value].z);
            }
        } else {
            grid[editIndexX, editIndexY] += vec;
            grid[editIndexX, editIndexY].z = Mathf.Clamp01(grid[editIndexX, editIndexY].z);
        }
    }

    public void AddRemoveToSelection() {
        bool needstoadd = true;
        for (int i = 0; i < selection.Count; i++) {
            if (editIndexX == selection[i].Key && editIndexY == selection[i].Value) {
                selection.RemoveAt(i);
                needstoadd = false;
                break;
            }
        }
        if (needstoadd) {
            selection.Add(new System.Collections.Generic.KeyValuePair<int, int>(editIndexX, editIndexY));
        }
    }

    // Update is called once per frame
    public void _Update() {
        if (!Omnity.anOmnity.pluginIDs.Contains((int)OmnityPluginsIDs.OmnitySimpleWarp)) {
            return;
        }
        if (editing) {
            editIndexX = Mathf.Clamp(editIndexX, 0, grid.GetLength(0) - 1);
            editIndexY = Mathf.Clamp(editIndexY, 0, grid.GetLength(1) - 1);
            if (Input.GetKeyDown(KeyCode.Space)) {
                AddRemoveToSelection();
            }

            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) {
                float speed = .03f * (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ? 5 : 1);
                float distance = speed * Time.deltaTime;

                float speed2 = 1f * (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ? 1 : .5f);
                float distance2 = speed2 * Time.deltaTime;

                if (!(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))) {
                    distance = 0;
                }
                if (!(Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))) {
                    distance2 = 0;
                }

                if (Input.GetKey(KeyCode.LeftArrow)) {
                    Nudge(new Vector3(-distance, 0, 0));
                }
                if (Input.GetKey(KeyCode.RightArrow)) {
                    Nudge(new Vector3(distance, 0, 0));
                }
                if (Input.GetKey(KeyCode.UpArrow)) {
                    Nudge(new Vector3(0, distance, distance2));
                }
                if (Input.GetKey(KeyCode.DownArrow)) {
                    Nudge(new Vector3(0, -distance, -distance2));
                }
            } else {
                if (Input.GetKeyDown(KeyCode.LeftArrow)) {
                    editIndexX -= 1;
                }
                if (Input.GetKeyDown(KeyCode.RightArrow)) {
                    editIndexX += 1;
                }
                if (Input.GetKeyDown(KeyCode.UpArrow)) {
                    editIndexY += 1;
                }
                if (Input.GetKeyDown(KeyCode.DownArrow)) {
                    editIndexY -= 1;
                }
            }
        }
    }
}

public class OmniWarpCameraHelper : MonoBehaviour {
    public LinkedFinalPassCameraGroup_ExtraSettings_SimpleWarp settings = null;

    internal void Apply(LinkedFinalPassCameraGroup_ExtraSettings_SimpleWarp linkedFinalPassCameraGroup_ExtraSettings_SimpleWarp) {
        //        Debug.Log("Settings connected");
        settings = linkedFinalPassCameraGroup_ExtraSettings_SimpleWarp;
        mat = settings.blendMaterial;
        lineMat = settings.lineMaterial;
        enabled = settings.blendEnabled;
    }

    public Material mat;
    public Material lineMat;

    private void OnRenderImage(RenderTexture src, RenderTexture dest) {
        if (settings == null) {
            Debug.LogWarning("Reload needed");
            return;
        }
        mat.mainTexture = src;
        mat.SetFloat("_Gamma", settings.gamma);

        RenderTexture.active = dest;
        GL.Clear(true, true, Color.black);
        GL.PushMatrix();
        GL.LoadOrtho();
        //if(Camera.main!=null)
        //Camera.main.projectionMatrix = Camera.main.projectionMatrix * Matrix4x4.Scale(new Vector3(1, -1, 1));

        // todo submit as batch...
        OmniSpline2DExtensions.DrawGridWithPaddingQuadSpline(settings.grid, mat, settings.divisions, settings.flip);
        if (settings.editing) {
            OmniSpline2DExtensions.DrawGridWithPaddingLines(settings.grid, lineMat);
            OmniSpline2DExtensions.DrawCursor(settings.grid, lineMat, settings.editIndexX, settings.editIndexY);
            OmniSpline2DExtensions.DrawCursor(settings.grid, lineMat, settings.selection);
        }
        GL.PopMatrix();
    }
}
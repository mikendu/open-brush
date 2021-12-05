using OmnityMono.EdgeBlenderExtensions;
using System.Linq;
using System.Xml.XPath;
using UnityEngine;

public class OmniEdgeBlenderManager : OmnityTabbedFeature {
    static public OmnityPluginsIDs _myOmnityPluginsID = OmnityPluginsIDs.EdgeBlender;
    override public OmnityPluginsIDs myOmnityPluginsID { get { return _myOmnityPluginsID; } }

    static public OmniEdgeBlenderManager OmniCreateSingleton(GameObject go) {
        return GetSingleton<OmniEdgeBlenderManager>(ref singleton, go);
    }

    private static OmniEdgeBlenderManager singleton;

    static public OmniEdgeBlenderManager Get() {
        return singleton;
    }

    private void Awake() {
        if (VerifySingleton<OmniEdgeBlenderManager>(ref singleton)) {
            currentBlender = null;
            Omnity.onReloadEndCallbackPriority.AddLoaderFunction(PriorityEventHandler.Ordering.Order_Edgeblender, CoroutineLoader);
            Omnity.onLoadInMemoryConfigStart += (Omnity anOmnity) => {
                if (anOmnity.PluginEnabled(OmnityPluginsIDs.EdgeBlender)) {
                }
            };
            Omnity.onSaveCompleteCallback += Save;
        }
    }

    public override void Unload() {
        base.Unload();
        currentBlender = null;
        foreach (var b in blenders) {
            b.extraSettings.ClearListOfEdgeBlenderBehaviors();
        }
        blenders.Clear();
    }

    public override void ReadXMLDelegate(XPathNavigator nav) {
        blenders = LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroup_ExtraSettings_EdgeBlender>.ReadXMLAll(nav, Omnity.anOmnity);
        foreach (var b in blenders) {
            b.extraSettings.parent = b;
        }
    }

    public override System.Collections.IEnumerator PostLoad() {
        LinkCombiners(Omnity.anOmnity);
        yield break;
    }

    private void LinkCombiners(Omnity anOmnity) {
        foreach (LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroup_ExtraSettings_EdgeBlender> group in blenders) {
            ChangedBlend(anOmnity, group, true);
        }
    }

    public void ChangedBlend(Omnity anOmnity, LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroup_ExtraSettings_EdgeBlender> newgroup, bool removeIfFalse) {
        foreach (FinalPassCamera lfpc in newgroup.linked) {
            if (lfpc == null) {
                Debug.Log("error");
                continue;
            }

            if (lfpc.myCamera != null) {
                GameObject go = lfpc.myCamera.gameObject;
                OmniEdgeBlender omniEdgeBlender = null;
                if (!newgroup.extraSettings.listOfEdgeBlenderBehaviors.TryGetValue(go, out omniEdgeBlender)) {
                    omniEdgeBlender = go.AddComponent<OmniEdgeBlender>();
                    newgroup.extraSettings.listOfEdgeBlenderBehaviors[go] = omniEdgeBlender;
                }
                omniEdgeBlender.Apply(newgroup.extraSettings);
            }
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
            return "EdgeBlenderSettings";
        }
    }

    static public LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroup_ExtraSettings_EdgeBlender> currentBlender = null;
    static public System.Collections.Generic.List<LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroup_ExtraSettings_EdgeBlender>> blenders = new System.Collections.Generic.List<LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroup_ExtraSettings_EdgeBlender>>();

    private void Hide() {
        currentBlender = null;
        foreach (LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroup_ExtraSettings_EdgeBlender> blender in blenders) {
            blender.extraSettings.EditingMesh = false;
        }
    }

    override public void OnCloseGUI(Omnity anOmnity) {
        Hide();
    }

    public override void MyGuiCallback(Omnity anOmnity) {
        if (!anOmnity.pluginIDs.Contains((int)OmnityPluginsIDs.EdgeBlender)) {
            return;
        }
        if (blenders.Count == 0) {
            currentBlender = null;
        }
        Tabs(anOmnity);
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Disable All")) {
            foreach (LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroup_ExtraSettings_EdgeBlender> bb in blenders) {
                bb.extraSettings.blendEnabled = false;
            }
        }
        if (GUILayout.Button("Enable All")) {
            foreach (LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroup_ExtraSettings_EdgeBlender> bb in blenders) {
                bb.extraSettings.blendEnabled = true;
            }
        }

        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (currentBlender != null) {
            currentBlender.extraSettings.DrawGUIExtra(anOmnity);
            OmnityHelperFunctions.BR();
            foreach (var ss in anOmnity.finalPassCameras) {
                if (currentBlender.linked.Contains(ss)) {
                    if (ss.renderTextureSettings.enabled) {
                        ss.renderTextureSettings.OnGUI(false, 256, false);
                    } else {
                        GUILayout.Label(ss.name);
                    }
                }
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Blend", GUILayout.Width(300))) {
            var b = new LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroup_ExtraSettings_EdgeBlender>();
            b.extraSettings.parent = b;
            blenders.Add(b);
        }
        GUILayout.EndHorizontal();
        SaveLoadGUIButtons(anOmnity);

        GUILayout.BeginHorizontal();
        LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroup_ExtraSettings_EdgeBlender>.DrawGUIAll(anOmnity, blenders);
        GUILayout.EndHorizontal();
    }

    public void Tabs(Omnity anOmnity) {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(currentBlender == null ? "[Hide]" : "Hide")) {
            Hide();
        }
        foreach (LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroup_ExtraSettings_EdgeBlender> bb in blenders) {
            if (GUILayout.Button(currentBlender == bb ? "[" + bb.name + "]" : bb.name)) {
                Hide();
                currentBlender = bb;
                bb.extraSettings.EditingMesh = true;
            }
        }
        GUILayout.EndHorizontal();
    }

    // Use this for initialization
    private void Start() {
    }
}

public class OmniEdgeBlender : MonoBehaviour {
    public LinkedFinalPassCameraGroup_ExtraSettings_EdgeBlender settings = null;

    internal void Apply(LinkedFinalPassCameraGroup_ExtraSettings_EdgeBlender linkedFinalPassCameraGroup_ExtraSettings_EdgeBlender) {
        //        Debug.Log("Settings connected");
        settings = linkedFinalPassCameraGroup_ExtraSettings_EdgeBlender;
    }

    public void OnDestroy() {
        if (settings != null) {
            GameObject.Destroy(settings.blenderPatch);
        }
    }

    public void Start() {
    }

    private void Update() {
        if (settings != null && settings.EditingMesh) {
            settings.CheckKeyboard();
        }
    }

    private void OnPostRender() {
        if (settings != null) {
            settings.MyPostRender();
        }
    }
}

[System.Serializable]
public class LinkedFinalPassCameraGroup_ExtraSettings_EdgeBlender : LinkedFinalPassCameraGroup_ExtraSettingsInterface {

    //    public LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroup_ExtraSettings_EdgeBlender> parent;
    public LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroup_ExtraSettings_EdgeBlender> parent;

    public LinkedFinalPassCameraGroup_ExtraSettings_EdgeBlender() {
        Reset();
    }

    #region meshsettings

    public void MyPostRender() {
        if (blendEnabled) {
            GL.PushMatrix();
            GL.LoadOrtho();
            DrawBlenderBlend();
            if (EditingMesh) {
                DrawBlenderGrid();
            }
            GL.PopMatrix();
        }
    }

    // save
    [System.NonSerialized]
    public System.Collections.Generic.Dictionary<GameObject, OmniEdgeBlender> listOfEdgeBlenderBehaviors = new System.Collections.Generic.Dictionary<GameObject, OmniEdgeBlender>();

    public void ClearListOfEdgeBlenderBehaviors() {
        foreach (var ebb in listOfEdgeBlenderBehaviors.Values) {
            GameObject.Destroy(ebb);
        }
        listOfEdgeBlenderBehaviors.Clear();
    }

    // dont save
    public Mesh blenderPatch = new Mesh();

    public bool blendEnabled = true;
    public Material _blendMaterialAdd = null;
    public Material _blendMaterialMultiply = null;
    public Material _blendMaterialSubtract = null;

    public Material blendMaterial {
        get {
            switch (blendOperator) {
                case BlendOperator.Multiply_For_Blending:
                    if (_blendMaterialMultiply == null) {
                        _blendMaterialMultiply = OmnityMono.FinalPassSetup.ApplySettings(OmnityMono.FinalPassShaderType.OmniEdgeBlendShaderMultiply);
                    }
                    return _blendMaterialMultiply;

                case BlendOperator.Add_For_Black_Level_Matching:
                    if (_blendMaterialAdd == null) {
                        _blendMaterialAdd = OmnityMono.FinalPassSetup.ApplySettings(OmnityMono.FinalPassShaderType.OmniEdgeBlendShaderAdd);
                    }
                    return _blendMaterialAdd;

                case BlendOperator.Subtract_For_Black_levelMatching_across3XProjectors:
                    if (_blendMaterialSubtract == null) {
                        _blendMaterialSubtract = OmnityMono.FinalPassSetup.ApplySettings(OmnityMono.FinalPassShaderType.OmniEdgeBlendShaderSubtract);
                    }
                    return _blendMaterialSubtract;

                default:
                    Debug.LogError("Couldn't find " + blendOperator);
                    break;
            }
            if (_blendMaterialMultiply == null) {
                _blendMaterialMultiply = OmnityMono.FinalPassSetup.ApplySettings(OmnityMono.FinalPassShaderType.OmniEdgeBlendShaderMultiply);
            }
            return _blendMaterialMultiply;
        }
    }

    private static Material _lineMaterial = null;

    private static Material lineMaterial {
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
    static public float ColumnAdjustRate = .1f;

    private System.Collections.Generic.List<Vector3> newVertices = new System.Collections.Generic.List<Vector3>();
    private System.Collections.Generic.List<Vector2> newUV = new System.Collections.Generic.List<Vector2>();
    private System.Collections.Generic.List<int> newTriangles = new System.Collections.Generic.List<int>();
    private System.Collections.Generic.List<Color> newColors = new System.Collections.Generic.List<Color>();

    public System.Collections.Generic.List<System.Collections.Generic.List<OmniEdgeBlendNode>> nodes = new System.Collections.Generic.List<System.Collections.Generic.List<OmniEdgeBlendNode>>();

    [System.Serializable]
    public class OmniEdgeBlendNode {
        public Vector2 _normalizedPosition = Vector2.zero;

        public Vector2 normalizedPosition {
            get {
                return _normalizedPosition;
            }
            set {
                _normalizedPosition = new Vector2(Mathf.Clamp01(value.x), Mathf.Clamp01(value.y));
            }
        }

        public float factor = 1.0f;

        public Vector3 normalizedPositionV3 {
            get {
                return new Vector3(normalizedPosition.x, normalizedPosition.y, 0);
            }
        }

        public Vector2 XYtoUV {
            get {
                return new Vector2(ix / (float)(parent.nodes[0].Count - 1), iy / (float)(parent.nodes.Count - 1));
            }
        }

        public Color normalizedColor {
            get {
                return new Color(factor, factor, factor);
            }
        }

        public LinkedFinalPassCameraGroup_ExtraSettings_EdgeBlender parent;

        internal void _WriteReadXML(System.Xml.XmlTextWriter xmlWriter, System.Xml.XPath.XPathNavigator nav) {
            if (xmlWriter != null) {
                xmlWriter.WriteStartElement(XMLElement);
                xmlWriter.WriteElementString("normalizedPosition", _normalizedPosition.ToString("F4"));
                xmlWriter.WriteElementString("factor", factor.ToString("F4"));
                xmlWriter.WriteEndElement();
            } else {
                factor = OmnityHelperFunctions.ReadElementFloatDefault(nav, ".//factor", factor);
                _normalizedPosition = OmnityHelperFunctions.ReadElementVector2Default(nav, ".//normalizedPosition", Vector2.zero);
                computing = true;
            }
        }

        public const string XMLElement = "Node";
        private int ix;
        private int iy;

        public OmniEdgeBlendNode(int ix, int iy, LinkedFinalPassCameraGroup_ExtraSettings_EdgeBlender parent, Vector2 normalizedPosition) {
            this.ix = ix;
            this.iy = iy;
            this.parent = parent;
            this.normalizedPosition = normalizedPosition;
            computing = true;
        }

        private static float[,] gridX = new float[4, 4];
        private static float[,] gridY = new float[4, 4];
        private static float[,] gridZ = new float[4, 4];

        private Color[,,] colors = new Color[5 - 1, 5 - 1, 4];
        private Vector3[,,] positions = new Vector3[5 - 1, 5 - 1, 4];

        public bool computing = true;

        private void RetesselateQuad(BlendOperator blendOperator) {
            int amount = 5;
            if (computing) {
                for (int iyGrid = 0; iyGrid < 4; iyGrid++) {
                    for (int ixGrid = 0; ixGrid < 4; ixGrid++) {
                        int iXD = Mathf.Clamp(ix - 1 + ixGrid, 0, parent.nodes[0].Count - 1);
                        int iYD = Mathf.Clamp(iy - 1 + iyGrid, 0, parent.nodes.Count - 1);

                        Vector2 pos = parent.nodes[iYD][iXD].normalizedPosition;
                        gridX[ixGrid, iyGrid] = pos.x;
                        gridY[ixGrid, iyGrid] = pos.y;
                        gridZ[ixGrid, iyGrid] = parent.nodes[iYD][iXD].factor;
                    }
                }

                for (int iY = 0; iY < amount - 1; iY++) {
                    float y0 = iY / (float)(amount - 1);      // 0/2   1/2
                    float y1 = (iY + 1) / (float)(amount - 1);    // 1/2  2/2

                    for (int iX = 0; iX < amount - 1; iX++) {
                        float x0 = iX / (float)(amount - 1);
                        float x1 = (iX + 1) / (float)(amount - 1);

                        float X00 = BicubicInterpolator.getValuexy(gridX, x0, y0);
                        float X01 = BicubicInterpolator.getValuexy(gridX, x0, y1);
                        float X11 = BicubicInterpolator.getValuexy(gridX, x1, y1);
                        float X10 = BicubicInterpolator.getValuexy(gridX, x1, y0);

                        float Y00 = BicubicInterpolator.getValuexy(gridY, x0, y0);
                        float Y01 = BicubicInterpolator.getValuexy(gridY, x0, y1);
                        float Y11 = BicubicInterpolator.getValuexy(gridY, x1, y1);
                        float Y10 = BicubicInterpolator.getValuexy(gridY, x1, y0);

                        float Z00 = BicubicInterpolator.getValuexy(gridZ, x0, y0);
                        float Z01 = BicubicInterpolator.getValuexy(gridZ, x0, y1);
                        float Z11 = BicubicInterpolator.getValuexy(gridZ, x1, y1);
                        float Z10 = BicubicInterpolator.getValuexy(gridZ, x1, y0);

                        GL.Color(new Color(Z00, Z00, Z00));
                        GL.Vertex(new Vector3(X00, Y00, 0));

                        GL.Color(new Color(Z01, Z01, Z01));
                        GL.Vertex(new Vector3(X01, Y01, 0));

                        GL.Color(new Color(Z11, Z11, Z11));
                        GL.Vertex(new Vector3(X11, Y11, 0));

                        GL.Color(new Color(Z10, Z10, Z10));
                        GL.Vertex(new Vector3(X10, Y10, 0));

                        colors[iY, iX, 0] = new Color(Z00, Z00, Z00);
                        colors[iY, iX, 1] = new Color(Z01, Z01, Z01);
                        colors[iY, iX, 2] = new Color(Z11, Z11, Z11);
                        colors[iY, iX, 3] = new Color(Z10, Z10, Z10);

                        positions[iY, iX, 0] = new Vector3(X00, Y00, 0);
                        positions[iY, iX, 1] = new Vector3(X01, Y01, 0);
                        positions[iY, iX, 2] = new Vector3(X11, Y11, 0);
                        positions[iY, iX, 3] = new Vector3(X10, Y10, 0);
                    }
                }
            }
            for (int iY = 0; iY < amount - 1; iY++) {
                for (int iX = 0; iX < amount - 1; iX++) {
                    if (BlendOperator.Multiply_For_Blending == blendOperator && colors[iY, iX, 0] == myWhite && colors[iY, iX, 1] == myWhite && colors[iY, iX, 2] == myWhite && colors[iY, iX, 3] == myWhite) {
                    } else {
                        GL.Color(colors[iY, iX, 0]);
                        GL.Vertex(positions[iY, iX, 0]);

                        GL.Color(colors[iY, iX, 1]);
                        GL.Vertex(positions[iY, iX, 1]);

                        GL.Color(colors[iY, iX, 2]);
                        GL.Vertex(positions[iY, iX, 2]);

                        GL.Color(colors[iY, iX, 3]);
                        GL.Vertex(positions[iY, iX, 3]);
                    }
                }
            }
            computing = false;
        }

        private readonly Color myWhite = new Color(1, 1, 1);

        internal void DrawQuad(BlendOperator bo) {
            RetesselateQuad(bo);
        }

        internal void DrawMeshAsQuads() {
            if (ix != 0 && iy != 0) {
                var n00 = parent.nodes[iy - 1][ix - 1];
                var n10 = parent.nodes[iy][ix - 1];
                var n11 = parent.nodes[iy][ix];
                var n01 = parent.nodes[iy - 1][ix];

                DrawLineAsQuad(n00, n10);
                DrawLineAsQuad(n10, n11);
                DrawLineAsQuad(n11, n01);
                DrawLineAsQuad(n01, n00);
            }
        }

        public bool isSelected = false;

        internal void DrawPointSelector() {
            var pn = new Vector3(parent.nodes[iy][ix].normalizedPosition.x, parent.nodes[iy][ix].normalizedPosition.y, 0);
            float size = 0.01f;
            DrawDiamond(pn, isCurrentUnderCursor ? size * 3 : size, isSelected ? Color.green : Color.red);
        }

        private void DrawDiamond(Vector3 pn, float size, Color color) {
            GLExtensionsOmnity.GLDraw(GL.QUADS, () => {
                GL.Color(color);
                GL.Vertex(pn + Vector3.left * size);
                GL.Vertex(pn + Vector3.up * size);
                GL.Vertex(pn + Vector3.right * size);
                GL.Vertex(pn + Vector3.down * size);
            });
        }

        private void DrawDiamondOutline(Vector3 pn, float size, float width, Color color) {
            GLExtensionsOmnity.GLDraw(GL.QUADS, () => {
                GL.Color(color);
                DrawLineAsQuad(pn + Vector3.left * size, pn + Vector3.up * size, width);
                DrawLineAsQuad(pn + Vector3.up * size, pn + Vector3.right * size, width);
                DrawLineAsQuad(pn + Vector3.right * size, pn + Vector3.down * size, width);
                DrawLineAsQuad(pn + Vector3.down * size, pn + Vector3.left * size, width);
            });
        }

        private bool isCurrentUnderCursor {
            get {
                return ix == parent.ixSelected && iy == parent.iySelected;
            }
        }

        public void DrawLineAsQuad(OmniEdgeBlendNode n0, OmniEdgeBlendNode n1) {
            var pn0 = new Vector3(n0.normalizedPosition.x, n0.normalizedPosition.y, 0);
            var pn1 = new Vector3(n1.normalizedPosition.x, n1.normalizedPosition.y, 0);
            float width = .002f;
            var perpendicular = new Vector3(pn1.y - pn0.y, pn1.x - pn0.x, 0).normalized * width;

            GL.Color(Color.white * n0.factor);
            GL.Vertex(pn0 - perpendicular);
            GL.Vertex(pn0 + perpendicular);
            GL.Color(Color.white * n1.factor);
            GL.Vertex(pn1 + perpendicular);
            GL.Vertex(pn1 - perpendicular);
        }

        public void DrawLineAsQuad(Vector3 pn0, Vector3 pn1, float width = .002f) {
            var perpendicular = new Vector3(pn1.y - pn0.y, pn1.x - pn0.x, 0).normalized * width;

            GL.Vertex(pn0 - perpendicular);
            GL.Vertex(pn0 + perpendicular);
            GL.Vertex(pn1 + perpendicular);
            GL.Vertex(pn1 - perpendicular);
        }

        public void OnGUI() {
            /*
            if (Random.value < .001f) {
                Debug.Log("Possible flip here");
            }
            if (parent == null || parent.parent == null) {
                Debug.Log("Forgot to connect parent");
                return;
            }
            foreach (var fpc in parent.parent.linked) {
                Rect r = fpc.normalizedViewportRect;
                Rect buttonInset = new Rect(normalizedPosition.x, normalizedPosition.y, .05f, .05f);
                Rect normalizedPositionOffset = r.Transform_NormalizedRect_ToFitInsideThis(buttonInset);
                Rect pixelSize = normalizedPositionOffset.NormalizedViewportRectToPixels();

                if (GUI.RepeatButton(pixelSize, "button")) {
                    Debug.Log("pressed");
                }
            }
             * */
            computing = true;
        }
    }

    public float gamma = 1.0f;
    public float add = .10f;
    public float subtract = .10f;

    #endregion meshsettings

    #region meshfunctions

    public void _WriteXML(System.Xml.XmlTextWriter xmlWriter) {
        xmlWriter.WriteStartElement("EdgeBlender");
        xmlWriter.WriteElementString("gamma", gamma.ToString("F4"));
        xmlWriter.WriteElementString("add", add.ToString("F4"));
        xmlWriter.WriteElementString("subtract", subtract.ToString("F4"));
        xmlWriter.WriteElementString("blendEnabled", blendEnabled.ToString());

        xmlWriter.WriteElementString("nodes.width", nodes[0].Count.ToString());
        xmlWriter.WriteElementString("nodes.height", nodes.Count.ToString());
        xmlWriter.WriteElementString("blendOperator", ((int)blendOperator).ToString());

        ForEach<OmniEdgeBlendNode>(EveryNode(), (n) => {
            n._WriteReadXML(xmlWriter, null);
        });

        xmlWriter.WriteEndElement();
    }

    private int nodesH {
        get {
            return nodes.Count;
        }
    }

    private int nodesW {
        get {
            if (nodes.Count > 0) {
                return nodes[0].Count;
            } else {
                return 0;
            }
        }
    }

    private void EnsureNodesAreThere() {
        if (nodesH < 2 || nodesW < 2) {
            ResetNodes(defaultW, defaultH);
        }
    }

    private void ResetNodes(int w, int h) {
        w = Mathf.Max(w, 2);
        h = Mathf.Max(h, 2);

        for (int iY = 0; iY < nodes.Count; iY++) {
            nodes[iY].Clear();
        }
        nodes.Clear();
        for (int iy = 0; iy < h; iy++) {
            nodes.Add(new System.Collections.Generic.List<OmniEdgeBlendNode>());
            for (int ix = 0; ix < w; ix++) {
                Vector2 np = new Vector2(.25f + .5f * (ix / (float)(w - 1)), .25f + .5f * (iy / (float)(h - 1)));
                nodes[iy].Add(new OmniEdgeBlendNode(ix, iy, this, np));
            }
        }
    }

    public void _ReadXML(System.Xml.XPath.XPathNavigator nav) {
        gamma = OmnityHelperFunctions.ReadElementFloatDefault(nav, ".//gamma", gamma);
        add = OmnityHelperFunctions.ReadElementFloatDefault(nav, ".//add", add);
        subtract = OmnityHelperFunctions.ReadElementFloatDefault(nav, ".//subtract", subtract);
        blendEnabled = OmnityHelperFunctions.ReadElementBoolDefault(nav, ".//blendEnabled", blendEnabled);

        int height = OmnityHelperFunctions.ReadElementIntDefault(nav, ".//nodes.height", 8);
        int width = OmnityHelperFunctions.ReadElementIntDefault(nav, ".//nodes.width", 5);

        blendOperator = (BlendOperator)OmnityHelperFunctions.ReadElementIntDefault(nav, ".//blendOperator", (int)BlendOperator.Multiply_For_Blending);

        ResetNodes(width, height);

        System.Xml.XPath.XPathNodeIterator XYStartItor = nav.Select("(.//" + OmniEdgeBlendNode.XMLElement + ")");

        try {
            ForEach<OmniEdgeBlendNode>(EveryNode(), (n) => {
                XYStartItor.MoveNext();
                n._WriteReadXML(null, XYStartItor.Current);
            });
        } catch (System.Exception e) {
            Debug.LogError("Exception With Read xml omni edge blender " + e.Message);
        }

        ApplyMesh();
    }

    private const int defaultW = 4;
    private const int defaultH = 4;

    private void Reset() {
        gamma = 1;
        ResetNodes(defaultW, defaultH);
        ApplyMesh();
    }

    private void ApplyMesh() {
        EnsureNodesAreThere();

        newVertices.Clear();
        newTriangles.Clear();
        newUV.Clear();
        newColors.Clear();

        if (blenderPatch == null) {
            blenderPatch = new Mesh();
        }
        blenderPatch.Clear();

        for (int iY = 1; iY < nodes.Count; iY++) {
            for (int iX = 1; iX < nodes[iY].Count; iX++) {
                var n00 = nodes[iY - 1][iX - 1];
                var n10 = nodes[iY - 1][iX];
                var n11 = nodes[iY][iX];
                var n01 = nodes[iY][iX - 1];

                newVertices.Add(n00.normalizedPositionV3);
                newVertices.Add(n10.normalizedPositionV3);
                newVertices.Add(n11.normalizedPositionV3);
                newVertices.Add(n01.normalizedPositionV3);

                newColors.Add(new Color(n00.normalizedPosition.x, n00.normalizedPosition.y, 0 * n00.factor));
                newColors.Add(new Color(n10.normalizedPosition.x, n10.normalizedPosition.y, 0 * n10.factor));
                newColors.Add(new Color(n11.normalizedPosition.x, n11.normalizedPosition.y, 0 * n11.factor));
                newColors.Add(new Color(n01.normalizedPosition.x, n01.normalizedPosition.y, 0 * n01.factor));

                newUV.Add(n00.XYtoUV);
                newUV.Add(n10.XYtoUV);
                newUV.Add(n11.XYtoUV);
                newUV.Add(n01.XYtoUV);

                int Index00 = newVertices.Count - 4;
                int Index10 = newVertices.Count - 3;
                int Index11 = newVertices.Count - 2;
                int Index01 = newVertices.Count - 1;

                newTriangles.Add(Index00);
                newTriangles.Add(Index10);
                newTriangles.Add(Index11);

                newTriangles.Add(Index00);
                newTriangles.Add(Index11);
                newTriangles.Add(Index01);
            }
        }

        blenderPatch.vertices = newVertices.ToArray();
        blenderPatch.colors = newColors.ToArray();
        blenderPatch.uv = newUV.ToArray();
        blenderPatch.triangles = newTriangles.ToArray();
        OmnityPlatformDefines.OptimizeMesh(blenderPatch);
        switch (blendOperator) {
            case BlendOperator.Multiply_For_Blending:
                blendMaterial.SetFloat("_Value", gamma);
                break;

            case BlendOperator.Add_For_Black_Level_Matching:
                blendMaterial.SetFloat("_Value", add);
                break;

            case BlendOperator.Subtract_For_Black_levelMatching_across3XProjectors:
                blendMaterial.SetFloat("_Value", subtract);
                break;

            default:
                Debug.LogError("Unknown blend mode");
                break;
        }

        // Debug.LogWarning("todo make mesh");
    }

    #endregion meshfunctions

    #region morefunctions

    private void DrawBlenderBlend() {
        blendMaterial.SetPass(0);
        int w = nodes[0].Count;
        int h = nodes.Count;
        GL.Begin(GL.QUADS);
        for (int y = 0; y < h - 1; y++) {
            for (int x = 0; x < w - 1; x++) {
                nodes[y][x].DrawQuad(blendOperator);
            }
        }
        GL.End();
    }

    private void DrawBlenderGrid() {
        lineMaterial.SetPass(0);
        GLExtensionsOmnity.GLDraw(GL.QUADS, () => {
            ForEach<OmniEdgeBlendNode>(EveryNode(), (n) => {
                n.DrawMeshAsQuads();
            });
        });
        ForEach<OmniEdgeBlendNode>(EveryNode(), (n) => {
            n.DrawPointSelector();
        });
    }

    #endregion morefunctions

    ////////////////////////
    public void RemoveOrKeepGroupCallBack<T>(Omnity anOmnity, LinkedFinalPassCameraGroupBase<T> _linkedFinalPassCameraGroupBase, bool keepThis, bool linkSizeChanged) where T : LinkedFinalPassCameraGroup_ExtraSettingsInterface, new() {
        //        LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroup_ExtraSettingsInterface> linkedFinalPassCameraGroupBase = _linkedFinalPassCameraGroupBase as LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroup_ExtraSettingsInterface>;
        if (keepThis) {
        } else {
        }
    }

    public void WriteExtraSettings(System.Xml.XmlTextWriter xmlWriter) {
        _WriteXML(xmlWriter);
    }

    public void ReadExtraSettings(System.Xml.XPath.XPathNavigator currentgroup) {
        Reset();
        _ReadXML(currentgroup);
    }

    private int negmod(int x, int m) {
        if (m == 0) {
            return -1;
        }
        return (x % m + m) % m;
    }

    private System.Collections.Generic.IEnumerable<OmniEdgeBlendNode> FindEachSelectedNodeOrCurrentNodeIfNoneAreSelected() {
        bool foundOne = false;
        if (nodes.Count == 0 || nodes[0].Count == 0) {
            yield break;
        }

        for (int iY = 0; iY < nodes.Count; iY++) {
            for (int iX = 0; iX < nodes[iY].Count; iX++) {
                if (nodes[iY][iX].isSelected) {
                    yield return nodes[iY][iX];
                    foundOne = true;
                }
            }
        }
        if (!foundOne) {
            yield return nodes[iySelected][ixSelected];
        }
    }

    private System.Collections.Generic.IEnumerable<OmniEdgeBlendNode> FindEachSelectedNode() {
        for (int iY = 0; iY < nodes.Count; iY++) {
            for (int iX = 0; iX < nodes[iY].Count; iX++) {
                if (nodes[iY][iX].isSelected) {
                    yield return nodes[iY][iX];
                }
            }
        }
    }

    private System.Collections.Generic.IEnumerable<OmniEdgeBlendNode> EveryNode() {
        for (int iY = 0; iY < nodes.Count; iY++) {
            for (int iX = 0; iX < nodes[iY].Count; iX++) {
                yield return nodes[iY][iX];
            }
        }
    }

    public static void ForEach<T>(System.Collections.Generic.IEnumerable<T> source, System.Action<T> action) {
        //        source.ThrowIfNull("source");
        //      action.ThrowIfNull("action");
        foreach (T element in source) {
            action(element);
        }
    }

    private bool okToCheckKeyboard = false;

    private float posMoveSpeed = .02f;

    public void CheckKeyboard() {
        if (!okToCheckKeyboard) {
            return;
        }
        okToCheckKeyboard = false;

        //        bool isOkToEdit = tEventType.Repaint==Event.current.type;
        bool adjustSelection = false;
        bool adjustPosition = false;
        bool moveFast = false;

        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift)) {
            adjustPosition = true;
            moveFast = true;
        } else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftShift)) {
            adjustPosition = true;
        } else {
            adjustSelection = true;
        }

        if (adjustSelection) {
            if (Input.GetKeyDown(KeyCode.UpArrow)) {
                iySelected++;
                if (moveFast) {
                    nodes[iySelected][ixSelected].isSelected = true;
                }
            }
            if (Input.GetKeyDown(KeyCode.DownArrow)) {
                iySelected--;
                if (moveFast) {
                    nodes[iySelected][ixSelected].isSelected = true;
                }
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow)) {
                ixSelected--;
                if (moveFast) {
                    nodes[iySelected][ixSelected].isSelected = true;
                }
            }
            if (Input.GetKeyDown(KeyCode.RightArrow)) {
                ixSelected++;
                if (moveFast) {
                    nodes[iySelected][ixSelected].isSelected = true;
                }
            }
            if (Input.GetKeyDown(KeyCode.Space)) {
                nodes[iySelected][ixSelected].isSelected = !nodes[iySelected][ixSelected].isSelected;
            }
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.BackQuote)) {
                ForEach<OmniEdgeBlendNode>(EveryNode(), (n) => {
                    n.isSelected = false;
                });
            }
        } else if (adjustPosition) {
            float x = 0, y = 0;
            if (Input.GetKey(KeyCode.UpArrow)) {
                y = 1;
            }
            if (Input.GetKey(KeyCode.DownArrow)) {
                y = -1;
            }
            if (Input.GetKey(KeyCode.LeftArrow)) {
                x = -1;
            }
            if (Input.GetKey(KeyCode.RightArrow)) {
                x = 1;
            }
            if (x != 0 || y != 0) {
                ForEach<OmniEdgeBlendNode>(FindEachSelectedNodeOrCurrentNodeIfNoneAreSelected(), (n) => {
                    Vector2 offset = new Vector2(x, y) * Time.deltaTime * (moveFast ? (posMoveSpeed * 5) : posMoveSpeed);
                    Debug.LogWarning("offset " + offset.ToString());

                    n.normalizedPosition += new Vector2(x, y) * Time.deltaTime * (moveFast ? (posMoveSpeed * 5) : posMoveSpeed);
                });
            }
        }
    }

    public enum MovementType {
        Selection = 0,
        MoveNode = 1,
        AdjustBrightness = 2
    }

    public enum BlendOperator {
        Multiply_For_Blending = 0,
        Add_For_Black_Level_Matching = 1,
        Subtract_For_Black_levelMatching_across3XProjectors = 2
    }

    public BlendOperator blendOperator = BlendOperator.Multiply_For_Blending;

    public void DrawGUIExtra(Omnity anOmnity) {
        okToCheckKeyboard = true;
        GUI.skin.box.VerticalLayout(() => {
            GUI.skin.box.HorizontalLayout(() => {
                if (OmnityHelperFunctions.EnumInputResetWasChanged<BlendOperator>("BlendOperator", "BlendOperator", ref blendOperator, BlendOperator.Multiply_For_Blending, 0)) {
                }
                if (BlendOperator.Multiply_For_Blending == blendOperator) {
                    gamma = OmnityHelperFunctions.FloatInputResetSlider("gamma", gamma, 0.5f, 1, 2f);
                }
                if (BlendOperator.Add_For_Black_Level_Matching == blendOperator) {
                    add = OmnityHelperFunctions.FloatInputResetSlider("add", add, 0.0f, .1f, 1f);
                }
                if (BlendOperator.Subtract_For_Black_levelMatching_across3XProjectors == blendOperator) {
                    subtract = OmnityHelperFunctions.FloatInputResetSlider("subtract", subtract, 0.0f, .1f, 1f);
                }
            });
            GUI.skin.box.HorizontalLayout(() => {
                blendEnabled = OmnityHelperFunctions.BoolInputReset("blendEnabled", blendEnabled, true);
                if (GUILayout.Button("Reset This EdgeBlend")) {
                    Reset();
                }
            });
            iySelected = OmnityHelperFunctions.IntInputReset("iy", iySelected, 0);
            ixSelected = OmnityHelperFunctions.IntInputReset("ix", ixSelected, 0);

            int iw = nodes[0].Count;
            int ih = nodes.Count;

            if (OmnityHelperFunctions.IntInputResetWasChanged("Nodes Height", ref ih, defaultH)) {
                ResetNodes(iw, ih);
                iw = nodes[0].Count;
                ih = nodes.Count;
                iySelected = 0;
                ixSelected = 0;
            }
            if (OmnityHelperFunctions.IntInputResetWasChanged("Nodes Width", ref iw, defaultW)) {
                ResetNodes(iw, ih);
                iw = nodes[0].Count;
                ih = nodes.Count;
                iySelected = 0;
                ixSelected = 0;
            }

            GUILayoutOption w = GUILayout.Width(50);
            GUILayoutOption h = GUILayout.Height(50);
            int x = 0, y = 0;

            GUI.skin.box.VerticalLayout(() => {
                if (BlendOperator.Multiply_For_Blending == blendOperator || BlendOperator.Add_For_Black_Level_Matching == blendOperator) {
                    GUI.skin.box.VerticalLayout(() => {
                        float brightness = 1;
                        System.Collections.Generic.List<OmniEdgeBlendNode> list = FindEachSelectedNodeOrCurrentNodeIfNoneAreSelected().OrderBy(xx => xx.factor).ToList();
                        if (list.Count > 0) {
                            brightness = list[(list.Count - 1) / 2].factor;
                            if (OmnityHelperFunctions.FloatInputResetSliderWasChanged("Brightness", ref brightness, 0, 1, 1)) {
                                ForEach<OmniEdgeBlendNode>(list, (n) => {
                                    n.factor = brightness;
                                    n.computing = true;
                                });
                            }
                        }
                    });
                }
                GUI.skin.label.HorizontalLayout(() => {
                    GUI.skin.box.VerticalLayout(() => {
                        GUILayout.Label("Move/Select");
                        GUI.skin.label.HorizontalLayout(() => {
                            GUILayout.Box("", GUI.skin.label, w, h);
                            if (GUILayout.Button("^", w, h)) {
                                iySelected++;
                            }
                            GUILayout.Box("", GUI.skin.label, w, h);
                            GUILayout.FlexibleSpace();
                        });
                        GUI.skin.label.HorizontalLayout(() => {
                            if (GUILayout.Button("<", w, h)) {
                                ixSelected--;
                            }
                            if (GUILayout.Button("Select", w, h)) {
                                nodes[iySelected][ixSelected].isSelected = !nodes[iySelected][ixSelected].isSelected;
                            }
                            if (GUILayout.Button(">", w, h)) {
                                ixSelected++;
                            }
                        });
                        GUI.skin.label.HorizontalLayout(() => {
                            GUILayout.Box("", GUI.skin.label, w, h);
                            if (GUILayout.Button("v", w, h)) {
                                iySelected--;
                            }
                            GUILayout.Box("", GUI.skin.label, w, h);
                        });
                        if (GUILayout.Button("Clear")) {
                            ForEach<OmniEdgeBlendNode>(EveryNode(), (n) => {
                                n.isSelected = false;
                            });
                        }
                    }, GUILayout.MaxWidth(250));

                    GUI.skin.box.VerticalLayout(() => {
                        x = 0;
                        y = 0;
                        GUILayout.Label("Arrows reposition nodes");
                        GUI.skin.label.HorizontalLayout(() => {
                            GUILayout.Box("", GUI.skin.label, w, h);
                            if (GUILayout.RepeatButton("^", w, h)) {
                                y++;
                            }
                            GUILayout.Box("", GUI.skin.label, w, h);
                            GUILayout.FlexibleSpace();
                        });
                        GUI.skin.label.HorizontalLayout(() => {
                            if (GUILayout.RepeatButton("<", w, h)) {
                                x--;
                            }
                            GUILayout.Label("Move", w, h);

                            if (GUILayout.RepeatButton(">", w, h)) {
                                x++;
                            }
                        });
                        GUI.skin.label.HorizontalLayout(() => {
                            GUILayout.Box("", GUI.skin.label, w, h);
                            if (GUILayout.RepeatButton("v", w, h)) {
                                y--;
                            }
                            GUILayout.Box("", GUI.skin.label, w, h);
                        });

                        if (x != 0 || y != 0) {
                            ForEach<OmniEdgeBlendNode>(FindEachSelectedNodeOrCurrentNodeIfNoneAreSelected(), (n) => {
                                n.normalizedPosition += new Vector2(x, y) * Time.deltaTime * posMoveSpeed;
                            });
                        }
                    }, GUILayout.MaxWidth(250));
                });
            });

            try {
                iySelected = Mathf.Clamp(iySelected, 0, nodes.Count - 1);
                ixSelected = Mathf.Clamp(ixSelected, 0, nodes[0].Count - 1);
            } catch (System.Exception e) {
                Debug.LogError("Nodes not correct size " + e.Message);
            }
        });
        foreach (var node2 in nodes) {
            foreach (OmniEdgeBlendNode node in node2) {
                node.computing = true;
            }
        }
        ApplyMesh();
    }

    static public float CubicInterpolateF(float y0, float y1, float y2, float y3, float t) {
        float a0, a1, a2, a3, mu2;
        a0 = -0.5f * y0 + 1.5f * y1 - 1.5f * y2 + 0.5f * y3;
        a1 = y0 - 2.5f * y1 + 2f * y2 - 0.5f * y3;
        a2 = -0.5f * y0 + 0.5f * y2;
        a3 = y1;
        mu2 = t * t;
        return (a0 * t * mu2 + a1 * mu2 + a2 * t + a3);
    }

    public bool EditingMesh = false;

    private int _iySelected = 0;
    private int _ixSelected = 0;

    public int iySelected {
        get {
            return _iySelected;
        }
        set {
            _iySelected = Mathf.Clamp(value, 0, nodes.Count - 1);
        }
    }

    public int ixSelected {
        get {
            return _ixSelected;
        }
        set {
            _ixSelected = Mathf.Clamp(value, 0, nodes[0].Count - 1);
        }
    }
}

/*

public bool blendEnabled = true; // show blend
public bool _EditingMesh = false;  // show edit on this one
static public bool anyoneEditingBlend = false;
public bool EditingMesh {
    get {
        return _EditingMesh;
    }
    set {
        foreach (EdgeBlender eb in OverlapBlender.allOverlapBlenders) {
            eb._EditingMesh = false;
        }
        _EditingMesh = anyoneEditingBlend = value;
    }
}

public void SaveCalibration(string filename) {
    System.Xml.XmlTextWriter xmlWriter = new System.Xml.XmlTextWriter(filename, new System.Text.UTF8Encoding(false));
    xmlWriter.Formatting = System.Xml.Formatting.Indented;
    xmlWriter.IndentChar = '\t';
    xmlWriter.Indentation = 1;
    xmlWriter.WriteStartDocument();
    WriteXML(xmlWriter);
    xmlWriter.WriteEndDocument();
    xmlWriter.Close();
}

public void WriteXML(System.Xml.XmlTextWriter xmlWriter) {
    xmlWriter.WriteStartElement("MeshWarp");
    _WriteXML(xmlWriter);
    xmlWriter.WriteEndElement();
}

public void LoadConfig(string filename) {
    if (!System.IO.File.Exists(filename))
        return;
    System.Xml.XPath.XPathNavigator nav = OmnityHelperFunctions.LoadXML(filename);
    _ReadXML(nav);
}

*/

//    static public System.Collections.Generic.List<OverlapBlender> allOverlapBlenders = new System.Collections.Generic.List<OverlapBlender>();

static public partial class GLExtensionsOmnity {

    public static void GLVERTEXFLIPY(Vector2 v2) {
        GL.Vertex(new Vector3(v2.x, 1 - v2.y, 0));
    }

    public static void GLOrthoTemp(System.Action a) {
        GL.PushMatrix();
        GL.LoadOrtho();
        a();
        GL.PopMatrix();
    }

    public static void GLDraw(int mode, System.Action a) {
        GL.Begin(mode);
        a();
        GL.End();
    }
}

static public partial class RectExtensionsOmnity {

    public static Rect Transform_NormalizedRect_ToFitInsideThis(this Rect outer, Rect innerNormalized) {
        return new Rect(outer.xMin + innerNormalized.xMin * outer.width, outer.yMin + innerNormalized.yMin * outer.width, outer.width * innerNormalized.width, outer.height * innerNormalized.height);
    }

    public static Rect NormalizedViewportRectToPixels(this Rect outer) {
        return new Rect(outer.xMin * Screen.width, outer.yMin * Screen.height, outer.width * Screen.width, outer.height * Screen.height);
    }
}
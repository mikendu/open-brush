using OmnityMono;

using System.Xml;
using System.Xml.XPath;
using UnityEngine;

public class BiclopsBlenderManager : OmnityTabbedFeature {
    static public OmnityPluginsIDs _myOmnityPluginsID = OmnityPluginsIDs.BiclopsBlender;
    override public OmnityPluginsIDs myOmnityPluginsID { get { return _myOmnityPluginsID; } }

    static public BiclopsBlenderManager OmniCreateSingleton(GameObject go) {
        return GetSingleton<BiclopsBlenderManager>(ref singleton, go);
    }

    static public bool symmetricEdit = true;
    private static BiclopsBlenderManager singleton;

    static public BiclopsBlenderManager Get() {
        return singleton;
    }

    private void Awake() {
        if (VerifySingleton<BiclopsBlenderManager>(ref singleton)) {
            Omnity.onReloadEndCallbackPriority.AddLoaderFunction(PriorityEventHandler.Ordering.Order_BiclopsBlender, CoroutineLoader);
            Omnity.onSaveCompleteCallback += Save;
        }
    }

    public override void Unload() {
        BiclopsBlender.allBiclopsBlenders.Clear();
        currentWarp = null;
    }

    override public void OtherLoaders() {
        foreach (FinalPassCamera fp in Omnity.anOmnity.finalPassCameras) {
            if (!fp.startEnabled) {
                continue;
            }
            if (fp.myCameraTransform.gameObject.GetComponent<BiclopsBlender>() == null) {
                /* BiclopsBlender mw = */
                fp.myCameraTransform.gameObject.AddComponent<BiclopsBlender>();
            } else {
                Debug.LogError("Warning this is getting called twice, gracefully recovering");
            }
        }
    }

    static public BiclopsBlender currentWarp = null;

    private void Hide() {
        currentWarp = null;
        if (BiclopsBlender.allBiclopsBlenders.Count > 0) {
            try {
                BiclopsBlender.allBiclopsBlenders[0].EditingMesh = false;
            } catch {
            }
        }
    }

    /// overridden abstract functions
    public override string BaseName {
        get {
            return "BiclopsBlender";
        }
    }

    public override void ReadXMLDelegate(XPathNavigator nav) {
    }

    public override void WriteXMLDelegate(XmlTextWriter xmlWriter) {
    }

    override public void OnCloseGUI(Omnity anOmnity) {
        Hide();
    }

    public override void MyGuiCallback(Omnity anOmnity) {
        if (!anOmnity.pluginIDs.Contains((int)OmnityPluginsIDs.BiclopsBlender)) {
            return;
        }

        if (BiclopsBlender.allBiclopsBlenders.Count == 0) {
            currentWarp = null;
        }
        GUILayout.BeginHorizontal();
        string tabname;
        if (currentWarp == null) {
            tabname = "[Hide]";
        } else {
            tabname = "Hide";
        }
        if (GUILayout.Button(tabname)) {
            Hide();
        }
        GUILayout.Label("The config should have two screen shapes with shader set to BiClops6ChannelWarpPreviewUniformityBlackLift.  Create two Blenders and connect the screen shapes.");

        foreach (BiclopsBlender bb in BiclopsBlender.allBiclopsBlenders) {
            if (currentWarp == bb) {
                tabname = "[" + bb.name + "]";
            } else {
                tabname = bb.name;
            }
            if (GUILayout.Button(tabname)) {
                currentWarp = bb;
                bb.EditingMesh = true;
            }
        }
        GUILayout.EndHorizontal();
        if (currentWarp != null) {
            currentWarp.DoGUI(anOmnity);
        }
    }

    public override void OtherSavers() {
        foreach (var eb in BiclopsBlender.allBiclopsBlenders) {
            eb.SaveCalibration(eb.FileName);
        }
    }

    // Use this for initialization
    private void Start() {
    }
}

public class BiclopsBlender : MonoBehaviour {

    public void OnDestroy() {
        UnloadEdgeBlender();
    }

    private void OnEnable() {
        BiclopsBlender.allBiclopsBlenders.Add(this);
    }

    private void OnDisable() {
        BiclopsBlender.allBiclopsBlenders.Remove(this);
    }

    public void Start() {
        Init();
        LoadConfig(FileName);
    }

    public string FileName {
        get {
            return OmnityLoader.AddSpecialConfigPath(Omnity.anOmnity, gameObject.GetComponent<FinalPassCameraProxy>().myFinalPassCamera.name + ".ini");
        }
    }

    private void Update() {
        if (EditingMesh) {
            if (Input.GetKeyDown(KeyCode.LeftArrow)) {
                ColumnToAdjust = OmnityHelperFunctions.NegModInt(ColumnToAdjust + 1, XYStart.Count);
            } else if (Input.GetKeyDown(KeyCode.RightArrow)) {
                ColumnToAdjust = OmnityHelperFunctions.NegModInt(ColumnToAdjust - 1, XYStart.Count);
            } else if (Input.GetKey(KeyCode.UpArrow)) {
                XYEnd[ColumnToAdjust] = new Vector2(XYEnd[ColumnToAdjust].x, Mathf.Clamp01(XYEnd[ColumnToAdjust].y + BiclopsBlender.ColumnAdjustRate * Time.deltaTime));

                if (BiclopsBlenderManager.symmetricEdit) {
                    float newY = XYEnd[ColumnToAdjust].y;
                    foreach (BiclopsBlender blender in BiclopsBlender.allBiclopsBlenders) {
                        blender.XYEnd[ColumnToAdjust] = new Vector2(blender.XYEnd[ColumnToAdjust].x, newY);
                        blender.XYEnd[blender.XYEnd.Count - ColumnToAdjust - 1] = new Vector2(blender.XYEnd[blender.XYEnd.Count - ColumnToAdjust - 1].x, newY);
                    }
                }
            } else if (Input.GetKey(KeyCode.DownArrow)) {
                XYEnd[ColumnToAdjust] = new Vector2(XYEnd[ColumnToAdjust].x, Mathf.Clamp01(XYEnd[ColumnToAdjust].y - BiclopsBlender.ColumnAdjustRate * Time.deltaTime));
                if (BiclopsBlenderManager.symmetricEdit) {
                    float newY = XYEnd[ColumnToAdjust].y;
                    foreach (BiclopsBlender blender in BiclopsBlender.allBiclopsBlenders) {
                        blender.XYEnd[ColumnToAdjust] = new Vector2(blender.XYEnd[ColumnToAdjust].x, newY);
                        blender.XYEnd[blender.XYEnd.Count - ColumnToAdjust - 1] = new Vector2(blender.XYEnd[blender.XYEnd.Count - ColumnToAdjust - 1].x, newY);
                    }
                }
            }
        }
    }

    public void DoGUI(Omnity anOmnity) {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Save All")) {
            foreach (var eb in BiclopsBlender.allBiclopsBlenders) {
                eb.SaveCalibration(eb.FileName);
            }
        }
        if (GUILayout.Button("Load All")) {
            foreach (var eb in BiclopsBlender.allBiclopsBlenders) {
                eb.LoadConfig(eb.FileName);
            }
        }
        GUILayout.EndHorizontal();

        blendEnabled = OmnityHelperFunctions.BoolInputReset("blendEnabled", blendEnabled, true);

        if (GUILayout.Button("Reset EdgeBlend")) {
            Reset();
        }

        GUILayout.BeginHorizontal();
        BiclopsBlenderManager.symmetricEdit = OmnityHelperFunctions.BoolInputReset("editAllSymetrically", BiclopsBlenderManager.symmetricEdit, false);
        GUILayout.EndHorizontal();
        if (OmnityHelperFunctions.FloatInputResetSliderWasChanged("Gamma", ref gamma, 0, 1, 4)) {
            if (blendMaterial != null) {
                blendMaterial.SetFloat("_Value", gamma);
            }
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("-")) {
            ColumnToAdjust--;
        }
        GUILayout.Label("Column to Adjust" + ColumnToAdjust + "/" + XYStart.Count);
        if (GUILayout.Button("+")) {
            ColumnToAdjust++;
        }
        ColumnToAdjust = negmod(ColumnToAdjust, XYStart.Count);

        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.RepeatButton("-")) {
            float newY = Mathf.Clamp01(XYEnd[ColumnToAdjust].y - ColumnAdjustRate * Time.deltaTime);
            XYEnd[ColumnToAdjust] = new Vector2(XYEnd[ColumnToAdjust].x, newY);
            if (BiclopsBlenderManager.symmetricEdit) {
                foreach (BiclopsBlender eb in BiclopsBlender.allBiclopsBlenders) {
                    eb.XYEnd[ColumnToAdjust] = XYEnd[ColumnToAdjust];
                    eb.XYEnd[eb.XYEnd.Count - ColumnToAdjust - 1] = new Vector2(XYEnd[eb.XYEnd.Count - ColumnToAdjust - 1].x, newY);
                }
            }
        }

        GUILayout.Label("Blend Distance" + XYEnd[ColumnToAdjust].y);
        if (GUILayout.RepeatButton("+")) {
            float newY = Mathf.Clamp01(XYEnd[ColumnToAdjust].y + ColumnAdjustRate * Time.deltaTime);
            XYEnd[ColumnToAdjust] = new Vector2(XYEnd[ColumnToAdjust].x, newY);
            if (BiclopsBlenderManager.symmetricEdit) {
                Debug.Log("EdgeBlender.allEdgeBlenders " + BiclopsBlender.allBiclopsBlenders.Count);
                foreach (BiclopsBlender blender in BiclopsBlender.allBiclopsBlenders) {
                    blender.XYEnd[ColumnToAdjust] = new Vector2(XYEnd[ColumnToAdjust].x, XYEnd[ColumnToAdjust].y);
                    blender.XYEnd[blender.XYEnd.Count - ColumnToAdjust - 1] = new Vector2(XYEnd[blender.XYEnd.Count - ColumnToAdjust - 1].x, newY);
                }
            }
        }
        GUILayout.EndHorizontal();
        foreach (BiclopsBlender blender in BiclopsBlender.allBiclopsBlenders) {
            blender.ApplyMesh();
        }
    }

    public void OnPostRender() {
        if (EditingMesh) {
            //  RenderTexture.active = destination;
            GL.PushMatrix();
            GL.LoadOrtho();

            if (blendEnabled) {
                if (blendMaterial != null) {
                    blendMaterial.SetPass(0);
                }
                Graphics.DrawMeshNow(blenderPatch, transform.position, transform.rotation);
                lineMaterial.SetPass(0);
                GL.Begin(GL.LINES);
                GL.Color(Color.green);
                BiclopsBlenderExtensions.DrawBlenderMesh(XYStart, XYEnd);
                GL.Color(Color.red);
                BiclopsBlenderExtensions.GLVERTEXFLIP(new Vector2(.01f, 0) + XYStart[ColumnToAdjust]);
                BiclopsBlenderExtensions.GLVERTEXFLIP(new Vector2(.01f, 0) + XYEnd[ColumnToAdjust]);
                BiclopsBlenderExtensions.GLVERTEXFLIP(new Vector2(-.01f, 0) + XYStart[ColumnToAdjust]);
                BiclopsBlenderExtensions.GLVERTEXFLIP(new Vector2(-.01f, 0) + XYEnd[ColumnToAdjust]);
                GL.End();
            }

            GL.PopMatrix();
        } else {
            //  RenderTexture.active = destination;
            GL.PushMatrix();
            GL.LoadOrtho();
            if (blendEnabled) {
                if (blendMaterial) {
                    blendMaterial.SetPass(0);
                } else {
                    Debug.LogWarning("blendMaterial missing");
                }
                BiclopsBlenderExtensions.DrawPatch(blenderPatch, transform.position, transform.rotation);
            }

            if (anyoneEditingBlend && BiclopsBlenderManager.symmetricEdit && blendEnabled) {
                GL.Begin(GL.LINES);
                GL.Color(Color.grey);
                BiclopsBlenderExtensions.DrawBlenderMesh(XYStart, XYEnd);
                GL.End();
            }
            GL.PopMatrix();
        }
    }

    private static Material _lineMaterial = null;

    private static Material lineMaterial {
        get {
            if (_lineMaterial == null) {
                _lineMaterial = FinalPassSetup.ApplySettings(FinalPassShaderType.LineShader);
                _lineMaterial.hideFlags = HideFlags.HideAndDontSave;
                _lineMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
            }
            return _lineMaterial;
        }
    }

    public void SaveCalibration(string filename) {
        _SaveCalibration(filename.Replace(".xml", "_biclopsblender.xml"));
        UnityEngine.Debug.LogWarning("THIS FILENAME TYPE " + filename + " IS DEPRECATED... make sure all biclops enabled apps are upgraded...");
        _SaveCalibration(filename);
    }

    private void _SaveCalibration(string filename) {
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

    public void LoadConfig(string _filename) {
        string filename = _filename.Replace(".xml", "_biclopsblender.xml");
        if (System.IO.File.Exists(_filename.Replace(".xml", "_biclopsblender.xml"))) {
            filename = _filename.Replace(".xml", "_biclopsblender.xml");
        } else if (System.IO.File.Exists(_filename)) {
            UnityEngine.Debug.LogWarning("THIS FILENAME TYPE IS DEPRECATED... save and reload...");
            filename = _filename;
        } else {
            return;
        }
        System.Xml.XPath.XPathNavigator nav = OmnityHelperFunctions.LoadXML(filename);
        _ReadXML(nav);
    }

    public void UnloadEdgeBlender() {
        GameObject.Destroy(blenderPatch);
    }

    static public float ColumnAdjustRate = .1f;

    private int count = 20;
    private Mesh blenderPatch;
    private System.Collections.Generic.List<Vector3> newVertices;
    private System.Collections.Generic.List<Vector2> newUV;
    private System.Collections.Generic.List<Color> newColors;
    private System.Collections.Generic.List<int> newTriangles;

    public System.Collections.Generic.List<Vector2> XYStart = new System.Collections.Generic.List<Vector2>();
    public System.Collections.Generic.List<Vector2> XYEnd = new System.Collections.Generic.List<Vector2>();
    static public float gamma = 1.0f; // this must remain static

    static public System.Collections.Generic.List<BiclopsBlender> allBiclopsBlenders = new System.Collections.Generic.List<BiclopsBlender>();

    public void _WriteXML(System.Xml.XmlTextWriter xmlWriter) {
        xmlWriter.WriteStartElement("EdgeBlender");
        xmlWriter.WriteElementString("gamma", gamma.ToString("F4"));
        xmlWriter.WriteElementString("blendEnabled", blendEnabled.ToString());

        foreach (Vector2 start in XYStart) {
            xmlWriter.WriteStartElement("XYStart");
            xmlWriter.WriteElementString("element", start.ToString("F4"));
            xmlWriter.WriteEndElement();
        }
        foreach (Vector2 end in XYEnd) {
            xmlWriter.WriteStartElement("XYEnd");
            xmlWriter.WriteElementString("element", end.ToString("F4"));
            xmlWriter.WriteEndElement();
        }
        xmlWriter.WriteEndElement();
    }

    public void _ReadXML(System.Xml.XPath.XPathNavigator nav) {
        gamma = OmnityHelperFunctions.ReadElementFloatDefault(nav, ".//gamma", gamma);
        blendEnabled = OmnityHelperFunctions.ReadElementBoolDefault(nav, ".//blendEnabled", blendEnabled);

        XYStart.Clear();
        System.Xml.XPath.XPathNodeIterator XYStartItor = nav.Select("(.//XYStart)");
        while (XYStartItor.MoveNext()) {
            XYStart.Add(OmnityHelperFunctions.ReadElementVector2Default(XYStartItor.Current, "element", Vector2.zero));
        }

        XYEnd.Clear();
        System.Xml.XPath.XPathNodeIterator XYEndItor = nav.Select("(.//XYEnd)");
        while (XYEndItor.MoveNext()) {
            XYEnd.Add(OmnityHelperFunctions.ReadElementVector2Default(XYEndItor.Current, "element", Vector2.zero));
        }
        ApplyMesh();
    }

    [System.NonSerialized]
    private Material blendMaterial;

    public void Init() {
        blenderPatch = new Mesh();
        newVertices = new System.Collections.Generic.List<Vector3>();
        newUV = new System.Collections.Generic.List<Vector2>();
        newTriangles = new System.Collections.Generic.List<int>();
        XYStart = new System.Collections.Generic.List<Vector2>();
        XYEnd = new System.Collections.Generic.List<Vector2>();
        newColors = new System.Collections.Generic.List<Color>();

        if (blendMaterial == null) {
            blendMaterial = FinalPassSetup.ApplySettings(FinalPassShaderType.OmniEdgeBlendShaderMultiply);
        }
        if (blendMaterial == null) {
            Debug.LogWarning("blendMaterial missing");
        }
        Reset();
    }

    private void Reset() {
        gamma = 1;
        XYStart.Clear();
        XYEnd.Clear();
        count = 20;
        ColumnToAdjust = count / 2;
        for (int index = 0; index < count; index++) {
            float x = (float)index / (float)(count - 1);
            float y = 0;
            switch (index) {
                case 0:
                    y = 0.008f;
                    break;

                case 1:
                    y = 0.0782f;
                    break;

                case 2:
                    y = 0.1139f;
                    break;

                case 3:
                    y = 0.1342f;
                    break;

                case 4:
                    y = 0.1720f;
                    break;

                case 5:
                    y = 0.2091f;
                    break;

                case 6:
                    y = 0.2314f;
                    break;

                case 7:
                    y = 0.2479f;
                    break;

                case 8:
                    y = 0.2628f;
                    break;

                case 9:
                    y = 0.2528f;
                    break;

                case 10:
                    y = 0.2528f;
                    break;

                case 11:
                    y = 0.2628f;
                    break;

                case 12:
                    y = 0.2479f;
                    break;

                case 13:
                    y = 0.2314f;
                    break;

                case 14:
                    y = 0.2091f;
                    break;

                case 15:
                    y = 0.1720f;
                    break;

                case 16:
                    y = 0.1342f;
                    break;

                case 17:
                    y = 0.1139f;
                    break;

                case 18:
                    y = 0.0782f;
                    break;

                case 19:
                    y = 0.008f;
                    break;
            }

            XYStart.Add(new Vector2(x, 0));
            XYEnd.Add(new Vector2(x, y));
        }
        ApplyMesh();
    }

    private float scale = -1f;
    private float offset = 1f;

    private float XToU(float x) {
        float rangeX = (XYStart[XYStart.Count - 1].x - XYStart[0].x);
        return (x / rangeX) - XYStart[0].x;
    }

    private void ApplyMesh() {
        newVertices.Clear();
        newTriangles.Clear();
        newUV.Clear();
        newColors.Clear();
        blenderPatch.Clear();
        Vector2 UVTop = new Vector2(XToU(XYStart[0].x), 0);
        Vector2 UVBot = new Vector2(XToU(XYStart[0].x), 1);

        Vector3 TopL = new Vector3(XYStart[0].x, offset + scale * XYStart[0].y, 0.0f);
        Vector3 BotL = new Vector3(XYEnd[0].x, offset + scale * XYEnd[0].y, 0.0f);

        newVertices.Add(TopL); //0
        newUV.Add(UVTop); //1
        newColors.Add(Color.black);
        newVertices.Add(BotL); //1
        newUV.Add(UVBot); //0
        newColors.Add(Color.white);

        float xstart0 = XYStart[0].x;
        float xend0 = XYEnd[0].x;

        float ystart0 = XYStart[0].y;
        float yend0 = XYEnd[0].y;

        float xstart1 = XYStart[0].x;
        float xend1 = XYEnd[0].x;

        float ystart1 = XYStart[0].y;
        float yend1 = XYEnd[0].y;

        float xstart3 = XYStart.Count > 2 ? XYStart[2].x : 0;
        float xend3 = XYStart.Count > 2 ? XYEnd[2].x : 0;
        float ystart3 = XYStart.Count > 2 ? XYStart[2].y : 0;
        float yend3 = XYStart.Count > 2 ? XYEnd[2].y : 0;

        for (int i = 1; i < count; i++) {
            float xstart2 = XYStart[i].x;
            float xend2 = XYEnd[i].x;

            float ystart2 = XYStart[i].y;
            float yend2 = XYEnd[i].y;

            for (int j = 0; j < 3; j++) {
                float f = (j + 1) / 3.0f;
                float xstart = CubicInterpolateF(xstart0, xstart1, xstart2, xstart3, f);
                float ystart = CubicInterpolateF(ystart0, ystart1, ystart2, ystart3, f);
                float xend = CubicInterpolateF(xend0, xend1, xend2, xend3, f);
                float yend = CubicInterpolateF(yend0, yend1, yend2, yend3, f);

                UVTop = new Vector2(XToU(xstart), 0);
                UVBot = new Vector2(XToU(xend), 1);
                Vector3 TopR = new Vector3(xstart, offset + scale * ystart, 0);
                Vector3 BotR = new Vector3(xend, offset + scale * yend, 0);

                newVertices.Add(TopR);
                newVertices.Add(BotR);
                newUV.Add(UVTop);
                newColors.Add(Color.black);
                newUV.Add(UVBot);
                newColors.Add(Color.white);
                int TopRIndex = newVertices.Count - 2;
                int BotRIndex = newVertices.Count - 1;
                int TopLIndex = newVertices.Count - 4;
                int BotLIndex = newVertices.Count - 3;

                newTriangles.Add(BotLIndex);
                newTriangles.Add(TopLIndex);
                newTriangles.Add(BotRIndex);

                newTriangles.Add(TopLIndex);
                newTriangles.Add(TopRIndex);
                newTriangles.Add(BotRIndex);
            }

            xstart0 = xstart1;
            ystart0 = ystart1;
            xend0 = xend1;
            yend0 = yend1;

            xstart1 = XYStart[i].x;
            xend1 = XYEnd[i].x;
            ystart1 = XYStart[i].y;
            yend1 = XYEnd[i].y;

            if (XYStart.Count > i + 2) {
                xstart3 = XYStart[i + 2].x;
                xend3 = XYEnd[i + 2].x;
                ystart3 = XYStart[i + 2].y;
                yend3 = XYEnd[i + 2].y;
            }
        }

        blenderPatch.vertices = newVertices.ToArray();
        blenderPatch.uv = newUV.ToArray();
        blenderPatch.colors = newColors.ToArray();
        blenderPatch.triangles = newTriangles.ToArray();
        OmnityPlatformDefines.OptimizeMesh(blenderPatch);
        if (blendMaterial == null) {
            blendMaterial = FinalPassSetup.ApplySettings(FinalPassShaderType.OmniEdgeBlendShaderMultiply);
        }
        if (blendMaterial == null) {
            Debug.LogWarning("blendMaterial missing");
        } else {
            blendMaterial.SetFloat("_Value", gamma);
        }
    }

    private int negmod(int x, int m) {
        return (x % m + m) % m;
    }

    public int ColumnToAdjust = 0;

    public bool blendEnabled = true; // show blend
    public bool _EditingMesh = false;  // show edit on this one
    static public bool anyoneEditingBlend = false;

    public bool EditingMesh {
        get {
            return _EditingMesh;
        }

        set {
            foreach (BiclopsBlender eb in BiclopsBlender.allBiclopsBlenders) {
                eb._EditingMesh = false;
            }
            _EditingMesh = anyoneEditingBlend = value;
        }
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
}
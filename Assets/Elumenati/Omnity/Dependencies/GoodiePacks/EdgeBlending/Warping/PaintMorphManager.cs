#if UNITY_WEBPLAYER
#define LOADSAVE_NOTSUPORTED
#else
#endif

using OmnityMono;
using System.Xml.XPath;
using UnityEngine;

public class PaintMorphManager : OmnityTabbedFeature {
    static public OmnityPluginsIDs _myOmnityPluginsID = OmnityPluginsIDs.PaintMorph;
    override public OmnityPluginsIDs myOmnityPluginsID { get { return _myOmnityPluginsID; } }

    static public PaintMorphManager OmniCreateSingleton(GameObject go) {
        return GetSingleton<PaintMorphManager>(ref singleton, go);
    }

    static private PaintMorphManager singleton = null;

    static public PaintMorphManager Get() {
        return singleton;
    }

    private void Awake() {
        if (VerifySingleton<PaintMorphManager>(ref singleton)) {
            Omnity.onReloadEndCallbackPriority.AddLoaderFunction(PriorityEventHandler.Ordering.Order_PaintMorphManager, CoroutineLoader);
            Omnity.onLoadInMemoryConfigStart += (Omnity anOmnity) => {
                if (anOmnity.PluginEnabled(OmnityPluginsIDs.PaintMorph)) {
                }
            };
            Omnity.onSaveCompleteCallback += Save;
        }
    }

    public override void Unload() {
        linkedScreenShapeGroupsPM.Clear();
    }

    override public string BaseName {
        get {
            return "PaintMorphSetting";
        }
    }

    public override void ReadXMLDelegate(XPathNavigator nav) {
        linkedScreenShapeGroupsPM = LinkedScreenShapeGroupPM.ReadXMLAll<LinkedScreenShapeGroupPM>(nav, Omnity.anOmnity);
        foreach (LinkedScreenShapeGroupPM group in linkedScreenShapeGroupsPM) {
            group.Apply(Omnity.anOmnity);
        }
    }

    /// <param name="xmlWriter">The XML writer.</param>
    override public void WriteXMLDelegate(System.Xml.XmlTextWriter xmlWriter) {
        foreach (var group in linkedScreenShapeGroupsPM) {
            group.WriteXML(xmlWriter);
        }
    }

    public override System.Collections.IEnumerator PostLoad() {
        PaintMorph.HideAll();
        yield break;
    }

    public override void Save(Omnity anOmnity) {
        base.Save(anOmnity);
        if (anOmnity.PluginEnabled(myOmnityPluginsID)) {
            foreach (LinkedScreenShapeGroupPM group in linkedScreenShapeGroupsPM) {
                if (group.pm == null) {
                    Debug.LogWarning("Skipping save paint morph that has not finished loading...");
                } else {
                    group.pm.Save(anOmnity, group);
                    Debug.Log("Saving " + group.pm.gameObject.name);
                }
            }
        }
    }

    public System.Collections.Generic.List<LinkedScreenShapeGroupBase> linkedScreenShapeGroupsPM = new System.Collections.Generic.List<LinkedScreenShapeGroupBase>();

    override public void OnCloseGUI(Omnity anOmnity) {
        PaintMorph.HideAll();
    }

    private static int paintMorphShowingOnFrame = -999;

    public override void MyGuiCallback(Omnity anOmnity) {
        if (!anOmnity.PluginEnabled(OmnityPluginsIDs.PaintMorph)) {
            return;
        }
        paintMorphShowingOnFrame = Time.frameCount;
        if (OmnityPlatformDefines.EventTypeRepaint== Event.current.type) { 
            for (int i = 0; i < linkedScreenShapeGroupsPM.Count; i++) {
                LinkedScreenShapeGroupPM group = (LinkedScreenShapeGroupPM)linkedScreenShapeGroupsPM[i];
                if (group.pm != null) {
                    group.pm.DoUpdate();
                }
            }
        }

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Hide")) {
            PaintMorph.HideAll();
        }

        foreach (LinkedScreenShapeGroupPM group in linkedScreenShapeGroupsPM) {
            if (GUILayout.Button(group.pm.showing ? "[" + group.name + "]" : group.name)) {
                group.pm.SetShowing();
            }
        }
        GUILayout.EndHorizontal();
        OmniJoystick.OnGUI();
        GUILayout.Label("Arrows:\tMove Target Zone");
        GUILayout.Label("Shift Arrows:\tMove Warp Target");
        GUILayout.Label("Control Up/Down:\tResize Target");
        GUILayout.Label("Tab/Shift Tab:\tSwitch Screen");
        GUILayout.Label("R:\tReduce Warp");
        GUILayout.Label("Enter: Smooth Warp");
        GUILayout.Label(OmniJoystick.Button2str + "spot morph; ");
        GUILayout.Label(OmniJoystick.Button1str + "spot smooth; ");
        GUILayout.Label(OmniJoystick.Button3str + "joystick (left analog): Resize target; ");

        GUILayout.BeginHorizontal();
        if (PaintMorph.lastActive != null) {
            if (GUILayout.Button("Save current")) {
                PaintMorph.lastActive.pm.Save(anOmnity, PaintMorph.lastActive);
            }
            if (GUILayout.Button("Reload current")) {
                PaintMorph.lastActive.pm.Load(anOmnity, PaintMorph.lastActive);
                PaintMorphExtensions.EngageWarp(PaintMorph.lastActive.pm._Warp, PaintMorph.lastActive.pm.offset, PaintMorph.lastActive.pm.myRenderer);
            }
            if (GUILayout.Button("Reset current")) {
                PaintMorph.lastActive.pm.Reset();
                PaintMorphExtensions.EngageWarp(PaintMorph.lastActive.pm._Warp, PaintMorph.lastActive.pm.offset, PaintMorph.lastActive.pm.myRenderer);
            }
        }
        OmnityHelperFunctions.BR();

        if (GUILayout.Button("Save All")) {
            Save(anOmnity);
            foreach (LinkedScreenShapeGroupPM group in linkedScreenShapeGroupsPM) {
                group.pm.Save(anOmnity, group);
                Debug.Log("Saving " + group.pm.gameObject.name);
            }
        }
        if (GUILayout.Button("Reload All")) {
            StartCoroutine(Load(anOmnity));
            foreach (LinkedScreenShapeGroupPM group in linkedScreenShapeGroupsPM) {
                group.pm.Load(anOmnity, group);
                if (PaintMorph.lastActive != null && PaintMorph.lastActive.pm != null) {
                    PaintMorphExtensions.EngageWarp(PaintMorph.lastActive.pm._Warp, PaintMorph.lastActive.pm.offset, PaintMorph.lastActive.pm.myRenderer);
                }
            }
        }
        if (GUILayout.Button("Reset All")) {
            foreach (LinkedScreenShapeGroupPM group in linkedScreenShapeGroupsPM) {
                group.pm.Reset();
                PaintMorphExtensions.EngageWarp(group.pm._Warp, group.pm.offset, group.pm.myRenderer);
            }
        }

        OmnityHelperFunctions.BR();

        LinkedScreenShapeGroupPM.DrawGUIAll<LinkedScreenShapeGroupPM>(anOmnity, linkedScreenShapeGroupsPM);

        // incase you added a new screen shape group
        foreach (LinkedScreenShapeGroupPM group in linkedScreenShapeGroupsPM) {
            if (group.pm == null) {
                group.Apply(anOmnity);
            }
        }

        GUILayout.EndHorizontal();

        PaintMorph.brushAspectRatio = OmnityHelperFunctions.FloatInputReset("brushAspectRatio", PaintMorph.brushAspectRatio, 1);
        PaintMorph.radius = OmnityHelperFunctions.FloatInputReset("radius", PaintMorph.radius, .25f);
        PaintMorph.cursorUV = OmnityHelperFunctions.Vector2InputReset("cursorUV", PaintMorph.cursorUV, new Vector2(.5f, .5f));

        PaintMorph.flow = OmnityHelperFunctions.FloatInputResetSlider("flow", PaintMorph.flow, 0.1f, 1f, 5f);
    }

    private void SetIndex(int i) {
        if (linkedScreenShapeGroupsPM != null && linkedScreenShapeGroupsPM.Count >= 1) {
            if (i < 0) {
                i = linkedScreenShapeGroupsPM.Count - 1;
            }
            if (i >= linkedScreenShapeGroupsPM.Count) {
                i = 0;
            }
            LinkedScreenShapeGroupPM gpm = (LinkedScreenShapeGroupPM)linkedScreenShapeGroupsPM[i];
            gpm.pm.SetShowing();
        }
    }

    public void CheckJoystickSwitchTab() {
        try {
            int index = PaintMorph.lastActive == null ? 0 : linkedScreenShapeGroupsPM.IndexOf(PaintMorph.lastActive);
            if (OmniJoystick.ShoulderLeftDown()) {
                SetIndex(index - 1);
            } else if (OmniJoystick.ShoulderRightDown() || Input.GetKeyDown(KeyCode.Tab)) {
                SetIndex(index + 1);
            }
        } catch {
        }
    }

    override public void Update() {
        base.Update();
        if (Omnity.anyGuiShowing && Mathf.Abs(paintMorphShowingOnFrame - Time.frameCount) < 2) {
            CheckJoystickSwitchTab();
        }
    }
}

public class PaintMorph : MonoBehaviour {
    static public bool anyShowing = false;
    public Omnity anOmnity = null;

    public System.Collections.Generic.List<Renderer> myRenderer = new System.Collections.Generic.List<Renderer>();
    public Texture2D _Warp = null;

    [System.NonSerialized]
    private TextureWrapMode myTextureWrapMode = TextureWrapMode.Clamp;

    [System.NonSerialized]
    internal Color[] offset = new Color[512 * 512];

    static public float radius = .25f;
    private static float outlineWidth = .01f;

    static public float brushAspectRatio = 1;

    public float MScale = .40f; //.25 RGH  //.4 micoy
    static public float flow = 1.0f;

    public static Vector2 cursorUV = new Vector2(.5f, .5f);
    public bool _showing = false;

    static public LinkedScreenShapeGroupPM lastActive = null;

    static public bool AnyShowing() {
        if (lastActive != null && lastActive.pm.showing) {
            return false;
        } else {
            return true;
        }
    }

    public static void HideAll() {
        foreach (LinkedScreenShapeGroupPM _group in PaintMorphManager.Get().linkedScreenShapeGroupsPM) {
            _group.pm._showing = false;

            foreach (var ss in _group.linked) {
                ss.EasyToggleShaderKeyword("WARP_PREVIEW_OFF", true);
            }
        }
        lastActive = null;
    }

    public void SetShowing() {
        lastActive = group;
        foreach (LinkedScreenShapeGroupPM _group in PaintMorphManager.Get().linkedScreenShapeGroupsPM) {
            if (group == _group) {
                _showing = true;
            } else {
                _group.pm._showing = false;
            }

            foreach (var ss in _group.linked) {
                if (group == _group) {
                    ss.EasyToggleShaderKeyword("WARP_PREVIEW_ON", true);
                } else {
                    ss.EasyToggleShaderKeyword("WARP_PREVIEW_OFF", true);
                }
                //      Debug.Log("Set " + ss.name + " " + _showing);
            }
        }
        foreach (Renderer r in myRenderer) {
            SetParameters(r);
        }
        PaintMorphExtensions.EngageWarp(_Warp, offset, myRenderer);
    }

    public bool showing {
        get {
            return _showing;
        }
    }

    public Vector4 cursorUV_Showing_Size {
        get {
            return new Vector4(cursorUV.x, cursorUV.y, outlineWidth, radius);
        }

        set {
            cursorUV.x = value.x;
            cursorUV.y = value.y;
            radius = value.w;
            outlineWidth = value.z;
        }
    }

    private void OnEnable() {
        us.Add(this);
    }

    private void OnDisable() {
        us.Remove(this);
    }

    public void Initialize(Omnity anOmnity, LinkedScreenShapeGroupPM group) {
        if (_Warp != null) {
            return;
        }
        _Warp = new Texture2D(512, 512, TextureFormat.RGBA32, false);
        _Warp.wrapMode = myTextureWrapMode;
        Reset();
        Load(anOmnity, group);

        PaintMorphExtensions.EngageWarp(_Warp, offset, myRenderer);
        foreach (Renderer r in myRenderer) {
            SetParameters(r);
        }
    }

    private void SetParameters(Renderer r) {
        r.sharedMaterial.SetVector("cursorUV_Showing_Size", cursorUV_Showing_Size);
        r.sharedMaterial.SetFloat("brushAspectRatio", brushAspectRatio);
        r.sharedMaterial.SetFloat("M", MScale);
    }

    private void Start() {
        if (lastActive == null) {
        }
        Initialize(anOmnity, group);
    }

    private Vector3 GrayCenteredColorToZeroCenteredVector(Color c) {
        return new Vector3(c.r - .5f, c.g - .5f, c.b - .5f);
    }

    private Vector3 Blur(int x, int y, float radius) {
        int iterations = 50;
        float weightedHits = 0.0f;
        Vector3 colorSum = Vector3.zero;
        for (int i = 0; i < iterations; i++) {
            Vector2 dx = new Vector2(UnityEngine.Random.Range(-radius, radius), UnityEngine.Random.Range(-radius, radius));
            float weight = 1.0f - (dx.magnitude / (radius));
            if (weight > 1.0f || weight < 0)
                continue;
            int iX = x + (int)(dx.x * 512 / 4.0f);
            int iY = y + (int)(dx.y * 512 / 4.0f);

            if (iY < 0 || iY >= 512 || iX < 0 || iX >= 512) {
                continue;
            }

            weightedHits += weight;
            colorSum += GrayCenteredColorToZeroCenteredVector(offset[iX + iY * 512]) * weight;
        }
        return colorSum * (1.0f / weightedHits);
    }

    // Update is called once per frame
    public void DoUpdate() {
        if (showing) {
            //            float size = cursorUV_Showing_Size.z;
            float right = (Input.GetKey(KeyCode.RightArrow) ? 1.0f : 0.0f) - (Input.GetKey(KeyCode.LeftArrow) ? 1.0f : 0.0f);
            float up = (Input.GetKey(KeyCode.UpArrow) ? 1.0f : 0.0f) - (Input.GetKey(KeyCode.DownArrow) ? 1.0f : 0.0f);
            up = -up;
            float reduce = Input.GetKey(KeyCode.R) ? 1.0f : 0.0f;
            float smooth = Input.GetKey(KeyCode.Return) ? 1.0f : 0.0f;

            float deadZone = .1f;
            if (Mathf.Abs(OmniJoystick.HorizontalMain) > deadZone) {
                right += OmniJoystick.HorizontalMain;
            }
            if (Mathf.Abs(OmniJoystick.VerticalMain) > deadZone) {
                up += -OmniJoystick.VerticalMain;
            }

            up = -up;
            //            right = -right;

            if (group.morphFlipX)
                right = -right;
            if (group.morphFlipY)
                up = -up;
            bool push = Input.GetKey(KeyCode.LeftShift) || OmniJoystick.Button2;
            bool resize = Input.GetKey(KeyCode.LeftControl) || OmniJoystick.Button3;

            smooth = smooth + (OmniJoystick.Button1 ? 1f : 0.0f);

            //     right *= -1;
            right *= Time.deltaTime;
            up *= Time.deltaTime;
            reduce *= Time.deltaTime;

            if (right != 0 || up != 0 || reduce != 0 || smooth != 0) {
                if (resize) {
                    if (right != 0) {
                        up = right;

                        float v = right * .9f;
                        float vv = v * .25f;

                        brushAspectRatio += v;
                        radius += vv;
                    } else {
                        radius += up * .1f;
                    }

                    radius = Mathf.Clamp(radius, 0.03f, 1);
                    brushAspectRatio = Mathf.Clamp(brushAspectRatio, 0.1f, 5f);

                    foreach (Renderer r in myRenderer) {
                        SetParameters(r);
                    }
                } else {
                    if (!push && reduce == 0 && smooth == 0) {
                        cursorUV.x += right * .1f;
                        cursorUV.y += up * .1f;
                        if (group.morphRepeatX) {
                            cursorUV.x = Mathf.Repeat(cursorUV.x, 1);
                        } else {
                            cursorUV.x = Mathf.Clamp01(cursorUV.x);
                        }

                        if (group.morphRepeatY) {
                            cursorUV.y = Mathf.Repeat(cursorUV.y, 1);
                        } else {
                            cursorUV.y = Mathf.Clamp01(cursorUV.y);
                        }
                        foreach (Renderer r in myRenderer) {
                            SetParameters(r);
                        }
                    } else {
                        int width = _Warp.width;
                        //myRenderer.material.SetVector("cursorUV_Showing_Size",new Vector4(cursorUV.x,cursorUV.y,showing?othersize:0.0f,size));
                        for (int y = 0; y < _Warp.height; ++y) {
                            for (int x = 0; x < width; ++x) {
                                if (group.morphRepeatX && x == (width - 1)) {
                                    offset[x + y * width] = offset[0 + y * width];
                                    continue;
                                }

                                Vector2 UV = new Vector2((float)x / (float)_Warp.width, (float)y / (float)_Warp.height);
                                float distanceFromCursorX = Mathf.Abs(cursorUV.x - UV.x);
                                if (group.morphRepeatX) {
                                    distanceFromCursorX = Mathf.Repeat(distanceFromCursorX, 1f);
                                    if (distanceFromCursorX > .5f) {
                                        distanceFromCursorX = 1f - distanceFromCursorX;
                                    }
                                }

                                float distanceFromCursorY = Mathf.Abs(group.morphRepeatY ? ((UV.y - cursorUV.y < .5f) ? (UV.y - cursorUV.y) : (cursorUV.y + 1 - UV.y)) : Mathf.Abs(UV.y - cursorUV.y));

                                float powerX = Mathf.Clamp01(distanceFromCursorX);
                                float powerY = Mathf.Clamp01(distanceFromCursorY * brushAspectRatio);

                                float amount = Mathf.Clamp01(radius - (Mathf.Sqrt(powerX * powerX + powerY * powerY))) * flow * Time.deltaTime;
                                float amountF = Mathf.Clamp01(radius - (Mathf.Sqrt(powerX * powerX + powerY * powerY))) * flow * 100f * Time.deltaTime;

                                Color oldColor = offset[x + y * width];
                                Vector3 oldColor2 = new Vector3(oldColor.r - .5f, oldColor.g - .5f, oldColor.b - .5f);

                                if (smooth > 0) {
                                    if (amount > .01) {
                                        oldColor2 = Vector3.Lerp(Blur(x, y, radius), oldColor2, smooth * amountF * .5f);
                                    }
                                } else if (reduce > 0) {
                                    oldColor2 = Vector3.Lerp(oldColor2, Vector3.zero, reduce * amountF);
                                } else {
                                    oldColor2.x += amount * -right;
                                    oldColor2.y += amount * -up;
                                    oldColor2.z += 0;
                                }
                                oldColor2.x += .5f;
                                oldColor2.y += .5f;
                                oldColor2.z += .5f;
                                Color color = new Color(oldColor2.x, oldColor2.y, oldColor2.z);
                                offset[x + y * width] = color;
                            }
                        }
                        PaintMorphExtensions.EngageWarp(_Warp, offset, myRenderer);
                    }
                }
            }
        }
    }

    private static System.Collections.Generic.List<PaintMorph> us = new System.Collections.Generic.List<PaintMorph>();

    private string GetConfigINI(Omnity anOmnity, LinkedScreenShapeGroupPM group) {
        return OmnityLoader.AddSpecialConfigPath(anOmnity, group.name + "/morph.ini");
    }

    private string GetConfigPNG(Omnity anOmnity, LinkedScreenShapeGroupPM group) {
        return OmnityLoader.AddSpecialConfigPath(anOmnity, group.name + "/morph.png");
    }

    public void Save(Omnity anOmnity, LinkedScreenShapeGroupPM group) {
#if !LOADSAVE_NOTSUPORTED
        string filenameINI = GetConfigINI(anOmnity, group);
        string filenamePNG = GetConfigPNG(anOmnity, group);

        string offsets = PaintMorphExtensions.SerializeColorArray(offset);
        string data = offsets;
        System.IO.File.WriteAllText(filenameINI, data);
        if (_Warp != null) {
            byte[] bytes = _Warp.EncodeToPNG();
            System.IO.File.WriteAllBytes(filenamePNG, bytes);
        }
#endif
    }

    public void Reset() {
        for (int y = 0; y < _Warp.height; ++y) {
            for (int x = 0; x < _Warp.width; ++x) {
                offset[x + y * 512] = new Color(.5f, .5f, 0.0f);
            }
        }
    }

    public void Load(Omnity anOmnity, LinkedScreenShapeGroupPM group) {
#if LOADSAVE_NOTSUPORTED
        Debug.Log("Load not supported");
#else
        if (myRenderer != null && myRenderer.Count > 0) {
            string filename = GetConfigINI(anOmnity, group);

            if (!System.IO.File.Exists(filename))
                return;
            string data = System.IO.File.ReadAllText(filename);
            Color[] colors = PaintMorphExtensions.Vector3ArrayToColorArray(PaintMorphExtensions.ParseVector3String(data));

            if (colors.Length != offset.Length) {
                throw new System.Exception("file " + filename + " length mismatch expecting " + offset.Length + " got " + colors.Length);
            } else {
                //                Debug.Log("Loaded " + filename);
                offset = colors;
            }
        }
#endif
    }

    public LinkedScreenShapeGroupPM group {
        get;
        set;
    }
}

[System.Serializable]
public class LinkedScreenShapeGroupPM : LinkedScreenShapeGroupBase {
    public bool morphFlipX = false;
    public bool morphFlipY = false;
    public bool morphRepeatX = false;
    public bool morphRepeatY = false;
    public bool morphLoopX = false;

    static public string settingXMLTag {
        get {
            return "OmnityPaintMorphSetting";
        }
    }

    override public void WriteXML_var(System.Xml.XmlTextWriter xmlWriter) {
        xmlWriter.WriteElementString("morphFlipX", morphFlipX.ToString());
        xmlWriter.WriteElementString("morphFlipY", morphFlipY.ToString());
        xmlWriter.WriteElementString("morphRepeatX", morphRepeatX.ToString());
        xmlWriter.WriteElementString("morphRepeatY", morphRepeatY.ToString());
        xmlWriter.WriteElementString("morphLoopX", morphLoopX.ToString());
    }

    override public void ReadXML_var(System.Xml.XPath.XPathNavigator currentGroup, LinkedScreenShapeGroupBase _newGroup) {
        LinkedScreenShapeGroupPM newGroup = (LinkedScreenShapeGroupPM)_newGroup;
        newGroup.morphFlipX = OmnityHelperFunctions.ReadElementBoolDefault(currentGroup, ".//morphFlipX", newGroup.morphFlipX);
        newGroup.morphFlipY = OmnityHelperFunctions.ReadElementBoolDefault(currentGroup, ".//morphFlipY", newGroup.morphFlipY);
        newGroup.morphRepeatX = OmnityHelperFunctions.ReadElementBoolDefault(currentGroup, ".//morphRepeatX", newGroup.morphRepeatX);
        newGroup.morphRepeatY = OmnityHelperFunctions.ReadElementBoolDefault(currentGroup, ".//morphRepeatY", newGroup.morphRepeatY);
        newGroup.morphLoopX = OmnityHelperFunctions.ReadElementBoolDefault(currentGroup, ".//morphLoopX", newGroup.morphLoopX);
    }

    override public void DrawGUI_variable(Omnity anOmnity) {

        #region VaribleSelection

        if (OmnityHelperFunctions.BeginExpanderSimple(expanderHash + "PM", "ScreenShape (Group) To Warp", "ScreenShape (Group) To Warp")) {
            GUILayout.BeginVertical();
            morphFlipX = OmnityHelperFunctions.BoolInputReset("morphFlipX", morphFlipX, false);
            morphFlipY = OmnityHelperFunctions.BoolInputReset("morphFlipY", morphFlipY, false);
            morphRepeatX = OmnityHelperFunctions.BoolInputReset("morphRepeatX", morphRepeatX, false);
            morphRepeatY = OmnityHelperFunctions.BoolInputReset("morphRepeatY", morphRepeatY, false);
            morphLoopX = OmnityHelperFunctions.BoolInputReset("morphLoopX", morphLoopX, false);
            GUILayout.EndVertical();
        }

        OmnityHelperFunctions.EndExpanderSimple();

        #endregion VaribleSelection
    }

    public void AddPaintMorph(Omnity anOmnity) {
        if (pm == null) {
            pm = anOmnity.omnityTransform.gameObject.AddComponent<PaintMorph>();
            pm.anOmnity = anOmnity;
            pm.group = this;
        }
    }

    public void Apply(Omnity anOmnity) {
        if (pm == null) {
            AddPaintMorph(anOmnity);
        }
        pm.myRenderer.Clear();
        foreach (var morph in linked) {
            pm.myRenderer.Add(morph.renderer);
        }
    }

    public PaintMorph pm = null;
}
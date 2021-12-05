using OmnityMono;
using System.Xml;
using System.Xml.XPath;
using UnityEngine;

public class OmnityCobraHelper : OmnityTabbedFeature {
    static public OmnityPluginsIDs _myOmnityPluginsID = OmnityPluginsIDs.CobraHelper;
    override public OmnityPluginsIDs myOmnityPluginsID { get { return _myOmnityPluginsID; } }

    static public OmnityCobraHelper OmniCreateSingleton(GameObject go) {
        return GetSingleton<OmnityCobraHelper>(ref singleton, go);
    }

    private void Awake() {
        if (VerifySingleton<OmnityCobraHelper>(ref singleton)) {
            DLL_LOADED = NFDistorterInterfaceDLL.Constructor();
            if (DLL_LOADED) {
                DLL_LOADED &= NFDistorterInterfaceDLL.API.NFMemoryAccessInit();
            }
            if (!DLL_LOADED) {
                return;
            }
            /////////////////////////
            NFDistorterInterfaceDLL.API.NFDISTORTER_SHAREDMEM memShared = new NFDistorterInterfaceDLL.API.NFDISTORTER_SHAREDMEM();
            TrueDimensionOn = NFDistorterInterfaceDLL.API.NFMemoryAccessAdquireLock(-1, -1, ref memShared);
            NFDistorterInterfaceDLL.API.NFMemoryAccessReleaseLock();
            ///////////////////////

            Omnity.onReloadStartCallback += (omnity) => {
                Unload();
            };

            Omnity.onReloadEndCallbackPriority.AddLoaderFunction(PriorityEventHandler.Ordering.Order_Cobra, CoroutineOnLoadFullFunction);

            onNewTextureCallback += (Texture tex) => {
                Omnity anOmnity = gameObject.GetComponent<Omnity>();
                if (!anOmnity.PluginEnabled(OmnityPluginsIDs.CobraHelper)) {
                    this.enabled = false;
                    return;
                } else {
                    this.enabled = true;
                    ReconnectTextures(Omnity.anOmnity, tex);
                }
            };

            Omnity.onSaveCompleteCallback += Save;
        }
    }

    public TextureSource textureSource = TextureSource.PerspectiveCamera;
    public bool disableTrueDimensionOnStart = true;
    public bool restoreTrueDimensionStateOnExit = true;

    private static bool DLL_LOADED = false;
    private static bool TrueDimensionOn = false;
    private static OmnityCobraHelper singleton;

    public enum TextureSource {
        TextureFromApplication = 0,
        PerspectiveCamera = 1,
        FinalPassCamera = 2,
    }

    [System.NonSerialized]
    private bool lastWarpState = true;

    static public OmnityCobraHelper Get() {
        return singleton;
    }

    public void OnDestroy() {
        base.OnDisable();
        Unload();
        if (DLL_LOADED) {
            NFDistorterInterfaceDLL.API.NFMemoryAccessClose();
        }
        NFDistorterInterfaceDLL.Destructor();
        DLL_LOADED = false;
    }

    private System.Collections.IEnumerator CoroutineOnLoadFullFunction(Omnity anOmnity) {
        if (anOmnity.PluginEnabled(OmnityPluginsIDs.CobraHelper)) {
            this.enabled = true;
            yield return anOmnity.StartCoroutine(Load(anOmnity));
        } else {
            this.enabled = false;
        }
        yield break;
    }

    private const int layerMono = 0;

    public override void Unload() {
        if (myScreenShapeRight != null) {
            if (myScreenShapeRight != null && myScreenShapeRight.trans != null && myScreenShapeRight.trans.GetComponent<MeshFilter>() != null && myScreenShapeRight.trans.GetComponent<MeshFilter>().sharedMesh != null) {
                GameObject.Destroy(myScreenShapeRight.trans.GetComponent<MeshFilter>().sharedMesh);
                myScreenShapeRight.trans.GetComponent<MeshFilter>().sharedMesh = null;
            }
            Omnity.anOmnity.RemoveAndDestroyScreenShape(myScreenShapeRight);
        }

        if (myScreenShapeLeft != null) {
            if (myScreenShapeLeft != null && myScreenShapeLeft.trans != null && myScreenShapeLeft.trans.GetComponent<MeshFilter>() != null && myScreenShapeLeft.trans.GetComponent<MeshFilter>().sharedMesh != null) {
                GameObject.Destroy(myScreenShapeLeft.trans.GetComponent<MeshFilter>().sharedMesh);
                myScreenShapeLeft.trans.GetComponent<MeshFilter>().sharedMesh = null;
            }
            Omnity.anOmnity.RemoveAndDestroyScreenShape(myScreenShapeLeft);

            EnableTrueDimension();
        }

        myScreenShapeRight = null;
        myScreenShapeLeft = null;
    }

    private void EnableTrueDimension() {
        if (DLL_LOADED && TrueDimensionOn) {
            if (restoreTrueDimensionStateOnExit) {
                NFDistorterInterfaceDLL.API.NFMemoryAccessSetWarping(lastWarpState);
            }
        }
    }

    private void DisableTrueDimension() {
        if (DLL_LOADED && TrueDimensionOn) {
            lastWarpState = NFDistorterInterfaceDLL.API.NFMemoryAccessGetWarping();
            if (disableTrueDimensionOnStart) {
                NFDistorterInterfaceDLL.API.NFMemoryAccessSetWarping(false);
            }
        }
    }

    override public System.Collections.IEnumerator PostLoad() {
        DisableTrueDimension();
        StopAllCoroutines();
        yield return StartCoroutine(CoLoad(Omnity.anOmnity));
    }

    private void ConnectTextures() {
        ReconnectTextures(Omnity.anOmnity, getLatestTextureCallback());
    }

    private void ConnectShader(ScreenShape screenShape) {
        switch (textureSource) {
            case TextureSource.FinalPassCamera:
                screenShape.ConnectFinalPassShader(FinalPassShaderType.Cobra_TrueDimensionOffFlat);
                break;

            case TextureSource.PerspectiveCamera:
                screenShape.ConnectFinalPassShader(FinalPassShaderType.Cobra_TrueDimensionOff);
                break;

            case TextureSource.TextureFromApplication:
                screenShape.ConnectFinalPassShader(FinalPassShaderType.Cobra_TrueDimensionOffFlat);
                break;

            default:
                screenShape.ConnectFinalPassShader(FinalPassShaderType.Cobra_TrueDimensionOff);
                Debug.LogError("UNKNOWN MODE " + textureSource.ToString());
                break;
        }
    }

    private bool isStereoRig(Omnity myOmnity) {
        if (myOmnity == null) return false;
        if (myOmnity.cameraArray == null) return false;
        if (myOmnity.cameraArray.Length <= 0) return false;
        if (myOmnity.cameraArray[0].renderTextureSettings == null) return false;
        return myOmnity.cameraArray[0].renderTextureSettings.stereoPair;
    }

    private System.Collections.IEnumerator CoLoad(Omnity myOmnity) {
        yield return null;
        //    int screenLayer = 29;
        //   int fpcLayerMask = 1 << screenLayer;

        if (isStereoRig(myOmnity)) {
        myScreenShapeRight = myOmnity.AddScreenShape(_serialize: false, _automaticallyConnectTextures: true, _screenShapeType: OmnityScreenShapeType.CustomApplicationLoaded, _finalPassShaderType: FinalPassShaderType.Custom_ApplicationLoaded, spawnNow: false);
            myScreenShapeRight.eyeTarget = OmnityTargetEye.Right;
            myScreenShapeRight.name = "CobraScreen Right";
            myScreenShapeRight.layer = 29;
        myScreenShapeRight.SpawnScreenShape(myOmnity);
        }
        myScreenShapeLeft = myOmnity.AddScreenShape(_serialize: false, _automaticallyConnectTextures: true, _screenShapeType: OmnityScreenShapeType.CustomApplicationLoaded, _finalPassShaderType: FinalPassShaderType.Custom_ApplicationLoaded, spawnNow: false);
        myScreenShapeLeft.name = isStereoRig(myOmnity) ? "CobraScreen Left" : "CobraScreen";
        myScreenShapeLeft.eyeTarget = isStereoRig(myOmnity) ? OmnityTargetEye.Left : OmnityTargetEye.Both;
        myScreenShapeLeft.layer = 30;
        myScreenShapeLeft.SpawnScreenShape(myOmnity);

        yield return null;
        yield return null;

        while (!FinalPassSetup.isDoneLoading) {
            yield return null;
        }
        if (isStereoRig(myOmnity)) {
            UpdateMesh(myScreenShapeRight);
            ConnectShader(myScreenShapeRight);
        }
            UpdateMesh(myScreenShapeLeft);
            ConnectShader(myScreenShapeLeft);
        ConnectTextures();
        myOmnity.forceRefresh = true;
        yield break;
    }

    private ScreenShape myScreenShapeRight = null;
    private ScreenShape myScreenShapeLeft = null;

    public override void WriteXMLDelegate(XmlTextWriter xmlWriter) {
        xmlWriter.WriteElementString("textureSource", textureSource.ToString());
        xmlWriter.WriteElementString("disableTrueDimensionOnStart", disableTrueDimensionOnStart.ToString());
        xmlWriter.WriteElementString("restoreTrueDimensionOnExit", restoreTrueDimensionStateOnExit.ToString());
    }

    public override void ReadXMLDelegate(XPathNavigator nav) {
        textureSource = OmnityHelperFunctions.ReadElementEnumDefault<TextureSource>(nav, "(.//textureSource)", TextureSource.TextureFromApplication);
        disableTrueDimensionOnStart = OmnityHelperFunctions.ReadElementBoolDefault(nav, "(.//disableTrueDimensionOnStart)", true);
        restoreTrueDimensionStateOnExit = OmnityHelperFunctions.ReadElementBoolDefault(nav, "(.//restoreTrueDimensionStateOnExit)", true);
    }

    public override string BaseName {
        get {
            return "CobraHelper";
        }
    }

    public override void MyGuiCallback(Omnity anOmnity) {
        if (!anOmnity.pluginIDs.Contains((int)OmnityPluginsIDs.CobraHelper)) {
            return;
        }
        Tabs(anOmnity);

        if (OmnityHelperFunctions.EnumInputResetWasChanged<TextureSource>("SDM.textureSource", "textureSource", ref textureSource, TextureSource.TextureFromApplication, 1)) {
            Debug.LogError("TODO DEAL WITH THIS CHANGE SDM.textureSource");
        }

        disableTrueDimensionOnStart = OmnityHelperFunctions.BoolInputReset("Disable TrueDimension On Start", disableTrueDimensionOnStart, true);
        restoreTrueDimensionStateOnExit = OmnityHelperFunctions.BoolInputReset("Restore TrueDimensionState On Exit", restoreTrueDimensionStateOnExit, true);

        SaveLoadGUIButtons(anOmnity);
    }

    public void Tabs(Omnity anOmnity) {
        GUILayout.BeginHorizontal();
        GUILayout.EndHorizontal();
    }

    // Use this for initialization
    private void Start() {
    }

    public System.Action<Texture> onNewTextureCallback = delegate {
    };

    public System.Func<Texture> getLatestTextureCallback = () => {
        Texture t = null;
        switch (OmnityCobraHelper.Get().textureSource) {
            case TextureSource.TextureFromApplication:
                Debug.LogError("Replace with callback Playlist.tex");
                return null;

            case TextureSource.FinalPassCamera:
                for (int i = 0; i < Omnity.anOmnity.finalPassCameras.Length; i++) {
                    if (Omnity.anOmnity.finalPassCameras[i].renderTextureSettings.rt != null) {
                        return Omnity.anOmnity.finalPassCameras[i].renderTextureSettings.rt;
                    }
                }
                Debug.Log("Render texture needs to be set up on a final pass camera");
                return null;

            case TextureSource.PerspectiveCamera:
                if (Omnity.anOmnity.cameraArray.Length > 0) {
                    t = Omnity.anOmnity.cameraArray[0].renderTextureSettings.rt;
                }
                break;

            default:

                Debug.LogError("Unknown type " + OmnityCobraHelper.Get().textureSource);
                return null;
        }
        if (t == null) {
            Debug.LogError(OmnityCobraHelper.Get().textureSource.ToString() + " Error: Make sure to add a camera with a render texture");
        }
        return t;
    };

    public System.Func<bool> getLatestFlipY = () => {
        switch (OmnityCobraHelper.Get().textureSource) {
            case TextureSource.TextureFromApplication:
                Debug.LogError("Replace with callback Playlist.tex");
                return false;

            case TextureSource.FinalPassCamera:
                return false;

            case TextureSource.PerspectiveCamera:
                return false;

            default:
                Debug.LogError("Unknown " + OmnityCobraHelper.Get().textureSource);
                return false;
        }
    };

    public void ReconnectTextures(Omnity anOmnity, Texture tt) {
        if (!anOmnity.PluginEnabled(OmnityPluginsIDs.CobraHelper)) {
            return;
        }

        switch (textureSource) {
            case TextureSource.PerspectiveCamera:
                Omnity.anOmnity.DoConnectTextures();
                break;

            case TextureSource.FinalPassCamera:
                Debug.LogError("Todo Connect Final Pass Camera");
                break;

            case TextureSource.TextureFromApplication:
                if (myScreenShapeLeft.renderer != null) {
                    myScreenShapeLeft.renderer.sharedMaterial.mainTexture = tt;
                    if (getLatestFlipY()) {
                        Debug.Log("ADD flip in shader");
                        myScreenShapeLeft.renderer.sharedMaterial.mainTextureOffset = new Vector2(0, 1);
                        myScreenShapeLeft.renderer.sharedMaterial.mainTextureScale = new Vector2(1, -1);
                    } else {
                        myScreenShapeLeft.renderer.sharedMaterial.mainTextureOffset = new Vector2(0, 0);
                        myScreenShapeLeft.renderer.sharedMaterial.mainTextureScale = new Vector2(1, 1);
                    }
                }
                break;

            default:

                break;
        }
    }

    private bool warningShown = false;

    override public void Update() {
        if (!Omnity.anOmnity.PluginEnabled(OmnityPluginsIDs.CobraHelper)) {
            enabled = false;
            return;
        }
        base.Update();

        if (DLL_LOADED) {
            if (!TrueDimensionOn) {
                if (!warningShown) {
                    Debug.LogError("CobraTrueDimension not running or wrong version");
                    warningShown = true;
                }
                return;
            }
            if (NFDistorterInterfaceDLL.API.NFMemoryAccessTestChange()) {
                if (myScreenShapeRight != null) {
                    UpdateMesh(myScreenShapeRight);
                }
                if (myScreenShapeLeft != null) {
                    UpdateMesh(myScreenShapeLeft);
                }
            }
        }
    }

    private void UpdateMesh(ScreenShape s) {
        if (!DLL_LOADED || !TrueDimensionOn) {
            return;
        }
        Mesh.Destroy(s.trans.GetComponent<MeshFilter>().sharedMesh);
        s.trans.GetComponent<MeshFilter>().sharedMesh = GetLatestMesh();
        System.GC.Collect();
    }

    private Mesh GetLatestMesh() {
        if (!DLL_LOADED || !TrueDimensionOn) {
            return null;
        }
        bool success = true;
        if (!success) {
            Debug.Log("Constructor() -> " + success);
        }

        NFDistorterInterfaceDLL.API.NFDISTORTER_SHAREDMEM memShared = new NFDistorterInterfaceDLL.API.NFDISTORTER_SHAREDMEM();
        NFDistorterInterfaceDLL.API.NFDISTORTER_SHAREDMEM memSafe = new NFDistorterInterfaceDLL.API.NFDISTORTER_SHAREDMEM();

        int w = -1;
        int h = -1;
        success &= NFDistorterInterfaceDLL.API.NFMemoryAccessAdquireLock(w, h, ref memShared); // should be resolution of cobra monitor
        if (!success) {
            Debug.Log("NFDistorterInterfaceDLL.API.NFMemoryAccessAdquireLock(" + w + ", " + h + ", ref mem) -> " + success);
        }
        try {
            memSafe = memShared.Duplicate(); // deep copy
        } catch {
        }
        NFDistorterInterfaceDLL.API.NFMemoryAccessReleaseLock();

        return CreateMesh(memSafe);
    }

    private Mesh CreateMesh(NFDistorterInterfaceDLL.API.NFDISTORTER_SHAREDMEM memSafe) {
        Mesh m = new Mesh();
        System.Collections.Generic.List<Vector3> verts = new System.Collections.Generic.List<Vector3>();
        System.Collections.Generic.List<Vector3> norms = new System.Collections.Generic.List<Vector3>();
        System.Collections.Generic.List<Vector2> uv = new System.Collections.Generic.List<Vector2>();
        System.Collections.Generic.List<int> list = new System.Collections.Generic.List<int>();

        int totalTriangles = memSafe.mesh_TotalVertices[0] - 2;

        for (int i = 0; i < memSafe.mesh_TotalVertices[0]; i++) {
            verts.Add(new Vector3(memSafe.mesh_GLVertexData[i].x, memSafe.mesh_GLVertexData[i].y, memSafe.mesh_GLVertexData[i].z));
            norms.Add(new Vector3(memSafe.mesh_NormalData[i].x, memSafe.mesh_NormalData[i].y, memSafe.mesh_NormalData[i].z));
            uv.Add(new Vector3(memSafe.mesh_GLTextureData[i].x, memSafe.mesh_GLTextureData[i].y));
        }
        for (int i = 0; i < totalTriangles; i++) {
            if (1 == (i % 2)) {
                list.Add(i);
                list.Add(i + 1);
                list.Add(i + 2);
            } else {
                list.Add(i + 2);
                list.Add(i + 1);
                list.Add(i);
            }
        }

        m.vertices = verts.ToArray();
        m.normals = norms.ToArray();
        m.uv = uv.ToArray();
        m.triangles = list.ToArray();
        OmnityPlatformDefines.OptimizeMesh(m);
        return m;
    }
}
using OmnityMono.OmnitySDMNamespace;
using System.Collections;
using System.Xml;
using System.Xml.XPath;
using UnityEngine;

public class OmnitySDMManagerOrthographic : OmnityTabbedFeature {
    static public OmnityPluginsIDs _myOmnityPluginsID = OmnityPluginsIDs.SDMCalibrationOrthographic;
    override public OmnityPluginsIDs myOmnityPluginsID { get { return _myOmnityPluginsID; } }

    static public OmnitySDMManagerOrthographic OmniCreateSingleton(GameObject go) {
        return GetSingleton<OmnitySDMManagerOrthographic>(ref singleton, go);
    }

    public static OmnitySDMManagerOrthographic singleton;

    public enum TextureSource {
        TextureFromApplication = 0,
        PerspectiveCamera = 1,
        FinalPassCamera = 2,
    }

    public TextureSource textureSource = TextureSource.TextureFromApplication;

    static public OmnitySDMManagerOrthographic Get() {
        return singleton;
    }

    private void Awake() {
        if (VerifySingleton<OmnitySDMManagerOrthographic>(ref singleton)) {
            Omnity.onReloadEndCallbackPriority.AddLoaderFunction(PriorityEventHandler.Ordering.SDMManagerOrthographic, CoroutineLoader);

            onNewTextureCallback += (Texture tex) => {
                Omnity anOmnity = gameObject.GetComponent<Omnity>();
                if (!anOmnity.PluginEnabled(OmnityPluginsIDs.SDMCalibrationOrthographic)) {
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

    private const int layerMono = 0;

    public override void ReadXMLDelegate(XPathNavigator nav) {
        textureSource = OmnityHelperFunctions.ReadElementEnumDefault<TextureSource>(nav, "(.//textureSource)", TextureSource.TextureFromApplication);
    }

    public override void WriteXMLDelegate(XmlTextWriter xmlWriter) {
        xmlWriter.WriteElementString("textureSource", textureSource.ToString());
    }

    public override IEnumerator PostLoad() {
        StopAllCoroutines();
        if (Omnity.anOmnity.PluginEnabled(OmnityPluginsIDs.SDMCalibrationOrthographic)) {
            yield return StartCoroutine(CoLoad(Omnity.anOmnity));
        }

    }

    private System.Collections.IEnumerator CoLoad(Omnity myOmnity) {
        // no need to clear existing arrays because of nonser var
        //   myOmnity.CameraArrayResize(0);
        //   myOmnity.ScreenShapeArrayResize(0);
        //   myOmnity.FinalPassCameraArrayResize(0);
        OmnitySDMExtensions.ClearMasks();
        yield return null;
        int rcLayerMask = 0;
        int screenLayer = 29;
        int fpcLayerMask = 1 << screenLayer;
        SDMPROJECTOR[] ps = OmnitySDMExtensions.GetProjectors(true, false);
        int startLengthSS = myOmnity.screenShapes.Length;
        int startLengthFPC = myOmnity.finalPassCameras.Length;
        yield return StartCoroutine(OmnitySDMExtensionsLocal.CoRoutineLoadFileSetOthographic(ps, rcLayerMask, screenLayer, fpcLayerMask, "mono", false, false));

        for (int i = 0; i < ps.Length; i++) {
            int myScreenLayer = screenLayer - i;
            myOmnity.screenShapes[startLengthSS + i].layer = myScreenLayer;
            if (myOmnity.screenShapes[startLengthSS + i].trans != null) {
                myOmnity.screenShapes[startLengthSS + i].trans.gameObject.layer = myScreenLayer;
            }
            myOmnity.finalPassCameras[startLengthFPC + i].cullingMask = 1 << myScreenLayer;
            if (myOmnity.finalPassCameras[startLengthFPC + i].myCamera != null) {
                myOmnity.finalPassCameras[startLengthFPC + i].myCamera.cullingMask = 1 << myScreenLayer;
            }
            myOmnity.screenShapes[startLengthSS + i].trans.parent = null;
            myOmnity.finalPassCameras[startLengthFPC + i].myCameraTransform.parent = null;
            myOmnity.screenShapes[startLengthSS + i].trans.localScale = Vector3.one;
            myOmnity.finalPassCameras[startLengthFPC + i].myCameraTransform.localScale = Vector3.one;
        }

        yield return null;
        yield return null;
        yield return null;
        ReconnectTextures(myOmnity, getLatestTextureCallback());
    }

    public override string BaseName {
        get {
            return "OmnitySDMManagerOrthographicSettings.xml";
        }
    }

    private float _Gamma1 = 1.0f;
    private float _Gamma2 = 1.0f;
    private float _Gamma3 = 1.0f;
    private float _Gamma4 = 1.0f;

    private bool _Gamma1Inv = false;
    private bool _Gamma2Inv = false;
    private bool _Gamma3Inv = false;
    private bool _Gamma4Inv = false;

    public override void MyGuiCallback(Omnity anOmnity) {
        if (!anOmnity.pluginIDs.Contains((int)OmnityPluginsIDs.SDMCalibrationOrthographic)) {
            return;
        }
        Tabs(anOmnity);

        if (OmnityHelperFunctions.EnumInputResetWasChanged<TextureSource>("SDM.textureSource", "textureSource", ref textureSource, TextureSource.TextureFromApplication, 1)) {
            Debug.LogError("TODO DEAL WITH THIS CHANGE SDM.textureSource");
        }

        bool changed1 = OmnityHelperFunctions.BoolInputResetWasChanged("Gamma1Inv", ref _Gamma1Inv, false);
        if (OmnityHelperFunctions.FloatInputResetSliderWasChanged("Gamma1", ref _Gamma1, 0.0f, 1.0f, 3.0f) || changed1) {
            for (int i = 0; i < anOmnity.screenShapes.Length; i++) {
                anOmnity.screenShapes[i].renderer.sharedMaterial.SetFloat("_Gamma1", _Gamma1Inv ? (1.0f / _Gamma1) : _Gamma1);
            }
        }
        bool changed2 = OmnityHelperFunctions.BoolInputResetWasChanged("Gamma2Inv", ref _Gamma2Inv, false);

        if (OmnityHelperFunctions.FloatInputResetSliderWasChanged("Gamma2", ref _Gamma2, 0.0f, 1.0f, 3.0f) || changed2) {
            for (int i = 0; i < anOmnity.screenShapes.Length; i++) {
                anOmnity.screenShapes[i].renderer.sharedMaterial.SetFloat("_Gamma2", _Gamma2Inv ? (1.0f / _Gamma2) : _Gamma2);
            }
        }
        bool changed3 = OmnityHelperFunctions.BoolInputResetWasChanged("Gamma3Inv", ref _Gamma3Inv, false);

        if (OmnityHelperFunctions.FloatInputResetSliderWasChanged("Gamma3", ref _Gamma3, 0.0f, 1.0f, 3.0f) || changed3) {
            for (int i = 0; i < anOmnity.screenShapes.Length; i++) {
                anOmnity.screenShapes[i].renderer.sharedMaterial.SetFloat("_Gamma3", _Gamma3Inv ? (1.0f / _Gamma3) : _Gamma3);
            }
        }

        bool changed4 = OmnityHelperFunctions.BoolInputResetWasChanged("Gamma4Inv", ref _Gamma4Inv, false);
        if (OmnityHelperFunctions.FloatInputResetSliderWasChanged("Gamma4", ref _Gamma4, 0.0f, 1.0f, 3.0f) || changed4) {
            for (int i = 0; i < anOmnity.screenShapes.Length; i++) {
                anOmnity.screenShapes[i].renderer.sharedMaterial.SetFloat("_Gamma4", _Gamma4Inv ? (1.0f / _Gamma4) : _Gamma4);
            }
        }

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Save")) {
            Save(anOmnity);
        }
        if (GUILayout.Button("Reload")) {
            anOmnity.StartCoroutine(Load(anOmnity));
        }
        GUILayout.EndHorizontal();
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
        if (OmnitySDMManagerOrthographic.Get() == null || Omnity.anOmnity == null) {
            return null;
        }
        switch (OmnitySDMManagerOrthographic.Get().textureSource) {
            case TextureSource.TextureFromApplication:
                Debug.LogError("Replace with callback Playlist.tex");
                return null;

            case TextureSource.FinalPassCamera:
                if (Omnity.anOmnity.finalPassCameras.Length > 0) {
                    t = Omnity.anOmnity.finalPassCameras[0].renderTextureSettings.rt;
                }
                break;

            case TextureSource.PerspectiveCamera:
                if (Omnity.anOmnity.cameraArray.Length > 0) {
                    t = Omnity.anOmnity.cameraArray[0].renderTextureSettings.rt;
                }
                break;

            default:

                Debug.LogError("Unknown type " + OmnitySDMManagerOrthographic.Get().textureSource);
                return null;
        }
        if (t == null) {
            Debug.LogError(OmnitySDMManagerOrthographic.Get().textureSource.ToString() + " Error: Make sure to add a camera with a render texture");
        }
        return t;
    };

    public System.Func<bool> getLatestFlipY = () => {
        switch (OmnitySDMManagerOrthographic.Get().textureSource) {
            case TextureSource.TextureFromApplication:
                Debug.LogError("Replace with callback Playlist.tex");
                return false;

            case TextureSource.FinalPassCamera:
                return false;

            case TextureSource.PerspectiveCamera:
                return false;

            default:
                Debug.LogError("Unknown " + OmnitySDMManagerOrthographic.Get().textureSource);
                return false;
        }
    };

    public void ReconnectTextures(Omnity anOmnity, Texture tt) {
        if (!anOmnity && anOmnity.screenShapes != null) {
            return;
        }
        if (!anOmnity.PluginEnabled(OmnityPluginsIDs.SDMCalibrationOrthographic)) {
            return;
        }

        foreach (var ss in anOmnity.screenShapes) {
            if (!ss.automaticallyConnectTextures) {
                if (ss.renderer != null && ss.renderer.sharedMaterial != null) {
                    ss.renderer.sharedMaterial.mainTexture = tt;
                    if (getLatestFlipY()) {
                        ss.renderer.sharedMaterial.mainTextureOffset = new Vector2(0, 1);
                        ss.renderer.sharedMaterial.mainTextureScale = new Vector2(1, -1);
                    } else {
                        ss.renderer.sharedMaterial.mainTextureOffset = new Vector2(0, 0);
                        ss.renderer.sharedMaterial.mainTextureScale = new Vector2(1, 1);
                    }
                }
            }
        }
    }
}
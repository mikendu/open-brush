using OmnityMono;
using OmnityMono.OmnitySDMNamespace;
using System.Xml;
using System.Xml.XPath;
using UnityEngine;

public class OmnitySDMManager : OmnityTabbedFeature {
    static public OmnityPluginsIDs _myOmnityPluginsID = OmnityPluginsIDs.SDMCalibration;
    override public OmnityPluginsIDs myOmnityPluginsID { get { return _myOmnityPluginsID; } }

    static public OmnitySDMManager OmniCreateSingleton(GameObject go) {
        return GetSingleton<OmnitySDMManager>(ref singleton, go);
    }

    private static OmnitySDMManager singleton;
    public FinalPassShaderType edgeBlendVertexLitShader = FinalPassShaderType.EdgeBlendVertexColor;
    public bool stereo = false;

    static public OmnitySDMManager Get() {
        return singleton;
    }

    private void Awake() {
        if (VerifySingleton<OmnitySDMManager>(ref singleton)) {
            // try and find the shader, if this is null;
            Omnity.onReloadEndCallbackPriority.AddLoaderFunction(PriorityEventHandler.Ordering.SDMManager, CoroutineOnLoadFullFunction);
            Omnity.onSaveCompleteCallback += Save;
        }
    }

    private System.Collections.IEnumerator CoroutineOnLoadFullFunction(Omnity anOmnity) {
        if (anOmnity.PluginEnabled(OmnityPluginsIDs.SDMCalibration)) {
            this.enabled = true;
            StopAllCoroutines();
            yield return StartCoroutine(Load(anOmnity));
        } else {
            this.enabled = false;
        }
        yield break;
    }

    private const int layerMono = 0;
    private const int layerStereoLeft = 0;
    private const int layerStereoRight = 1;

    private System.Collections.IEnumerator CoLoad(Omnity myOmnity, FinalPassShaderType s) {
        myOmnity.CameraArrayResize(0, false, false);
        myOmnity.ScreenShapeArrayResize(0, false, false);
        myOmnity.FinalPassCameraArrayResize(0, false);
        OmnitySDMExtensions.ClearMasks();
        yield return null;
        if (stereo) {
            int rcLayerMaskL = 1 << 0;
            int screenLayerL = 29;
            int fpcLayerMaskL = 1 << screenLayerL;

            int rcLayerMaskR = 1 << 1;
            int screenLayerR = 30;
            int fpcLayerMaskR = 1 << screenLayerR;

            yield return StartCoroutine(OmnitySDMExtensionsLocal.CoRoutineLoadFileSet(OmnitySDMExtensions.GetProjectors(false, false), rcLayerMaskL, screenLayerL, fpcLayerMaskL, s, "left", true));

            float orthoSizeX = 0;
            float orthoSizeY = 0;
            foreach (var rc in myOmnity.cameraArray) {
                orthoSizeX += rc.myCamera.targetTexture.width;
                orthoSizeY = Mathf.Max(orthoSizeY, rc.myCamera.targetTexture.height);
            }

            yield return StartCoroutine(OmnitySDMExtensionsLocal.CoRoutineLoadFileSet(OmnitySDMExtensions.GetProjectors(false, false), rcLayerMaskR, screenLayerR, fpcLayerMaskR, s, "right", true));

            myOmnity.FinalPassCameraArrayResize(2);
            myOmnity.finalPassCameras[0].cullingMask = fpcLayerMaskL;
            myOmnity.finalPassCameras[1].cullingMask = fpcLayerMaskR;
            myOmnity.finalPassCameras[0].name = "SDM Stereo Combiner Camera Left";
            myOmnity.finalPassCameras[1].name = "SDM Stereo Combiner Camera Right";
            myOmnity.finalPassCameras[0].myCamera.name = "SDM Stereo Combiner Camera Left";
            myOmnity.finalPassCameras[1].myCamera.name = "SDM Stereo Combiner Camera Right";
            yield return null;

            myOmnity.finalPassCameras[0].myCamera.gameObject.layer = screenLayerL;
            myOmnity.finalPassCameras[1].myCamera.gameObject.layer = screenLayerR;

            for (int i = 0; i < 2; i++) {
                myOmnity.finalPassCameras[i].myCamera.cullingMask = myOmnity.finalPassCameras[i].cullingMask;
                myOmnity.finalPassCameras[i].projectorType = OmnityProjectorType.Rectilinear;
                myOmnity.finalPassCameras[i].omnityPerspectiveMatrix.matrixMode = MatrixMode.Orthographic;
                myOmnity.finalPassCameras[i].omnityPerspectiveMatrix.orthographicSize = orthoSizeY / 2.0f;
                myOmnity.finalPassCameras[i].omnityPerspectiveMatrix.nearOrtho = -1;
                myOmnity.finalPassCameras[i].omnityPerspectiveMatrix.near = -1;
                myOmnity.finalPassCameras[i].omnityPerspectiveMatrix.farOrtho = 1;
                myOmnity.finalPassCameras[i].omnityPerspectiveMatrix.far = 1;

                myOmnity.finalPassCameras[i].transformInfo.position = new Vector3(orthoSizeX / 2f, orthoSizeY / 2f, 0);

                myOmnity.finalPassCameras[i].renderTextureSettings.height = (int)orthoSizeY;
                myOmnity.finalPassCameras[i].renderTextureSettings.width = (int)orthoSizeX;

                myOmnity.finalPassCameras[i].renderTextureSettings.enabled = true;
                myOmnity.finalPassCameras[i].renderTextureSettings.GenerateRenderTexture();

                myOmnity.finalPassCameras[i].UpdateCamera();
                myOmnity.finalPassCameras[i].myCamera.targetTexture = myOmnity.finalPassCameras[i].renderTextureSettings.rt;
                myOmnity.finalPassCameras[i].myCamera.ResetProjectionMatrix();
                myOmnity.finalPassCameras[i].myCamera.nearClipPlane = -1;
                myOmnity.finalPassCameras[i].myCamera.farClipPlane = 1;
                myOmnity.finalPassCameras[i].myCamera.orthographicSize = orthoSizeY / 2.0f;
                myOmnity.finalPassCameras[i].transformInfo.position = new Vector3(orthoSizeX / 2, orthoSizeY / 2, 0);

                myOmnity.finalPassCameras[i].myCamera.transform.gameObject.name = "FinalPass" + ((i == 0) ? "Left" : "Right");

                if (myOmnity.pluginIDs.Contains((int)OmnityPluginsIDs.ClipMapperSDM)) {
                    OmnitySDMClipMapper.singleton.ReconnectTextures(myOmnity, OmnitySDMClipMapper.singleton.getLatestTextureCallback());
                }
            }

            if (myOmnity.pluginIDs.Contains((int)OmnityPluginsIDs.StereoCombiner)) {
                foreach (var lfpcg in StereoCombinerManager.Get().linkedFinalPassCameraGroupsPM) {
                    if (lfpcg.linked.Count == 2) {
                        lfpcg.linked[0] = myOmnity.finalPassCameras[0];
                        lfpcg.linked[1] = myOmnity.finalPassCameras[1];
                    }
                }
                StereoCombinerManager.Get().LinkCombiners(myOmnity);
                StereoCombinerManager.Get().linkedFinalPassCameraGroupsPM[0].extraSettings.combinerPlane.transform.localScale = new Vector3(orthoSizeX, 1, orthoSizeY);
                StereoCombinerManager.CustomOthroSize = orthoSizeY * 5 * .10f;
                //        StereoCombinerManager.Get().GetCombinerCamera().orthographicSize = orthoSizeY * 5;
                foreach (var lfpcg in StereoCombinerManager.Get().linkedFinalPassCameraGroupsPM) {
                    StereoCombinerManager.Get().ChangedStereo(myOmnity, lfpcg, true);
                }
            }
        } else {
            int rcLayerMask = 0;
            int screenLayer = 29;
            int fpcLayerMask = 1 << screenLayer;
            yield return StartCoroutine(OmnitySDMExtensionsLocal.CoRoutineLoadFileSet(OmnitySDMExtensions.GetProjectors(false, false), rcLayerMask, screenLayer, fpcLayerMask, s, "mono", false));
            foreach (var rc in myOmnity.cameraArray) {
                rc.cullingMask = 1 << 0;
                if (rc.myCamera != null) {
                    rc.myCamera.cullingMask = 1 << 0;
                }
            }
        }

        ApplyShaderVariables(myOmnity);
    }

    public override void ReadXMLDelegate(XPathNavigator nav) {
        stereo = OmnityHelperFunctions.ReadElementBoolDefault(nav, "(.//stereo)", false);
        _floor = OmnityHelperFunctions.ReadElementFloatDefault(nav, "(.//floor)", 0);

        _floorOverall = OmnityHelperFunctions.ReadElementFloatDefault(nav, "(.//floorOverall)", 0);
        _floorGamma = OmnityHelperFunctions.ReadElementFloatDefault(nav, "(.//floorGamma)", 1);
        _floorGammaOffset = OmnityHelperFunctions.ReadElementFloatDefault(nav, "(.//floorGammaOffset)", 0);
        _floorGammaWidth = OmnityHelperFunctions.ReadElementFloatDefault(nav, "(.//floorGammaWidth)", 1);

        _Gamma1 = OmnityHelperFunctions.ReadElementFloatDefault(nav, "(.//Gamma1)", 1);
        _Gamma2 = OmnityHelperFunctions.ReadElementFloatDefault(nav, "(.//Gamma2)", 1);
        _Gamma3 = OmnityHelperFunctions.ReadElementFloatDefault(nav, "(.//Gamma3)", 1);
        _Gamma4 = OmnityHelperFunctions.ReadElementFloatDefault(nav, "(.//Gamma4)", 1);

        _Gamma1Inv = OmnityHelperFunctions.ReadElementBoolDefault(nav, "(.//Gamma1Inv)", false);
        _Gamma2Inv = OmnityHelperFunctions.ReadElementBoolDefault(nav, "(.//Gamma2Inv)", false);
        _Gamma3Inv = OmnityHelperFunctions.ReadElementBoolDefault(nav, "(.//Gamma3Inv)", false);
        _Gamma4Inv = OmnityHelperFunctions.ReadElementBoolDefault(nav, "(.//Gamma4Inv)", false);
    }

    public override void WriteXMLDelegate(XmlTextWriter xmlWriter) {
        xmlWriter.WriteElementString("stereo", stereo.ToString());

        xmlWriter.WriteElementString("floor", _floor.ToString());
        xmlWriter.WriteElementString("floorOverall", _floorOverall.ToString());
        xmlWriter.WriteElementString("floorGamma", _floorGamma.ToString());
        xmlWriter.WriteElementString("floorGammaOffset", _floorGammaOffset.ToString());
        xmlWriter.WriteElementString("floorGammaWidth", _floorGammaWidth.ToString());

        xmlWriter.WriteElementString("Gamma1", _Gamma1.ToString());
        xmlWriter.WriteElementString("Gamma2", _Gamma2.ToString());
        xmlWriter.WriteElementString("Gamma3", _Gamma3.ToString());
        xmlWriter.WriteElementString("Gamma4", _Gamma4.ToString());

        xmlWriter.WriteElementString("Gamma1Inv", _Gamma1Inv.ToString());
        xmlWriter.WriteElementString("Gamma2Inv", _Gamma2Inv.ToString());
        xmlWriter.WriteElementString("Gamma3Inv", _Gamma3Inv.ToString());
        xmlWriter.WriteElementString("Gamma4Inv", _Gamma4Inv.ToString());
    }

    public override System.Collections.IEnumerator PostLoad() {
        yield return StartCoroutine(CoLoad(Omnity.anOmnity, edgeBlendVertexLitShader));
    }

    public override string BaseName {
        get {
            return "SDMManagerSettings";
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

    private float _floor = 0.0f;
    private float _floorOverall = 0.0f;
    private float _floorGamma = 0.0f;
    private float _floorGammaOffset = 0.0f;
    private float _floorGammaWidth = 1.0f;

    public override void MyGuiCallback(Omnity anOmnity) {
        if (!anOmnity.pluginIDs.Contains((int)OmnityPluginsIDs.SDMCalibration)) {
            return;
        }
        Tabs(anOmnity);

        stereo = OmnityHelperFunctions.BoolInputReset("stereo", stereo, false);
        bool changed = false;

        GUI.skin.box.VerticalLayout(() => {
            GUILayout.Label("Adjust SDM's blend rate");
            changed |= OmnityHelperFunctions.BoolInputResetWasChanged("Gamma1Inv", ref _Gamma1Inv, false);
            changed |= OmnityHelperFunctions.FloatInputResetSliderWasChanged("Gamma1", ref _Gamma1, 0.0f, 1.0f, 3.0f);
            changed |= OmnityHelperFunctions.BoolInputResetWasChanged("Gamma2Inv", ref _Gamma2Inv, false);
            changed |= OmnityHelperFunctions.FloatInputResetSliderWasChanged("Gamma2", ref _Gamma2, 0.0f, 1.0f, 3.0f);
            changed |= OmnityHelperFunctions.BoolInputResetWasChanged("Gamma3Inv", ref _Gamma3Inv, false);
            changed |= OmnityHelperFunctions.FloatInputResetSliderWasChanged("Gamma3", ref _Gamma3, 0.0f, 1.0f, 3.0f);
            changed |= OmnityHelperFunctions.BoolInputResetWasChanged("Gamma4Inv", ref _Gamma4Inv, false);
            changed |= OmnityHelperFunctions.FloatInputResetSliderWasChanged("Gamma4", ref _Gamma4, 0.0f, 1.0f, 3.0f);
        });

        GUI.skin.box.VerticalLayout(() => {
            GUILayout.Label("Black Level Matching");
            changed |= OmnityHelperFunctions.FloatInputResetSliderWasChanged("Floor", ref _floor, 0.0f, 0.0f, 1.0f);
            changed |= OmnityHelperFunctions.FloatInputResetSliderWasChanged("FloorOverall", ref _floorOverall, 0.0f, 0.0f, 1.0f);
            changed |= OmnityHelperFunctions.FloatInputResetSliderWasChanged("FloorOffset", ref _floorGammaOffset, 0.0f, 0.0f, 1);
            changed |= OmnityHelperFunctions.FloatInputResetSliderWasChanged("FloorGammaWidth", ref _floorGammaWidth, 0, 1.0f, 1.0f);
            changed |= OmnityHelperFunctions.FloatInputResetSliderWasChanged("FloorGamma", ref _floorGamma, 0.0f, 1.0f, 33.0f);
            GUILayout.Label("Use floor to raise the black level on the regions where there is only 1 projector.  Use FloorOverall to raise the blacklevel on regions edge blend regions(this is only needed to account for tripple overlap regions).  floorGammaWidth makes the blend happen over a smaller range, instead of the entire overlap region. floorGammaOffset moves the blend region.  floorGamma changes the blend from linear to gamma.");
        });

        if (changed) {
            ApplyShaderVariables(anOmnity);
        }

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Save")) {
            Save(anOmnity);
        }
        if (GUILayout.Button("Reload")) {
            StopAllCoroutines();
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

    private void ApplyShaderVariables(Omnity anOmnity) {
        for (int i = 0; i < anOmnity.screenShapes.Length; i++) {
            anOmnity.screenShapes[i].renderer.sharedMaterial.SetFloat("_Gamma1", _Gamma1Inv ? (1.0f / _Gamma1) : _Gamma1);
            anOmnity.screenShapes[i].renderer.sharedMaterial.SetFloat("_Gamma2", _Gamma2Inv ? (1.0f / _Gamma2) : _Gamma2);
            anOmnity.screenShapes[i].renderer.sharedMaterial.SetFloat("_Gamma3", _Gamma3Inv ? (1.0f / _Gamma3) : _Gamma3);
            anOmnity.screenShapes[i].renderer.sharedMaterial.SetFloat("_Gamma4", _Gamma4Inv ? (1.0f / _Gamma4) : _Gamma4);
            anOmnity.screenShapes[i].renderer.sharedMaterial.SetFloat("_floor", _floor);
            anOmnity.screenShapes[i].renderer.sharedMaterial.SetFloat("_floorOverall", _floorOverall);
            anOmnity.screenShapes[i].renderer.sharedMaterial.SetFloat("_floorGamma", _floorGamma);
            anOmnity.screenShapes[i].renderer.sharedMaterial.SetFloat("_floorGammaOffset", _floorGammaOffset);

            float tempWidth = Mathf.Clamp(_floorGammaWidth, 0, .99f - _floorGammaOffset);

            anOmnity.screenShapes[i].renderer.sharedMaterial.SetFloat("_floorGammaWidth", tempWidth);
        }
    }
}
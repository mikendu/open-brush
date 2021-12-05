using OmnityMono;
using OmnityMono.OmnitySDMNamespace;
using System.Collections;
using System.Xml;
using System.Xml.XPath;
using UnityEngine;

public class OmnitySDMManagerStereoOrthoPersective : OmnityTabbedFeature {
    static public OmnityPluginsIDs _myOmnityPluginsID = OmnityPluginsIDs.SDMCalibrationStereoOrthoPerspective;
    override public OmnityPluginsIDs myOmnityPluginsID { get { return _myOmnityPluginsID; } }

    public bool fullscreenPerProjector = false;

    static public OmnitySDMManagerStereoOrthoPersective OmniCreateSingleton(GameObject go) {
        return GetSingleton<OmnitySDMManagerStereoOrthoPersective>(ref singleton, go);
    }

    public static OmnitySDMManagerStereoOrthoPersective singleton;

    static public OmnitySDMManagerStereoOrthoPersective Get() {
        return singleton;
    }

    public class ScreenMapper {
        public float xMin = -90;
        public float xMax = 90;
        public float yMin = -45;
        public float yMax = 45;

        public Rect rect {
            get {
                return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
            }
            set {
                xMin = value.xMin;
                xMax = value.xMax;
                yMin = value.yMin;
                yMax = value.yMax;
            }
        }
    }

    public System.Collections.Generic.List<ScreenMapper> screenMapperData = new System.Collections.Generic.List<ScreenMapper>();

    private void Awake() {
        if (VerifySingleton<OmnitySDMManagerStereoOrthoPersective>(ref singleton)) {
            // try and find the shader, if this is null;

            Omnity.onReloadEndCallbackPriority.AddLoaderFunction(PriorityEventHandler.Ordering.StereoOrthoPerspective, CoroutineLoader);

            Omnity.onSaveCompleteCallback += Save;
        }
    }

    private const int layerMono = 0;

    public string XMLTagName {
        get {
            return "OmnitySDMManagerStereoOrthoPersective";
        }
    }

    public override void WriteXMLDelegate(XmlTextWriter xmlWriter) {
        xmlWriter.WriteStartElement(XMLTagName);
        xmlWriter.WriteElementString("fullscreenPerProjector", fullscreenPerProjector.ToString());

        foreach (var group in screenMapperData) {
            xmlWriter.WriteStartElement("Screen");
            xmlWriter.WriteElementString("Screen.rect", group.rect.ToString());
            xmlWriter.WriteEndElement();
        }
        xmlWriter.WriteEndElement();
    }

    public override void ReadXMLDelegate(XPathNavigator nav) {
        screenMapperData.Clear();

        fullscreenPerProjector = OmnityHelperFunctions.ReadElementBoolDefault(nav, ".//fullscreenPerProjector", false);

        System.Xml.XPath.XPathNodeIterator group = nav.Select("(.//Screen)");
        while (group.MoveNext()) {
            try {
                System.Xml.XPath.XPathNavigator currentgroup = group.Current;
                Rect r = OmnityHelperFunctions.ReadElementRectDefault(currentgroup, ".//Screen.rect", new Rect(-90, -45, 180, 90));
                screenMapperData.Add(new ScreenMapper { rect = r });
            } catch (System.Exception e) {
                Debug.LogWarning("exception " + e.Message);
            }
        }
    }

    public override IEnumerator PostLoad() {
        StopAllCoroutines();
        yield return StartCoroutine(CoLoad(Omnity.anOmnity));
    }

    public const bool stereo = true;

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
        var ps = OmnitySDMExtensions.GetProjectors(true, true);
        int startLengthSS = myOmnity.screenShapes.Length;
        int startLengthFPC = myOmnity.finalPassCameras.Length;

        Debug.Log("START LENGTH FPC " + startLengthFPC);

        foreach (var p in ps) {
            while (p.screenIndex + 1 > screenMapperData.Count) {
                screenMapperData.Add(new ScreenMapper());
            }
        }

        yield return StartCoroutine(CoRoutineLoadFileSetOthographicPerspective(ps, rcLayerMask, screenLayer, fpcLayerMask, "mono", stereo));

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
            myOmnity.screenShapes[startLengthSS + i].trans.localScale = Vector3.one;
            myOmnity.finalPassCameras[startLengthFPC + i].myCameraTransform.localScale = Vector3.one;
            myOmnity.screenShapes[startLengthSS + i].trans.localPosition = Vector3.zero;
            myOmnity.screenShapes[startLengthSS + i].trans.localEulerAngles = Vector3.zero;
        }

        yield return null;
        yield return null;
        yield return null;
        ReconnectTextures(myOmnity);
    }

    public override string BaseName {
        get {
            return "StereoOrthoPerspective";
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
        if (!anOmnity.pluginIDs.Contains((int)OmnityPluginsIDs.SDMCalibrationStereoOrthoPerspective)) {
            return;
        }
        Tabs(anOmnity);

        fullscreenPerProjector = OmnityHelperFunctions.BoolInputReset("fullscreenPerProjector (true for ez3, false for ez1)",  fullscreenPerProjector, true);

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

        for (int i = 0; i < screenMapperData.Count; i++) {
            GUILayout.Label("channel " + i);
            screenMapperData[i].xMin = OmnityHelperFunctions.FloatInputReset("xMin", screenMapperData[i].xMin, -90);
            screenMapperData[i].xMax = OmnityHelperFunctions.FloatInputReset("xMax", screenMapperData[i].xMax, 90);
            screenMapperData[i].yMin = OmnityHelperFunctions.FloatInputReset("yMin", screenMapperData[i].yMin, -90);
            screenMapperData[i].yMax = OmnityHelperFunctions.FloatInputReset("yMax", screenMapperData[i].yMax, 90);
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

    public override void Update() {
        base.Update();
        ReconnectTextures(Omnity.anOmnity);
    }

    public void LateUpdate() {
        ReconnectTextures(Omnity.anOmnity);
    }

    public void ReconnectTextures(Omnity anOmnity) {
        if (!anOmnity && anOmnity.screenShapes != null) {
            return;
        }
        if (!anOmnity.PluginEnabled(OmnityPluginsIDs.SDMCalibrationStereoOrthoPerspective)) {
            return;
        }
        for (int ij = 0; ij < anOmnity.screenShapes.Length; ij++) {
            var ss = anOmnity.screenShapes[ij];
            if (!ss.automaticallyConnectTextures) {
                OmnityTargetEye target = ss.eyeTarget;
                ss.transformInfo.position = Vector3.zero;
                ss.renderer.enabled = false;

                if (ss.renderer != null && ss.renderer.sharedMaterial != null) {
                    int textureIndex = 0;
                    for (int i = 0; i < anOmnity.cameraArray.Length; i++) {
                        PerspectiveCamera p = anOmnity.cameraArray[i];
                        p.localPosition = Vector3.zero;
                        if (target == OmnityTargetEye.Left || target == OmnityTargetEye.Both) {
                            ss.renderer.sharedMaterial.SetTexture(Omnity.GetTexStringFast(textureIndex), p.renderTextureSettings.rt);
                        } else {
                            if (p.renderTextureSettings.rtR == null) {
                                Debug.LogError("Right render texture is null... make sure to enable the stereo render textures in the perspective camera");
                                ss.renderer.sharedMaterial.SetTexture(Omnity.GetTexStringFast(textureIndex), p.renderTextureSettings.rt);
                            } else {
                                ss.renderer.sharedMaterial.SetTexture(Omnity.GetTexStringFast(textureIndex), p.renderTextureSettings.rtR);
                            }
                        }
                        Omnity.anOmnity.DoSetShaderMatrix(ss, textureIndex, Omnity.GenerateProjectivePerspectiveProjectionMatrix(p.myCamera, ss.trans));

                        textureIndex++;
                    }
                    ss.SetJunkMatrices(textureIndex);
                }
            }
        }
    }

    public IEnumerator CoRoutineLoadFileSetOthographicPerspective(SDMPROJECTOR[] files, int rcLayerMask, int screenLayer, int fpcLayerMask, string prefix, bool stereo) {
        Debug.LogWarning("CoRoutineLoadFileSet");
        Omnity myOmnity = Omnity.anOmnity;
        int eyeCount = stereo ? 2 : 1;

        int startLengthSS = myOmnity.screenShapes.Length;
        int startLengthFPC = myOmnity.finalPassCameras.Length;

        myOmnity.ScreenShapeArrayResize(startLengthSS + files.Length * eyeCount, false, false);
        myOmnity.FinalPassCameraArrayResize(startLengthFPC + files.Length * eyeCount, false);

        yield return null;
        for (int iCameraIndex = 0; iCameraIndex < files.Length; iCameraIndex++) {
            for (int eye = 0; eye < eyeCount; eye++) {
                myOmnity.screenShapes[startLengthSS + iCameraIndex * eyeCount + eye].finalPassShaderType = FinalPassShaderType.EdgeBlendVertexColorPerspective;
            }
        }
        yield return null;
        yield return null;

        //     int offset = 0;

        int totalXRes = 0;
        int totalYRes = 0;

        for (int iProjectorIndex = 0; iProjectorIndex < files.Length; iProjectorIndex++) {
            if (System.IO.File.Exists(files[iProjectorIndex].maskFilename)) {
#pragma warning disable CS0618  
                WWW www3 = new WWW(files[iProjectorIndex].maskFilenameURL);
#pragma warning restore CS0618  
                yield return www3;
                try {
                    if (www3.texture) {
                        OmnitySDMExtensions.masks.Add(www3.texture);
                    } else {
                        OmnitySDMExtensions.masks.Add(null);
                    }
                } catch (System.Exception e) {
                    Debug.LogError(e.Message);
                }
            } else {
                OmnitySDMExtensions.masks.Add(null);
            }

            string myName = System.IO.Path.GetFileNameWithoutExtension(files[iProjectorIndex].file);
            Debug.Log(myName);
            int finalPassCameraLayer = screenLayer;

            for (int eye = 0; eye < eyeCount; eye++) {
                int iProjectorIndexStereo = iProjectorIndex * eyeCount + eye;
                var screenShape = myOmnity.screenShapes[startLengthSS + iProjectorIndexStereo];
                var finalPassCamera = myOmnity.finalPassCameras[startLengthFPC + iProjectorIndexStereo];
                string eyeAppendicy = !stereo ? "" : ((eye == 0) ? "(left eye)" : "(right eye)");
                screenShape.trans.gameObject.name = "SDM Screen " + iProjectorIndex.ToString() + " " + eyeAppendicy;
                screenShape.name = "SDM Screen " + iProjectorIndex.ToString() + " " + eyeAppendicy;

                Debug.Log("Looking at " + screenShape.trans.gameObject.name);

                OmnitySDMMeshMono sdmMesh = screenShape.trans.gameObject.AddComponent<OmnitySDMMeshMono>();
                sdmMesh.Read(files[iProjectorIndex].file, FinalPassShaderType.EdgeBlendVertexColorPerspective, null, OmnityRendererExtensions.DisableShadowsAndLightProbes, OmnityPlatformDefines.OptimizeMesh);

                yield return null;

                if (stereo) {
                    screenShape.eyeTarget = eye == 0 ? OmnityTargetEye.Left : OmnityTargetEye.Right;
                } else {
                    screenShape.eyeTarget = OmnityTargetEye.Both;
                }

                if (eye == 1) {
                    finalPassCamera.myCamera.clearFlags = finalPassCamera.clearFlags = CameraClearFlags.Nothing;
                }

                //                    ss.transformInfo.position = new Vector3(offset, 0, 0);

                screenShape.layer = finalPassCameraLayer;
                screenShape.renderer.gameObject.layer = finalPassCameraLayer;

                screenShape.trans.localPosition = screenShape.transformInfo.position;
                screenShape.UpdateScreen();

                screenShape.screenShapeType = OmnityScreenShapeType.CustomApplicationLoaded;

                Material.Destroy(screenShape.renderer.sharedMaterial);
                screenShape.renderer.gameObject.GetComponent<MeshFilter>().sharedMesh = sdmMesh.m;
                screenShape.finalPassShaderType = FinalPassShaderType.EdgeBlendVertexColorPerspective;
                screenShape.renderer.sharedMaterial = FinalPassSetup.ApplySettings(FinalPassShaderType.EdgeBlendVertexColorPerspective);

                var finalPassCameraSDMProxy = finalPassCamera.myCameraTransform.gameObject.AddComponent<FPC_SDMPerspectiveCameraProxyStereo>();
                finalPassCameraSDMProxy.mesh = screenShape.renderer.gameObject.GetComponent<MeshFilter>().sharedMesh;
                finalPassCameraSDMProxy.m = myOmnity.screenShapes[startLengthSS + iProjectorIndexStereo].renderer.sharedMaterial;
                finalPassCameraSDMProxy.c = finalPassCamera;
                Debug.Log("todo set texture");

                if (fullscreenPerProjector) {
                    finalPassCamera.omnityPerspectiveMatrix.orthographicSize = sdmMesh.sdmBlendMesh.NATIVEYRES / 2.0f;
                    finalPassCamera.transformInfo.position = new Vector3(sdmMesh.sdmBlendMesh.NATIVEXRES / 2, sdmMesh.sdmBlendMesh.NATIVEYRES / 2f, .5f);
                    finalPassCamera.myCamera.clearFlags = finalPassCamera.clearFlags = CameraClearFlags.Color;
                    finalPassCamera.myCamera.cullingMask = finalPassCamera.cullingMask = 0;
                } else {
                    if (stereo) {
                        finalPassCamera.omnityPerspectiveMatrix.orthographicSize = sdmMesh.sdmBlendMesh.NATIVEYRES;

                        if (eye == 1) {
                            finalPassCamera.transformInfo.position = new Vector3(sdmMesh.sdmBlendMesh.NATIVEXRES / 2f, sdmMesh.sdmBlendMesh.NATIVEYRES, 0);
                        } else {
                            finalPassCamera.transformInfo.position = new Vector3(sdmMesh.sdmBlendMesh.NATIVEXRES / 2f, 0, 0);
                        }
                    } else {
                        finalPassCamera.omnityPerspectiveMatrix.orthographicSize = sdmMesh.sdmBlendMesh.NATIVEYRES / 2.0f;
                        finalPassCamera.transformInfo.position = new Vector3(sdmMesh.sdmBlendMesh.NATIVEXRES / 2, sdmMesh.sdmBlendMesh.NATIVEYRES / 2f, 0);
                    }
                }

                finalPassCamera.omnityPerspectiveMatrix.matrixMode = MatrixMode.Orthographic;
                finalPassCamera.projectorType = OmnityProjectorType.Rectilinear;
                finalPassCamera.omnityPerspectiveMatrix.near = -1;
                finalPassCamera.omnityPerspectiveMatrix.far = 1;
                finalPassCamera.cullingMask = 0;

                finalPassCamera.myCamera.gameObject.layer = finalPassCameraLayer;
                finalPassCamera.myCamera.gameObject.name = "SDM Projector " + iProjectorIndex.ToString() + " " + eyeAppendicy;
                finalPassCamera.name = "SDM Projector " + iProjectorIndex.ToString() + " " + eyeAppendicy;

                finalPassCamera.myCamera.ResetProjectionMatrix();
                finalPassCamera.UpdateCamera();

                if (fullscreenPerProjector) {
                    finalPassCamera.myCamera.rect = files[iProjectorIndex].rect = new Rect(0, 0, 1, 1);
                } else {
                    finalPassCamera.myCamera.rect = files[iProjectorIndex].rect;
                }

                if (fullscreenPerProjector) {
                    totalXRes = sdmMesh.sdmBlendMesh.NATIVEXRES;
                    totalYRes = sdmMesh.sdmBlendMesh.NATIVEYRES;
                } else {
                    // warning this is not safe for mixed resoutions
                    if (eye == 0) {
                        totalXRes = Mathf.Max(totalXRes, sdmMesh.sdmBlendMesh.NATIVEXRES * (files[iProjectorIndex].screenIndexX + 1));
                    }
                    if (stereo) {
                        totalYRes = Mathf.Max(totalYRes, sdmMesh.sdmBlendMesh.NATIVEYRES * 2 * (files[iProjectorIndex].screenIndex + 1));
                    } else {
                        totalYRes = Mathf.Max(totalYRes, sdmMesh.sdmBlendMesh.NATIVEYRES * (files[iProjectorIndex].screenIndex + 1));
                    }
                }

                int screenIndex = files[iProjectorIndex].screenIndex;
                ScreenMapper screenMapInfo = new ScreenMapper();
                if (screenMapperData.Count > screenIndex) {
                    screenMapInfo = screenMapperData[screenIndex];
                }

                screenShape.renderer.sharedMaterial.SetFloat("leftDegrees", screenMapInfo.xMin);
                screenShape.renderer.sharedMaterial.SetFloat("rightDegrees", screenMapInfo.xMax);
                screenShape.renderer.sharedMaterial.SetFloat("topDegrees", screenMapInfo.yMax);
                screenShape.renderer.sharedMaterial.SetFloat("bottomDegrees", screenMapInfo.yMin);
            }
        }
        yield return null;
        Debug.Log("CALL RESIZE " + totalXRes + " X " + totalYRes);

        if (fullscreenPerProjector) {
        } else {
            Omnity.anOmnity.windowInfo.fullscreenInfo.goalWindowPosAndRes = new Rect(0, 0, totalXRes, totalYRes);
            Omnity.anOmnity.windowInfo.fullscreenInfo.DoUpdate();
        }
        myOmnity.forceRefresh = true;
    }
}

public class FPC_SDMPerspectiveCameraProxyStereo : MonoBehaviour {
    public Material m;
    public Mesh mesh;
    public FinalPassCamera c;

    public void OnPostRender() {
        //   OmnitySDMManagerStereoOrthoPersective.Get().ReconnectTextures(Omnity.anOmnity);
        m.SetPass(0);
        Graphics.DrawMeshNow(mesh, transform.localToWorldMatrix * Matrix4x4.TRS(-new Vector3(c.transformInfo.position.x, c.transformInfo.position.y, c.transformInfo.position.z), Quaternion.identity, Vector3.one));
    }
}
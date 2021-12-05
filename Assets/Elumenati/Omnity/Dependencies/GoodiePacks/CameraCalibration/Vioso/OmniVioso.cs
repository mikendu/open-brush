#if UNITY_STANDALONE_WIN

using OmnityMono;
using OmnityMono.Navegar;
using System.Collections;
using System.Xml;
using System.Xml.XPath;
using UnityEngine;

public class OmniVioso : OmnityTabbedFeature {
    static public OmnityPluginsIDs _myOmnityPluginsID = OmnityPluginsIDs.OmniVioso;
    override public OmnityPluginsIDs myOmnityPluginsID { get { return _myOmnityPluginsID; } }

    static public OmniVioso OmniCreateSingleton(GameObject go) {
        return GetSingleton<OmniVioso>(ref singleton, go);
    }

    public static OmniVioso singleton;

    public enum TextureSource {
        TextureFromApplication = 0,
        PerspectiveCamera = 1,
        FinalPassCamera = 2,
    }



    public class ViosoConfig {

        public class ViosoWarpInfo {
            public FinalPassCamera finalPassCamera;
            public ScreenShape screenShape;

            public Mesh mesh;
            public Texture2D blendTexture;

            public int monitorHeight;
            public int monitorWidth;
            public int monitorOffsetX;
            public int monitorOffsetY;


            public void Setup_Vioso(ViosoEngine.API.VIOSO_DATA viosoData, Mesh m, Texture2D bt) {
                mesh = m;
                blendTexture = bt;

                monitorHeight = viosoData.monitorHeight;
                monitorWidth = viosoData.monitorWidth;
                monitorOffsetX = viosoData.monitorOffsetX;
                monitorOffsetY = viosoData.monitorOffsetY;
            }


            public void Setup_FPC(Omnity myOmnity, int configID, int meshNumber, bool stereo, FinalPassCamera fpc) {
                finalPassCamera = fpc;
                finalPassCamera.normalizedViewportRect = new Rect(0, 0, 1, 1);
                finalPassCamera.myCameraTransform.position = new Vector3(0.0f, 0.0f, 0.0f);
                finalPassCamera.myCameraTransform.localEulerAngles = new Vector3(0.0f, 0.0f, 0.0f);
                finalPassCamera.transformInfo.position = new Vector3(0.0f, 0.0f, 0.0f);
                finalPassCamera.transformInfo.localEulerAngles = new Vector3(0.0f, 0.0f, 0.0f);
                finalPassCamera.projectorType = OmnityProjectorType.FisheyeFullDome;
                finalPassCamera.name = finalPassCamera.myCameraTransform.gameObject.name = "VIOSOCAM_IG" + NameCalibrationMeshSuffix(configID, meshNumber, stereo);
                finalPassCamera.myCamera.cullingMask = 1 << screenShape.layer;
                /*if (stereo) {
                    if ((meshNumber % 2) == 0) finalPassCamera.targetEyeHint = OmnityTargetEye.Left;
                    else finalPassCamera.targetEyeHint = OmnityTargetEye.Right;
                } else {
                    finalPassCamera.targetEyeHint = OmnityTargetEye.Both;
                }*/

                if (System.IO.File.Exists(FinalPassCameraFilename(myOmnity, configID, meshNumber, stereo))) {
                    finalPassCamera.serialize = true;
                    OmnityHelperFunctions.LoadXML(FinalPassCameraFilename(myOmnity, configID, meshNumber, stereo), finalPassCamera.ReadXML);
                    finalPassCamera.serialize = false;
                }
            }


            public void Setup_SS(int configID, int meshNumber, bool stereo, ScreenShape ss) {
                screenShape = ss;
                screenShape.name = "VIOSOCAM_SCREEN" + NameCalibrationMeshSuffix(configID, meshNumber, stereo);
                if (stereo) {
                    if ((meshNumber % 2) == 0) {
                        screenShape.eyeTarget = OmnityTargetEye.Left;
                        screenShape.layer = 27;
                    } else {
                        screenShape.eyeTarget = OmnityTargetEye.Right;
                        screenShape.layer = 28;
                    }
                } else {
                    screenShape.eyeTarget = OmnityTargetEye.Both;
                    screenShape.layer = 28;
                }
            }


            public void Setup_Shaders(bool useEz4, TextureSource textureSource) {
                var mr = screenShape.trans.gameObject.GetComponent<MeshRenderer>();
                var mf = screenShape.trans.gameObject.GetComponent<MeshFilter>();

                switch (textureSource) {
                    case TextureSource.FinalPassCamera:
                        screenShape.automaticallyConnectTextures = false;
                        mr.sharedMaterial = FinalPassSetup.ApplySettings(FinalPassShaderType.ViosoShaderFlat);
                        break;

                    case TextureSource.PerspectiveCamera:
                        screenShape.automaticallyConnectTextures = true;
                        mr.sharedMaterial = FinalPassSetup.ApplySettings(FinalPassShaderType.ViosoShader);
                        break;

                    case TextureSource.TextureFromApplication:
                        screenShape.automaticallyConnectTextures = false;
                        mr.sharedMaterial = FinalPassSetup.ApplySettings(FinalPassShaderType.ViosoShaderFlat);
                        break;

                    default:
                        screenShape.automaticallyConnectTextures = false;
                        Debug.LogError("UNKNOWN MODE " + textureSource.ToString());
                        mr.sharedMaterial = FinalPassSetup.ApplySettings(FinalPassShaderType.ViosoShaderFlat);
                        break;
                }

                mr.enabled = true;

                mf.mesh = mesh;
                mr.sharedMaterial.SetTexture("_BlendTexture", blendTexture);
                if (useEz4) {
                    mr.sharedMaterial.SetTextureOffset("_BlendTexture", new Vector2(.5f, .5f));
                    mr.sharedMaterial.SetTextureScale("_BlendTexture", new Vector2(.5f, -.5f));
                } else {
                    mr.sharedMaterial.SetTextureOffset("_BlendTexture", new Vector2(.5f, -.5f));
                    mr.sharedMaterial.SetTextureScale("_BlendTexture", new Vector2(.5f, .5f));
                }
            }


            public void DrawCamera() {
                if (screenShape != null && screenShape.renderer != null && finalPassCamera != null) {
                    Graphics.DrawMesh(mesh, screenShape.trans.localToWorldMatrix, screenShape.renderer.material, screenShape.layer, finalPassCamera.myCamera);
                }
            }

        };


        public System.Collections.Generic.List<ViosoWarpInfo> viosoWarpInfo = new System.Collections.Generic.List<ViosoWarpInfo>();

        public int meshTotal = 0;

        public TextureSource textureSource = TextureSource.PerspectiveCamera;

        public Vector3 eulerAnglesDegree = Vector3.zero;

        public float blackLevelUplift = 0.0f;
        public float blackLevelUplift_OverlapSize = 1.0f;
        public float blackLevelUplift_OverlapIntensity = 0.5f;
        public bool isBlendUsed = true;
        public bool useEz4 = true;
        public bool wasCalibrationSpherical = false;
        public float angleAtDomePerimeterDegrees = 90;
        public bool forceStereo = false;

        public ScreenMapper screenMapperData = new ScreenMapper();
        public float gamma1 = 1.0f;

        private bool guiTabOpen = false;
        private int originalSSLength = 0;
        private int originalFPCLength = 0;
        private string vwfFIleWithExtensionSource = "";
        private string vwfFIleWithExtensionDest32 = "";
        private string vwfFIleWithExtensionDest64 = "";
        private string iniFIleWithoutExtension = "";




        static public string NameSuffix(int calibrationID) {
            if (calibrationID == 0) {
                return "";
            } else {
                return calibrationID.ToString();
            }
        }


        static public string NameCalibrationMeshSuffix(int calibrationID, int meshIndex, bool stereo) {
            string stereoTag = "";

            if (stereo) {
                if ((meshIndex % 2) == 0) stereoTag = "_L";
                else stereoTag = "_R";
                meshIndex /= 2;
            }

            return stereoTag + "_" + calibrationID + "_" + meshIndex;
        }



        static public string FinalPassCameraFilename(Omnity myOmnity, int calibrationID, int meshIndex, bool stereo) {
            if (calibrationID == 0 && !stereo) {
                return OmnityLoader.AddSpecialConfigPath(myOmnity, "FinalPassCamera" + meshIndex + ".xml");
            } else {
                return OmnityLoader.AddSpecialConfigPath(myOmnity, "FinalPassCamera" + NameCalibrationMeshSuffix(calibrationID, meshIndex, stereo) + ".xml");
            }

        }




        public void Setup_Phase1(Omnity myOmnity, int configID) {
            viosoWarpInfo.Clear();

            vwfFIleWithExtensionSource = @"C:\Users\Public\Documents\VIOSO\Anyblend\Export\calibration" + NameSuffix(configID) + ".vwf";
            vwfFIleWithExtensionDest32 = System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, @"Vioso\32bits\calibration" + NameSuffix(configID) + ".vwf").Replace("/", "\\");
            vwfFIleWithExtensionDest64 = System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, @"Vioso\64bits\calibration" + NameSuffix(configID) + ".vwf").Replace("/", "\\");
            iniFIleWithoutExtension = System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, @"Vioso\64bits\calibration" + NameSuffix(configID)).Replace("/", "\\");

            try {
                System.IO.File.Copy(vwfFIleWithExtensionSource, vwfFIleWithExtensionDest64, true);
                System.IO.File.Copy(vwfFIleWithExtensionSource, vwfFIleWithExtensionDest32, true);
            } catch (System.Exception e) {
                Debug.LogError("Error could not copy vwf file " + e.Message);
            }

            ViosoEngine.ConstructorPart2(iniFIleWithoutExtension);
            meshTotal = ViosoEngine.API.GetMeshTotal();

            if (forceStereo) {
                meshTotal *= 2;

                // set camera array to stereo mode
                foreach (var c in myOmnity.cameraArray) {
                    if (c != null && c.renderTextureSettings != null) {
                        c.renderTextureSettings.stereoPair = true;
                        c.renderTextureSettings.GenerateRenderTexture(true);
                    }
                }
            }

            originalSSLength = myOmnity.screenShapes.Length;
            originalFPCLength = myOmnity.finalPassCameras.Length;
            myOmnity.ScreenShapeArrayResize(originalSSLength + meshTotal, false, true, OmnityScreenShapeType.CustomApplicationLoaded, FinalPassShaderType.Custom_ApplicationLoaded, true, true, false);
            myOmnity.FinalPassCameraArrayResize(originalFPCLength + meshTotal, false, false, true, true, false);

            for (int m = 0; m < meshTotal; m++) {
                int meshNumber = forceStereo ? m / 2 : m;
                var currentSS = myOmnity.screenShapes[originalSSLength + m];
                var currentFPC = myOmnity.finalPassCameras[originalFPCLength + m];
                ViosoEngine.API.VIOSO_DATA meshFile = ViosoEngine.API.GetMesh(meshNumber);
                var newWarp = new ViosoWarpInfo();

                newWarp.Setup_Vioso(meshFile, ViosoEngine.API.GetUnityMesh(meshFile, OmnityPlatformDefines.OptimizeMesh), ViosoEngine.API.GetBlendTexture(meshNumber, meshFile));
                newWarp.Setup_SS(configID, m, forceStereo, currentSS);
                newWarp.Setup_FPC(myOmnity, configID, m, forceStereo, currentFPC);

                viosoWarpInfo.Add(newWarp);
            }

            ViosoEngine.API.Destructor();

        }



        public void Setup_Phase2(Omnity myOmnity, int configID) {
            for (int m = 0; m < meshTotal; m++) {
                viosoWarpInfo[m].Setup_Shaders(useEz4, textureSource);
            }

            SetExtentsNowExpensive(myOmnity);

            switch (textureSource) {
                case TextureSource.TextureFromApplication:
                    ReconnectTextures(myOmnity);
                    break;

                case TextureSource.PerspectiveCamera:
                    myOmnity.DoConnectTextures();
                    break;

                case TextureSource.FinalPassCamera:
                    ReconnectTextures(myOmnity);
                    Debug.LogWarning("mode not tested");
                    break;

                default:
                    Debug.LogWarning("mode not found");
                    break;
            }
            myOmnity.forceRefresh = true;

            if (useEz4) {
                OmnityWindowsExtensions.FindWindowByAltCache();
                ActivateDisplays();

                var ez1 = EasyMultiDisplay.Get();

                if (ez1) {
                    ez1.TrySetPositionAndResolutionCallback(myOmnity);
                }
            }
        }



        public void GuiCallback(Omnity myOmnity, int configID) {
            OmnityHelperFunctions.BeginExpander(ref guiTabOpen, "Vioso Configuration[" + configID + "]");
            if (guiTabOpen) {
                if (OmnityHelperFunctions.EnumInputResetWasChanged<TextureSource>("SDM.textureSource", "textureSource", ref textureSource, TextureSource.PerspectiveCamera, 1)) {
                    Debug.LogError("TODO DEAL WITH THIS CHANGE SDM.textureSource");
                }

                if (OmnityHelperFunctions.FloatInputResetSliderWasChanged("Gamma1", ref gamma1, 0.0f, 1.0f, 3.0f)) {
                    for (int i = 0; i < myOmnity.screenShapes.Length; i++) {
                        myOmnity.screenShapes[i].renderer.sharedMaterial.SetFloat("_Gamma1", gamma1);
                    }
                }
                useEz4 = OmnityHelperFunctions.BoolInputReset("Use Ez4", useEz4, true);
                isBlendUsed = OmnityHelperFunctions.BoolInputReset("Use Blend", isBlendUsed, true);
                forceStereo = OmnityHelperFunctions.BoolInputReset("Enable Stereo", forceStereo, false);
                blackLevelUplift = OmnityHelperFunctions.FloatInputResetSlider("Black Level Uplift", blackLevelUplift, 0.0f, 0.0f, 1.0f);
                blackLevelUplift_OverlapSize = OmnityHelperFunctions.FloatInputResetSlider("Black Level Uplift Overlap Size", blackLevelUplift_OverlapSize, 0.0f, 0.0f, 1.0f);
                blackLevelUplift_OverlapIntensity = OmnityHelperFunctions.FloatInputResetSlider("Black Level Uplift Overlap Intensity", blackLevelUplift_OverlapIntensity, 0.0f, 0.5f, 1.0f);
                wasCalibrationSpherical = OmnityHelperFunctions.BoolInputReset("Was Image Calibration Spherical", wasCalibrationSpherical, false);
                eulerAnglesDegree = OmnityHelperFunctions.Vector3InputReset("Euler Angles Degree", eulerAnglesDegree, Vector3.zero);
                GUILayout.Label("(set false for rectangular or equirectangular calibration preview image, set true for fulldome calibration preview image)", GUILayout.MaxWidth(400));

                if (wasCalibrationSpherical) {
                    angleAtDomePerimeterDegrees = OmnityHelperFunctions.FloatInputResetSlider("AngleAtDomePerimeterDegrees", angleAtDomePerimeterDegrees, 0, 90, 180);
                } else {
                    screenMapperData.xMin = OmnityHelperFunctions.FloatInputReset("xMin", screenMapperData.xMin, -90);
                    screenMapperData.xMax = OmnityHelperFunctions.FloatInputReset("xMax", screenMapperData.xMax, 90);
                    screenMapperData.yMin = OmnityHelperFunctions.FloatInputReset("yMin", screenMapperData.yMin, -90);
                    screenMapperData.yMax = OmnityHelperFunctions.FloatInputReset("yMax", screenMapperData.yMax, 90);
                }

                GUILayout.Label(@"Make sure to export your latest VIOSO config to C:\Users\Public\Documents\VIOSO\Anyblend\Export\calibration" + configID + ".vwf", GUILayout.MaxWidth(400));
                GUILayout.Label(@"textureSource should be PerspectiveCamera for most applications.  This mode will use the Perspective cameras to generate a cube map like data structure and uses projective perspective mapping to map the captured perspective onto the screen geometry.  Developers can opt for TextureFromApplication if they want to map a single equirectangular or fisheye that passed to Omnity via passing a function that returns a texture to this plugin.  Here is one way to do it.  OmniVioso.Get().getTextureFromApp =()=>{ return myTexture; };", GUILayout.MaxWidth(400));
            }

            SetExtentsNowExpensive(myOmnity);

            OmnityHelperFunctions.EndExpander();
        }


        public void WriteXMLDelegateCallback(System.Xml.XmlTextWriter xmlWriter, int configID) {
            xmlWriter.WriteStartElement("ViosoConfig");

            xmlWriter.WriteElementString("textureSource", textureSource.ToString());
            xmlWriter.WriteElementString("screenMapper", screenMapperData.rect.ToString());
            xmlWriter.WriteElementString("useEz4", useEz4.ToString());
            xmlWriter.WriteElementString("forceStereo", forceStereo.ToString());
            xmlWriter.WriteElementString("isBlendUsed", isBlendUsed.ToString());
            xmlWriter.WriteElementString("eulerAnglesDegree", eulerAnglesDegree.ToString("F4"));
            xmlWriter.WriteElementString("blackLevelUplift", blackLevelUplift.ToString());
            xmlWriter.WriteElementString("blackLevelUplift_OverlapSize", blackLevelUplift_OverlapSize.ToString());
            xmlWriter.WriteElementString("blackLevelUplift_OverlapIntensity", blackLevelUplift_OverlapIntensity.ToString());
            xmlWriter.WriteElementString("wasCalibrationSpherical", wasCalibrationSpherical.ToString());
            xmlWriter.WriteElementString("angleAtDomePerimeterDegrees", angleAtDomePerimeterDegrees.ToString());

            xmlWriter.WriteEndElement();

            ////////////////////////////////////////////////////////////
            // NOTE.... THIS IS SAVED TO A DIFFERENT FILENAME....
            /// SAVE EACH SCREENSHAPE INDIVIDULALLY...
            for (int m = 0; m < viosoWarpInfo.Count; m++) {
                viosoWarpInfo[m].finalPassCamera.serialize = true;
                OmnityHelperFunctions.SaveXML(FinalPassCameraFilename(Omnity.anOmnity, configID, m, forceStereo), viosoWarpInfo[m].finalPassCamera.WriteXML);
                viosoWarpInfo[m].finalPassCamera.serialize = false;
            }
        }



        public void ReadXMLDelegateCallback(XPathNavigator nav) {
            textureSource = OmnityHelperFunctions.ReadElementEnumDefault<TextureSource>(nav, "(.//textureSource)", TextureSource.TextureFromApplication);
            Rect r = OmnityHelperFunctions.ReadElementRectDefault(nav, ".//screenMapper", new Rect(-90, -45, 180, 90));
            screenMapperData = new ScreenMapper { rect = r };
            useEz4 = OmnityHelperFunctions.ReadElementBoolDefault(nav, "(.//useEz4)", true);
            forceStereo = OmnityHelperFunctions.ReadElementBoolDefault(nav, "(.//forceStereo)", false);
            isBlendUsed = OmnityHelperFunctions.ReadElementBoolDefault(nav, "(.//isBlendUsed)", true);
            eulerAnglesDegree = OmnityHelperFunctions.ReadElementVector3Default(nav, ".//eulerAnglesDegree", eulerAnglesDegree);
            blackLevelUplift = OmnityHelperFunctions.ReadElementFloatDefault(nav, "(.//blackLevelUplift)", 0.0f);
            blackLevelUplift_OverlapSize = OmnityHelperFunctions.ReadElementFloatDefault(nav, "(.//blackLevelUplift_OverlapSize)", 1.0f);
            blackLevelUplift_OverlapIntensity = OmnityHelperFunctions.ReadElementFloatDefault(nav, "(.//blackLevelUplift_OverlapIntensity)", 1.0f);
            wasCalibrationSpherical = OmnityHelperFunctions.ReadElementBoolDefault(nav, "(.//wasCalibrationSpherical)", false);
            angleAtDomePerimeterDegrees = OmnityHelperFunctions.ReadElementFloatDefault(nav, "(.//angleAtDomePerimeterDegrees)", 90f);
        }





        private void SetExtentsNowExpensive(Omnity myOmnity) {
            for (int m = 0; m < meshTotal; m++) {
                int ss = m + originalSSLength;
                if (ss < myOmnity.screenShapes.Length && myOmnity.screenShapes[ss].trans != null && myOmnity.screenShapes[ss].trans.gameObject.GetComponent<MeshRenderer>() != null && myOmnity.screenShapes[ss].trans.gameObject.GetComponent<MeshRenderer>().sharedMaterial) {
                    var mat = myOmnity.screenShapes[ss].trans.gameObject.GetComponent<MeshRenderer>().sharedMaterial;
                    mat.SetFloat("leftDegrees", screenMapperData.xMin);
                    mat.SetFloat("rightDegrees", screenMapperData.xMax);
                    mat.SetFloat("topDegrees", screenMapperData.yMax);
                    mat.SetFloat("bottomDegrees", screenMapperData.yMin);
                    mat.SetFloat("_eulerDegreesX", eulerAnglesDegree.x);
                    mat.SetFloat("_eulerDegreesY", eulerAnglesDegree.y);
                    mat.SetFloat("_eulerDegreesZ", eulerAnglesDegree.z);
                    mat.SetFloat("_wasCalibrationSpherical", wasCalibrationSpherical ? 1f : 0f);
                    mat.SetFloat("_isBlendUsed", isBlendUsed ? 1f : 0f);
                    mat.SetFloat("_angleAtDomePerimeterDegrees", angleAtDomePerimeterDegrees);
                    mat.SetFloat("_BlackUplift", blackLevelUplift);
                    mat.SetFloat("_BlackUplift_OverlapSize", blackLevelUplift_OverlapSize);
                    mat.SetFloat("_BlackUplift_OverlapIntensity", blackLevelUplift_OverlapIntensity);
                }
            }
        }



        private void ActivateDisplays() {
            viosoWarpInfo.Sort((a, b) => {
                if (a.monitorOffsetY == 0 && a.monitorOffsetX == 0) {
                    return 0.CompareTo(1);
                } else if (b.monitorOffsetY == 0 && b.monitorOffsetX == 0) {
                    return 1.CompareTo(0);
                } else if (a.monitorOffsetY != b.monitorOffsetY) {
                    return a.monitorOffsetY.CompareTo(b.monitorOffsetY);
                } else if (a.monitorOffsetX != b.monitorOffsetX) {
                    return a.monitorOffsetX.CompareTo(b.monitorOffsetX);
                } else {
                    return 0;
                }
            });

            bool hasMainDisplay = false;

            for (int i = 0; i < viosoWarpInfo.Count; i++) {
                if (0 == viosoWarpInfo[i].monitorOffsetX && 0 == viosoWarpInfo[i].monitorOffsetY) {
                    hasMainDisplay = true;
                    break;
                }
            }

            for (int i = 0; i < viosoWarpInfo.Count; i++) {
                int targetDisplay = hasMainDisplay ? i : i + 1;
                viosoWarpInfo[i].finalPassCamera.myCamera.targetDisplay = targetDisplay;
                viosoWarpInfo[i].finalPassCamera.targetDisplay = targetDisplay;

                viosoWarpInfo[i].finalPassCamera.cullingMask = 1 << viosoWarpInfo[i].screenShape.layer;
                viosoWarpInfo[i].finalPassCamera.myCamera.cullingMask = 1 << viosoWarpInfo[i].screenShape.layer;

                if (targetDisplay > UnityEngine.Display.displays.Length - 1) {
                    if (!Application.isEditor) {
                        Debug.LogWarning("Warning target display " + targetDisplay + " overflows displays list " + UnityEngine.Display.displays.Length + " usually this is because a monitor was removed since the calibration happened.");
                    }
                    targetDisplay = UnityEngine.Display.displays.Length - 1;
                }
                if (targetDisplay < UnityEngine.Display.displays.Length) {
                    UnityEngine.Display.displays[targetDisplay].Activate();
                }
            }
        }



        private bool getLatestFlipY() {
            switch (textureSource) {
                case TextureSource.TextureFromApplication:
                    return getTextureFlipFromApp();

                case TextureSource.FinalPassCamera:
                    return false;

                case TextureSource.PerspectiveCamera:
                    return false;

                default:
                    Debug.LogError("Unknown " + textureSource);
                    return false;
            }
        }



        public void ReconnectTextures(Omnity myOmnity) {
            if (myOmnity.screenShapes == null) {
                return;
            }

            foreach (var ss in myOmnity.screenShapes) {
                if (!ss.automaticallyConnectTextures) {
                    if (ss.renderer != null && ss.renderer.sharedMaterial != null) {
                        ss.renderer.sharedMaterial.SetTexture("_Cam0Tex", getTextureFromApp());
                        if (getLatestFlipY()) {
                            ss.renderer.sharedMaterial.SetTextureOffset("_Cam0Tex", new Vector2(0, 1));
                            ss.renderer.sharedMaterial.SetTextureScale("_Cam0Tex", new Vector2(1, -1));
                        } else {
                            ss.renderer.sharedMaterial.SetTextureOffset("_Cam0Tex", new Vector2(0, 0));
                            ss.renderer.sharedMaterial.SetTextureScale("_Cam0Tex", new Vector2(1, 1));
                        }
                    }
                }
            }
        }
    }




    public ViosoConfig[] viosoProps = new ViosoConfig[1] { new ViosoConfig() };
    private bool guiTabOpen = false;
    private bool loadedOnce = false;




    public void ViosoPropsArrayResize(int newsize) {
        newsize = Mathf.Max(0, newsize);
        if (newsize == viosoProps.Length) {
            return;
        } else {
            int oldSize = viosoProps.Length;
            System.Array.Resize<ViosoConfig>(ref viosoProps, newsize);
            for (int i = oldSize; i < viosoProps.Length; i++) {
                viosoProps[i] = new ViosoConfig();
            }
        }
    }



    public override void MyGuiCallback(Omnity myOmnity) {
        OmnityHelperFunctions.BeginExpander(ref guiTabOpen, "Vioso Configuration[" + viosoProps.Length + "]");
        if (guiTabOpen) {
            ViosoPropsArrayResize(OmnityHelperFunctions.NumericUpDownReset("Size", viosoProps.Length, 1));
            for (int i = 0; i < viosoProps.Length; i++) {
                GUILayout.BeginHorizontal();
                Omnity.GenericUpDownArrayControl(viosoProps, i);
                viosoProps[i].GuiCallback(myOmnity, i);
                GUILayout.EndHorizontal();
            }
        }
        OmnityHelperFunctions.EndExpander();


        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Save")) {
            Save(myOmnity);
        }
        if (GUILayout.Button("Reload")) {
            myOmnity.StartCoroutine(Load(myOmnity));
        }
        GUILayout.EndHorizontal();

        GUILayout.Label(@"PLEASE NOTE: FPC for vioso cameras are saved in the config's sub-sub directory under FinalPassCamera0.xml, FinalPassCamera1.xml", GUILayout.MaxWidth(400));
        GUILayout.Label(@"PLEASE NOTE: Delete and restart the application to reset tweaks", GUILayout.MaxWidth(400));
    }



    public override void WriteXMLDelegate(XmlTextWriter xmlWriter) {
        for (int i = 0; i < viosoProps.Length; i++) {
            viosoProps[i].WriteXMLDelegateCallback(xmlWriter, i);
        }
    }

    public override void ReadXMLDelegate(XPathNavigator nav) {
        System.Xml.XPath.XPathNodeIterator myViosoPropsArrayIterator = nav.Select("(//ViosoConfig)");
        if (myViosoPropsArrayIterator.Count > 0) {
            viosoProps = new ViosoConfig[myViosoPropsArrayIterator.Count];
            int Index = 0;
            while (myViosoPropsArrayIterator.MoveNext()) {
                viosoProps[Index] = new ViosoConfig();
                viosoProps[Index++].ReadXMLDelegateCallback(myViosoPropsArrayIterator.Current);
            }
        } else {
            viosoProps = new ViosoConfig[1] { new ViosoConfig() };
            viosoProps[0].ReadXMLDelegateCallback(nav);
        }
    }











    static public System.Func<Texture> getTextureFromApp = () => {
        Debug.LogError("Replace with callback from app");
        return null;
    };

    static public System.Func<bool> getTextureFlipFromApp = () => {
        Debug.LogError("Replace with callback from app");
        return false;
    };

    static public OmniVioso Get() {
        return singleton;
    }

    private void Awake() {
        if (VerifySingleton<OmniVioso>(ref singleton)) {
            Omnity.onReloadEndCallbackPriority.AddLoaderFunction(PriorityEventHandler.Ordering.Order_OmniVioso, CoroutineLoader);
            onNewTextureCallback += () => {
                if (!Omnity.anOmnity.PluginEnabled(OmnityPluginsIDs.OmniVioso)) {
                    this.enabled = false;
                    return;
                } else {
                    this.enabled = true;
                    for (int i = 0; i < viosoProps.Length; i++) {
                        viosoProps[i].ReconnectTextures(Omnity.anOmnity);
                    }
                }
            };

            Omnity.onSaveCompleteCallback += Save;
        }
    }

    

    public override IEnumerator PostLoad() {
        StopAllCoroutines();
        yield return StartCoroutine(CoLoad(Omnity.anOmnity));
    }


    public void OnDestroy() {
        ViosoEngine.Destructor();
    }



    private System.Collections.IEnumerator CoLoad(Omnity myOmnity) {
        if (!loadedOnce) {
            if (!ViosoEngine.ConstructorPart1()) {
                Debug.Log("Failed constructing vioso glue");
            }
            loadedOnce = true;
        }

        yield return null;

        for (int i = 0; i < viosoProps.Length; i++) {
            viosoProps[i].Setup_Phase1(myOmnity, i);
        }

        yield return null;
        yield return null;

        while (!FinalPassSetup.isDoneLoading) {
            yield return null;
        }

        for (int i = 0; i < viosoProps.Length; i++) {
            viosoProps[i].Setup_Phase2(myOmnity, i);
        }

        yield break;
    }



    public override string BaseName {
        get {
            return "OmniViosoSettings.xml";
        }
    }


    public void Tabs(Omnity anOmnity) {
        GUILayout.BeginHorizontal();
        GUILayout.EndHorizontal();
    }


    // Use this for initialization
    private void Start() {
    }


    private void LateUpdate() {
        for (int i = 0; i < viosoProps.Length; i++) {
            if (viosoProps[i].viosoWarpInfo != null) {
                foreach (var m in viosoProps[i].viosoWarpInfo) {
                    m.DrawCamera();
                }
            }
        }
    }

    
    public System.Action onNewTextureCallback = delegate {
    };
    /*
    public System.Func<Texture> getLatestTextureCallback = () => {
        Texture t = null;
        if (OmniVioso.Get() == null || Omnity.anOmnity == null) {
            return null;
        }
        switch (OmniVioso.Get().textureSource) {
            case TextureSource.TextureFromApplication:
                return getTextureFromApp();

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

                Debug.LogError("Unknown type " + OmniVioso.Get().textureSource);
                return null;
        }
        if (t == null) {
            Debug.LogError(OmniVioso.Get().textureSource.ToString() + " Error: Make sure to add a camera with a render texture");
        }
        return t;
    };
    */



    [System.Serializable]
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
}


#endif
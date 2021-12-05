using System.Xml.XPath;
using UnityEngine;
/*
public class StereoLeftRight : OmnityTabbedFeature {
    static public OmnityPluginsIDs _myOmnityPluginsID = OmnityPluginsIDs.StereoLeftRight;
    override public OmnityPluginsIDs myOmnityPluginsID { get { return _myOmnityPluginsID; } }

    static public StereoLeftRight OmniCreateSingleton(GameObject go) {
        return GetSingleton<StereoLeftRight>(ref singleton, go);
    }

    static private StereoLeftRight singleton = null;

    static public StereoLeftRight Get() {
        return singleton;
    }

    private void Awake() {
        if (VerifySingleton<StereoLeftRight>(ref singleton)) {
            Omnity.onReloadEndCallbackPriority.AddLoaderFunction(PriorityEventHandler.Ordering.StereoLeftRight, CoroutineLoader);
            Omnity.onSaveCompleteCallback += Save;

            Omnity.onReloadEndCallback += (o) => {
                if (o.PluginEnabled(_myOmnityPluginsID)) {
               //     DoConnectTextures();
              ///      SetMatrix();
                }
            };
        }
    }


    public override void Unload() {
        base.Unload();
    }

    override public string BaseName {
        get {
            return "StereoLeftRight";
        }
    }

    public override void ReadXMLDelegate(XPathNavigator nav) {
//        linkedFinalPassCameraGroupsPM = LinkedFinalPassCameraGroupBase<LinkedFinalPassCameraGroupStereo>.ReadXMLAll(nav, Omnity.anOmnity);
    }

    /// <param name="xmlWriter">The XML writer.</param>
    override public void WriteXMLDelegate(System.Xml.XmlTextWriter xmlWriter) {
   //     foreach (var group in linkedFinalPassCameraGroupsPM) {
   //         group.WriteXML(xmlWriter);
    //    }
    }

    override public System.Collections.IEnumerator PostLoad() {
      //  LinkCombiners(Omnity.anOmnity);
        yield break;
    }


    public override void MyGuiCallback(Omnity anOmnity) {
        if (!anOmnity.PluginEnabled(OmnityPluginsIDs.StereoLeftRight)) {
            return;
        }
        GUILayout.Label("Omnity Stereo Left Right Helper.");
        GUILayout.Label("Please set perspective, screen shape, and final pass cameras to be have target eye hint.");
        GUILayout.Label("set screen shape's layer to be 30 for left and 29 for right eye");
        GUILayout.Label("set projectors/final pass camera culling mask to be 30 for left eye / 29 for right eye.");
    }


    void LateUpdate() {
        if (Omnity.anOmnity.PluginEnabled(OmnityPluginsIDs.StereoLeftRight)) {
            // we probabbly dont need to update shaders every frame, but we MUST set matrix after update shader happens.
            // so for now we disable omnity from doing it, and do BOTH here every frame.  
          //  Omnity.anOmnity.keepUpdatingShaders = false;
         //  // Omnity.anOmnity.DoUpdateShaders();
         //   SetMatrix();
        }
    }

    bool isLeft(PerspectiveCamera p) {
        return p.targetEyeHint == OmnityTargetEye.Left;
    }

    bool isRight(PerspectiveCamera p) {
        return p.targetEyeHint == OmnityTargetEye.Right;
    }
    bool isLeft(ScreenShape p) {
        return p.eyeTarget == OmnityTargetEye.Left;
    }

    bool isRight(ScreenShape p) {
        return p.eyeTarget == OmnityTargetEye.Right;
    }

    bool isOther(PerspectiveCamera name, ScreenShape name2) {
        return name.targetEyeHint == name2.eyeTarget;
    }

    bool AreEyesMatching(ScreenShape myScreenShape, PerspectiveCamera aCamera) {
        if (isLeft(aCamera) && isLeft(myScreenShape)) {
            return true;
        } else if (isRight(aCamera) && isRight(myScreenShape)) {
            return true;
        } else if (isOther(aCamera,myScreenShape)) {
            return true;
        } else {
            return false;
        }
    }

    public void DoConnectTextures() {
        foreach (ScreenShape myScreenShape in Omnity.anOmnity.screenShapes) {
            if (myScreenShape.automaticallyConnectTextures) {
                int i = 0;
                foreach (PerspectiveCamera aCamera in Omnity.anOmnity.cameraArray) {
                    if (aCamera.targetEyeHint == OmnityTargetEye.Both && aCamera.renderTextureSettings.enabled && aCamera.renderTextureSettings.stereoPair &&(myScreenShape.eyeTarget == OmnityTargetEye.Left || myScreenShape.eyeTarget == OmnityTargetEye.Right)) {
                        if (myScreenShape.eyeTarget == OmnityTargetEye.Left) {
                            myScreenShape.renderer.sharedMaterial.SetTexture(Omnity.GetTexStringFast(i) , aCamera.renderTextureSettings.rt);
                        } else {
                            myScreenShape.renderer.sharedMaterial.SetTexture(Omnity.GetTexStringFast(i), aCamera.renderTextureSettings.rtR);
                        }

                    } else {

                        if (!AreEyesMatching(myScreenShape, aCamera)) {
                            continue;
                        }
                        if (myScreenShape.renderer == null || myScreenShape.renderer.sharedMaterial == null || aCamera.myCamera == null) {
                            continue;
                        }
                        myScreenShape.renderer.sharedMaterial.SetTexture(Omnity.GetTexStringFast(i), aCamera.myCamera.targetTexture);
                        i++;
                    }
                }
                // set missing textures to blank
                while (i < 6) {
                    
                    myScreenShape.renderer.sharedMaterial.SetTexture(Omnity.GetTexStringFast(i), null);
                    i++;
                }
            }
        }
    }
    void SetMatrix() {
        foreach (ScreenShape myScreenShape in Omnity.anOmnity.screenShapes) {
            if (myScreenShape.startEnabled) {
                if (myScreenShape == null) {
                    continue;
                } else if (myScreenShape.renderer == null) {
                    continue;
                }

                int i = 0;
                foreach (PerspectiveCamera aCamera in Omnity.anOmnity.cameraArray) {
                    if (!AreEyesMatching(myScreenShape, aCamera)) {
                        continue;
                    }
                    Omnity.anOmnity.DoSetShaderMatrix(myScreenShape, i, Omnity.anOmnity.GenerateProjectivePerspectiveProjectionMatrix(aCamera.myCamera, myScreenShape.trans));
                    i++;
                }
                myScreenShape.SetJunkMatrices(i);
            }
        }
    }
}
*/

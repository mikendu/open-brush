using System.Collections.Generic;
using System.Xml.XPath;
using UnityEngine;

public class OmnityQualityHelper : OmnityTabbedFeature {
    private static OmnityQualityHelper singleton = null;

    public static OmnityPluginsIDs _myOmnityPluginsID = OmnityPluginsIDs.OmnityQualityHelper;
    public override OmnityPluginsIDs myOmnityPluginsID { get { return _myOmnityPluginsID; } }

    public static OmnityQualityHelper OmniCreateSingleton(GameObject go) {
        return GetSingleton<OmnityQualityHelper>(ref singleton, go);
    }

    public static OmnityQualityHelper Get() {
        return singleton;
    }

    private void Awake() {
        if (VerifySingleton<OmnityQualityHelper>(ref singleton)) {
            if (singleton == null) {
                singleton = this;
            } else if (singleton != this) {
                Debug.LogError("ERROR THERE SHOULD ONLY BE ONE of " + this.GetType().ToString() + " per scene");
                enabled = false;
                return;
            }

            Omnity.onReloadEndCallbackPriority.AddLoaderFunction(PriorityEventHandler.Ordering.Order_OmnityQualityHelper, CoroutineLoader);
            Omnity.onLoadInMemoryConfigStart += MyLoad;
            Omnity.onSaveCompleteCallback += Save;
        }
    }

    private void MyLoad(Omnity anOmnity) {
   //     if (anOmnity.PluginEnabled(OmnityPluginsIDs.OmnityQualityHelper)) {
         //   anOmnity.StartCoroutine(Load(anOmnity));
    //    }
    }

    private void OnDestroy() {
        singleton = null;
        Omnity.onReloadEndCallbackPriority.RemoveLoaderFunction(PriorityEventHandler.Ordering.Order_OmnityQualityHelper, CoroutineLoader);
        Omnity.onSaveCompleteCallback -= Save;
        Omnity.onLoadInMemoryConfigStart -= MyLoad;
    }

    public override string BaseName {
        get {
            return "OmnityQualityHelper";
        }
    }

    /// <param name="xmlWriter">The XML writer.</param>
    public override void WriteXMLDelegate(System.Xml.XmlTextWriter xmlWriter) {
 
    }

    public override void ReadXMLDelegate(XPathNavigator nav) {

    }


    /// <summary>
    /// Overload this with the gui layout calls.
    /// </summary>
    ///
    ///
    bool IsInconsistent(Omnity anOmnity)  {
        if(anOmnity.cameraArray.Length >= 2) {
            var rt1 = anOmnity.cameraArray[0].renderTextureSettings;
            for(int i = 1; i < anOmnity.cameraArray.Length; i++) {
                var rt2 = anOmnity.cameraArray[i].renderTextureSettings;
                if((rt1.mipmap != rt2.mipmap) || (rt1.mipMapBias != rt2.mipMapBias) || (rt1.width != rt2.width) || (rt1.height != rt2.height) || (rt1.anisoLevel != rt2.anisoLevel)
                   || (rt1.antiAliasing != rt2.antiAliasing) || (rt1.filterMode != rt2.filterMode) || (rt1.depth != rt2.depth) || (rt1.myRenderTextureFormat != rt2.myRenderTextureFormat)
                   || (rt1.stereoPair != rt2.stereoPair) || (rt1.wrapMode != rt2.wrapMode)) {
                    return true;
                }
            }
        }
        return false;
    }

    void ResetSettings(Omnity anOmnity) {
        if( anOmnity.cameraArray.Length>0)
            for(int i = 0; i < anOmnity.cameraArray.Length; i++) {
                var rt1 = anOmnity.cameraArray[i].renderTextureSettings;
                rt1.mipmap = true;
                rt1.depth = 24;
                rt1.mipMapBias = -1;
                rt1.width = 2048;
                rt1.height = 2048;
                rt1.anisoLevel = 2;
                rt1.stereoPair=false;
                rt1.filterMode = FilterMode.Trilinear;
                rt1.myRenderTextureFormat = RenderTextureFormat.Default;
                rt1.antiAliasing = RenderTextureSettings.AntiAliasingSampleCount.AASampleCount_1;
                rt1.wrapMode = TextureWrapMode.Clamp;
            }
    }void SyncSettings(Omnity anOmnity) {

        if(anOmnity.cameraArray.Length <= 0) {
            return;
        }
        var rt0 = anOmnity.cameraArray[0].renderTextureSettings;
        for(int i = 1; i < anOmnity.cameraArray.Length; i++) {
            var rt1 = anOmnity.cameraArray[i].renderTextureSettings;
            rt1.mipmap = rt0.mipmap;
            rt1.depth = rt0.depth;
            rt1.mipMapBias = rt0.mipMapBias;
            rt1.width = rt0.width;
            rt1.height = rt0.height;
            rt1.anisoLevel = rt0.anisoLevel;
            rt1.stereoPair = rt0.stereoPair;
            rt1.filterMode = rt0.filterMode;
            rt1.myRenderTextureFormat = rt0.myRenderTextureFormat;
            rt1.antiAliasing = rt0.antiAliasing;
            rt1.wrapMode = rt0.wrapMode;
        }
    }
    public override void MyGuiCallback(Omnity anOmnity) {
        if (!anOmnity.PluginEnabled(OmnityPluginsIDs.OmnityQualityHelper)) {
            return;
        }
    


        if( anOmnity.cameraArray.Length >0 ) {
            var rtBase = anOmnity.cameraArray[0].renderTextureSettings;
            
            bool needsupdate =false ;

      
            if(GUILayout.Button("Filtering : (currently "+ rtBase.filterMode+")")) {
                switch(rtBase.filterMode) {
                    case FilterMode.Point:
                        rtBase.filterMode = FilterMode.Bilinear;
                        break;
                    case FilterMode.Bilinear:
                        rtBase.filterMode = FilterMode.Trilinear;
                        break;
                        case FilterMode.Trilinear:
                            rtBase.filterMode = FilterMode.Point;
                            break;
                        default:
                        rtBase.filterMode = FilterMode.Point;
                        break;
                }

                for(int i = 1; i < anOmnity.cameraArray.Length; i++) {
                    anOmnity.cameraArray[i].renderTextureSettings.filterMode = rtBase.filterMode;
                }
                needsupdate = true;
            }
            if(GUILayout.Button( rtBase.mipmap? "Mip Mapping (currently enabled)":"Mip Mapping (currently disabled)")) {
                rtBase.mipmap = !rtBase.mipmap;
                needsupdate = true;
                for(int i = 1; i < anOmnity.cameraArray.Length; i++) {
                    anOmnity.cameraArray[i].renderTextureSettings.mipmap = rtBase.mipmap;
                }
            }
            if(rtBase.mipmap)
            if(OmnityHelperFunctions.FloatInputResetSliderWasChanged("Mip Map Bias",ref rtBase.mipMapBias, -3,-1,2)) {
                needsupdate = true;
                for(int i = 1; i < anOmnity.cameraArray.Length; i++) {
                    anOmnity.cameraArray[i].renderTextureSettings.mipMapBias = rtBase.mipMapBias;
                }
            }

            
            GUILayout.BeginHorizontal();
            if(IsInconsistent(anOmnity)) {
                if(GUILayout.Button("Sync Settings")) {
                    SyncSettings(anOmnity);
                    needsupdate = true;
                }
            }
            if(GUILayout.Button("Reset Settings")) {
                ResetSettings(anOmnity);
                needsupdate = true;
            }
            GUILayout.EndHorizontal();


            
            
            if(needsupdate) {
                for(int i = 0; i < anOmnity.cameraArray.Length; i++) {
                    anOmnity.cameraArray[i].myCamera.targetTexture = anOmnity.cameraArray[i].renderTextureSettings.GenerateRenderTexture(true);
                }

                anOmnity.DoConnectTextures();
            }
        }
    

        SaveLoadGUIButtons(anOmnity);
    }

    /// <summary>
    /// Called when the GUI closes
    /// </summary>
    public override void OnCloseGUI(Omnity anOmnity) {
        if (!anOmnity.PluginEnabled(OmnityPluginsIDs.OmnityQualityHelper)) {
            return;
        }
    }
}

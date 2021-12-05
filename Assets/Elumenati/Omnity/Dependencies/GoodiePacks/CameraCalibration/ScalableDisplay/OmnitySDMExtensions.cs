using OmnityMono;
using OmnityMono.OmnitySDMNamespace;
using System.Collections;
using UnityEngine;

static public class OmnitySDMExtensionsLocal {

    static public Material GetNewEdgeBlendVertexLitMaterial() {
        return FinalPassSetup.ApplySettings(FinalPassShaderType.EdgeBlendVertexColor);
    }

    static public Material GetNewEdgeBlendVertexLitMaterialPerspective() {
        return FinalPassSetup.ApplySettings(FinalPassShaderType.EdgeBlendVertexColorPerspective);
    }

    static public IEnumerator CoRoutineLoadFileSet(SDMPROJECTOR[] files, int rcLayerMask, int screenLayer, int fpcLayerMask, FinalPassShaderType finalPassShaderType, string prefix, bool stereo) {
        Debug.LogWarning("CoRoutineLoadFileSet");
        Omnity myOmnity = Omnity.anOmnity;

        int startLength = myOmnity.screenShapes.Length;

        myOmnity.CameraArrayResize(startLength + files.Length);
        myOmnity.ScreenShapeArrayResize(startLength + files.Length);
        if (!stereo) {
            myOmnity.FinalPassCameraArrayResize(startLength + files.Length);
        }
        yield return null;
        for (int iCameraIndex = 0; iCameraIndex < files.Length; iCameraIndex++) {
            myOmnity.screenShapes[iCameraIndex].finalPassShaderType = finalPassShaderType;
        }
        yield return null;
        yield return null;

        int offset = 0;

        for (int iProjectorIndex = 0; iProjectorIndex < files.Length; iProjectorIndex++) {
            Debug.Log("trying to load mask " + files[iProjectorIndex].maskFilenameURL);
            if (System.IO.File.Exists(files[iProjectorIndex].maskFilename)) {
                Debug.Log("file exists");

#pragma warning disable CS0618 
                WWW www3 = new WWW(files[iProjectorIndex].maskFilenameURL);
#pragma warning restore CS0618  
                yield return www3;
                try {
                    if (www3.texture) {
                        OmnityMono.OmnitySDMNamespace.OmnitySDMExtensions.masks.Add(www3.texture);
                        Debug.Log("sucess load mask");
                    } else {
                        OmnityMono.OmnitySDMNamespace.OmnitySDMExtensions.masks.Add(null);
                    }
                } catch (System.Exception e) {
                    Debug.LogError(e.Message);
                }
            } else {
                OmnityMono.OmnitySDMNamespace.OmnitySDMExtensions.masks.Add(null);
            }
            int iCameraIndex = iProjectorIndex + startLength;
            string myName = System.IO.Path.GetFileNameWithoutExtension(files[iProjectorIndex].file);
            Debug.Log(myName);
            int finalPassCameraLayer = screenLayer;
            int finalPassCameraCullingMask = 1 << finalPassCameraLayer;
            GameObject go = new GameObject();
            go.layer = finalPassCameraLayer;
            go.name = myName;

            OmnitySDMMeshMono sdmMesh = go.AddComponent<OmnitySDMMeshMono>();
            sdmMesh.Read(files[iProjectorIndex].file, finalPassShaderType, null, OmnityRendererExtensions.DisableShadowsAndLightProbes, OmnityPlatformDefines.OptimizeMesh);

            myOmnity.cameraArray[iCameraIndex].renderTextureSettings.width = sdmMesh.sdmBlendMesh.NATIVEXRES;
            ///                Debug.LogWarning("THIS SHOULD BE NATIVE Y RES");
            myOmnity.cameraArray[iCameraIndex].renderTextureSettings.height = sdmMesh.sdmBlendMesh.NATIVEYRES;

            myOmnity.cameraArray[iCameraIndex].omnityPerspectiveMatrix.fovL = sdmMesh.sdmBlendMesh.frustum.LeftAngle;
            myOmnity.cameraArray[iCameraIndex].omnityPerspectiveMatrix.fovR = sdmMesh.sdmBlendMesh.frustum.RightAngle;
            myOmnity.cameraArray[iCameraIndex].omnityPerspectiveMatrix.fovT = sdmMesh.sdmBlendMesh.frustum.TopAngle;
            myOmnity.cameraArray[iCameraIndex].omnityPerspectiveMatrix.fovB = sdmMesh.sdmBlendMesh.frustum.BottomAngle;

            Quaternion qP = Quaternion.Euler(sdmMesh.sdmBlendMesh.frustum.ViewAngleC_Pitch, 0, 0);
            Quaternion qY = Quaternion.Euler(0, sdmMesh.sdmBlendMesh.frustum.ViewAngleB_Yaw, 0);
            Quaternion qR = Quaternion.Euler(0, 0, sdmMesh.sdmBlendMesh.frustum.ViewAngleA_Roll);
            myOmnity.cameraArray[iCameraIndex].localEulerAngles = (qP * qY * qR).eulerAngles;
            myOmnity.cameraArray[iCameraIndex].localPosition = new Vector3(sdmMesh.sdmBlendMesh.frustum.XOffset, sdmMesh.sdmBlendMesh.frustum.YOffset, sdmMesh.sdmBlendMesh.frustum.ZOffset);

            myOmnity.cameraArray[iCameraIndex].cullingMask = finalPassCameraCullingMask;
            myOmnity.cameraArray[iCameraIndex].myCamera.gameObject.layer = finalPassCameraLayer;
            myOmnity.cameraArray[iCameraIndex].myCamera.gameObject.name = myName;

            if (myOmnity.cameraArray[iCameraIndex].localPosition != Vector3.zero) {
                Debug.Log("Make sure offset is correct");
            }

            myOmnity.cameraArray[iCameraIndex].UpdateCamera();
            yield return null;

            if (myOmnity.cameraArray[iCameraIndex].renderTextureSettings.rt != null) {
                myOmnity.cameraArray[iCameraIndex].renderTextureSettings.rt.Release();
                myOmnity.cameraArray[iCameraIndex].renderTextureSettings.rt = null;
            }
            yield return null;
            myOmnity.cameraArray[iCameraIndex].renderTextureSettings.GenerateRenderTexture();
            yield return null;

            if (stereo) {
                Debug.LogWarning("Setting zero depth fpc.myCamera.targetTexture.depth " + myOmnity.cameraArray[iCameraIndex].renderTextureSettings.rt.depth);
                myOmnity.cameraArray[iCameraIndex].renderTextureSettings.rt.depth = 0;
            }
            myOmnity.cameraArray[iCameraIndex].myCamera.targetTexture = myOmnity.cameraArray[iCameraIndex].renderTextureSettings.rt;

            yield return null;

            myOmnity.screenShapes[iCameraIndex].transformInfo.position = new Vector3(offset, 0, 0);

            myOmnity.screenShapes[iCameraIndex].layer = finalPassCameraLayer;
            myOmnity.screenShapes[iCameraIndex].renderer.gameObject.layer = finalPassCameraLayer;
            myOmnity.screenShapes[iCameraIndex].renderer.gameObject.name = myName;

            myOmnity.screenShapes[iCameraIndex].UpdateScreen();

            myOmnity.screenShapes[iCameraIndex].screenShapeType = OmnityScreenShapeType.CustomApplicationLoaded;
            if (myOmnity.screenShapes[iCameraIndex].renderer.gameObject.GetComponent<MeshFilter>().sharedMesh != null) {
                GameObject.Destroy(myOmnity.screenShapes[iCameraIndex].renderer.gameObject.GetComponent<MeshFilter>().sharedMesh);
            }
            myOmnity.screenShapes[iCameraIndex].renderer.gameObject.GetComponent<MeshFilter>().sharedMesh = sdmMesh.m;
            if (myOmnity.screenShapes[iCameraIndex].renderer.sharedMaterial != null) {
                Material.Destroy(myOmnity.screenShapes[iCameraIndex].renderer.sharedMaterial);
            }
            yield return null;
            myOmnity.screenShapes[iCameraIndex].renderer.sharedMaterial = OmnitySDMExtensionsLocal.GetNewEdgeBlendVertexLitMaterial();
            yield return null;
            Debug.Log("setting " + myOmnity.cameraArray[iCameraIndex].myCamera.targetTexture.width);
            myOmnity.screenShapes[iCameraIndex].renderer.sharedMaterial.mainTexture = myOmnity.cameraArray[iCameraIndex].myCamera.targetTexture;

            if (!stereo) {
                myOmnity.finalPassCameras[iCameraIndex].transformInfo.position = new Vector3(offset + sdmMesh.sdmBlendMesh.NATIVEXRES / 2, sdmMesh.sdmBlendMesh.NATIVEYRES / 2, 0);
                myOmnity.finalPassCameras[iCameraIndex].omnityPerspectiveMatrix.matrixMode = MatrixMode.Orthographic;
                myOmnity.finalPassCameras[iCameraIndex].projectorType = OmnityProjectorType.Rectilinear;
                myOmnity.finalPassCameras[iCameraIndex].omnityPerspectiveMatrix.orthographicSize = sdmMesh.sdmBlendMesh.NATIVEYRES / 2;
                myOmnity.finalPassCameras[iCameraIndex].omnityPerspectiveMatrix.near = -1;
                myOmnity.finalPassCameras[iCameraIndex].omnityPerspectiveMatrix.far = 1;

                myOmnity.finalPassCameras[iCameraIndex].cullingMask = finalPassCameraCullingMask;
                myOmnity.finalPassCameras[iCameraIndex].myCamera.gameObject.layer = finalPassCameraLayer;
                myOmnity.finalPassCameras[iCameraIndex].myCamera.gameObject.name = myName;
                myOmnity.finalPassCameras[iCameraIndex].normalizedViewportRect = files[iProjectorIndex].rect;
                myOmnity.finalPassCameras[iCameraIndex].myCamera.ResetProjectionMatrix();
                myOmnity.finalPassCameras[iCameraIndex].UpdateCamera();
                myOmnity.finalPassCameras[iCameraIndex].myCamera.rect = files[iProjectorIndex].rect;
            }
            go.transform.localPosition = new Vector3(offset, 0, 0);
            offset += sdmMesh.sdmBlendMesh.NATIVEXRES;

            Renderer myRenderer = go.GetComponent<Renderer>();
            Material mat = null;
            if (myRenderer != null) {
                mat = myRenderer.sharedMaterial;
            }

            GameObject.Destroy(mat);
            GameObject.Destroy(myRenderer);
            GameObject.Destroy(go);
        }
        yield return null;

        for (int iProjectorIndex = 0; iProjectorIndex < files.Length; iProjectorIndex++) {
            int iCameraIndex = iProjectorIndex + startLength;
            int iMaskIndex = (iProjectorIndex % files.Length);

            myOmnity.screenShapes[iCameraIndex].renderer.sharedMaterial.mainTexture = myOmnity.cameraArray[iCameraIndex].myCamera.targetTexture = myOmnity.cameraArray[iCameraIndex].renderTextureSettings.rt;

            try {
                Debug.LogWarning("try apply mask");
                myOmnity.screenShapes[iCameraIndex].renderer.sharedMaterial.SetTexture("_MaskTex", OmnitySDMExtensions.masks[iMaskIndex]);
            } catch (System.Exception e) {
                Debug.LogError(e.Message);
            }
            myOmnity.screenShapes[iCameraIndex].layer = screenLayer;
            myOmnity.screenShapes[iCameraIndex].trans.gameObject.layer = screenLayer;

            myOmnity.cameraArray[iCameraIndex].cullingMask = rcLayerMask;
            myOmnity.cameraArray[iCameraIndex].myCamera.cullingMask = rcLayerMask;
            myOmnity.cameraArray[iCameraIndex].myCamera.gameObject.layer = screenLayer;
            if (!stereo) {
                myOmnity.finalPassCameras[iCameraIndex].myCamera.cullingMask = fpcLayerMask;
                myOmnity.finalPassCameras[iCameraIndex].cullingMask = fpcLayerMask;
            }
        }

        for (int iProjectorIndex = 0; iProjectorIndex < files.Length; iProjectorIndex++) {
            int iCameraIndex = iProjectorIndex + startLength;

            myOmnity.screenShapes[iCameraIndex].name = prefix + iProjectorIndex;

            myOmnity.cameraArray[iCameraIndex].name = prefix + iProjectorIndex;

            myOmnity.screenShapes[iCameraIndex].trans.gameObject.name = prefix + iProjectorIndex;
            myOmnity.cameraArray[iCameraIndex].myCamera.gameObject.name = prefix + iProjectorIndex;

            myOmnity.screenShapes[iCameraIndex].trans.gameObject.layer = rcLayerMask;
            myOmnity.cameraArray[iCameraIndex].myCamera.gameObject.layer = screenLayer;

            myOmnity.screenShapes[iCameraIndex].trans.gameObject.layer = screenLayer;
            myOmnity.cameraArray[iCameraIndex].myCamera.gameObject.layer = screenLayer;
            if (!stereo) {
                myOmnity.finalPassCameras[iCameraIndex].name = prefix + iProjectorIndex;
                myOmnity.finalPassCameras[iCameraIndex].myCamera.gameObject.name = prefix + iProjectorIndex;
                myOmnity.finalPassCameras[iCameraIndex].myCamera.gameObject.layer = screenLayer;
                myOmnity.finalPassCameras[iCameraIndex].myCamera.gameObject.layer = screenLayer;
            }
        }

        myOmnity.forceRefresh = true;
    }

    static public IEnumerator CoRoutineLoadFileSetOthographic(SDMPROJECTOR[] files, int rcLayerMask, int screenLayer, int fpcLayerMask, string prefix, bool stereo, bool alternativeCentering) {
        Debug.LogWarning("CoRoutineLoadFileSet");
        Omnity myOmnity = Omnity.anOmnity;

        int startLengthSS = myOmnity.screenShapes.Length;
        int startLengthFPC = myOmnity.finalPassCameras.Length;

        myOmnity.ScreenShapeArrayResize(startLengthSS + files.Length, false, false);
        if (!stereo) {
            myOmnity.FinalPassCameraArrayResize(startLengthSS + files.Length, false);
        }
        for (int iCameraIndex = 0; iCameraIndex < files.Length; iCameraIndex++) {
            myOmnity.screenShapes[startLengthSS + iCameraIndex].finalPassShaderType = FinalPassShaderType.EdgeBlendVertexColor;
        }
        yield return null;
        yield return null;
        yield return null;

        int offset = 0;

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
            int finalPassCameraCullingMask = 1 << finalPassCameraLayer;
            GameObject go = new GameObject();
            go.layer = finalPassCameraLayer;
            go.name = myName;
            OmnitySDMMeshMono sdmMesh = go.AddComponent<OmnitySDMMeshMono>();
            sdmMesh.Read(files[iProjectorIndex].file, FinalPassShaderType.EdgeBlendVertexColor, null, OmnityRendererExtensions.DisableShadowsAndLightProbes, OmnityPlatformDefines.OptimizeMesh);

            yield return null;

            myOmnity.screenShapes[startLengthSS + iProjectorIndex].transformInfo.position = new Vector3(offset, 0, 0);

            myOmnity.screenShapes[startLengthSS + iProjectorIndex].layer = finalPassCameraLayer;
            myOmnity.screenShapes[startLengthSS + iProjectorIndex].renderer.gameObject.layer = finalPassCameraLayer;
            myOmnity.screenShapes[startLengthSS + iProjectorIndex].renderer.gameObject.name = myName;

            myOmnity.screenShapes[startLengthSS + iProjectorIndex].trans.localPosition = myOmnity.screenShapes[startLengthSS + iProjectorIndex].transformInfo.position;
            myOmnity.screenShapes[startLengthSS + iProjectorIndex].UpdateScreen();

            myOmnity.screenShapes[startLengthSS + iProjectorIndex].screenShapeType = OmnityScreenShapeType.CustomApplicationLoaded;
            if (myOmnity.screenShapes[startLengthSS + iProjectorIndex].renderer.gameObject.GetComponent<MeshFilter>().sharedMesh != null) {
                GameObject.Destroy(myOmnity.screenShapes[startLengthSS + iProjectorIndex].renderer.gameObject.GetComponent<MeshFilter>().sharedMesh);
            }
            myOmnity.screenShapes[startLengthSS + iProjectorIndex].renderer.gameObject.GetComponent<MeshFilter>().sharedMesh = sdmMesh.m;
            if (myOmnity.screenShapes[startLengthSS + iProjectorIndex].renderer.sharedMaterial != null) {
                Material.Destroy(myOmnity.screenShapes[startLengthSS + iProjectorIndex].renderer.sharedMaterial);
            }
            yield return null;
            myOmnity.screenShapes[startLengthSS + iProjectorIndex].finalPassShaderType = FinalPassShaderType.EdgeBlendVertexColor;
            myOmnity.screenShapes[startLengthSS + iProjectorIndex].renderer.sharedMaterial = FinalPassSetup.ApplySettings(FinalPassShaderType.EdgeBlendVertexColor);
            yield return null;

            Debug.Log("todo set texture");

            if (!stereo) {
                if (alternativeCentering) {
                    myOmnity.finalPassCameras[startLengthFPC + iProjectorIndex].transformInfo.position = new Vector3(sdmMesh.sdmBlendMesh.NATIVEXRES / 2, sdmMesh.sdmBlendMesh.NATIVEYRES / 2, 0);
                } else {
                    myOmnity.finalPassCameras[startLengthFPC + iProjectorIndex].transformInfo.position = new Vector3(offset + sdmMesh.sdmBlendMesh.NATIVEXRES / 2, sdmMesh.sdmBlendMesh.NATIVEYRES / 2, 0);
                }
                myOmnity.finalPassCameras[startLengthFPC + iProjectorIndex].omnityPerspectiveMatrix.matrixMode = MatrixMode.Orthographic;
                myOmnity.finalPassCameras[startLengthFPC + iProjectorIndex].projectorType = OmnityProjectorType.Rectilinear;
                myOmnity.finalPassCameras[startLengthFPC + iProjectorIndex].omnityPerspectiveMatrix.orthographicSize = sdmMesh.sdmBlendMesh.NATIVEYRES / 2;
                myOmnity.finalPassCameras[startLengthFPC + iProjectorIndex].omnityPerspectiveMatrix.near = -1;
                myOmnity.finalPassCameras[startLengthFPC + iProjectorIndex].omnityPerspectiveMatrix.far = 1;
                myOmnity.finalPassCameras[startLengthFPC + iProjectorIndex].cullingMask = finalPassCameraCullingMask;
                myOmnity.finalPassCameras[startLengthFPC + iProjectorIndex].myCamera.gameObject.layer = finalPassCameraLayer;
                myOmnity.finalPassCameras[startLengthFPC + iProjectorIndex].myCamera.gameObject.name = myName;
                myOmnity.finalPassCameras[startLengthFPC + iProjectorIndex].normalizedViewportRect = files[iProjectorIndex].rect;
                myOmnity.finalPassCameras[startLengthFPC + iProjectorIndex].myCamera.ResetProjectionMatrix();
                myOmnity.finalPassCameras[startLengthFPC + iProjectorIndex].UpdateCamera();
                myOmnity.finalPassCameras[startLengthFPC + iProjectorIndex].myCamera.rect = files[iProjectorIndex].rect;
            }
            go.transform.localPosition = new Vector3(offset, 0, 0);
            offset += sdmMesh.sdmBlendMesh.NATIVEXRES;

            Renderer gorenderer = go.GetComponent<Renderer>();
            Material mat = null;
            if (gorenderer != null) {
                mat = gorenderer.sharedMaterial;
            }

            GameObject.Destroy(mat);
            GameObject.Destroy(gorenderer);
            GameObject.Destroy(go);
        }
        yield return null;

        for (int iProjectorIndex = 0; iProjectorIndex < files.Length; iProjectorIndex++) {
            myOmnity.screenShapes[startLengthSS + iProjectorIndex].layer = screenLayer;
            myOmnity.screenShapes[startLengthSS + iProjectorIndex].trans.gameObject.layer = screenLayer;
            if (!stereo) {
                myOmnity.finalPassCameras[startLengthFPC + iProjectorIndex].myCamera.cullingMask = fpcLayerMask;
                myOmnity.finalPassCameras[startLengthFPC + iProjectorIndex].cullingMask = fpcLayerMask;
            }
        }

        for (int iProjectorIndex = 0; iProjectorIndex < files.Length; iProjectorIndex++) {
            myOmnity.screenShapes[startLengthSS + iProjectorIndex].name = prefix + iProjectorIndex;
            myOmnity.screenShapes[startLengthSS + iProjectorIndex].trans.gameObject.name = prefix + iProjectorIndex;
            myOmnity.screenShapes[startLengthSS + iProjectorIndex].trans.gameObject.layer = rcLayerMask;
            myOmnity.screenShapes[startLengthSS + iProjectorIndex].trans.gameObject.layer = screenLayer;
            if (!stereo) {
                myOmnity.finalPassCameras[startLengthFPC + iProjectorIndex].name = prefix + iProjectorIndex;
                myOmnity.finalPassCameras[startLengthFPC + iProjectorIndex].myCamera.gameObject.name = prefix + iProjectorIndex;
                myOmnity.finalPassCameras[startLengthFPC + iProjectorIndex].myCamera.gameObject.layer = screenLayer;
                myOmnity.finalPassCameras[startLengthFPC + iProjectorIndex].myCamera.gameObject.layer = screenLayer;
            }
        }

        myOmnity.forceRefresh = true;
    }
}
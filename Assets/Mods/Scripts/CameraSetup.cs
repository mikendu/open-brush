using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using Klak.Ndi;

public class CameraSetup : MonoBehaviour
{
    public int TextureSize = 1024;
    public PostProcessResources PostProcessingResources;

    private int attempts = 0;
    private bool setupPostProcessing;
    private bool success = false;

    private RenderTexture renderTexture;

    // Start is called before the first frame update
    IEnumerator Start()
    {
        yield return StartCoroutine("AttemptSetup");
    }

    // Update is called once per frame
    void Update()
    {        
    }

    private IEnumerator AttemptSetup()
    {
        while (attempts < 100 && !success)
        {
            yield return new WaitForSeconds(0.25f);

            attempts += 1;
            Debug.Log("Starting camera setup attempt #" + attempts);

            FinalPassCameraProxy finalPassProxy;
            Camera finalPass;
            PerspectiveCameraProxy[] cameraProxies;
            List<Camera> cameras = new List<Camera>();
            NdiSender ndiSender;

            finalPassProxy = FindObjectOfType<FinalPassCameraProxy>();
            if (finalPassProxy == null)
            {
                Debug.LogError("Camera setup failed. Could not find target camera proxy!");
                continue;
            }

            finalPass = finalPassProxy.GetComponent<Camera>();
            if (finalPass == null)
            {
                Debug.LogError("Camera setup failed. Could not find final pass camera!");
                continue;
            }


            cameraProxies = FindObjectsOfType<PerspectiveCameraProxy>();
            if (cameraProxies == null)
            {
                Debug.LogError("Camera setup failed. Could not find perspective camera proxy!");
                continue;
            }
            
            foreach (PerspectiveCameraProxy proxy in cameraProxies)
                cameras.Add(proxy.GetComponent<Camera>());

            if (cameras.Count == 0)
            {
                Debug.LogError("Camera setup failed. Could not find final pass camera!");
                continue;
            }

            ndiSender = GetComponent<NdiSender>();
            if (ndiSender == null)
            {
                Debug.LogError("Camera setup failed. Could not find ndi connection!");
                continue;
            }


            if (!setupPostProcessing)
            {
                foreach (Camera camera in cameras) {
                    PostProcessLayer postProcess = camera.gameObject.AddComponent<PostProcessLayer>();
                    postProcess.Init(PostProcessingResources);
                    postProcess.volumeTrigger = camera.transform;
                    postProcess.volumeLayer = LayerMask.GetMask("PostProcessing");
                    postProcess.antialiasingMode = PostProcessLayer.Antialiasing.FastApproximateAntialiasing;
                    postProcess.stopNaNPropagation = true;
                }
                setupPostProcessing = true;
            }

            if (renderTexture == null)
            {
                renderTexture = new RenderTexture(TextureSize, TextureSize, 0, RenderTextureFormat.RGB111110Float);
                renderTexture.name = "NDI Target Texture";
                renderTexture.Create();
                finalPass.targetTexture = renderTexture;
                ndiSender.sourceTexture = renderTexture;
            }


            success = true;
            Debug.Log("Camera setup succeeded after " + attempts + " attempts!");
        }
    }
}

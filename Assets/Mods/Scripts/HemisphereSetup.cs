using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HemisphereSetup : MonoBehaviour
{
    public float HemisphereSize = 3.0f;

    private ScreenShapeProxy hemisphere;
    private float counter = 0.0f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (counter < 3.0f && hemisphere == null)
        {
            hemisphere = GameObject.FindObjectOfType<ScreenShapeProxy>();
            counter += Time.deltaTime;

            if (hemisphere != null)
                hemisphere.gameObject.AddComponent<DomeCameraWidget>();
        }

        if (hemisphere != null)
        {
            hemisphere.transform.localScale = HemisphereSize * Vector3.one;
        }
    }
}

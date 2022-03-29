using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TiltBrush;

public class CameraPositioner : MonoBehaviour
{
    private Vector3 startingPosition = new Vector3(-30, 30, -30);
    private Quaternion startingRotation = Quaternion.identity;

    void Start()
    {
        startingPosition = this.transform.localPosition;
        startingRotation = this.transform.localRotation;
    }


    // Update is called once per frame
    void Update()
    {
        float scale = App.Scene.Pose.scale;
        Quaternion rotation = App.Scene.Pose.rotation;

        // Vector3 invertPosition = new Vector3(0, 0 - App.Scene.Pose.translation.y, 0.0f);
        Vector3 invertScale = Vector3.one * (1.0f / scale);
        Quaternion invertRotation = rotation.TrueInverse();
        Matrix4x4 inverse = Matrix4x4.TRS(Vector3.zero, invertRotation, invertScale);

        // this.transform.localPosition = inverse * (Position + invertPosition);
        this.transform.localPosition = inverse * startingPosition;
        this.transform.localScale = invertScale;
        this.transform.localRotation = invertRotation * startingRotation;
    }
}

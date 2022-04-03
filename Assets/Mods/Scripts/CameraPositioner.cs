using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TiltBrush;

public class CameraPositioner : MonoBehaviour
{
    public Vector3 staticPosition = new Vector3(-30, 30, -30);
    public Quaternion staticRotation = Quaternion.identity;
    private bool snapRotation;

    void Start()
    {
        staticPosition = this.transform.localPosition;
        staticRotation = this.transform.localRotation;
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
        // Vector3 worldOrigin = App.Scene.Pose.inverse.MultiplyPoint(Vector3.zero);
        // this.transform.localPosition = worldOrigin;

        this.transform.localPosition = inverse * staticPosition;
        this.transform.localScale = invertScale;
        this.transform.localRotation = invertRotation * staticRotation;

        float rollAngle = Mathf.Rad2Deg * Mathf.Acos(Vector3.Dot(transform.right, Vector3.up));
        float rollCorrectionAngle = rollAngle - 90.0f;
        Quaternion rollCorrection = Quaternion.AngleAxis(rollCorrectionAngle, transform.up);
        transform.localRotation = rollCorrection * transform.localRotation;

        if (snapRotation)
        {
            float pitchAngle = Mathf.Rad2Deg * Mathf.Acos(Vector3.Dot(transform.forward, -Vector3.up));
            float pitchCorrectionAngle = pitchAngle - 45;
            Quaternion pitchCorrection = Quaternion.AngleAxis(pitchCorrectionAngle, transform.right);
            transform.localRotation = pitchCorrection * transform.localRotation;
        }
    }


    public void SetWorldPose(Vector3 worldPosition, Quaternion worldRotation, bool snapped)
    {
        float scale = App.Scene.Pose.scale;
        Quaternion rotation = App.Scene.Pose.rotation;
        Matrix4x4 rotateScale = Matrix4x4.TRS(Vector3.zero, rotation, scale * Vector3.one);

        Vector3 desiredLocalPosition = App.Scene.Pose.inverse * worldPosition;
        Vector3 desiredStaticPosition = rotateScale * desiredLocalPosition;
        staticPosition = desiredStaticPosition;

        // Quaternion desiredLocalRotation = App.Scene.Pose.inverse.rotation * worldRotation;
        // Quaternion desiredStaticRotation = rotation * desiredLocalRotation;
        //staticRotation = desiredStaticRotation;

        // Fully un-snapped, can rotate any which way
        staticRotation = worldRotation;
        this.snapRotation = snapped;


    }

}

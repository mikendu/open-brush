using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TiltBrush;

public class DomeCameraWidget : GrabWidget
{
    private SphereCollider Collider;
    private CameraPositioner Positioner;

    // Start is called before the first frame update
    protected override void Awake()
    {
        Collider = gameObject.AddComponent<SphereCollider>();
        Collider.center = Vector3.zero;
        Collider.radius = 1.0f;

        m_HighlightMeshXfs = new Transform[] { transform };
        m_AllowSnapping = true;
        m_GrabDistance = 4.0f;
        m_CollisionRadius = 1.2f;

        Positioner = transform.root.gameObject.GetComponentInChildren<CameraPositioner>();
        base.Awake();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    override public void RecordAndSetPosRot(TrTransform inputXf)
    {
        // base.RecordAndSetPosRot(inputXf);
        // transform.position = inputXf.translation;

        HandleSnap();
        
        bool snapped = m_AllowSnapping && SnapEnabled;
        Positioner.SetWorldPose(inputXf.translation, inputXf.rotation, snapped);
    }

    private void HandleSnap()
    {
        // Refresh snap input and enter/exit snapping state.
        if (m_AllowSnapping)
        {
            SnapEnabled = InputManager.Controllers[(int)m_InteractingController].GetCommand(
                    InputManager.SketchCommands.MenuContextClick) &&
                SketchControlsScript.m_Instance.ShouldRespondToPadInput(m_InteractingController) &&
                !m_Pinned;

            if (!m_bWasSnapping && SnapEnabled)
            {
                InitiateSnapping();
            }

            if (m_bWasSnapping && !SnapEnabled)
            {
                FinishSnapping();
                m_SnapDriftCancel = false;
            }
        }
    }




    public override Bounds GetBounds_SelectionCanvasSpace()
    {
        if (Collider != null)
        {
            TrTransform colliderToCanvasXf = App.Scene.SelectionCanvas.Pose.inverse *
                TrTransform.FromTransform(Collider.transform);
            Bounds bounds = new Bounds(colliderToCanvasXf * Collider.center, Vector3.zero);

            // Spheres are invariant with rotation, so take out the rotation from the transform and just
            // add the two opposing corners.
            colliderToCanvasXf.rotation = Quaternion.identity;
            bounds.Encapsulate(colliderToCanvasXf * (Collider.center + Collider.radius * Vector3.one));
            bounds.Encapsulate(colliderToCanvasXf * (Collider.center - Collider.radius * Vector3.one));

            return bounds;
        }
        return base.GetBounds_SelectionCanvasSpace();
    }
}

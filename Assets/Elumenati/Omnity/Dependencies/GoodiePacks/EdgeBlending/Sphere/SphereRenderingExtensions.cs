using OmnityMono;
using UnityEngine;

/// <summary>
/// TTools for sphere rendering extensions
/// </summary>
public class SphereRenderingExtensions : OmnityAutoInitPlugin {
    static public OmnityPluginsIDs _myOmnityPluginsID = OmnityPluginsIDs.SphereRenderingExtensions;
    override public OmnityPluginsIDs myOmnityPluginsID { get { return _myOmnityPluginsID; } }

    static public void OmnityInit() {
        if (!Omnity.anOmnity.pluginIDs.Contains((int)OmnityPluginsIDs.OmniVioso)) {
            StartReg();
            if (IsRegisteredSuccessful) {
                OmnityPerspectiveMatrix.customMatrixFunction = MatrixHelper.PerspectiveOffCenterWithPotentialInvert;
            }
        }
    }


    private static bool IsRegisteredSuccessful = false;
    private static bool startedChecking = false;
    private static bool OmniMouseEnabled = false;
    private static bool MouseEnabled = false;
    private static bool anOmnityEnabled = false;

    static public void StartReg() {
        if (!startedChecking) {
            System.Collections.Generic.List<string> features = new System.Collections.Generic.List<string> { _myOmnityPluginsID.ToString() };
            if (ActivationClass.CheckActivationFast(features)) {
                IsRegisteredSuccessful = true;
                return;
            }

            startedChecking = true;
            PushSettings();
            ActivationClass.CheckActivationAsynchronous(
                features,
            () => {
                //    invertRendering = true;
                IsRegisteredSuccessful = true;
                PopSettings();
            },
            () => {
                // invertRendering = true;
                IsRegisteredSuccessful = true;
                PopSettings();
            },
            () => {
                // invertRendering = false;
                IsRegisteredSuccessful = false;
                PopSettings();
            });
        }
    }


    private static void PushSettings() {
        if (OmniMouse.singleton != null) {
            OmniMouseEnabled = OmniMouse.singleton.enabled;
        }
        MouseEnabled = OmnityPlatformDefines.GetCursor_visible();
        anOmnityEnabled = Omnity.anOmnity.myOmnityGUI.GUIEnabled;
        Omnity.anOmnity.myOmnityGUI.GUIEnabled = false;
        if (OmniMouse.singleton != null) {
            OmniMouse.singleton.enabled = false;
        }
        OmnityPlatformDefines.SetCursor_visible(true);
    }

    private static void PopSettings() {
        if (OmniMouse.singleton != null) {
            OmniMouse.singleton.enabled = OmniMouseEnabled;
        }
        OmnityPlatformDefines.SetCursor_visible(MouseEnabled);
        Omnity.anOmnity.myOmnityGUI.GUIEnabled = anOmnityEnabled;
    }
}

/*

/// <summary>
/// call this in engine before omnity is loaded for the first time
/// Omnity.onReloadEndCallback += NewInfinityMatrix.DoRegister;
/// </summary>
///

static public class NewInfinityMatrix {
    static  OmnityPerspectiveMatrix.CustomMatrixFunction oldMatrixFunction =null;

    static public void DoRegister(Omnity anOmnity) {
        // Save the old custom matrix in case we need it for sphere rendering....  Or you can just make SphereRenderingExtensions.PerspectiveOffCenterWithPotentialInvert public
        // this only needs to be done once, but it must be called after OmnityPerspectiveMatrix.customMatrixFunction is set if you want sphere rendering to continue working.
        if (oldMatrixFunction == null) {
            if (OmnityPerspectiveMatrix.customMatrixFunction == null) {
                Debug.LogError("Uh Oh, we started too soon.");
            }
            oldMatrixFunction = OmnityPerspectiveMatrix.customMatrixFunction;
            OmnityPerspectiveMatrix.customMatrixFunction = MyCustomMatrix;
        }
    }

    static private Matrix4x4 MyCustomMatrix(float left, float right, float bottom, float top, float near, float far, MatrixMode matrixMode) {
        if (matrixMode == MatrixMode.Inverted && oldMatrixFunction !=null) {
            return oldMatrixFunction(left, right, bottom, top, near, far, matrixMode);
        }

        float fov = Mathf.Abs(right - left);

        Matrix4x4 m = new Matrix4x4();
        m[0, 0] = G;
        m[0, 1] = O;
        m[0, 2] = O;
        m[0, 3] = D;
        m[1, 0] = _;
        m[1, 1] = L;
        m[1, 2] = U;
        m[1, 3] = C;
        m[2, 0] = K;
        m[2, 1] = !;
        m[2, 2] = !;
        m[2, 3] = !;
        m[3, 0] = !;
        m[3, 1] = !;
        m[3, 2] = !;
        m[3, 3] = !;
        return m;
    }
}*/
using System.Xml.XPath;
using UnityEngine;

public class OmniMouse : OmnityTabbedFeature {
    static public OmnityPluginsIDs _myOmnityPluginsID = OmnityPluginsIDs.OmniMouse;
    override public OmnityPluginsIDs myOmnityPluginsID { get { return _myOmnityPluginsID; } }
    /*
    static public OmniMouse OmniCreateSingleton(GameObject go) {
        // Omnimouse is a special case and must be added seperately to its own quad
        return singleton;
        //        return GetSingleton<OmniMouse>(ref singleton, go);
    }*/

    static public class InputRouter {

        public class MouseCalls {
            virtual public bool fnMouseLeftPress() { return Input.GetMouseButton(0); }
            virtual public bool fnMouseLeftReleaseOnce() { return Input.GetMouseButtonUp(0); }
            virtual public bool fnMouseLeftPressOnce() { return Input.GetMouseButtonDown(0); }
            virtual public float fnMouseWheelX() { return UnityEngine.Input.mouseScrollDelta.x; }
            virtual public float fnMouseWheelY() { return UnityEngine.Input.mouseScrollDelta.y; }
            virtual public Vector2 fnMousePosition() { return Input.mousePosition; }
            virtual public float fnMouseAxisX() { return Input.GetAxis("Mouse X"); }
            virtual public float fnMouseAxisY() { return Input.GetAxis("Mouse Y"); }
            virtual public int fnMouseRemoteID() { return -1; }
        }


        static public MouseCalls mouseRouter = new MouseCalls();

        static public bool mouseLeftPress { get { return mouseRouter.fnMouseLeftPress(); } }
        static public bool mouseLeftReleaseOnce { get { return mouseRouter.fnMouseLeftReleaseOnce(); } }
        static public bool mouseLeftPressOnce { get { return mouseRouter.fnMouseLeftPressOnce(); } }
        static public float mouseWheelX { get { return mouseRouter.fnMouseWheelX(); } }
        static public float mouseWheelY { get { return mouseRouter.fnMouseWheelY(); } }
        static public Vector2 mousePosition { get { return mouseRouter.fnMousePosition(); } }
        static public float mouseAxisX { get { return mouseRouter.fnMouseAxisX(); } }
        static public float mouseAxisY { get { return mouseRouter.fnMouseAxisY(); } }
        static public int mouseRemoteId { get { return mouseRouter.fnMouseRemoteID(); } }

#if INPUTROUTING_DDEBUG
        static public string debugStatus() {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            sb.AppendLine("OmniMouse");
            sb.Append("mouseLeftPress\t"); sb.Append(mouseLeftPress); sb.AppendLine("");
            sb.Append("mouseLeftReleaseOnce\t"); sb.Append(mouseLeftReleaseOnce); sb.AppendLine("");
            sb.Append("mouseLeftPressOnce\t"); sb.Append(mouseLeftPressOnce); sb.AppendLine("");
            sb.Append("mousePosition\t"); sb.Append(mousePosition); sb.AppendLine("");
            sb.Append("mouseAxisX\t"); sb.Append(mouseAxisX); sb.AppendLine("");
            sb.Append("mouseAxisY\t"); sb.Append(mouseAxisY); sb.AppendLine("");
            sb.Append("mouseWheelY\t"); sb.Append(mouseWheelY); sb.AppendLine("");
            sb.Append("mouseRemoteID\t"); sb.Append(mouseRemoteId); sb.AppendLine("");
            sb.AppendLine("");

            return sb.ToString();
        }
#endif
    }


    private void Awake() {
        if (VerifySingleton<OmniMouse>(ref singleton)) {
            myRenderer = GetComponent<Renderer>();
            if (myRenderer == null) {
                Debug.LogError("OMNIMOUSE SHOULD BE ATTACHED TO THE CROSSHAIR GAME OBJECT");
            }
            singleton = this;
            Omnity.onReloadStartCallback += ReloadDelegate;
            Omnity.onReloadEndCallbackPriority.AddLoaderFunction(PriorityEventHandler.Ordering.Order_OmniMouse, CoroutineLoader);
            Omnity.onSaveCompleteCallback += Save;
        }
    }

    public static bool isEnabled {
        get {
            return (singleton != null && singleton.enabled);
        }
    }

    //register funcNeedSystemMouse with an action that returns true if a gui is showing and you need to show the mouse.
    // for example if you have an escape menu that needs mouse tempoarly.
    static public System.Collections.Generic.List<System.Func<bool>> funcNeedSystemMouse = new System.Collections.Generic.List<System.Func<bool>>();

    public override string BaseName {
        get {
            return "OmniMouse";
        }
    }

    static public OmnityMouseType mouseType = OmnityMouseType.MouseFlatScreen;

    public enum OmnityMouseType {
        MouseFlatScreen = 1,
        TouchFlatScreen = 2,
        CenteredCrossHair = 3,
        MouseDomeScreen = 4
    }

    public enum OmnityMouseStyle {
        AlwaysVisible = 0,
        AlwaysHidden = 1,
    }

    public OmnityMouseStyle mouseStyle = OmnityMouseStyle.AlwaysVisible;
    public OmnityMouseStyle crosshairStyle = OmnityMouseStyle.AlwaysVisible;

    public OmnityMouseStyle MouseStyleDefault {
        get {
            return OmnityMouseStyle.AlwaysVisible;
        }
    }

    public OmnityMouseStyle CrosshairStyleDefault {
        get {
            return OmnityMouseStyle.AlwaysVisible;
        }
    }

    static public int finalPassCameraIndex = 0;
    static public int screenShapeIndex = 0;

    public override void ReadXMLDelegate(XPathNavigator nav) {
        finalPassCameraIndex = OmnityHelperFunctions.ReadElementIntDefault(nav, ".//finalPassCameraIndex", 0);
        screenShapeIndex = OmnityHelperFunctions.ReadElementIntDefault(nav, ".//screenShapeIndex", 0);
        mouseType = OmnityHelperFunctions.ReadElementEnumDefault<OmnityMouseType>(nav, ".//mouseType", OmnityMouseType.CenteredCrossHair);
        mouseStyle = OmnityHelperFunctions.ReadElementEnumDefault<OmnityMouseStyle>(nav, ".//mouseStyle", MouseStyleDefault);
        crosshairStyle = OmnityHelperFunctions.ReadElementEnumDefault<OmnityMouseStyle>(nav, ".//crosshairStyle", CrosshairStyleDefault);
        xSensitivity = OmnityHelperFunctions.ReadElementFloatDefault(nav, ".//xSensitivity", 1);
        ySensitivity = OmnityHelperFunctions.ReadElementFloatDefault(nav, ".//ySensitivity", 1);
        flipTiltCorrection = OmnityHelperFunctions.ReadElementBoolDefault(nav, ".//flipTiltCorrection", false);
        eulerOffset = OmnityHelperFunctions.ReadElementVector3Default(nav, ".//eulerOffset", Vector3.zero);
    }

    /// <param name="xmlWriter">The XML writer.</param>
    override public void WriteXMLDelegate(System.Xml.XmlTextWriter xmlWriter) {
        xmlWriter.WriteElementString("finalPassCameraIndex", finalPassCameraIndex.ToString());
        xmlWriter.WriteElementString("screenShapeIndex", screenShapeIndex.ToString());
        xmlWriter.WriteElementString("mouseType", mouseType.ToString());
        xmlWriter.WriteElementString("mouseStyle", mouseStyle.ToString());
        xmlWriter.WriteElementString("crosshairStyle", crosshairStyle.ToString());
        xmlWriter.WriteElementString("eulerOffset", eulerOffset.ToString());
        xmlWriter.WriteElementString("xSensitivity", xSensitivity.ToString());
        xmlWriter.WriteElementString("ySensitivity", ySensitivity.ToString());
        xmlWriter.WriteElementString("flipTiltCorrection", flipTiltCorrection.ToString());
    }

    static public OmniMouse singleton = null;

    [System.NonSerialized]
    static private Renderer myRenderer = null;

    private void OnDestroy() {
        singleton = null;
        Omnity.onReloadStartCallback -= ReloadDelegate;
        Omnity.onReloadEndCallbackPriority.RemoveLoaderFunction(PriorityEventHandler.Ordering.Order_OmniMouse, CoroutineLoader);
        Omnity.onSaveCompleteCallback -= Save;
    }

    private void ReloadDelegate(Omnity omnityClass) {
        if (myRenderer != null) {
            // this should happen even if not enabled
            this.transform.parent = null;
        }
    }

    public override System.Collections.IEnumerator PostLoad() {
        if (myRenderer != null) {
            ApplyMouseExtensExpensiveCall(Omnity.anOmnity);
        }
        yield break;
    }

    /// <summary>
    /// Overload this with the gui layout calls.
    /// </summary>
    override public void MyGuiCallback(Omnity anOmnity) {
        ApplyMouseExtensExpensiveCall(anOmnity);
        GUILayout.Label("Mouse movement is defined via the screen layout.");
        if (myRenderer == null) {
            GUILayout.Label("Warning OmniMouse should be attached to mouse cursor quad.");
        }
        mouseType = OmnityHelperFunctions.EnumInputReset<OmnityMouseType>("OmnityMouseType", "Mouse Type", mouseType, OmnityMouseType.CenteredCrossHair, 1);
        mouseStyle = OmnityHelperFunctions.EnumInputReset<OmnityMouseStyle>("OmnityMouseStyle", "Mouse Style", mouseStyle, MouseStyleDefault, 1);
        crosshairStyle = OmnityHelperFunctions.EnumInputReset<OmnityMouseStyle>("OmnityMouseStyle", "Crosshair Style", crosshairStyle, CrosshairStyleDefault, 1);

        switch (mouseType) {
            case OmnityMouseType.MouseDomeScreen:
                xSensitivity = OmnityHelperFunctions.FloatInputReset("xSensitivity", xSensitivity, 1);
                ySensitivity = OmnityHelperFunctions.FloatInputReset("ySensitivity", ySensitivity, 1);
                flipTiltCorrection = OmnityHelperFunctions.BoolInputReset("flipTiltCorrection", flipTiltCorrection, false);
                eulerOffset = OmnityHelperFunctions.Vector3InputReset("eulerOffset", eulerOffset, Vector3.zero);
                ShowDomeSelector(anOmnity);
                break;

            case OmnityMouseType.MouseFlatScreen:
                ShowFlatSelector(anOmnity);
                break;

            case OmnityMouseType.TouchFlatScreen:
                ShowFlatSelector(anOmnity);
                break;

            case OmnityMouseType.CenteredCrossHair:
                GUILayout.Label("Mouse cursor is centered");
                break;
        }
        SaveLoadGUIButtons(anOmnity);
    }

    private void ShowFlatSelector(Omnity anOmnity) {
        GUILayout.BeginHorizontal(GUI.skin.box);
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("Mapping input to final pass camera's viewport");
        for (int i = 0; i < anOmnity.finalPassCameras.Length; i++) {
            if (i == finalPassCameraIndex) {
                GUILayout.Button("[" + anOmnity.finalPassCameras[i].name + "]");
            } else {
                if (GUILayout.Button(anOmnity.finalPassCameras[i].name)) {
                    finalPassCameraIndex = i;
                }
            }
        }
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
        finalPassCameraIndex = Mathf.Clamp(finalPassCameraIndex, 0, anOmnity.finalPassCameras.Length - 1);
    }

    private void ShowDomeSelector(Omnity anOmnity) {
        GUILayout.BeginHorizontal(GUI.skin.box);
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("Mapping final pass camera's viewport");
        for (int i = 0; i < anOmnity.finalPassCameras.Length; i++) {
            if (i == finalPassCameraIndex) {
                GUILayout.Button("[" + anOmnity.finalPassCameras[i].name + "]");
            } else {
                if (GUILayout.Button(anOmnity.finalPassCameras[i].name)) {
                    finalPassCameraIndex = i;
                }
            }
        }
        GUILayout.EndVertical();
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("to screen shape's extents");
        for (int i = 0; i < anOmnity.screenShapes.Length; i++) {
            if (i == screenShapeIndex) {
                GUILayout.Button("[" + anOmnity.screenShapes[i].name + "]");
            } else {
                if (GUILayout.Button(anOmnity.screenShapes[i].name)) {
                    screenShapeIndex = i;
                }
            }
        }
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
        finalPassCameraIndex = Mathf.Clamp(finalPassCameraIndex, 0, anOmnity.finalPassCameras.Length - 1);
        screenShapeIndex = Mathf.Clamp(screenShapeIndex, 0, anOmnity.screenShapes.Length - 1);
    }

    static private Omnity myOmnityClass = null;

    private void ApplyMouseExtensExpensiveCall(Omnity anOmnity) {
        if (myRenderer == null) {
            return;
        }
        myOmnityClass = anOmnity;
        transform.localScale = Vector3.one * localSize;
        this.transform.parent = anOmnity.transform;

        if (anOmnity.pluginIDs.Contains((int)OmnityPluginsIDs.OmniMouse)) {
            myRenderer.enabled = this.enabled = true;
        } else {
            myRenderer.enabled = this.enabled = false;
        }
    }

    /*
    public Camera cameraForDome = null;
    public Camera cameraForFlat = null;
    public Rect pixelRectForDome;
    */

    // Use this for initialization
    private void Start() {
        if (myRenderer != null) {
            myRenderer.sharedMaterial.renderQueue = int.MaxValue;
        }
    }

    private void OnDrawGizmosSelected() {
        if (myRenderer != null) {
            if (Application.isPlaying) {
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(ray);
            }
        }
    }

    static public System.Func<bool> buttonHandler = new System.Func<bool>(() => {
        return false;
    });

    static public System.Func<bool> buttonDownHandler = new System.Func<bool>(() => {
        return false;
    });

    static public System.Func<bool> buttonUpHandler = new System.Func<bool>(() => {
        return false;
    });

    static private bool? _rayButtonUp = null;
    static public bool? _rayButtonDown = null;
    static public bool? _rayButton = null;
    static public Ray? _ray = null;

    private static bool ComputeNeeded() {
        if (lastUpdate != Time.frameCount) {
            _rayButtonUp = null;
            _rayButtonDown = null;
            _rayButton = null;
            _ray = null;
            lastUpdate = Time.frameCount;
            return true;
        }
        return false;
    }

    private static int lastUpdate = -1;

    static public bool rayButtonUp {
        get {
            if (ComputeNeeded() || _rayButtonUp == null) {
                switch (mouseType) {
                    case OmnityMouseType.CenteredCrossHair:
                        _rayButtonUp = buttonUpHandler();
                        break;

                    case OmnityMouseType.TouchFlatScreen:
                        goto case OmnityMouseType.MouseFlatScreen;
                    case OmnityMouseType.MouseDomeScreen:
                        goto case OmnityMouseType.MouseFlatScreen;
                    case OmnityMouseType.MouseFlatScreen:
                        _rayButtonUp = InputRouter.mouseLeftReleaseOnce;
                        break;

                    default:
                        Debug.LogError("Unknown mouse type " + mouseType);
                        _rayButtonUp = false;
                        break;
                }
            }
            return _rayButtonUp.GetValueOrDefault();
        }
    }

    static public bool rayButtonDown {
        get {
            if (ComputeNeeded() || _rayButtonDown == null) {
                switch (mouseType) {
                    case OmnityMouseType.CenteredCrossHair:
                        _rayButtonDown = buttonDownHandler();
                        break;

                    case OmnityMouseType.MouseDomeScreen:
                        goto case OmnityMouseType.MouseFlatScreen;
                    case OmnityMouseType.TouchFlatScreen:
                        goto case OmnityMouseType.MouseFlatScreen;

                    case OmnityMouseType.MouseFlatScreen:
                        _rayButtonDown = InputRouter.mouseLeftPressOnce;
                        break;

                    default:
                        Debug.LogError("Unknown mouse type " + mouseType);
                        _rayButtonDown = false;
                        break;
                }
            }

            return _rayButtonDown.GetValueOrDefault();
        }
    }

    static public bool rayButton {
        get {
            if (ComputeNeeded() || null == _rayButton) {
                switch (mouseType) {
                    case OmnityMouseType.CenteredCrossHair:
                        _rayButton = buttonHandler();
                        break;

                    case OmnityMouseType.MouseDomeScreen:
                        goto case OmnityMouseType.MouseFlatScreen;
                    case OmnityMouseType.TouchFlatScreen:
                        goto case OmnityMouseType.MouseFlatScreen;

                    case OmnityMouseType.MouseFlatScreen:
                        _rayButton = InputRouter.mouseLeftPress;
                        break;

                    default:
                        Debug.LogError("Unknown mouse type " + mouseType);
                        _rayButton = false;
                        break;
                }
            }
            return _rayButton.GetValueOrDefault();
        }
    }

    static private Ray oldRay = new Ray();

    static public Ray ray {
        get {
            if (myOmnityClass == null || singleton == null) {
                return oldRay;
            }

            if (myRenderer == null) {
                return oldRay;
            }

            if (ComputeNeeded() || null == _ray) {
                finalPassCameraIndex = Mathf.Clamp(finalPassCameraIndex, 0, myOmnityClass.finalPassCameras.Length - 1);
                screenShapeIndex = Mathf.Clamp(screenShapeIndex, 0, myOmnityClass.screenShapes.Length - 1);

                bool onCorrectDisplay;
                switch (mouseType) {
                    case OmnityMouseType.CenteredCrossHair:

                        break;

                    case OmnityMouseType.TouchFlatScreen:
                        goto case OmnityMouseType.MouseFlatScreen;
                    case OmnityMouseType.MouseFlatScreen:

                        try {
                            if (myOmnityClass.finalPassCameras.Length <= finalPassCameraIndex || finalPassCameraIndex < 0) {
                                break;
                            }
                            FinalPassCamera cameraForFlat = myOmnityClass.finalPassCameras[finalPassCameraIndex];
                            if (cameraForFlat == null) {
                                onCorrectDisplay = false;
                            } else if (Omnity.anOmnity.PluginEnabled(OmnityPluginsIDs.EasyMultiDisplay3)) {
                                if (InputRouter.mouseRemoteId == -1) {
                                    onCorrectDisplay = cameraForFlat.myCamera.pixelRect.Contains(InputRouter.mousePosition);
                                } else {
                                    onCorrectDisplay = (InputRouter.mouseRemoteId == finalPassCameraIndex);
                                }
                            } else {
                                onCorrectDisplay = cameraForFlat.myCamera.pixelRect.Contains(InputRouter.mousePosition);
                            }
                            if (onCorrectDisplay) {
                                oldRay = cameraForFlat.myCamera.ScreenPointToRay(InputRouter.mousePosition);
                            }
                            _ray = oldRay;
                        } catch {
                        }
                        break;

                    case OmnityMouseType.MouseDomeScreen:
                        if (myOmnityClass.finalPassCameras.Length <= finalPassCameraIndex || finalPassCameraIndex < 0) {
                            break;
                        }
                        FinalPassCamera cameraForDome = myOmnityClass.finalPassCameras[finalPassCameraIndex];
                        if (cameraForDome == null || cameraForDome.myCamera == null) {
                            onCorrectDisplay = false;
                        } else if (Omnity.anOmnity.PluginEnabled(OmnityPluginsIDs.EasyMultiDisplay3)) {
                            onCorrectDisplay = (InputRouter.mouseRemoteId == finalPassCameraIndex);
                        } else {
                            onCorrectDisplay = cameraForDome.myCamera.pixelRect.Contains(InputRouter.mousePosition);
                        }

                        if (onCorrectDisplay) {
#if UNITY_4_2
                                Debug.Log("Warning Omnimouse code code needs to be updated for this version of unity3d UNITY_4_2");
#elif UNITY_4_1_5
                                Debug.Log("Warning Omnimouse code code needs to be updated for this version of unity3d UNITY_4_1_5");
#elif UNITY_4_3
                                Debug.Log("Warning Omnimouse code code needs to be updated for this version of unity3d UNITY_4_3");
#elif UNITY_4_4
                                Debug.Log("Warning Omnimouse code code needs to be updated for this version of unity3d UNITY_4_4");
#elif UNITY_4_5
                                Debug.Log("Warning Omnimouse code code needs to be updated for this version of unity3d UNITY_4_5");
#else
                            Vector3 InputmousePosition = cameraForDome.myCamera.ScreenToViewportPoint(InputRouter.mousePosition);

                            float preLonOffset = 90 + singleton.eulerOffset.y;
                            float latOffset = 90 + singleton.eulerOffset.x;

                            float minLon = -180;
                            float maxLon = 180;
                            float minLat = -90;
                            float maxLat = 90;
                            if (myOmnityClass.screenShapes.Length > 0) {
                                minLon = myOmnityClass.screenShapes[screenShapeIndex].sphereParams.thetaStart * singleton.xSensitivity;
                                maxLon = myOmnityClass.screenShapes[screenShapeIndex].sphereParams.thetaEnd * singleton.xSensitivity;
                                minLat = myOmnityClass.screenShapes[screenShapeIndex].sphereParams.phiEnd * singleton.ySensitivity;
                                maxLat = myOmnityClass.screenShapes[screenShapeIndex].sphereParams.phiStart * singleton.ySensitivity;
                            }

                            float thetad = Mathf.Lerp(minLon, maxLon, InputmousePosition.x) + preLonOffset;
                            float phid = Mathf.Lerp(minLat, maxLat, InputmousePosition.y) + latOffset;

                            // OFFSET OMNITY TILT
                            phid += singleton.flipTiltCorrection ? Omnity.anOmnity.tilt : -Omnity.anOmnity.tilt;

                            float theta = thetad * Mathf.Deg2Rad;
                            float phi = phid * Mathf.Deg2Rad;
                            // CONVERT TO CARTESIAN
                            float x = -Mathf.Sin(phi) * Mathf.Cos(theta);
                            float z = Mathf.Sin(phi) * Mathf.Sin(theta);
                            float y = Mathf.Cos(phi);
                            Vector3 xyz = new Vector3(x, y, z);
                            //   Quaternion rotate = Quaternion.Euler(singleton.eulerOffset);
                            //  xyz = rotate* xyz;
                            Vector3 V = myOmnityClass.transform.TransformVector(xyz);
                            oldRay = new Ray(myOmnityClass.transform.position, V);

                            // position in front of camera

#endif
                        }

                        _ray = oldRay;

                        break;
                }

                if (_ray == null) {
                    _ray = new Ray(myOmnityClass.transform.position, singleton.transform.forward);
                }
            }
            return _ray.GetValueOrDefault();
        }
    }

    private bool IsMouseNeeded() {
        for (int i = 0; i < funcNeedSystemMouse.Count; i++) {
            if (funcNeedSystemMouse[i]()) {
                return true;
            }
        }
        return false;
    }

    public enum OmnimouseUpdatePhase {
        OnUpdate = 0,
        None = 1
    }

    // this isn't saved...  must be set in scene or through app startup
    public OmnimouseUpdatePhase omnimouseUpdatePhase = OmnimouseUpdatePhase.OnUpdate;

    // Update is called once per frame
    override public void Update() {
        base.Update();
        if (omnimouseUpdatePhase == OmnimouseUpdatePhase.OnUpdate) {
            UpdateMouse();
        }
    }

    static public bool forceHide = false;

    public void UpdateMouse() {
        if (forceHide) {
            OmnityPlatformDefines.SetCursor_visible(false);
            return;
        }

        if (Omnity.anyGuiShowing || IsMouseNeeded()) {
            OmnityPlatformDefines.SetCursor_visible(true);
            return;
        }

        if (myRenderer == null) {
            return;
        }

        switch (mouseStyle) {
            case OmnityMouseStyle.AlwaysVisible:
                OmnityPlatformDefines.SetCursor_visible(true);
                break;

            case OmnityMouseStyle.AlwaysHidden:
                if (IsMouseNeeded()) {
                    OmnityPlatformDefines.SetCursor_visible(true);
                } else {
                    OmnityPlatformDefines.SetCursor_visible(false);
                }
                break;

            default:
                OmnityPlatformDefines.SetCursor_visible(true);
                Debug.Log("UNKNOWN MOUSE STYLE ");
                Debug.Log(mouseStyle.ToString());
                break;
        }

        switch (crosshairStyle) {
            case OmnityMouseStyle.AlwaysVisible:
                myRenderer.enabled = true;
                break;

            case OmnityMouseStyle.AlwaysHidden:
                myRenderer.enabled = false;
                break;

            default:
                myRenderer.enabled = true;
                break;
        }

        Ray r = ray;
        switch (mouseType) {
            case OmnityMouseType.CenteredCrossHair:
                // no need to update the crosshair position
                transform.position = r.origin + r.direction;
                break;

            case OmnityMouseType.TouchFlatScreen:
                // no need to update the crosshair position since it should not be visible...
                transform.position = r.origin + r.direction;
                break;

            case OmnityMouseType.MouseFlatScreen:
                transform.position = r.origin + r.direction;
                break;

            case OmnityMouseType.MouseDomeScreen:
                transform.position = r.origin + r.direction;
                if (r.direction.sqrMagnitude != 0) {
                    transform.forward = transform.position - r.origin;
                }
                break;
        }

        bool lockCursor = false;
        if (lockCursor) {
            transform.localPosition = new Vector3(0, 0, 2);
            transform.localEulerAngles = new Vector3(0, 0, 0);
        }
    }

    public Vector3 eulerOffset = new Vector3(0, 0, 0);
    public bool flipTiltCorrection = false;
    public float localSize = .1f;
    public float ySensitivity = 1.0f, xSensitivity = 1.0f;
}
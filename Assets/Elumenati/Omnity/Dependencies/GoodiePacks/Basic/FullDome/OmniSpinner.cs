using UnityEngine;

public class OmniSpinner : OmnityTabbedFeature {
    static public OmnityPluginsIDs _myOmnityPluginsID = OmnityPluginsIDs.OmniSpinner;
    override public OmnityPluginsIDs myOmnityPluginsID { get { return _myOmnityPluginsID; } }

    static public OmniSpinner OmniCreateSingleton(GameObject go) {
        return GetSingleton<OmniSpinner>(ref singleton, go);
    }

    static public OmniSpinner singleton = null;
    public bool isSpinningYaw = true;
    public bool isSpinningTilt = true;
    public bool isSpinningRoll = true;
    public float minPerRevolution = 60.0f * 5.0f;

    public static OmniSpinner Get() {
        return singleton;
    }

    private void Awake() {
        if (VerifySingleton<OmniSpinner>(ref singleton)) {
            Omnity.onReloadEndCallbackPriority.AddLoaderFunction(PriorityEventHandler.Ordering.Order_OmniSpinner, CoroutineLoader);
            Omnity.onSaveCompleteCallback += Save;
        }
    }

    // Use this for initialization
    private void Start() {
    }

    override public void Update() {
        if (null != Omnity.anOmnity) {
            if (minPerRevolution != 0 && (isSpinningYaw || isSpinningTilt || isSpinningRoll)) {
                float deltaDegrees = Time.deltaTime / (minPerRevolution * 60f);
                if (isSpinningYaw) {
                    Omnity.anOmnity.yaw = Mathf.Repeat(Omnity.anOmnity.yaw + deltaDegrees * 360.0f, 360f);
                }
                if (isSpinningTilt) {
                    Omnity.anOmnity.tilt = Mathf.Repeat(Omnity.anOmnity.tilt + deltaDegrees * 360.0f, 360f);
                }
                if (isSpinningRoll) {
                    Omnity.anOmnity.roll = Mathf.Repeat(Omnity.anOmnity.roll + deltaDegrees * 360.0f, 360f);
                }
                Omnity.anOmnity.RefreshTilt();
            }
        }
    }

    override public string BaseName {
        get {
            return "OmniSpinner";
        }
    }

    /// <param name="xmlWriter">The XML writer.</param>
    override public void WriteXMLDelegate(System.Xml.XmlTextWriter xmlWriter) {
        xmlWriter.WriteElementString("isSpinning", isSpinningYaw.ToString());
        xmlWriter.WriteElementString("isSpinningTilt", isSpinningTilt.ToString());
        xmlWriter.WriteElementString("isSpinningRoll", isSpinningRoll.ToString());
        xmlWriter.WriteElementString("minPerRevolution", minPerRevolution.ToString());
    }

    override public void ReadXMLDelegate(System.Xml.XPath.XPathNavigator nav) {
        isSpinningYaw = OmnityHelperFunctions.ReadElementBoolDefault(nav, ".//isSpinning", true);
        isSpinningTilt = OmnityHelperFunctions.ReadElementBoolDefault(nav, ".//isSpinningTilt", false);
        isSpinningRoll = OmnityHelperFunctions.ReadElementBoolDefault(nav, ".//isSpinningRoll", false);
        minPerRevolution = OmnityHelperFunctions.ReadElementFloatDefault(nav, ".//minPerRevolution", 5.0f);
    }

    public override void MyGuiCallback(Omnity anOmnity) {
        if (!anOmnity.PluginEnabled(myOmnityPluginsID)) {
            return;
        }
        isSpinningYaw = OmnityHelperFunctions.BoolInputReset("isSpinning Yaw", isSpinningYaw, true);
        isSpinningTilt = OmnityHelperFunctions.BoolInputReset("isSpinning Tilt", isSpinningTilt, false);
        isSpinningRoll = OmnityHelperFunctions.BoolInputReset("isSpinning Roll", isSpinningRoll, false);
        minPerRevolution = OmnityHelperFunctions.FloatInputReset("minPerRevolution", minPerRevolution, 5.0f);
        SaveLoadGUIButtons(anOmnity);
    }
}
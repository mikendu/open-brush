using UnityEngine;

public class ExampleViosoTextureScript : MonoBehaviour {
#if UNITY_STANDALONE_WIN

    [System.NonSerialized]
    public Texture textureFromApp = null;

    [System.NonSerialized]
    public bool textureFlipFromApp = false;

    private Texture getTextureFromApp() {
        return textureFromApp;
    }

    private bool getTextureFlipFromApp() {
        return textureFlipFromApp;
    }

    // Use this for initialization
    private void Start() {
        OmniVioso.getTextureFromApp = getTextureFromApp;
        OmniVioso.getTextureFlipFromApp = getTextureFlipFromApp;
        OmniVioso.Get().onNewTextureCallback();
    }

    // Update is called once per frame
    private void Update() {
    }

#endif
}
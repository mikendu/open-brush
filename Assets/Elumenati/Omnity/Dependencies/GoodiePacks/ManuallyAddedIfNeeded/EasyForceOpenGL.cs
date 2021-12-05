//////////////////////////////////////////////////////////////////////////////////////////////////
// Easy Force OpenGL for Windows Standalone
// By Clement Shimizu, PhD. of the Elumenati
// 2/20/2012 : Version 1.1
//////////////////////////////////////////////////////////////////////////////////////////////////
//
// WHAT
// This simple script ensures that windows stand alone unity projects run in OpenGL mode.
// It provides a reminder if the Unity Editor is run in DirectX mode.  This has no effect
// on IOS, Mac, Flash, or any other platform.
//
// To use it, just attach the script to any game object in the first loaded scene.
//
// WHY
// Some Unity3D pro plugins require OpenGL mode, which requires the user to run with an inconvenient
// command line parameter.  This script automatically runs the application in OpenGL mode without
// having to remember to add the command line parameter
//
// NOTES
// The script works by re-writing the command line parameters and restarting the app if needed.
// If you game uses Unity's Display Resolution Dialog, please note that it will cause the
// dialog box to repeat itself one time.  For better results, set the Display Resolution
// Dialog box to disabled, or hidden by default.
//
// SUPPORT
// For help, email clemshim@gmail.com with the subject line EasyForceOpenGL Version 1 Help
// Be sure to include Unity Version, platform, and any other details.
//
// History
// 2/20/2012 : Version 1.1 submitted to asset store. Updated licence and tested 3.5 Windows Standalone.
// 2/4/2012 : Version 1.0 submitted to asset store. Built for Unity 3.4 Windows Standalone.
//
//////////////////////////////////////////////////////////////////////////////////////////////////

using UnityEngine;

public class EasyForceOpenGL : MonoBehaviour {
    public bool forceOpenGL = true;
    public float wait = 0;
#if UNITY_STANDALONE_WIN // This script is only for Unity3D windows standalone

    private void Awake() {
        if (!enabled) {
            return;
        }
        if (isDirectX()) {
            if (Application.isEditor) {
                Debug.Log("You are running the editor in DirectX mode.  To run the editor in OpenGL mode, use the command line option -Force-Opengl when starting the Unity editor.");
                Debug.Log("Create an OpenGL Unity Editor shortcut using the following shortcut target\n\"C:\\Program Files (x86)\\Unity\\Editor 34\\Unity.exe\" -Force-Opengl -projectpath c:\\\nnote that you may have a different install path.");
                // "C:\Program Files (x86)\Unity\Editor 34\Unity.exe" -Force-Opengl -projectpath c:\
            } else {
                StartCoroutine(Restart());
            }
        } else {
            Debug.Log("Running in OpenGL mode");
        }
    }

    private System.Collections.IEnumerator Restart() {
        if (wait > 0) {
            yield return new WaitForSeconds(wait);
        }
        Debug.Log("Running in DirectX mode... attempting to restart in OpenGL mode");
        string[] commandlineargs = System.Environment.GetCommandLineArgs();  // find out what command line args were used to start the app
        string commandlineApp = commandlineargs[0];                          // get the shortcut to the application executable
        commandlineargs[0] = "";                                             // Remove the app from the rest of command line args
        string newCommandLineArgs = string.Join(" ", commandlineargs) + "-force-opengl"; // create the updated command line args
        System.Diagnostics.Process.Start(commandlineApp, newCommandLineArgs); // run the app as an OpenGL instance
        Omnity.SoftQuit(); // Exit out of this directX instance
    }

#else
    void Awake(){
    }
#endif

    private void Start() {
    }

    static private int _isDirectX = -1; // -1 means uninitalized varible, 0 means opengl, 1 means directx

    public static bool isDirectX() {
        if (_isDirectX == -1) {
            _isDirectX = SystemInfo.graphicsDeviceVersion.ToLower().Contains("direct") ? 1 : 0;
        }
        return (_isDirectX == 1);
    }

    public static bool isOpenGL() {
        return !isDirectX();
    }
}
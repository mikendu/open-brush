using UnityEditor;
using UnityEngine;

#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX

public class OmnityEditor : EditorWindow {

    private class MyAllPostprocessor : AssetPostprocessor {

        private static System.Collections.Generic.List<string> assetstowatch = new System.Collections.Generic.List<string> {
            "NFDistorterInterface_DLL_32","NFDistorterInterface_DLL_64","libNFDistorterInterface_dylib","ViosoEngine_dll","VIOSOWarpBlend32_dll","calibration.ini","ViosoEngine_dll","VIOSOWarpBlend64_dll"
        };

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
            if (!System.IO.Directory.Exists(OmnityMono.Navegar.ViosoEngine.pathToDLLs64) ||
                !System.IO.Directory.Exists(cobra64PathDest)) {
                InstallStreamingAssets();
                return;
            }
            foreach (var str in importedAssets) {
                foreach (var contain in assetstowatch) {
                    if (str.Contains(contain)) {
                        InstallStreamingAssets();
                        return;
                    }
                }
            }
        }
    }

    [MenuItem("Elumenati/Install Streaming Assets")]
    private static void InstallStreamingAssets() {
        InstallStreamingAssets1();
        InstallStreamingAssets2();
    }

#if UNITY_EDITOR
    static private string cobra64PathDest { get { return Application.streamingAssetsPath + "/cobra/macos"; } }
#endif

    private static void InstallStreamingAssets1() {
        string[] paths = new string[] {
            Application.streamingAssetsPath,
            Application.streamingAssetsPath+"/cobra",
            Application.streamingAssetsPath+"/cobra/32bits",
            Application.streamingAssetsPath+"/cobra/64bits",
            Application.streamingAssetsPath+"/cobra/macos",
        };
        string[] filesFrom = new string[] {
            "Assets/Elumenati/Omnity/Dependencies/Editor/StreamingAssetsToInstall/NFDistorterInterface_DLL_32",
            "Assets/Elumenati/Omnity/Dependencies/Editor/StreamingAssetsToInstall/NFDistorterInterface_DLL_64",
            "Assets/Elumenati/Omnity/Dependencies/Editor/StreamingAssetsToInstall/libNFDistorterInterface_dylib",
        };
        string[] filesTo = new string[] {
            Application.streamingAssetsPath+"/cobra/32bits/NFDistorterInterface.dll",
            Application.streamingAssetsPath+"/cobra/64bits/NFDistorterInterface.dll",
            Application.streamingAssetsPath+"/cobra/macos/libNFDistorterInterface.dylib"
        };
        foreach (var p in paths) {
            if (!System.IO.Directory.Exists(p)) {
                System.IO.Directory.CreateDirectory(p);
            }
        }
        bool allGood = true;
        string errror = "";
        bool actuallyCopiedSomething = false;
        for (int i = 0; i < filesFrom.Length; i++) {
            var f = filesFrom[i];
            var t = filesTo[i];
            if (System.IO.File.Exists(f)) {
                try {
                    if (CopySkipIfEqual(f, t)) {
                        actuallyCopiedSomething = true;
                    }
                } catch (System.Exception e) {
                    errror += e.Message + System.Environment.NewLine;
                    allGood = false;
                }
            } else {
                allGood = false;
                Debug.LogError("Source file " + f + " does not exist.  Copy failed.  Make sure that Omnity is installed in Project Folder/Assets/Elumenati/Omnity");
            }
        }
        if (allGood) {
            if (actuallyCopiedSomething) {
                Debug.Log("<color=green>Copied NFDistorterInterface into streaming assets</color>");
            }
        } else {
            Debug.LogError("Installing streaming assets failed, you will need to restart unity and select Elumenati->Install Straming assets. Locked file. " + errror);
        }
    }

    private static string DllNamePackaged = "ViosoEngine_dll";

    private static string pathToDLL_Packaged_32 {
        get {
            return "Assets/Elumenati/Omnity/Dependencies/Editor/StreamingAssetsToInstall/DLLs/32bits";
        }
    }

    private static string pathToDLL_Packaged_64 {
        get {
            return "Assets/Elumenati/Omnity/Dependencies/Editor/StreamingAssetsToInstall/DLLs/64bits";
        }
    }

    private static void InstallStreamingAssets2() {
        bool actuallyCopiedSomething = false;
        bool allGood = true;
        try {
            System.IO.Directory.CreateDirectory(UnityEngine.Application.streamingAssetsPath);
            System.IO.Directory.CreateDirectory(OmnityMono.Navegar.ViosoEngine.pathToDLLs);
            System.IO.Directory.CreateDirectory(OmnityMono.Navegar.ViosoEngine.pathToDLLs32);
            System.IO.Directory.CreateDirectory(OmnityMono.Navegar.ViosoEngine.pathToDLLs64);
        } catch {
            allGood = false;
        }
        string iniFIleWithExtensionSource = pathToDLL_Packaged_64 + "/calibration.ini";
        string iniFIleWithExtensionDest32 = System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "Vioso/32bits/calibration.ini");
        string iniFIleWithExtensionDest64 = System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "Vioso/64bits/calibration.ini");
        try {
            if (CopySkipIfEqual(pathToDLL_Packaged_32 + "/" + DllNamePackaged, OmnityMono.Navegar.ViosoEngine.pathToDLLs32 + "/" + OmnityMono.Navegar.ViosoEngine.DllName)) {
                actuallyCopiedSomething = true;
            }
            if (CopySkipIfEqual(pathToDLL_Packaged_32 + "/" + "VIOSOWarpBlend32_dll", OmnityMono.Navegar.ViosoEngine.pathToDLLs32 + "/" + "VIOSOWarpBlend32.dll")) {
                actuallyCopiedSomething = true;
            }
        } catch (System.Exception e) {
            UnityEngine.Debug.LogError(e.Message);
            allGood = false;
        }
        try {
            if (CopySkipIfEqual(pathToDLL_Packaged_64 + "/" + DllNamePackaged, OmnityMono.Navegar.ViosoEngine.pathToDLLs64 + "/" + OmnityMono.Navegar.ViosoEngine.DllName)) {
                actuallyCopiedSomething = true;
            }
            if (CopySkipIfEqual(pathToDLL_Packaged_64 + "/" + "VIOSOWarpBlend64_dll", OmnityMono.Navegar.ViosoEngine.pathToDLLs64 + "/" + "VIOSOWarpBlend64.dll")) {
                actuallyCopiedSomething = true;
            }
        } catch (System.Exception e) {
            UnityEngine.Debug.LogError(e.Message);
            allGood = false;
        }
        try {
            if (CopySkipIfEqual(iniFIleWithExtensionSource, iniFIleWithExtensionDest32)) {
                actuallyCopiedSomething = true;
            }
        } catch (System.Exception e) {
            UnityEngine.Debug.LogError("Error could not copy ini file " + e.Message);
            allGood = false;
        }
        try {
            if (CopySkipIfEqual(iniFIleWithExtensionSource, iniFIleWithExtensionDest64)) {
                actuallyCopiedSomething = true;
            }
        } catch (System.Exception e) {
            UnityEngine.Debug.LogError("Error could not copy ini file " + e.Message);
            allGood = false;
        }

        if (allGood) {
            if (actuallyCopiedSomething) {
                Debug.Log("<color=green>Copied StreamingAssets2 succeeded</color>");
            }
        } else {
            Debug.LogWarning("<color=red>Installing streaming assets failed, you will need to restart unity and select Elumenati->Install Straming assets.</color>");
        }
    }

    [MenuItem("Elumenati/Rename Layers")]
    private static void OpenTagsEditorWindow() {
        RenameLayer(30, "Final Pass Layer");
        RenameLayer(31, "Final Pass Layer (alt)");
    }

    static public void RenameLayer(int layerIndex, string layerName) {
        try {
            SerializedObject myTagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty myLayers = myTagManager.FindProperty("layers");
            SerializedProperty myUserLayer = myTagManager.FindProperty("User Layer " + layerIndex.ToString());
            if (myUserLayer != null) {
                myUserLayer.stringValue = layerName;
            } else {
                myUserLayer = myLayers.GetArrayElementAtIndex(layerIndex);
            }
            if (myUserLayer != null) {
                myUserLayer.stringValue = layerName;
            }
            myTagManager.ApplyModifiedProperties();
        } catch (System.Exception e) {
            Debug.LogError(e.Message);
            Debug.LogError("Error renaming layer " + layerIndex.ToString() + " to " + layerName);
            Debug.LogError("You should manually do it.");
        }
    }

    private static bool CopySkipIfEqual(string first, string second) {
        if (!FilesAreEqual(first, second)) {
            System.IO.File.Copy(first, second, true);
            return true;
        } else {
            return false;
        }
    }

    // from http://stackoverflow.com/questions/1358510/how-to-compare-2-files-fast-using-net
    private static bool FilesAreEqual(string first, string second) {
        var v1 = new System.IO.FileInfo(first);
        var v2 = new System.IO.FileInfo(second);
        if (!v1.Exists) {
            throw new System.Exception("file " + first + " does not exist");
        }
        if (!v2.Exists) {
            return false;
        }
        var equal = FilesAreEqual(v1, v2);
        return equal;
    }

    private static bool FilesAreEqual(System.IO.FileInfo first, System.IO.FileInfo second) {
        if (first.Length != second.Length)
            return false;
        const int BYTES_TO_READ = sizeof(System.Int64);

        int iterations = (int)System.Math.Ceiling((double)first.Length / BYTES_TO_READ);

        using (System.IO.FileStream fs1 = first.OpenRead())
        using (System.IO.FileStream fs2 = second.OpenRead()) {
            byte[] one = new byte[BYTES_TO_READ];
            byte[] two = new byte[BYTES_TO_READ];

            for (int i = 0; i < iterations; i++) {
                fs1.Read(one, 0, BYTES_TO_READ);
                fs2.Read(two, 0, BYTES_TO_READ);

                if (System.BitConverter.ToInt64(one, 0) != System.BitConverter.ToInt64(two, 0))
                    return false;
            }
        }

        return true;
    }
}

#endif
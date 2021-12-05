//////////////////////////////////////
// HeartBeat.js
// By Clement Shimizu for the Elumenati
//
// This loop CoroutineUpdate touches the HeartBeatFile by updates the file a CreationTime, LastWriteTime and LastAccessTime.
// if the program freezes or crashes the file will become out of date.
//  The heartbeat monitor will detect it and restart the program.
// In order to exit the program gracefully you must call GracefulExit.
//
// This will not work in the webplayer.
//
//////////////////////////////////////

#undef UNITY_EDITOR

using UnityEngine;

#if UNITY_STANDALONE_WIN

using System.Linq;

#endif

public class HeartRateMonitor : OmnityAutoAddPlugin {
    static public OmnityPluginsIDs _myOmnityPluginsID = OmnityPluginsIDs.HeartRateMonitor;
    override public OmnityPluginsIDs myOmnityPluginsID { get { return _myOmnityPluginsID; } }

    static public bool isHeartRateMonitorEnabled {
        get {
            return singleton != null && singleton.enabled;
        }
    }

    static private HeartRateMonitor singleton = null;

    static public HeartRateMonitor OmniCreateSingleton(GameObject go) {
        var s = GetSingleton<HeartRateMonitor>(ref singleton, go);
        if (s != null) {
            s.Init();
        }
        return s;
    }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

    private void Awake() {
        Init();
    }

    private bool? foundHRM = null;

    private void Init() {
        if (foundHRM != null && singleton == this) {
            return;
        }

        foundHRM = IsRunningByFilenameNamesCaseInsensitive(HeartRateMonitor.potentiaHeartRateMonitorNames);
        if (!foundHRM.GetValueOrDefault()) {
            this.enabled = false;
            return;
        } else {
            this.enabled = true;
        }
        if (singleton == null) {
            singleton = this;
        } else if (singleton != this) {
            Debug.Log("Heart rate monitor singleton exists. Disabling this instance.");
            this.enabled = false;
            return;
        }
        if (!System.IO.File.Exists(HeartBeatFilename)) {
            System.IO.FileStream fs = System.IO.File.Create(HeartBeatFilename);
            fs.Close();
        }
        try {
            System.IO.File.Delete(SuicideFilename);
        } catch (System.Exception e) {
            Debug.Log("Exception " + e.Message);
        }
        try {
            System.IO.File.Delete(RestartFilename);
        } catch (System.Exception e) {
            Debug.Log("Exception " + e.Message);
        }

        heartBeatFileInfo = new System.IO.FileInfo(HeartBeatFilename); // maybe better to do this once on startup after file is created.
    }

    //#if  UNITY_EDITOR
    static public System.Collections.Generic.List<string> potentiaHeartRateMonitorNames = new System.Collections.Generic.List<string> {
        "UnityHeartbeatMonitor",
        "UnityHeartRateMonitor",
        "HeartbeatMonitor",
        "HeartRateMonitor"
    };

    private static float HeartBeatIntervalInSec = .5f; // .5 will be approximately twice a second.  HOWEVER THERE MAY DROPS DURING ASSET LOADING
    private static string HeartBeatFilename = "heartbeat.txt";
    private static string SuicideFilename = "suicide.txt";
    private static string RestartFilename = "restart.txt";

    private void Start() {
        Init();
    }

    private System.IO.FileInfo heartBeatFileInfo = null;

    ////////////////////////////////////////////////////////////////////////
    // this shuts down the program in a graceful way.
    // it creates the SuicideFile in order to signify to the (heartbeat monitor)
    ////////////////////////////////////////////////////////////////////////
    static public void GracefulExit() {
        KillSelfAndHRM();

        Application.Quit();
        try {
            System.Diagnostics.Process currentProcess = System.Diagnostics.Process.GetCurrentProcess();
            currentProcess.Kill();
        } catch (System.Exception e) {
            print("Exception " + e.Message);
        }
    }

    static public void KillSelfAndHRM() {
        try {
            System.IO.FileStream fs = System.IO.File.Create(SuicideFilename);
            fs.Close();
        } catch (System.Exception e) {
            Debug.Log("Exception " + e.Message);
        }
    }

    ////////////////////////////////////////////////////////////////////////
    // this reboots the program
    // the heartbeat monitor will dect the program has stopped and it will re-start it.
    ////////////////////////////////////////////////////////////////////////
    static public void ForceReboot() {
        try {
            System.IO.FileStream fs = System.IO.File.Create(RestartFilename);
            fs.Close();
        } catch (System.Exception e) {
            print("Exception " + e.Message);
        }

        try {
            System.Diagnostics.Process currentProcess = System.Diagnostics.Process.GetCurrentProcess();
            currentProcess.Kill();
        } catch (System.Exception e) {
            print("Exception " + e.Message);
        }
    }

    ////////////////////////////////////////////////////////////////////////
    // This loop CoroutineUpdate touches the HeartBeatFile by updates
    // the file a CreationTime, LastWriteTime and LastAccessTime.
    // if the program freezes or crashes the file will become out of date.
    //  The heartbeat monitor will detect it and restart the program.
    //
    // The loop repeats every HeartBeatIntervalInSec.
    // THIS MAY NOT EACTLY CONSISENT (IE THE FRAMERATE SLOWS DOWN OR A LEVEL LOAD )
    ////////////////////////////////////////////////////////////////////////

    private float lastTouch = 0;

    private void Update() {
        if (foundHRM == null) {
            Debug.LogError("SINGLETON NULL THIS SHOULD BE INITED");
            Init();
            if (!enabled) {
                return;
            }
        } else if (!foundHRM.GetValueOrDefault()) {
            enabled = false;
            return;
        }

        bool updateNow = false;
        if (lastTouch == 0 && Time.realtimeSinceStartup != 0) {
            updateNow = true;
        }
        if (Time.realtimeSinceStartup - lastTouch > HeartBeatIntervalInSec) {
            updateNow = true;
        }

        if (updateNow) {
            lastTouch = Time.realtimeSinceStartup;
            if (!touchFile(heartBeatFileInfo)) {
                Debug.LogError("ERRROR heart beat stopping");
                enabled = false;
            }
        }
    }

    ///////////////////////////////////////////////////////////////////////
    // touches the HeartBeatFile by updates the file a CreationTime, LastWriteTime and LastAccessTime.
    ///////////////////////////////////////////////////////////////////////
    private static bool touchFile(System.IO.FileSystemInfo fsi) {
        try {
            // Update the CreationTime, LastWriteTime and LastAccessTime.
            fsi.CreationTime = fsi.LastWriteTime = fsi.LastAccessTime = System.DateTime.Now;
            return true;
        } catch (System.Exception e) {
            print("Exception " + e.Message);
            return false;
        }
    }

    static private string CleanInput(string strIn) {
        try {
            return System.Text.RegularExpressions.Regex.Replace(strIn, @"[^a-zA-Z]", "", System.Text.RegularExpressions.RegexOptions.None);
        } catch {
            return strIn;
        }
    }

    static public bool IsRunningByFilenameNamesCaseInsensitive(System.Collections.Generic.List<string> names, bool mustBeInSameDirectory = true) {
        //NOTE: GetProcessByName() doesn't seem to work on Win7
        //Process[] running = Process.GetProcessesByName("notepad");
        try {
            System.Diagnostics.Process[] running = System.Diagnostics.Process.GetProcesses();
            foreach (System.Diagnostics.Process process in running) {
                try {
                    string pname = CleanInput(process.ProcessName);
                    if (names.Contains(pname, System.StringComparer.OrdinalIgnoreCase)) {
                        if (mustBeInSameDirectory) {
                            var medir = NormalizePath(System.IO.Path.Combine(Application.dataPath, "../"));
                            var testdir = NormalizePath(System.IO.Path.GetDirectoryName(process.MainModule.FileName));
                            return medir == testdir; // careful about this not being exactly equal.
                        } else {
                            return true;
                        }
                    }
                } catch {
                }
            }
        } catch {
        }
        return false;
    }

#else
    void Init() {
    }
    static public void GracefulExit() {
    }

    static public void KillSelfAndHRM() {
        Application.Quit();
    }

    static public void ForceReboot() {
        Application.Quit();
    }

#endif

    public static string NormalizePath(string path) {
        return System.IO.Path.GetFullPath(new System.Uri(path).LocalPath)
                   .TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar)
                   .ToUpperInvariant();
    }
}
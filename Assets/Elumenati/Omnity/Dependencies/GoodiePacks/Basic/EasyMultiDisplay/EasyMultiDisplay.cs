using UnityEngine;
using System.Collections;

#if UNITY_STANDALONE_WIN

using System.Runtime.InteropServices;

#endif
// Easy Multi Display
// by Dr. Clement Shimizu for the Elumenati
// NOTE Extensive use of code from the Pinvoke project

[System.Serializable]
public class EasyMultiDisplay : OmnityAutoAddPlugin {
    static public OmnityPluginsIDs _myOmnityPluginsID = OmnityPluginsIDs.EasyMultiDisplay;
    override public OmnityPluginsIDs myOmnityPluginsID { get { return _myOmnityPluginsID; } }

    static public EasyMultiDisplay OmniCreateSingleton(GameObject go) {
        return GetSingleton<EasyMultiDisplay>(ref singleton, go);
    }

    private void Awake() {
        if (singleton == null) {
            singleton = this;
        } else if (singleton != this) {
            Debug.LogError("ERROR THERE SHOULD ONLY BE ONE of " + this.GetType().ToString() + " per scene");
            enabled = false;
            return;
        }
        Omnity.onResizeWindowFunctionCallback = TrySetPositionAndResolutionCallback;
    }

    static private EasyMultiDisplay singleton = null;

    static public EasyMultiDisplay Get() {
        return singleton;
    }

    public IEnumerator CoroutineSetPositionResHelper(Rect Screen_PositionAndResolution_PositionAndResolution, bool topMost, bool bordered) {
        if (Application.isEditor) {
            yield break;
        }
        if (Screen.fullScreen) {
            Screen.SetResolution((int)Screen_PositionAndResolution_PositionAndResolution.width - 1, (int)Screen_PositionAndResolution_PositionAndResolution.height - 1, false);
            yield return null;
        }
#if UNITY_STANDALONE_WIN
        yield return new WaitForEndOfFrame();
        OmnityWindowsExtensions.SetBorderlessWindowPositionResolution(OmnityWindowsExtensions.FindWindowByAlt(), Screen_PositionAndResolution_PositionAndResolution, topMost, bordered);
        yield return new WaitForSeconds(1);
        OmnityWindowsExtensions.SetBorderlessWindowPositionResolution(OmnityWindowsExtensions.FindWindowByAlt(), Screen_PositionAndResolution_PositionAndResolution, topMost, bordered);

        OmnityWindowsExtensions.RefreshDesktop();
#else
         yield break;
#endif
    }

    public void TrySetPositionAndResolutionCallback(Omnity omnity) {
        try {
            if (Application.isEditor || Omnity.anOmnity.PluginEnabled(OmnityPluginsIDs.EasyMultiDisplay3)) {
                return;
            }

            Rect goalWindowPosAndRes = omnity.windowInfo.fullscreenInfo.goalWindowPosAndRes;
            switch (omnity.windowInfo.fullscreenInfo.fullScreenMode) {
                case OmnityFullScreenMode.ApplicationDefault:

                    break;

                case OmnityFullScreenMode.WindowedWithBorder:
                    StopAllCoroutines();
                    try {
                        StartCoroutine(CoroutineSetPositionResHelper(omnity.windowInfo.fullscreenInfo.goalWindowPosAndRes, false, true));
                    } catch {
                    }

                    break;

                case OmnityFullScreenMode.FullScreenUnity:
                    if (omnity.windowInfo.fullscreenInfo.goalRefreshRate > 9) {
                        Screen.SetResolution((int)goalWindowPosAndRes.width, (int)goalWindowPosAndRes.height, true, omnity.windowInfo.fullscreenInfo.goalRefreshRate);
                    } else {
                        Screen.SetResolution((int)goalWindowPosAndRes.width, (int)goalWindowPosAndRes.height, true);
                    }
                    break;

                case OmnityFullScreenMode.Borderless:
                    StopAllCoroutines();
                    try {
                        StartCoroutine(CoroutineSetPositionResHelper(omnity.windowInfo.fullscreenInfo.goalWindowPosAndRes, false, false));
                    } catch {
                    }
                    break;

                case OmnityFullScreenMode.BorderlessTopMost:
                    StopAllCoroutines();
                    try {
                        StartCoroutine(CoroutineSetPositionResHelper(omnity.windowInfo.fullscreenInfo.goalWindowPosAndRes, true, false));
                    } catch {
                    }
                    break;

                default:
                    throw new System.Exception("Unknown fullScreenMode " + omnity.windowInfo.fullscreenInfo.fullScreenMode.ToString());
            }
        } catch (System.Exception e) {
            Debug.LogError("EasyMultiDisplay error " + e.Message);
        }
    }

    public void Start() {
    }

    static public void Kill() {
        if (Application.isEditor || Omnity.anOmnity.PluginEnabled(OmnityPluginsIDs.EasyMultiDisplay3)) {
        } else {
#if UNITY_STANDALONE_WIN
            OmnityWindowsExtensions.Kill(OmnityWindowsExtensions.FindWindowByAlt());
            System.Diagnostics.Process currentProcess = System.Diagnostics.Process.GetCurrentProcess();
            Debug.Log("trying to kill");
            print(currentProcess.ProcessName);
            currentProcess.Kill();
            /*print(currentProcess.ProcessName);
            System.Diagnostics.Process[] localByName = System.Diagnostics.Process.GetProcessesByName(currentProcess.ProcessName);
            print(localByName.Length);
            Debug.Log("trying to kill3");
            if(localByName.Length > 1){
                foreach (var lbn in localByName) {
                    lbn.Kill();
                }
            }*/

#else
#endif
            Omnity.SoftQuit();
        }
    }
}

/// <summary>
/// Some Functions specific to windows
/// </summary>
#if UNITY_STANDALONE_WIN

static public partial class OmnityWindowsExtensions {

    #region MODIFY_WINDOW

    [DllImport("user32.dll")]
    public static extern System.IntPtr SetWindowLong(System.IntPtr hwnd, int _nIndex, uint dwNewLong);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(System.IntPtr hWnd, int hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(System.IntPtr hWnd, out RECT lpRect);

    public static RECT GetClientRect(System.IntPtr hWnd) {
        RECT result;
        GetClientRect(hWnd, out result);
        return result;
    }

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(System.IntPtr hWnd);

    static private void SetBorderless(System.IntPtr hWnd) {
        SetWindowLong(hWnd, GWL_STYLE, WS_BORDER);
        Debug.Log("SetWindowLong(hWnd, GWL_STYLE, WS_BORDER);");
        SetWindowLong(hWnd, GWL_STYLE, WS_BORDER);
    }

    public static void SetBorderedWindow(System.IntPtr hWnd) {
        if (hWnd != System.IntPtr.Zero) {
            Debug.Log("SetWindowLong(hWnd, GWL_STYLE, WS_OVERLAPPEDWINDOW);");
            SetWindowLong(hWnd, GWL_STYLE, WS_OVERLAPPEDWINDOW);
            SetWindowLong(hWnd, GWL_STYLE, WS_OVERLAPPEDWINDOW);
        } else {
            Debug.LogError("winID == null");
        }
    }

    public static void SetBorderlessWindowPositionResolution(System.IntPtr winID, Rect Screen_PositionAndResolution_PositionAndResolution, bool topMost, bool bordered) {
        if (winID != System.IntPtr.Zero) {
            if (bordered) {
                SetBorderedWindow(winID);
            } else {
                SetBorderless(winID);
            }
            int layer = HWND_NOTOPMOST;
            if (topMost) {
                layer = HWND_TOPMOST;
            }
            SetWindowPos(winID, layer, (int)Screen_PositionAndResolution_PositionAndResolution.x, (int)Screen_PositionAndResolution_PositionAndResolution.y, (int)Screen_PositionAndResolution_PositionAndResolution.width, (int)Screen_PositionAndResolution_PositionAndResolution.height, OmnityWindowsExtensions.SWP_SHOWWINDOW);
        } else {
            Debug.LogError("winID == null");
        }
    }

    public static void SetWindow_FrameChanged(System.IntPtr winID) {
        if (winID != System.IntPtr.Zero) {
            SetWindowPos(winID, 0, 0, 0, 0, 0, SWP_NOZORDER | SWP_NOMOVE | SWP_NOSIZE | SWP_FRAMECHANGED);
        } else {
            Debug.LogError("winID == null");
        }
    }

#if UNITY_STANDALONE_WIN

    private enum ShowWindowCommands {

        /// <summary>
        /// Hides the window and activates another window.
        /// </summary>
        Hide = 0,

        /// <summary>
        /// Activates and displays a window. If the window is minimized or
        /// maximized, the system restores it to its original size and position.
        /// An application should specify this flag when displaying the window
        /// for the first time.
        /// </summary>
        Normal = 1,

        /// <summary>
        /// Activates the window and displays it as a minimized window.
        /// </summary>
        ShowMinimized = 2,

        /// <summary>
        /// Maximizes the specified window.
        /// </summary>
        Maximize = 3, // is this the right value?

        /// <summary>
        /// Activates the window and displays it as a maximized window.
        /// </summary>
        ShowMaximized = 3,

        /// <summary>
        /// Displays a window in its most recent size and position. This value
        /// is similar to <see cref="Win32.ShowWindowCommand.Normal"/>, except
        /// the window is not activated.
        /// </summary>
        ShowNoActivate = 4,

        /// <summary>
        /// Activates the window and displays it in its current size and position.
        /// </summary>
        Show = 5,

        /// <summary>
        /// Minimizes the specified window and activates the next top-level
        /// window in the Z order.
        /// </summary>
        Minimize = 6,

        /// <summary>
        /// Displays the window as a minimized window. This value is similar to
        /// <see cref="Win32.ShowWindowCommand.ShowMinimized"/>, except the
        /// window is not activated.
        /// </summary>
        ShowMinNoActive = 7,

        /// <summary>
        /// Displays the window in its current size and position. This value is
        /// similar to <see cref="Win32.ShowWindowCommand.Show"/>, except the
        /// window is not activated.
        /// </summary>
        ShowNA = 8,

        /// <summary>
        /// Activates and displays the window. If the window is minimized or
        /// maximized, the system restores it to its original size and position.
        /// An application should specify this flag when restoring a minimized window.
        /// </summary>
        Restore = 9,

        /// <summary>
        /// Sets the show state based on the SW_* value specified in the
        /// STARTUPINFO structure passed to the CreateProcess function by the
        /// program that started the application.
        /// </summary>
        ShowDefault = 10,

        /// <summary>
        ///  <b>Windows 2000/XP:</b> Minimizes a window, even if the thread
        /// that owns the window is not responding. This flag should only be
        /// used when minimizing windows from a different thread.
        /// </summary>
        ForceMinimize = 11
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(System.IntPtr hWnd, ShowWindowCommands nCmdShow);

#endif

    public static void TempLowerWindow(System.IntPtr winID, System.Action a) {
        if (Application.isEditor
            &&
            winID != System.IntPtr.Zero
            ) {
            a();
        } else {
            ShowWindow(winID, ShowWindowCommands.ForceMinimize);
            a();
            ShowWindow(winID, ShowWindowCommands.Restore);
        }
    }

    static public void Kill(System.IntPtr WindowHandleID) {
        if (WindowHandleID != System.IntPtr.Zero) {
            DestroyWindow(WindowHandleID);
        }
    }

    private const uint SWP_SHOWWINDOW = 0x0040; // needed

    private const uint SWP_ASYNCWINDOWPOS = 0x4000;
    private const uint SWP_DEFERERASE = 0x2000;
    private const uint SWP_DRAWFRAME = SWP_FRAMECHANGED;
    private const uint SWP_FRAMECHANGED = 0x20;
    private const uint SWP_HIDEWINDOW = 0x80;
    private const uint SWP_NOACTIVATE = 0x10;
    private const uint SWP_NOCOPYBITS = 0x100;
    private const uint SWP_NOMOVE = 0x2;
    private const uint SWP_NOOWNERZORDER = 0x200;
    private const uint SWP_NOREDRAW = 0x8;
    private const uint SWP_NOREPOSITION = SWP_NOOWNERZORDER;
    private const uint SWP_NOSENDCHANGING = 0x400;
    private const uint SWP_NOSIZE = 0x1;
    private const uint SWP_NOZORDER = 0x4;
    //    private const uint SWP_SHOWWINDOW = 0x40;

    public const int GWL_STYLE = -16;
    private const int WS_BORDER = 1;

    ///////// Basic window style
    public const uint WS_OVERLAPPED = 0x00000000;

    public const uint WS_CAPTION = 0x00C00000;     /* WS_BORDER | WS_DLGFRAME  */
    public const uint WS_SYSMENU = 0x00080000;
    public const uint WS_THICKFRAME = 0x00040000;
    public const uint WS_MINIMIZEBOX = 0x00020000;
    public const uint WS_MAXIMIZEBOX = 0x00010000;

    public const uint WS_OVERLAPPEDWINDOW =
            (WS_OVERLAPPED |
              WS_CAPTION |
              WS_SYSMENU |
              WS_THICKFRAME |
              WS_MINIMIZEBOX |
              WS_MAXIMIZEBOX);

    /////////////////////////////////////////////////
    //////////// window layer info //////////////////
    private const int HWND_TOP = 0;

    private const int HWND_BOTTOM = 1;
    private const int HWND_TOPMOST = -1;
    private const int HWND_NOTOPMOST = -2;
    ////////////////////////////////////////////////

    #endregion MODIFY_WINDOW
}

#endif

#if UNITY_STANDALONE_WIN

static public partial class OmnityWindowsExtensions {

    #region REDRAW_CODE

    //  [Flags()]
    private enum RedrawWindowFlags : uint {

        /// <summary>
        /// Invalidates the rectangle or region that you specify in lprcUpdate or hrgnUpdate.
        /// You can set only one of these parameters to a non-NULL value. If both are NULL, RDW_INVALIDATE invalidates the entire window.
        /// </summary>
        Invalidate = 0x1,

        /// <summary>Causes the OS to post a WM_PAINT message to the window regardless of whether a portion of the window is invalid.</summary>
        InternalPaint = 0x2,

        /// <summary>
        /// Causes the window to receive a WM_ERASEBKGND message when the window is repainted.
        /// Specify this value in combination with the RDW_INVALIDATE value; otherwise, RDW_ERASE has no effect.
        /// </summary>
        Erase = 0x4,

        /// <summary>
        /// Validates the rectangle or region that you specify in lprcUpdate or hrgnUpdate.
        /// You can set only one of these parameters to a non-NULL value. If both are NULL, RDW_VALIDATE validates the entire window.
        /// This value does not affect internal WM_PAINT messages.
        /// </summary>
        Validate = 0x8,

        NoInternalPaint = 0x10,

        /// <summary>Suppresses any pending WM_ERASEBKGND messages.</summary>
        NoErase = 0x20,

        /// <summary>Excludes child windows, if any, from the repainting operation.</summary>
        NoChildren = 0x40,

        /// <summary>Includes child windows, if any, in the repainting operation.</summary>
        AllChildren = 0x80,

        /// <summary>Causes the affected windows, which you specify by setting the RDW_ALLCHILDREN and RDW_NOCHILDREN values, to receive WM_ERASEBKGND and WM_PAINT messages before the RedrawWindow returns, if necessary.</summary>
        UpdateNow = 0x100,

        /// <summary>
        /// Causes the affected windows, which you specify by setting the RDW_ALLCHILDREN and RDW_NOCHILDREN values, to receive WM_ERASEBKGND messages before RedrawWindow returns, if necessary.
        /// The affected windows receive WM_PAINT messages at the ordinary time.
        /// </summary>
        EraseNow = 0x200,

        Frame = 0x400,

        NoFrame = 0x800
    }

    [DllImport("user32.dll", SetLastError = false)]
    private static extern System.IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    private static extern bool RedrawWindow(System.IntPtr hWnd, ref RECT lprcUpdate, System.IntPtr hrgnUpdate, RedrawWindowFlags flags);

    static public void RefreshDesktop() {
        try {
            RECT rect;
            GetClientRect(GetDesktopWindow(), out rect);
            //            System.IntPtr lprcUpdate = System.IntPtr.Zero;
            RedrawWindow(GetDesktopWindow(), ref rect, System.IntPtr.Zero, RedrawWindowFlags.Invalidate | RedrawWindowFlags.UpdateNow | RedrawWindowFlags.EraseNow | RedrawWindowFlags.AllChildren);
        } catch {
        }
    }

    #endregion REDRAW_CODE
}

#endif

#if UNITY_STANDALONE_WIN

static public partial class OmnityWindowsExtensions {

    #region FINDCURRENTWINDOW_CODE

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int GetWindowThreadProcessId(HandleRef handle, out int processId);

    private delegate bool EnumWindowsProc(System.IntPtr hWnd, System.IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool EnumWindows(EnumWindowsProc callback, System.IntPtr extraData);

    /*
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT {
        public int Left, Top, Right, Bottom;
    }*/

    [StructLayout(LayoutKind.Sequential)]
    private struct WINDOWINFO {
        public uint cbSize;
        public RECT rcWindow;
        public RECT rcClient;
        public uint dwStyle;
        public uint dwExStyle;
        public uint dwWindowStatus;
        public uint cxWindowBorders;
        public uint cyWindowBorders;
        public ushort atomWindowType;
        public ushort wCreatorVersion;

        public WINDOWINFO(System.Boolean? filler)
            : this()   // Allows automatic initialization of "cbSize" with "new WINDOWINFO(null/true/false)".
        {
            cbSize = (System.UInt32)(Marshal.SizeOf(typeof(WINDOWINFO)));
        }

        public int areaWindow {
            get {
                return (rcWindow.Right - rcWindow.Left) * (rcWindow.Bottom - rcWindow.Top);
            }
        }

        public int areaClient {
            get {
                return (rcClient.Right - rcClient.Left) * (rcClient.Bottom - rcClient.Top);
            }
        }

        public int widthClient {
            get {
                return (rcClient.Right - rcClient.Left);
            }
        }

        public int heightClient {
            get {
                return (rcClient.Bottom - rcClient.Top);
            }
        }

        public int widthWindow {
            get {
                return (rcWindow.Right - rcWindow.Left);
            }
        }

        public int heightWindow {
            get {
                return (rcWindow.Bottom - rcWindow.Top);
            }
        }

        public void PrintInfo() {
            Debug.Log(Screen.width + "," + Screen.height + " : " + widthClient + "x" + heightClient + " : " + widthWindow + "x" + heightWindow);
        }

        public bool RectEqualToUnityWindowSize {
            get {
                return (Screen.width == widthClient && Screen.height == heightClient);
            }
        }
    }

#if UNITY_STANDALONE_WIN

    [return: MarshalAs(UnmanagedType.Bool)]
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowInfo(System.IntPtr hwnd, ref WINDOWINFO pwi);

#endif

    // private static bool bUnityHandleSet = false;
    private static System.IntPtr unityWindowHandle = System.IntPtr.Zero;

    private static int biggest = 0;

    static public bool EnumWindowsCallBack(System.IntPtr hWnd, System.IntPtr lParam) {
        int procid;
        /*   int returnVal = */
        GetWindowThreadProcessId(new HandleRef(Omnity.anOmnity, hWnd), out procid);
        int currentPID = System.Diagnostics.Process.GetCurrentProcess().Id;
        if (procid == currentPID) {
            try {
                WINDOWINFO info = new WINDOWINFO();
                info.cbSize = (uint)Marshal.SizeOf(info);
                GetWindowInfo(hWnd, ref info);
                //              Debug.Log("Found possible Match " + hWnd + " : " +             info.rcClient.Top.ToString() + "," + info.rcClient.Bottom.ToString() + " " +                     info.rcClient.Left.ToString() + "," + info.rcClient.Right.ToString()                    + " more " + info.areaWindow + " : " + info.areaClient);
                if (biggest < info.areaWindow) {
                    biggest = info.areaWindow;
                    unityWindowHandle = hWnd;
                    //   bUnityHandleSet = true;
                    //                  Debug.Log("----\r\nFound possible Match " + hWnd );
                    //info.PrintInfo();
                } else {
                    //                    Debug.Log("----\r\nInequal Match " + hWnd );
                    // info.PrintInfo();
                }
            } catch {
            }
            return true;
        }
        return true;
    }

    // cache the window handle so we we spawn new windows, the old main window will be still known....
    public static void FindWindowByAltCache() {
        FindWindowByAlt();
    }

    public static System.IntPtr FindWindowByAlt() {
        if (Application.isEditor) {
            unityWindowHandle = System.IntPtr.Zero;
            return System.IntPtr.Zero;
        }

#if UNITY_STANDALONE_WIN
        if (unityWindowHandle == System.IntPtr.Zero) {
            System.IntPtr lParam = System.IntPtr.Zero;
            if (!EnumWindows(EnumWindowsCallBack, lParam)) {
                //                Debug.Log("i think we found it");
                // return value useless
            } else {
                // return value useless
            }
        }
        return unityWindowHandle;
#else
    return System.IntPtr.Zero;
#endif
    }

    #endregion FINDCURRENTWINDOW_CODE
}

#endif

/*
 *
 *
 *

#if UNITY_STANDALONE_WIN
   static public partial class OmnityWindowsExtensions {
  static private string CleanInput(string strIn) {
        try {
            return System.Text.RegularExpressions.Regex.Replace(strIn, @"[^a-zA-Z]", "",
                                 System.Text.RegularExpressions.RegexOptions.None);
        } catch {
            return strIn;
        }
    }
    static public bool IsRunningByFilenameNamesCaseInsensitive(System.Collections.Generic.List<string> names) {
        //NOTE: GetProcessByName() doesn't seem to work on Win7
        //Process[] running = Process.GetProcessesByName("notepad");
        try {
            System.Diagnostics.Process[] running = System.Diagnostics.Process.GetProcesses();
            foreach (System.Diagnostics.Process process in running) {
                try {
                    string pname = CleanInput(process.ProcessName);
                    if (names.Contains(pname, System.StringComparer.OrdinalIgnoreCase)) {
                        return true;
                    }
                } catch {
                }
            }
        } catch {
        }
        return false;
    }
    //Import find window function
    [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
    private static extern System.IntPtr FindWindowByCaption(System.IntPtr ZeroOnly, string lpWindowName);

    public static void SetBorderlessWindowPositionResolutionByName(System.Collections.Generic.List<string> PotentialNames, Rect Screen_PositionAndResolution_PositionAndResolution,bool topMost) {
        SetBorderlessWindowPositionResolution(FindByExeLooseCaseInsensitive(PotentialNames), Screen_PositionAndResolution_PositionAndResolution,topMost);
        SetBorderlessWindowPositionResolution(GetApplicationPtrByName(PotentialNames), Screen_PositionAndResolution_PositionAndResolution, topMost);
        SetBorderlessWindowPositionResolution(FindWindowByAlt(), Screen_PositionAndResolution_PositionAndResolution, topMost);
    }

    public static void KillByName(System.Collections.Generic.List<string> PotentialNames) {
        Kill(GetApplicationPtrByName(PotentialNames));
    }

    private static System.IntPtr GetApplicationPtrByName(System.Collections.Generic.List<string> PotentialNames) {
        System.IntPtr windowID = OmnityWindowsExtensions.FindWindowByNames(PotentialNames);
        if (windowID != System.IntPtr.Zero) {
            Debug.Log("FOUND " + windowID);
            return windowID;
        } else {
            foreach (string s in PotentialNames) {
                Debug.Log("GetForegroundWindow fail : " + s);
            }
            return System.IntPtr.Zero;
        }
    }

    static public System.IntPtr FindByExeLooseCaseInsensitive(System.Collections.Generic.List<string> names) {
        string nameToFind = CleanInput(System.IO.Path.GetFileNameWithoutExtension(System.Environment.CommandLine));
        //NOTE: GetProcessByName() doesn't seem to work on Win7
        //Process[] running = Process.GetProcessesByName("notepad");
        try {
            System.Diagnostics.Process[] running = System.Diagnostics.Process.GetProcesses();
            foreach (System.Diagnostics.Process process in running) {
                try {
                    string pname = CleanInput(process.ProcessName);
                    Debug.LogError("Checking " + pname + " vs " + nameToFind);
                    if (System.StringComparer.OrdinalIgnoreCase.Compare(pname, nameToFind) == 0) {
                        Debug.LogError("found " + pname +  "->"+process.Handle);
                        return process.Handle;
                    }
                } catch {
                }
            }
        } catch {
        }
        Debug.LogError("Didn't find anything ");
        return System.IntPtr.Zero;
    }
}
#endif
*/

#if UNITY_STANDALONE_WIN

[StructLayout(LayoutKind.Sequential)]
public struct RECT {
    public int Left, Top, Right, Bottom;

    public RECT(int left, int top, int right, int bottom) {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    public int X {
        get { return Left; }
        set { Right -= (Left - value); Left = value; }
    }

    public int Y {
        get { return Top; }
        set { Bottom -= (Top - value); Top = value; }
    }

    public int Height {
        get { return Bottom - Top; }
        set { Bottom = value + Top; }
    }

    public int Width {
        get { return Right - Left; }
        set { Right = value + Left; }
    }

    public bool Equals(RECT r) {
        return r.Left == Left && r.Top == Top && r.Right == Right && r.Bottom == Bottom;
    }

    public override string ToString() {
        return string.Format(System.Globalization.CultureInfo.CurrentCulture, "{{Left={0},Top={1},Right={2},Bottom={3}}}", Left, Top, Right, Bottom);
    }
}

#endif
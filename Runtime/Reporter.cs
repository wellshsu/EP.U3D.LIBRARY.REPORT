//---------------------------------------------------------------------//
//                    GNU GENERAL PUBLIC LICENSE                       //
//                       Version 2, June 1991                          //
//                                                                     //
// Copyright (C) Wells Hsu, wellshsu@outlook.com, All rights reserved. //
// Everyone is permitted to copy and distribute verbatim copies        //
// of this license document, but changing it is not allowed.           //
//                  SEE LICENSE.md FOR MORE DETAILS.                   //
//---------------------------------------------------------------------//
using EP.U3D.LIBRARY.BASE;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace EP.U3D.LIBRARY.REPORT
{
    public class MKDictionary<T1, T2, T3> : Dictionary<T1, Dictionary<T2, T3>>
    {
        new public Dictionary<T2, T3> this[T1 key]
        {
            get
            {
                if (!ContainsKey(key))
                    Add(key, new Dictionary<T2, T3>());
                Dictionary<T2, T3> returnObj;
                TryGetValue(key, out returnObj);
                return returnObj;
            }
        }

        public bool ContainsKey(T1 key1, T2 key2)
        {
            Dictionary<T2, T3> returnObj;
            TryGetValue(key1, out returnObj);
            if (returnObj == null)
                return false;
            return returnObj.ContainsKey(key2);
        }
    }

    [Serializable]
    public class Images
    {
        public Texture2D clearImage;
        public Texture2D collapseImage;
        public Texture2D clearOnNewSceneImage;
        public Texture2D showTimeImage;
        public Texture2D showSceneImage;
        public Texture2D userImage;
        public Texture2D showMemoryImage;
        public Texture2D softwareImage;
        public Texture2D dateImage;
        public Texture2D showFpsImage;
        public Texture2D infoImage;
        public Texture2D searchImage;
        public Texture2D closeImage;

        public Texture2D buildFromImage;
        public Texture2D systemInfoImage;
        public Texture2D graphicsInfoImage;
        public Texture2D backImage;

        public Texture2D logImage;
        public Texture2D warningImage;
        public Texture2D errorImage;

        public Texture2D barImage;
        public Texture2D button_activeImage;
        public Texture2D even_logImage;
        public Texture2D odd_logImage;
        public Texture2D selectedImage;

        public GUISkin reporterScrollerSkin;
    }

    public class Reporter : MonoBehaviour
    {
        public class Sample
        {
            public long Time;
            public string Scene;
            public float Memory;
            public float FPS;
            public string FPSText;

            public static int Size()
            {
                return sizeof(float) * 3 + sizeof(byte);
            }

            public override string ToString()
            {
                return Helper.StringFormat("[{0}][{1}][{2}]", new DateTime(Time).ToString("HH:mm:ss"), FPS.ToString("0.00"), Memory.ToString("0.00"));
            }
        }

        public class Log
        {
            public int Count = 1;
            public int SampleID;
            public LogType Type;
            public string Condition;
            public string Stacktrace;

            public int Size()
            {
                int val = sizeof(int) * 2 + sizeof(LogType);
                if (string.IsNullOrEmpty(Condition) == false)
                {
                    val += Condition.Length * sizeof(char);
                }
                if (Type == LogType.Error || Type == LogType.Exception)
                {
                    if (string.IsNullOrEmpty(Stacktrace) == false)
                    {
                        val += Stacktrace.Length + sizeof(char);
                    }
                }
                return val;
            }

            public override string ToString()
            {
                string val = null;
                try
                {
                    Sample s = null;
                    if (Instance.samples != null && Instance.samples.Count > SampleID)
                    {
                        s = Instance.samples[SampleID];
                    }
                    Condition = Regex.Replace(Condition, "\t", "");
                    Condition = Regex.Replace(Condition, "\r", "");
                    Condition = Regex.Replace(Condition, "\n", "\n\t");
                    if (Condition.EndsWith("\n\t"))
                    {
                        Condition = Condition.Substring(0, Condition.Length - 5);
                    }
                    val = Helper.StringFormat("[{0}]{1}{2}", Type.ToString(), s == null ? "[NULLSAMPLE]" : s.ToString(), Condition);
                    if (Type == LogType.Error || Type == LogType.Exception)
                    {
                        string[] frames = Stacktrace.Split(Environment.NewLine.ToCharArray());
                        int len = frames.Length > 5 ? 5 : frames.Length - 1;
                        string str = string.Empty;
                        for (int i = 0; i <= len; i++)
                        {
                            string temp = frames[i];
                            temp = Regex.Replace(temp, "\t", "");
                            temp = Regex.Replace(temp, "\r", "");
                            if (i == len)
                            {
                                temp = Regex.Replace(temp, "\n", "");
                            }
                            else
                            {
                                if (!temp.EndsWith("\n"))
                                {
                                    temp += "\n\t";
                                }
                                else
                                {
                                    temp += "\t";
                                }
                            }
                            str += temp;
                        }
                        val += str;
                    }
                }
                catch
                {
                    val = "Reporter.Log.ToString() exception";
                }
                return val;
            }
        }

        public enum ViewType
        {
            None,
            Logs,
            Info,
            Snapshot
        }

        private List<Sample> samples = new List<Sample>(60 * 60 * 60);
        private List<Log> logs = new List<Log>();
        private List<Log> collapsedLogs = new List<Log>();
        private List<Log> currentLog = new List<Log>();
        private MKDictionary<string, string, Log> logsDic = new MKDictionary<string, string, Log>();
        private Dictionary<string, string> cachedString = new Dictionary<string, string>();
        private bool collapse;
        private bool clearOnNewSceneLoaded;
        private bool showTime;
        private bool showScene;
        private bool showMemory;
        private bool showFps;
        private bool showGraph;
        private bool showLog = true;
        private bool showWarning = true;
        private bool showError = true;
        private int numOfLogs;
        private int numOfLogsWarning;
        private int numOfLogsError;
        private int numOfCollapsedLogs;
        private int numOfCollapsedLogsWarning;
        private int numOfCollapsedLogsError;
        private bool showClearOnNewSceneLoadedButton = true;
        private bool showTimeButton = true;
        private bool showSceneButton = true;
        private bool showMemButton = true;
        private bool showFpsButton = true;
        private bool showSearchText = true;
        private string buildDate = string.Empty;
        private string logDate;
        private float logsMemUsage;
        private float graphMemUsage;
        private float gcTotalMemory;
        public float fps;
        public string fpsText;

        private ViewType currentView = ViewType.Logs;
        private Images images;
        private GUIContent clearContent;
        private GUIContent collapseContent;
        private GUIContent clearOnNewSceneContent;
        private GUIContent showTimeContent;
        private GUIContent showSceneContent;
        private GUIContent userContent;
        private GUIContent showMemoryContent;
        private GUIContent softwareContent;
        private GUIContent dateContent;
        private GUIContent showFpsContent;
        private GUIContent infoContent;
        private GUIContent searchContent;
        private GUIContent closeContent;
        private GUIContent buildFromContent;
        private GUIContent systemInfoContent;
        private GUIContent graphicsInfoContent;
        private GUIContent backContent;
        private GUIContent logContent;
        private GUIContent warningContent;
        private GUIContent errorContent;
        private GUIStyle barStyle;
        private GUIStyle buttonActiveStyle;
        private GUIStyle nonStyle;
        private GUIStyle lowerLeftFontStyle;
        private GUIStyle backStyle;
        private GUIStyle evenLogStyle;
        private GUIStyle oddLogStyle;
        private GUIStyle logButtonStyle;
        private GUIStyle selectedLogStyle;
        private GUIStyle selectedLogFontStyle;
        private GUIStyle stackLabelStyle;
        private GUIStyle scrollerStyle;
        private GUIStyle searchStyle;
        private GUIStyle sliderBackStyle;
        private GUIStyle sliderThumbStyle;
        private GUISkin toolbarScrollerSkin;
        private GUISkin logScrollerSkin;
        private GUISkin graphScrollerSkin;

        public Vector2 size = new Vector2(32, 32);
        public float maxSize = 20;
        public int numOfCircleToShow = 1;
        private string currentScene;
        private string filterText = "";

        private string deviceModel;
        private string deviceType;
        private string deviceName;
        private string graphicsMemorySize;
        private string maxTextureSize;
        private string systemMemorySize;

        public static Reporter Instance;
        private static string _userData;
        public static string UserData
        {
            get
            {
                try
                {
                    if (string.IsNullOrEmpty(_userData))
                    {
                        string file = Helper.StringFormat("{0}User.bytes", Constants.CONFIG_PATH);
                        _userData = File.ReadAllText(file);
                    }
                }
                catch { _userData = "NONAME"; }
                return _userData;
            }
            set
            {
                _userData = value;
                try
                {
                    if (string.IsNullOrEmpty(_userData) == false)
                    {
                        string file = Helper.StringFormat("{0}User.bytes", Constants.CONFIG_PATH);
                        File.WriteAllText(file, _userData);
                    }
                }
                catch { }
            }
        }
        private bool guiOK;
        private bool guiEnabled;
        private bool guiShow;
        private int verboseLogSize;
        public static string LOG_EXCEPTION;
        public static string LOG_VERBOSE;

        public const int LOG_MEM_MAX = 256 * 1024;
        public const int VERBOSE_FILE_MAX = 1024 * 1024;
        public const int EXCEPTION_FILE_MAX = 256 * 1024;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            SavePref();
        }

        private void OnApplicationQuit()
        {
            SavePref();
            SaveVerbose();
        }

        private void AddSample()
        {
            var sample = new Sample();
            sample.FPS = fps;
            sample.FPSText = fpsText;
            sample.Scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            sample.Time = DateTime.Now.Ticks;
            sample.Memory = gcTotalMemory;
            samples.Add(sample);
            graphMemUsage = samples.Count * Sample.Size() / 1024.0f / 1024.0f;
        }

        public static event Action<string, bool> OnException;
        public static void Initialize(Transform root, Action<string, bool> onException)
        {
            OnException += onException;
            if (!Instance)
            {
                GameObject go = new GameObject("Reporter");
                go.transform.parent = root;
                go.AddComponent<Reporter>();
                UnityEngine.SceneManagement.SceneManager.sceneLoaded += Instance.OnSceneWasLoaded;
                Instance.currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                Application.logMessageReceivedThreaded += Instance.CaptureLogThread;
                Instance.deviceModel = SystemInfo.deviceModel;
                Instance.deviceType = SystemInfo.deviceType.ToString();
                Instance.deviceName = SystemInfo.deviceName;
                Instance.graphicsMemorySize = SystemInfo.graphicsMemorySize.ToString();
                Instance.maxTextureSize = SystemInfo.maxTextureSize.ToString();
                Instance.systemMemorySize = SystemInfo.systemMemorySize.ToString();
                Instance.logDate = DateTime.Now.ToString("yyyy-MM-dd_HH_mm_ss");
                LOG_VERBOSE = Helper.StringFormat("{0}verbose.log", Constants.LOG_PATH);
                LOG_EXCEPTION = Helper.StringFormat("{0}exception.log", Constants.LOG_PATH);
                try
                {
                    if (File.Exists(LOG_VERBOSE))
                    {
                        byte[] bytes = File.ReadAllBytes(LOG_VERBOSE);
                        if (bytes.Length > VERBOSE_FILE_MAX)
                        {
                            Helper.DeleteFile(LOG_VERBOSE);
                        }
                    }
                }
                catch { }
            }
        }

        public static void EnableGUI(bool status)
        {
            if (Instance && !Instance.guiOK)
            {
                if (!Instance.guiOK)
                {
                    Instance.InitGUI();
                }
                Instance.guiEnabled = status;
            }
        }

        public static void CommitVerbose()
        {
            if (!Application.isEditor)
            {
                string name = Helper.StringFormat("[{0}][{1}].verbose", UserData, DateTime.Now.ToString("yyyy-MM-dd_HH_mm_ss"));
                Commit(Constants.REPORT_URL, name, LOG_VERBOSE, true);
            }
        }

        public static void CommitException()
        {
            if (!Application.isEditor)
            {
                string name = Helper.StringFormat("[{0}][{1}].exception", UserData, DateTime.Now.ToString("yyyy-MM-dd_HH_mm_ss"));
                Commit(Constants.REPORT_URL, name, LOG_EXCEPTION, true);
            }
        }

        public static void Commit(string url, string name, string file, bool delete)
        {
            if (File.Exists(file))
            {
                byte[] bytes = File.ReadAllBytes(file);
                Loom.StartCR(CommitCR(url, name, bytes));
                if (delete)
                {
                    Helper.DeleteFile(file);
                }
            }
        }

        private static IEnumerator CommitCR(string url, string name, byte[] bytes)
        {
            if (string.IsNullOrEmpty(url) == false && string.IsNullOrEmpty(name) == false && bytes != null && bytes.Length > 0)
            {
                WWWForm form = new WWWForm();
                form.AddField("enctype", "multipart/form-data");
                form.AddField("userdata", UserData);
                form.AddField("channel", Constants.CHANNEL_NAME);
                form.AddBinaryData("file", bytes, name);
                var www = new WWW(url, form);
                yield return www;
            }
            yield return 0;
        }

        private void InitGUI()
        {
            if (Instance && !Instance.guiOK)
            {
                images = new Images();
                images.clearImage = (Texture2D)Resources.Load("Raw/Images/Console/clear", typeof(Texture2D));
                images.collapseImage = (Texture2D)Resources.Load("Raw/Images/Console/collapse", typeof(Texture2D));
                images.clearOnNewSceneImage = (Texture2D)Resources.Load("Raw/Images/Console/clearOnSceneLoaded", typeof(Texture2D));
                images.showTimeImage = (Texture2D)Resources.Load("Raw/Images/Console/timer_1", typeof(Texture2D));
                images.showSceneImage = (Texture2D)Resources.Load("Raw/Images/Console/UnityIcon", typeof(Texture2D));
                images.userImage = (Texture2D)Resources.Load("Raw/Images/Console/user", typeof(Texture2D));
                images.showMemoryImage = (Texture2D)Resources.Load("Raw/Images/Console/memory", typeof(Texture2D));
                images.softwareImage = (Texture2D)Resources.Load("Raw/Images/Console/software", typeof(Texture2D));
                images.dateImage = (Texture2D)Resources.Load("Raw/Images/Console/date", typeof(Texture2D));
                images.showFpsImage = (Texture2D)Resources.Load("Raw/Images/Console/fps", typeof(Texture2D));
                images.infoImage = (Texture2D)Resources.Load("Raw/Images/Console/info", typeof(Texture2D));
                images.searchImage = (Texture2D)Resources.Load("Raw/Images/Console/search", typeof(Texture2D));
                images.closeImage = (Texture2D)Resources.Load("Raw/Images/Console/close", typeof(Texture2D));
                images.buildFromImage = (Texture2D)Resources.Load("Raw/Images/Console/buildFrom", typeof(Texture2D));
                images.systemInfoImage = (Texture2D)Resources.Load("Raw/Images/Console/ComputerIcon", typeof(Texture2D));
                images.graphicsInfoImage = (Texture2D)Resources.Load("Raw/Images/Console/graphicCard", typeof(Texture2D));
                images.backImage = (Texture2D)Resources.Load("Raw/Images/Console/back", typeof(Texture2D));
                images.logImage = (Texture2D)Resources.Load("Raw/Images/Console/log_icon", typeof(Texture2D));
                images.warningImage = (Texture2D)Resources.Load("Raw/Images/Console/warning_icon", typeof(Texture2D));
                images.errorImage = (Texture2D)Resources.Load("Raw/Images/Console/error_icon", typeof(Texture2D));
                images.barImage = (Texture2D)Resources.Load("Raw/Images/Console/bar", typeof(Texture2D));
                images.button_activeImage = (Texture2D)Resources.Load("Raw/Images/Console/button_active", typeof(Texture2D));
                images.even_logImage = (Texture2D)Resources.Load("Raw/Images/Console/even_log", typeof(Texture2D));
                images.odd_logImage = (Texture2D)Resources.Load("Raw/Images/Console/odd_log", typeof(Texture2D));
                images.selectedImage = (Texture2D)Resources.Load("Raw/Images/Console/selected", typeof(Texture2D));
                images.reporterScrollerSkin = (GUISkin)Resources.Load("Raw/Images/Console/reporterScrollerSkin", typeof(GUISkin));

                clearContent = new GUIContent("", images.clearImage, "Clear logs");
                collapseContent = new GUIContent("", images.collapseImage, "Collapse logs");
                clearOnNewSceneContent = new GUIContent("", images.clearOnNewSceneImage, "Clear logs on new scene loaded");
                showTimeContent = new GUIContent("", images.showTimeImage, "Show Hide Time");
                showSceneContent = new GUIContent("", images.showSceneImage, "Show Hide Scene");
                showMemoryContent = new GUIContent("", images.showMemoryImage, "Show Hide Memory");
                softwareContent = new GUIContent("", images.softwareImage, "Software");
                dateContent = new GUIContent("", images.dateImage, "Date");
                showFpsContent = new GUIContent("", images.showFpsImage, "Show Hide fps");
                infoContent = new GUIContent("", images.infoImage, "Information about application");
                searchContent = new GUIContent("", images.searchImage, "Search for logs");
                closeContent = new GUIContent("", images.closeImage, "Hide logs");
                userContent = new GUIContent("", images.userImage, "User");
                buildFromContent = new GUIContent("", images.buildFromImage, "Build From");
                systemInfoContent = new GUIContent("", images.systemInfoImage, "System Info");
                graphicsInfoContent = new GUIContent("", images.graphicsInfoImage, "Graphics Info");
                backContent = new GUIContent("", images.backImage, "Back");
                logContent = new GUIContent("", images.logImage, "show or hide logs");
                warningContent = new GUIContent("", images.warningImage, "show or hide warnings");
                errorContent = new GUIContent("", images.errorImage, "show or hide errors");
                currentView = (ViewType)PlayerPrefs.GetInt("Reporter_currentView", 1);
                collapse = PlayerPrefs.GetInt("Reporter_collapse") == 1;
                clearOnNewSceneLoaded = PlayerPrefs.GetInt("Reporter_clearOnNewSceneLoaded") == 1;
                showTime = PlayerPrefs.GetInt("Reporter_showTime") == 1;
                showScene = PlayerPrefs.GetInt("Reporter_showScene") == 1;
                showMemory = PlayerPrefs.GetInt("Reporter_showMemory") == 1;
                showFps = PlayerPrefs.GetInt("Reporter_showFps") == 1;
                showGraph = PlayerPrefs.GetInt("Reporter_showGraph") == 1;
                showLog = PlayerPrefs.GetInt("Reporter_showLog", 1) == 1;
                showWarning = PlayerPrefs.GetInt("Reporter_showWarning", 1) == 1;
                showError = PlayerPrefs.GetInt("Reporter_showError", 1) == 1;
                filterText = PlayerPrefs.GetString("Reporter_filterText");
                size.x = size.y = PlayerPrefs.GetFloat("Reporter_size", 32);
                showClearOnNewSceneLoadedButton = PlayerPrefs.GetInt("Reporter_showClearOnNewSceneLoadedButton", 1) == 1;
                showTimeButton = PlayerPrefs.GetInt("Reporter_showTimeButton", 1) == 1;
                showSceneButton = PlayerPrefs.GetInt("Reporter_showSceneButton", 1) == 1;
                showMemButton = PlayerPrefs.GetInt("Reporter_showMemButton", 1) == 1;
                showFpsButton = PlayerPrefs.GetInt("Reporter_showFpsButton", 1) == 1;
                showSearchText = PlayerPrefs.GetInt("Reporter_showSearchText", 1) == 1;

                InitStyle();

                Instance.guiOK = true;
            }
        }

        private void InitStyle()
        {
            var paddingX = (int)(size.x * 0.2f);
            var paddingY = (int)(size.y * 0.2f);
            nonStyle = new GUIStyle();
            nonStyle.clipping = TextClipping.Clip;
            nonStyle.border = new RectOffset(0, 0, 0, 0);
            nonStyle.normal.background = null;
            nonStyle.fontSize = (int)(size.y / 2);
            nonStyle.alignment = TextAnchor.MiddleCenter;

            lowerLeftFontStyle = new GUIStyle();
            lowerLeftFontStyle.clipping = TextClipping.Clip;
            lowerLeftFontStyle.border = new RectOffset(0, 0, 0, 0);
            lowerLeftFontStyle.normal.background = null;
            lowerLeftFontStyle.fontSize = (int)(size.y / 2);
            lowerLeftFontStyle.fontStyle = FontStyle.Bold;
            lowerLeftFontStyle.alignment = TextAnchor.LowerLeft;


            barStyle = new GUIStyle();
            barStyle.border = new RectOffset(1, 1, 1, 1);
            barStyle.normal.background = images.barImage;
            barStyle.active.background = images.button_activeImage;
            barStyle.alignment = TextAnchor.MiddleCenter;
            barStyle.margin = new RectOffset(1, 1, 1, 1);

            barStyle.clipping = TextClipping.Clip;
            barStyle.fontSize = (int)(size.y / 2);


            buttonActiveStyle = new GUIStyle();
            buttonActiveStyle.border = new RectOffset(1, 1, 1, 1);
            buttonActiveStyle.normal.background = images.button_activeImage;
            buttonActiveStyle.alignment = TextAnchor.MiddleCenter;
            buttonActiveStyle.margin = new RectOffset(1, 1, 1, 1);
            //buttonActiveStyle.padding = new RectOffset(4,4,4,4);
            buttonActiveStyle.fontSize = (int)(size.y / 2);

            backStyle = new GUIStyle();
            backStyle.normal.background = images.even_logImage;
            backStyle.clipping = TextClipping.Clip;
            backStyle.fontSize = (int)(size.y / 2);

            evenLogStyle = new GUIStyle();
            evenLogStyle.normal.background = images.even_logImage;
            evenLogStyle.fixedHeight = size.y;
            evenLogStyle.clipping = TextClipping.Clip;
            evenLogStyle.alignment = TextAnchor.UpperLeft;
            evenLogStyle.imagePosition = ImagePosition.ImageLeft;
            evenLogStyle.fontSize = (int)(size.y / 2);
            //evenLogStyle.wordWrap = true;

            oddLogStyle = new GUIStyle();
            oddLogStyle.normal.background = images.odd_logImage;
            oddLogStyle.fixedHeight = size.y;
            oddLogStyle.clipping = TextClipping.Clip;
            oddLogStyle.alignment = TextAnchor.UpperLeft;
            oddLogStyle.imagePosition = ImagePosition.ImageLeft;
            oddLogStyle.fontSize = (int)(size.y / 2);
            //oddLogStyle.wordWrap = true ;

            logButtonStyle = new GUIStyle();
            //logButtonStyle.wordWrap = true;
            logButtonStyle.fixedHeight = size.y;
            logButtonStyle.clipping = TextClipping.Clip;
            logButtonStyle.alignment = TextAnchor.UpperLeft;
            //logButtonStyle.imagePosition = ImagePosition.ImageLeft ;
            //logButtonStyle.wordWrap = true;
            logButtonStyle.fontSize = (int)(size.y / 2);
            logButtonStyle.padding = new RectOffset(paddingX, paddingX, paddingY, paddingY);

            selectedLogStyle = new GUIStyle();
            selectedLogStyle.normal.background = images.selectedImage;
            selectedLogStyle.fixedHeight = size.y;
            selectedLogStyle.clipping = TextClipping.Clip;
            selectedLogStyle.alignment = TextAnchor.UpperLeft;
            selectedLogStyle.normal.textColor = Color.white;
            //selectedLogStyle.wordWrap = true;
            selectedLogStyle.fontSize = (int)(size.y / 2);

            selectedLogFontStyle = new GUIStyle();
            selectedLogFontStyle.normal.background = images.selectedImage;
            selectedLogFontStyle.fixedHeight = size.y;
            selectedLogFontStyle.clipping = TextClipping.Clip;
            selectedLogFontStyle.alignment = TextAnchor.UpperLeft;
            selectedLogFontStyle.normal.textColor = Color.white;
            //selectedLogStyle.wordWrap = true;
            selectedLogFontStyle.fontSize = (int)(size.y / 2);
            selectedLogFontStyle.padding = new RectOffset(paddingX, paddingX, paddingY, paddingY);

            stackLabelStyle = new GUIStyle();
            stackLabelStyle.wordWrap = true;
            stackLabelStyle.fontSize = (int)(size.y / 2);
            stackLabelStyle.padding = new RectOffset(paddingX, paddingX, paddingY, paddingY);

            scrollerStyle = new GUIStyle();
            scrollerStyle.normal.background = images.barImage;

            searchStyle = new GUIStyle();
            searchStyle.clipping = TextClipping.Clip;
            searchStyle.alignment = TextAnchor.LowerCenter;
            searchStyle.fontSize = (int)(size.y / 2);
            searchStyle.wordWrap = true;


            sliderBackStyle = new GUIStyle();
            sliderBackStyle.normal.background = images.barImage;
            sliderBackStyle.fixedHeight = size.y;
            sliderBackStyle.border = new RectOffset(1, 1, 1, 1);

            sliderThumbStyle = new GUIStyle();
            sliderThumbStyle.normal.background = images.selectedImage;
            sliderThumbStyle.fixedWidth = size.x;

            var skin = images.reporterScrollerSkin;

            toolbarScrollerSkin = Instantiate(skin);
            toolbarScrollerSkin.verticalScrollbar.fixedWidth = 0f;
            toolbarScrollerSkin.horizontalScrollbar.fixedHeight = 0f;
            toolbarScrollerSkin.verticalScrollbarThumb.fixedWidth = 0f;
            toolbarScrollerSkin.horizontalScrollbarThumb.fixedHeight = 0f;

            logScrollerSkin = Instantiate(skin);
            logScrollerSkin.verticalScrollbar.fixedWidth = size.x * 2f;
            logScrollerSkin.horizontalScrollbar.fixedHeight = 0f;
            logScrollerSkin.verticalScrollbarThumb.fixedWidth = size.x * 2f;
            logScrollerSkin.horizontalScrollbarThumb.fixedHeight = 0f;

            graphScrollerSkin = Instantiate(skin);
            graphScrollerSkin.verticalScrollbar.fixedWidth = 0f;
            graphScrollerSkin.horizontalScrollbar.fixedHeight = size.x * 2f;
            graphScrollerSkin.verticalScrollbarThumb.fixedWidth = 0f;
            graphScrollerSkin.horizontalScrollbarThumb.fixedHeight = size.x * 2f;
        }

        private void Clear()
        {
            verboseLogSize = 0;
            startIndex = 0;
            logs.Clear();
            collapsedLogs.Clear();
            currentLog.Clear();
            logsDic.Clear();
            selectedLog = null;
            numOfLogs = 0;
            numOfLogsWarning = 0;
            numOfLogsError = 0;
            numOfCollapsedLogs = 0;
            numOfCollapsedLogsWarning = 0;
            numOfCollapsedLogsError = 0;
            logsMemUsage = 0;
            graphMemUsage = 0;
            samples.Clear();
            GC.Collect();
            selectedLog = null;
        }

        private Rect screenRect = new Rect();
        private Rect toolBarRect = new Rect();
        private Rect logsRect = new Rect();
        private Rect stackRect = new Rect();
        private Rect graphRect = new Rect();
        private Rect graphMinRect = new Rect();
        private Rect graphMaxRect = new Rect();
        private Rect buttomRect = new Rect();
        private Vector2 stackRectTopLeft;
        private Rect detailRect = new Rect();
        private Vector2 scrollPosition;
        private Vector2 scrollPosition2;
        private Vector2 toolbarScrollPosition;
        private Log selectedLog;
        private float toolbarOldDrag;
        private float oldDrag;
        private float oldDrag2;
        private float oldDrag3;
        private int startIndex;

        private void CalculateCurrentLog()
        {
            var filter = !string.IsNullOrEmpty(filterText);
            var _filterText = "";
            if (filter)
                _filterText = filterText.ToLower();
            currentLog.Clear();
            if (collapse)
                for (var i = 0; i < collapsedLogs.Count; i++)
                {
                    var log = collapsedLogs[i];
                    if (log.Type == LogType.Log && !showLog)
                        continue;
                    if (log.Type == LogType.Warning && !showWarning)
                        continue;
                    if (log.Type == LogType.Error && !showError)
                        continue;
                    if (log.Type == LogType.Assert && !showError)
                        continue;
                    if (log.Type == LogType.Exception && !showError)
                        continue;

                    if (filter)
                    {
                        if (log.Condition.ToLower().Contains(_filterText))
                            currentLog.Add(log);
                    }
                    else
                    {
                        currentLog.Add(log);
                    }
                }
            else
                for (var i = 0; i < logs.Count; i++)
                {
                    var log = logs[i];
                    if (log.Type == LogType.Log && !showLog)
                        continue;
                    if (log.Type == LogType.Warning && !showWarning)
                        continue;
                    if (log.Type == LogType.Error && !showError)
                        continue;
                    if (log.Type == LogType.Assert && !showError)
                        continue;
                    if (log.Type == LogType.Exception && !showError)
                        continue;

                    if (filter)
                    {
                        if (log.Condition.ToLower().Contains(_filterText))
                            currentLog.Add(log);
                    }
                    else
                    {
                        currentLog.Add(log);
                    }
                }
            if (selectedLog != null)
            {
                var newSelectedIndex = currentLog.IndexOf(selectedLog);
                if (newSelectedIndex == -1)
                {
                    var collapsedSelected = logsDic[selectedLog.Condition][selectedLog.Stacktrace];
                    newSelectedIndex = currentLog.IndexOf(collapsedSelected);
                    if (newSelectedIndex != -1)
                        scrollPosition.y = newSelectedIndex * size.y;
                }
                else
                {
                    scrollPosition.y = newSelectedIndex * size.y;
                }
            }
        }

        private Rect countRect = new Rect();
        private Rect timeRect = new Rect();
        private Rect timeLabelRect = new Rect();
        private Rect sceneRect = new Rect();
        private Rect sceneLabelRect = new Rect();
        private Rect memoryRect = new Rect();
        private Rect memoryLabelRect = new Rect();
        private Rect fpsRect = new Rect();
        private Rect fpsLabelRect = new Rect();
        private GUIContent tempContent = new GUIContent();
        private Vector2 infoScrollPosition;
        private Vector2 oldInfoDrag;

        private void DrawInfo()
        {
            GUILayout.BeginArea(screenRect, backStyle);

            var drag = GetDragPos();
            if (drag.x != 0 && downPos != Vector2.zero)
                infoScrollPosition.x -= drag.x - oldInfoDrag.x;
            if (drag.y != 0 && downPos != Vector2.zero)
                infoScrollPosition.y += drag.y - oldInfoDrag.y;
            oldInfoDrag = drag;

            GUI.skin = toolbarScrollerSkin;
            infoScrollPosition = GUILayout.BeginScrollView(infoScrollPosition);
            GUILayout.Space(size.x);
            GUILayout.BeginHorizontal();
            GUILayout.Space(size.x);
            GUILayout.Box(buildFromContent, nonStyle, GUILayout.Width(size.x), GUILayout.Height(size.y));
            GUILayout.Space(size.x);
            GUILayout.Label(buildDate, nonStyle, GUILayout.Height(size.y));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(size.x);
            GUILayout.Box(systemInfoContent, nonStyle, GUILayout.Width(size.x), GUILayout.Height(size.y));
            GUILayout.Space(size.x);
            GUILayout.Label(deviceModel, nonStyle, GUILayout.Height(size.y));
            GUILayout.Space(size.x);
            GUILayout.Label(deviceType, nonStyle, GUILayout.Height(size.y));
            GUILayout.Space(size.x);
            GUILayout.Label(deviceName, nonStyle, GUILayout.Height(size.y));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(size.x);
            GUILayout.Box(graphicsInfoContent, nonStyle, GUILayout.Width(size.x), GUILayout.Height(size.y));
            GUILayout.Space(size.x);
            GUILayout.Label(SystemInfo.graphicsDeviceName, nonStyle, GUILayout.Height(size.y));
            GUILayout.Space(size.x);
            GUILayout.Label(graphicsMemorySize, nonStyle, GUILayout.Height(size.y));
            GUILayout.Space(size.x);
            GUILayout.Label(maxTextureSize, nonStyle, GUILayout.Height(size.y));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(size.x);
            GUILayout.Space(size.x);
            GUILayout.Space(size.x);
            GUILayout.Label("Screen Width " + Screen.width, nonStyle, GUILayout.Height(size.y));
            GUILayout.Space(size.x);
            GUILayout.Label("Screen Height " + Screen.height, nonStyle, GUILayout.Height(size.y));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(size.x);
            GUILayout.Box(showMemoryContent, nonStyle, GUILayout.Width(size.x), GUILayout.Height(size.y));
            GUILayout.Space(size.x);
            GUILayout.Label(systemMemorySize + " mb", nonStyle, GUILayout.Height(size.y));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(size.x);
            GUILayout.Space(size.x);
            GUILayout.Space(size.x);
            GUILayout.Label("Mem Usage Of Logs " + logsMemUsage.ToString("0.000") + " mb", nonStyle, GUILayout.Height(size.y));
            GUILayout.Space(size.x);

            GUILayout.Label("Mem Usage Of Graph " + graphMemUsage.ToString("0.000") + " mb", nonStyle, GUILayout.Height(size.y));
            GUILayout.Space(size.x);

            GUILayout.Label("GC Memory " + gcTotalMemory.ToString("0.000") + " mb", nonStyle, GUILayout.Height(size.y));

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(size.x);
            GUILayout.Box(softwareContent, nonStyle, GUILayout.Width(size.x), GUILayout.Height(size.y));
            GUILayout.Space(size.x);
            GUILayout.Label(SystemInfo.operatingSystem, nonStyle, GUILayout.Height(size.y));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(size.x);
            GUILayout.Box(dateContent, nonStyle, GUILayout.Width(size.x), GUILayout.Height(size.y));
            GUILayout.Space(size.x);
            GUILayout.Label(DateTime.Now.ToString(CultureInfo.InvariantCulture), nonStyle, GUILayout.Height(size.y));
            GUILayout.Label(" - Application Started At " + logDate, nonStyle, GUILayout.Height(size.y));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(size.x);
            GUILayout.Box(showTimeContent, nonStyle, GUILayout.Width(size.x), GUILayout.Height(size.y));
            GUILayout.Space(size.x);
            GUILayout.Label(Time.realtimeSinceStartup.ToString("000"), nonStyle, GUILayout.Height(size.y));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(size.x);
            GUILayout.Box(showFpsContent, nonStyle, GUILayout.Width(size.x), GUILayout.Height(size.y));
            GUILayout.Space(size.x);
            GUILayout.Label(fpsText, nonStyle, GUILayout.Height(size.y));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(size.x);
            GUILayout.Box(userContent, nonStyle, GUILayout.Width(size.x), GUILayout.Height(size.y));
            GUILayout.Space(size.x);
            GUILayout.Label(UserData, nonStyle, GUILayout.Height(size.y));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(size.x);
            GUILayout.Box(showSceneContent, nonStyle, GUILayout.Width(size.x), GUILayout.Height(size.y));
            GUILayout.Space(size.x);
            GUILayout.Label(currentScene, nonStyle, GUILayout.Height(size.y));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(size.x);
            GUILayout.Box(showSceneContent, nonStyle, GUILayout.Width(size.x), GUILayout.Height(size.y));
            GUILayout.Space(size.x);
            GUILayout.Label("Unity Version = " + Application.unityVersion, nonStyle, GUILayout.Height(size.y));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            drawInfo_enableDisableToolBarButtons();

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            GUILayout.Space(size.x);
            GUILayout.Label("Size = " + size.x.ToString("0.0"), nonStyle, GUILayout.Height(size.y));
            GUILayout.Space(size.x);
            var _size = GUILayout.HorizontalSlider(size.x, 16, 64, sliderBackStyle, sliderThumbStyle,
                GUILayout.Width(Screen.width * 0.5f));
            if (size.x != _size)
            {
                size.x = size.y = _size;
                InitStyle();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(size.x);
            if (GUILayout.Button(backContent, barStyle, GUILayout.Width(size.x * 2), GUILayout.Height(size.y * 2)))
                currentView = ViewType.Logs;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();


            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void drawInfo_enableDisableToolBarButtons()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(size.x);
            GUILayout.Label("Hide or Show tool bar buttons", nonStyle, GUILayout.Height(size.y));
            GUILayout.Space(size.x);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(size.x);

            if (GUILayout.Button(clearOnNewSceneContent, showClearOnNewSceneLoadedButton ? buttonActiveStyle : barStyle,
                GUILayout.Width(size.x * 2), GUILayout.Height(size.y * 2)))
                showClearOnNewSceneLoadedButton = !showClearOnNewSceneLoadedButton;

            if (GUILayout.Button(showTimeContent, showTimeButton ? buttonActiveStyle : barStyle,
                GUILayout.Width(size.x * 2), GUILayout.Height(size.y * 2)))
                showTimeButton = !showTimeButton;
            tempRect = GUILayoutUtility.GetLastRect();
            GUI.Label(tempRect, Time.realtimeSinceStartup.ToString("0.0"), lowerLeftFontStyle);
            if (GUILayout.Button(showSceneContent, showSceneButton ? buttonActiveStyle : barStyle,
                GUILayout.Width(size.x * 2), GUILayout.Height(size.y * 2)))
                showSceneButton = !showSceneButton;
            tempRect = GUILayoutUtility.GetLastRect();
            GUI.Label(tempRect, currentScene, lowerLeftFontStyle);
            if (GUILayout.Button(showMemoryContent, showMemButton ? buttonActiveStyle : barStyle,
                GUILayout.Width(size.x * 2), GUILayout.Height(size.y * 2)))
                showMemButton = !showMemButton;
            tempRect = GUILayoutUtility.GetLastRect();
            GUI.Label(tempRect, gcTotalMemory.ToString("0.0"), lowerLeftFontStyle);

            if (GUILayout.Button(showFpsContent, showFpsButton ? buttonActiveStyle : barStyle,
                GUILayout.Width(size.x * 2),
                GUILayout.Height(size.y * 2)))
                showFpsButton = !showFpsButton;
            tempRect = GUILayoutUtility.GetLastRect();
            GUI.Label(tempRect, fpsText, lowerLeftFontStyle);

            if (GUILayout.Button(searchContent, showSearchText ? buttonActiveStyle : barStyle,
                GUILayout.Width(size.x * 2),
                GUILayout.Height(size.y * 2)))
                showSearchText = !showSearchText;
            tempRect = GUILayoutUtility.GetLastRect();
            GUI.TextField(tempRect, filterText, searchStyle);

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawToolBar()
        {
            toolBarRect.x = 0f;
            toolBarRect.y = 0f;
            toolBarRect.width = Screen.width;
            toolBarRect.height = size.y * 2f;

            GUI.skin = toolbarScrollerSkin;
            var drag = GetDragPos();
            if (drag.x != 0 && downPos != Vector2.zero && downPos.y > Screen.height - size.y * 2f)
                toolbarScrollPosition.x -= drag.x - toolbarOldDrag;
            toolbarOldDrag = drag.x;
            GUILayout.BeginArea(toolBarRect);
            toolbarScrollPosition = GUILayout.BeginScrollView(toolbarScrollPosition);
            GUILayout.BeginHorizontal(barStyle);

            if (GUILayout.Button(clearContent, barStyle, GUILayout.Width(size.x * 2), GUILayout.Height(size.y * 2)))
                Clear();
            if (GUILayout.Button(collapseContent, collapse ? buttonActiveStyle : barStyle, GUILayout.Width(size.x * 2),
                GUILayout.Height(size.y * 2)))
            {
                collapse = !collapse;
                CalculateCurrentLog();
            }
            if (showClearOnNewSceneLoadedButton && GUILayout.Button(clearOnNewSceneContent,
                    clearOnNewSceneLoaded ? buttonActiveStyle : barStyle, GUILayout.Width(size.x * 2),
                    GUILayout.Height(size.y * 2)))
                clearOnNewSceneLoaded = !clearOnNewSceneLoaded;

            if (showTimeButton && GUILayout.Button(showTimeContent, showTime ? buttonActiveStyle : barStyle,
                    GUILayout.Width(size.x * 2), GUILayout.Height(size.y * 2)))
                showTime = !showTime;
            if (showSceneButton)
            {
                tempRect = GUILayoutUtility.GetLastRect();
                GUI.Label(tempRect, Time.realtimeSinceStartup.ToString("0.0"), lowerLeftFontStyle);
                if (GUILayout.Button(showSceneContent, showScene ? buttonActiveStyle : barStyle,
                    GUILayout.Width(size.x * 2), GUILayout.Height(size.y * 2)))
                    showScene = !showScene;
                tempRect = GUILayoutUtility.GetLastRect();
                GUI.Label(tempRect, currentScene, lowerLeftFontStyle);
            }
            if (showMemButton)
            {
                if (GUILayout.Button(showMemoryContent, showMemory ? buttonActiveStyle : barStyle,
                    GUILayout.Width(size.x * 2), GUILayout.Height(size.y * 2)))
                    showMemory = !showMemory;
                tempRect = GUILayoutUtility.GetLastRect();
                GUI.Label(tempRect, gcTotalMemory.ToString("0.0"), lowerLeftFontStyle);
            }
            if (showFpsButton)
            {
                if (GUILayout.Button(showFpsContent, showFps ? buttonActiveStyle : barStyle,
                    GUILayout.Width(size.x * 2),
                    GUILayout.Height(size.y * 2)))
                    showFps = !showFps;
                tempRect = GUILayoutUtility.GetLastRect();
                GUI.Label(tempRect, fpsText, lowerLeftFontStyle);
            }

            if (showSearchText)
            {
                GUILayout.Box(searchContent, barStyle, GUILayout.Width(size.x * 2), GUILayout.Height(size.y * 2));
                tempRect = GUILayoutUtility.GetLastRect();
                var newFilterText = GUI.TextField(tempRect, filterText, searchStyle);
                if (newFilterText != filterText)
                {
                    filterText = newFilterText;
                    CalculateCurrentLog();
                }
            }

            if (GUILayout.Button(infoContent, barStyle, GUILayout.Width(size.x * 2), GUILayout.Height(size.y * 2)))
                currentView = ViewType.Info;

            GUILayout.FlexibleSpace();

            var logsText = " ";
            if (collapse)
                logsText += numOfCollapsedLogs;
            else
                logsText += numOfLogs;
            var logsWarningText = " ";
            if (collapse)
                logsWarningText += numOfCollapsedLogsWarning;
            else
                logsWarningText += numOfLogsWarning;
            var logsErrorText = " ";
            if (collapse)
                logsErrorText += numOfCollapsedLogsError;
            else
                logsErrorText += numOfLogsError;

            GUILayout.BeginHorizontal(showLog ? buttonActiveStyle : barStyle);
            if (GUILayout.Button(logContent, nonStyle, GUILayout.Width(size.x * 2), GUILayout.Height(size.y * 2)))
            {
                showLog = !showLog;
                CalculateCurrentLog();
            }
            if (GUILayout.Button(logsText, nonStyle, GUILayout.Width(size.x * 2), GUILayout.Height(size.y * 2)))
            {
                showLog = !showLog;
                CalculateCurrentLog();
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal(showWarning ? buttonActiveStyle : barStyle);
            if (GUILayout.Button(warningContent, nonStyle, GUILayout.Width(size.x * 2), GUILayout.Height(size.y * 2)))
            {
                showWarning = !showWarning;
                CalculateCurrentLog();
            }
            if (GUILayout.Button(logsWarningText, nonStyle, GUILayout.Width(size.x * 2), GUILayout.Height(size.y * 2)))
            {
                showWarning = !showWarning;
                CalculateCurrentLog();
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal(showError ? buttonActiveStyle : nonStyle);
            if (GUILayout.Button(errorContent, nonStyle, GUILayout.Width(size.x * 2), GUILayout.Height(size.y * 2)))
            {
                showError = !showError;
                CalculateCurrentLog();
            }
            if (GUILayout.Button(logsErrorText, nonStyle, GUILayout.Width(size.x * 2), GUILayout.Height(size.y * 2)))
            {
                showError = !showError;
                CalculateCurrentLog();
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button(closeContent, barStyle, GUILayout.Width(size.x * 2), GUILayout.Height(size.y * 2)))
            {
                guiShow = false;
                SavePref();
            }

            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private Rect tempRect;

        private void DrawLogs()
        {
            GUILayout.BeginArea(logsRect, backStyle);

            GUI.skin = logScrollerSkin;
            var drag = GetDragPos();

            if (drag.y != 0 && logsRect.Contains(new Vector2(downPos.x, Screen.height - downPos.y)))
                scrollPosition.y += drag.y - oldDrag;
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            oldDrag = drag.y;

            var totalVisibleCount = (int)(Screen.height * 0.75f / size.y);
            var totalCount = currentLog.Count;

            totalVisibleCount = Mathf.Min(totalVisibleCount, totalCount - startIndex);
            var index = 0;
            var beforeHeight = (int)(startIndex * size.y);
            if (beforeHeight > 0)
            {
                GUILayout.BeginHorizontal(GUILayout.Height(beforeHeight));
                GUILayout.Label("---");
                GUILayout.EndHorizontal();
            }

            var endIndex = startIndex + totalVisibleCount;
            endIndex = Mathf.Clamp(endIndex, 0, totalCount);
            var scrollerVisible = totalVisibleCount < totalCount;
            for (var i = startIndex; startIndex + index < endIndex; i++)
            {
                if (i >= currentLog.Count)
                    break;
                var log = currentLog[i];

                if (log.Type == LogType.Log && !showLog)
                    continue;
                if (log.Type == LogType.Warning && !showWarning)
                    continue;
                if (log.Type == LogType.Error && !showError)
                    continue;
                if (log.Type == LogType.Assert && !showError)
                    continue;
                if (log.Type == LogType.Exception && !showError)
                    continue;

                if (index >= totalVisibleCount)
                    break;

                GUIContent content;
                if (log.Type == LogType.Log)
                    content = logContent;
                else if (log.Type == LogType.Warning)
                    content = warningContent;
                else
                    content = errorContent;

                var currentLogStyle = (startIndex + index) % 2 == 0 ? evenLogStyle : oddLogStyle;
                if (log == selectedLog)
                    currentLogStyle = selectedLogStyle;
                tempContent.text = log.Count.ToString();
                var w = 0f;
                if (collapse)
                    w = barStyle.CalcSize(tempContent).x + 3;
                countRect.x = Screen.width - w;
                countRect.y = size.y * i;
                if (beforeHeight > 0)
                    countRect.y += 8; //i will check later why
                countRect.width = w;
                countRect.height = size.y;

                if (scrollerVisible)
                    countRect.x -= size.x * 2;

                var sample = samples[log.SampleID];

                fpsRect = countRect;
                if (showFps)
                {
                    tempContent.text = sample.FPSText;
                    w = currentLogStyle.CalcSize(tempContent).x + size.x;
                    fpsRect.x -= w;
                    fpsRect.width = size.x;
                    fpsLabelRect = fpsRect;
                    fpsLabelRect.x += size.x;
                    fpsLabelRect.width = w - size.x;
                }

                memoryRect = fpsRect;
                if (showMemory)
                {
                    tempContent.text = sample.Memory.ToString("0.000");
                    w = currentLogStyle.CalcSize(tempContent).x + size.x;
                    memoryRect.x -= w;
                    memoryRect.width = size.x;
                    memoryLabelRect = memoryRect;
                    memoryLabelRect.x += size.x;
                    memoryLabelRect.width = w - size.x;
                }
                sceneRect = memoryRect;
                if (showScene)
                {
                    tempContent.text = sample.Scene;
                    w = currentLogStyle.CalcSize(tempContent).x + size.x;
                    sceneRect.x -= w;
                    sceneRect.width = size.x;
                    sceneLabelRect = sceneRect;
                    sceneLabelRect.x += size.x;
                    sceneLabelRect.width = w - size.x;
                }
                timeRect = sceneRect;
                if (showTime)
                {
                    tempContent.text = sample.Time.ToString("0.000");
                    w = currentLogStyle.CalcSize(tempContent).x + size.x;
                    timeRect.x -= w;
                    timeRect.width = size.x;
                    timeLabelRect = timeRect;
                    timeLabelRect.x += size.x;
                    timeLabelRect.width = w - size.x;
                }

                GUILayout.BeginHorizontal(currentLogStyle);
                if (log == selectedLog)
                {
                    GUILayout.Box(content, nonStyle, GUILayout.Width(size.x), GUILayout.Height(size.y));
                    GUILayout.Label(log.Condition, selectedLogFontStyle);
                    //GUILayout.FlexibleSpace();
                    if (showTime)
                    {
                        GUI.Box(timeRect, showTimeContent, currentLogStyle);
                        GUI.Label(timeLabelRect, sample.Time.ToString("0.000"), currentLogStyle);
                    }
                    if (showScene)
                    {
                        GUI.Box(sceneRect, showSceneContent, currentLogStyle);
                        GUI.Label(sceneLabelRect, sample.Scene, currentLogStyle);
                    }
                    if (showMemory)
                    {
                        GUI.Box(memoryRect, showMemoryContent, currentLogStyle);
                        GUI.Label(memoryLabelRect, sample.Memory.ToString("0.000") + " mb", currentLogStyle);
                    }
                    if (showFps)
                    {
                        GUI.Box(fpsRect, showFpsContent, currentLogStyle);
                        GUI.Label(fpsLabelRect, sample.FPSText, currentLogStyle);
                    }
                }
                else
                {
                    if (GUILayout.Button(content, nonStyle, GUILayout.Width(size.x), GUILayout.Height(size.y)))
                        selectedLog = log;
                    if (GUILayout.Button(log.Condition, logButtonStyle))
                        selectedLog = log;
                    //GUILayout.FlexibleSpace();
                    if (showTime)
                    {
                        GUI.Box(timeRect, showTimeContent, currentLogStyle);
                        GUI.Label(timeLabelRect, sample.Time.ToString("0.000"), currentLogStyle);
                    }
                    if (showScene)
                    {
                        GUI.Box(sceneRect, showSceneContent, currentLogStyle);
                        GUI.Label(sceneLabelRect, sample.Scene, currentLogStyle);
                    }
                    if (showMemory)
                    {
                        GUI.Box(memoryRect, showMemoryContent, currentLogStyle);
                        GUI.Label(memoryLabelRect, sample.Memory.ToString("0.000") + " mb", currentLogStyle);
                    }
                    if (showFps)
                    {
                        GUI.Box(fpsRect, showFpsContent, currentLogStyle);
                        GUI.Label(fpsLabelRect, sample.FPSText, currentLogStyle);
                    }
                }
                if (collapse)
                    GUI.Label(countRect, log.Count.ToString(), barStyle);
                GUILayout.EndHorizontal();
                index++;
            }

            var afterHeight = (int)((totalCount - (startIndex + totalVisibleCount)) * size.y);
            if (afterHeight > 0)
            {
                GUILayout.BeginHorizontal(GUILayout.Height(afterHeight));
                GUILayout.Label(" ");
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();

            buttomRect.x = 0f;
            buttomRect.y = Screen.height - size.y;
            buttomRect.width = Screen.width;
            buttomRect.height = size.y;

            if (showGraph)
                DrawGraph();
            else
                DrawStack();
        }

        private readonly float graphSize = 4f;
        private int startFrame;
        private int currentFrame;
        private Vector2 graphScrollerPos;
        private float maxFpsValue;
        private float minFpsValue;
        private float maxMemoryValue;
        private float minMemoryValue;

        private void DrawGraph()
        {
            graphRect = stackRect;
            graphRect.height = Screen.height * 0.25f;
            GUI.skin = graphScrollerSkin;
            var drag = GetDragPos();
            if (graphRect.Contains(new Vector2(downPos.x, Screen.height - downPos.y)))
            {
                if (drag.x != 0)
                {
                    graphScrollerPos.x -= drag.x - oldDrag3;
                    graphScrollerPos.x = Mathf.Max(0, graphScrollerPos.x);
                }

                var p = downPos;
                if (p != Vector2.zero)
                    currentFrame = startFrame + (int)(p.x / graphSize);
            }

            oldDrag3 = drag.x;
            GUILayout.BeginArea(graphRect, backStyle);

            graphScrollerPos = GUILayout.BeginScrollView(graphScrollerPos);
            startFrame = (int)(graphScrollerPos.x / graphSize);
            if (graphScrollerPos.x >= samples.Count * graphSize - Screen.width)
                graphScrollerPos.x += graphSize;

            GUILayout.Label(" ", GUILayout.Width(samples.Count * graphSize));
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            maxFpsValue = 0;
            minFpsValue = 100000;
            maxMemoryValue = 0;
            minMemoryValue = 100000;
            for (var i = 0; i < Screen.width / graphSize; i++)
            {
                var index = startFrame + i;
                if (index >= samples.Count)
                    break;
                var s = samples[index];
                if (maxFpsValue < s.FPS) maxFpsValue = s.FPS;
                if (minFpsValue > s.FPS) minFpsValue = s.FPS;
                if (maxMemoryValue < s.Memory) maxMemoryValue = s.Memory;
                if (minMemoryValue > s.Memory) minMemoryValue = s.Memory;
            }

            if (currentFrame != -1 && currentFrame < samples.Count)
            {
                var selectedSample = samples[currentFrame];
                GUILayout.BeginArea(buttomRect, backStyle);
                GUILayout.BeginHorizontal();

                GUILayout.Box(showTimeContent, nonStyle, GUILayout.Width(size.x), GUILayout.Height(size.y));
                GUILayout.Label(selectedSample.Time.ToString("0.0"), nonStyle);
                GUILayout.Space(size.x);

                GUILayout.Box(showSceneContent, nonStyle, GUILayout.Width(size.x), GUILayout.Height(size.y));
                GUILayout.Label(selectedSample.Scene, nonStyle);
                GUILayout.Space(size.x);

                GUILayout.Box(showMemoryContent, nonStyle, GUILayout.Width(size.x), GUILayout.Height(size.y));
                GUILayout.Label(selectedSample.Memory.ToString("0.000"), nonStyle);
                GUILayout.Space(size.x);

                GUILayout.Box(showFpsContent, nonStyle, GUILayout.Width(size.x), GUILayout.Height(size.y));
                GUILayout.Label(selectedSample.FPSText, nonStyle);
                GUILayout.Space(size.x);

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.EndArea();
            }

            graphMaxRect = stackRect;
            graphMaxRect.height = size.y;
            GUILayout.BeginArea(graphMaxRect);
            GUILayout.BeginHorizontal();

            GUILayout.Box(showMemoryContent, nonStyle, GUILayout.Width(size.x), GUILayout.Height(size.y));
            GUILayout.Label(maxMemoryValue.ToString("0.000"), nonStyle);

            GUILayout.Box(showFpsContent, nonStyle, GUILayout.Width(size.x), GUILayout.Height(size.y));
            GUILayout.Label(maxFpsValue.ToString("0.000"), nonStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            graphMinRect = stackRect;
            graphMinRect.y = stackRect.y + stackRect.height - size.y;
            graphMinRect.height = size.y;
            GUILayout.BeginArea(graphMinRect);
            GUILayout.BeginHorizontal();

            GUILayout.Box(showMemoryContent, nonStyle, GUILayout.Width(size.x), GUILayout.Height(size.y));

            GUILayout.Label(minMemoryValue.ToString("0.000"), nonStyle);

            GUILayout.Box(showFpsContent, nonStyle, GUILayout.Width(size.x), GUILayout.Height(size.y));

            GUILayout.Label(minFpsValue.ToString("0.000"), nonStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawStack()
        {
            if (selectedLog != null)
            {
                var drag = GetDragPos();
                if (drag.y != 0 && stackRect.Contains(new Vector2(downPos.x, Screen.height - downPos.y)))
                    scrollPosition2.y += drag.y - oldDrag2;
                oldDrag2 = drag.y;

                GUILayout.BeginArea(stackRect, backStyle);
                scrollPosition2 = GUILayout.BeginScrollView(scrollPosition2);
                Sample selectedSample = null;
                try
                {
                    selectedSample = samples[selectedLog.SampleID];
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label(selectedLog.Condition, stackLabelStyle);
                GUILayout.EndHorizontal();
                GUILayout.Space(size.y * 0.25f);
                GUILayout.BeginHorizontal();
                GUILayout.Label(selectedLog.Stacktrace, stackLabelStyle);
                GUILayout.EndHorizontal();
                GUILayout.Space(size.y);
                GUILayout.EndScrollView();
                GUILayout.EndArea();

                GUILayout.BeginArea(buttomRect, backStyle);
                GUILayout.BeginHorizontal();

                GUILayout.Box(showTimeContent, nonStyle, GUILayout.Width(size.x), GUILayout.Height(size.y));
                if (selectedSample != null)
                {
                    GUILayout.Label(selectedSample.Time.ToString("0.000"), nonStyle);
                    GUILayout.Space(size.x);

                    GUILayout.Box(showSceneContent, nonStyle, GUILayout.Width(size.x), GUILayout.Height(size.y));
                    GUILayout.Label(selectedSample.Scene, nonStyle);
                    GUILayout.Space(size.x);

                    GUILayout.Box(showMemoryContent, nonStyle, GUILayout.Width(size.x), GUILayout.Height(size.y));
                    GUILayout.Label(selectedSample.Memory.ToString("0.000"), nonStyle);
                    GUILayout.Space(size.x);

                    GUILayout.Box(showFpsContent, nonStyle, GUILayout.Width(size.x), GUILayout.Height(size.y));
                    GUILayout.Label(selectedSample.FPSText, nonStyle);
                }

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.EndArea();
            }
            else
            {
                GUILayout.BeginArea(stackRect, backStyle);
                GUILayout.EndArea();
                GUILayout.BeginArea(buttomRect, backStyle);
                GUILayout.EndArea();
            }
        }

        public void OnGUI()
        {
            if (guiShow)
            {
                UpdateGCAndFPS();
                screenRect.x = 0;
                screenRect.y = 0;
                screenRect.width = Screen.width;
                screenRect.height = Screen.height;

                GetDownPos();

                logsRect.x = 0f;
                logsRect.y = size.y * 2f;
                logsRect.width = Screen.width;
                logsRect.height = Screen.height * 0.75f - size.y * 2f;

                stackRectTopLeft.x = 0f;
                stackRect.x = 0f;
                stackRectTopLeft.y = Screen.height * 0.75f;
                stackRect.y = Screen.height * 0.75f;
                stackRect.width = Screen.width;
                stackRect.height = Screen.height * 0.25f - size.y;

                detailRect.x = 0f;
                detailRect.y = Screen.height - size.y * 3;
                detailRect.width = Screen.width;
                detailRect.height = size.y * 3;

                if (currentView == ViewType.Info)
                {
                    DrawInfo();
                }
                else if (currentView == ViewType.Logs)
                {
                    DrawToolBar();
                    DrawLogs();
                }
            }
        }

        private readonly List<Vector2> gestureDetector = new List<Vector2>();
        private Vector2 gestureSum = Vector2.zero;
        private float gestureLength;
        private int gestureCount;

        private bool IsGestureDone()
        {
            if (guiEnabled)
            {
                if (Application.platform == RuntimePlatform.Android ||
                    Application.platform == RuntimePlatform.IPhonePlayer)
                {
                    if (Input.touches.Length != 1)
                    {
                        gestureDetector.Clear();
                        gestureCount = 0;
                    }
                    else
                    {
                        if (Input.touches[0].phase == TouchPhase.Canceled || Input.touches[0].phase == TouchPhase.Ended)
                        {
                            gestureDetector.Clear();
                        }
                        else if (Input.touches[0].phase == TouchPhase.Moved)
                        {
                            var p = Input.touches[0].position;
                            if (gestureDetector.Count == 0 ||
                                (p - gestureDetector[gestureDetector.Count - 1]).magnitude > 10)
                                gestureDetector.Add(p);
                        }
                    }
                }
                else
                {
                    if (Input.GetMouseButtonUp(0))
                    {
                        gestureDetector.Clear();
                        gestureCount = 0;
                    }
                    else
                    {
                        if (Input.GetMouseButton(0))
                        {
                            var p = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                            if (gestureDetector.Count == 0 ||
                                (p - gestureDetector[gestureDetector.Count - 1]).magnitude > 10)
                                gestureDetector.Add(p);
                        }
                    }
                }

                if (gestureDetector.Count < 10)
                    return false;

                gestureSum = Vector2.zero;
                gestureLength = 0;
                var prevDelta = Vector2.zero;
                for (var i = 0; i < gestureDetector.Count - 2; i++)
                {
                    var delta = gestureDetector[i + 1] - gestureDetector[i];
                    var deltaLength = delta.magnitude;
                    gestureSum += delta;
                    gestureLength += deltaLength;

                    var dot = Vector2.Dot(delta, prevDelta);
                    if (dot < 0f)
                    {
                        gestureDetector.Clear();
                        gestureCount = 0;
                        return false;
                    }

                    prevDelta = delta;
                }

                var gestureBase = (Screen.width + Screen.height) / 4;

                if (gestureLength > gestureBase && gestureSum.magnitude < gestureBase / 2.0f)
                {
                    gestureDetector.Clear();
                    gestureCount++;
                    if (gestureCount >= numOfCircleToShow)
                        return true;
                }
            }
            return false;
        }

        private Vector2 downPos;

        private Vector2 GetDownPos()
        {
            if (Application.isMobilePlatform)
            {
                if (Input.touches.Length == 1 && Input.touches[0].phase == TouchPhase.Began)
                {
                    downPos = Input.touches[0].position;
                    return downPos;
                }
            }
            else
            {
                if (Input.GetMouseButtonDown(0))
                {
                    downPos.x = Input.mousePosition.x;
                    downPos.y = Input.mousePosition.y;
                    return downPos;
                }
            }
            return Vector2.zero;
        }

        private Vector2 mousePosition;

        private Vector2 GetDragPos()
        {
            if (Application.platform == RuntimePlatform.Android ||
                Application.platform == RuntimePlatform.IPhonePlayer)
            {
                if (Input.touches.Length != 1)
                    return Vector2.zero;
                return Input.touches[0].position - downPos;
            }
            if (Input.GetMouseButton(0))
            {
                mousePosition = Input.mousePosition;
                return mousePosition - downPos;
            }
            return Vector2.zero;
        }

        private void CalculateStartIndex()
        {
            startIndex = (int)(scrollPosition.y / size.y);
            startIndex = Mathf.Clamp(startIndex, 0, currentLog.Count);
        }

        private int frames;
        private bool firstTime = true;
        private float lastUpdate;
        private const int requiredFrames = 10;
        private const float updateInterval = 0.25f;

        private void Update()
        {
            try
            {
                CalculateStartIndex();
                if (!guiShow && IsGestureDone())
                {
                    guiShow = true;
                    currentView = ViewType.Logs;
                }
                if (threadedLogs.Count > 0)
                {
                    lock (threadedLogs)
                    {
                        for (var i = 0; i < threadedLogs.Count; i++)
                        {
                            var l = threadedLogs[i];
                            AddLog(l.Condition, l.Stacktrace, l.Type);
                        }
                        threadedLogs.Clear();
                    }
                }
                if (firstTime)
                {
                    firstTime = false;
                    lastUpdate = Time.realtimeSinceStartup;
                    frames = 0;
                    return;
                }
                frames++;
                var dt = Time.realtimeSinceStartup - lastUpdate;
                if (dt > updateInterval && frames > requiredFrames)
                {
                    fps = frames / dt;
                    lastUpdate = Time.realtimeSinceStartup;
                    frames = 0;
                }
            }
            catch { }
        }

        private void UpdateGCAndFPS()
        {
            fpsText = fps.ToString("0.000");
            gcTotalMemory = (float)GC.GetTotalMemory(false) / 1024 / 1024;
        }

        private void CaptureLog(string condition, string stacktrace, LogType type)
        {
            AddLog(condition, stacktrace, type);
        }

        private void AddLog(string condition, string stacktrace, LogType type)
        {
            UpdateGCAndFPS();
            var memUsage = 0f;
            string value;
            if (cachedString.ContainsKey(condition))
            {
                value = cachedString[condition];
            }
            else
            {
                value = condition;
                cachedString.Add(value, value);
                memUsage += string.IsNullOrEmpty(value) ? 0 : value.Length * sizeof(char);
                memUsage += IntPtr.Size;
            }
            string _stacktrace;
            if (cachedString.ContainsKey(stacktrace))
            {
                _stacktrace = cachedString[stacktrace];
            }
            else
            {
                _stacktrace = stacktrace;
                cachedString.Add(_stacktrace, _stacktrace);
                memUsage += string.IsNullOrEmpty(_stacktrace) ? 0 : _stacktrace.Length * sizeof(char);
                memUsage += IntPtr.Size;
            }
            var newLogAdded = false;

            AddSample();
            var log = new Log
            {
                Type = type,
                Condition = value,
                Stacktrace = _stacktrace,
                SampleID = samples.Count - 1
            };
            memUsage += log.Size();

            logsMemUsage += memUsage / 1024 / 1024;

            bool isNew;
            if (logsDic.ContainsKey(value, stacktrace))
            {
                isNew = false;
                logsDic[value][stacktrace].Count++;
            }
            else
            {
                isNew = true;
                collapsedLogs.Add(log);
                logsDic[value][stacktrace] = log;

                if (type == LogType.Log)
                    numOfCollapsedLogs++;
                else if (type == LogType.Warning)
                    numOfCollapsedLogsWarning++;
                else
                    numOfCollapsedLogsError++;
            }

            if (type == LogType.Log)
                numOfLogs++;
            else if (type == LogType.Warning)
                numOfLogsWarning++;
            else
                numOfLogsError++;

            logs.Add(log);
            if (!collapse || isNew)
            {
                bool skip = log.Type == LogType.Log && !showLog;
                if (log.Type == LogType.Warning && !showWarning)
                    skip = true;
                if (log.Type == LogType.Error && !showError)
                    skip = true;
                if (log.Type == LogType.Assert && !showError)
                    skip = true;
                if (log.Type == LogType.Exception && !showError)
                    skip = true;

                if (!skip)
                    if (string.IsNullOrEmpty(filterText) || log.Condition.ToLower().Contains(filterText.ToLower()))
                    {
                        currentLog.Add(log);
                        newLogAdded = true;
                    }
            }
            if (newLogAdded)
            {
                CalculateStartIndex();
                var totalCount = currentLog.Count;
                var totalVisibleCount = (int)(Screen.height * 0.75f / size.y);
                if (startIndex >= totalCount - totalVisibleCount)
                    scrollPosition.y += size.y;
            }
            CheckLog(log, isNew);
        }

        private void CheckLog(Log log, bool isNew)
        {
            if (log.Type == LogType.Exception)
            {
                try
                {
                    string logstr = log.ToString();
                    if (isNew)
                    {
                        if (File.Exists(LOG_EXCEPTION))
                        {
                            byte[] bytes = File.ReadAllBytes(LOG_EXCEPTION);
                            if (bytes.Length > EXCEPTION_FILE_MAX)
                            {
                                Helper.DeleteFile(LOG_EXCEPTION);
                            }
                        }
                        using (var file = File.Open(LOG_EXCEPTION, FileMode.Append))
                        {
                            var sw = new StreamWriter(file);
                            if (file.Length == 0)
                            {
                                sw.WriteLine(Helper.StringFormat("[Device:{0}][OS:{1}][ID:{2}]", deviceModel, SystemInfo.operatingSystem, SystemInfo.deviceUniqueIdentifier));
                                sw.WriteLine(Helper.StringFormat("[Graphics:{0}({1}-{2})][Screen:{3}x{4}]",
                                SystemInfo.graphicsDeviceName, SystemInfo.graphicsMemorySize, SystemInfo.maxTextureSize,
                                Screen.width, Screen.height));
                                sw.WriteLine();
                            }
                            sw.WriteLine(logstr);
                            sw.Close();
                            file.Close();
                        }
                        if (!Constants.RELEASE_MODE && !Constants.LIVE_MODE)
                        {
                            CommitException();
                        }
                    }
                    OnException?.Invoke(logstr, isNew);
                }
                catch { }
            }
            verboseLogSize += log.Size();
            if (verboseLogSize >= LOG_MEM_MAX)
            {
                SaveVerbose();
            }
        }

        public static void SaveVerbose()
        {
            if (!Constants.RELEASE_MODE)
            {
                try
                {
                    using (var file = File.Open(LOG_VERBOSE, FileMode.Append))
                    {
                        var sw = new StreamWriter(file);
                        if (file.Length == 0)
                        {
                            sw.WriteLine(Helper.StringFormat("[Device:{0}][OS:{1}][ID:{2}]", Instance.deviceModel, SystemInfo.operatingSystem, SystemInfo.deviceUniqueIdentifier));
                            sw.WriteLine(Helper.StringFormat("[Graphics:{0}({1}-{2})][Screen:{3}x{4}]",
                            SystemInfo.graphicsDeviceName, SystemInfo.graphicsMemorySize, SystemInfo.maxTextureSize,
                            Screen.width, Screen.height));
                            sw.WriteLine();
                        }
                        for (int i = 0; i < Instance.logs.Count; i++)
                        {
                            sw.WriteLine(Instance.logs[i].ToString());
                        }
                        sw.Close();
                        file.Close();
                    }
                    Instance.Clear();
                }
                catch { }
            }
        }

        private readonly List<Log> threadedLogs = new List<Log>();

        private void CaptureLogThread(string condition, string stacktrace, LogType type)
        {
            var log = new Log { Condition = condition, Stacktrace = stacktrace, Type = type };
            lock (threadedLogs)
            {
                threadedLogs.Add(log);
            }
        }

        private void OnSceneWasLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            if (clearOnNewSceneLoaded)
                Clear();
            currentScene = scene.name;
        }

        private void SavePref()
        {
            PlayerPrefs.SetInt("Reporter_currentView", (int)currentView);
            PlayerPrefs.SetInt("Reporter_show", guiShow ? 1 : 0);
            PlayerPrefs.SetInt("Reporter_collapse", collapse ? 1 : 0);
            PlayerPrefs.SetInt("Reporter_clearOnNewSceneLoaded", clearOnNewSceneLoaded ? 1 : 0);
            PlayerPrefs.SetInt("Reporter_showTime", showTime ? 1 : 0);
            PlayerPrefs.SetInt("Reporter_showScene", showScene ? 1 : 0);
            PlayerPrefs.SetInt("Reporter_showMemory", showMemory ? 1 : 0);
            PlayerPrefs.SetInt("Reporter_showFps", showFps ? 1 : 0);
            PlayerPrefs.SetInt("Reporter_showGraph", showGraph ? 1 : 0);
            PlayerPrefs.SetInt("Reporter_showLog", showLog ? 1 : 0);
            PlayerPrefs.SetInt("Reporter_showWarning", showWarning ? 1 : 0);
            PlayerPrefs.SetInt("Reporter_showError", showError ? 1 : 0);
            PlayerPrefs.SetString("Reporter_filterText", filterText);
            PlayerPrefs.SetFloat("Reporter_size", size.x);
            PlayerPrefs.SetInt("Reporter_showClearOnNewSceneLoadedButton", showClearOnNewSceneLoadedButton ? 1 : 0);
            PlayerPrefs.SetInt("Reporter_showTimeButton", showTimeButton ? 1 : 0);
            PlayerPrefs.SetInt("Reporter_showSceneButton", showSceneButton ? 1 : 0);
            PlayerPrefs.SetInt("Reporter_showMemButton", showMemButton ? 1 : 0);
            PlayerPrefs.SetInt("Reporter_showFpsButton", showFpsButton ? 1 : 0);
            PlayerPrefs.SetInt("Reporter_showSearchText", showSearchText ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}

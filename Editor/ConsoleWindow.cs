using System;
using System.Globalization;
using UnityEngine;
using UnityEditor;
using UnityEngine.Experimental.Networking.PlayerConnection;
using UnityEditor.Experimental.Networking.PlayerConnection;
using ConnectionGUILayout = UnityEditor.Experimental.Networking.PlayerConnection.EditorGUILayout;
using EditorGUI = UnityEditor.EditorGUI;
using EditorGUILayout = UnityEditor.EditorGUILayout;
using EditorGUIUtility = UnityEditor.EditorGUIUtility;
using System.IO;
using static ConsoleTiny.ConsoleParameters;

namespace ConsoleTiny
{
    [EditorWindowTitle(title = "Console", useTypeNameAsIconName = true)]
    public class ConsoleWindow : EditorWindow, IHasCustomMenu
    {
        #region Static

        #endregion 


        #region Window

        /// <summary>
        /// 单例
        /// </summary>
        static ConsoleWindow ms_ConsoleWindow = null;

        /// <summary>
        /// 打开控制台
        /// </summary>
        [MenuItem("Window/General/ConsoleT %#t", false, 7)]
        static void ShowConsole()
        {
            GetWindow<ConsoleWindow>();
        }

        void OnEnable()
        {
            ms_ConsoleWindow = this;
            titleContent.text = "Console T";

            Application.logMessageReceived += DoLogChanged;

            position = new Rect(200, 200, 800, 400);
            logListView = new ListViewState(0, 0);
            messageListView = new ListViewState(0, 14);
            m_StacktraceLineContextClickRow = -1;


            if (m_ConsoleAttachToPlayerState == null)
                m_ConsoleAttachToPlayerState = new ConsoleAttachToPlayerState(this);

            m_DevBuild = Unsupported.IsDeveloperMode();
            LogEntries.wrapped.searchHistory = m_SearchHistory;
        }

        void OnDisable()
        {
            Application.logMessageReceived -= DoLogChanged;
            ms_ConsoleWindow = null;

            m_ConsoleAttachToPlayerState?.Dispose();
            m_ConsoleAttachToPlayerState = null;
            m_SearchHistory = LogEntries.wrapped.searchHistory;
        }

        public void DoLogChanged(string logString, string stackTrace, LogType type)
        {
            if (ms_ConsoleWindow == null)
                return;
            ms_ConsoleWindow.m_NextRepaint = EditorApplication.timeSinceStartup + 0.25f;
        }

        void OnInspectorUpdate()
        {
            if (EditorApplication.timeSinceStartup > m_NextRepaint)
            {
                m_NextRepaint = double.MaxValue;
                Repaint();
            }
        }
        #endregion

        #region Parameters

        /// <summary>
        /// 是否刷新了GUIStyle
        /// </summary>
        bool m_HasUpdatedGuiStyles = false;

        /// <summary>
        /// 显示信息列表
        /// </summary>
        ListViewState logListView;
        /// <summary>
        /// 信息详情列表
        /// </summary>
        ListViewState messageListView;

        bool m_DevBuild;
        private string[] m_SearchHistory = new[] { "" };

        private double m_NextRepaint = double.MaxValue;

        SplitterState spl = new SplitterState(new float[] { 70, 30 }, new int[] { 32, 32 }, null);

        int ms_LVHeight = 0;

        IConnectionState m_ConsoleAttachToPlayerState;

        #endregion

        #region Style

        /// <summary>
        /// 行高
        /// </summary>
        int m_LineHeight;
        /// <summary>
        /// 边框高度
        /// </summary>
        int m_BorderHeight;

        /// <summary>
        /// log列表行高
        /// </summary>
        private int RowHeight => (LogStyleLineCount * m_LineHeight) + m_BorderHeight;

        #endregion







        #region Tools
        /// <summary>
        /// 判断控制台全部标记是否包含目标标记
        /// </summary>
        /// <param name="flags"></param>
        /// <returns></returns>
        private static bool HasFlag(ConsoleFlags flags)
        {
            return (UnityEditor.LogEntries.consoleFlags & (int)flags) != 0;
        }

        /// <summary>
        /// 设置控制台标记
        /// </summary>
        /// <param name="flags"></param>
        /// <param name="val"></param>
        private static void SetFlag(ConsoleFlags flags, bool val)
        {
            UnityEditor.LogEntries.SetConsoleFlag((int)flags, val);
        }

        /// <summary>
        /// 获取目标GUIStyle
        /// </summary>
        /// <param name="flags"></param>
        /// <param name="isIcon"></param>
        /// <param name="isSmall"></param>
        /// <returns></returns>
        private static GUIStyle GetStyleForErrorMode(ConsoleFlags flags, bool isIcon, bool isSmall)
        {
            // Errors
            if (flags == ConsoleFlags.LogLevelError)
            {
                if (isIcon)
                {
                    if (isSmall)
                    {
                        return IconErrorSmallStyle;
                    }
                    return IconErrorStyle;
                }

                if (isSmall)
                {
                    return ErrorSmallStyle;
                }
                return ErrorStyle;
            }
            // Warnings
            if (flags == ConsoleFlags.LogLevelWarning)
            {
                if (isIcon)
                {
                    if (isSmall)
                    {
                        return IconWarningSmallStyle;
                    }
                    return IconWarningStyle;
                }

                if (isSmall)
                {
                    return WarningSmallStyle;
                }
                return WarningStyle;
            }
            // Logs
            if (isIcon)
            {
                if (isSmall)
                {
                    return IconLogSmallStyle;
                }
                return IconLogStyle;
            }

            if (isSmall)
            {
                return LogSmallStyle;
            }

            return ConsoleParameters.LogStyle;
        }

        /// <summary>
        /// 刷新列表视图
        /// </summary>
        void UpdateListView()
        {
            m_HasUpdatedGuiStyles = true;
            int newRowHeight = RowHeight;

            // We reset the scroll list to auto scrolling whenever the log entry count is modified
            logListView.rowHeight = 32;
            logListView.row = -1;
            logListView.scrollPos.y = LogEntries.wrapped.GetCount() * newRowHeight;
        }
        #endregion 


        #region 点击索引到游戏物体
        /// <summary>
        /// 上一个选中的索引
        /// </summary>
        private int m_ActiveInstanceID = 0;

        /// <summary>
        /// 设置点击入口，如果该输出包含游戏物体信息，选中输出目标消息的游戏物体
        /// 如果反复选择同一个输出，或目标游戏物体相同，则不会重复选择
        /// </summary>
        /// <param name="selectedIndex">选中的信息的id</param>
        void SetActiveEntry(int selectedIndex)
        {
            messageListView.row = -1;
            messageListView.scrollPos.y = 0;
            if (selectedIndex != -1)
            {
                var instanceID = LogEntries.wrapped.SetSelectedEntry(selectedIndex);
                // ping object referred by the log entry
                if (m_ActiveInstanceID != instanceID)
                {
                    m_ActiveInstanceID = instanceID;
                    if (instanceID != 0)
                        EditorGUIUtility.PingObject(instanceID);
                }
            }
        }
        #endregion


        /// <summary>
        /// 主要的控制台渲染在这里进行
        /// </summary>
        void OnGUI()
        {
            Event e = Event.current;
            LogEntries.wrapped.UpdateEntries();

            if (!m_HasUpdatedGuiStyles)
            {
                m_LineHeight = Mathf.RoundToInt(ErrorStyle.lineHeight);
                m_BorderHeight = ErrorStyle.border.top + ErrorStyle.border.bottom;
                UpdateListView();
            }

            #region Horizontal
            GUILayout.BeginHorizontal(ConsoleParameters.Toolbar);//开启一个横向的布局（有点没搞懂）

            if (GUILayout.Button(ClearLabel, MiniButton))
            {
                LogEntries.Clear();
                GUIUtility.keyboardControl = 0;
            }

            int currCount = LogEntries.wrapped.GetCount();

            if (logListView.totalRows != currCount && logListView.totalRows > 0)
            {
                // scroll bar was at the bottom?
                if (logListView.scrollPos.y >= logListView.rowHeight * logListView.totalRows - ms_LVHeight)
                {
                    logListView.scrollPos.y = currCount * RowHeight - ms_LVHeight;
                }
            }

            if (LogEntries.wrapped.searchFrame)
            {
                LogEntries.wrapped.searchFrame = false;
                int selectedIndex = LogEntries.wrapped.GetSelectedEntryIndex();
                if (selectedIndex != -1)
                {
                    int showIndex = selectedIndex + 1;
                    if (currCount > showIndex)
                    {
                        int showCount = ms_LVHeight / RowHeight;
                        showIndex = showIndex + showCount / 2;
                    }
                    logListView.scrollPos.y = showIndex * RowHeight - ms_LVHeight;
                }
            }

            EditorGUILayout.Space();

            bool wasCollapsed = LogEntries.wrapped.collapse;
            LogEntries.wrapped.collapse = GUILayout.Toggle(wasCollapsed, CollapseLabel, MiniButton);

            bool collapsedChanged = wasCollapsed != LogEntries.wrapped.collapse;
            if (collapsedChanged)
            {
                // unselect if collapsed flag changed
                logListView.row = -1;

                // scroll to bottom
                logListView.scrollPos.y = LogEntries.wrapped.GetCount() * RowHeight;
            }

            SetFlag(ConsoleFlags.ClearOnPlay, GUILayout.Toggle(HasFlag(ConsoleFlags.ClearOnPlay), ClearOnPlayLabel, MiniButton));

#if UNITY_2019_1_OR_NEWER
            SetFlag(ConsoleFlags.ClearOnBuild, GUILayout.Toggle(HasFlag(ConsoleFlags.ClearOnBuild), Constants.ClearOnBuildLabel, Constants.MiniButton));
#endif
            SetFlag(ConsoleFlags.ErrorPause, GUILayout.Toggle(HasFlag(ConsoleFlags.ErrorPause), ErrorPauseLabel, MiniButton));

            ConnectionGUILayout.AttachToPlayerDropdown(m_ConsoleAttachToPlayerState, EditorStyles.toolbarDropDown);

            EditorGUILayout.Space();

            if (m_DevBuild)
            {
                GUILayout.FlexibleSpace();
                SetFlag(ConsoleFlags.StopForAssert, GUILayout.Toggle(HasFlag(ConsoleFlags.StopForAssert), StopForAssertLabel, MiniButton));
                SetFlag(ConsoleFlags.StopForError, GUILayout.Toggle(HasFlag(ConsoleFlags.StopForError), StopForErrorLabel, MiniButton));
            }

            GUILayout.FlexibleSpace();

            // Search bar
            GUILayout.Space(4f);
            SearchField(e);

            int errorCount = 0, warningCount = 0, logCount = 0;
            LogEntries.wrapped.GetCountsByType(ref errorCount, ref warningCount, ref logCount);
            EditorGUI.BeginChangeCheck();
            bool setLogFlag = GUILayout.Toggle(LogEntries.wrapped.HasFlag((int)ConsoleFlags.LogLevelLog), new GUIContent((logCount <= 999 ? logCount.ToString() : "999+"), logCount > 0 ? iconInfoSmall : iconInfoMono), MiniButton);
            bool setWarningFlag = GUILayout.Toggle(LogEntries.wrapped.HasFlag((int)ConsoleFlags.LogLevelWarning), new GUIContent((warningCount <= 999 ? warningCount.ToString() : "999+"), warningCount > 0 ? iconWarnSmall : iconWarnMono), MiniButton);
            bool setErrorFlag = GUILayout.Toggle(LogEntries.wrapped.HasFlag((int)ConsoleFlags.LogLevelError), new GUIContent((errorCount <= 999 ? errorCount.ToString() : "999+"), errorCount > 0 ? iconErrorSmall : iconErrorMono), MiniButton);

            LogEntries.wrapped.SetFlag((int)ConsoleFlags.LogLevelLog, setLogFlag);
            LogEntries.wrapped.SetFlag((int)ConsoleFlags.LogLevelWarning, setWarningFlag);
            LogEntries.wrapped.SetFlag((int)ConsoleFlags.LogLevelError, setErrorFlag);

            if (GUILayout.Button(new GUIContent(errorCount > 0 ? iconFirstErrorSmall : iconFirstErrorMono, FirstErrorLabel), MiniButton))
            {
                int firstErrorIndex = LogEntries.wrapped.GetFirstErrorEntryIndex();
                if (firstErrorIndex != -1)
                {
                    SetActiveEntry(firstErrorIndex);
                    LogEntries.wrapped.searchFrame = true;
                }
            }

            GUILayout.EndHorizontal();
            #endregion


            SplitterGUILayout.BeginVerticalSplit(spl);
            int rowHeight = RowHeight;
            EditorGUIUtility.SetIconSize(new Vector2(rowHeight, rowHeight));
            GUIContent tempContent = new GUIContent();
            int id = GUIUtility.GetControlID(0);
            int rowDoubleClicked = -1;

            /////@TODO: Make Frame selected work with ListViewState
            using (new GettingLogEntriesScope(logListView))
            {
                int selectedRow = -1;
                bool openSelectedItem = false;
                bool collapsed = LogEntries.wrapped.collapse;
                foreach (ListViewElement el in ListViewGUI.ListView(logListView, Box))
                {
                    if (e.type == EventType.MouseDown && e.button == 0 && el.position.Contains(e.mousePosition))
                    {
                        logListView.row = el.row;
                        selectedRow = el.row;
                        if (e.clickCount == 2)
                            openSelectedItem = true;
                    }
                    else if (e.type == EventType.Repaint)
                    {
                        int mode = 0;
                        int entryCount = 0;
                        int searchIndex = 0;
                        int searchEndIndex = 0;
                        string text = LogEntries.wrapped.GetEntryLinesAndFlagAndCount(el.row, ref mode, ref entryCount,
                            ref searchIndex, ref searchEndIndex);
                        ConsoleFlags flag = (ConsoleFlags)mode;
                        bool isSelected = LogEntries.wrapped.IsEntrySelected(el.row);

                        // Draw the background
                        GUIStyle s = el.row % 2 == 0 ? OddBackground : EvenBackground;
                        s.Draw(el.position, true, false, isSelected, false);

                        // Draw the icon
                        GUIStyle iconStyle = GetStyleForErrorMode(flag, true, LogStyleLineCount == 1);
                        iconStyle.fixedWidth = 100;
                        iconStyle.Draw(el.position, false, false, isSelected, false);




                        // Draw the text
                        tempContent.text = text;
                        GUIStyle errorModeStyle = GetStyleForErrorMode(flag, false, LogStyleLineCount == 1);

                        if (string.IsNullOrEmpty(LogEntries.wrapped.searchString) || searchIndex == -1 || searchIndex >= text.Length)
                        {
                            Rect v2 = el.position;
                            v2.x += 100;
                            errorModeStyle.Draw(v2, tempContent, id, isSelected);
                        }
                        else
                        {
                            errorModeStyle.DrawWithTextSelection(el.position, tempContent, GUIUtility.keyboardControl, searchIndex, searchEndIndex);
                        }

                        if (collapsed)
                        {
                            Rect badgeRect = el.position;
                            tempContent.text = entryCount.ToString(CultureInfo.InvariantCulture);
                            Vector2 badgeSize = CountBadge.CalcSize(tempContent);
                            badgeRect.xMin = badgeRect.xMax - badgeSize.x;
                            badgeRect.yMin += ((badgeRect.yMax - badgeRect.yMin) - badgeSize.y) * 0.5f;
                            badgeRect.x -= 5f;
                            GUI.Label(badgeRect, tempContent, CountBadge);
                        }

                        Rect iconRect = el.position;
                        iconRect.size = new Vector2(100, 100);
                        GUIStyle style = "Icon.Clip";
                        //GUI.Label(iconRect, m_Tex, style);

                    }
                }

                if (selectedRow != -1)
                {
                    if (logListView.scrollPos.y >= logListView.rowHeight * logListView.totalRows - ms_LVHeight)
                        logListView.scrollPos.y = logListView.rowHeight * logListView.totalRows - ms_LVHeight - 1;
                }

                // Make sure the selected entry is up to date
                if (logListView.totalRows == 0 || logListView.row >= logListView.totalRows || logListView.row < 0)
                {
                }
                else
                {
                    if (logListView.selectionChanged)
                    {
                        SetActiveEntry(logListView.row);
                    }
                }

                // Open entry using return key
                if ((GUIUtility.keyboardControl == logListView.ID) && (e.type == EventType.KeyDown) && (e.keyCode == KeyCode.Return) && (logListView.row != 0))
                {
                    selectedRow = logListView.row;
                    openSelectedItem = true;
                }

                if (e.type != EventType.Layout && ListViewGUI.ilvState.rectHeight != 1)
                    ms_LVHeight = ListViewGUI.ilvState.rectHeight;

                if (openSelectedItem)
                {
                    rowDoubleClicked = selectedRow;
                    e.Use();
                }

                if (selectedRow != -1)
                {
                    SetActiveEntry(selectedRow);
                }
            }

            // Prevent dead locking in EditorMonoConsole by delaying callbacks (which can log to the console) until after LogEntries.EndGettingEntries() has been
            // called (this releases the mutex in EditorMonoConsole so logging again is allowed). Fix for case 1081060.
            if (rowDoubleClicked != -1)
                LogEntries.wrapped.StacktraceListView_RowGotDoubleClicked();

            EditorGUIUtility.SetIconSize(Vector2.zero);

            StacktraceListView(e, tempContent);

            SplitterGUILayout.EndVerticalSplit();

            // Copy & Paste selected item
            if ((e.type == EventType.ValidateCommand || e.type == EventType.ExecuteCommand) && e.commandName == "Copy")
            {
                if (e.type == EventType.ExecuteCommand)
                    LogEntries.wrapped.StacktraceListView_CopyAll();
                e.Use();
            }
        }

        private void SearchField(Event e)
        {
            string searchBarName = "SearchFilter";
            if (e.commandName == "Find")
            {
                if (e.type == EventType.ExecuteCommand)
                {
                    EditorGUI.FocusTextInControl(searchBarName);
                }

                if (e.type != EventType.Layout)
                    e.Use();
            }

            string searchText = LogEntries.wrapped.searchString;
            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Escape)
                {
                    searchText = string.Empty;
                    GUIUtility.keyboardControl = logListView.ID;
                    Repaint();
                }
                else if ((e.keyCode == KeyCode.UpArrow || e.keyCode == KeyCode.DownArrow) &&
                         GUI.GetNameOfFocusedControl() == searchBarName)
                {
                    GUIUtility.keyboardControl = logListView.ID;
                }
            }

            GUI.SetNextControlName(searchBarName);
            Rect rect = GUILayoutUtility.GetRect(0, EditorGUILayout.kLabelFloatMaxW * 1.5f, EditorGUI.kSingleLineHeight,
                EditorGUI.kSingleLineHeight, EditorStyles.toolbarSearchField, GUILayout.MinWidth(100),
                GUILayout.MaxWidth(300));

            bool showHistory = LogEntries.wrapped.searchHistory[0].Length != 0;
            Rect popupPosition = rect;
            popupPosition.width = 20;
            if (showHistory && Event.current.type == EventType.MouseDown && popupPosition.Contains(Event.current.mousePosition))
            {
                GUIUtility.keyboardControl = 0;
                EditorUtility.DisplayCustomMenu(rect, EditorGUIUtility.TempContent(LogEntries.wrapped.searchHistory), -1, OnSetFilteringHistoryCallback, null);
                Event.current.Use();
            }

            LogEntries.wrapped.searchString = EditorGUI.ToolbarSearchField(

                rect, searchText, showHistory);

            if (GUILayout.Button(new GUIContent(iconCustomFiltersMono, CustomFiltersLabel), EditorStyles.toolbarDropDown))
            {
                Rect buttonRect = rect;
                buttonRect.x += buttonRect.width;
                var menuData = new CustomFiltersItemProvider(LogEntries.wrapped.customFilters);
                var flexibleMenu = new FlexibleMenu(menuData, -1, new CustomFiltersModifyItemUI(), null);
                PopupWindow.Show(buttonRect, flexibleMenu);
            }

            int iconIndex = 0;
            foreach (var filter in LogEntries.wrapped.customFilters.filters)
            {
                if (iconIndex >= 7)
                {
                    iconIndex = 0;
                }
                filter.toggle = GUILayout.Toggle(filter.toggle, new GUIContent(filter.filter, iconCustomFiltersSmalls[iconIndex++]), MiniButton);
            }
        }

        private void OnSetFilteringHistoryCallback(object userData, string[] options, int selected)
        {
            LogEntries.wrapped.searchString = options[selected];
        }

        #region 跟踪堆栈
        /// <summary>
        /// 跟踪堆栈点击的行数
        /// </summary>
        private int m_StacktraceLineContextClickRow;


        /// <summary>
        /// 跟踪堆栈
        /// </summary>
        /// <param name="e"></param>
        /// <param name="tempContent"></param>
        private void StacktraceListView(Event e, GUIContent tempContent)
        {
            float maxWidth = LogEntries.wrapped.StacktraceListView_GetMaxWidth(tempContent, MessageStyle);

            if (m_StacktraceLineContextClickRow != -1)
            {
                var stacktraceLineInfoIndex = m_StacktraceLineContextClickRow;
                m_StacktraceLineContextClickRow = -1;
                GenericMenu menu = new GenericMenu();
                if (LogEntries.wrapped.StacktraceListView_CanOpen(stacktraceLineInfoIndex))
                {
                    menu.AddItem(new GUIContent("Open"), false, LogEntries.wrapped.StacktraceListView_Open, stacktraceLineInfoIndex);
                    menu.AddSeparator("");
                    if (LogEntries.wrapped.StacktraceListView_CanWrapper(stacktraceLineInfoIndex))
                    {
                        menu.AddItem(new GUIContent("Wrapper"), LogEntries.wrapped.StacktraceListView_IsWrapper(stacktraceLineInfoIndex), LogEntries.wrapped.StacktraceListView_Wrapper, stacktraceLineInfoIndex);
                    }
                }
                menu.AddItem(new GUIContent("Copy"), false, LogEntries.wrapped.StacktraceListView_Copy, stacktraceLineInfoIndex);
                menu.AddItem(new GUIContent("Copy All"), false, LogEntries.wrapped.StacktraceListView_CopyAll);
                menu.ShowAsContext();
            }

            int id = GUIUtility.GetControlID(0);
            int rowDoubleClicked = -1;
            int selectedRow = -1;
            bool openSelectedItem = false;
            messageListView.totalRows = LogEntries.wrapped.StacktraceListView_GetCount();
            GUILayout.BeginHorizontal(Box);
            messageListView.scrollPos = EditorGUILayout.BeginScrollView(messageListView.scrollPos);
            ListViewGUI.ilvState.beganHorizontal = true;
            messageListView.draggedFrom = -1;
            messageListView.draggedTo = -1;
            messageListView.fileNames = (string[])null;
            Rect rect = GUILayoutUtility.GetRect(maxWidth,
                (float)(messageListView.totalRows * messageListView.rowHeight + 3));
            foreach (ListViewElement el in ListViewGUI.DoListView(rect, messageListView, null, string.Empty))
            {
                if (e.type == EventType.MouseDown && (e.button == 0 || e.button == 1) && el.position.Contains(e.mousePosition))
                {
                    if (e.button == 1)
                    {
                        messageListView.row = el.row;
                        selectedRow = messageListView.row;
                        m_StacktraceLineContextClickRow = selectedRow;
                        continue;
                    }

                    selectedRow = messageListView.row;
                    if (e.clickCount == 2)
                        openSelectedItem = true;
                }
                else if (e.type == EventType.Repaint)
                {
                    tempContent.text = LogEntries.wrapped.StacktraceListView_GetLine(el.row);
                    rect = el.position;
                    if (rect.width < maxWidth)
                    {
                        rect.width = maxWidth;
                    }
                    MessageStyle.Draw(rect, tempContent, id, messageListView.row == el.row);
                }
            }

            // Open entry using return key
            if ((GUIUtility.keyboardControl == messageListView.ID) && (e.type == EventType.KeyDown) && (e.keyCode == KeyCode.Return) && (messageListView.row != 0))
            {
                selectedRow = messageListView.row;
                openSelectedItem = true;
            }

            if (openSelectedItem)
            {
                rowDoubleClicked = selectedRow;
                e.Use();
            }

            if (m_StacktraceLineContextClickRow != -1)
            {
                Repaint();
            }

            if (rowDoubleClicked != -1)
            {
                LogEntries.wrapped.StacktraceListView_Open(rowDoubleClicked);
            }
        }

        public struct StackTraceLogTypeData
        {
            public LogType logType;
            public StackTraceLogType stackTraceLogType;
        }

        public void ToggleLogStackTraces(object userData)
        {
            StackTraceLogTypeData data = (StackTraceLogTypeData)userData;
            PlayerSettings.SetStackTraceLogType(data.logType, data.stackTraceLogType);
        }

        public void ToggleLogStackTracesForAll(object userData)
        {
            foreach (LogType logType in Enum.GetValues(typeof(LogType)))
                PlayerSettings.SetStackTraceLogType(logType, (StackTraceLogType)userData);
        }

        #endregion

        public void AddItemsToMenu(GenericMenu menu)
        {
            if (Application.platform == RuntimePlatform.OSXEditor)
                menu.AddItem(EditorGUIUtility.TextContent("Open Player Log"), false, UnityEditorInternal.InternalEditorUtility.OpenPlayerConsole);
            menu.AddItem(EditorGUIUtility.TextContent("Open Editor Log"), false, UnityEditorInternal.InternalEditorUtility.OpenEditorConsole);
            menu.AddItem(EditorGUIUtility.TextContent("Export Console Log"), false, LogEntries.wrapped.ExportLog);

            menu.AddItem(EditorGUIUtility.TrTextContent("Show Timestamp"), LogEntries.wrapped.showTimestamp, SetTimestamp);

            for (int i = 1; i <= 10; ++i)
            {
                var lineString = i == 1 ? "Line" : "Lines";
                menu.AddItem(new GUIContent(string.Format("Log Entry/{0} {1}", i, lineString)), i == LogStyleLineCount, SetLogLineCount, i);
            }

            AddStackTraceLoggingMenu(menu);
        }

        private void SetTimestamp()
        {
            LogEntries.wrapped.showTimestamp = !LogEntries.wrapped.showTimestamp;
        }

        private void SetLogLineCount(object obj)
        {
            int count = (int)obj;
            EditorPrefs.SetInt("ConsoleWindowLogLineCount", count);
            LogStyleLineCount = count;

            UpdateListView();
        }

        private void AddStackTraceLoggingMenu(GenericMenu menu)
        {
            // TODO: Maybe remove this, because it basically duplicates UI in PlayerSettings
            foreach (LogType logType in Enum.GetValues(typeof(LogType)))
            {
                foreach (StackTraceLogType stackTraceLogType in Enum.GetValues(typeof(StackTraceLogType)))
                {
                    StackTraceLogTypeData data;
                    data.logType = logType;
                    data.stackTraceLogType = stackTraceLogType;

                    menu.AddItem(EditorGUIUtility.TextContent("Stack Trace Logging/" + logType + "/" + stackTraceLogType), PlayerSettings.GetStackTraceLogType(logType) == stackTraceLogType,
                        ToggleLogStackTraces, data);
                }
            }

            int stackTraceLogTypeForAll = (int)PlayerSettings.GetStackTraceLogType(LogType.Log);
            foreach (LogType logType in Enum.GetValues(typeof(LogType)))
            {
                if (PlayerSettings.GetStackTraceLogType(logType) != (StackTraceLogType)stackTraceLogTypeForAll)
                {
                    stackTraceLogTypeForAll = -1;
                    break;
                }
            }

            foreach (StackTraceLogType stackTraceLogType in Enum.GetValues(typeof(StackTraceLogType)))
            {
                menu.AddItem(EditorGUIUtility.TextContent("Stack Trace Logging/All/" + stackTraceLogType), (StackTraceLogType)stackTraceLogTypeForAll == stackTraceLogType,
                    ToggleLogStackTracesForAll, stackTraceLogType);
            }
        }
    }

    internal class GettingLogEntriesScope : IDisposable
    {
        private bool m_Disposed;

        public GettingLogEntriesScope(ListViewState listView)
        {
            listView.totalRows = LogEntries.wrapped.GetCount();
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
        }

        ~GettingLogEntriesScope()
        {
            if (!m_Disposed)
                Debug.LogError("Scope was not disposed! You should use the 'using' keyword or manually call Dispose.");
        }
    }

    #region CustomFilters

    internal class CustomFiltersItemProvider : IFlexibleMenuItemProvider
    {
        private readonly EntryWrapped.CustomFiltersGroup m_Groups;

        public CustomFiltersItemProvider(EntryWrapped.CustomFiltersGroup groups)
        {
            m_Groups = groups;
        }

        public int Count()
        {
            return m_Groups.filters.Count;
        }

        public object GetItem(int index)
        {
            return m_Groups.filters[index];
        }

        public int Add(object obj)
        {
            m_Groups.filters.Add(new EntryWrapped.CustomFiltersItem() { filter = (string)obj, changed = false });
            m_Groups.Save();
            return Count() - 1;
        }

        public void Replace(int index, object newPresetObject)
        {
            m_Groups.filters[index].filter = (string)newPresetObject;
            m_Groups.Save();
        }

        public void Remove(int index)
        {
            if (m_Groups.filters[index].toggle)
            {
                m_Groups.changed = true;
            }
            m_Groups.filters.RemoveAt(index);
            m_Groups.Save();
        }

        public object Create()
        {
            return "log";
        }

        public void Move(int index, int destIndex, bool insertAfterDestIndex)
        {
            Debug.LogError("Missing impl");
        }

        public string GetName(int index)
        {
            return m_Groups.filters[index].filter;
        }

        public bool IsModificationAllowed(int index)
        {
            return true;
        }

        public int[] GetSeperatorIndices()
        {
            return new int[0];
        }
    }

    internal class CustomFiltersModifyItemUI : FlexibleMenuModifyItemUI
    {
        private static class Styles
        {
            public static GUIContent headerAdd = EditorGUIUtility.TextContent("Add");
            public static GUIContent headerEdit = EditorGUIUtility.TextContent("Edit");
            public static GUIContent optionalText = EditorGUIUtility.TextContent("Search");
            public static GUIContent ok = EditorGUIUtility.TextContent("OK");
            public static GUIContent cancel = EditorGUIUtility.TextContent("Cancel");
        }

        private string m_TextSearch;

        public override void OnClose()
        {
            m_TextSearch = null;
            base.OnClose();
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(330f, 80f);
        }

        public override void OnGUI(Rect rect)
        {
            string itemValue = m_Object as string;
            if (itemValue == null)
            {
                Debug.LogError("Invalid object");
                return;
            }

            if (m_TextSearch == null)
            {
                m_TextSearch = itemValue;
            }

            const float kColumnWidth = 70f;
            const float kSpacing = 10f;

            GUILayout.Space(3);
            GUILayout.Label(m_MenuType == MenuType.Add ? Styles.headerAdd : Styles.headerEdit, EditorStyles.boldLabel);

            Rect seperatorRect = GUILayoutUtility.GetRect(1, 1);
            FlexibleMenu.DrawRect(seperatorRect,
                (EditorGUIUtility.isProSkin)
                    ? new Color(0.32f, 0.32f, 0.32f, 1.333f)
                    : new Color(0.6f, 0.6f, 0.6f, 1.333f));                      // dark : light
            GUILayout.Space(4);

            // Optional text
            GUILayout.BeginHorizontal();
            GUILayout.Label(Styles.optionalText, GUILayout.Width(kColumnWidth));
            GUILayout.Space(kSpacing);
            m_TextSearch = EditorGUILayout.TextField(m_TextSearch);
            GUILayout.EndHorizontal();

            GUILayout.Space(5f);

            // Cancel, Ok
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            if (GUILayout.Button(Styles.cancel))
            {
                editorWindow.Close();
            }

            if (GUILayout.Button(Styles.ok))
            {
                var textSearch = m_TextSearch.Trim();
                if (!string.IsNullOrEmpty(textSearch))
                {
                    m_Object = m_TextSearch;
                    Accepted();
                    editorWindow.Close();
                }
            }
            GUILayout.Space(10);
            GUILayout.EndHorizontal();
        }
    }

    #endregion
}

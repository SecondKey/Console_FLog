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

        #region 窗口
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
        #endregion

        #region Parameters
        /// <summary>
        /// 显示信息列表
        /// </summary>
        ListViewState logListView;
        /// <summary>
        /// 信息详情列表
        /// </summary>
        ListViewState messageListView;
        /// <summary>
        /// 
        /// </summary>
        bool m_DevBuild;

        /// <summary>
        /// 选择历史
        /// </summary>
        private string[] m_SearchHistory = new[] { "" };

        /// <summary>
        /// 用于判断页面是否需要刷新，默认是double最大值
        /// 当需要刷新时，m_NextRepaint会记录发生变化的时间
        /// OnInspectorUpdate将会计算时间差，刷新页面并还原m_NextRepaint
        /// </summary>
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

        public void RefreshPageStyle()
        {

        }

        /// <summary>
        /// 刷新列表视图
        /// </summary>
        void UpdateListView()
        {
            m_LineHeight = Mathf.RoundToInt(ErrorStyle.lineHeight);
            m_BorderHeight = ErrorStyle.border.top + ErrorStyle.border.bottom;
            UpdateListView();

            int newRowHeight = RowHeight;

            // We reset the scroll list to auto scrolling whenever the log entry count is modified
            logListView.rowHeight = 32;
            logListView.row = -1;
            logListView.scrollPos.y = EntryWrapped.Instence.GetCount() * newRowHeight;

            Repaint();
        }
        #endregion


        #region 生命周期
        void OnEnable()
        {
            ms_ConsoleWindow = this;//实现类似单例的效果
            titleContent.text = "Console T";//设置窗口名

            Application.logMessageReceived += DoLogChanged;//添加打印输出的回调函数

            position = new Rect(200, 200, 800, 400);//设置窗口起始位置
            logListView = new ListViewState(0, 0);//清空信息列表
            messageListView = new ListViewState(0, 14);//清空详细信息列表
            m_StacktraceLineContextClickRow = -1;//设置当前未选择跟踪堆栈的行


            if (m_ConsoleAttachToPlayerState == null)//如果未建立与玩家连接
                m_ConsoleAttachToPlayerState = new ConsoleAttachToPlayerState(this);//建立与玩家连接

            m_DevBuild = Unsupported.IsDeveloperMode();//检查当前是否未开发者模式（进入开发者模式：UnityEditor.EditorPrefs.SetBool("DeveloperMode", true);）
            EntryWrapped.Instence.searchHistory = m_SearchHistory;
        }

        void OnDisable()
        {
            Application.logMessageReceived -= DoLogChanged;
            ms_ConsoleWindow = null;

            m_ConsoleAttachToPlayerState?.Dispose();
            m_ConsoleAttachToPlayerState = null;
            m_SearchHistory = EntryWrapped.Instence.searchHistory;
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

        void OnGUI()
        {
            Event e = Event.current;//获取当前正在处理的事件
            EntryWrapped.Instence.UpdateEntries();//更新所有入口

            #region 复制粘贴选中的项
            if ((e.type == EventType.ValidateCommand || e.type == EventType.ExecuteCommand) && e.commandName == "Copy")
            {
                if (e.type == EventType.ExecuteCommand)
                    EntryWrapped.Instence.StacktraceListView_CopyAll();
                e.Use();
            }
            #endregion

            #region Horizontal
            GUILayout.BeginHorizontal(ConsoleParameters.Toolbar);//开启一个横向的布局（有点没搞懂）

            #region Clear按钮
            if (GUILayout.Button(ClearLabel, MiniButton))//创建Clear按钮，判断该按钮是否被按下
            {
                LogEntries.Clear();//清空所有入口
                GUIUtility.keyboardControl = 0;//清空键盘控制
            }
            #endregion  

            #region 调整列表的位置
            //获取被过滤后的行数
            int currCount = EntryWrapped.Instence.GetCount();
            if (logListView.totalRows != currCount && logListView.totalRows > 0)
            {
                if (logListView.scrollPos.y >= logListView.rowHeight * logListView.totalRows - ms_LVHeight)//判断滚动条是否在最低位置
                {
                    logListView.scrollPos.y = currCount * RowHeight - ms_LVHeight;
                }
            }

            if (EntryWrapped.Instence.searchFrame)
            {
                EntryWrapped.Instence.searchFrame = false;
                int selectedIndex = EntryWrapped.Instence.GetSelectedEntryIndex();
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

            #endregion
            EditorGUILayout.Space();//插入空格

            #region collapse开关
            bool wasCollapsed = EntryWrapped.Instence.collapse;
            EntryWrapped.Instence.collapse = GUILayout.Toggle(wasCollapsed, CollapseLabel, MiniButton);
            bool collapsedChanged = wasCollapsed != EntryWrapped.Instence.collapse;
            if (collapsedChanged)
            {
                logListView.row = -1;// unselect if collapsed flag changed
                logListView.scrollPos.y = EntryWrapped.Instence.GetCount() * RowHeight;//滚动条滚到最低
            }
            #endregion

            #region ClearOnPlay开关
            SetFlag(ConsoleFlags.ClearOnPlay, GUILayout.Toggle(HasFlag(ConsoleFlags.ClearOnPlay), ClearOnPlayLabel, MiniButton));
            #endregion

            #region ClearOnBuild开关
#if UNITY_2019_1_OR_NEWER
            SetFlag(ConsoleFlags.ClearOnBuild, GUILayout.Toggle(HasFlag(ConsoleFlags.ClearOnBuild), Constants.ClearOnBuildLabel, Constants.MiniButton));
#endif
            #endregion

            #region ErrorPause开关
            SetFlag(ConsoleFlags.ErrorPause, GUILayout.Toggle(HasFlag(ConsoleFlags.ErrorPause), ErrorPauseLabel, MiniButton));//报错时暂停按钮
            #endregion

            #region AttachToPlayer下拉菜单
            ConnectionGUILayout.AttachToPlayerDropdown(m_ConsoleAttachToPlayerState, EditorStyles.toolbarDropDown);//AttachToPlayer下拉按钮
            #endregion

            EditorGUILayout.Space();//插入空格

            #region 开发模式开关
            if (m_DevBuild)//检查是不是开发人员模式
            {
                GUILayout.FlexibleSpace();//插入智能空间
                SetFlag(ConsoleFlags.StopForAssert, GUILayout.Toggle(HasFlag(ConsoleFlags.StopForAssert), StopForAssertLabel, MiniButton));//维护时停止
                SetFlag(ConsoleFlags.StopForError, GUILayout.Toggle(HasFlag(ConsoleFlags.StopForError), StopForErrorLabel, MiniButton));//报错时停止
            }
            #endregion

            GUILayout.FlexibleSpace(); GUILayout.FlexibleSpace();//插入智能空间

            #region 搜索条
            GUILayout.Space(4f);//插入四格空格
            SearchField(e);//添加搜索条
            #endregion

            #region Log开关，Worring开关，Error开关
            int errorCount = 0;//报错数量
            int warningCount = 0;//错误数量
            int logCount = 0;//日志数量
            EntryWrapped.Instence.GetCountsByType(ref errorCount, ref warningCount, ref logCount);//获取三种输出的数量
            EditorGUI.BeginChangeCheck();//检查是否有代码被修改

            bool setLogFlag = GUILayout.Toggle(EntryWrapped.Instence.HasFlag((int)ConsoleFlags.LogLevelLog), new GUIContent((logCount <= 999 ? logCount.ToString() : "999+"), logCount > 0 ? iconInfoSmall : iconInfoMono), MiniButton);//日志按钮
            bool setWarningFlag = GUILayout.Toggle(EntryWrapped.Instence.HasFlag((int)ConsoleFlags.LogLevelWarning), new GUIContent((warningCount <= 999 ? warningCount.ToString() : "999+"), warningCount > 0 ? iconWarnSmall : iconWarnMono), MiniButton);//警告按钮
            bool setErrorFlag = GUILayout.Toggle(EntryWrapped.Instence.HasFlag((int)ConsoleFlags.LogLevelError), new GUIContent((errorCount <= 999 ? errorCount.ToString() : "999+"), errorCount > 0 ? iconErrorSmall : iconErrorMono), MiniButton);//报错按钮

            EntryWrapped.Instence.SetFlag((int)ConsoleFlags.LogLevelLog, setLogFlag);//设置日志是否输出
            EntryWrapped.Instence.SetFlag((int)ConsoleFlags.LogLevelWarning, setWarningFlag);//设置警告是否输出
            EntryWrapped.Instence.SetFlag((int)ConsoleFlags.LogLevelError, setErrorFlag);//设置报错是否输出
            #endregion

            #region FirstError按钮
            if (GUILayout.Button(new GUIContent(errorCount > 0 ? iconFirstErrorSmall : iconFirstErrorMono, FirstErrorLabel), MiniButton))
            {
                int firstErrorIndex = EntryWrapped.Instence.GetFirstErrorEntryIndex();
                if (firstErrorIndex != -1)
                {
                    SetActiveEntry(firstErrorIndex);
                    EntryWrapped.Instence.searchFrame = true;
                }
            }
            #endregion

            GUILayout.EndHorizontal();//关闭横向布局
            #endregion

            #region Vertical
            SplitterGUILayout.BeginVerticalSplit(spl);//开启纵向布局
            EditorGUIUtility.SetIconSize(new Vector2(RowHeight, RowHeight));//设置图标
            GUIContent tempContent = new GUIContent();
            int id = GUIUtility.GetControlID(0);
            int rowDoubleClicked = -1;//双击行号

            /////@TODO: Make Frame selected work with ListViewState
            using (new GettingLogEntriesScope(logListView))
            {
                int selectedRow = -1;//当前选中行号
                bool collapsed = EntryWrapped.Instence.collapse;//折叠
                bool openSelectedItem = false;//是否要打开选中条目

                #region 遍历渲染所有的条目
                foreach (ListViewElement el in ListViewGUI.ListView(logListView, Box))//遍历所有条目
                {
                    if (e.type == EventType.MouseDown && e.button == 0 && el.position.Contains(e.mousePosition))//如果左键点击当前条目
                    {
                        logListView.row = el.row;//将列表的当前行设置为当前选中的行
                        selectedRow = el.row;//设置选中行为当前选中行
                        if (e.clickCount == 2)//如果双击
                            openSelectedItem = true;//打开选择条目
                    }
                    else if (e.type == EventType.Repaint)//如果目标事件是每帧发送的刷新消息
                    {
                        var parameters = EntryWrapped.Instence.GetEntryLinesAndFlagAndCount(el.row);//获取入口的数据1：文本 2：类型 3：入口数量 4：选择的条目 5：多选结束条目
                        ConsoleFlags flag = (ConsoleFlags)parameters.Item2;//输出的类型
                        bool isSelected = EntryWrapped.Instence.IsEntrySelected(el.row);//条目是否被选中

                        #region 绘制条目
                        #region 背景
                        GUIStyle s = el.row % 2 == 0 ? OddBackground : EvenBackground;//交替背景颜色
                        s.Draw(el.position, true, false, isSelected, false);//绘制背景
                        #endregion
                        #region 图标
                        GUIStyle iconStyle = GetStyleForErrorMode(flag, true, LogStyleLineCount == 1);//设置条目图标样式
                        iconStyle.Draw(el.position, false, false, isSelected, false);//绘制图标
                        #endregion
                        #region 文本
                        tempContent.text = parameters.Item1;//获取文本
                        GUIStyle errorModeStyle = GetStyleForErrorMode(flag, false, LogStyleLineCount == 1);//绘制文本
                        if (string.IsNullOrEmpty(EntryWrapped.Instence.searchString) || parameters.Item4 == -1 || parameters.Item4 >= parameters.Item1.Length)
                        {
                            errorModeStyle.Draw(el.position, tempContent, id, isSelected);//直接绘制文本
                        }
                        else
                        {
                            //TODO:不太懂
                            errorModeStyle.DrawWithTextSelection(el.position, tempContent, GUIUtility.keyboardControl, parameters.Item4, parameters.Item5);//绘制可以选中的文本
                        }
                        #endregion
                        #region 折叠数字角标
                        if (collapsed)//如果需要折叠
                        {
                            Rect badgeRect = el.position;//角标位置
                            tempContent.text = parameters.Item3.ToString(CultureInfo.InvariantCulture);//角标中的文本
                            Vector2 badgeSize = CountBadge.CalcSize(tempContent);//计算角标大小
                            badgeRect.xMin = badgeRect.xMax - badgeSize.x;
                            badgeRect.yMin += ((badgeRect.yMax - badgeRect.yMin) - badgeSize.y) * 0.5f;
                            badgeRect.x -= 5f;
                            GUI.Label(badgeRect, tempContent, CountBadge);//绘制角标
                        }
                        #endregion
                        //Rect iconRect = el.position;
                        //iconRect.size = new Vector2(100, 100);
                        //GUIStyle style = "Icon.Clip";
                        //GUI.Label(iconRect, m_Tex, style);
                        #endregion 

                    }
                }
                #endregion

                #region 列表的滚动位置
                if (selectedRow != -1)//如果有选中的条目
                {
                    if (logListView.scrollPos.y >= logListView.rowHeight * logListView.totalRows - ms_LVHeight)//如果当前列表位置在滚轮位置以下
                    {
                        logListView.scrollPos.y = logListView.rowHeight * logListView.totalRows - ms_LVHeight - 1;//设置当前列表位置到
                    }
                }
                #endregion

                #region 确保选中项已经更新
                if (logListView.totalRows != 0 && logListView.row < logListView.totalRows && logListView.row >= 0)
                {
                    if (logListView.selectionChanged)//如果选中项发生了变化
                    {
                        SetActiveEntry(logListView.row);//设置选中入口
                    }
                }
                #endregion

                #region 使用回车键打开入口
                if ((GUIUtility.keyboardControl == logListView.ID) && (e.type == EventType.KeyDown) && (e.keyCode == KeyCode.Return) && (logListView.row != 0))
                {
                    selectedRow = logListView.row;
                    openSelectedItem = true;
                }
                #endregion

                #region
                //TODO:不懂
                if (e.type != EventType.Layout && ListViewGUI.ilvState.rectHeight != 1)//
                {
                    ms_LVHeight = ListViewGUI.ilvState.rectHeight;
                }
                #endregion

                #region 打开选中项目
                if (openSelectedItem)
                {
                    rowDoubleClicked = selectedRow;
                    e.Use();
                }
                #endregion

                #region 选中游戏对象
                if (selectedRow != -1)
                {
                    SetActiveEntry(selectedRow);
                }
                #endregion
            }

            #region 延迟回调防止死锁
            // Prevent dead locking in EditorMonoConsole by delaying callbacks (which can log to the console) until after LogEntries.EndGettingEntries() has been
            // called (this releases the mutex in EditorMonoConsole so logging again is allowed). Fix for case 1081060.
            if (rowDoubleClicked != -1)//如果有双击的目标
            {
                EntryWrapped.Instence.StacktraceListView_RowGotDoubleClicked();//双击打开
            }
            #endregion

            EditorGUIUtility.SetIconSize(Vector2.zero);//设置Icon显示为：自适应大小

            StacktraceListView(e, tempContent);//绘制跟踪堆栈列表

            SplitterGUILayout.EndVerticalSplit();//结束纵向布局
            #endregion
        }
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

        #endregion 

        #region 点击索引到游戏物体
        /// <summary>
        /// 上一个选中的索引
        /// </summary>
        private int m_ActiveInstanceID = 0;

        /// <summary>
        /// 设置点击入口，如果该输出包含游戏物体信息，选中输出目标消息的游戏物体，并显示选中特效
        /// 如果反复选择同一个输出，或目标游戏物体相同，则不会重复选择
        /// </summary>
        /// <param name="selectedIndex">选中的信息的id</param>
        void SetActiveEntry(int selectedIndex)
        {
            messageListView.row = -1;//详细信息列表试图的选中行为-1
            messageListView.scrollPos.y = 0;//详细信息列表滚动到最顶
            if (selectedIndex != -1)//如果选择了目标
            {
                var instanceID = EntryWrapped.Instence.SetSelectedEntry(selectedIndex);//设置选择目标并获取目标实例的ID
                // ping object referred by the log entry
                if (m_ActiveInstanceID != instanceID)//如果之前选的实例的ID和当前实例的ID不同
                {
                    m_ActiveInstanceID = instanceID;//同步
                    if (instanceID != 0)//如果可以找到目标unity游戏物体
                        EditorGUIUtility.PingObject(instanceID);//ping这个物体，选中并触发选中特效
                }
            }
        }
        #endregion

        /// <summary>
        /// 查找文件
        /// </summary>
        /// <param name="e"></param>
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

            string searchText = EntryWrapped.Instence.searchString;
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

            bool showHistory = EntryWrapped.Instence.searchHistory[0].Length != 0;
            Rect popupPosition = rect;
            popupPosition.width = 20;
            if (showHistory && Event.current.type == EventType.MouseDown && popupPosition.Contains(Event.current.mousePosition))
            {
                GUIUtility.keyboardControl = 0;
                EditorUtility.DisplayCustomMenu(rect, EditorGUIUtility.TempContent(EntryWrapped.Instence.searchHistory), -1, OnSetFilteringHistoryCallback, null);
                Event.current.Use();
            }

            EntryWrapped.Instence.searchString = EditorGUI.ToolbarSearchField(

                rect, searchText, showHistory);

            if (GUILayout.Button(new GUIContent(iconCustomFiltersMono, CustomFiltersLabel), EditorStyles.toolbarDropDown))
            {
                Rect buttonRect = rect;
                buttonRect.x += buttonRect.width;
                var menuData = new CustomFiltersItemProvider(EntryWrapped.Instence.customFilters);
                var flexibleMenu = new FlexibleMenu(menuData, -1, new CustomFiltersModifyItemUI(), null);
                PopupWindow.Show(buttonRect, flexibleMenu);
            }

            int iconIndex = 0;
            foreach (var filter in EntryWrapped.Instence.customFilters.filters)
            {
                if (iconIndex >= 7)
                {
                    iconIndex = 0;
                }
                filter.toggle = GUILayout.Toggle(filter.toggle, new GUIContent(filter.filter, iconCustomFiltersSmalls[iconIndex++]), MiniButton);
            }
        }

        /// <summary>
        /// 设置了过滤历史的回调函数
        /// </summary>
        /// <param name="userData"></param>
        /// <param name="options"></param>
        /// <param name="selected"></param>
        private void OnSetFilteringHistoryCallback(object userData, string[] options, int selected)
        {
            EntryWrapped.Instence.searchString = options[selected];
        }

        #region 跟踪堆栈
        /// <summary>
        /// 跟踪堆栈点击的行数
        /// </summary>
        private int m_StacktraceLineContextClickRow;


        /// <summary>
        /// 绘制跟踪堆栈页面
        /// 这个函数仅由OnGUI调用
        /// </summary>
        /// <param name="e">当前OnGUI处理的事件</param>
        /// <param name="tempContent"></param>
        private void StacktraceListView(Event e, GUIContent tempContent)
        {
            if (m_StacktraceLineContextClickRow != -1)//如果选择了跟踪堆栈的行
            {
                var stacktraceLineInfoIndex = m_StacktraceLineContextClickRow;//保存选中行信息
                m_StacktraceLineContextClickRow = -1;//清除选中信息
                GenericMenu menu = new GenericMenu();//创建一个邮件菜单
                if (EntryWrapped.Instence.StacktraceListView_CanOpen(stacktraceLineInfoIndex))//如果入口可以打开
                {
                    menu.AddItem(new GUIContent("Open"), false, EntryWrapped.Instence.StacktraceListView_Open, stacktraceLineInfoIndex);//添加一个 打开 按钮
                    menu.AddSeparator("");//创建分隔符
                    if (EntryWrapped.Instence.StacktraceListView_CanWrapper(stacktraceLineInfoIndex))//如果入口可以被包装
                    {
                        menu.AddItem(new GUIContent("Wrapper"), EntryWrapped.Instence.StacktraceListView_IsWrapper(stacktraceLineInfoIndex), EntryWrapped.Instence.StacktraceListView_Wrapper, stacktraceLineInfoIndex);//添加包装按钮
                    }
                }
                menu.AddItem(new GUIContent("Copy"), false, EntryWrapped.Instence.StacktraceListView_Copy, stacktraceLineInfoIndex);//添加复制按钮
                menu.AddItem(new GUIContent("Copy All"), false, EntryWrapped.Instence.StacktraceListView_CopyAll);//添加复制所有按钮
                menu.ShowAsContext();//当鼠标右键点击时显示该菜单
            }

            float maxWidth = EntryWrapped.Instence.StacktraceListView_GetMaxWidth(tempContent, MessageStyle);//获取跟踪堆栈所有行的最大宽度
            int id = GUIUtility.GetControlID(0);
            int rowDoubleClicked = -1;//双击的行
            int selectedRow = -1;//选择的行
            bool openSelectedItem = false;//是否要打开选中项

            GUILayout.BeginHorizontal(Box);//开启一个纵向布局

            #region ListView Layout
            messageListView.totalRows = EntryWrapped.Instence.StacktraceListView_GetCount();//获取跟踪堆栈总行数
            messageListView.scrollPos = EditorGUILayout.BeginScrollView(messageListView.scrollPos);//开启一个自动滚动的试图
            ListViewGUI.ilvState.beganHorizontal = true;
            messageListView.draggedFrom = -1;
            messageListView.draggedTo = -1;
            messageListView.fileNames = null;
            #endregion

            Rect rect = GUILayoutUtility.GetRect(maxWidth, messageListView.totalRows * messageListView.rowHeight + 3);//获取需要显示的范围
            foreach (ListViewElement el in ListViewGUI.DoListView(rect, messageListView, null, string.Empty))//遍历所有列表项
            {
                if (e.type == EventType.MouseDown && el.position.Contains(e.mousePosition) && (e.button == 0 || e.button == 1))//如果点中了该项
                {
                    if (e.button == 1)//如果是右键点击
                    {
                        messageListView.row = el.row;//设置详细信息列表的选中行为当前行
                        selectedRow = messageListView.row;//设置选中行
                        m_StacktraceLineContextClickRow = selectedRow;//设置选中行
                        continue;//右键不能双击打开
                    }

                    selectedRow = messageListView.row;//设置选中行
                    if (e.clickCount == 2)//如果双击
                        openSelectedItem = true;//准备打开目标项目
                }
                else if (e.type == EventType.Repaint)//如果事件是重绘事件
                {
                    tempContent.text = EntryWrapped.Instence.StacktraceListView_GetLine(el.row);//获取跟踪堆栈的文本信息
                    rect = el.position;//设置重绘范围为目标重绘范围
                    if (rect.width < maxWidth)//设置最大宽度
                    {
                        rect.width = maxWidth;
                    }
                    MessageStyle.Draw(rect, tempContent, id, messageListView.row == el.row);//绘制目标范围
                }
            }


            if ((GUIUtility.keyboardControl == messageListView.ID) && (e.type == EventType.KeyDown) && (e.keyCode == KeyCode.Return) && (messageListView.row != 0))//如果按下的键是回车键
            {
                selectedRow = messageListView.row;//设置选中行
                openSelectedItem = true;//准备打开目标项目
            }

            if (openSelectedItem)//如果要打开目标界面
            {
                rowDoubleClicked = selectedRow;//设置要打开的页面
                e.Use();//停止事件路由
            }

            if (m_StacktraceLineContextClickRow != -1)//如果点击
            {
                Repaint();
            }

            if (rowDoubleClicked != -1)//如果要打开目标文件
            {
                EntryWrapped.Instence.StacktraceListView_Open(rowDoubleClicked);//打开目标文件
            }
        }

        /// <summary>
        /// 跟踪堆栈类型数据
        /// </summary>
        public struct StackTraceLogTypeData
        {
            /// <summary>
            /// 输出类型
            /// </summary>
            public LogType logType;
            /// <summary>
            /// 跟踪堆栈的类型
            /// </summary>
            public StackTraceLogType stackTraceLogType;
        }

        /// <summary>
        /// 设置跟踪堆栈输出选项
        /// </summary>
        /// <param name="userData">跟踪堆栈的类型</param>
        public void ToggleLogStackTraces(object userData)
        {
            StackTraceLogTypeData data = (StackTraceLogTypeData)userData;//类型转换
            PlayerSettings.SetStackTraceLogType(data.logType, data.stackTraceLogType);//设置类型
        }
        /// <summary>
        /// 为所有的输出类型设置对对应的跟踪堆栈类型
        /// </summary>
        /// <param name="userData">跟踪堆栈类型</param>
        public void ToggleLogStackTracesForAll(object userData)
        {
            foreach (LogType logType in Enum.GetValues(typeof(LogType)))//遍历所有的输出类型
            {
                PlayerSettings.SetStackTraceLogType(logType, (StackTraceLogType)userData);
            }
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

                    menu.AddItem(EditorGUIUtility.TextContent("Stack Trace Logging/" + logType + "/" + stackTraceLogType), PlayerSettings.GetStackTraceLogType(logType) == stackTraceLogType, ToggleLogStackTraces, data);
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

        #endregion

        public void AddItemsToMenu(GenericMenu menu)
        {
            if (Application.platform == RuntimePlatform.OSXEditor)
                menu.AddItem(EditorGUIUtility.TextContent("Open Player Log"), false, UnityEditorInternal.InternalEditorUtility.OpenPlayerConsole);
            menu.AddItem(EditorGUIUtility.TextContent("Open Editor Log"), false, UnityEditorInternal.InternalEditorUtility.OpenEditorConsole);
            menu.AddItem(EditorGUIUtility.TextContent("Export Console Log"), false, EntryWrapped.Instence.ExportLog);

            menu.AddItem(EditorGUIUtility.TrTextContent("Show Timestamp"), EntryWrapped.Instence.showTimestamp, SetTimestamp);

            for (int i = 1; i <= 10; ++i)
            {
                var lineString = i == 1 ? "Line" : "Lines";
                menu.AddItem(new GUIContent(string.Format("Log Entry/{0} {1}", i, lineString)), i == LogStyleLineCount, SetLogLineCount, i);
            }

            AddStackTraceLoggingMenu(menu);
        }

        private void SetTimestamp()
        {
            EntryWrapped.Instence.showTimestamp = !EntryWrapped.Instence.showTimestamp;
        }

        private void SetLogLineCount(object obj)
        {
            int count = (int)obj;
            EditorPrefs.SetInt("ConsoleWindowLogLineCount", count);
            LogStyleLineCount = count;

            UpdateListView();
        }

    }

    internal class GettingLogEntriesScope : IDisposable
    {
        private bool m_Disposed;

        public GettingLogEntriesScope(ListViewState listView)
        {
            listView.totalRows = EntryWrapped.Instence.GetCount();
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
}

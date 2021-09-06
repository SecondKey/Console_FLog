using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Text;
using System.IO;
using static ConsoleTiny.ConsoleParameters;

namespace ConsoleTiny
{

    /// <summary>
    /// log条目包装
    /// </summary>
    public class EntryWrapped
    {
        #region 单例
        private static EntryWrapped instence;
        private EntryWrapped() { }
        public static EntryWrapped Instence { get { if (instence == null) instence = new EntryWrapped(); return instence; } }
        #endregion 


        [Flags]
        enum Mode
        {
            /// <summary>
            /// 错误
            /// </summary>
            Error = 1 << 0,
            /// <summary>
            /// 断言
            /// </summary>
            Assert = 1 << 1,
            /// <summary>
            /// 输出
            /// </summary>
            Log = 1 << 2,
            /// <summary>
            /// 致命的
            /// </summary>
            Fatal = 1 << 4,
            /// <summary>
            /// 不进行预处理的条件
            /// </summary>
            DontPreprocessCondition = 1 << 5,
            /// <summary>
            /// 资源导入错误
            /// </summary>
            AssetImportError = 1 << 6,
            /// <summary>
            /// 资源导入警告
            /// </summary>
            AssetImportWarning = 1 << 7,
            /// <summary>
            /// 脚本报错
            /// </summary>
            ScriptingError = 1 << 8,
            /// <summary>
            /// 脚本警告
            /// </summary>
            ScriptingWarning = 1 << 9,
            /// <summary>
            /// 脚本处理输出
            /// </summary>
            ScriptingLog = 1 << 10,
            /// <summary>
            /// 脚本编译报错
            /// </summary>
            ScriptCompileError = 1 << 11,
            /// <summary>
            /// 脚本编译警告
            /// </summary>
            ScriptCompileWarning = 1 << 12,
            /// <summary>
            /// 粘性报错
            /// </summary>
            StickyError = 1 << 13,
            /// <summary>
            /// 或许忽略行号
            /// </summary>
            MayIgnoreLineNumber = 1 << 14,
            /// <summary>
            /// 报BUG
            /// </summary>
            ReportBug = 1 << 15,
            /// <summary>
            /// 在状态栏显示以前的错误
            /// </summary>
            DisplayPreviousErrorInStatusBar = 1 << 16,
            /// <summary>
            /// 脚本异常
            /// </summary>
            ScriptingException = 1 << 17,
            /// <summary>
            /// 不提取跟踪堆栈
            /// </summary>
            DontExtractStacktrace = 1 << 18,
            /// <summary>
            /// 应当在开始时清除
            /// </summary>
            ShouldClearOnPlay = 1 << 19,
            /// <summary>
            /// 图形编译器报错
            /// </summary>
            GraphCompileError = 1 << 20,
            /// <summary>
            /// 脚本断言
            /// </summary>
            ScriptingAssertion = 1 << 21,
            /// <summary>
            /// 可视化脚本报错
            /// </summary>
            VisualScriptingError = 1 << 22
        };
        /// <summary>
        /// 是否显示时间戳
        /// </summary>
        public bool showTimestamp
        {
            get { return m_ShowTimestamp; }
            set
            {
                m_ShowTimestamp = value;
                EditorPrefs.SetBool(kPrefShowTimestamp, value);
            }
        }

        /// <summary>
        /// 是否折叠
        /// </summary>
        public bool collapse
        {
            get { return m_Collapse; }
            set
            {
                if (m_Collapse != value)
                {
                    m_Collapse = value;
                    EditorPrefs.SetBool(kPrefCollapse, value);
                    ClearEntries();
                    UpdateEntries();
                }
            }
        }

        /// <summary>
        /// 搜索的字符串
        /// </summary>
        public string searchString
        {
            get { return m_SearchString; }
            set { m_SearchStringComing = value; }
        }

        /// <summary>
        /// 搜索的历史
        /// </summary>
        public string[] searchHistory = new[] { "" };

        public bool searchFrame { get; set; }


        //TODO:这里暂时不知道为什么要用两个来处理，估计是为了避免重读
        /// <summary>
        /// 控制台特性标识
        /// </summary>
        private int m_ConsoleFlags;
        /// <summary>
        /// 之后的控制台特性标识
        /// </summary>
        private int m_ConsoleFlagsComing;
        /// <summary>
        /// 选择的字符串
        /// </summary>
        private string m_SearchString;
        /// <summary>
        /// 之后的选择的字符串
        /// </summary>
        private string m_SearchStringComing;

        /// <summary>
        /// 上次选择的时间
        /// </summary>
        private double m_LastSearchStringTime;

        private bool m_Init;
        private bool m_ShowTimestamp;
        private bool m_Collapse;
        private int[] m_TypeCounts = new[] { 0, 0, 0 };
        private int m_LastEntryCount = -1;
        /// <summary>
        /// 被选择的条目的信息
        /// </summary>
        private EntryInfo m_SelectedInfo;
        /// <summary>
        /// 所有条目信息
        /// </summary>
        private readonly List<EntryInfo> m_EntryInfos = new List<EntryInfo>();
        /// <summary>
        /// 被过滤后的条目信息
        /// </summary>
        private readonly List<EntryInfo> m_FilteredInfos = new List<EntryInfo>();
        private readonly CustomFiltersGroup m_CustomFilters = new CustomFiltersGroup();
        private readonly List<string> m_WrapperInfos = new List<string>();

        /// <summary>
        /// 检查目标是否包含目标枚举值
        /// </summary>
        /// <param name="flags">目标枚举值</param>
        /// <returns>是否包含</returns>
        public bool HasFlag(int flags)
        {
            return (m_ConsoleFlags & flags) != 0;
        }

        /// <summary>
        /// 设置枚举值
        /// </summary>
        /// <param name="flags"></param>
        /// <param name="val">true：添加，false：移除</param>
        public void SetFlag(int flags, bool val)
        {
            if (val)
            {
                m_ConsoleFlagsComing |= flags;
            }
            else
            {
                m_ConsoleFlagsComing &= ~flags;
            }
        }

        /// <summary>
        /// 获取被过滤后的行数
        /// </summary>
        /// <returns>过滤后的行数</returns>
        public int GetCount()
        {
            return m_FilteredInfos.Count;
        }

        /// <summary>
        /// 根据点击的行数获取
        /// </summary>
        /// <param name="row">行数</param>
        /// <returns>目标值元组</returns>
        internal EntryInfo GetEntryLinesAndFlagAndCount(int row)
        {
            if (row < 0 || row >= m_FilteredInfos.Count)
            {
                return default;
            }

            return m_FilteredInfos[row];
        }
        //public (string, int, int, int, int) GetEntryLinesAndFlagAndCount(int row)
        //{
        //    if (row < 0 || row >= m_FilteredInfos.Count)
        //    {
        //        return default;
        //    }

        //    EntryInfo entryInfo = m_FilteredInfos[row];
        //    return (entryInfo.text, (int)entryInfo.flags, entryInfo.entryCount, entryInfo.searchIndex, entryInfo.searchEndIndex);
        //}

        /// <summary>
        /// 根据输出类型获取输出数量
        /// </summary>
        /// <param name="errorCount">报错数量</param>
        /// <param name="warningCount">警告数量</param>
        /// <param name="logCount">日志数量</param>
        public void GetCountsByType(ref int errorCount, ref int warningCount, ref int logCount)
        {
            errorCount = m_TypeCounts[0];
            warningCount = m_TypeCounts[1];
            logCount = m_TypeCounts[2];
        }

        /// <summary>
        /// 设置选择的条目
        /// </summary>
        /// <param name="row">选择的行</param>
        /// <returns>条目实例的ID</returns>
        public int SetSelectedEntry(int row)
        {
            m_SelectedInfo = null;//清空原本的目标
            if (row < 0 || row >= m_FilteredInfos.Count)//如果选择了不合法的值
            {
                return 0;//直接返回
            }

            m_SelectedInfo = m_FilteredInfos[row];//设置选择的值
            return m_SelectedInfo.entry.instanceID;//返回目标ID
        }

        /// <summary>
        /// 检查某个条目是否被选中
        /// </summary>
        /// <param name="row">要判断的条目</param>
        /// <returns>是否被选中</returns>
        public bool IsEntrySelected(int row)
        {
            if (row < 0 || row >= m_FilteredInfos.Count)//如果传入的条目不合法
            {
                return false;//直接返回
            }

            return m_FilteredInfos[row] == m_SelectedInfo;//返回目标行是否是当前选中的行
        }

        /// <summary>
        /// 检查当前选中的条目是否显示
        /// </summary>
        /// <returns>显示：true</returns>
        public bool IsSelectedEntryShow()
        {
            return m_SelectedInfo != null && m_FilteredInfos.Contains(m_SelectedInfo);//如果 选中条目不为空且条目在被过滤后的列表中返回true
        }

        /// <summary>
        /// 获取当前选中条目的行数
        /// </summary>
        /// <returns></returns>
        public int GetSelectedEntryIndex()
        {
            if (m_SelectedInfo != null)//检查当前选中行是否为空
            {
                for (int i = 0; i < m_FilteredInfos.Count; i++)//遍历所有的行
                {
                    if (m_FilteredInfos[i] == m_SelectedInfo)//如果目标行的条目和当前条目相等
                    {
                        return i;//返回行号
                    }
                }
            }
            return -1;//如果当前条目为空或在所有行中没有当前条目返回-1
        }

        /// <summary>
        /// 获取过滤后第一个报错信息的行数
        /// </summary>
        /// <returns>首个报错的行数</returns>
        public int GetFirstErrorEntryIndex()
        {
            for (int i = 0; i < m_FilteredInfos.Count; i++)//遍历所有的行
            {
                if (m_FilteredInfos[i].flags == ConsoleFlags.LogLevelError)//找到第一个报错信息
                {
                    return i;//返回行数
                }
            }
            return -1;//如果没有找到，返回-1
        }

        /// <summary>
        /// 更新条目列表
        /// </summary>
        public void UpdateEntries()
        {
            CheckInit();//检查是否更新
            int flags = LogEntries.consoleFlags;//获取所有的
            LogEntries.SetConsoleFlag((int)ConsoleFlags.LogLevelLog, true);
            LogEntries.SetConsoleFlag((int)ConsoleFlags.LogLevelWarning, true);
            LogEntries.SetConsoleFlag((int)ConsoleFlags.LogLevelError, true);
            LogEntries.SetConsoleFlag((int)ConsoleFlags.Collapse, collapse);
            int count = LogEntries.GetCount();
            if (count == m_LastEntryCount)
            {
                LogEntries.consoleFlags = flags;
                return;
            }

            if (m_LastEntryCount > count)
            {
                ClearEntries();
            }

            LogEntries.SetConsoleFlag((int)ConsoleFlags.ShowTimestamp, true);
            LogEntries.StartGettingEntries();
            for (int i = m_LastEntryCount; i < count; i++)
            {
                LogEntry entry = new LogEntry();
                if (!UnityEditor.LogEntries.GetEntryInternal(i, entry))
                {
                    continue;
                }

                int mode = 0;
                string text = null;
                LogEntries.GetLinesAndModeFromEntryInternal(i, 10, ref mode, ref text);

                int entryCount = 0;
                if (collapse)
                {
                    entryCount = LogEntries.GetEntryCount(i);
                }
                AddEntry(i, entry, text, entryCount);
            }
            LogEntries.EndGettingEntries();
            LogEntries.consoleFlags = flags;
            m_LastEntryCount = count;

            CheckSearchStringChanged();
        }

        /// <summary>
        /// 清空所有条目
        /// </summary>
        private void ClearEntries()
        {
            m_SelectedInfo = null;//清除条目选择信息
            m_EntryInfos.Clear();//清空所有条目
            m_FilteredInfos.Clear();//清空所有被过滤的条目
            m_LastEntryCount = -1;
            m_TypeCounts = new[] { 0, 0, 0 };
        }

        /// <summary>
        /// 添加一个条目
        /// </summary>
        /// <param name="row">行数</param>
        /// <param name="entry">条目</param>
        /// <param name="text">文本</param>
        /// <param name="entryCount">条目号</param>
        private void AddEntry(int row, LogEntry entry, string lines, int entryCount)
        {
            EntryInfo entryInfo = new EntryInfo
            {
                row = row,
                lines = lines,
                text = lines,
                entryCount = entryCount,
                flags = GetConsoleFlagFromMode(entry.mode),
                entry = entry
            };

            if (showTimestamp)
            {
                entryInfo.timeText = lines.Substring(0, lines.IndexOf("]"));
                entryInfo.text = lines.Substring(lines.IndexOf("]") + 2);
            }
            else
            {
                entryInfo.timeText = "";
            }


            if (entryInfo.text.Substring(0, 3) == "#!@")
            {
                int index_1 = entryInfo.text.IndexOf(":");
                int index_2 = entryInfo.text.IndexOf(";");

                entryInfo.logGroup = entryInfo.text.Substring(3, index_1 - 3);
                entryInfo.logType = entryInfo.text.Substring(index_1 + 1, index_2 - index_1 - 1);
                int i = entryInfo.text.IndexOf(";") + 1;
                int j = entryInfo.text.IndexOf("#!!") - i;
                entryInfo.text = entryInfo.text.Substring(i, j);
            }
            else
            {
                entryInfo.logGroup = "System";

                switch (entryInfo.flags)
                {
                    case ConsoleFlags.LogLevelLog:
                        entryInfo.logType = "Log";
                        break;
                    case ConsoleFlags.LogLevelWarning:
                        entryInfo.logType = "Warning";
                        break;
                    case ConsoleFlags.LogLevelError:
                        entryInfo.logType = "Error";
                        break;

                }
            }
            entryInfo.pure = GetPureLines(entryInfo.text, out entryInfo.HTMLTagPosInfos);
            entryInfo.lower = entryInfo.pure.ToLower();//将全部转换为小写
            m_EntryInfos.Add(entryInfo);//将条目信息添加到条目列表中

            bool hasSearchString = !string.IsNullOrEmpty(m_SearchString);
            string searchStringValue = null;
            int searchStringLen = 0;
            if (hasSearchString)
            {
                searchStringValue = m_SearchString.ToLower();
                searchStringLen = searchStringValue.Length;
            }

            // 没有将堆栈都进行搜索，以免信息太杂，只根据行数，但是变化行数时不会重新搜索
            if (HasFlag((int)entryInfo.flags) && m_CustomFilters.HasFilters(entryInfo.lower) && (!hasSearchString ||
                  (entryInfo.searchIndex = entryInfo.lower.IndexOf(searchStringValue, StringComparison.Ordinal)) != -1))
            {
                SearchIndexToTagIndex(entryInfo, searchStringLen);
                m_FilteredInfos.Add(entryInfo);
            }

            if (entryInfo.flags == ConsoleFlags.LogLevelError)
            {
                m_TypeCounts[0]++;
            }
            else if (entryInfo.flags == ConsoleFlags.LogLevelWarning)
            {
                m_TypeCounts[1]++;
            }
            else
            {
                m_TypeCounts[2]++;
            }
        }


        /// <summary>
        /// 检查是否初始化
        /// </summary>
        private void CheckInit()
        {
            if (m_Init)//如果已经初始胡
            {
                return;//直接返回
            }

            m_Init = true;//设置已经初始化
            m_ConsoleFlagsComing = EditorPrefs.GetInt(kPrefConsoleFlags, 896);//设置稍后需要修改的控制台特性标识
            m_ShowTimestamp = EditorPrefs.GetBool(kPrefShowTimestamp, false);//设置是否显示时间
            m_Collapse = EditorPrefs.GetBool(kPrefCollapse, false);//设置是否折叠
            m_WrapperInfos.Clear();//清空所有信息
            m_WrapperInfos.AddRange(EditorPrefs.GetString(kPrefWrappers, string.Empty).Split('\n'));//获取包装信息
            m_CustomFilters.Load();//加载自定义过滤器
        }

        /// <summary>
        /// 检查选择字符串是否发生改变
        /// </summary>
        /// <returns>是否发生了改变</returns>
        private bool CheckSearchStringChanged()
        {
            if (m_LastSearchStringTime > 1f && m_LastSearchStringTime < EditorApplication.timeSinceStartup)
            {
                m_LastSearchStringTime = -1f;
                if (!string.IsNullOrEmpty(m_SearchString))
                {
                    if (searchHistory[0].Length == 0)
                    {
                        ArrayUtility.RemoveAt(ref searchHistory, 0);
                    }
                    else
                    {
                        ArrayUtility.Remove(ref searchHistory, m_SearchString);
                    }
                    ArrayUtility.Insert(ref searchHistory, 0, m_SearchString);
                    if (searchHistory.Length > 10)
                    {
                        ArrayUtility.RemoveAt(ref searchHistory, 10);
                    }
                }
            }

            bool customFiltersChangedValue = m_CustomFilters.IsChanged();
            if (m_SearchString == m_SearchStringComing && m_ConsoleFlags == m_ConsoleFlagsComing && !customFiltersChangedValue)
            {
                return false;
            }

            bool hasSearchString = !string.IsNullOrEmpty(m_SearchStringComing);
            bool startsWithValue = hasSearchString && !string.IsNullOrEmpty(m_SearchString)
                                   && m_SearchStringComing.StartsWith(m_SearchString, StringComparison.Ordinal);
            bool flagsChangedValue = m_ConsoleFlags != m_ConsoleFlagsComing;
            m_ConsoleFlags = m_ConsoleFlagsComing;
            string searchStringValue = null;
            int searchStringLen = 0;
            if (hasSearchString)
            {
                searchStringValue = m_SearchStringComing.ToLower();
                searchStringLen = searchStringValue.Length;
            }

            if (flagsChangedValue || !startsWithValue || customFiltersChangedValue)
            {
                m_FilteredInfos.Clear();

                foreach (var entryInfo in m_EntryInfos)
                {
                    if (HasFlag((int)entryInfo.flags) && m_CustomFilters.HasFilters(entryInfo.lower) && (!hasSearchString
                        || (entryInfo.searchIndex = entryInfo.lower.IndexOf(searchStringValue, StringComparison.Ordinal)) != -1))
                    {
                        SearchIndexToTagIndex(entryInfo, searchStringLen);
                        m_FilteredInfos.Add(entryInfo);
                    }
                }
            }
            else
            {
                for (int i = m_FilteredInfos.Count - 1; i >= 0; i--)
                {
                    if ((m_FilteredInfos[i].searchIndex = m_FilteredInfos[i].lower.IndexOf(searchStringValue, StringComparison.Ordinal)) == -1)
                    {
                        m_FilteredInfos.RemoveAt(i);
                    }
                    else
                    {
                        SearchIndexToTagIndex(m_FilteredInfos[i], searchStringLen);
                    }
                }
            }

            m_SearchString = m_SearchStringComing;
            if (hasSearchString)
            {
                m_LastSearchStringTime = EditorApplication.timeSinceStartup + 3f;
            }

            if (flagsChangedValue)
            {
                EditorPrefs.SetInt(kPrefConsoleFlags, m_ConsoleFlags);
            }

            if (customFiltersChangedValue)
            {
                m_CustomFilters.ClearChanged();
                m_CustomFilters.Save();
            }

            searchFrame = IsSelectedEntryShow();
            return true;
        }

        /// <summary>
        /// 判断两个模式是否匹配
        /// </summary>
        /// <param name="mode">匹配模式</param>
        /// <param name="modeToCheck">匹配模式</param>
        /// <returns>是否匹配</returns>
        private bool HasMode(int mode, Mode modeToCheck) { return (mode & (int)modeToCheck) != 0; }

        /// <summary>
        /// 根据模式获取控制台输出模式
        /// </summary>
        /// <param name="mode">模式</param>
        /// <returns>输出模式</returns>
        private ConsoleFlags GetConsoleFlagFromMode(int mode)
        {
            // Errors
            if (HasMode(mode, Mode.Fatal | Mode.Assert |
                              Mode.Error | Mode.ScriptingError |
                              Mode.AssetImportError | Mode.ScriptCompileError |
                              Mode.GraphCompileError | Mode.ScriptingAssertion))
            {
                return ConsoleFlags.LogLevelError;
            }
            // Warnings
            if (HasMode(mode, Mode.ScriptCompileWarning | Mode.ScriptingWarning | Mode.AssetImportWarning))
            {
                return ConsoleFlags.LogLevelWarning;
            }
            // Logs
            return ConsoleFlags.LogLevelLog;
        }

        public void ExportLog()
        {
            string filePath = EditorUtility.SaveFilePanel("Export Log", "",
                "Console Log " + string.Format("{0:HHmm}", DateTime.Now) + ".txt", "txt");
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            StringBuilder sb = new StringBuilder();
            foreach (var entryInfo in m_FilteredInfos)
            {
                sb.AppendLine(entryInfo.entry.condition);
            }
            File.WriteAllText(filePath, sb.ToString());
        }

        #region HTMLTag

        private const int kTagQuadIndex = 5;

        private readonly StringBuilder m_StringBuilder = new StringBuilder();
        private readonly Stack<int> m_TagStack = new Stack<int>();

        private readonly string[] m_TagStrings = new string[]
        {
                "b",
                "i",
                "color",
                "size",
                "material",
                "quad",
                "x",
                "y",
                "width",
                "height",
        };


        /// <summary>
        /// 获取一个tag在整个字符串中的位置
        /// </summary>
        /// <param name="input">整个字符串</param>
        /// <param name="pos">当前扫描的位置</param>
        /// <param name="closing">如果是HTMLtag  </param>
        /// <returns></returns>
        private int GetTagIndex(string input, ref int pos, out bool closing)
        {
            closing = false;
            if (input[pos] == '<')
            {
                int inputLen = input.Length;
                int nextPos = pos + 1;
                if (nextPos == inputLen)
                {
                    return -1;
                }

                closing = input[nextPos] == '/';
                if (closing)
                {
                    nextPos++;
                }

                for (int i = 0; i < m_TagStrings.Length; i++)
                {
                    var tagString = m_TagStrings[i];
                    bool find = true;

                    for (int j = 0; j < tagString.Length; j++)
                    {
                        int pingPos = nextPos + j;
                        if (pingPos == inputLen || char.ToLower(input[pingPos]) != tagString[j])
                        {
                            find = false;
                            break;
                        }
                    }

                    if (find)
                    {
                        int endPos = nextPos + tagString.Length;
                        if (endPos == inputLen)
                        {
                            continue;
                        }

                        if ((!closing && input[endPos] == '=') || (input[endPos] == ' ' && i == kTagQuadIndex))
                        {
                            while (input[endPos] != '>' && endPos < inputLen)
                            {
                                endPos++;
                            }
                        }

                        if (input[endPos] != '>')
                        {
                            continue;
                        }

                        pos = endPos;
                        return i;
                    }
                }
            }
            return -1;
        }


        /// <summary>
        /// 获取去除标签的文本信息
        /// </summary>
        /// <param name="input"></param>
        /// <param name="posList"></param>
        /// <returns></returns>
        private string GetPureLines(string input, out List<int> posList)
        {
            m_StringBuilder.Length = 0;
            m_TagStack.Clear();
            posList = null;

            int preStrPos = 0;
            int pos = 0;
            while (pos < input.Length)
            {
                int oldPos = pos;
                bool closing;
                int tagIndex = GetTagIndex(input, ref pos, out closing);
                if (tagIndex != -1)
                {
                    if (closing)
                    {
                        if (m_TagStack.Count == 0 || m_TagStack.Pop() != tagIndex)
                        {
                            posList = null;
                            return input;
                        }
                    }

                    if (posList == null)
                    {
                        posList = new List<int>();
                    }
                    posList.Add(oldPos);
                    posList.Add(pos);

                    if (preStrPos != oldPos)
                    {
                        m_StringBuilder.Append(input, preStrPos, oldPos - preStrPos);
                    }
                    preStrPos = pos + 1;

                    if (closing || tagIndex == kTagQuadIndex)
                    {
                        continue;
                    }

                    m_TagStack.Push(tagIndex);
                }
                pos++;
            }

            if (m_TagStack.Count > 0)
            {
                posList = null;
                return input;
            }

            if (preStrPos > 0 && preStrPos < input.Length)
            {
                m_StringBuilder.Append(input, preStrPos, input.Length - preStrPos);
            }
            if (m_StringBuilder.Length > 0)
            {
                return m_StringBuilder.ToString();
            }

            return input;
        }

        private int GetOriginalCharIndex(int idx, List<int> posList)
        {
            if (posList == null || posList.Count == 0)
            {
                return idx;
            }

            int idx2 = 0;
            for (int i = 0; i < posList.Count && (i + 1) < posList.Count;)
            {
                int idx1 = idx2;
                if ((i - 1) > 0)
                {
                    idx2 += posList[i] - posList[i - 1] - 1;
                }
                else
                {
                    idx2 = posList[i] - 1;
                }

                if (idx >= idx1 && idx <= idx2)
                {
                    if ((i - 1) > 0)
                    {
                        return posList[i - 1] + idx - idx1;
                    }

                    return idx;
                }

                i += 2;
            }

            return posList[posList.Count - 1] + idx - idx2;
        }

        private void SearchIndexToTagIndex(EntryInfo entryInfo, int searchLength)
        {
            if (entryInfo.searchIndex == -1)
            {
                return;
            }

            entryInfo.searchEndIndex = GetOriginalCharIndex(entryInfo.searchIndex + searchLength,
                entryInfo.HTMLTagPosInfos);
            entryInfo.searchIndex = GetOriginalCharIndex(entryInfo.searchIndex, entryInfo.HTMLTagPosInfos);
        }

        #endregion

        #region CustomFilters

        public class CustomFiltersItem
        {
            private string m_Filter;
            private bool m_Toggle;

            public bool changed { get; set; }

            public string filter
            {
                get { return m_Filter; }
                set
                {
                    if (value != m_Filter)
                    {
                        m_Filter = value.ToLower();
                        if (toggle)
                        {
                            changed = true;
                        }
                    }
                }
            }

            public bool toggle
            {
                get { return m_Toggle; }
                set
                {
                    if (value != m_Toggle)
                    {
                        m_Toggle = value;
                        changed = true;
                    }
                }
            }
        }

        public class CustomFiltersGroup
        {
            public readonly List<CustomFiltersItem> filters = new List<CustomFiltersItem>();
            public bool changed { get; set; }

            public bool IsChanged()
            {
                foreach (var filter in filters)
                {
                    if (filter.changed)
                    {
                        return true;
                    }
                }
                return changed;
            }

            public void ClearChanged()
            {
                changed = false;
                foreach (var filter in filters)
                {
                    filter.changed = false;
                }
            }

            public bool HasFilters(string input)
            {
                foreach (var filter in filters)
                {
                    if (filter.toggle && !input.Contains(filter.filter))
                    {
                        return false;
                    }
                }

                return true;
            }

            public void Load()
            {
                filters.Clear();
                var val = EditorPrefs.GetString(kPrefCustomFilters, String.Empty);
                if (string.IsNullOrEmpty(val))
                {
                    return;
                }

                var vals = val.Split('\n');
                try
                {
                    for (int i = 0; i < vals.Length && (i + 1) < vals.Length; i++)
                    {
                        var item = new CustomFiltersItem { filter = vals[i], toggle = bool.Parse(vals[i + 1]) };
                        filters.Add(item);
                        i++;
                    }
                }
                catch
                {
                    // ignored
                }
            }

            public void Save()
            {
                StringBuilder sb = new StringBuilder();
                foreach (var filter in filters)
                {
                    sb.Append(filter.filter);
                    sb.Append('\n');
                    sb.Append(filter.toggle.ToString());
                    sb.Append('\n');
                }
                EditorPrefs.SetString(kPrefCustomFilters, sb.ToString());
            }
        }

        public CustomFiltersGroup customFilters
        {
            get { return m_CustomFilters; }
        }

        #endregion

        #region Stacktrace

        internal class Constants
        {
            public static string colorNamespace, colorNamespaceAlpha;
            public static string colorClass, colorClassAlpha;
            public static string colorMethod, colorMethodAlpha;
            public static string colorParameters, colorParametersAlpha;
            public static string colorPath, colorPathAlpha;
            public static string colorFilename, colorFilenameAlpha;
        }

        /// <summary>
        /// 是否存在目标的跟踪信息
        /// </summary>
        /// <returns></returns>
        public bool StacktraceListView_IsExist()
        {
            return m_SelectedInfo != null && m_SelectedInfo.stacktraceLineInfos != null;//如果选择的条目不为空且选择的条目的跟踪堆栈信息不为空：返回true
        }

        /// <summary>
        /// 获取当前条目跟踪堆栈信息的数量
        /// </summary>
        /// <returns>跟踪堆栈信息的数量</returns>
        public int StacktraceListView_GetCount()
        {
            return StacktraceListView_IsExist() && IsSelectedEntryShow() ? m_SelectedInfo.stacktraceLineInfos.Count : 0;
        }

        /// <summary>
        /// 获取目标行条目跟踪堆栈的文本信息
        /// </summary>
        /// <param name="row">目标条目</param>
        /// <returns>目标条目的文本信息</returns>
        public string StacktraceListView_GetLine(int row)
        {
            if (StacktraceListView_IsExist())
            {
                StacktraceLineInfo info = m_SelectedInfo.stacktraceLineInfos[row];
                if (info.filePath == null && info.text.Substring(0, 3) == "#!@")
                {
                    return ConsoleManager.Instence.GetItemText(m_SelectedInfo);
                }
                return m_SelectedInfo.stacktraceLineInfos[row].text;
            }
            return string.Empty;
        }

        /// <summary>
        /// 显示跟踪堆栈，获取最大宽度
        /// </summary>
        /// <param name="tempContent">用于包含目标文本的UI内容</param>
        /// <param name="tempStyle">计算包含UI内容的UI样式</param>
        /// <returns>使用tempContent包含 最长的跟踪堆栈文本 时，所使用的tempStyle的宽度</returns>
        public float StacktraceListView_GetMaxWidth(GUIContent tempContent, GUIStyle tempStyle)
        {
            if (m_SelectedInfo == null || !IsSelectedEntryShow())//如果没有选择条目或选择的条目没有显示
            {
                return 1f;//直接返回
            }

            if (!StacktraceListView_IsExist())//如果不存在目标跟踪信息
            {
                StacktraceListView_Parse(m_SelectedInfo);//解析跟踪堆栈信息
            }

            var maxLine = -1;//最长的行
            var maxLineLen = -1;//最长的行的长度
            for (int i = 0; i < m_SelectedInfo.stacktraceLineInfos.Count; i++)//遍历所有的行
            {
                if (maxLineLen < m_SelectedInfo.stacktraceLineInfos[i].plain.Length)//如果之前的最长的行的长度小于当前行长度
                {
                    maxLineLen = m_SelectedInfo.stacktraceLineInfos[i].plain.Length;//设置最长行长度为当前行长度
                    maxLine = i;//设置最长行为当前行
                }
            }

            float maxWidth = 1f;//最大宽度
            if (maxLine != -1)//如果有最长行
            {
                tempContent.text = m_SelectedInfo.stacktraceLineInfos[maxLine].plain;//设置目标UIContent的文本为最长文本
                maxWidth = tempStyle.CalcSize(tempContent).x;//计算样式的最大宽度
            }

            return maxWidth;//返回最大宽度
        }

        #region 解析跟踪堆栈信息
        /// <summary>
        /// 解析跟踪堆栈信息
        /// </summary>
        /// <param name="entryInfo">目标条目</param>
        private void StacktraceListView_Parse(EntryInfo entryInfo)
        {
            var lines = entryInfo.lines.Split(new char[] { '\n' }, StringSplitOptions.None);
            entryInfo.stacktraceLineInfos = new List<StacktraceLineInfo>(lines.Length);

            string rootDirectory = System.IO.Path.Combine(Application.dataPath, "..");
            Uri uriRoot = new Uri(rootDirectory);
            string textBeforeFilePath = ") (at ";
            string textUnityEngineDebug = "UnityEngine.Debug";
#if UNITY_2019_1_OR_NEWER
                string fileInBuildSlave = "D:/unity/";
#else
            string fileInBuildSlave = "C:/buildslave/unity/";
#endif
            string luaCFunction = "[C]";
            string luaMethodBefore = ": in function ";
            string luaFileExt = ".lua";
            string luaAssetPath = "Assets/Lua/";
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (i == lines.Length - 1 && string.IsNullOrEmpty(line))
                {
                    continue;
                }
                if (line.StartsWith(textUnityEngineDebug))
                {
                    continue;
                }

                StacktraceLineInfo info = new StacktraceLineInfo();
                info.plain = line;
                info.text = info.plain;
                entryInfo.stacktraceLineInfos.Add(info);

                if (i == 0)
                {
                    continue;
                }

                if (!StacktraceListView_Parse_CSharp(line, info, textBeforeFilePath, fileInBuildSlave, uriRoot))
                {
                    StacktraceListView_Parse_Lua(line, info, luaCFunction, luaMethodBefore, luaFileExt, luaAssetPath);
                }
            }
        }

        /// <summary>
        /// 解析C#跟踪堆栈信息
        /// </summary>
        /// <param name="line"></param>
        /// <param name="info"></param>
        /// <param name="textBeforeFilePath"></param>
        /// <param name="fileInBuildSlave"></param>
        /// <param name="uriRoot"></param>
        /// <returns></returns>
        private bool StacktraceListView_Parse_CSharp(string line, StacktraceLineInfo info, string textBeforeFilePath, string fileInBuildSlave, Uri uriRoot)
        {
            int methodLastIndex = line.IndexOf('(');
            if (methodLastIndex <= 0)
            {
                return false;
            }
            int argsLastIndex = line.IndexOf(')', methodLastIndex);
            if (argsLastIndex <= 0)
            {
                return false;
            }
            int methodFirstIndex = line.LastIndexOf(':', methodLastIndex);
            if (methodFirstIndex <= 0)
            {
                methodFirstIndex = line.LastIndexOf('.', methodLastIndex);
                if (methodFirstIndex <= 0)
                {
                    return false;
                }
            }
            string methodString = line.Substring(methodFirstIndex + 1, methodLastIndex - methodFirstIndex - 1);

            string classString;
            string namespaceString = String.Empty;
            int classFirstIndex = line.LastIndexOf('.', methodFirstIndex - 1);
            if (classFirstIndex <= 0)
            {
                classString = line.Substring(0, methodFirstIndex + 1);
            }
            else
            {
                classString = line.Substring(classFirstIndex + 1, methodFirstIndex - classFirstIndex);
                namespaceString = line.Substring(0, classFirstIndex + 1);
            }

            string argsString = line.Substring(methodLastIndex, argsLastIndex - methodLastIndex + 1);
            string fileString = String.Empty;
            string fileNameString = String.Empty;
            string fileLineString = String.Empty;
            bool alphaColor = true;

            int filePathIndex = line.IndexOf(textBeforeFilePath, argsLastIndex, StringComparison.Ordinal);
            if (filePathIndex > 0)
            {
                filePathIndex += textBeforeFilePath.Length;
                if (line[filePathIndex] != '<') // sometimes no url is given, just an id between <>, we can't do an hyperlink
                {
                    string filePathPart = line.Substring(filePathIndex);
                    int lineIndex = filePathPart.LastIndexOf(":", StringComparison.Ordinal); // LastIndex because the url can contain ':' ex:"C:"
                    if (lineIndex > 0)
                    {
                        int endLineIndex = filePathPart.LastIndexOf(")", StringComparison.Ordinal); // LastIndex because files or folder in the url can contain ')'
                        if (endLineIndex > 0)
                        {
                            string lineString =
                                filePathPart.Substring(lineIndex + 1, (endLineIndex) - (lineIndex + 1));
                            string filePath = filePathPart.Substring(0, lineIndex);

                            bool isInBuildSlave = filePath.StartsWith(fileInBuildSlave, StringComparison.Ordinal);
                            if (!isInBuildSlave)
                            {
                                alphaColor = false;
                            }

                            info.filePath = filePath;
                            info.lineNum = int.Parse(lineString);

                            if (filePath.Length > 2 && filePath[1] == ':' && !isInBuildSlave)
                            {
                                Uri uriFile = new Uri(filePath);
                                Uri relativeUri = uriRoot.MakeRelativeUri(uriFile);
                                string relativePath = relativeUri.ToString();
                                if (!string.IsNullOrEmpty(relativePath))
                                {
                                    info.plain = info.plain.Replace(filePath, relativePath);
                                    filePath = relativePath;
                                }
                            }

                            fileNameString = System.IO.Path.GetFileName(filePath);
                            fileString = textBeforeFilePath.Substring(1) + filePath.Substring(0, filePath.Length - fileNameString.Length);
                            fileLineString = filePathPart.Substring(lineIndex, endLineIndex - lineIndex + 1);
                        }
                    }
                }
                else
                {
                    fileString = line.Substring(argsLastIndex + 1);
                }
            }

            if (alphaColor)
            {
                info.text =
                    string.Format("<color=#{0}>{1}</color>", Constants.colorNamespaceAlpha, namespaceString) +
                    string.Format("<color=#{0}>{1}</color>", Constants.colorClassAlpha, classString) +
                    string.Format("<color=#{0}>{1}</color>", Constants.colorMethodAlpha, methodString) +
                    string.Format("<color=#{0}>{1}</color>", Constants.colorParametersAlpha, argsString) +
                    string.Format("<color=#{0}>{1}</color>", Constants.colorPathAlpha, fileString) +
                    string.Format("<color=#{0}>{1}</color>", Constants.colorFilenameAlpha, fileNameString) +
                    string.Format("<color=#{0}>{1}</color>", Constants.colorPathAlpha, fileLineString);
            }
            else
            {
                info.text = string.Format("<color=#{0}>{1}</color>", Constants.colorNamespace, namespaceString) +
                            string.Format("<color=#{0}>{1}</color>", Constants.colorClass, classString) +
                            string.Format("<color=#{0}>{1}</color>", Constants.colorMethod, methodString) +
                            string.Format("<color=#{0}>{1}</color>", Constants.colorParameters, argsString) +
                            string.Format("<color=#{0}>{1}</color>", Constants.colorPath, fileString) +
                            string.Format("<color=#{0}>{1}</color>", Constants.colorFilename, fileNameString) +
                            string.Format("<color=#{0}>{1}</color>", Constants.colorPath, fileLineString);
                info.wrapper = namespaceString + classString;
                if (info.wrapper.Length > 0)
                {
                    info.wrapper = info.wrapper.Remove(info.wrapper.Length - 1);
                }
            }

            return true;
        }

        /// <summary>
        /// 解析Lua跟踪堆栈信息
        /// </summary>
        /// <param name="line"></param>
        /// <param name="info"></param>
        /// <param name="luaCFunction"></param>
        /// <param name="luaMethodBefore"></param>
        /// <param name="luaFileExt"></param>
        /// <param name="luaAssetPath"></param>
        /// <returns></returns>
        private bool StacktraceListView_Parse_Lua(string line, StacktraceLineInfo info, string luaCFunction, string luaMethodBefore, string luaFileExt, string luaAssetPath)
        {
            if (string.IsNullOrEmpty(line) || line[0] != '	')
            {
                return false;
            }

            string preMethodString = line;
            string methodString = String.Empty;
            int methodFirstIndex = line.IndexOf(luaMethodBefore, StringComparison.Ordinal);
            if (methodFirstIndex > 0)
            {
                methodString = line.Substring(methodFirstIndex + luaMethodBefore.Length);
                preMethodString = preMethodString.Remove(methodFirstIndex + luaMethodBefore.Length);
            }

            bool cFunction = line.IndexOf(luaCFunction, 1, StringComparison.Ordinal) == 1;
            if (!cFunction)
            {
                int lineIndex = line.IndexOf(':');
                if (lineIndex > 0)
                {
                    int endLineIndex = line.IndexOf(':', lineIndex + 1);
                    if (endLineIndex > 0)
                    {
                        string lineString =
                            line.Substring(lineIndex + 1, (endLineIndex) - (lineIndex + 1));
                        string filePath = line.Substring(1, lineIndex - 1);
                        if (!filePath.EndsWith(luaFileExt, StringComparison.Ordinal))
                        {
                            filePath += luaFileExt;
                        }

                        if (!int.TryParse(lineString, out info.lineNum))
                        {
                            return false;
                        }
                        info.filePath = luaAssetPath + filePath;

                        string namespaceString = String.Empty;
                        int classFirstIndex = filePath.LastIndexOf('/');
                        if (classFirstIndex > 0)
                        {
                            namespaceString = filePath.Substring(0, classFirstIndex + 1);
                        }

                        string classString = filePath.Substring(classFirstIndex + 1,
                            filePath.Length - namespaceString.Length - luaFileExt.Length);


                        info.text = string.Format("	<color=#{0}>{1}</color>", Constants.colorNamespace,
                                        namespaceString) +
                                    string.Format("<color=#{0}>{1}</color>", Constants.colorClass, classString) +
                                    string.Format("<color=#{0}>:{1}</color>", Constants.colorPath, lineString) +
                                    string.Format("<color=#{0}>{1}</color>", Constants.colorPath, luaMethodBefore) +
                                    string.Format("<color=#{0}>{1}</color>", Constants.colorMethod, methodString);
                    }
                }
            }
            else
            {
                info.text = string.Format("<color=#{0}>{1}</color>", Constants.colorPathAlpha, preMethodString) +
                            string.Format("<color=#{0}>{1}</color>", Constants.colorMethodAlpha, methodString);
            }

            return true;
        }
        #endregion

        /// <summary>
        /// 检查目标条目是否可以打开
        /// </summary>
        /// <param name="stacktraceLineInfoIndex">目标条目行号</param>
        /// <returns>是否可以打开</returns>
        public bool StacktraceListView_CanOpen(int stacktraceLineInfoIndex)
        {
            if (!StacktraceListView_IsExist())//如果没有跟踪堆栈信息
            {
                return false;//不能打开
            }

            if (stacktraceLineInfoIndex < m_SelectedInfo.stacktraceLineInfos.Count)//如果选择的目标是合法的
            {
                return !string.IsNullOrEmpty(m_SelectedInfo.stacktraceLineInfos[stacktraceLineInfoIndex].filePath);//返回目标是否包含文件地址
            }
            return false;//不能打开
        }
        /// <summary>
        /// 检查目标条目是否可以被包装
        /// </summary>
        /// <param name="stacktraceLineInfoIndex">目标条目行号</param>
        /// <returns>是否可以被包装</returns>
        public bool StacktraceListView_CanWrapper(int stacktraceLineInfoIndex)
        {
            if (!StacktraceListView_IsExist())//如果没有跟踪堆栈信息
            {
                return false;//不能包装
            }

            if (stacktraceLineInfoIndex < m_SelectedInfo.stacktraceLineInfos.Count)//如果选择的目标是合法的
            {
                return !string.IsNullOrEmpty(m_SelectedInfo.stacktraceLineInfos[stacktraceLineInfoIndex].wrapper);//返回目标是否包含包装信息
            }
            return false;//不能包装
        }
        /// <summary>
        /// 检查目标条目是否已经被包装
        /// </summary>
        /// <param name="stacktraceLineInfoIndex">目标条目行号</param>
        /// <returns>是否可以被包装</returns>
        public bool StacktraceListView_IsWrapper(int stacktraceLineInfoIndex)
        {
            if (!StacktraceListView_IsExist())//如果没有跟踪堆栈信息
            {
                return false;//没有包装
            }

            if (stacktraceLineInfoIndex < m_SelectedInfo.stacktraceLineInfos.Count)//如果选择的目标是合法的
            {
                return !string.IsNullOrEmpty(m_SelectedInfo.stacktraceLineInfos[stacktraceLineInfoIndex].wrapper) && m_WrapperInfos.Contains(m_SelectedInfo.stacktraceLineInfos[stacktraceLineInfoIndex].wrapper);//目标条目的包装不为空且包装列表中包含该目标条目
            }
            return false;//没有包装
        }
        /// <summary>
        /// 双击打开
        /// </summary>
        public void StacktraceListView_RowGotDoubleClicked()
        {
            if (!StacktraceListView_IsExist())//如果没有跟踪堆栈
            {
                return;//直接返回
            }

            for (var i = 0; i < m_SelectedInfo.stacktraceLineInfos.Count; i++)
            {
                var stacktraceLineInfo = m_SelectedInfo.stacktraceLineInfos[i];
                if (!string.IsNullOrEmpty(stacktraceLineInfo.filePath))
                {
                    if (string.IsNullOrEmpty(stacktraceLineInfo.wrapper) || !m_WrapperInfos.Contains(stacktraceLineInfo.wrapper))
                    {
                        StacktraceListView_Open(i);
                        break;
                    }
                }
            }
        }
        /// <summary>
        /// 打开跟踪堆栈文件
        /// </summary>
        /// <param name="userData">目标条目行号</param>
        public void StacktraceListView_Open(object userData)
        {
            if (!StacktraceListView_IsExist())//如果没有跟踪堆栈
            {
                return;//直接返回
            }

            var stacktraceLineInfoIndex = (int)userData;
            if (stacktraceLineInfoIndex < m_SelectedInfo.stacktraceLineInfos.Count)
            {
                var filePath = m_SelectedInfo.stacktraceLineInfos[stacktraceLineInfoIndex].filePath;
                var lineNum = m_SelectedInfo.stacktraceLineInfos[stacktraceLineInfoIndex].lineNum;
                ScriptAssetOpener.OpenAsset(filePath, lineNum);
            }
        }
        /// <summary>
        /// 将一条新的跟踪堆栈信息打包到EditorPrefs数据中
        /// </summary>
        /// <param name="userData">行号</param>
        public void StacktraceListView_Wrapper(object userData)
        {
            if (!StacktraceListView_IsExist())//如果不存在跟踪堆栈信息
            {
                return;//直接返回
            }

            var stacktraceLineInfoIndex = (int)userData;//定义选中行
            if (stacktraceLineInfoIndex < m_SelectedInfo.stacktraceLineInfos.Count)//如果选中行合法
            {
                var wrapper = m_SelectedInfo.stacktraceLineInfos[stacktraceLineInfoIndex].wrapper;//获取目标跟踪堆栈的包装信息
                if (m_WrapperInfos.Contains(wrapper))//检查是否已经包含目标数据
                {
                    m_WrapperInfos.Remove(wrapper);//如果已经包含，则删除
                }
                else
                {
                    m_WrapperInfos.Add(wrapper);//如果不包含，则添加
                }
                StringBuilder sb = new StringBuilder();//创建StringBuilder
                foreach (var info in m_WrapperInfos)//遍历所有包装信息
                {
                    if (!string.IsNullOrEmpty(info))//将所有的条目清空
                    {
                        sb.Append(info);//打包包装信息
                        sb.Append('\n');//用换行分割
                    }
                }
                EditorPrefs.SetString(kPrefWrappers, sb.ToString());//保存信息
            }
        }
        /// <summary>
        /// 复制目标条目
        /// </summary>
        /// <param name="userData"></param>
        public void StacktraceListView_Copy(object userData)
        {
            if (!StacktraceListView_IsExist())
            {
                return;
            }

            var stacktraceLineInfoIndex = (int)userData;//定义选中行
            if (stacktraceLineInfoIndex < m_SelectedInfo.stacktraceLineInfos.Count)
            {
                EditorGUIUtility.systemCopyBuffer = m_SelectedInfo.stacktraceLineInfos[stacktraceLineInfoIndex].plain;
            }
        }
        /// <summary>
        /// 复制所有
        /// </summary>
        public void StacktraceListView_CopyAll()
        {
            if (!StacktraceListView_IsExist() || !IsSelectedEntryShow())
            {
                return;
            }

            EditorGUIUtility.systemCopyBuffer = m_SelectedInfo.entry.condition;
        }
        #endregion
    }

}
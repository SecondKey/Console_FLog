using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ConsoleTiny
{
    class EntryInfo
    {
        public int row;
        public string lines;
        public string text;
        public string pure; // remove tag
        public string lower;//pure的小写格式
        public int entryCount;
        public int searchIndex;
        public int searchEndIndex;
        public ConsoleFlags flags;
        public LogEntry entry;
        public List<StacktraceLineInfo> stacktraceLineInfos;
        public List<int> tagPosInfos;
    }

    class StacktraceLineInfo
    {
        public string plain;
        public string text;
        public string wrapper;
        /// <summary>
        /// 文件地址
        /// </summary>
        public string filePath;
        /// <summary>
        /// 行号
        /// </summary>
        public int lineNum;
    }

    /// <summary>
    /// log入口包装
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
            Error = 1 << 0,
            Assert = 1 << 1,
            Log = 1 << 2,
            Fatal = 1 << 4,
            DontPreprocessCondition = 1 << 5,
            AssetImportError = 1 << 6,
            AssetImportWarning = 1 << 7,
            ScriptingError = 1 << 8,
            ScriptingWarning = 1 << 9,
            ScriptingLog = 1 << 10,
            ScriptCompileError = 1 << 11,
            ScriptCompileWarning = 1 << 12,
            StickyError = 1 << 13,
            MayIgnoreLineNumber = 1 << 14,
            ReportBug = 1 << 15,
            DisplayPreviousErrorInStatusBar = 1 << 16,
            ScriptingException = 1 << 17,
            DontExtractStacktrace = 1 << 18,
            ShouldClearOnPlay = 1 << 19,
            GraphCompileError = 1 << 20,
            ScriptingAssertion = 1 << 21,
            VisualScriptingError = 1 << 22
        };

        public int numberOfLines
        {
            private get { return m_NumberOfLines; }
            set { m_NumberOfLines = value; ResetEntriesForNumberLines(); }
        }

        public bool showTimestamp
        {
            get { return m_ShowTimestamp; }
            set
            {
                m_ShowTimestamp = value;
                EditorPrefs.SetBool(kPrefShowTimestamp, value);
                ResetEntriesForNumberLines();
            }
        }

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

        public string searchString
        {
            get { return m_SearchString; }
            set { m_SearchStringComing = value; }
        }

        public string[] searchHistory = new[] { "" };

        public bool searchFrame { get; set; }

        private int m_ConsoleFlags;
        private int m_ConsoleFlagsComing;
        private string m_SearchString;
        private string m_SearchStringComing;
        private double m_LastSearchStringTime;

        private bool m_Init;
        private int m_NumberOfLines;
        private bool m_ShowTimestamp;
        private bool m_Collapse;
        private int[] m_TypeCounts = new[] { 0, 0, 0 };
        private int m_LastEntryCount = -1;
        private EntryInfo m_SelectedInfo;
        /// <summary>
        /// 所有入口信息
        /// </summary>
        private readonly List<EntryInfo> m_EntryInfos = new List<EntryInfo>();
        /// <summary>
        /// 被过滤后的入口信息
        /// </summary>
        private readonly List<EntryInfo> m_FilteredInfos = new List<EntryInfo>();
        private readonly CustomFiltersGroup m_CustomFilters = new CustomFiltersGroup();
        private readonly List<string> m_WrapperInfos = new List<string>();

        private const string kPrefConsoleFlags = "ConsoleTiny_ConsoleFlags";
        private const string kPrefShowTimestamp = "ConsoleTiny_ShowTimestamp";
        private const string kPrefCollapse = "ConsoleTiny_Collapse";
        private const string kPrefCustomFilters = "ConsoleTiny_CustomFilters";
        private const string kPrefWrappers = "ConsoleTiny_Wrappers";

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
        /// <param name="consoleFlag">输出的类型</param>
        /// <param name="entryCount">入口号</param>
        /// <param name="searchIndex"></param>
        /// <param name="searchEndIndex"></param>
        /// <returns></returns>
        public string GetEntryLinesAndFlagAndCount(int row, ref int consoleFlag, ref int entryCount, ref int searchIndex, ref int searchEndIndex)
        {
            if (row < 0 || row >= m_FilteredInfos.Count)
            {
                return string.Empty;
            }

            EntryInfo entryInfo = m_FilteredInfos[row];
            consoleFlag = (int)entryInfo.flags;
            entryCount = entryInfo.entryCount;
            searchIndex = entryInfo.searchIndex;
            searchEndIndex = entryInfo.searchEndIndex;
            return entryInfo.text;
        }

        public void GetCountsByType(ref int errorCount, ref int warningCount, ref int logCount)
        {
            errorCount = m_TypeCounts[0];
            warningCount = m_TypeCounts[1];
            logCount = m_TypeCounts[2];
        }

        public int SetSelectedEntry(int row)
        {
            m_SelectedInfo = null;
            if (row < 0 || row >= m_FilteredInfos.Count)
            {
                return 0;
            }

            m_SelectedInfo = m_FilteredInfos[row];
            return m_SelectedInfo.entry.instanceID;
        }

        public bool IsEntrySelected(int row)
        {
            if (row < 0 || row >= m_FilteredInfos.Count)
            {
                return false;
            }

            return m_FilteredInfos[row] == m_SelectedInfo;
        }

        public bool IsSelectedEntryShow()
        {
            if (m_SelectedInfo != null)
            {
                return m_FilteredInfos.Contains(m_SelectedInfo);
            }
            return false;
        }

        public int GetSelectedEntryIndex()
        {
            if (m_SelectedInfo != null)
            {
                for (int i = 0; i < m_FilteredInfos.Count; i++)
                {
                    if (m_FilteredInfos[i] == m_SelectedInfo)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        public int GetFirstErrorEntryIndex()
        {
            for (int i = 0; i < m_FilteredInfos.Count; i++)
            {
                if (m_FilteredInfos[i].flags == ConsoleFlags.LogLevelError)
                {
                    return i;
                }
            }
            return -1;
        }

        public void UpdateEntries()
        {
            CheckInit();
            int flags = UnityEditor.LogEntries.consoleFlags;
            UnityEditor.LogEntries.SetConsoleFlag((int)ConsoleFlags.LogLevelLog, true);
            UnityEditor.LogEntries.SetConsoleFlag((int)ConsoleFlags.LogLevelWarning, true);
            UnityEditor.LogEntries.SetConsoleFlag((int)ConsoleFlags.LogLevelError, true);
            UnityEditor.LogEntries.SetConsoleFlag((int)ConsoleFlags.Collapse, collapse);
            int count = UnityEditor.LogEntries.GetCount();
            if (count == m_LastEntryCount)
            {
                UnityEditor.LogEntries.consoleFlags = flags;
                CheckRepaint(CheckSearchStringChanged());
                return;
            }

            if (m_LastEntryCount > count)
            {
                ClearEntries();
            }

            UnityEditor.LogEntries.SetConsoleFlag((int)ConsoleFlags.ShowTimestamp, true);
            UnityEditor.LogEntries.StartGettingEntries();
            for (int i = m_LastEntryCount; i < count; i++)
            {
                LogEntry entry = new LogEntry();
                if (!UnityEditor.LogEntries.GetEntryInternal(i, entry))
                {
                    continue;
                }

                int mode = 0;
                string text = null;
                UnityEditor.LogEntries.GetLinesAndModeFromEntryInternal(i, 10, ref mode, ref text);


                int entryCount = 0;
                if (collapse)
                {
                    entryCount = UnityEditor.LogEntries.GetEntryCount(i);
                }
                AddEntry(i, entry, text, entryCount);
            }
            UnityEditor.LogEntries.EndGettingEntries();
            UnityEditor.LogEntries.consoleFlags = flags;
            m_LastEntryCount = count;

            CheckSearchStringChanged();
            CheckRepaint(true);
        }

        /// <summary>
        /// 清空所有入口
        /// </summary>
        private void ClearEntries()
        {
            m_SelectedInfo = null;//清除入口选择信息
            m_EntryInfos.Clear();//清空所有入口
            m_FilteredInfos.Clear();//清空所有被过滤的入口
            m_LastEntryCount = -1;
            m_TypeCounts = new[] { 0, 0, 0 };
        }

        /// <summary>
        /// 添加一个入口
        /// </summary>
        /// <param name="row">行数</param>
        /// <param name="entry">入口</param>
        /// <param name="text">文本</param>
        /// <param name="entryCount">入口号</param>
        private void AddEntry(int row, LogEntry entry, string text, int entryCount)
        {
            EntryInfo entryInfo = new EntryInfo
            {
                row = row,
                lines = text,
                text = GetNumberLines(text),
                entryCount = entryCount,
                flags = GetConsoleFlagFromMode(entry.mode),
                entry = entry
            };
            entryInfo.pure = GetPureLines(entryInfo.text, out entryInfo.tagPosInfos);
            entryInfo.lower = entryInfo.pure.ToLower();//将  全部转换为小写
            m_EntryInfos.Add(entryInfo);//将入口信息添加到入口列表中

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
        /// 根据行刷新所有入口
        /// </summary>
        private void ResetEntriesForNumberLines()
        {
            foreach (var entryInfo in m_EntryInfos)//遍历所有入口
            {
                entryInfo.text = GetNumberLines(entryInfo.lines);//设置文本
                entryInfo.pure = GetPureLines(entryInfo.text, out entryInfo.tagPosInfos);
                entryInfo.lower = entryInfo.pure.ToLower();
            }
        }

        private void CheckInit()
        {
            if (m_Init)
            {
                return;
            }

            m_Init = true;
            m_ConsoleFlagsComing = EditorPrefs.GetInt(kPrefConsoleFlags, 896);
            m_ShowTimestamp = EditorPrefs.GetBool(kPrefShowTimestamp, false);
            m_Collapse = EditorPrefs.GetBool(kPrefCollapse, false);
            m_WrapperInfos.Clear();
            m_WrapperInfos.AddRange(EditorPrefs.GetString(kPrefWrappers, String.Empty).Split('\n'));
            m_CustomFilters.Load();
        }

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

        private void CheckRepaint(bool repaint)
        {

        }

        private bool HasMode(int mode, Mode modeToCheck) { return (mode & (int)modeToCheck) != 0; }

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

        private string GetNumberLines(string s)
        {
            int num = numberOfLines;
            int i = -1;
            for (int j = 1, k = 0; j <= num; j++)
            {
                i = s.IndexOf('\n', i + 1);
                if (i == -1)
                {
                    if (k < num)
                    {
                        i = s.Length;
                    }
                    break;
                }
                k++;
            }

            if (i != -1)
            {
                int startIndex = 0;
                if (!showTimestamp)
                {
                    startIndex = 11;
                }
                return s.Substring(startIndex, i - startIndex);
            }
            return s;
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

        private readonly StringBuilder m_StringBuilder = new StringBuilder();
        private readonly Stack<int> m_TagStack = new Stack<int>();


        /// <summary>
        /// 根据输入获取纯系列表
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
                entryInfo.tagPosInfos);
            entryInfo.searchIndex = GetOriginalCharIndex(entryInfo.searchIndex, entryInfo.tagPosInfos);
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


        public bool StacktraceListView_IsExist()
        {
            if (m_SelectedInfo == null || m_SelectedInfo.stacktraceLineInfos == null)
            {
                return false;
            }

            return true;
        }

        public int StacktraceListView_GetCount()
        {
            if (StacktraceListView_IsExist() && IsSelectedEntryShow())
            {
                return m_SelectedInfo.stacktraceLineInfos.Count;
            }

            return 0;
        }

        public string StacktraceListView_GetLine(int row)
        {
            if (StacktraceListView_IsExist())
            {
                return m_SelectedInfo.stacktraceLineInfos[row].text;
            }

            return String.Empty;
        }

        public float StacktraceListView_GetMaxWidth(GUIContent tempContent, GUIStyle tempStyle)
        {
            if (m_SelectedInfo == null || !IsSelectedEntryShow())
            {
                return 1f;
            }

            if (!StacktraceListView_IsExist())
            {
                StacktraceListView_Parse(m_SelectedInfo);
            }

            var maxLine = -1;
            var maxLineLen = -1;
            for (int i = 0; i < m_SelectedInfo.stacktraceLineInfos.Count; i++)
            {
                if (maxLineLen < m_SelectedInfo.stacktraceLineInfos[i].plain.Length)
                {
                    maxLineLen = m_SelectedInfo.stacktraceLineInfos[i].plain.Length;
                    maxLine = i;
                }
            }

            float maxWidth = 1f;
            if (maxLine != -1)
            {
                tempContent.text = m_SelectedInfo.stacktraceLineInfos[maxLine].plain;
                maxWidth = tempStyle.CalcSize(tempContent).x;
            }

            return maxWidth;
        }

        private void StacktraceListView_Parse(EntryInfo entryInfo)
        {
            var lines = entryInfo.entry.condition.Split(new char[] { '\n' }, StringSplitOptions.None);
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

        private bool StacktraceListView_Parse_CSharp(string line, StacktraceLineInfo info,
            string textBeforeFilePath, string fileInBuildSlave, Uri uriRoot)
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

        private bool StacktraceListView_Parse_Lua(string line, StacktraceLineInfo info,
            string luaCFunction, string luaMethodBefore, string luaFileExt, string luaAssetPath)
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

        public bool StacktraceListView_CanOpen(int stacktraceLineInfoIndex)
        {
            if (!StacktraceListView_IsExist())
            {
                return false;
            }

            if (stacktraceLineInfoIndex < m_SelectedInfo.stacktraceLineInfos.Count)
            {
                return !string.IsNullOrEmpty(m_SelectedInfo.stacktraceLineInfos[stacktraceLineInfoIndex].filePath);
            }
            return false;
        }

        public bool StacktraceListView_CanWrapper(int stacktraceLineInfoIndex)
        {
            if (!StacktraceListView_IsExist())
            {
                return false;
            }

            if (stacktraceLineInfoIndex < m_SelectedInfo.stacktraceLineInfos.Count)
            {
                return !string.IsNullOrEmpty(m_SelectedInfo.stacktraceLineInfos[stacktraceLineInfoIndex].wrapper);
            }
            return false;
        }

        public bool StacktraceListView_IsWrapper(int stacktraceLineInfoIndex)
        {
            if (!StacktraceListView_IsExist())
            {
                return false;
            }

            if (stacktraceLineInfoIndex < m_SelectedInfo.stacktraceLineInfos.Count)
            {
                return !string.IsNullOrEmpty(m_SelectedInfo.stacktraceLineInfos[stacktraceLineInfoIndex].wrapper) &&
                    m_WrapperInfos.Contains(m_SelectedInfo.stacktraceLineInfos[stacktraceLineInfoIndex].wrapper);
            }
            return false;
        }

        public void StacktraceListView_RowGotDoubleClicked()
        {
            if (!StacktraceListView_IsExist())
            {
                return;
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

        public void StacktraceListView_Open(object userData)
        {
            if (!StacktraceListView_IsExist())
            {
                return;
            }

            var stacktraceLineInfoIndex = (int)userData;
            if (stacktraceLineInfoIndex < m_SelectedInfo.stacktraceLineInfos.Count)
            {
                var filePath = m_SelectedInfo.stacktraceLineInfos[stacktraceLineInfoIndex].filePath;
                var lineNum = m_SelectedInfo.stacktraceLineInfos[stacktraceLineInfoIndex].lineNum;
                ScriptAssetOpener.OpenAsset(filePath, lineNum);
            }
        }

        public void StacktraceListView_Wrapper(object userData)
        {
            if (!StacktraceListView_IsExist())
            {
                return;
            }

            var stacktraceLineInfoIndex = (int)userData;
            if (stacktraceLineInfoIndex < m_SelectedInfo.stacktraceLineInfos.Count)
            {
                var wrapper = m_SelectedInfo.stacktraceLineInfos[stacktraceLineInfoIndex].wrapper;
                if (m_WrapperInfos.Contains(wrapper))
                {
                    m_WrapperInfos.Remove(wrapper);
                }
                else
                {
                    m_WrapperInfos.Add(wrapper);
                }
                StringBuilder sb = new StringBuilder();
                foreach (var info in m_WrapperInfos)
                {
                    if (!string.IsNullOrEmpty(info))
                    {
                        sb.Append(info);
                        sb.Append('\n');
                    }
                }
                EditorPrefs.SetString(kPrefWrappers, sb.ToString());
            }
        }

        public void StacktraceListView_Copy(object userData)
        {
            if (!StacktraceListView_IsExist())
            {
                return;
            }

            var stacktraceLineInfoIndex = (int)userData;
            if (stacktraceLineInfoIndex < m_SelectedInfo.stacktraceLineInfos.Count)
            {
                EditorGUIUtility.systemCopyBuffer = m_SelectedInfo.stacktraceLineInfos[stacktraceLineInfoIndex].plain;
            }
        }

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
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ConsoleTiny
{
    /// <summary>
    /// 跟踪堆栈入口信息
    /// </summary>
    class EntryInfo
    {
        public int row;
        public string lines;
        public string text;
        public string pure; // remove tag
        /// <summary>
        /// pure的小写格式
        /// </summary>
        public string lower;
        public int entryCount;
        public int searchIndex;
        public int searchEndIndex;
        public ConsoleFlags flags;
        public LogEntry entry;
        /// <summary>
        /// 所有的堆栈行信息
        /// </summary>
        public List<StacktraceLineInfo> stacktraceLineInfos;
        public List<int> tagPosInfos;
    }

    /// <summary>
    /// 堆栈跟踪行信息
    /// </summary>
    class StacktraceLineInfo
    {
        /// <summary>
        /// 用于复制的文本信息
        /// </summary>
        public string plain;
        /// <summary>
        /// 用于显示的文本信息
        /// </summary>
        public string text;
        /// <summary>
        /// 用于包装的文本
        /// </summary>
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
}
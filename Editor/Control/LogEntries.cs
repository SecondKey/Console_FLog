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

    /// <summary>
    /// 堆栈跟踪行信息
    /// </summary>
    class StacktraceLineInfo
    {
        public string plain;
        /// <summary>
        /// 文本
        /// </summary>
        public string text;
        /// <summary>
        /// 包装器
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
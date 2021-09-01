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
        /// <summary>
        /// 当前入口的行数
        /// </summary>
        public int row;
        /// <summary>
        /// 这里才是全部的文本
        /// </summary>
        public string lines;
        /// <summary>
        /// 这里是在信息列表中显示的文本
        /// </summary>
        public string text;
        /// <summary>
        /// 移除了tag后的文本
        /// </summary>
        public string pure; 
        /// <summary>
        /// pure的小写格式
        /// </summary>
        public string lower;
        /// <summary>
        /// 所包含的入数
        /// </summary>
        public int entryCount;
        /// <summary>
        /// 选择开始的index
        /// </summary>
        public int searchIndex;
        /// <summary>
        /// 选择结束的index
        /// </summary>
        public int searchEndIndex;
        /// <summary>
        /// ConsoleFlags
        /// </summary>
        public ConsoleFlags flags;
        /// <summary>
        /// 具体入口
        /// </summary>
        public LogEntry entry;
        /// <summary>
        /// 所有的堆栈行信息
        /// </summary>
        public List<StacktraceLineInfo> stacktraceLineInfos;
        /// <summary>
        /// 每个tag的位置
        /// </summary>
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
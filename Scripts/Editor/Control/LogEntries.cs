using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ConsoleTiny
{
    /// <summary>
    /// 跟踪堆栈条目信息
    /// </summary>
    internal class EntryInfo
    {
        /// <summary>
        /// 当前条目的行数
        /// </summary>
        public int row;
        /// <summary>
        /// log的时间
        /// </summary>
        public string timeText;

        #region Text
        /// <summary>
        /// 这里才是全部的文本
        /// </summary>
        public string lines;
        /// <summary>
        /// 用户输出的文本
        /// </summary>
        public string text;
        /// <summary>
        /// 移除了标签(HTML标签)后的文本
        /// </summary>
        public string pure;
        /// <summary>
        /// pure的小写格式
        /// </summary>
        public string lower;
        #endregion

        #region Log
        /// <summary>
        /// 输出大类型
        /// </summary>
        public ConsoleFlags flags;
        /// <summary>
        /// 具体输出类型
        /// </summary>
        public string logType;
        /// <summary>
        /// 文本的起始标签
        /// </summary>
        public string logGroup;
        /// <summary>
        /// 显示在信息列表中的行数
        /// </summary>
        public int numberLines;
        #endregion 



        /// <summary>
        /// 所有该类条目数（折叠时有效）
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
        /// 具体条目
        /// </summary>
        public LogEntry entry;
        /// <summary>
        /// 所有的堆栈行信息
        /// </summary>
        public List<StacktraceLineInfo> stacktraceLineInfos;
        /// <summary>
        /// 每个tag的位置
        /// </summary>
        public List<int> HTMLTagPosInfos;
    }

    /// <summary>
    /// 堆栈跟踪行信息
    /// </summary>
    public class StacktraceLineInfo
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
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ConsoleTiny
{
    /// <summary>
    /// 控制台输出模式，这里的标记和Unity控制台的标记时一致的
    /// </summary>
    [Flags]
    public enum ConsoleFlags
    {
        /// <summary>
        /// 折叠
        /// </summary>
        Collapse = 1 << 0,
        /// <summary>
        /// 开始时清空
        /// </summary>
        ClearOnPlay = 1 << 1,
        /// <summary>
        /// 报错暂停
        /// </summary>
        ErrorPause = 1 << 2,
        /// <summary>
        /// 详细模式
        /// </summary>
        Verbose = 1 << 3,
        /// <summary>
        /// 维护停止
        /// </summary>
        StopForAssert = 1 << 4,
        /// <summary>
        /// 报错停止
        /// </summary>
        StopForError = 1 << 5,
        /// <summary>
        /// 自动滚动
        /// </summary>
        Autoscroll = 1 << 6,
        LogLevelLog = 1 << 7,
        LogLevelWarning = 1 << 8,
        LogLevelError = 1 << 9,
        ShowTimestamp = 1 << 10,
        /// <summary>
        /// 在编译时清空
        /// </summary>
        ClearOnBuild = 1 << 11,
    };
}


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ConsoleTiny
{
    public static class CLog
    {
        #region TMP
        public static void Log(string logText)
        {
            Log(logText, "TMP");
        }

        public static void LogWarning(string logText)
        {
            LogWarning(logText, "TMP");
        }

        public static void LogError(string logText)
        {
            LogError(logText, "TMP");
        }
        #endregion


        #region Private
        static void Log(string logText, string logType)
        {
            Debug.Log("#!@" + logType + ";" + logText + "#!!");
        }

        static void LogWarning(string logText, string logType)
        {
            Debug.LogWarning("#!@" + logType + ";" + logText + "#!!");
        }

        static void LogError(string logText, string logType)
        {
            Debug.LogError("#!@" + logType + ";" + logText + "#!!");
        }
        #endregion 
    }
}
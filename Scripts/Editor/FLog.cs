using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ConsoleTiny
{
    public static class FLog
    {
        #region TMP
        public static void Log(string logText)
        {
            Log(logText, "TMP");
        }

        public static void LogMainParameter(string logText)
        {
            LogMainParameter(logText, "TMP");
        }
        public static void LogExamine(string logText)
        {
            LogExamine(logText, "TMP");
        }

        public static void LogWarning(string logText)
        {
            LogWarning(logText, "TMP");
        }

        public static void LogError(string logText)
        {
            LogError(logText, "TMP");
        }
        public static void LogAssert(string logText)
        {
            LogAssert(logText, "TMP");
        }
        public static void LogDisastrous(object logText)
        {
            LogDisastrous(logText.ToString(), "TMP");
        }
        #endregion


        #region Private
        static void Log(string logText, string logType)
        {
            Debug.Log("#!@" + logType + ":Log" + ";" + logText + "#!!");
        }
        static void LogMainParameter(string logText, string logType)
        {
            Debug.Log("#!@" + logType + ":MainParameter" + ";" + logText + "#!!");
        }
        static void LogExamine(string logText, string logType)
        {
            Debug.Log("#!@" + logType + ":Examine" + ";" + logText + "#!!");
        }

        static void LogWarning(string logText, string logType)
        {
            Debug.LogWarning("#!@" + logType + ":Warning" + ";" + logText + "#!!");
        }

        static void LogError(string logText, string logType)
        {
            Debug.LogError("#!@" + logType + ":Error" + ";" + logText + "#!!");
        }
        static void LogAssert(string logText, string logType)
        {
            Debug.LogError("#!@" + logType + ":Assert" + ";" + logText + "#!!");
        }
        static void LogDisastrous(string logText, string logType)
        {
            Debug.LogError("#!@" + logType + ":Disastrous" + ";" + logText + "#!!");
        }
        #endregion 
    }
}
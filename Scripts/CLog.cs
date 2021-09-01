using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ConsoleTiny
{
    public class CLog
    {
        public void Log(string logText, string logType = "")
        {
            Debug.Log("%#T" + logType + logText);
        }

        public void LogWarning(string logText, string logType = "")
        {
            Debug.LogWarning("%#T" + logType + logText);
        }

        public void LogError(string logText, string logType = "")
        {
            Debug.LogError("%#T" + logType + logText);
        }
    }
}
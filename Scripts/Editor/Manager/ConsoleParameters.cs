using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ConsoleTiny
{
    public static class ConsoleParameters
    {
        #region Const
        public const string kPrefConsoleFlags = "ConsoleTiny_ConsoleFlags";
        public const string kPrefShowTimestamp = "ConsoleTiny_ShowTimestamp";
        public const string kPrefCollapse = "ConsoleTiny_Collapse";
        public const string kPrefCustomFilters = "ConsoleTiny_CustomFilters";
        public const string kPrefWrappers = "ConsoleTiny_Wrappers";



        public const string ClearLabel = "Clear";
        public const string ClearOnPlayLabel = "Clear on Play";
        public const string ErrorPauseLabel = "Error Pause";
        public const string CollapseLabel = "Collapse";
        public const string StopForAssertLabel = "Stop for Assert";
        public const string StopForErrorLabel = "Stop for Error";
        public const string ClearOnBuildLabel = "Clear on Build";
        public const string FirstErrorLabel = "First Error";
        public const string CustomFiltersLabel = "Custom Filters";
        #endregion

        #region Static 
        #region Info
        public static string ConsoleTinyPath = Application.dataPath + "/Plugin/ConsoleTiny/";
        #endregion

        #region List
        public static int ListLineHeight { get { return ConsoleManager.Instence.NowLogStyle.ListLineHeight; } }
        #endregion

        #region BackGround

        #endregion 

        #region Icon
        public static int IconOffectX { get { return ConsoleManager.Instence.NowLogStyle.IconOffectX; } }
        public static int IconOffectY { get { return ConsoleManager.Instence.NowLogStyle.IconOffectY; } }
        public static int IconSizeX { get { return ConsoleManager.Instence.NowLogStyle.IconSizeX; } }
        public static int IconSizeY { get { return ConsoleManager.Instence.NowLogStyle.IconSizeY; } }
        #endregion 







        #region GUIStyle
        public static GUIStyle Box { get { return GetGUIStyle("Box"); } }
        public static GUIStyle MiniButton { get { return GetGUIStyle("MiniButton"); } }
        public static GUIStyle Toolbar { get { return GetGUIStyle("Toolbar"); } }
        public static GUIStyle LogStyle { get { return GetGUIStyle("LogStyle"); } }
        public static GUIStyle WarningStyle { get { return GetGUIStyle("WarningStyle"); } }
        public static GUIStyle ErrorStyle { get { return GetGUIStyle("ErrorStyle"); } }
        public static GUIStyle IconLogStyle { get { return GetGUIStyle("IconLogStyle"); } }
        public static GUIStyle IconWarningStyle { get { return GetGUIStyle("IconWarningStyle"); } }
        public static GUIStyle IconErrorStyle { get { return GetGUIStyle("IconErrorStyle"); } }
        public static GUIStyle EvenBackground { get { return GetGUIStyle("EvenBackground"); } }
        public static GUIStyle OddBackground { get { return GetGUIStyle("OddBackground"); } }
        public static GUIStyle MessageStyle {   get { return GetGUIStyle("MessageStyle"); }}
        public static GUIStyle StatusError { get { return GetGUIStyle("StatusError"); } }
        public static GUIStyle StatusWarn { get { return GetGUIStyle("StatusWarn"); } }
        public static GUIStyle StatusLog { get { return GetGUIStyle("StatusLog"); } }
        public static GUIStyle CountBadge { get { return GetGUIStyle("CountBadge"); } }
        public static GUIStyle LogSmallStyle { get { return GetGUIStyle("LogSmallStyle"); } }
        public static GUIStyle WarningSmallStyle { get { return GetGUIStyle("WarningSmallStyle"); } }
        public static GUIStyle ErrorSmallStyle { get { return GetGUIStyle("ErrorSmallStyle"); } }
        public static GUIStyle IconLogSmallStyle { get { return GetGUIStyle("IconLogSmallStyle"); } }
        public static GUIStyle IconWarningSmallStyle { get { return GetGUIStyle("IconWarningSmallStyle"); } }
        public static GUIStyle IconErrorSmallStyle { get { return GetGUIStyle("IconErrorSmallStyle"); } }

        static GUIStyle GetGUIStyle(string styleName)
        {
            return ConsoleManager.Instence.GetConsoleStyle(styleName);
        }
        #endregion



        #region Icon
        public static Texture2D iconInfoSmall;
        public static Texture2D iconWarnSmall;
        public static Texture2D iconErrorSmall;
        public static Texture2D iconFirstErrorSmall;
        public static Texture2D iconInfoMono;
        public static Texture2D iconWarnMono;
        public static Texture2D iconErrorMono;
        public static Texture2D iconFirstErrorMono;
        public static Texture2D iconCustomFiltersMono;

        public static Texture2D[] iconCustomFiltersSmalls;
        #endregion 
        #endregion

        #region Texture
        private static Dictionary<string, List<Texture2D>> ConsoleIconList;
        private static Dictionary<string, List<Texture2D>> DefaultConsoleIconList;

        public static Texture2D GetTexture2D(string logType)
        {
            if (ConsoleIconList != null && ConsoleIconList.ContainsKey(logType))
            {
                return ConsoleIconList[logType][UnityEngine.Random.Range(0, ConsoleIconList[logType].Count)];
            }
            if (DefaultConsoleIconList != null && DefaultConsoleIconList.ContainsKey(logType))
            {
                return DefaultConsoleIconList[logType][UnityEngine.Random.Range(0, ConsoleIconList[logType].Count)];

            }

            return null;
        }
        #endregion

    }
}
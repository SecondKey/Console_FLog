using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ConsoleTiny
{
    public class ConsoleParameters
    {
        #region StyleFile
        Dictionary<string, ValueTuple<ConsoleStyle, Dictionary<string, LogStyle>>> StyleList;

        public void LoadStyle()
        {
            StyleList = new Dictionary<string, (ConsoleStyle, Dictionary<string, LogStyle>)>();

            foreach (DirectoryInfo info in new DirectoryInfo("").GetDirectories())
            {

            }
        }

        public void RefreshStyle()
        {

        }
        #endregion

        static ConsoleStyle consoleStyle;
        static LogStyle logStyle;

        static ConsoleStyle defaultConsoleStyle;
        static LogStyle defaultLogStyle;

        public void LoadConfig()
        {
            GUIStyle msessageStyle = new GUIStyle("");
            msessageStyle.onNormal.textColor = msessageStyle.active.textColor;
            msessageStyle.padding.top = 0;
            msessageStyle.padding.bottom = 0;
            var selectedStyle = new GUIStyle("MeTransitionSelect");
            msessageStyle.onNormal.background = selectedStyle.normal.background;


            iconInfo = EditorGUIUtility.LoadIcon("console.infoicon");
            iconWarn = EditorGUIUtility.LoadIcon("console.warnicon");
            iconError = EditorGUIUtility.LoadIcon("console.erroricon");
            iconInfoSmall = EditorGUIUtility.LoadIcon("console.infoicon.sml");
            iconWarnSmall = EditorGUIUtility.LoadIcon("console.warnicon.sml");
            iconErrorSmall = EditorGUIUtility.LoadIcon("console.erroricon.sml");
            iconFirstErrorSmall = EditorGUIUtility.LoadIcon("sv_icon_dot14_sml");

            // TODO: Once we get the proper monochrome images put them here.
            /*iconInfoMono = EditorGUIUtility.LoadIcon("console.infoicon.mono");
            iconWarnMono = EditorGUIUtility.LoadIcon("console.warnicon.mono");
            iconErrorMono = EditorGUIUtility.LoadIcon("console.erroricon.mono");*/
            iconInfoMono = EditorGUIUtility.LoadIcon("console.infoicon.sml");
            iconWarnMono = EditorGUIUtility.LoadIcon("console.warnicon.inactive.sml");
            iconErrorMono = EditorGUIUtility.LoadIcon("console.erroricon.inactive.sml");
            iconFirstErrorMono = EditorGUIUtility.LoadIcon("sv_icon_dot8_sml");
            iconCustomFiltersMono = EditorGUIUtility.LoadIcon("sv_icon_dot0_sml");

            iconCustomFiltersSmalls = new Texture2D[7];
            for (int i = 0; i < 7; i++)
            {
                iconCustomFiltersSmalls[i] = EditorGUIUtility.LoadIcon("sv_icon_dot" + (i + 1) + "_sml");
            }





            bool isProSkin = EditorGUIUtility.isProSkin;
            colorNamespace = isProSkin ? "6A87A7" : "66677E";
            colorClass = isProSkin ? "1A7ECD" : "0072A0";
            colorMethod = isProSkin ? "0D9DDC" : "335B89";
            colorParameters = isProSkin ? "4F7F9F" : "4C5B72";
            colorPath = isProSkin ? "375E68" : "7F8B90";
            colorFilename = isProSkin ? "4A6E8A" : "6285A1";
            colorNamespaceAlpha = isProSkin ? "4E5B6A" : "87878F";
            colorClassAlpha = isProSkin ? "2A577B" : "628B9B";
            colorMethodAlpha = isProSkin ? "246581" : "748393";
            colorParametersAlpha = isProSkin ? "425766" : "7D838B";
            colorPathAlpha = isProSkin ? "375860" : "8E989D";
            colorFilenameAlpha = isProSkin ? "4A6E8A" : "6285A1";




            LogStyleLineCount = EditorPrefs.GetInt("ConsoleWindowLogLineCount", 2);//强制读取消息行数，因为如果控制台没有打开，则其行数始终为0
        }

        private Texture2D LoadTexture(string imgPath)
        {
            FileStream fileStream = new FileStream(imgPath, FileMode.Open, FileAccess.Read);
            byte[] imgBytes = new byte[fileStream.Length]; //创建文件长度的buffer
            fileStream.Read(imgBytes, 0, (int)fileStream.Length);
            fileStream.Close();
            fileStream.Dispose();
            fileStream = null;
            Image img = Image.FromStream(new MemoryStream(imgBytes));
            Texture2D texture = new Texture2D(img.Width, img.Height);
            texture.LoadImage(imgBytes);
            return texture;
        }


        #region static

        #region Const
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
        public static GUIStyle MessageStyle { get { return GetGUIStyle("MessageStyle"); } }
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

        private static GUIStyle GetGUIStyle(string styleName)
        {
            GUIStyle style = consoleStyle.GetGUIStyle(styleName);
            if (style == null)
            {
                style = defaultConsoleStyle.GetGUIStyle(styleName);
            }
            return style;
        }
        #endregion


        #region Color
        public static string colorNamespace, colorNamespaceAlpha;
        public static string colorClass, colorClassAlpha;
        public static string colorMethod, colorMethodAlpha;
        public static string colorParameters, colorParametersAlpha;
        public static string colorPath, colorPathAlpha;
        public static string colorFilename, colorFilenameAlpha;

        #endregion

        #region Icon
        static internal Texture2D iconInfo;
        static internal Texture2D iconWarn;
        static internal Texture2D iconError;
        static internal Texture2D iconInfoSmall;
        static internal Texture2D iconWarnSmall;
        static internal Texture2D iconErrorSmall;
        static internal Texture2D iconFirstErrorSmall;

        static internal Texture2D iconInfoMono;
        static internal Texture2D iconWarnMono;
        static internal Texture2D iconErrorMono;
        static internal Texture2D iconFirstErrorMono;
        static internal Texture2D iconCustomFiltersMono;

        static internal Texture2D[] iconCustomFiltersSmalls;
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

        #region 输出条目的行数
        private static int ms_logStyleLineCount;
        public static int LogStyleLineCount
        {
            get { return ms_logStyleLineCount; }
            set
            {
                ms_logStyleLineCount = value;
                EntryWrapped.Instence.numberOfLines = value;

                // If Constants hasn't been initialized yet we just skip this for now
                // If Constants hasn't been initialized yet we just skip this for now and let Init() call this for us in a bit.
                // if (!Instence.loadAlready)
                //    return;
                // UpdateLogStyleFixedHeights();
            }
        }
        #endregion 

        public void LoadSytle(string consoleStyleName, string logStyleName)
        {
            if (consoleStyleName == consoleStyle.StyleName && logStyleName == logStyle.StyleName)
                return;
            if (consoleStyleName != consoleStyle.StyleName)
            {
                consoleStyle = StyleList[consoleStyleName].Item1;
            }

            logStyle = StyleList[consoleStyleName].Item2[logStyleName];
        }

        private static void UpdateLogStyleFixedHeights()
        {
            // Whenever we change the line height count or the styles are set we need to update the fixed height of the following GuiStyles so the entries do not get cropped incorrectly.
            ErrorStyle.fixedHeight = (LogStyleLineCount * ErrorStyle.lineHeight) + ErrorStyle.border.top;
            WarningStyle.fixedHeight = (LogStyleLineCount * WarningStyle.lineHeight) + WarningStyle.border.top;
            LogStyle.fixedHeight = (LogStyleLineCount * LogStyle.lineHeight) + LogStyle.border.top;
        }


    }
    #endregion 
}
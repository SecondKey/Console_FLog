using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;
using static ConsoleTiny.ConsoleParameters;

namespace ConsoleTiny
{
    public class ConsoleManager
    {
        #region Instence
        private static ConsoleManager instence;
        private ConsoleManager() { LoadStyle(); LoadConfig(); }
        public static ConsoleManager Instence
        {
            get
            {
                if (instence == null)
                {
                    instence = new ConsoleManager();
                }
                if (!instence.styleLoadAlready)
                {
                    instence.LoadStyle();
                }
                if (!instence.optionLoadAlready)
                {
                    instence.LoadOptions();
                }
                return instence;
            }
        }
        #endregion

        #region Options
        bool optionLoadAlready;

        XDocument optionXml;

        public void LoadOptions()
        {
            optionLoadAlready = true;

            optionXml = XDocument.Load(ConsoleTingPath + "/Options/Option.xml");
            XElement root = optionXml.Root;

            ChangeLogStyle(root.Element("Style").Element("NowChoiseLogStyle").Value);

        }
        #endregion

        #region Style
        bool styleLoadAlready;

        Dictionary<string, ConsoleStyle> ConsoleStyleList;
        Dictionary<string, LogStyle> LogStyleList;

        ConsoleStyle consoleStyle;
        LogStyle logStyle;

        ConsoleStyle defaultConsoleStyle;
        LogStyle defaultLogStyle;

        public int LogStyleLineCount { get { return logStyle.LineCount; } }

        public void LoadStyle()
        {
            ConsoleStyleList = new Dictionary<string, ConsoleStyle>();
            LogStyleList = new Dictionary<string, LogStyle>();

            foreach (DirectoryInfo info in new DirectoryInfo(ConsoleTingPath + "Styles/ConsoleStyles").GetDirectories())
            {
                ConsoleStyle style = new ConsoleStyle(info.FullName);
                ConsoleStyleList.Add(style.StyleName, style);
            }
            foreach (DirectoryInfo info in new DirectoryInfo(ConsoleTingPath + "Styles/LogStyles").GetDirectories())
            {
                LogStyle style = new LogStyle(info.FullName);
                LogStyleList.Add(style.StyleName, style);
            }
            styleLoadAlready = true;
        }
        #endregion


        public void LoadConfig()
        {
            GUIStyle messageStyle = new GUIStyle("CN Message");
            messageStyle.onNormal.textColor = messageStyle.active.textColor;
            messageStyle.padding.top = 0;
            messageStyle.padding.bottom = 0;
            var selectedStyle = new GUIStyle("MeTransitionSelect");
            messageStyle.onNormal.background = selectedStyle.normal.background;


            iconInfo = EditorGUIUtility.LoadIcon("console.infoicon");
            iconWarn = EditorGUIUtility.LoadIcon("console.warnicon");
            iconError = EditorGUIUtility.LoadIcon("console.erroricon");
            iconInfoSmall = EditorGUIUtility.LoadIcon("console.infoicon.sml");
            iconWarnSmall = EditorGUIUtility.LoadIcon("console.warnicon.sml");
            iconErrorSmall = EditorGUIUtility.LoadIcon("console.erroricon.sml");
            iconFirstErrorSmall = EditorGUIUtility.LoadIcon("sv_icon_dot14_sml");

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

        }

        private void ChangeConsoleStyle(string consoleStyleName)
        {
            if (consoleStyle != null && consoleStyle.StyleName == consoleStyleName)
                return;
            if (ConsoleStyleList.ContainsKey(consoleStyleName))
            {
                consoleStyle = ConsoleStyleList[consoleStyleName];
            }
            else
            {
                Debug.Log("Console样式中没有指定的");
            }
        }

        public void ChangeLogStyle(string logStyleName)
        {
            if (logStyle != null && logStyle.StyleName == logStyleName)
                return;

            if (LogStyleList.ContainsKey(logStyleName))
            {
                logStyle = LogStyleList[logStyleName];
                ChangeConsoleStyle(logStyle.TargetConsoleStyleName);
                ConsoleWindow.RefreshWindow();
            }
            else
            {
                Debug.Log("Log样式中没有指定的");
            }
        }

        public GUIStyle GetConsoleStyle(string styleName)
        {
            Debug.Log(styleName);
            GUIStyle style = consoleStyle.GetGUIStyle(styleName);
            if (style == null)
            {
                style = defaultConsoleStyle.GetGUIStyle(styleName);
            }
            return style;
        }
    }
}

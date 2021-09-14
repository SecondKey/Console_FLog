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
        private ConsoleManager()
        {
            LoadStyle();
            LoadConfig();
        }


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
                if (instence.NowLogStyle.check == null)
                {
                    instence.NowLogStyle.LoadStyle();
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

        public ConsoleStyle NowConsoleStyle { get; private set; }
        public LogStyle NowLogStyle { get; private set; }

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

        private void ChangeConsoleStyle(string consoleStyleName)
        {
            if (NowConsoleStyle != null && NowConsoleStyle.StyleName == consoleStyleName)
                return;
            if (ConsoleStyleList.ContainsKey(consoleStyleName))
            {
                NowConsoleStyle = ConsoleStyleList[consoleStyleName];
            }
            else
            {
                Debug.Log("没有指定的Console样式");
            }
        }

        public void ChangeLogStyle(string logStyleName)
        {
            if (NowLogStyle != null && NowLogStyle.StyleName == logStyleName)
                return;

            if (LogStyleList.ContainsKey(logStyleName))
            {
                NowLogStyle = LogStyleList[logStyleName];
                ChangeConsoleStyle(NowLogStyle.TargetConsoleStyleName);
                ConsoleWindow.instence.UpdateListView();
            }
            else
            {
                Debug.Log("没有指定的Log样式");
            }
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



        #region Resources/Style
        public GUIStyle GetConsoleStyle(string styleName)
        {
            GUIStyle style = NowLogStyle.GetGUIStyle(styleName);
            if (style == null)
            {
                style = NowConsoleStyle.GetGUIStyle(styleName);
            }
            return style;
        }

        public Texture GetTexture(string logType)
        {
            return null;
        }

        //public GUIStyle GetItemBackgroundStyle(ConsoleFlags logType, bool userLog, int num)
        internal GUIStyle GetItemBackgroundStyle(EntryInfo info, int num)
        {
            if (!string.IsNullOrEmpty(info.logType) && NowLogStyle.LogItemStyleCollection.ContainsKey(info.logType))
            {
                int count = NowLogStyle.LogItemStyleCollection[info.logType].BackgroundStyleList.Count;
                if (count > 0)
                {
                    return NowLogStyle.LogItemStyleCollection[info.logType].BackgroundStyleList[num % count];
                }
            }
            GUIStyle style = num % 2 == 0 ? OddBackground : EvenBackground;
            return style;
        }

        internal GUIContent GetItemIconContent(EntryInfo info)
        {
            if (!string.IsNullOrEmpty(info.logType) && NowLogStyle.LogItemStyleCollection.ContainsKey(info.logType))
            {
                List<GUIContent> contentList = NowLogStyle.LogItemStyleCollection[info.logType].IconContentList;
                if (contentList.Count > 0)
                {
                    return contentList[Random.Range(0, contentList.Count)];
                }
            }
            return new GUIContent();
        }

        internal GUIStyle GetItemTextStyle(EntryInfo info)
        {
            if (!string.IsNullOrEmpty(info.logType) && NowLogStyle.LogItemStyleCollection.ContainsKey(info.logType))
            {
                return NowLogStyle.LogItemStyleCollection[info.logType].TextStyle;
            }
            return "CN EntryInfo";
        }


        //public string GetItemText(string text, string logGroup, ConsoleFlags logType)
        internal string GetItemText(EntryInfo info)
        {
            string t = "";
            if (EntryWrapped.Instence.showTimestamp)
            {
                t += info.timeText + "  ";
            }
            if (!string.IsNullOrEmpty(info.logGroup) && NowLogStyle.LogGroupCollection.ContainsKey(info.logGroup))
            {
                t += NowLogStyle.LogGroupCollection[info.logGroup].Text;
            }
            if (!string.IsNullOrEmpty(info.logType) && NowLogStyle.LogItemStyleCollection.ContainsKey(info.logType))
            {
                t += NowLogStyle.LogItemStyleCollection[info.logType].Text;
            }
            if (t != "")
            {
                t = "<size=20>" + t + "</size>";
                t += "\n";
            }
            string[] lineList = info.text.Split('\n');
            string LogText = "";
            if (lineList.Length > NowLogStyle.MaxLine)
            {
                for (int i = 0; i < NowLogStyle.MaxLine; i++)
                {
                    LogText += lineList[i] + "\n";
                }
            }
            else
            {
                LogText = info.text;
            }
            t += LogText;
            return t;
        }

        public Rect GetTextRect(Rect origionRect)
        {
            return new Rect(
                origionRect.x + NowLogStyle.TextOffectX,
                origionRect.y + NowLogStyle.TextOffectY,
                origionRect.width - NowLogStyle.TextOffectX,
                origionRect.height - NowLogStyle.TextOffectY);
        }
        #endregion
    }
}

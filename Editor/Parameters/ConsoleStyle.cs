using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml.Linq;

namespace ConsoleTiny
{
    public class ConsoleStyle
    {
        public string StyleName { get; }

        #region GUIStyel
        private Dictionary<string, GUIStyle> GUIStyleList;
        #endregion

        #region logLineCount
        private int logLineCount;
        //public static int LogStyleLineCount
        //{
        //    get { return 20; }
        //    set
        //    {
        //        ms_logStyleLineCount = value;
        //        LogEntries.wrapped.numberOfLines = value;

        //        // If Constants hasn't been initialized yet we just skip this for now
        //        // and let Init() call this for us in a bit.
        //        if (!Instence.loadAlready)
        //            return;
        //        UpdateLogStyleFixedHeights();
        //    }
        //}
        //private static void UpdateLogStyleFixedHeights()
        //{
        // Whenever we change the line height count or the styles are set we need to update the fixed height
        // of the following GuiStyles so the entries do not get cropped incorrectly.
        //ErrorStyle.fixedHeight = (LogStyleLineCount * ErrorStyle.lineHeight) + ErrorStyle.border.top;
        //WarningStyle.fixedHeight = (LogStyleLineCount * WarningStyle.lineHeight) + WarningStyle.border.top;
        //LogStyle.fixedHeight = (LogStyleLineCount * LogStyle.lineHeight) + LogStyle.border.top;
        //}
        #endregion
        public ConsoleStyle(string styleName, string stylePath)
        {
            StyleName = styleName;

            XElement root = XDocument.Load(stylePath).Root;

            foreach (XElement element in root.Element("GUIStyle").Elements())
            {
                LoadGUIStyleFromXml(element);
            }
        }

        public void LoadGUIStyleFromXml(XElement styleElement)
        {
            GUIStyle style = styleElement.Value.RemoveTabs();
            foreach (XElement styleParameters in styleElement.Elements())
            {
                switch (styleParameters.Name.ToString())
                {
                    case "OnNormal":

                        break;
                    case "Padding":
                        break;
                }
            }

            GUIStyleList.Add(styleElement.Name.ToString(), style);
        }


        #region ParsingStyleXml
        public void OnNormal(XElement element, GUIStyle style)
        {
            foreach (XElement e in element.Elements())
            {
                switch (e.Name.ToString())
                {
                    case "textColor":
                        style.onNormal.textColor = e.Value.RemoveTabs().GetColor();
                        break;
                }
            }


        }

        public void Paddin(XElement element, GUIStyle style)
        {
            style.padding = new RectOffset();
            style.padding = element.Value.RemoveTabs().GetRectOffset();
        }

        #endregion

        public GUIStyle GetGUIStyle(string styleName)
        {
            if (GUIStyleList.ContainsKey(styleName))
            {
                return GUIStyleList[styleName];
            }
            return null;
        }
    }
}
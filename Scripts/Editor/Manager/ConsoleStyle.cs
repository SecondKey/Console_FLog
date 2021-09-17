using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml.Linq;

namespace ConsoleTiny
{
    public class ConsoleStyle
    {
        public string StyleName { get; }
        public string DefaultLogStyleName { get; }

        #region GUIStyel
        private Dictionary<string, GUIStyle> GUIStyleList = new Dictionary<string, GUIStyle>();
        #endregion

        #region StackTraceIgnore
        public List<string> FileIgnoreCollection;
        public List<string> NameSpackIgnoreCollection;
        #endregion

        #region Log
        public List<string> LogTypeCollection;
        public List<string> LogGroupCollection;
        #endregion 

        public ConsoleStyle(string stylePath)
        {
            XElement root = XDocument.Load(stylePath + "/Style.xml").Root;

            XElement infoElement = root.Element("Info");
            StyleName = infoElement.Element("StyleName").Value.ToString();
            DefaultLogStyleName = infoElement.Element("DefaultLogStyle").Value.ToString();

            GUIStyleList.Clear();
            foreach (XElement element in root.Element("GUIStyle").Elements())
            {
                LoadGUIStyleFromXml(element);
            }

            FileIgnoreCollection = new List<string>();
            foreach (XElement element in root.Element("StackTraceIgnore").Element("File").Elements())
            {
                FileIgnoreCollection.Add(element.Value);
            }
            NameSpackIgnoreCollection = new List<string>();
            foreach (XElement element in root.Element("StackTraceIgnore").Element("NameSpace").Elements())
            {
                NameSpackIgnoreCollection.Add(element.Value);
            }


            LogTypeCollection = new List<string>();
            foreach (XElement element in root.Element("LogType").Elements())
            {
                LogTypeCollection.Add(element.Value);
            }
            LogGroupCollection = new List<string>();
            foreach (XElement element in root.Element("LogGroup").Elements())
            {
                LogGroupCollection.Add(element.Value);
            }

        }

        public void LoadGUIStyleFromXml(XElement styleElement)
        {
            string styleName = styleElement.Element("Name") != null ? styleElement.Element("Name").Value.ToString() : styleElement.Value;
            GUIStyle style = styleName;
            foreach (XElement styleParameters in styleElement.Elements())
            {
                switch (styleParameters.Name.ToString())
                {
                    case "normal":
                        foreach (XElement OnNormalParameter in styleParameters.Elements())
                        {
                            switch (OnNormalParameter.Name.ToString())
                            {
                                case "textColor":
                                    style.normal.textColor = OnNormalParameter.Value.GetColor();
                                    break;
                            }
                        }
                        break;
                    case "onNormal":
                        foreach (XElement OnNormalParameter in styleParameters.Elements())
                        {
                            switch (OnNormalParameter.Name.ToString())
                            {
                                case "textColor":
                                    style.onNormal.textColor = OnNormalParameter.Value.GetColor();
                                    break;
                            }
                        }
                        break;
                    case "FongSize":
                        style.fontSize = int.Parse(styleParameters.Value.ToString());
                        style.stretchHeight = true;
                        break;
                    case "Padding":
                        style.padding = styleParameters.Value.GetRectOffset();
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
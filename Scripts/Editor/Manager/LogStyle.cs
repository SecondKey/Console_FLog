using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml.Linq;
using System.IO;
using System.Drawing;

namespace ConsoleTiny
{
    public struct LogItemStyle
    {
        public List<GUIStyle> BackgroundStyleList;
        public List<GUIContent> IconContentList;
        public GUIStyle TextStyle;
        public string Text;
    }

    public struct LogGroup
    {
        public string Text;
    }

    public class LogStyle
    {
        public Texture2D check;
        string stylePath;

        #region Info
        public string StyleName { get; private set; }
        public string TargetConsoleStyleName { get; private set; }

        #region List
        public int ListLineHeight { get; private set; }
        #endregion

        #region TextPositionOffect
        public int TextOffectX { get; private set; }
        public int TextOffectY { get; private set; }

        public int FontSize { get; private set; }
        #endregion 

        #region Icon
        public int IconOffectX { get; private set; }
        public int IconOffectY { get; private set; }

        public int IconSizeX { get; private set; }
        public int IconSizeY { get; private set; }
        #endregion 
        #endregion

        #region Texture
        public Dictionary<string, Texture2D> BackGroundTextureCollection;
        public Dictionary<string, Texture2D> IconTextureCollection;
        #endregion

        public Dictionary<string, LogItemStyle> LogItemStyleCollection;
        public Dictionary<string, LogGroup> LogGroupCollection;

        public void LoadStyle()
        {
            check = new Texture2D(0,0);
            XElement root = XDocument.Load(stylePath + "/Style.xml").Root;

            #region Info
            XElement infoElement = root.Element("Info");
            StyleName = infoElement.Element("StyleName").Value.ToString();
            TargetConsoleStyleName = infoElement.Element("TargetConsoleStyle").Value.ToString();

            XElement ListElement = infoElement.Element("List");
            ListLineHeight = int.Parse(ListElement.Element("ListLineHeight").Value.ToString());


            XElement IconElement = infoElement.Element("Icon");
            IconSizeX = int.Parse(IconElement.Element("IconSizeX").Value.ToString());
            IconSizeY = int.Parse(IconElement.Element("IconSizeY").Value.ToString());
            IconOffectX = int.Parse(IconElement.Element("IconOffectX").Value.ToString());
            IconOffectY = int.Parse(IconElement.Element("IconOffectY").Value.ToString());



            XElement TextElement = infoElement.Element("Text");
            TextOffectX = int.Parse(TextElement.Element("TextOffectX").Value.ToString());
            TextOffectY = int.Parse(TextElement.Element("TextOffectY").Value.ToString());

            FontSize = int.Parse(TextElement.Element("FontSize").Value.ToString());
            #endregion

            #region Texture
            BackGroundTextureCollection = new Dictionary<string, Texture2D>();
            foreach (XElement texture in root.Element("Image").Element("BackGround").Elements())
            {
                BackGroundTextureCollection.Add(texture.Name.ToString(), LoadImageToTexture(stylePath + $"/{texture.Value}"));
            }

            IconTextureCollection = new Dictionary<string, Texture2D>();
            foreach (XElement image in root.Element("Image").Element("Icon").Elements())
            {
                IconTextureCollection.Add(image.Name.ToString(), LoadImageToTexture(stylePath + $"/{image.Value}"));

            }
            #endregion



            #region LogItemStyle
            LogItemStyleCollection = new Dictionary<string, LogItemStyle>();
            foreach (XElement ItemStyle in root.Element("ItemStyle").Elements())
            {
                LogItemStyle styleStruct = new LogItemStyle();

                #region BackGround
                styleStruct.BackgroundStyleList = new List<GUIStyle>();
                if (ItemStyle.Element("BackGroundImage") != null)
                {

                }
                else
                {
                    foreach (XElement backGroundItemStyle in ItemStyle.Element("BackGroundColor").Elements())
                    {
                        GUIStyle style = new GUIStyle();
                        Texture2D texture = new Texture2D(1, 1);
                        texture.SetPixel(1, 1, backGroundItemStyle.Value.GetColor());
                        texture.Apply();
                        style.normal.background = texture;
                        styleStruct.BackgroundStyleList.Add(style);
                    }
                }
                #endregion

                #region Icon
                styleStruct.IconContentList = new List<GUIContent>();
                foreach (XElement iconTexture in ItemStyle.Element("Icon").Elements())
                {
                    if (IconTextureCollection.ContainsKey(iconTexture.Value))
                    {
                        GUIContent content = new GUIContent(IconTextureCollection[iconTexture.Value]);
                        styleStruct.IconContentList.Add(content);
                    }
                }
                #endregion

                #region Text
                if (ItemStyle.Element("TextStyle") != null)
                {
                    styleStruct.TextStyle = new GUIStyle(ItemStyle.Element("Style").Value);
                }
                else
                {
                    styleStruct.TextStyle = new GUIStyle("CN EntryInfo");
                }

                if (ItemStyle.Element("TextColor") != null)
                {
                    styleStruct.TextStyle.normal.textColor = ItemStyle.Element("TextColor").Value.GetColor();
                }
                else
                {
                    styleStruct.TextStyle.normal.textColor = new UnityEngine.Color(0, 0, 0, 1);
                }

                styleStruct.TextStyle.fixedHeight = ListLineHeight - 10;
                styleStruct.TextStyle.fontSize = FontSize;

                if (ItemStyle.Element("Text") != null)
                {
                    styleStruct.Text = ItemStyle.Element("Text").Value;
                }
                #endregion
                LogItemStyleCollection.Add(ItemStyle.Name.ToString(), styleStruct);
            }


            #endregion

            #region Text

            LogGroupCollection = new Dictionary<string, LogGroup>();
            foreach (XElement logGroup in root.Element("LogGroup").Elements())
            {
                LogGroupCollection.Add(logGroup.Name.ToString(), new LogGroup() { Text = logGroup.Element("Text").Value });
            }
            #endregion

        }


        public LogStyle(string stylePath)
        {
            this.stylePath = stylePath;
            LoadStyle();


            XElement root = XDocument.Load(stylePath + "/Style.xml").Root;

            #region Info
            XElement infoElement = root.Element("Info");
            StyleName = infoElement.Element("StyleName").Value.ToString();
            TargetConsoleStyleName = infoElement.Element("TargetConsoleStyle").Value.ToString();

            XElement ListElement = infoElement.Element("List");
            ListLineHeight = int.Parse(ListElement.Element("ListLineHeight").Value.ToString());


            XElement IconElement = infoElement.Element("Icon");
            IconSizeX = int.Parse(IconElement.Element("IconSizeX").Value.ToString());
            IconSizeY = int.Parse(IconElement.Element("IconSizeY").Value.ToString());
            IconOffectX = int.Parse(IconElement.Element("IconOffectX").Value.ToString());
            IconOffectY = int.Parse(IconElement.Element("IconOffectY").Value.ToString());



            XElement TextElement = infoElement.Element("Text");
            TextOffectX = int.Parse(TextElement.Element("TextOffectX").Value.ToString());
            TextOffectY = int.Parse(TextElement.Element("TextOffectY").Value.ToString());

            FontSize = int.Parse(TextElement.Element("FontSize").Value.ToString());
            #endregion

            #region Texture
            BackGroundTextureCollection = new Dictionary<string, Texture2D>();
            foreach (XElement texture in root.Element("Image").Element("BackGround").Elements())
            {
                BackGroundTextureCollection.Add(texture.Name.ToString(), LoadImageToTexture(stylePath + $"/{texture.Value}"));
            }

            IconTextureCollection = new Dictionary<string, Texture2D>();
            foreach (XElement image in root.Element("Image").Element("Icon").Elements())
            {
                IconTextureCollection.Add(image.Name.ToString(), LoadImageToTexture(stylePath + $"/{image.Value}"));

            }
            #endregion 



            #region LogItemStyle
            LogItemStyleCollection = new Dictionary<string, LogItemStyle>();
            foreach (XElement ItemStyle in root.Element("ItemStyle").Elements())
            {
                LogItemStyle styleStruct = new LogItemStyle();

                #region BackGround
                styleStruct.BackgroundStyleList = new List<GUIStyle>();
                if (ItemStyle.Element("BackGroundImage") != null)
                {

                }
                else
                {
                    foreach (XElement backGroundItemStyle in ItemStyle.Element("BackGroundColor").Elements())
                    {
                        GUIStyle style = new GUIStyle();
                        Texture2D texture = new Texture2D(1, 1);
                        texture.SetPixel(1, 1, backGroundItemStyle.Value.GetColor());
                        texture.Apply();
                        style.normal.background = texture;
                        styleStruct.BackgroundStyleList.Add(style);
                    }
                }
                #endregion

                #region Icon
                styleStruct.IconContentList = new List<GUIContent>();
                foreach (XElement iconTexture in ItemStyle.Element("Icon").Elements())
                {
                    if (IconTextureCollection.ContainsKey(iconTexture.Value))
                    {
                        GUIContent content = new GUIContent(IconTextureCollection[iconTexture.Value]);
                        styleStruct.IconContentList.Add(content);
                    }
                }
                #endregion

                #region Text
                if (ItemStyle.Element("TextStyle") != null)
                {
                    styleStruct.TextStyle = new GUIStyle(ItemStyle.Element("Style").Value);
                }
                else
                {
                    styleStruct.TextStyle = new GUIStyle("CN EntryInfo");
                }

                if (ItemStyle.Element("TextColor") != null)
                {
                    styleStruct.TextStyle.normal.textColor = ItemStyle.Element("TextColor").Value.GetColor();
                }
                else
                {
                    styleStruct.TextStyle.normal.textColor = new UnityEngine.Color(0, 0, 0, 1);
                }

                styleStruct.TextStyle.fixedHeight = ListLineHeight - 10;
                styleStruct.TextStyle.fontSize = FontSize;

                if (ItemStyle.Element("Text") != null)
                {
                    styleStruct.Text = ItemStyle.Element("Text").Value;
                }
                #endregion
                LogItemStyleCollection.Add(ItemStyle.Name.ToString(), styleStruct);
            }


            #endregion

            #region Text

            LogGroupCollection = new Dictionary<string, LogGroup>();
            foreach (XElement logGroup in root.Element("LogGroup").Elements())
            {
                LogGroupCollection.Add(logGroup.Name.ToString(), new LogGroup() { Text = logGroup.Element("Text").Value });
            }
            #endregion 
        }

        public Texture2D LoadImageToTexture(string path)
        {
            FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            int byteLength = (int)fs.Length;
            byte[] imgBytes = new byte[byteLength];
            fs.Read(imgBytes, 0, byteLength);
            fs.Close();
            fs.Dispose();
            Image img = Image.FromStream(new MemoryStream(imgBytes));
            Texture2D t2d = new Texture2D(img.Width, img.Height);
            img.Dispose();
            t2d.LoadImage(imgBytes);
            t2d.Apply();
            return t2d;
        }


        public GUIStyle GetGUIStyle(string styleName)
        {
            return null;
        }
    }
}
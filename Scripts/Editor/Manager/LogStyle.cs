﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml.Linq;
using System.IO;
using System.Drawing;

namespace ConsoleTiny
{
    public struct LogItemTextStyle
    {
        public GUIStyle style;
        public string Text;

    }

    public class LogStyle
    {
        #region Info
        public string StyleName { get; }
        public string TargetConsoleStyleName { get; }
        #endregion

        #region Texture
        public Dictionary<string, Texture2D> BackGroundTextureCollection;
        public Dictionary<string, Dictionary<string, Texture2D>> IconTextureCollection;
        #endregion

        #region List
        public int ListLineHeight { get; }
        #endregion

        #region BackGround
        public Dictionary<string, GUIStyle> BackGroundStyleCollection;
        #endregion

        #region Icon
        public int IconSizeX { get; }
        public int IconSizeY { get; }
        #endregion 

        #region Text
        public Dictionary<string, LogItemTextStyle> LogItemTextStyleCollection;
        public Dictionary<string, string> LogGroupCollection;
        #endregion 



        public LogStyle(string stylePath)
        {
            XElement root = XDocument.Load(stylePath + "/Style.xml").Root;

            #region Info
            XElement infoElement = root.Element("Info");
            StyleName = infoElement.Element("StyleName").Value.ToString();
            TargetConsoleStyleName = infoElement.Element("TargetConsoleStyle").Value.ToString();
            #endregion

            #region Texture
            BackGroundTextureCollection = new Dictionary<string, Texture2D>();
            foreach (XElement texture in root.Element("Image").Element("BackGround").Elements())
            {
                BackGroundTextureCollection.Add(texture.Name.ToString(), LoadImageToTexture(stylePath + $"/{texture.Value}.png"));
            }

            IconTextureCollection = new Dictionary<string, Dictionary<string, Texture2D>>();
            foreach (XElement textureGroup in root.Element("Image").Element("Icon").Elements())
            {
                Dictionary<string, Texture2D> tmpList = new Dictionary<string, Texture2D>();
                foreach (XElement texture in textureGroup.Elements())
                {
                    tmpList.Add(texture.Name.ToString(), LoadImageToTexture(stylePath + $"/{texture.Value}.png"));
                }

                IconTextureCollection.Add(textureGroup.Name.ToString(), tmpList);
            }
            #endregion 



            #region List
            XElement ListElement = root.Element("List");
            ListLineHeight = int.Parse(ListElement.Element("ListLineHeight").Value.ToString());
            #endregion

            #region BackGround
            BackGroundStyleCollection = new Dictionary<string, GUIStyle>();
            foreach (XElement backGroundStyle in root.Element("BackGround").Elements())
            {
                GUIStyle style = new GUIStyle();
                if (backGroundStyle.Element("Color") != null)
                {
                    Texture2D texture = new Texture2D(1, 1);
                    texture.SetPixel(1, 1, backGroundStyle.Element("Color").Value.GetColor());
                    texture.Apply();
                    style.normal.background = texture;
                }
                if (backGroundStyle.Element("Image") != null)
                {
                    string imageName = backGroundStyle.Element("Image").Value;
                    if (BackGroundTextureCollection.ContainsKey(imageName))
                    {
                        style.normal.background = BackGroundTextureCollection[imageName];
                    }
                }
                BackGroundStyleCollection.Add(backGroundStyle.Name.ToString(), style);
            }
            #endregion

            #region Icon
            XElement IconElement = root.Element("Icon");
            IconSizeX = int.Parse(IconElement.Element("IconSizeX").Value.ToString());
            IconSizeY = int.Parse(IconElement.Element("IconSizeY").Value.ToString());
            #endregion

            #region Text
            LogItemTextStyleCollection = new Dictionary<string, LogItemTextStyle>();
            foreach (XElement textStyle in root.Element("Text").Elements())
            {
                LogItemTextStyle TextStyle = new LogItemTextStyle();
                if (textStyle.Element("Style") != null)
                {
                    TextStyle.style = new GUIStyle(textStyle.Element("Style").Value);
                }
                else
                {
                    TextStyle.style = new GUIStyle("CN EntryInfo");
                }
                if (textStyle.Element("Color") != null)
                {
                    TextStyle.style.normal.textColor = textStyle.Element("Color").Value.GetColor();
                }

                if (textStyle.Element("Text") != null)
                {
                    TextStyle.Text = textStyle.Element("Text").Value;
                }
                LogItemTextStyleCollection.Add(textStyle.Name.ToString(), TextStyle);
            }

            LogGroupCollection = new Dictionary<string, string>();
            foreach (XElement logGroup in root.Element("LogGroup").Elements())
            {
                LogGroupCollection.Add(logGroup.Name.ToString(), logGroup.Value);
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
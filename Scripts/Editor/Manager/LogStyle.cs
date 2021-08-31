using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml.Linq;
using System.IO;
using System.Drawing;

namespace ConsoleTiny
{
    public class LogStyle
    {
        #region Info
        public string StyleName { get; }
        public string TargetConsoleStyleName { get; }
        #endregion

        #region List
        public int ListLineHeight { get; }
        #endregion

        #region Icon
        public int IconSizeX { get; }
        public int IconSizeY { get; }


        #endregion 


        #region Texture
        Dictionary<string, Dictionary<string, Texture2D>> TextureList;
        #endregion

        public LogStyle(string stylePath)
        {
            XElement root = XDocument.Load(stylePath + "/Style.xml").Root;

            XElement infoElement = root.Element("Info");
            StyleName = infoElement.Element("StyleName").Value.ToString();
            TargetConsoleStyleName = infoElement.Element("TargetConsoleStyle").Value.ToString();

            XElement ListElement = root.Element("List");
            ListLineHeight = int.Parse(ListElement.Element("ListLineHeight").Value.ToString());

            XElement IconElement = root.Element("Icon");
            IconSizeX = int.Parse(IconElement.Element("IconSizeX").Value.ToString());
            IconSizeY = int.Parse(IconElement.Element("IconSizeY").Value.ToString());


            TextureList = new Dictionary<string, Dictionary<string, Texture2D>>();
            foreach (XElement textureGroup in root.Element("Resources").Element("Images").Elements())
            {
                Dictionary<string, Texture2D> tmpList = new Dictionary<string, Texture2D>();
                foreach (XElement texture in textureGroup.Elements())
                {
                    tmpList.Add(texture.Name.ToString(), LoadImageToTexture(stylePath + $"/{texture.Value}.png"));
                }

                TextureList.Add(textureGroup.Name.ToString(), tmpList);
            }
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
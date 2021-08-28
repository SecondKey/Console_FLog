using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml.Linq;
using System.IO;
using System.Drawing;

namespace ConsoleTiny
{
    public class LogStyle : MonoBehaviour
    {
        public string StyleName { get; }
        public string TargetStyleName { get; }

        public List<string> AvailableConsoelStyleList { get; }

        #region Texture
        Dictionary<string, Dictionary<string, Texture2D>> TextureList;
        #endregion

        public LogStyle(string stylePath)
        {
            XElement root = XDocument.Load(stylePath + "/Style.xml").Root;
            StyleName = root.Element("StyleName").Value.ToString();

            TextureList = new Dictionary<string, Dictionary<string, Texture2D>>();
            foreach (XElement textureGroup in root.Element("Images").Elements())
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
    }
}
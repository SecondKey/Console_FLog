using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml.Linq;

namespace ConsoleTiny
{
    public class LogStyle : MonoBehaviour
    {
        public string StyleName { get; }
        public List<string> AvailableConsoelStyleList { get; }

        #region Texture
        Dictionary<string, Texture2D> TextureList;

        #endregion

        public LogStyle(string styleName, string styleType)
        {
            StyleName = styleName;
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ConsoleTiny
{
    public static class Tools
    {
        public static string RemoveTabs(this string s)
        {
            return s.Replace("\n", "");
        }

        public static Color GetColor(this string s)
        {
            string[] colorList = s.Split(',');
            return new Color(
                float.Parse(colorList[0]),
                float.Parse(colorList[1]),
                float.Parse(colorList[2]),
                float.Parse(colorList[3]));
        }

        public static RectOffset GetRectOffset(this string s)
        {
            string[] offsetList = s.Split(',');

            RectOffset offest = new RectOffset(
                int.Parse(offsetList[0]),
                int.Parse(offsetList[1]),
                int.Parse(offsetList[2]),
                int.Parse(offsetList[3]));
            return offest;
        }
    }
}
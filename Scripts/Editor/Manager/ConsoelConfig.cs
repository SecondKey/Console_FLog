using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml.Linq;


namespace ConsoleTiny
{
    public class ConsoelConfig
    {
        XDocument config;
        string fileName;

        public string ConfigName;

        public bool UseSingleType = false;
        public string SingleType;

        public bool UseSingleGroup = false;
        public string SingleGroup;

        public Dictionary<string, bool> LogTypeActiveCollection;
        public Dictionary<string, bool> LogGroupActiveCollection;


        public ConsoelConfig(string stylePath)
        {
            fileName = stylePath + "/ConsoleConfig.xml";
            config = XDocument.Load(fileName);
            XElement root = config.Root;

            XElement logTypeElement = root.Element("LogType");
            UseSingleType = logTypeElement.Element("Single").Element("UseSingle").Value == "ture";
            SingleType = logTypeElement.Element("Single").Element("Single").Value;
            LogTypeActiveCollection = new Dictionary<string, bool>();
            foreach (XElement element in logTypeElement.Element("Multi").Elements())
            {
                LogTypeActiveCollection.Add(element.Name.ToString(), element.Value == "true");
            }


            XElement logGroupElement = root.Element("LogGroup");
            UseSingleGroup = logGroupElement.Element("Single").Element("UseSingle").Value == "ture";
            SingleGroup = logGroupElement.Element("Single").Element("Single").Value;
            LogGroupActiveCollection = new Dictionary<string, bool>();
            foreach (XElement element in logGroupElement.Element("Multi").Elements())
            {
                LogGroupActiveCollection.Add(element.Name.ToString(), element.Value == "true");
            }

            ConsoleManager.GetInstenceWithoutCheck().ChangeLogStyle(root.Element("NowChoiseLogStyle").Value);
        }
    }
}

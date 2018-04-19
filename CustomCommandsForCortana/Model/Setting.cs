using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace CustomCommandsForCortana.Model
{
    public class Setting
    {
        public string Value { get; set; }
        public string Name { get; set; }
        public string Description { get; set; } = "";

        /*public XmlNodeList ToXmlNode()
        {
            return XmlNodeList;
        }*/

        public new string ToString()
        {
            return "Setting Name: " + Name + "; Value: " + Value + "; Description: " + Description + ";";
        }
    }

    public class ToggleSetting : Setting
    {

        public ToggleSetting(string name, bool value, string description = "")
        {
            Value = value ? "true" : "false";
            Name = name;
            Description = description;
        }

        public XElement ToXml()
        {
            return
                new XElement("toggle",
                    new XAttribute("name", Name),
                    new XAttribute("value", Value),
                    new XAttribute("description", Description)
                    );
        }
    }

    public class StringSetting : Setting
    {

        public StringSetting(string name, string value, string description = "")
        {
            Value = value;
            Name = name;
            Description = description;
        }

        public XElement ToXml()
        {
            return
                new XElement("string",
                    new XAttribute("name", Name),
                    new XAttribute("value", Value),
                    new XAttribute("description", Description)
                    );
        }
    }

    public class IntSetting : Setting
    {

        public IntSetting(string name, int value, string description = "")
        {
            Value = value.ToString();
            Name = name;
            Description = description;
        }

        public XElement ToXml()
        {
            return
                new XElement("int",
                    new XAttribute("name", Name),
                    new XAttribute("value", Value),
                    new XAttribute("description", Description)
                    );
        }
    }

    public class SettingsViewModel
    {
        private ObservableCollection<Setting> settings = new ObservableCollection<Setting>();
        public ObservableCollection<Setting> Settings { get { return this.settings; } }
        public SettingsViewModel() { }
    }
}

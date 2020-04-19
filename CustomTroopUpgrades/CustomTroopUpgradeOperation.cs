using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Xml.Serialization;

namespace CustomTroopUpgrades
{
    public class CustomTroopUpgradeOperation
    {
        // For XMLSerializer only
        private CustomTroopUpgradeOperation() { }

        public CustomTroopUpgradeOperation(string src, string dest, bool delete = false)
        {
            Source = src;
            Destination = dest;
            DeleteUpgradePath = delete;
        }

        [XmlAttribute]
        public string Source { get; set; }

        [XmlAttribute]
        public string Destination { get; set; }

        [XmlAttribute(DataType = "boolean")]
        [DefaultValue(false)]
        public bool DeleteUpgradePath { get; set; }
    }

    [XmlRoot("CustomTroopUpgrades", IsNullable = false)]
    public class CustomTroopUpgrades
    {
        // For XMLSerializer only
        private CustomTroopUpgrades() { }

        public CustomTroopUpgrades(string[] modules = null, params CustomTroopUpgradeOperation[] ctuOps)
        {
            if (modules == null || modules.Count() == 0)
                DependentModules = new string[] { "SandBoxCore" };
            else
                DependentModules = modules;
            CustomTroopUpgradeOps = ctuOps ?? (new CustomTroopUpgradeOperation[] { });
        }

        [XmlArray]
        [XmlArrayItem(typeof(string), ElementName = "Module")]
        [DefaultValue(new string[] { "SandBoxCore" })]
        public string[] DependentModules { get; set; }

        [XmlArray("CustomTroopUpgradeOperations")]
        [XmlArrayItem(typeof(CustomTroopUpgradeOperation), ElementName = "CustomTroopUpgrade")]
        public CustomTroopUpgradeOperation[] CustomTroopUpgradeOps { get; set; }
    }
}

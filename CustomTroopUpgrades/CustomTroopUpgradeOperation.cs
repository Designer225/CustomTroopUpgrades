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

        public CustomTroopUpgrades(string module = "SandBoxCore", params CustomTroopUpgradeOperation[] ctuOps)
        {
            Module = module;
            CustomTroopUpgradeOps = ctuOps;
        }

        [XmlAttribute]
        [DefaultValue("SandBoxCore")]
        public string Module { get; set; }

        [XmlElement("CustomTroopUpgrade")]
        public CustomTroopUpgradeOperation[] CustomTroopUpgradeOps { get; set; }
    }
}

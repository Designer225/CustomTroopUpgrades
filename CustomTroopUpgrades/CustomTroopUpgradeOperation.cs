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
        [XmlAttribute]
        public string Module { get; set; }

        [XmlElement("CustomTroopUpgrade")]
        public CustomTroopUpgradeOperation[] CustomTroopUpgradeOps { get; set; }
    }
}

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
    public class CustomTroopUpgrades : IComparable<CustomTroopUpgrades>
    {
        // For XMLSerializer only
        private CustomTroopUpgrades() { }

        public CustomTroopUpgrades(string[] modules = null, int priority = 10, params CustomTroopUpgradeOperation[] ctuOps)
        {
            if (modules == null || modules.Count() == 0)
                DependentModules = new string[] { "SandBoxCore" };
            else
                DependentModules = modules;
            Priority = priority;
            CustomTroopUpgradeOps = ctuOps ?? (new CustomTroopUpgradeOperation[] { });
        }

        [XmlArray]
        [XmlArrayItem(typeof(string), ElementName = "Module")]
        [DefaultValue(new string[] { "SandBoxCore" })]
        public string[] DependentModules { get; set; }

        [XmlAttribute]
        [DefaultValue(10)]
        public int Priority { get; set; }

        [XmlArray("CustomTroopUpgradeOperations")]
        [XmlArrayItem(typeof(CustomTroopUpgradeOperation), ElementName = "CustomTroopUpgrade")]
        public CustomTroopUpgradeOperation[] CustomTroopUpgradeOps { get; set; }

        public int CompareTo(CustomTroopUpgrades other)
        {
            int value = Priority.CompareTo(other.Priority);

            if (value == 0)
            {
                // Creating a set first to remove duplicates
                var copyThis = new HashSet<string>(DependentModules).ToList();
                var copyOther = new HashSet<string>(other.DependentModules).ToList();
                // Sorting the thing first just in case.
                copyThis.Sort();
                copyOther.Sort();

                for (int i = 0; i < copyThis.Count && i < copyOther.Count; i++)
                {
                    value = copyThis[i].CompareTo(copyOther[i]);

                    if (value != 0)
                        break;
                }

                if (value == 0)
                    value = copyThis.Count.CompareTo(copyOther.Count);
            }

            return value;
        }
    }
}

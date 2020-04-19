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
        internal static readonly string[] DEFAULT_MODULES = { "Native", "SandBoxCore", "Sandbox", "StoryMode", "CustomBattle" };

        // For XMLSerializer only
        private CustomTroopUpgrades() { }

        public CustomTroopUpgrades(string[] modules = null, int priority = 1000, params CustomTroopUpgradeOperation[] ctuOps)
        {
            DependentModules = modules;
            ProcessModules();
            Priority = priority;
            CustomTroopUpgradeOps = ctuOps ?? (new CustomTroopUpgradeOperation[] { });
        }

        [XmlArray]
        [XmlArrayItem(typeof(string), ElementName = "Module")]
        public string[] DependentModules { get; set; }

        [XmlAttribute]
        [DefaultValue(1000)]
        public int Priority { get; set; }

        [XmlArray("CustomTroopUpgradeOperations")]
        [XmlArrayItem(typeof(CustomTroopUpgradeOperation), ElementName = "CustomTroopUpgrade")]
        public CustomTroopUpgradeOperation[] CustomTroopUpgradeOps { get; set; }

        public int CompareTo(CustomTroopUpgrades other)
        {
            int value = Priority.CompareTo(other.Priority);

            if (value == 0)
            {
                for (int i = 0; i < DependentModules.Count() && i < other.DependentModules.Count(); i++)
                {
                    value = DependentModules[i].CompareTo(other.DependentModules[i]);

                    if (value != 0)
                        break;
                }

                if (value == 0)
                    value = DependentModules.Count().CompareTo(other.DependentModules.Count());
            }

            return value;
        }

        internal void ProcessModules ()
        {
            var combined = new List<string>(DEFAULT_MODULES);
            if (DependentModules != null)
                combined.AddRange(DependentModules);
            for (int i = 0; i < combined.Count; i++)
            {
                combined[i] = combined[i].ToLowerInvariant();
            }
            combined = combined.ToHashSet().ToList();
            combined.Sort();
            DependentModules = combined.ToArray();
        }
    }
}

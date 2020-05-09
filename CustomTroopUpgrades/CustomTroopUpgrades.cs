using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Linq;

namespace CustomTroopUpgrades
{

    [XmlRoot("CustomTroopUpgrades", IsNullable = false)]
    public class CustomTroopUpgrades : IComparable<CustomTroopUpgrades>
    {
        internal static readonly string[] DefaultModules = { "Native", "SandBoxCore", "Sandbox", "StoryMode", "CustomBattle" };

        // For XMLSerializer only
        private CustomTroopUpgrades() { }

        public CustomTroopUpgrades(string[] modules = null, int priority = 1000, CustomTroopUpgradeOperation[] ctuOps = null, CustomTroopReplaceOperation[] ctrOps = null)
        {
            DependentModules = modules;
            ProcessModules();
            Priority = priority;
            CustomTroopUpgradeOps = ctuOps;
            CustomTroopReplaceOps = ctrOps;
            ProcessArrays();
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

        [XmlArray("CustomTroopReplaceOperations")]
        [XmlArrayItem(typeof(CustomTroopReplaceOperation), ElementName = "CustomTroopReplacement")]
        public CustomTroopReplaceOperation[] CustomTroopReplaceOps { get; set; }

        public int CompareTo(CustomTroopUpgrades other)
        {
            int value = Priority.CompareTo(other.Priority);

            if (value == 0)
            {
                value = DependentModules.Count().CompareTo(other.DependentModules.Count());

                if (value == 0)
                {
                    for (int i = 0; i < DependentModules.Count() && i < other.DependentModules.Count(); i++)
                    {
                        value = DependentModules[i].CompareTo(other.DependentModules[i]);

                        if (value != 0)
                            break;
                    }
                }
            }

            return value;
        }

        internal void ProcessModules()
        {
            var combined = new List<string>(DefaultModules);
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

        internal void ProcessArrays()
        {
            if (CustomTroopUpgradeOps == null)
                CustomTroopUpgradeOps = new CustomTroopUpgradeOperation[] { };
            if (CustomTroopReplaceOps == null)
                CustomTroopReplaceOps = new CustomTroopReplaceOperation[] { };
        }
    }
}
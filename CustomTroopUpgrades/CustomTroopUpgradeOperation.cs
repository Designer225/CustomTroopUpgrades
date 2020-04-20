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

    public static class ReplaceFlags
    {
        public const int
            AllFlagsZero = 0,
            Age = 1,
            IsBasicTroop = 1 << 1, // 2
            TattooTags = 1 << 2, // 4
            IsSoldier = 1 << 3, // 8
            IsBasicHero = 1 << 4, // 16
            UpgradeTargets = 1 << 5, // 32
            UpgradeRequires = 1 << 6, // 64
            Culture = 1 << 7, // 128
            DefaultGroup = 1 << 8, // 256
            BodyProperties = 1 << 9, // 512
            FormationPositionPreference = 1 << 10, // 1024
            IsFemale = 1 << 11, // 2048
            Level = 1 << 12, // 4096
            Name = 1 << 13, // 8192
            Occupation = 1 << 14, // 16384
            Skills = 1 << 15, // 32768
            Traits = 1 << 16, // 65536
            Feats = 1 << 17, // 131072
            HairTags = 1 << 18, // 262144
            BeardTags = 1 << 19, // 524288
            CivilianEquipments = 1 << 20, // 1048576
            BattleEquipments = 1 << 21, // 2097152

            Upgrades = UpgradeTargets,
            CivilianTemplate = CivilianEquipments,
            BattleTemplate = BattleEquipments,
            FormationPosition = FormationPositionPreference,
            Equipments = CivilianEquipments | BattleEquipments, // 3145728
            Face = TattooTags | HairTags | BeardTags | BodyProperties, // 786948

            AllFlags = Age | IsBasicTroop | Face | IsSoldier | IsBasicHero | UpgradeTargets | UpgradeRequires | Culture | DefaultGroup | FormationPositionPreference
                | IsFemale | Level | Name | Occupation | Skills | Traits | Feats | Equipments; // 4194303

        public static bool HasFlag (int combinedFlag, int specificFlag)
        {
            return (combinedFlag & specificFlag) == specificFlag;
        }
    }

    public class CustomTroopReplaceOperation
    {
        // For XMLSerializer only
        private CustomTroopReplaceOperation() { }

        public CustomTroopReplaceOperation(string src, string dest, int copyUpgrades = 0)
        {
            Source = src;
            Destination = dest;
            ReplaceFlag = copyUpgrades;
        }

        [XmlAttribute]
        public string Source { get; set; }

        [XmlAttribute]
        public string Destination { get; set; }

        [XmlAttribute]
        [DefaultValue(ReplaceFlags.AllFlags)]
        public int ReplaceFlag { get; set; }
    }

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

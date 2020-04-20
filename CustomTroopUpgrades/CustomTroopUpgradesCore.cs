using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace CustomTroopUpgrades
{
    public class CustomTroopUpgradesCore : MBSubModuleBase
    {
        public static string ModulesPath { get; private set; } = System.IO.Path.Combine(BasePath.Name, "Modules");
        
        public static List<ModuleInfo> Modules { get; private set; } = new List<ModuleInfo>();

        public static List<CustomTroopUpgrades> CustomTroopUpgradesList { get; private set; } = new List<CustomTroopUpgrades>();

        public override void OnGameInitializationFinished(Game game)
        {
            base.OnGameInitializationFinished(game);
            if (Campaign.Current == null)
                return;
            if (!Modules.IsEmpty()) Modules.Clear();
            if (!CustomTroopUpgradesList.IsEmpty()) CustomTroopUpgradesList.Clear();

            // DO NOT USE TaleWorlds.Library.ModuleInfo.GetModules()! It gets ALL modules, not just the active ones.
            // Use TaleWorlds.Engine.Utilities.GetModulesNames(), then load ModuleInfos individually.
            // That function returns all ACTIVE modules instead.

            string[] moduleNames = Utilities.GetModulesNames();
            foreach (string moduleName in moduleNames)
            {
                ModuleInfo m = new ModuleInfo();
                m.Load(moduleName);
                Modules.Add(m);
            }

            XmlSerializer deserializer = new XmlSerializer(typeof(CustomTroopUpgrades));
            foreach (ModuleInfo module in Modules)
            {
                DirectoryInfo dataPath = new DirectoryInfo(System.IO.Path.Combine(ModulesPath, module.Alias, "CustomTroopUpgradesData"));
                if (dataPath.Exists)
                {
                    foreach (FileInfo xmlFile in dataPath.EnumerateFiles("*.xml"))
                    {
                        try
                        {
                            var upgrade = deserializer.Deserialize(xmlFile.OpenText()) as CustomTroopUpgrades;
                            upgrade.ProcessModules();
                            upgrade.ProcessArrays();
                            CustomTroopUpgradesList.Add(upgrade);
                        }
                        catch (Exception e)
                        {
                            Debug.PrintError(string.Format("[CustomTroopUpgrades] Failed to load file {0}\n\nError: {1}\n\n{2}",
                                xmlFile.FullName, e.Message, e.StackTrace), e.StackTrace);
                        }
                    }
                }
            }
            CustomTroopUpgradesList.Sort();

            foreach (CustomTroopUpgrades upgrades in CustomTroopUpgradesList)
            {
                ApplyOperations(upgrades, game);
            }
        }

        public static void ApplyOperations(CustomTroopUpgrades upgrades, Game g = null)
        {
            if (Modules.Where(x => upgrades.DependentModules.Contains(x.Id.ToLowerInvariant())).Count() < upgrades.DependentModules.Count()) return;
            if (g == null) g = Game.Current;

            var objectList = g.ObjectManager.GetObjectTypeList<CharacterObject>();
            try
            {
                ApplyReplaceOperations(upgrades, objectList);
            }
            catch (Exception e)
            {
                Debug.PrintError(string.Format("[CustomTroopUpgrades] Failed to apply upgrade set.\n\n{0}", e), e.StackTrace);
            }
            try
            {
                ApplyUpgradeOperations(upgrades, objectList);
            }
            catch (Exception e)
            {
                Debug.PrintError(string.Format("[CustomTroopUpgrades] Failed to apply upgrade set.\n\n{0}", e), e.StackTrace);
            }
        }

        private const BindingFlags AllAccessFlag = (BindingFlags)(-1);

        private static readonly PropertyInfo IsSoldierProperty =
            typeof(BasicCharacterObject).GetProperty(nameof(BasicCharacterObject.IsSoldier), AllAccessFlag);

        private static readonly FieldInfo IsBasicHeroField =
            typeof(BasicCharacterObject).GetField("_isBasicHero", AllAccessFlag);

        private static readonly PropertyInfo UpgradeTargetsProperty =
            typeof(CharacterObject).GetProperty(nameof(CharacterObject.UpgradeTargets), AllAccessFlag);

        private static readonly PropertyInfo UpgradeRequiresItemFromCategoryProperty =
            typeof(CharacterObject).GetProperty(nameof(CharacterObject.UpgradeRequiresItemFromCategory), AllAccessFlag);

        private static readonly FieldInfo DynamicBodyPropertiesField =
            typeof(BasicCharacterObject).GetField("_dynamicBodyProperties", AllAccessFlag);

        private static readonly PropertyInfo StaticBodyPropertiesProperty =
            typeof(Hero).GetProperty("StaticBodyProperties", AllAccessFlag);

        private static readonly PropertyInfo FormationPositionPreferenceProperty =
            typeof(BasicCharacterObject).GetProperty(nameof(BasicCharacterObject.FormationPositionPreference), AllAccessFlag);

        private static readonly PropertyInfo OccupationProperty =
            typeof(CharacterObject).GetProperty(nameof(CharacterObject.Occupation), AllAccessFlag);

        private static readonly FieldInfo CharacterSkillsField =
            typeof(BasicCharacterObject).GetField("_characterSkills", AllAccessFlag);

        private static readonly FieldInfo CharacterTraitsField =
            typeof(CharacterObject).GetField("_characterTraits", AllAccessFlag);

        private static readonly FieldInfo CharacterFeatsField =
            typeof(CharacterObject).GetField("_characterFeats", AllAccessFlag);

        private static readonly FieldInfo CivilianEquipmentTemplateField =
            typeof(CharacterObject).GetField("_civilianEquipmentTemplate", AllAccessFlag);

        private static readonly FieldInfo BattleEquipmentTemplateField =
            typeof(CharacterObject).GetField("_battleEquipmentTemplate", AllAccessFlag);

        internal static void ApplyReplaceOperations(CustomTroopUpgrades upgrades, MBReadOnlyList<CharacterObject> objectList)
        {
            foreach (CustomTroopReplaceOperation operation in upgrades.CustomTroopReplaceOps)
            {
                var items = new List<CharacterObject>(objectList.Where(x => x.StringId.Equals(operation.Source) || x.StringId.Equals(operation.Destination)));
                if (items.Count() < 2) continue;

                CharacterObject source = items.Find(x => x.StringId.Equals(operation.Source));
                CharacterObject destination = items.Find(x => x.StringId.Equals(operation.Destination));
                if (source == null || destination == null) continue;
                int replaceFlag = operation.ReplaceFlag == ReplaceFlags.AllFlagsZero ? ReplaceFlags.AllFlags : operation.ReplaceFlag;

                // always unaffected: StringID, _originCharacterStringID
                destination.Initialize();
                if (ReplaceFlags.HasFlag(replaceFlag, ReplaceFlags.Age)) destination.Age = source.Age;
                if (ReplaceFlags.HasFlag(replaceFlag, ReplaceFlags.IsBasicTroop)) destination.IsBasicTroop = source.IsBasicTroop;
                if (ReplaceFlags.HasFlag(replaceFlag, ReplaceFlags.TattooTags)) destination.TattooTags = source.TattooTags;
                if (ReplaceFlags.HasFlag(replaceFlag, ReplaceFlags.IsSoldier)) IsSoldierProperty.SetValue(destination, source.IsSoldier);
                if (ReplaceFlags.HasFlag(replaceFlag, ReplaceFlags.IsBasicHero)) IsBasicHeroField.SetValue(destination, IsBasicHeroField.GetValue(source));
                if (ReplaceFlags.HasFlag(replaceFlag, ReplaceFlags.UpgradeTargets))
                    UpgradeTargetsProperty.SetValue(destination, source.UpgradeTargets?.ToArray());
                if (ReplaceFlags.HasFlag(replaceFlag, ReplaceFlags.UpgradeRequires))
                    UpgradeRequiresItemFromCategoryProperty.SetValue(destination, source.UpgradeRequiresItemFromCategory);
                if (ReplaceFlags.HasFlag(replaceFlag, ReplaceFlags.Culture)) destination.Culture = source.Culture;
                if (ReplaceFlags.HasFlag(replaceFlag, ReplaceFlags.DefaultGroup))
                {
                    destination.CurrentFormationClass = source.CurrentFormationClass;
                    destination.DefaultFormationGroup = source.DefaultFormationGroup;
                }
                if (ReplaceFlags.HasFlag(replaceFlag, ReplaceFlags.BodyProperties))
                {
                    DynamicBodyPropertiesField.SetValue(destination, DynamicBodyPropertiesField.GetValue(source));
                    if (!source.IsHero)
                    {
                        destination.StaticBodyPropertiesMin = source.StaticBodyPropertiesMin;
                        destination.StaticBodyPropertiesMax = source.StaticBodyPropertiesMax;
                    }
                    else destination.StaticBodyPropertiesMin = (StaticBodyProperties)(StaticBodyPropertiesProperty.GetValue(source.HeroObject));
                }

                if (ReplaceFlags.HasFlag(replaceFlag, ReplaceFlags.FormationPositionPreference))
                    FormationPositionPreferenceProperty.SetValue(destination, source.FormationPositionPreference);
                if (ReplaceFlags.HasFlag(replaceFlag, ReplaceFlags.IsFemale)) destination.IsFemale = source.IsFemale;
                if (ReplaceFlags.HasFlag(replaceFlag, ReplaceFlags.Level)) destination.Level = source.Level;
                if (ReplaceFlags.HasFlag(replaceFlag, ReplaceFlags.Name)) destination.Name = source.Name;
                if (ReplaceFlags.HasFlag(replaceFlag, ReplaceFlags.Occupation)) OccupationProperty.SetValue(destination, source.Occupation);
                if (ReplaceFlags.HasFlag(replaceFlag, ReplaceFlags.Skills)) CharacterSkillsField.SetValue(destination, CharacterSkillsField.GetValue(source));
                // Half end
                if (ReplaceFlags.HasFlag(replaceFlag, ReplaceFlags.Traits)) CharacterTraitsField.SetValue(destination, CharacterTraitsField.GetValue(source));
                if (ReplaceFlags.HasFlag(replaceFlag, ReplaceFlags.Feats)) CharacterFeatsField.SetValue(destination, CharacterFeatsField.GetValue(source));
                if (ReplaceFlags.HasFlag(replaceFlag, ReplaceFlags.HairTags)) destination.HairTags = source.HairTags;
                if (ReplaceFlags.HasFlag(replaceFlag, ReplaceFlags.BeardTags)) destination.BeardTags = source.BeardTags;
                if (ReplaceFlags.HasFlag(replaceFlag, ReplaceFlags.CivilianTemplate))
                    CivilianEquipmentTemplateField.SetValue(destination, CivilianEquipmentTemplateField.GetValue(source));
                if (ReplaceFlags.HasFlag(replaceFlag, ReplaceFlags.BattleTemplate))
                    BattleEquipmentTemplateField.SetValue(destination, BattleEquipmentTemplateField.GetValue(source));
                if (ReplaceFlags.HasFlag(replaceFlag, ReplaceFlags.Equipments))
                    destination.InitializeEquipmentsOnLoad(source.AllEquipments.ToList());
                else
                    destination.InitializeEquipmentsOnLoad(destination.AllEquipments.ToList());
            }
        }

        internal static void ApplyUpgradeOperations(CustomTroopUpgrades upgrades, MBReadOnlyList<CharacterObject> objectList)
        {
            foreach (CustomTroopUpgradeOperation operation in upgrades.CustomTroopUpgradeOps)
            {
                var items = new List<CharacterObject>(objectList.Where(x => x.StringId.Equals(operation.Source) || x.StringId.Equals(operation.Destination)));
                if (items.Count() < 2) continue;

                CharacterObject source = items.Find(x => x.StringId.Equals(operation.Source));
                CharacterObject destination = items.Find(x => x.StringId.Equals(operation.Destination));
                if (source == null || destination == null) continue;

                var upgradeTargets = new List<CharacterObject>();
                if (source.UpgradeTargets != null)
                    upgradeTargets.AddRange(source.UpgradeTargets);

                if (upgradeTargets.Contains(destination) && operation.DeleteUpgradePath)
                    upgradeTargets.Remove(destination);
                if (!upgradeTargets.Contains(destination) && !operation.DeleteUpgradePath)
                {
                    if (upgradeTargets.Count == 2)
                        Debug.PrintWarning("[CustomTroopUpgrades] Total upgrade target count reached (max 2). " +
                            "Consider applying delete operations first. Stopping addition.\n" +
                            String.Format("source: {0}, targets: {1}, attempting to add: {2}",
                            source.StringId, upgradeTargets.Select(x => x.StringId), destination.StringId));
                    else
                        upgradeTargets.Add(destination);
                }

                UpgradeTargetsProperty.SetValue(source, upgradeTargets.ToArray());
            }
        }
    }
}

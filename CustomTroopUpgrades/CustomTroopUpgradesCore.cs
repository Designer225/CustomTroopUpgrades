using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

            var objectList = new List<CharacterObject>(g.ObjectManager.GetObjectTypeList<CharacterObject>());
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

        private static readonly PropertyInfo DefaultFormationClassProperty =
            typeof(BasicCharacterObject).GetProperty(nameof(BasicCharacterObject.DefaultFormationClass), AllAccessFlag);

        private static readonly FieldInfo DynamicBodyPropertiesField =
            typeof(BasicCharacterObject).GetField("_dynamicBodyProperties", AllAccessFlag);

        private static readonly PropertyInfo FormationPositionPreferenceProperty =
            typeof(BasicCharacterObject).GetProperty(nameof(BasicCharacterObject.FormationPositionPreference), AllAccessFlag);

        private static readonly FieldInfo CharacterSkillsField =
            typeof(BasicCharacterObject).GetField("_characterSkills", AllAccessFlag);

        private static readonly PropertyInfo UpgradeTargetsProperty =
            typeof(CharacterObject).GetProperty(nameof(CharacterObject.UpgradeTargets), AllAccessFlag);

        private static readonly PropertyInfo UpgradeRequiresItemFromCategoryProperty =
            typeof(CharacterObject).GetProperty(nameof(CharacterObject.UpgradeRequiresItemFromCategory), AllAccessFlag);

        private static readonly PropertyInfo OccupationProperty =
            typeof(CharacterObject).GetProperty(nameof(CharacterObject.Occupation), AllAccessFlag);

        private static readonly FieldInfo CharacterTraitsField =
            typeof(CharacterObject).GetField("_characterTraits", AllAccessFlag);

        private static readonly FieldInfo CharacterFeatsField =
            typeof(CharacterObject).GetField("_characterFeats", AllAccessFlag);

        private static readonly FieldInfo CivilianEquipmentTemplateField =
            typeof(CharacterObject).GetField("_civilianEquipmentTemplate", AllAccessFlag);

        private static readonly FieldInfo BattleEquipmentTemplateField =
            typeof(CharacterObject).GetField("_battleEquipmentTemplate", AllAccessFlag);

        private static readonly PropertyInfo IsTemplateProperty =
            typeof(CharacterObject).GetProperty(nameof(CharacterObject.IsTemplate), AllAccessFlag);

        private static readonly PropertyInfo IsChildTemplateProperty =
            typeof(CharacterObject).GetProperty(nameof(CharacterObject.IsChildTemplate), AllAccessFlag);

        private static readonly FieldInfo PersonaField =
            typeof(CharacterObject).GetField("_persona", AllAccessFlag);

        private static readonly PropertyInfo StaticBodyPropertiesProperty =
            typeof(Hero).GetProperty("StaticBodyProperties", AllAccessFlag);

        internal static void ApplyReplaceOperations(CustomTroopUpgrades upgrades, List<CharacterObject> objectList)
        {
            foreach (CustomTroopReplaceOperation operation in upgrades.CustomTroopReplaceOps)
            {
                CharacterObject source = objectList.Find(x => x.StringId.Equals(operation.Source));
                CharacterObject destination = objectList.Find(x => x.StringId.Equals(operation.Destination));
                if (source == default || destination == default) continue;
                //int replaceFlag = operation.ReplaceFlag == ReplaceFlags.AllFlagsZero ? ReplaceFlags.AllFlags : operation.ReplaceFlag;
                ReplaceFlags replaceFlag;

                if (!Enum.TryParse(operation.ReplaceFlag, out replaceFlag))
                {
                    Debug.Print($"[CustomTroopUpgrades] Invalid enum combo or value detected ({operation.ReplaceFlag}). Defaulting to {ReplaceFlags.AllFlags}.");
                    replaceFlag = ReplaceFlags.AllFlags;
                }

                if (replaceFlag == ReplaceFlags.AllFlagsZero) replaceFlag = ReplaceFlags.AllFlags;

                Debug.Print($"[CustomTroopUpgrades] Performing replace op from {source.StringId} to {destination.StringId} with flag {(int)replaceFlag} ({replaceFlag})");

                // always unaffected: StringID, _originCharacterStringID
                destination.Initialize();
                if (replaceFlag.HasFlag(ReplaceFlags.Age)) destination.Age = source.Age;
                if (replaceFlag.HasFlag(ReplaceFlags.IsBasicTroop)) destination.IsBasicTroop = source.IsBasicTroop;
                if (replaceFlag.HasFlag(ReplaceFlags.TattooTags)) destination.TattooTags = source.TattooTags;
                if (replaceFlag.HasFlag(ReplaceFlags.IsSoldier)) IsSoldierProperty.SetValue(destination, source.IsSoldier);
                if (replaceFlag.HasFlag(ReplaceFlags.IsBasicHero)) IsBasicHeroField.SetValue(destination, IsBasicHeroField.GetValue(source));
                if (replaceFlag.HasFlag(ReplaceFlags.UpgradeTargets))
                    UpgradeTargetsProperty.SetValue(destination, source.UpgradeTargets?.ToArray());
                if (replaceFlag.HasFlag(ReplaceFlags.UpgradeRequires))
                    UpgradeRequiresItemFromCategoryProperty.SetValue(destination, source.UpgradeRequiresItemFromCategory);
                if (replaceFlag.HasFlag(ReplaceFlags.Culture)) destination.Culture = source.Culture;
                if (replaceFlag.HasFlag(ReplaceFlags.DefaultGroup))
                {
                    DefaultFormationClassProperty.SetValue(destination, source.DefaultFormationClass); // seems to be changed from CurrentFormationClass in the latest update
                    destination.DefaultFormationGroup = source.DefaultFormationGroup;
                }
                if (replaceFlag.HasFlag(ReplaceFlags.BodyProperties))
                {
                    DynamicBodyPropertiesField.SetValue(destination, DynamicBodyPropertiesField.GetValue(source));
                    if (!source.IsHero)
                    {
                        destination.StaticBodyPropertiesMin = source.StaticBodyPropertiesMin;
                        destination.StaticBodyPropertiesMax = source.StaticBodyPropertiesMax;
                    }
                    else destination.StaticBodyPropertiesMin = (StaticBodyProperties)(StaticBodyPropertiesProperty.GetValue(source.HeroObject));
                }

                if (replaceFlag.HasFlag(ReplaceFlags.FormationPositionPreference))
                    FormationPositionPreferenceProperty.SetValue(destination, source.FormationPositionPreference);
                if (replaceFlag.HasFlag(ReplaceFlags.IsFemale)) destination.IsFemale = source.IsFemale;
                if (replaceFlag.HasFlag(ReplaceFlags.Level)) destination.Level = source.Level;
                if (replaceFlag.HasFlag(ReplaceFlags.Name)) destination.Name = source.Name;
                if (replaceFlag.HasFlag(ReplaceFlags.Occupation)) OccupationProperty.SetValue(destination, source.Occupation);
                if (replaceFlag.HasFlag(ReplaceFlags.Skills)) CharacterSkillsField.SetValue(destination, CharacterSkillsField.GetValue(source));
                // Half end
                if (replaceFlag.HasFlag(ReplaceFlags.Traits)) CharacterTraitsField.SetValue(destination, CharacterTraitsField.GetValue(source));
                if (replaceFlag.HasFlag(ReplaceFlags.Feats)) CharacterFeatsField.SetValue(destination, CharacterFeatsField.GetValue(source));
                if (replaceFlag.HasFlag(ReplaceFlags.HairTags)) destination.HairTags = source.HairTags;
                if (replaceFlag.HasFlag(ReplaceFlags.BeardTags)) destination.BeardTags = source.BeardTags;
                if (replaceFlag.HasFlag(ReplaceFlags.CivilianTemplate))
                    CivilianEquipmentTemplateField.SetValue(destination, CivilianEquipmentTemplateField.GetValue(source));
                if (replaceFlag.HasFlag(ReplaceFlags.BattleTemplate))
                    BattleEquipmentTemplateField.SetValue(destination, BattleEquipmentTemplateField.GetValue(source));
                if (replaceFlag.HasFlag(ReplaceFlags.Equipments))
                    destination.InitializeEquipmentsOnLoad(source.AllEquipments.ToList());
                else
                    destination.InitializeEquipmentsOnLoad(destination.AllEquipments.ToList());
                if (replaceFlag.HasFlag(ReplaceFlags.IsTemplate))
                {
                    IsTemplateProperty.SetValue(destination, source.IsTemplate);
                    IsChildTemplateProperty.SetValue(destination, source.IsChildTemplate);
                }
                if (replaceFlag.HasFlag(ReplaceFlags.Persona))
                    PersonaField.SetValue(destination, PersonaField.GetValue(source));
            }
        }

        internal static void ApplyUpgradeOperations(CustomTroopUpgrades upgrades, List<CharacterObject> objectList)
        {
            foreach (CustomTroopUpgradeOperation operation in upgrades.CustomTroopUpgradeOps)
            {
                CharacterObject source = objectList.Find(x => x.StringId.Equals(operation.Source));
                CharacterObject destination = objectList.Find(x => x.StringId.Equals(operation.Destination));
                if (source == default || destination == default) continue;

                var upgradeTargets = new List<CharacterObject>();
                if (source.UpgradeTargets != null)
                    upgradeTargets.AddRange(source.UpgradeTargets);

                CharacterObject replaced = objectList.Find(x => x.StringId.Equals(operation.Replaces));
                if (replaced != default)
                {
                    int replaceIndex = upgradeTargets.FindIndex(x => x.StringId.Equals(replaced.StringId));
                    if (replaceIndex > -1)
                    {
                        upgradeTargets[replaceIndex] = destination;
                    }
                    else
                    {
                        Debug.PrintWarning(string.Format("[CustomTroopUpgrades] No matching upgrade path from {0} to {1} ", source.StringId, replaced.StringId)
                            + string.Format("have been found. Aborting replacement with {0}.", destination.StringId));
                    }
                }
                else
                {
                    // sanity check to reduce number of current upgrades to 2?
                    while (upgradeTargets.Count > 2)
                    {
                        Debug.PrintError("[CustomTroopUpgrades] WARNING: excess troop upgrade paths detected. This mod will remove excess upgrades, but " +
                            string.Format("there will be problems (check your troop definition!). Removing: {0} -> {1}", source.StringId, upgradeTargets[0].StringId));
                        upgradeTargets.RemoveAt(0);
                    }

                    if (upgradeTargets.Contains(destination) && operation.DeleteUpgradePath)
                        upgradeTargets.Remove(destination);
                    if (!upgradeTargets.Contains(destination) && !operation.DeleteUpgradePath)
                    {
                        if (upgradeTargets.Count >= 2)
                            Debug.PrintWarning("[CustomTroopUpgrades] Total upgrade target count reached (max 2). " +
                                "Consider applying delete operations first. Aborting addition.\n" +
                                string.Format("source: {0}, targets: {1}, attempting to add: {2}",
                                source.StringId, upgradeTargets.Select(x => x.StringId), destination.StringId));
                        else
                            upgradeTargets.Add(destination);
                    }
                }

                UpgradeTargetsProperty.SetValue(source, upgradeTargets.ToArray());
            }
        }
    }
}

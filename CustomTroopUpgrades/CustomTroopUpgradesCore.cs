using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

            // DO NOT USE TaleWorlds.Library.ModuleInfo.GetModules()! It gets ALL modules, not just the active ones.
            // Use TaleWorlds.Engine.Utilities.GetModulesNames(), then load ModuleInfos individually.
            // That function returns all ACTIVE modules instead.
            if (!Modules.IsEmpty()) Modules.Clear();
            if (!CustomTroopUpgradesList.IsEmpty()) CustomTroopUpgradesList.Clear();

            string[] moduleNames = Utilities.GetModulesNames();
            foreach (string moduleName in moduleNames)
            {
                ModuleInfo m = new ModuleInfo();
                m.Load(moduleName);
                Modules.Add(m);
            }

            foreach (ModuleInfo module in Modules)
            {
                DirectoryInfo dataPath = new DirectoryInfo(System.IO.Path.Combine(ModulesPath, module.Alias, "CustomTroopUpgradesData"));
                if (dataPath.Exists)
                {
                    XmlSerializer deserializer = new XmlSerializer(typeof(CustomTroopUpgrades));
                    foreach (FileInfo xmlFile in dataPath.EnumerateFiles("*.xml"))
                    {
                        try
                        {
                            CustomTroopUpgradesList.Add(deserializer.Deserialize(xmlFile.OpenText()) as CustomTroopUpgrades);
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
                try
                {
                    ApplyOperations(upgrades, game);
                }
                catch (Exception e)
                {
                    Debug.PrintError(string.Format("[CustomTroopUpgrades] Failed to apply upgrade.\n\nError: {0}\n\n{1}",
                        e.Message, e.StackTrace), e.StackTrace);
                }
            }
        }

        public static void ApplyOperations(CustomTroopUpgrades upgrades, Game g = null)
        {
            if (Modules.Where(x => upgrades.DependentModules.Contains(x.Id)).Count() < upgrades.DependentModules.Count()) return;
            if (g == null) g = Game.Current;

            var objectList = g.ObjectManager.GetObjectTypeList<CharacterObject>();
            foreach (CustomTroopUpgradeOperation operation in upgrades.CustomTroopUpgradeOps)
            {
                var items = new List<CharacterObject>(objectList.Where(x => x.StringId.Equals(operation.Source) || x.StringId.Equals(operation.Destination)));
                if (items.Count() < 2) continue;

                CharacterObject source = items.Find(x => x.StringId.Equals(operation.Source));
                CharacterObject destination = items.Find(x => x.StringId.Equals(operation.Destination));
                bool deleteUpgradePath = operation.DeleteUpgradePath;

                var upgradeTargets = new List<CharacterObject>();
                if (source.UpgradeTargets != null)
                    upgradeTargets.AddRange(source.UpgradeTargets);

                if (upgradeTargets.Contains(destination) && deleteUpgradePath)
                    upgradeTargets.Remove(destination);
                if (!upgradeTargets.Contains(destination) && !deleteUpgradePath)
                {
                    if (upgradeTargets.Count == 2)
                        Debug.PrintWarning("[CustomTroopUpgrades] Total upgrade target count reached (max 2). Consider applying delete operations first. Stopping addition.");
                    else
                        upgradeTargets.Add(destination);
                }

                typeof(CharacterObject).GetProperty(nameof(CharacterObject.UpgradeTargets)).SetValue(source, upgradeTargets.ToArray());
            }
        }
    }
}

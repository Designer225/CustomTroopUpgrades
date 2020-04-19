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
        //public static string ModulePath { get; private set; } = System.IO.Path.Combine(BasePath.Name, "Modules", "CustomTroopUpgrades");
        public static List<ModuleInfo> Modules { get; private set; } = new List<ModuleInfo>();

        public override void OnGameInitializationFinished(Game game)
        {
            base.OnGameInitializationFinished(game);
            if (Campaign.Current == null)
                return;

            //DirectoryInfo moduleDir = new DirectoryInfo(ModulePath);
            //if (!moduleDir.Exists) return; // This should never happen. If it happens, this mod is the least of your problem.

            // DO NOT USE TaleWorlds.Library.ModuleInfo.GetModules()! It gets ALL modules, not just the active ones.
            // Use TaleWorlds.Engine.Utilities.GetModulesNames(), then load ModuleInfos individually.
            // That function returns all ACTIVE modules instead.
            if (!Modules.IsEmpty()) Modules.Clear();

            string[] moduleNames = Utilities.GetModulesNames();
            foreach (string moduleName in moduleNames)
            {
                ModuleInfo m = new ModuleInfo();
                m.Load(moduleName);
                Modules.Add(m);
            }

            foreach (ModuleInfo module in Modules)
            {
                //string stuff = string.Format("[CustomTroopUpgradesDebug] Printing module {0} information:\n", module.Id);
                //stuff += string.Format("[CustomTroopUpgradesDebug] module ID {0} matches Alias {1}: {2}\n",
                //    module.Id, module.Alias, string.Equals(module.Id, module.Alias, StringComparison.CurrentCultureIgnoreCase));
                //DirectoryInfo data = new DirectoryInfo(System.IO.Path.Combine(ModulesPath, module.Alias, "CustomTroopUpgradesData"));
                //stuff += string.Format("[CustomTroopUpgradesDebug] module contains CustomTroopUpgradesData: {0}\n", data.Exists);
                //Debug.Print(stuff);

                //DirectoryInfo dataPath = new DirectoryInfo(System.IO.Path.Combine(moduleDir.FullName, "CustomTroopUpgradesData"));
                DirectoryInfo dataPath = new DirectoryInfo(System.IO.Path.Combine(ModulesPath, module.Alias, "CustomTroopUpgradesData"));
                if (dataPath.Exists)
                {
                    XmlSerializer deserializer = new XmlSerializer(typeof(CustomTroopUpgrades));
                    foreach (FileInfo xmlFile in dataPath.EnumerateFiles("*.xml"))
                    {
                        try
                        {
                            var upgrades = deserializer.Deserialize(xmlFile.OpenText()) as CustomTroopUpgrades;
                            ApplyOperations(upgrades, game);
                        }
                        catch (Exception e)
                        {
                            Debug.PrintError(string.Format("[CustomTroopUpgrades] Failed to load file {0}\n\nError: {1}\n\n{2}",
                                xmlFile.FullName, e.Message, e.StackTrace), e.StackTrace);
                        }
                    }

                }
            }
        }

        public void ApplyOperations(CustomTroopUpgrades upgrades, Game g = null)
        {
            if (!Modules.Exists(x => x.Id.Equals(upgrades.Module))) return;
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

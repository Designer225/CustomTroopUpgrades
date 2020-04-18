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
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;

namespace CustomTroopUpgrades
{
    public class SubModule : MBSubModuleBase
    {
        //public static string ModulesPath { get; private set; } = Path.Combine(BasePath.Name, "Modules");
        public static string ModulePath { get; private set; } = Path.Combine(BasePath.Name, "Modules", "CustomTroopUpgrades");

        public override void OnGameInitializationFinished(Game game)
        {
            base.OnGameInitializationFinished(game);
            if (Campaign.Current == null)
                return;

            DirectoryInfo moduleDir = new DirectoryInfo(ModulePath);
            if (!moduleDir.Exists) return; // This should never happen. If it happens, this mod is the least of your problem.
            
            XmlSerializer deserializer = new XmlSerializer(typeof(CustomTroopUpgrades));
            //foreach (DirectoryInfo dir in modulesDir.EnumerateDirectories())
            //{
            DirectoryInfo dataPath = new DirectoryInfo(Path.Combine(moduleDir.FullName, "CustomTroopUpgradesData"));
            if (dataPath.Exists)
            {
                foreach (FileInfo xmlFile in dataPath.EnumerateFiles("*.xml"))
                {
                    try
                    {
                        var upgrades = deserializer.Deserialize(xmlFile.OpenText()) as CustomTroopUpgrades;
                        ApplyOperations(upgrades, game);
                    }
                    catch (Exception e)
                    {
                        Debug.PrintError(String.Format("[CustomTroopUpgrades] Failed to load file {0}\n\nError: {1}\n\n{2}", xmlFile.FullName, e.Message, e.StackTrace), e.StackTrace);
                    }
                }

            }
            //}
        }

        public void ApplyOperations(CustomTroopUpgrades upgrades, Game g)
        {
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
                        Debug.PrintWarning("[CustomTroopUpgrades] Total upgrade target count reached (max 2). Consider applying delete operations. Stopping addition.");
                    else
                        upgradeTargets.Add(destination);
                }

                typeof(CharacterObject).GetProperty(nameof(CharacterObject.UpgradeTargets)).SetValue(source, upgradeTargets.ToArray());
            }
        }
    }
}

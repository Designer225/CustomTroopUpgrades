﻿using System;
using System.Xml.Serialization;
using System.ComponentModel;

namespace CustomTroopUpgrades
{
    public class CustomTroopReplaceOperation
    {
        // For XMLSerializer only
        private CustomTroopReplaceOperation() { }

        public CustomTroopReplaceOperation(string src, string dest, string copyUpgrades = "AllFlags")
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
        [DefaultValue("AllFlags")]
        public string ReplaceFlag { get; set; }
    }

    [Flags]
    public enum ReplaceFlags
    {
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
        CivilianTemplate = 1 << 20, // 1048576
        BattleTemplate = 1 << 21, // 2097152
        Equipments = 1 << 22, // 4194304
        IsTemplate = 1 << 23, // 8388608
        Persona = 1 << 24, // 16777216
        HiddenInEncylopedia = 1 << 25, // 33554432
        CharacterRestrictionFlags = 1 << 26, // 67108864

        Upgrades = UpgradeTargets,
        FormationPosition = FormationPositionPreference,
        Face = TattooTags | HairTags | BeardTags | BodyProperties, // 786948

        AllButUpgrades = // 134217695
            AllFlags ^ Upgrades,

        AllFlags = // 134217727
            Age | IsBasicTroop | Face | IsSoldier | IsBasicHero | UpgradeTargets | UpgradeRequires | Culture | DefaultGroup | FormationPositionPreference
            | IsFemale | Level | Name | Occupation | Skills | Traits | Feats | CivilianTemplate | BattleTemplate | Equipments | IsTemplate | Persona | HiddenInEncylopedia
            | CharacterRestrictionFlags
    }
}
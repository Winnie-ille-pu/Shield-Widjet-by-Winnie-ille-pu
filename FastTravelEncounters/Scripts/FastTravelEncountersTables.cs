// Project:         Daggerfall Unity
// Copyright:       Copyright (C) 2009-2023 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Gavin Clayton (interkarma@dfworkshop.net)
// Contributors:    
// 
// Notes:
//

using System;
using System.Collections.Generic;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility;

namespace FastTravelEncounters
{
    #region Encounter Tables

    public static class RandomEncounterTables
    {
        public struct EncounterTable
        {
            public EncounterGroup[] Groups;
        }

        public struct EncounterGroup
        {
            public float cost;
            public int[] minions;   //spawn spawn spawn
            public int[] elites;    //spawn 1 after every 2 Minions
            public int[] bosses;    //spawn 1 after every 2 Elites

            //DEX indices
            public int[] minionsDEX;
            public int[] elitesDEX;
            public int[] bossesDEX;
        }

        public static EncounterGroup[] EncounterGroups = new EncounterGroup[]
        {
            //Vermin = 0
            new EncounterGroup()
            {
                cost = 0.5f,
                minions = new int[]
                {
                    (int)MobileTypes.Rat,
                    (int)MobileTypes.GiantBat,
                },
                minionsDEX = new int[]
                {
                    (int)MobileTypes.Rat,
                    (int)MobileTypes.GiantBat,
                    260, //Bat
                },
            },
            //Spriggans = 1
            new EncounterGroup()
            {
                cost = 1,
                minions = new int[]
                {
                    (int)MobileTypes.Spriggan,
                },
            },
            //Bears = 2
            new EncounterGroup()
            {
                cost = 2,
                minions = new int[]
                {
                    (int)MobileTypes.GrizzlyBear,
                },
                elitesDEX = new int[]
                {
                    384, //Druid
                    389, //Rogue Druid
                },
            },
            //Tigers = 3
            new EncounterGroup()
            {
                cost = 1,
                minions = new int[]
                {
                    (int)MobileTypes.SabertoothTiger,
                },
                minionsDEX = new int[]
                {
                    (int)MobileTypes.SabertoothTiger,
                    280, //Mountain Lion
                },
            },
            //Spiders = 4
            new EncounterGroup()
            {
                cost = 1,
                minions = new int[]
                {
                    (int)MobileTypes.Spider,
                },
                minionsDEX = new int[]
                {
                    (int)MobileTypes.Spider,
                    271, //Blood Spider
                },
            },
            //Orcs = 5
            new EncounterGroup()
            {
                cost = 1,
                minions = new int[]
                {
                    (int)MobileTypes.Orc,
                },
                elites = new int[]
                {
                    (int)MobileTypes.OrcSergeant,
                    (int)MobileTypes.OrcShaman,
                },
                bosses = new int[]
                {
                    (int)MobileTypes.OrcWarlord,
                },
                minionsDEX = new int[]
                {
                    (int)MobileTypes.Orc,
                    256, //Goblin
                },
                elitesDEX = new int[]
                {
                    (int)MobileTypes.OrcSergeant,
                    (int)MobileTypes.OrcShaman,
                    272, //Troll
                },
            },
            //Centaurs = 6
            new EncounterGroup()
            {
                cost = 1,
                minions = new int[]
                {
                    (int)MobileTypes.Centaur,
                },
                elitesDEX = new int[]
                {
                    269, //Minotaur
                },
            },
            //Werecreatures = 7
            new EncounterGroup()
            {
                cost = 1,
                minions = new int[]
                {
                    (int)MobileTypes.Werewolf,
                    (int)MobileTypes.Wereboar,
                },
                minionsDEX = new int[]
                {
                    (int)MobileTypes.Werewolf,
                    (int)MobileTypes.Wereboar,
                    262, //Wolf
                    267, //Dog
                    278, //Boar
                },
                elitesDEX = new int[]
                {
                    263, //Snow Wolf
                },
            },
            //Nymphs = 8
            new EncounterGroup()
            {
                cost = 1,
                minions = new int[]
                {
                    (int)MobileTypes.Nymph,
                },
                minionsDEX = new int[]
                {
                    (int)MobileTypes.Nymph,
                    268, //Nymph
                    283, //Will-o'-wisp
                },
            },
            //Harpies = 9
            new EncounterGroup()
            {
                cost = 1,
                minions = new int[]
                {
                    (int)MobileTypes.Harpy,
                },
            },
            //Undead = 10
            new EncounterGroup()
            {
                cost = 1,
                minions = new int[]
                {
                    (int)MobileTypes.SkeletalWarrior,
                    (int)MobileTypes.Zombie,
                    (int)MobileTypes.Ghost,
                },
                elites = new int[]
                {
                    (int)MobileTypes.Mummy,
                    (int)MobileTypes.Wraith,
                    (int)MobileTypes.Vampire,
                    (int)MobileTypes.Lich,
                },
                bosses = new int[]
                {
                    (int)MobileTypes.VampireAncient,
                    (int)MobileTypes.AncientLich,
                },
                minionsDEX = new int[]
                {
                    (int)MobileTypes.SkeletalWarrior,
                    (int)MobileTypes.Zombie,
                    (int)MobileTypes.Ghost,
                    266, //Skeletal Soldier
                    274, //Faded Ghost
                    277, //Ghoul
                    388, //Necromancer Acolyte
                },
                elitesDEX = new int[]
                {
                    (int)MobileTypes.Mummy,
                    (int)MobileTypes.Wraith,
                    (int)MobileTypes.Vampire,
                    (int)MobileTypes.Lich,
                    273, //Gloom Wraith
                    287, //Dire Ghoul
                    393, //Necromancer Glaiver
                    394, //Necromancer Assassin
                },
            },
            //Giants = 11
            new EncounterGroup()
            {
                cost = 1,
                minions = new int[]
                {
                    (int)MobileTypes.Giant,
                },
                minionsDEX = new int[]
                {
                    (int)MobileTypes.Giant,
                    282, //Ogre
                },
            },
            //Scorpions = 12
            new EncounterGroup()
            {
                cost = 1,
                minions = new int[]
                {
                    (int)MobileTypes.GiantScorpion,
                },
                minionsDEX = new int[]
                {
                    (int)MobileTypes.GiantScorpion,
                    258, //Lizard Man
                },
                elitesDEX = new int[]
                {
                    259, //Lizard Warrior
                },
            },
            //Magic = 13
            new EncounterGroup()
            {
                cost = 1,
                minions = new int[]
                {
                    (int)MobileTypes.Imp,
                },
                elites = new int[]
                {
                    (int)MobileTypes.FireAtronach,
                    (int)MobileTypes.IronAtronach,
                    (int)MobileTypes.FleshAtronach,
                    (int)MobileTypes.IceAtronach,
                    (int)MobileTypes.Gargoyle,
                },
                elitesDEX = new int[]
                {
                    (int)MobileTypes.FireAtronach,
                    (int)MobileTypes.IronAtronach,
                    (int)MobileTypes.FleshAtronach,
                    (int)MobileTypes.IceAtronach,
                    (int)MobileTypes.Gargoyle,
                    265, //Grotesque
                    289, //Centurion Sphere
                    257, //Homunculus
                    270, //Iron Golem
                    284, //Ice Golem
                    286, //Stone Golem
                    290, //Steam Centurioun
                },
            },
            //Daedra = 14
            new EncounterGroup()
            {
                cost = 1,
                minions = new int[]
                {
                    (int)MobileTypes.FrostDaedra,
                    (int)MobileTypes.FireDaedra,
                },
                elites = new int[]
                {
                    (int)MobileTypes.Daedroth,
                },
                bosses = new int[]
                {
                    (int)MobileTypes.DaedraSeducer,
                    (int)MobileTypes.DaedraLord,
                },
                minionsDEX = new int[]
                {
                    288, //Scamp
                },
                elitesDEX = new int[]
                {
                    (int)MobileTypes.FrostDaedra,
                    (int)MobileTypes.FireDaedra,
                    (int)MobileTypes.Daedroth,
                    264, //Hell Hound
                    285, //Dremora Churl
                    396, //Witch Defender
                },
                bossesDEX = new int[]
                {
                    (int)MobileTypes.DaedraSeducer,
                    (int)MobileTypes.DaedraLord,
                    276, //Fire Daemon
                },
            },
            //Dragonlings = 15
            new EncounterGroup()
            {
                cost = 1,
                minions = new int[]
                {
                    (int)MobileTypes.Dragonling,
                },
            },
            //KnightsAndMages = 16
            new EncounterGroup()
            {
                cost = 2,
                minions = new int[]
                {
                    (int)MobileTypes.Mage,
                    (int)MobileTypes.Spellsword,
                    (int)MobileTypes.Battlemage,
                    (int)MobileTypes.Sorcerer,
                    (int)MobileTypes.Healer,
                    (int)MobileTypes.Bard,
                    (int)MobileTypes.Acrobat,
                    (int)MobileTypes.Monk,
                    (int)MobileTypes.Archer,
                    (int)MobileTypes.Ranger,
                    (int)MobileTypes.Warrior,
                    (int)MobileTypes.Knight,
                },
                minionsDEX = new int[]
                {
                    (int)MobileTypes.Mage,
                    (int)MobileTypes.Spellsword,
                    (int)MobileTypes.Battlemage,
                    (int)MobileTypes.Sorcerer,
                    (int)MobileTypes.Healer,
                    (int)MobileTypes.Bard,
                    (int)MobileTypes.Acrobat,
                    (int)MobileTypes.Monk,
                    (int)MobileTypes.Archer,
                    (int)MobileTypes.Ranger,
                    (int)MobileTypes.Warrior,
                    (int)MobileTypes.Knight,
                    386, //Knight Rider
                    390, //Bounty Hunter
                    391, //Royal Knight
                    397, //Spellsword Rider
                    398, //Archer Rider
                },
            },
            //Criminals = 17
            new EncounterGroup()
            {
                cost = 2,
                minions = new int[]
                {
                    (int)MobileTypes.Nightblade,
                    (int)MobileTypes.Burglar,
                    (int)MobileTypes.Rogue,
                    (int)MobileTypes.Thief,
                    (int)MobileTypes.Assassin,
                    (int)MobileTypes.Barbarian,
                },
                minionsDEX = new int[]
                {
                    (int)MobileTypes.Nightblade,
                    (int)MobileTypes.Burglar,
                    (int)MobileTypes.Rogue,
                    (int)MobileTypes.Thief,
                    (int)MobileTypes.Assassin,
                    (int)MobileTypes.Barbarian,
                    387, //Rogue Rider
                    392, //Thief Rider
                    395, //Dark Slayer
                },
            },
            //KnightsAndMagesInterior = 18
            new EncounterGroup()
            {
                cost = 2,
                minions = new int[]
                {
                    (int)MobileTypes.Mage,
                    (int)MobileTypes.Spellsword,
                    (int)MobileTypes.Battlemage,
                    (int)MobileTypes.Sorcerer,
                    (int)MobileTypes.Healer,
                    (int)MobileTypes.Bard,
                    (int)MobileTypes.Acrobat,
                    (int)MobileTypes.Monk,
                    (int)MobileTypes.Archer,
                    (int)MobileTypes.Ranger,
                    (int)MobileTypes.Warrior,
                    (int)MobileTypes.Knight,
                },
                minionsDEX = new int[]
                {
                    (int)MobileTypes.Mage,
                    (int)MobileTypes.Spellsword,
                    (int)MobileTypes.Battlemage,
                    (int)MobileTypes.Sorcerer,
                    (int)MobileTypes.Healer,
                    (int)MobileTypes.Bard,
                    (int)MobileTypes.Acrobat,
                    (int)MobileTypes.Monk,
                    (int)MobileTypes.Archer,
                    (int)MobileTypes.Ranger,
                    (int)MobileTypes.Warrior,
                    (int)MobileTypes.Knight,
                    390, //Bounty Hunter
                    391, //Royal Knight
                },
            },
            //CriminalsInteriors = 19
            new EncounterGroup()
            {
                cost = 2,
                minions = new int[]
                {
                    (int)MobileTypes.Nightblade,
                    (int)MobileTypes.Burglar,
                    (int)MobileTypes.Rogue,
                    (int)MobileTypes.Thief,
                    (int)MobileTypes.Assassin,
                    (int)MobileTypes.Barbarian,
                },
                minionsDEX = new int[]
                {
                    (int)MobileTypes.Nightblade,
                    (int)MobileTypes.Burglar,
                    (int)MobileTypes.Rogue,
                    (int)MobileTypes.Thief,
                    (int)MobileTypes.Assassin,
                    (int)MobileTypes.Barbarian,
                    395, //Dark Slayer
                },
            },
            //Aquatic = 20
            new EncounterGroup()
            {
                cost = 2,
                minions = new int[]
                {
                    (int)MobileTypes.Rat,
                    (int)MobileTypes.GiantBat,
                },
                minionsDEX = new int[]
                {
                    281, //Mudcrab
                },
                elitesDEX = new int[]
                {
                    261, //Medusa
                    279, //Land Dreugh
                },
            },

        };

        public static EncounterTable[] EncounterTables = new EncounterTable[]
        {
            // Desert, in location, night - Index0
            new EncounterTable()
            {
                Groups = new EncounterGroup[]
                {
                    EncounterGroups[0],     //Vermin
                    EncounterGroups[5],     //Orcs
                    EncounterGroups[7],     //Werecreatures
                    EncounterGroups[10],     //Undead
                    EncounterGroups[12],     //Scorpions
                    EncounterGroups[16],     //KnightAndMages
                    EncounterGroups[17],     //Criminals
                },
            },

            // Desert, not in location, day - Index1
            new EncounterTable()
            {
                Groups = new EncounterGroup[]
                {
                    EncounterGroups[0],     //Vermin
                    EncounterGroups[3],     //Tigers
                    EncounterGroups[5],     //Orcs
                    EncounterGroups[6],     //Centaurs
                    EncounterGroups[9],     //Harpies
                    EncounterGroups[10],     //Undead
                    EncounterGroups[11],     //Giants
                    EncounterGroups[12],     //Scorpions
                    EncounterGroups[15],     //Dragonlings
                    EncounterGroups[16],     //KnightAndMages
                    EncounterGroups[17],     //Criminals
                },
            },

            // Desert, not in location, night - Index2
            new EncounterTable()
            {
                Groups = new EncounterGroup[]
                {
                    EncounterGroups[0],     //Vermin
                    EncounterGroups[3],     //Tigers
                    EncounterGroups[5],     //Orcs
                    EncounterGroups[7],     //Werecreatures
                    EncounterGroups[9],     //Harpies
                    EncounterGroups[10],     //Undead
                    EncounterGroups[12],     //Scorpions
                    EncounterGroups[13],     //Magic
                    EncounterGroups[15],     //Dragonlings
                    EncounterGroups[16],     //KnightAndMages
                    EncounterGroups[17],     //Criminals
                },
            },

            // Mountain, in location, night - Index3
            new EncounterTable()
            {
                Groups = new EncounterGroup[]
                {
                    EncounterGroups[0],     //Vermin
                    EncounterGroups[5],     //Orcs
                    EncounterGroups[7],     //Werecreatures
                    EncounterGroups[16],     //KnightAndMages
                    EncounterGroups[17],     //Criminals
                },
            },

            // Mountain, not in location, day - Index4
            new EncounterTable()
            {
                Groups = new EncounterGroup[]
                {
                    EncounterGroups[1],     //Spriggans
                    EncounterGroups[2],     //Bears
                    EncounterGroups[5],     //Orcs
                    EncounterGroups[8],     //Nymphs
                    EncounterGroups[9],     //Harpies
                    EncounterGroups[11],     //Giants
                    EncounterGroups[13],     //Magic
                    EncounterGroups[16],     //KnightAndMages
                    EncounterGroups[17],     //Criminals
                },
            },

            // Mountain, not in location, night - Index5
            new EncounterTable()
            {
                Groups = new EncounterGroup[]
                {
                    EncounterGroups[0],     //Vermin
                    EncounterGroups[1],     //Spriggans
                    EncounterGroups[2],     //Bears
                    EncounterGroups[3],     //Tigers
                    EncounterGroups[5],     //Orcs
                    EncounterGroups[6],     //Centaurs
                    EncounterGroups[7],     //Werecreatures
                    EncounterGroups[10],     //Undead
                    EncounterGroups[11],     //Giants
                    EncounterGroups[13],     //Magic
                    EncounterGroups[15],     //Dragonlings
                    EncounterGroups[16],     //KnightAndMages
                    EncounterGroups[17],     //Criminals
                },
            },

            // Rainforest, in location, night - Index6
            new EncounterTable()
            {
                Groups = new EncounterGroup[]
                {
                    EncounterGroups[0],     //Vermin
                    EncounterGroups[7],     //Werecreatures
                    EncounterGroups[9],     //Harpies
                    EncounterGroups[10],     //Undead
                    EncounterGroups[16],     //KnightAndMages
                    EncounterGroups[17],     //Criminals
                },
            },

            // Rainforest, not in location, day - Index7
            new EncounterTable()
            {
                Groups = new EncounterGroup[]
                {
                    EncounterGroups[0],     //Vermin
                    EncounterGroups[2],     //Bears
                    EncounterGroups[3],     //Tigers
                    EncounterGroups[4],     //Spiders
                    EncounterGroups[8],     //Nymphs
                    EncounterGroups[9],     //Harpies
                    EncounterGroups[11],     //Giants
                    EncounterGroups[12],     //Scorpions
                    EncounterGroups[16],     //KnightAndMages
                    EncounterGroups[17],     //Criminals
                    EncounterGroups[20],     //Aquatic
                },
            },

            // Rainforest, not in location, night - Index8
            new EncounterTable()
            {
                Groups = new EncounterGroup[]
                {
                    EncounterGroups[0],     //Vermin
                    EncounterGroups[3],     //Tigers
                    EncounterGroups[4],     //Spiders
                    EncounterGroups[7],     //Werecreatures
                    EncounterGroups[9],     //Harpies
                    EncounterGroups[10],     //Undead
                    EncounterGroups[11],     //Giants
                    EncounterGroups[12],     //Scorpions
                    EncounterGroups[14],     //Daedra
                    EncounterGroups[16],     //KnightAndMages
                    EncounterGroups[17],     //Criminals
                    EncounterGroups[20],     //Aquatic
                },
            },

            // Subtropical, in location, night - Index9
            new EncounterTable()
            {
                Groups = new EncounterGroup[]
                {
                    EncounterGroups[0],     //Vermin
                    EncounterGroups[10],     //Undead
                    EncounterGroups[16],     //KnightAndMages
                    EncounterGroups[17],     //Criminals
                },
            },

            // Subtropical, not in location, day - Index10
            new EncounterTable()
            {
                Groups = new EncounterGroup[]
                {
                    EncounterGroups[0],     //Vermin
                    EncounterGroups[2],     //Bears
                    EncounterGroups[3],     //Tigers
                    EncounterGroups[6],     //Centaurs
                    EncounterGroups[8],     //Nymphs
                    EncounterGroups[11],     //Giants
                    EncounterGroups[12],     //Scorpions
                    EncounterGroups[13],     //Magic
                    EncounterGroups[15],     //Dragonlings
                    EncounterGroups[16],     //KnightAndMages
                    EncounterGroups[17],     //Criminals
                    EncounterGroups[20],     //Aquatic
                },
            },

            // Subtropical, not in location, night - Index11
            new EncounterTable()
            {
                Groups = new EncounterGroup[]
                {
                    EncounterGroups[0],     //Vermin
                    EncounterGroups[2],     //Bears
                    EncounterGroups[3],     //Tigers
                    EncounterGroups[4],     //Spiders
                    EncounterGroups[7],     //Werecreatures
                    EncounterGroups[9],     //Harpies
                    EncounterGroups[10],     //Undead
                    EncounterGroups[12],     //Scorpions
                    EncounterGroups[16],     //KnightAndMages
                    EncounterGroups[17],     //Criminals
                    EncounterGroups[20],     //Aquatic
                },
            },

            // Swamp/woodlands, in location, night - Index12
            new EncounterTable()
            {
                Groups = new EncounterGroup[]
                {
                    EncounterGroups[0],     //Vermin
                    EncounterGroups[7],     //Werecreatures
                    EncounterGroups[10],     //Undead
                    EncounterGroups[16],     //KnightAndMages
                    EncounterGroups[17],     //Criminals
                },
            },

            // Swamp/woodlands, not in location, day - Index13
            new EncounterTable()
            {
                Groups = new EncounterGroup[]
                {
                    EncounterGroups[0],     //Vermin
                    EncounterGroups[1],     //Spriggans
                    EncounterGroups[2],     //Bears
                    EncounterGroups[5],     //Orcs
                    EncounterGroups[6],     //Centaurs
                    EncounterGroups[11],     //Giants
                    EncounterGroups[15],     //Dragonlings
                    EncounterGroups[16],     //KnightAndMages
                    EncounterGroups[17],     //Criminals
                    EncounterGroups[20],     //Aquatic
                },
            },

            // Swamp/woodlands, not in location, night - Index14
            new EncounterTable()
            {
                Groups = new EncounterGroup[]
                {
                    EncounterGroups[0],     //Vermin
                    EncounterGroups[2],     //Bears
                    EncounterGroups[4],     //Spiders
                    EncounterGroups[7],     //Werecreatures
                    EncounterGroups[9],     //Harpies
                    EncounterGroups[10],     //Undead
                    EncounterGroups[11],     //Giants
                    EncounterGroups[13],     //Magic
                    EncounterGroups[15],     //Dragonlings
                    EncounterGroups[16],     //KnightAndMages
                    EncounterGroups[17],     //Criminals
                    EncounterGroups[20],     //Aquatic
                },
            },

            // Haunted woodlands, in location, night - Index15
            new EncounterTable()
            {
                Groups = new EncounterGroup[]
                {
                    EncounterGroups[0],     //Vermin
                    EncounterGroups[7],     //Werecreatures
                    EncounterGroups[10],     //Undead
                    EncounterGroups[16],     //KnightAndMages
                    EncounterGroups[17],     //Criminals
                },
            },

            // Haunted woodlands, not in location, day - Index16
            new EncounterTable()
            {
                Groups = new EncounterGroup[]
                {
                    EncounterGroups[1],     //Spriggans
                    EncounterGroups[2],     //Bears
                    EncounterGroups[4],     //Spiders
                    EncounterGroups[6],     //Centaurs
                    EncounterGroups[8],     //Nymphs
                    EncounterGroups[9],     //Harpies
                    EncounterGroups[11],     //Giants
                    EncounterGroups[13],     //Magic
                    EncounterGroups[15],     //Dragonlings
                    EncounterGroups[16],     //KnightAndMages
                    EncounterGroups[17],     //Criminals
                },
            },

            // Haunted woodlands, not in location, night - Index17
            new EncounterTable()
            {
                Groups = new EncounterGroup[]
                {
                    EncounterGroups[0],     //Vermin
                    EncounterGroups[2],     //Bears
                    EncounterGroups[4],     //Spiders
                    EncounterGroups[7],     //Werecreatures
                    EncounterGroups[10],     //Undead
                    EncounterGroups[11],     //Giants
                    EncounterGroups[13],     //Magic
                    EncounterGroups[14],     //Daedra
                    EncounterGroups[15],     //Dragonlings
                    EncounterGroups[16],     //KnightAndMages
                    EncounterGroups[17],     //Criminals
                },
            },

            // Unused - Index18
            new EncounterTable()
            {
                Groups = new EncounterGroup[]
                {
                    EncounterGroups[0],     //Vermin
                    EncounterGroups[10],     //Undead
                    EncounterGroups[19],     //CriminalsInterior
                },
            },

            // Default building - Index19
            new EncounterTable()
            {
                Groups = new EncounterGroup[]
                {
                    EncounterGroups[0],     //Vermin
                    EncounterGroups[18],     //KnightAndMagesInterior
                    EncounterGroups[19],     //CriminalsInterior
                },
            },

            // Guildhall - Index20
            new EncounterTable()
            {
                Groups = new EncounterGroup[]
                {
                    EncounterGroups[13],     //Magic
                    EncounterGroups[14],     //Daedra
                    EncounterGroups[18],     //KnightAndMagesInterior
                },
            },

            // Temple - Index21
            new EncounterTable()
            {
                Groups = new EncounterGroup[]
                {
                    EncounterGroups[18],     //KnightAndMagesInterior
                },
            },

            // Palace, House1 - Index22
            new EncounterTable()
            {
                Groups = new EncounterGroup[]
                {
                    EncounterGroups[18],     //KnightAndMagesInterior
                },
            },

            // House2 - Index23
            new EncounterTable()
            {
                Groups = new EncounterGroup[]
                {
                    EncounterGroups[10],     //Undead
                    EncounterGroups[18],     //KnightAndMagesInterior
                    EncounterGroups[19],     //CriminalsInterior
                },
            },

            // House3 - Index24
            new EncounterTable()
            {
                Groups = new EncounterGroup[]
                {
                    EncounterGroups[0],     //Vermin
                    EncounterGroups[18],     //KnightAndMagesInterior
                    EncounterGroups[19],     //CriminalsInterior
                },
            },

            // ShipInterior - Index25
            new EncounterTable()
            {
                Groups = new EncounterGroup[]
                {
                    EncounterGroups[0],      //Vermin
                    EncounterGroups[5],      //Orcs
                    EncounterGroups[13],      //Magic
                    EncounterGroups[14],      //Daedra
                    EncounterGroups[18],     //KnightAndMagesInterior
                    EncounterGroups[19],     //CriminalsInterior
                    EncounterGroups[20],     //Aquatic
                },
            },

            // ShipExterior - Index26
            new EncounterTable()
            {
                Groups = new EncounterGroup[]
                {
                    EncounterGroups[0],      //Vermin
                    EncounterGroups[5],      //Orcs
                    EncounterGroups[9],      //Harpies
                    EncounterGroups[13],      //Magic
                    EncounterGroups[14],      //Daedra
                    EncounterGroups[15],      //Dragonlings
                    EncounterGroups[18],     //KnightAndMagesInterior
                    EncounterGroups[19],     //CriminalsInterior
                    EncounterGroups[20],     //Aquatic
                },
            },
        };
        #endregion

        #region Public methods

        public static MobileTypes[] BuildEncounter(int count, bool atSea = false)
        {
            int encounterTableIndex = 0;

            List<MobileTypes> encounterMobiles = new List<MobileTypes>();

            PlayerEnterExit playerEnterExit = GameManager.Instance.PlayerEnterExit;
            PlayerGPS playerGPS = GameManager.Instance.PlayerGPS;

            if (!playerEnterExit || !playerGPS)
                return encounterMobiles.ToArray();

            //get encounter table
            if (playerEnterExit.IsPlayerInsideBuilding)
            {
                DFLocation.BuildingTypes buildingType = playerEnterExit.BuildingType;

                if (buildingType == DFLocation.BuildingTypes.GuildHall)
                    encounterTableIndex = 20;
                else if (buildingType == DFLocation.BuildingTypes.Temple)
                    encounterTableIndex = 21;
                else if (buildingType == DFLocation.BuildingTypes.Palace
                    || buildingType == DFLocation.BuildingTypes.House1)
                    encounterTableIndex = 22;
                else if (buildingType == DFLocation.BuildingTypes.House2)
                    encounterTableIndex = 23;
                else if (buildingType == DFLocation.BuildingTypes.House3)
                    encounterTableIndex = 24;
                else
                {
                    if (atSea)
                        encounterTableIndex = 25;
                    else
                        encounterTableIndex = 19;
                }
            }
            else
            {
                int climate = playerGPS.CurrentClimateIndex;
                bool isDay = DaggerfallUnity.Instance.WorldTime.Now.IsDay;

                if (playerGPS.IsPlayerInLocationRect)
                {
                    // Player in location rectangle, night
                    switch (climate)
                    {
                        case (int)MapsFile.Climates.Desert:
                        case (int)MapsFile.Climates.Desert2:
                            encounterTableIndex = 0;
                            break;
                        case (int)MapsFile.Climates.Mountain:
                            encounterTableIndex = 3;
                            break;
                        case (int)MapsFile.Climates.Rainforest:
                            encounterTableIndex = 6;
                            break;
                        case (int)MapsFile.Climates.Subtropical:
                            encounterTableIndex = 9;
                            break;
                        case (int)MapsFile.Climates.Swamp:
                        case (int)MapsFile.Climates.MountainWoods:
                        case (int)MapsFile.Climates.Woodlands:
                            encounterTableIndex = 12;
                            break;
                        case (int)MapsFile.Climates.HauntedWoodlands:
                            encounterTableIndex = 15;
                            break;

                        default:
                            encounterTableIndex = 26;
                            break;
                    }
                }
                else
                {
                    if (isDay)
                    {
                        // Player not in location rectangle, day
                        switch (climate)
                        {
                            case (int)MapsFile.Climates.Desert:
                            case (int)MapsFile.Climates.Desert2:
                                encounterTableIndex = 1;
                                break;
                            case (int)MapsFile.Climates.Mountain:
                                encounterTableIndex = 4;
                                break;
                            case (int)MapsFile.Climates.Rainforest:
                                encounterTableIndex = 7;
                                break;
                            case (int)MapsFile.Climates.Subtropical:
                                encounterTableIndex = 10;
                                break;
                            case (int)MapsFile.Climates.Swamp:
                            case (int)MapsFile.Climates.MountainWoods:
                            case (int)MapsFile.Climates.Woodlands:
                                encounterTableIndex = 13;
                                break;
                            case (int)MapsFile.Climates.HauntedWoodlands:
                                encounterTableIndex = 16;
                                break;

                            default:
                                encounterTableIndex = 26;
                                break;
                        }
                    }
                    else
                    {
                        // Player not in location rectangle, night
                        switch (climate)
                        {
                            case (int)MapsFile.Climates.Desert:
                            case (int)MapsFile.Climates.Desert2:
                                encounterTableIndex = 2;
                                break;
                            case (int)MapsFile.Climates.Mountain:
                                encounterTableIndex = 5;
                                break;
                            case (int)MapsFile.Climates.Rainforest:
                                encounterTableIndex = 8;
                                break;
                            case (int)MapsFile.Climates.Subtropical:
                                encounterTableIndex = 11;
                                break;
                            case (int)MapsFile.Climates.Swamp:
                            case (int)MapsFile.Climates.MountainWoods:
                            case (int)MapsFile.Climates.Woodlands:
                                encounterTableIndex = 14;
                                break;
                            case (int)MapsFile.Climates.HauntedWoodlands:
                                encounterTableIndex = 17;
                                break;

                            default:
                                encounterTableIndex = 26;
                                break;
                        }
                    }
                }
            }

            UnityEngine.Debug.Log("FAST TRAVEL ENCOUNTERS - TABLE INDEX IS " + encounterTableIndex.ToString());

            EncounterTable encounterTable = EncounterTables[encounterTableIndex];

            //pick a group
            EncounterGroup encounterGroup = encounterTable.Groups[UnityEngine.Random.Range(0,encounterTable.Groups.Length)];

            //build encounter
            //add a minion
            //every 2nd minion, add an elite if any
            //every 2nd elite, add a boss if any
            float current = 0;
            int minions = 0;
            int elites = 0;
            int bosses = 0;

            int[] minionsArray = encounterGroup.minions;
            int[] elitesArray = encounterGroup.elites;
            int[] bossesArray = encounterGroup.bosses;

            if (FastTravelEncounters.hasDEX)
            {
                UnityEngine.Debug.Log("FAST TRAVEL ENCOUNTERS - DEX DETECTED, SUBSTITUTING ARRAYS");
                if (encounterGroup.minionsDEX != null)
                    minionsArray = encounterGroup.minionsDEX;

                if (encounterGroup.elitesDEX != null)
                    elitesArray = encounterGroup.elitesDEX;

                if (encounterGroup.bossesDEX != null)
                    bossesArray = encounterGroup.bossesDEX;
            }

            bool BossFirst = false;
            bool EliteFirst = false;
            if (!atSea)
            {
                if (bossesArray != null && bossesArray.Length > 0 && count >= encounterGroup.cost * 3 && UnityEngine.Random.value > 0.5f)
                {
                    DaggerfallUI.Instance.PopupMessage("A sense of looming dread overwhelms you...");
                    BossFirst = true;
                }
                else if (elitesArray != null && elitesArray.Length > 0 && count >= encounterGroup.cost * 2 && UnityEngine.Random.value > 0.5f)
                {
                    DaggerfallUI.Instance.PopupMessage("You feel something akin to foreboding...");
                    EliteFirst = true;
                }
            }

            if (BossFirst)
            {
                while (current < count)
                {
                    if (current + (encounterGroup.cost * 3) <= count && bossesArray != null && bossesArray.Length > 0)
                    {
                        //add a boss
                        encounterMobiles.Add((MobileTypes)bossesArray[UnityEngine.Random.Range(0, bossesArray.Length)]);
                        bosses++;
                        elites = 0;
                        minions = 0;

                        current += encounterGroup.cost * 3;
                    }
                    else
                    {
                        //add a minion
                        encounterMobiles.Add((MobileTypes)minionsArray[UnityEngine.Random.Range(0, minionsArray.Length)]);
                        minions++;

                        current += encounterGroup.cost;
                    }
                }
            }
            else if (EliteFirst)
            {
                while (current < count)
                {
                    if (current + (encounterGroup.cost * 2) <= count && elitesArray != null && elitesArray.Length > 0)
                    {
                        //add an elite
                        encounterMobiles.Add((MobileTypes)elitesArray[UnityEngine.Random.Range(0, elitesArray.Length)]);
                        elites++;
                        minions = 0;

                        current += encounterGroup.cost * 2;
                    }
                    else
                    {
                        //add a minion
                        encounterMobiles.Add((MobileTypes)minionsArray[UnityEngine.Random.Range(0, minionsArray.Length)]);
                        minions++;

                        current += encounterGroup.cost;
                    }
                }
            }
            else
            {
                while (current < count)
                {
                    if ((elites == 2 || minions == 4) && bossesArray != null && bossesArray.Length > 0)
                    {
                        //add a boss
                        encounterMobiles.Add((MobileTypes)bossesArray[UnityEngine.Random.Range(0, bossesArray.Length)]);
                        bosses++;
                        elites = 0;
                        minions = 0;

                        current += encounterGroup.cost * 3;
                    }
                    else if (minions == 2 && elitesArray != null && elitesArray.Length > 0)
                    {
                        //add an elite
                        encounterMobiles.Add((MobileTypes)elitesArray[UnityEngine.Random.Range(0, elitesArray.Length)]);
                        elites++;
                        minions = 0;

                        current += encounterGroup.cost * 2;
                    }
                    else
                    {
                        //add a minion
                        encounterMobiles.Add((MobileTypes)minionsArray[UnityEngine.Random.Range(0, minionsArray.Length)]);
                        minions++;

                        current += encounterGroup.cost;
                    }
                }
            }

            return encounterMobiles.ToArray();
        }
    }
    #endregion
}

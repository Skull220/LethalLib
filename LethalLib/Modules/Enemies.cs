﻿using HarmonyLib;
using LethalLib;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using static LethalLib.Modules.Items;

namespace LethalLib.Modules
{
    public class Enemies
    {
        public static void Init()
        {
            On.StartOfRound.Awake += RegisterLevelEnemies;
            On.Terminal.Start += Terminal_Start;

        }

        private static void Terminal_Start(On.Terminal.orig_Start orig, Terminal self)
        {
            var infoKeyword = self.terminalNodes.allKeywords.First(keyword => keyword.word == "info");
            foreach (SpawnableEnemy spawnableEnemy in spawnableEnemies)
            {
                if (spawnableEnemy.terminalNode != null)
                {
                    var keyword = TerminalUtils.CreateTerminalKeyword(spawnableEnemy.enemy.enemyName.ToLowerInvariant().Replace(" ", "-"), defaultVerb: infoKeyword);
                    var allKeywords = self.terminalNodes.allKeywords.ToList();
                    allKeywords.Add(keyword);
                    self.terminalNodes.allKeywords = allKeywords.ToArray();

                    var itemInfoNouns = infoKeyword.compatibleNouns.ToList();
                    itemInfoNouns.Add(new CompatibleNoun()
                    {
                        noun = keyword,
                        result = spawnableEnemy.terminalNode
                    });
                    infoKeyword.compatibleNouns = itemInfoNouns.ToArray();
                }
            }
            orig(self);
        }

        private static void RegisterLevelEnemies(On.StartOfRound.orig_Awake orig, StartOfRound self)
        {
            orig(self);

            foreach (SelectableLevel level in self.levels)
            {
                var name = level.name;

                if (Enum.IsDefined(typeof(Levels.LevelTypes), name))
                {
                    var levelEnum = (Levels.LevelTypes)Enum.Parse(typeof(Levels.LevelTypes), name);
                    foreach (SpawnableEnemy spawnableEnemy in spawnableEnemies)
                    {
                        if (spawnableEnemy.spawnLevels.HasFlag(levelEnum))
                        {
                            var spawnableEnemyWithRarity = new SpawnableEnemyWithRarity()
                            {
                                enemyType = spawnableEnemy.enemy,
                                rarity = spawnableEnemy.rarity
                            };

                            // make sure spawnableScrap does not already contain item
                            //Plugin.logger.LogInfo($"Checking if {spawnableEnemy.enemy.name} is already in {name}");

                            /*
                            if (!level.spawnableEnemies.Any(x => x.spawnableEnemy == spawnableEnemy.enemy))
                            {
                                level.spawnableEnemies.Add(spawnableEnemyWithRarity);
                                Logger.LogInfo($"Added {spawnableEnemy.enemy.name} to {name}");
                            }*/

                            switch (spawnableEnemy.spawnType)
                            {
                                case SpawnType.Default:
                                    if (!level.Enemies.Any(x => x.enemyType == spawnableEnemy.enemy))
                                    {
                                        level.Enemies.Add(spawnableEnemyWithRarity);
                                        Plugin.logger.LogInfo($"Added {spawnableEnemy.enemy.name} to {name} with SpawnType [Default]");
                                    }
                                    break;
                                case SpawnType.Daytime:
                                    if (!level.DaytimeEnemies.Any(x => x.enemyType == spawnableEnemy.enemy))
                                    {
                                        level.DaytimeEnemies.Add(spawnableEnemyWithRarity);
                                        Plugin.logger.LogInfo($"Added {spawnableEnemy.enemy.name} to {name} with SpawnType [Daytime]");
                                    }
                                    break;
                                case SpawnType.Outside:
                                    if (!level.OutsideEnemies.Any(x => x.enemyType == spawnableEnemy.enemy))
                                    {
                                        level.OutsideEnemies.Add(spawnableEnemyWithRarity);
                                        Plugin.logger.LogInfo($"Added {spawnableEnemy.enemy.name} to {name} with SpawnType [Outside]");
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            }
        }

        public enum SpawnType
        {
            Default,
            Daytime,
            Outside
        }

        public class SpawnableEnemy
        {
            public EnemyType enemy;
            public int rarity;
            public Levels.LevelTypes spawnLevels;
            public SpawnType spawnType;
            public TerminalNode terminalNode;
            public string modName;


            public SpawnableEnemy(EnemyType enemy, int rarity, Levels.LevelTypes spawnLevels, SpawnType spawnType)
            {
                this.enemy = enemy;
                this.rarity = rarity;
                this.spawnLevels = spawnLevels;
                this.spawnType = spawnType;
            }
        }

        public static List<SpawnableEnemy> spawnableEnemies = new List<SpawnableEnemy>();

        public static void RegisterEnemy(EnemyType enemy, int rarity, Levels.LevelTypes levelFlags, SpawnType spawnType, TerminalNode infoNode = null)
        {
            var spawnableEnemy = new SpawnableEnemy(enemy, rarity, levelFlags, spawnType);

            spawnableEnemy.terminalNode = infoNode;

            var callingAssembly = Assembly.GetCallingAssembly();
            var modDLL = callingAssembly.GetName().Name;
            spawnableEnemy.modName = modDLL;

            spawnableEnemies.Add(spawnableEnemy);
        }

        public static void RegisterEnemy(EnemyType enemy, int rarity, Levels.LevelTypes levelFlags, TerminalNode infoNode = null)
        {
            var spawnableEnemy = new SpawnableEnemy(enemy, rarity, levelFlags, enemy.isDaytimeEnemy ? SpawnType.Daytime : enemy.isOutsideEnemy ? SpawnType.Outside : SpawnType.Default);

            spawnableEnemy.terminalNode = infoNode;

            var callingAssembly = Assembly.GetCallingAssembly();
            var modDLL = callingAssembly.GetName().Name;
            spawnableEnemy.modName = modDLL;

            spawnableEnemies.Add(spawnableEnemy);
        }

    }
}

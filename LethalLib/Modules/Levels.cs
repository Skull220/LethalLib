using HarmonyLib;
using MOON_API;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LethalLib.Modules
{
    public class Levels {
        [Flags]
        public enum LevelTypes {
            None = 1 << 0,
            ExperimentationLevel = 1 << 1,
            AssuranceLevel = 1 << 2,
            VowLevel = 1 << 3,
            OffenseLevel = 1 << 4,
            MarchLevel = 1 << 5,
            RendLevel = 1 << 6,
            DineLevel = 1 << 7,
            TitanLevel = 1 << 8,
            All = ExperimentationLevel | AssuranceLevel | VowLevel | OffenseLevel | MarchLevel | RendLevel | DineLevel | TitanLevel
        }

        /* This class is called levels so I'm putting all the custom level code here.
         * If I need to move it to a seperate class let me know -Skull
         */
        public class CustomLevel {
            public TerminalKeyword LevelKeyword;
            public TerminalNode TerminalRoute;
            public SelectableLevel NewLevel;
            public TerminalNode LevelTerminalInfo;         
            public GameObject LevelPrefab;
            public static int MoonID = 8;
            private static string MoonFriendlyName;

            private static List<string> ObjectsToDestroy = new List<string> {
                "CompletedVowTerrain",
                "tree",
                "Tree",
                "Rock",
                "StaticLightingSky",
                "ForestAmbience",
                "Local Volumetric Fog",
                "GroundFog",
                "Sky and Fog Global Volume",
                "SunTexture"
            };

            public static void AddObjectToDestroyList(string NewObjectName) {
                ObjectsToDestroy.Add(NewObjectName);
            }

            public List<string> GetDestroyList() { return ObjectsToDestroy; }

            public CustomLevel(SelectableLevel newSelectableLevel, TerminalKeyword newTerminalAsset,
                    TerminalNode NewRoute, TerminalNode newTerminalInfo, GameObject newLevelPrefab) {
                MoonID = MoonID++;
                NewLevel = newSelectableLevel;
                NewLevel.levelID = MoonID;
                LevelKeyword = newTerminalAsset;
                TerminalRoute = NewRoute;
                NewRoute.buyRerouteToMoon = MoonID;
                NewRoute.terminalOptions[1].result.buyRerouteToMoon = MoonID;
                LevelTerminalInfo = newTerminalInfo;
                LevelPrefab = newLevelPrefab;
                MoonFriendlyName = NewLevel.PlanetName;

                CustomLevelList.Add(MoonFriendlyName, this);
            }
        }

        private static Dictionary<string, CustomLevel> CustomLevelList;
        
        private static void AddMoonToMoonsList(CustomLevel Moon, StartOfRound __instance) {
            SelectableLevel MyNewMoon = Moon.NewLevel;  
            {
                /* TODO: these assignments should only be made if there's no entry in any of these arrays already.
                 * Hopefully, the end user would be able to put their custom monsters/scrap/whatever into the 
                 * SelectableLevel class they made in unity or set it before this point or something.
                 * I want to make these default values, but the problem with that is we can't retrieve the instance without it existing.
                 */
                MyNewMoon.planetPrefab = __instance.levels[2].planetPrefab;
                MyNewMoon.spawnableMapObjects = __instance.levels[2].spawnableMapObjects;
                MyNewMoon.spawnableOutsideObjects = __instance.levels[2].spawnableOutsideObjects;
                MyNewMoon.spawnableScrap = __instance.levels[2].spawnableScrap;
                MyNewMoon.Enemies = __instance.levels[5].Enemies;
                MyNewMoon.levelAmbienceClips = __instance.levels[2].levelAmbienceClips;
                MyNewMoon.OutsideEnemies = __instance.levels[0].OutsideEnemies;
                MyNewMoon.DaytimeEnemies = __instance.levels[0].DaytimeEnemies;
            }
            //Core.AddMoon(MyNewMoon); Hopefully this is unnecessary now
        }

        //Defining the custom moon for the API
        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        [HarmonyPrefix]
        private static bool AddMoonsToList(StartOfRound __instance) {
            foreach (CustomLevel NextCustomLevel in CustomLevelList.Values) {
                AddMoonToMoonsList(NextCustomLevel, __instance);
            }
            return true;
        }

        //Add the custom moon to the terminal
        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        [HarmonyPostfix]
        private static void AddMoonsToTerminal(StartOfRound __instance) {
            foreach (CustomLevel NextCustomLevel in CustomLevelList.Values) {
                TerminalUtils.GrabTerminal(__instance);
                TerminalUtils.AddMoonTerminalEntry(NextCustomLevel.LevelKeyword, NextCustomLevel.NewLevel);
                TerminalUtils.AddRouteNode(NextCustomLevel.LevelKeyword, NextCustomLevel.TerminalRoute);
                TerminalUtils.AddMoonInfo(NextCustomLevel.LevelKeyword, NextCustomLevel.LevelTerminalInfo);
            }  
        }

        private static void CreateMoonOnNav(StartOfRound __instance) {
            if (!CustomLevelList.ContainsKey(__instance.currentLevel.PlanetName)) {
                return;
            }
            CustomLevel LevelToLoad = CustomLevelList.GetValueSafe<string, CustomLevel>(__instance.currentLevel.PlanetName);
            Debug.Log(" LethalLib Moon Tools: Loading into level " + __instance.currentLevel.PlanetName);

            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
            foreach (GameObject ObjToDestroy in allObjects) {
                if (ObjToDestroy.name.Contains("Models2VowFactory")) {
                    ObjToDestroy.SetActive(false);
                }

                //If the object's named Plane and its parent is Foliage, it's also gotta go. This gets rid of the grass
                if (ObjToDestroy.name.Contains("Plane") && (ObjToDestroy.transform.parent.gameObject.name.Contains("Foliage") || ObjToDestroy.transform.parent.gameObject.name.Contains("Mounds"))) {
                    GameObject.Destroy(ObjToDestroy);
                }
                foreach (string UnwantedObjString in LevelToLoad.GetDestroyList()) {
                    //If the object has any of the names in the list, it's gotta go
                    if (ObjToDestroy.name.Contains(UnwantedObjString)) {
                        GameObject.Destroy(ObjToDestroy);
                        continue;
                    }
                }
            }
            //Load our custom prefab
            GameObject MyLevelAsset = LevelToLoad.LevelPrefab as GameObject;
            GameObject.Instantiate(MyLevelAsset);

        }

        //Destroy the necessary actors and set our scene
        [HarmonyPatch(typeof(StartOfRound), "SceneManager_OnLoadComplete1")]
        [HarmonyPostfix]
        private static void CustomLevelInit(StartOfRound __instance) {
            CreateMoonOnNav(__instance);
        }       
    }
}

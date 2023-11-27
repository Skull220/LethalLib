using HarmonyLib;
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

        private static Dictionary<string, CustomLevel> CustomLevelList;

        /* This class is called levels so I'm putting all the custom level code here.
         * If I need to move it to a seperate class let me know -Skull
         */
        public class CustomLevel { 
            public SelectableLevel NewLevel;
            public TerminalKeyword TerminalAsset;
            public TerminalNode TerminalRouteConfirmation;
            public TerminalNode TerminalInfo;
            GameObject LevelPrefab;

            public SelectableLevel GetLevel() { return NewLevel; }
            public void SetLevel(SelectableLevel newSelectableLevel) { NewLevel = newSelectableLevel; }
            
            public TerminalKeyword GetTerminalName() { return TerminalAsset; }
            public void SetTerminalName(TerminalKeyword NewName) { TerminalAsset = NewName; }
            
            public TerminalNode GetTerminalRoute() { return TerminalRouteConfirmation; }
            public void SetTerminalRoute(TerminalNode newRoute) { TerminalRouteConfirmation = newRoute; }

            public TerminalNode GetTerminalInfo() { return TerminalInfo; }
            public void SetTerminalInfo(TerminalNode newInfo) { TerminalRouteConfirmation = newInfo; }

            //No setter for this one since this should be defined in and agree with what's in the unity asset 
            public string GetLevelName() { return NewLevel.PlanetName; }

            public GameObject GetLevelObject() { return LevelPrefab; }
            public void SetLevelObject(GameObject newPrefab) {  LevelPrefab = newPrefab; }

            public CustomLevel(SelectableLevel newSelectableLevel, TerminalKeyword newTerminalAsset,
                    TerminalNode NewRouteConfirmation, TerminalNode newTerminalInfo, GameObject newLevelPrefab) {
                NewLevel = newSelectableLevel;
                TerminalAsset = newTerminalAsset;
                TerminalRouteConfirmation = NewRouteConfirmation;
                TerminalInfo = newTerminalInfo;
                LevelPrefab = newLevelPrefab;
                CustomLevelList.Add(NewLevel.PlanetName, this);
            }
        }

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

        //This is just a fix for an error I get sometimes from Ooblterra. Probably not necessary once I figure that out
        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        [HarmonyPrefix]
        private static bool AddMoonsToList(StartOfRound __instance) {
            //Create new moon based on vow
            foreach (CustomLevel moon in CustomLevelList.Values) {
                moon.GetLevel().spawnableScrap = __instance.levels[2].spawnableScrap;
            }
            return true;
        }

        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        [HarmonyPostfix]
        [HarmonyPriority(30)]
        private static void AddMoonsToTerminal(StartOfRound __instance) {
            foreach (CustomLevel Moon in CustomLevelList.Values) {
                TerminalUtils.AddMoonTerminalEntry(Moon.GetTerminalName(), Moon.GetLevel());
                TerminalUtils.AddMoonConfirmation(Moon.GetTerminalName(), Moon.GetTerminalRoute());
                TerminalUtils.AddMoonInfo(Moon.GetTerminalName(), Moon.GetTerminalInfo());
            }

        }

        [HarmonyPatch(typeof(StartOfRound), "SceneManager_OnLoadComplete1")]
        [HarmonyPostfix]
        [HarmonyPriority(0)]
        private static void CustomLevelInit(StartOfRound __instance) {
            List<string> levelnames = new List<string>();
            string TargetLevelName = __instance.currentLevel.PlanetName;
            if (!CustomLevelList.Keys.ToArray().Contains(TargetLevelName)) {
                return;
            }

            Debug.Log("LethalLib: Loading into level " + TargetLevelName);

            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();

            foreach (GameObject ObjToDestroy in allObjects) {
                foreach (string UnwantedObjString in ObjectsToDestroy) {
                    //If the object has any of the names in the list, it's gotta go
                    if (ObjToDestroy.name.Contains(UnwantedObjString)) {
                        GameObject.Destroy(ObjToDestroy);
                    }
                }
                //If the object's named Plane and its parent is Foliage, it's also gotta go. This gets rid of the grass
                if (ObjToDestroy.name.Contains("Plane") && ObjToDestroy.transform.parent.gameObject.name.Contains("Foliage")) {
                    GameObject.Destroy(ObjToDestroy);
                }
            }
            //Load our custom prefab
            GameObject MyLevelAsset = CustomLevelList[TargetLevelName].GetLevelObject() as GameObject;
            GameObject MyInstantiatedLevel = GameObject.Instantiate(MyLevelAsset);
        }
    }
}

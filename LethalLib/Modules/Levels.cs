using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace LethalLib.Modules
{
    public class Levels
    {
        [Flags]
        public enum LevelTypes
        {
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

        public static IDictionary<string, SelectableLevel> CustomMoons = new Dictionary<string, SelectableLevel>();

        //Given a custom moon, add it to a list of custom moons we can reference later
        private static void AddMoon(SelectableLevel MoonToAdd) {
            CustomMoons[MoonToAdd.name] = MoonToAdd; 
        }

        public class customlevel {

            private static SelectableLevel NewMoon;

            [HarmonyPatch(typeof(StartOfRound), "Awake")]
            [HarmonyPrefix]
            private static bool AddMoonToList(StartOfRound __instance) {
                /* Currently our custom moon just inherits almost everything from vow.
                 * As a consequence, trying to fly to vow will also fly to our custom
                 * moon, and we can only have one custom moon at a time.
                 * TODO: Make this not the case lol
                 */
                SelectableLevel NewMoon = __instance.GetComponent<StartOfRound>().levels[2];
                return true;
            }
                //Build our custom moon's variables
                public customlevel(string MoonName, string MoonDescription, string RiskLevel, float TravelTime) {
                NewMoon.PlanetName = MoonName;
                NewMoon.name = MoonName;
                NewMoon.LevelDescription = MoonDescription;
                NewMoon.riskLevel = RiskLevel;
                NewMoon.timeToArrive = TravelTime;
                AddMoon(NewMoon);
            }

        }
    }
}

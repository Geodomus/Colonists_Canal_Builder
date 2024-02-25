using HarmonyLib;
using UnityEngine;
namespace Canal_Builder
{
    public class CanalBuilder
    {
        [StaticConstructorOnStartup]
        public static int Init()
        {
            Harmony.DEBUG = true;

            var harmony = new Harmony("CanalBuilder");

            FileLog.Log("testing harmony logging");

            harmony.PatchAll();
            SerialiseFactory.AddCustom(typeof(EntityDataComponentInfo), 201,
                typeof(BuildingProductionCanalBuilderDataInfo));
            SerialiseFactory.AddCustom(typeof(EntityDataComponent), 201,
                typeof(BuildingProductionCanalBuilderData));

            Debug.Log("#CanalBuilder# Init Complete!");

            return 2;
        }
    }
}
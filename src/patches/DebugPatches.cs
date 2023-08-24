using FistVR;
using HarmonyLib;
using System.Reflection;

namespace EFM
{
    public class DebugPatches
    {
        public static void DoPatching(Harmony harmony)
        {
            // IsPointInsideSphereGeoPatch
            MethodInfo isPointInsideSphereGeoPatchOriginal = typeof(FVRQuickBeltSlot).GetMethod("IsPointInsideSphereGeo", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo isPointInsideSphereGeoPatchPrefix = typeof(IsPointInsideSphereGeoPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(isPointInsideSphereGeoPatchOriginal, new HarmonyMethod(isPointInsideSphereGeoPatchPrefix));

            //// UpdateModeTwoAxisPatch
            //MethodInfo updateModeTwoAxisPatchOriginal = typeof(FVRMovementManager).GetMethod("UpdateModeTwoAxis", BindingFlags.NonPublic | BindingFlags.Instance);
            //MethodInfo updateModeTwoAxisPatchPrefix = typeof(UpdateModeTwoAxisPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //harmony.Patch(updateModeTwoAxisPatchOriginal, new HarmonyMethod(updateModeTwoAxisPatchPrefix));

            //// SetActivePatch
            //MethodInfo setActivePatchOriginal = typeof(UnityEngine.GameObject).GetMethod("SetActive", BindingFlags.Public | BindingFlags.Instance);
            //MethodInfo setActivePatchPrefix = typeof(SetActivePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //harmony.Patch(setActivePatchOriginal, new HarmonyMethod(setActivePatchPrefix));

            //// DestroyPatch
            //MethodInfo destroyPatchOriginal = typeof(UnityEngine.Object).GetMethod("Destroy", BindingFlags.Public | BindingFlags.Static, null, CallingConventions.Any, new Type[] { typeof(UnityEngine.Object) }, null);
            //MethodInfo destroyPatchPrefix = typeof(DestroyPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //harmony.Patch(destroyPatchOriginal, new HarmonyMethod(destroyPatchPrefix));

            //// SetParentagePatch
            //MethodInfo setParentagePatchOriginal = typeof(FVRPhysicalObject).GetMethod("SetParentage", BindingFlags.Public | BindingFlags.Instance);
            //MethodInfo setParentagePatchPrefix = typeof(SetParentagePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //harmony.Patch(setParentagePatchOriginal, new HarmonyMethod(setParentagePatchPrefix));

            //// DeadBoltPatch
            //MethodInfo deadBoltPatchOriginal = typeof(SideHingedDestructibleDoorDeadBolt).GetMethod("TurnBolt", BindingFlags.Public | BindingFlags.Instance);
            //MethodInfo deadBoltPatchPrefix = typeof(DeadBoltPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //harmony.Patch(deadBoltPatchOriginal, new HarmonyMethod(deadBoltPatchPrefix));

            //// DeadBoltLastHandPatch
            //MethodInfo deadBoltLastHandPatchOriginal = typeof(SideHingedDestructibleDoorDeadBolt).GetMethod("SetStartingLastHandForward", BindingFlags.Public | BindingFlags.Instance);
            //MethodInfo deadBoltLastHandPatchPrefix = typeof(DeadBoltLastHandPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //harmony.Patch(deadBoltLastHandPatchOriginal, new HarmonyMethod(deadBoltLastHandPatchPrefix));

            // DequeueAndPlayDebugPatch
            MethodInfo dequeueAndPlayDebugPatchOriginal = typeof(SM.AudioSourcePool).GetMethod("DequeueAndPlay", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo dequeueAndPlayDebugPatchPrefix = typeof(DequeueAndPlayDebugPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(dequeueAndPlayDebugPatchOriginal, new HarmonyMethod(dequeueAndPlayDebugPatchPrefix));

            //// EventSystemUpdateDebugPatch
            //MethodInfo eventSystemUpdateDebugPatchOriginal = typeof(EventSystem).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            //MethodInfo eventSystemUpdateDebugPatchPrefix = typeof(EventSystemUpdateDebugPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //harmony.Patch(eventSystemUpdateDebugPatchOriginal, new HarmonyMethod(eventSystemUpdateDebugPatchPrefix));

            //// inputModuleProcessDebugPatch
            //MethodInfo inputModuleProcessDebugPatchOriginal = typeof(StandaloneInputModule).GetMethod("Process", BindingFlags.Public | BindingFlags.Instance);
            //MethodInfo inputModuleProcessDebugPatchPrefix = typeof(inputModuleProcessDebugPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //harmony.Patch(inputModuleProcessDebugPatchOriginal, new HarmonyMethod(inputModuleProcessDebugPatchPrefix));

            //// InteractiveGlobalUpdateDebugPatch
            //MethodInfo interactiveGlobalUpdateDebugPatchOriginal = typeof(FVRInteractiveObject).GetMethod("GlobalUpdate", BindingFlags.Public | BindingFlags.Static);
            //MethodInfo interactiveGlobalUpdateDebugPatchPrefix = typeof(InteractiveGlobalUpdateDebugPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //harmony.Patch(interactiveGlobalUpdateDebugPatchOriginal, new HarmonyMethod(interactiveGlobalUpdateDebugPatchPrefix));

            //// PlayClipDebugPatch
            //MethodInfo playClipDebugPatchOriginal = typeof(AudioSourcePool).GetMethod("PlayClip", BindingFlags.Public | BindingFlags.Instance);
            //MethodInfo playClipDebugDebugPatchPrefix = typeof(PlayClipDebugPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //harmony.Patch(playClipDebugPatchOriginal, new HarmonyMethod(playClipDebugDebugPatchPrefix));

            //// InstantiateAndEnqueueDebugPatch
            //MethodInfo instantiateAndEnqueueDebugPatchOriginal = typeof(AudioSourcePool).GetMethod("InstantiateAndEnqueue", BindingFlags.Public | BindingFlags.Instance);
            //MethodInfo instantiateAndEnqueueDebugPatchPrefix = typeof(InstantiateAndEnqueueDebugPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //harmony.Patch(instantiateAndEnqueueDebugPatchOriginal, new HarmonyMethod(instantiateAndEnqueueDebugPatchPrefix));

            //// ChamberFireDebugPatch
            //MethodInfo chamberFireDebugPatchOriginal = typeof(FVRFireArmChamber).GetMethod("Fire", BindingFlags.Public | BindingFlags.Instance);
            //MethodInfo chamberFireDebugPatchPrefix = typeof(ChamberFireDebugPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //harmony.Patch(chamberFireDebugPatchOriginal, new HarmonyMethod(chamberFireDebugPatchPrefix));
        }
    }
}

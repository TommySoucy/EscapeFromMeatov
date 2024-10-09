using FistVR;
using HarmonyLib;
using ModularWorkshop;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace EFM
{
    public class GamePatches
    {
        public static void DoPatching(Harmony harmony)
        {
            // MovementManagerPatch
            MethodInfo movementManagerTwinstickOriginal = typeof(FVRMovementManager).GetMethod("HandUpdateTwinstick", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo movementManagerTwinstickTranspiler = typeof(MovementManagerPatch).GetMethod("TwinstickTranspiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo movementManagerTwoAxisOriginal = typeof(FVRMovementManager).GetMethod("HandUpdateTwoAxis", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo movementManagerTwoAxisTranspiler = typeof(MovementManagerPatch).GetMethod("TwoAxisTranspiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo movementManagerUpdateOriginal = typeof(FVRMovementManager).GetMethod("UpdateSmoothLocomotion", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo movementManagerUpdateTranspiler = typeof(MovementManagerPatch).GetMethod("UpdateTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(movementManagerTwinstickOriginal, harmony, true);
            PatchController.Verify(movementManagerTwoAxisOriginal, harmony, true);
            PatchController.Verify(movementManagerUpdateOriginal, harmony, true);
            harmony.Patch(movementManagerTwinstickOriginal, null, null, new HarmonyMethod(movementManagerTwinstickTranspiler));
            harmony.Patch(movementManagerTwoAxisOriginal, null, null, new HarmonyMethod(movementManagerTwoAxisTranspiler));
            harmony.Patch(movementManagerUpdateOriginal, null, null, new HarmonyMethod(movementManagerUpdateTranspiler));

            // AttachmentEndInteractionPatch
            MethodInfo attachmentEndInteractionOriginal = typeof(FVRFireArmAttachment).GetMethod("EndInteraction", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo attachmentEndInteractionPostfix = typeof(AttachmentEndInteractionPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(attachmentEndInteractionOriginal, harmony, true);
            harmony.Patch(attachmentEndInteractionOriginal, null, new HarmonyMethod(attachmentEndInteractionPostfix));

            // ConfigureQuickbeltPatch
            MethodInfo configureQuickbeltPatchOriginal = typeof(FVRPlayerBody).GetMethod("ConfigureQuickbelt", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo configureQuickbeltPatchPrefix = typeof(ConfigureQuickbeltPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo configureQuickbeltPatchPostfix = typeof(ConfigureQuickbeltPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(configureQuickbeltPatchOriginal, harmony, true);
            harmony.Patch(configureQuickbeltPatchOriginal, new HarmonyMethod(configureQuickbeltPatchPrefix), new HarmonyMethod(configureQuickbeltPatchPostfix));

            // TestQuickbeltPatch
            MethodInfo testQuickbeltPatchOriginal = typeof(FVRViveHand).GetMethod("TestQuickBeltDistances", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo testQuickbeltPatchPrefix = typeof(TestQuickbeltPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(testQuickbeltPatchOriginal, harmony, true);
            harmony.Patch(testQuickbeltPatchOriginal, new HarmonyMethod(testQuickbeltPatchPrefix));

            // SetQuickBeltSlotPatch
            MethodInfo setQuickBeltSlotPatchOriginal = typeof(FVRPhysicalObject).GetMethod("SetQuickBeltSlot", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo setQuickBeltSlotPatchPrefix = typeof(SetQuickBeltSlotPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo setQuickBeltSlotPatchPostfix = typeof(SetQuickBeltSlotPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(setQuickBeltSlotPatchOriginal, harmony, true);
            harmony.Patch(setQuickBeltSlotPatchOriginal, new HarmonyMethod(setQuickBeltSlotPatchPrefix), new HarmonyMethod(setQuickBeltSlotPatchPostfix));

            // BeginInteractionPatch
            MethodInfo beginInteractionPatchOriginal = typeof(FVRPhysicalObject).GetMethod("BeginInteraction", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo beginInteractionPatchPrefix = typeof(BeginInteractionPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(beginInteractionPatchOriginal, harmony, false);
            harmony.Patch(beginInteractionPatchOriginal, new HarmonyMethod(beginInteractionPatchPrefix));

            // DamagePatch
            MethodInfo damagePatchOriginal = typeof(FVRPlayerHitbox).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(Damage) }, null);
            MethodInfo damagePatchPrefix = typeof(DamagePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(damagePatchOriginal, harmony, true);
            harmony.Patch(damagePatchOriginal, new HarmonyMethod(damagePatchPrefix));

            // DamageFloatPatch
            MethodInfo damageFloatPatchOriginal = typeof(FVRPlayerHitbox).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(float) }, null);
            MethodInfo damageFloatPatchPrefix = typeof(DamageFloatPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(damageFloatPatchOriginal, harmony, true);
            harmony.Patch(damageFloatPatchOriginal, new HarmonyMethod(damageFloatPatchPrefix));

            // DamageDealtPatch
            MethodInfo damageDealtPatchOriginal = typeof(FVRPlayerHitbox).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(DamageDealt) }, null);
            MethodInfo damageDealtPatchPrefix = typeof(DamageDealtPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(damageDealtPatchOriginal, harmony, true);
            harmony.Patch(damageDealtPatchOriginal, new HarmonyMethod(damageDealtPatchPrefix));

            // HandTestColliderPatch
            MethodInfo handTestColliderPatchOriginal = typeof(FVRViveHand).GetMethod("TestCollider", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo handTestColliderPatchPrefix = typeof(HandTestColliderPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(handTestColliderPatchOriginal, harmony, true);
            harmony.Patch(handTestColliderPatchOriginal, new HarmonyMethod(handTestColliderPatchPrefix));

            // HandTriggerExitPatch
            MethodInfo handTriggerExitPatchOriginal = typeof(FVRViveHand).GetMethod("HandTriggerExit", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo handTriggerExitPatchPrefix = typeof(HandTriggerExitPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(handTriggerExitPatchOriginal, harmony, true);
            harmony.Patch(handTriggerExitPatchOriginal, new HarmonyMethod(handTriggerExitPatchPrefix));

            // InteractiveSetAllCollidersToLayerPatch
            MethodInfo interactiveSetAllCollidersToLayerOriginal = typeof(FVRInteractiveObject).GetMethod("SetAllCollidersToLayer", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo interactiveSetAllCollidersToLayerPrefix = typeof(InteractiveSetAllCollidersToLayerPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(interactiveSetAllCollidersToLayerOriginal, harmony, true);
            harmony.Patch(interactiveSetAllCollidersToLayerOriginal, new HarmonyMethod(interactiveSetAllCollidersToLayerPrefix));

            // HandUpdatePatch
            MethodInfo handUpdatePatchOriginal = typeof(FVRViveHand).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo handUpdatePatchPrefix = typeof(HandUpdatePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(handUpdatePatchOriginal, harmony, true);
            harmony.Patch(handUpdatePatchOriginal, new HarmonyMethod(handUpdatePatchPrefix));

            //// ClipUpdateInteractionPatch
            //MethodInfo clipUpdateInteractionPatchOriginal = typeof(FVRFireArmClip).GetMethod("UpdateInteraction", BindingFlags.Public | BindingFlags.Instance);
            //MethodInfo clipUpdateInteractionPatchPostfix = typeof(ClipUpdateInteractionPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);
            //MethodInfo clipUpdateInteractionPatchTranspiler = typeof(ClipUpdateInteractionPatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            //PatchController.Verify(clipUpdateInteractionPatchOriginal, harmony, true);
            //harmony.Patch(clipUpdateInteractionPatchOriginal, null, new HarmonyMethod(clipUpdateInteractionPatchPostfix), new HarmonyMethod(clipUpdateInteractionPatchTranspiler));

            // MovementManagerJumpPatch
            MethodInfo movementManagerJumpOriginal = typeof(FVRMovementManager).GetMethod("Jump", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo movementManagerJumpPrefix = typeof(MovementManagerJumpPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(movementManagerJumpOriginal, harmony, true);
            harmony.Patch(movementManagerJumpOriginal, new HarmonyMethod(movementManagerJumpPrefix));

            //// ChamberSetRoundPatch
            //MethodInfo chamberSetRoundPatchOriginal = typeof(FVRFireArmChamber).GetMethod("SetRound", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(FVRFireArmRound), typeof(bool) }, null);
            //MethodInfo chamberSetRoundPatchPrefix = typeof(ChamberSetRoundPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //PatchController.Verify(chamberSetRoundPatchOriginal, harmony, true);
            //harmony.Patch(chamberSetRoundPatchOriginal, new HarmonyMethod(chamberSetRoundPatchPrefix));

            //// ChamberSetRoundGivenPatch
            //MethodInfo chamberSetRoundGivenPatchOriginal = typeof(FVRFireArmChamber).GetMethod("SetRound", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(FVRFireArmRound), typeof(Vector3), typeof(Quaternion) }, null);
            //MethodInfo chamberSetRoundGivenPatchPrefix = typeof(ChamberSetRoundGivenPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //PatchController.Verify(chamberSetRoundGivenPatchOriginal, harmony, true);
            //harmony.Patch(chamberSetRoundGivenPatchOriginal, new HarmonyMethod(chamberSetRoundGivenPatchPrefix));

            //// ChamberSetRoundClassPatch
            //MethodInfo chamberSetRoundClassPatchOriginal = typeof(FVRFireArmChamber).GetMethod("SetRound", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(FireArmRoundClass), typeof(Vector3), typeof(Quaternion) }, null);
            //MethodInfo chamberSetRoundClassPatchPrefix = typeof(ChamberSetRoundClassPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //PatchController.Verify(chamberSetRoundClassPatchOriginal, harmony, true);
            //harmony.Patch(chamberSetRoundClassPatchOriginal, new HarmonyMethod(chamberSetRoundClassPatchPrefix));

            // MagRemoveRoundPatch
            MethodInfo magRemoveRoundPatchOriginal = typeof(FVRFireArmMagazine).GetMethod("RemoveRound", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { }, null);
            MethodInfo magRemoveRoundPatchPrefix = typeof(MagRemoveRoundPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(magRemoveRoundPatchOriginal, harmony, true);
            harmony.Patch(magRemoveRoundPatchOriginal, new HarmonyMethod(magRemoveRoundPatchPrefix));

            // MagRemoveRoundBoolPatch
            MethodInfo magRemoveRoundBoolPatchOriginal = typeof(FVRFireArmMagazine).GetMethod("RemoveRound", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(bool) }, null);
            MethodInfo magRemoveRoundBoolPatchPrefix = typeof(MagRemoveRoundBoolPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(magRemoveRoundBoolPatchOriginal, harmony, true);
            harmony.Patch(magRemoveRoundBoolPatchOriginal, new HarmonyMethod(magRemoveRoundBoolPatchPrefix));

            // MagRemoveRoundIntPatch
            MethodInfo magRemoveRoundIntPatchOriginal = typeof(FVRFireArmMagazine).GetMethod("RemoveRound", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(int) }, null);
            MethodInfo magRemoveRoundIntPatchPrefix = typeof(MagRemoveRoundIntPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(magRemoveRoundIntPatchOriginal, harmony, true);
            harmony.Patch(magRemoveRoundIntPatchOriginal, new HarmonyMethod(magRemoveRoundIntPatchPrefix));

            // ClipRemoveRoundPatch
            MethodInfo clipRemoveRoundPatchOriginal = typeof(FVRFireArmClip).GetMethod("RemoveRound", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { }, null);
            MethodInfo clipRemoveRoundPatchPrefix = typeof(ClipRemoveRoundPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(clipRemoveRoundPatchOriginal, harmony, true);
            harmony.Patch(clipRemoveRoundPatchOriginal, new HarmonyMethod(clipRemoveRoundPatchPrefix));

            // ClipRemoveRoundBoolPatch
            MethodInfo clipRemoveRoundBoolPatchOriginal = typeof(FVRFireArmClip).GetMethod("RemoveRound", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(bool) }, null);
            MethodInfo clipRemoveRoundBoolPatchPrefix = typeof(ClipRemoveRoundBoolPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(clipRemoveRoundBoolPatchOriginal, harmony, true);
            harmony.Patch(clipRemoveRoundBoolPatchOriginal, new HarmonyMethod(clipRemoveRoundBoolPatchPrefix));

            // ClipRemoveRoundClassPatch
            MethodInfo clipRemoveRoundClassPatchOriginal = typeof(FVRFireArmClip).GetMethod("RemoveRoundReturnClass", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo clipRemoveRoundClassPatchPrefix = typeof(ClipRemoveRoundClassPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(clipRemoveRoundClassPatchOriginal, harmony, true);
            harmony.Patch(clipRemoveRoundClassPatchOriginal, new HarmonyMethod(clipRemoveRoundClassPatchPrefix));

            //// FireArmLoadMagPatch
            //MethodInfo fireArmLoadMagPatchOriginal = typeof(FVRFireArm).GetMethod("LoadMag", BindingFlags.Public | BindingFlags.Instance);
            //MethodInfo fireArmLoadMagPatchPrefix = typeof(FireArmLoadMagPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //PatchController.Verify(fireArmLoadMagPatchOriginal, harmony, true);
            //harmony.Patch(fireArmLoadMagPatchOriginal, new HarmonyMethod(fireArmLoadMagPatchPrefix));

            //// FireArmEjectMagPatch
            //MethodInfo fireArmEjectMagPatchOriginal = typeof(FVRFireArm).GetMethod("EjectMag", BindingFlags.Public | BindingFlags.Instance);
            //MethodInfo fireArmEjectMagPatchPrefix = typeof(FireArmEjectMagPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            //MethodInfo fireArmEjectMagPatchPostfix = typeof(FireArmEjectMagPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            //PatchController.Verify(fireArmEjectMagPatchOriginal, harmony, true);
            //harmony.Patch(fireArmEjectMagPatchOriginal, new HarmonyMethod(fireArmEjectMagPatchPrefix), new HarmonyMethod(fireArmEjectMagPatchPostfix));

            //// FireArmLoadClipPatch
            //MethodInfo fireArmLoadClipPatchOriginal = typeof(FVRFireArm).GetMethod("LoadClip", BindingFlags.Public | BindingFlags.Instance);
            //MethodInfo fireArmLoadClipPatchPrefix = typeof(FireArmLoadClipPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //PatchController.Verify(fireArmLoadClipPatchOriginal, harmony, true);
            //harmony.Patch(fireArmLoadClipPatchOriginal, new HarmonyMethod(fireArmLoadClipPatchPrefix));

            //// FireArmEjectClipPatch
            //MethodInfo fireArmEjectClipPatchOriginal = typeof(FVRFireArm).GetMethod("EjectClip", BindingFlags.Public | BindingFlags.Instance);
            //MethodInfo fireArmEjectClipPatchPrefix = typeof(FireArmEjectClipPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            //MethodInfo fireArmEjectClipPatchPostfix = typeof(FireArmEjectClipPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            //PatchController.Verify(fireArmEjectClipPatchOriginal, harmony, true);
            //harmony.Patch(fireArmEjectClipPatchOriginal, new HarmonyMethod(fireArmEjectClipPatchPrefix), new HarmonyMethod(fireArmEjectClipPatchPostfix));

            // MagAddRoundPatch
            MethodInfo magAddRoundPatchOriginal = typeof(FVRFireArmMagazine).GetMethod("AddRound", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(FVRFireArmRound), typeof(bool), typeof(bool), typeof(bool) }, null);
            MethodInfo magAddRoundPatchPrefix = typeof(MagAddRoundPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(magAddRoundPatchOriginal, harmony, true);
            harmony.Patch(magAddRoundPatchOriginal, new HarmonyMethod(magAddRoundPatchPrefix));

            // MagAddRoundClassPatch
            MethodInfo magAddRoundClassPatchOriginal = typeof(FVRFireArmMagazine).GetMethod("AddRound", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(FireArmRoundClass), typeof(bool), typeof(bool) }, null);
            MethodInfo magAddRoundClassPatchPrefix = typeof(MagAddRoundClassPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(magAddRoundClassPatchOriginal, harmony, true);
            harmony.Patch(magAddRoundClassPatchOriginal, new HarmonyMethod(magAddRoundClassPatchPrefix));

            // MagReloadMagWithTypePatch
            MethodInfo magReloadMagWithTypeOriginal = typeof(FVRFireArmMagazine).GetMethod("ReloadMagWithType", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo magReloadMagWithTypePrefix = typeof(MagReloadMagWithTypePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo magReloadMagWithTypePostfix = typeof(MagReloadMagWithTypePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(magReloadMagWithTypeOriginal, harmony, true);
            harmony.Patch(magReloadMagWithTypeOriginal, new HarmonyMethod(magReloadMagWithTypePrefix), new HarmonyMethod(magReloadMagWithTypePostfix));

            // MagReloadMagWithListPatch
            MethodInfo magReloadMagWithListOriginal = typeof(FVRFireArmMagazine).GetMethod("ReloadMagWithList", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo magReloadMagWithListPrefix = typeof(MagReloadMagWithListPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo magReloadMagWithListPostfix = typeof(MagReloadMagWithListPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(magReloadMagWithListOriginal, harmony, true);
            harmony.Patch(magReloadMagWithListOriginal, new HarmonyMethod(magReloadMagWithListPrefix), new HarmonyMethod(magReloadMagWithListPostfix));

            // MagReloadMagWithTypeUpToPercentagePatch
            MethodInfo magReloadMagWithTypeUpToPercentageOriginal = typeof(FVRFireArmMagazine).GetMethod("ReloadMagWithTypeUpToPercentage", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo magReloadMagWithTypeUpToPercentagePrefix = typeof(MagReloadMagWithTypeUpToPercentagePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo magReloadMagWithTypeUpToPercentagePostfix = typeof(MagReloadMagWithTypeUpToPercentagePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(magReloadMagWithTypeUpToPercentageOriginal, harmony, true);
            harmony.Patch(magReloadMagWithTypeUpToPercentageOriginal, new HarmonyMethod(magReloadMagWithTypeUpToPercentagePrefix), new HarmonyMethod(magReloadMagWithTypeUpToPercentagePostfix));

            // ClipAddRoundPatch
            MethodInfo clipAddRoundPatchOriginal = typeof(FVRFireArmClip).GetMethod("AddRound", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(FVRFireArmRound), typeof(bool), typeof(bool) }, null);
            MethodInfo clipAddRoundPatchPrefix = typeof(ClipAddRoundPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(clipAddRoundPatchOriginal, harmony, true);
            harmony.Patch(clipAddRoundPatchOriginal, new HarmonyMethod(clipAddRoundPatchPrefix));

            // ClipAddRoundClassPatch
            MethodInfo clipAddRoundClassPatchOriginal = typeof(FVRFireArmClip).GetMethod("AddRound", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(FireArmRoundClass), typeof(bool), typeof(bool) }, null);
            MethodInfo clipAddRoundClassPatchPrefix = typeof(ClipAddRoundClassPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(clipAddRoundClassPatchOriginal, harmony, true);
            harmony.Patch(clipAddRoundClassPatchOriginal, new HarmonyMethod(clipAddRoundClassPatchPrefix));

            // ClipReloadClipWithTypePatch
            MethodInfo clipReloadClipWithTypeOriginal = typeof(FVRFireArmClip).GetMethod("ReloadClipWithType", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo clipReloadClipWithTypePrefix = typeof(ClipReloadClipWithTypePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo clipReloadClipWithTypePostfix = typeof(ClipReloadClipWithTypePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(clipReloadClipWithTypeOriginal, harmony, true);
            harmony.Patch(clipReloadClipWithTypeOriginal, new HarmonyMethod(clipReloadClipWithTypePrefix), new HarmonyMethod(clipReloadClipWithTypePostfix));

            // ClipReloadClipWithListPatch
            MethodInfo clipReloadClipWithListOriginal = typeof(FVRFireArmClip).GetMethod("ReloadClipWithList", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo clipReloadClipWithListPrefix = typeof(ClipReloadClipWithListPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo clipReloadClipWithListPostfix = typeof(ClipReloadClipWithListPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(clipReloadClipWithListOriginal, harmony, true);
            harmony.Patch(clipReloadClipWithListOriginal, new HarmonyMethod(clipReloadClipWithListPrefix), new HarmonyMethod(clipReloadClipWithListPostfix));

            // ModularWeaponPartPatch
            MethodInfo modularWeaponPartEnableOriginal = typeof(ModularWeaponPart).GetMethod("EnablePart", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo modularWeaponPartEnablePrefix = typeof(ModularWeaponPartPatch).GetMethod("EnablePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo modularWeaponPartDisableOriginal = typeof(ModularWeaponPart).GetMethod("DisablePart", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo modularWeaponPartDisablePrefix = typeof(ModularWeaponPartPatch).GetMethod("DisablePrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(modularWeaponPartEnableOriginal, harmony, true);
            PatchController.Verify(modularWeaponPartDisableOriginal, harmony, true);
            harmony.Patch(modularWeaponPartEnableOriginal, new HarmonyMethod(modularWeaponPartEnablePrefix));
            harmony.Patch(modularWeaponPartDisableOriginal, new HarmonyMethod(modularWeaponPartDisablePrefix));

            //// EntityCheckPatch
            //MethodInfo entityCheckPatchOriginal = typeof(AIManager).GetMethod("EntityCheck", BindingFlags.NonPublic | BindingFlags.Instance);
            //MethodInfo entityCheckPatchPrefix = typeof(EntityCheckPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //PatchController.Verify(entityCheckPatchOriginal, harmony, true);
            //harmony.Patch(entityCheckPatchOriginal, new HarmonyMethod(entityCheckPatchPrefix));

            //// GlobalFixedUpdatePatch
            //MethodInfo globalFixedUpdatePatchOriginal = typeof(FVRInteractiveObject).GetMethod("GlobalFixedUpdate", BindingFlags.Public | BindingFlags.Static);
            //MethodInfo globalFixedUpdatePatchPostfix = typeof(GlobalFixedUpdatePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            //PatchController.Verify(globalFixedUpdatePatchOriginal, harmony, true);
            //harmony.Patch(globalFixedUpdatePatchOriginal, null, new HarmonyMethod(globalFixedUpdatePatchPostfix));

            // PlayGrabSoundPatch
            MethodInfo playGrabSoundPatchOriginal = typeof(FVRInteractiveObject).GetMethod("PlayGrabSound", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo playGrabSoundPatchPrefix = typeof(PlayGrabSoundPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(playGrabSoundPatchOriginal, harmony, true);
            harmony.Patch(playGrabSoundPatchOriginal, new HarmonyMethod(playGrabSoundPatchPrefix));

            //// PlayReleaseSoundPatch
            //MethodInfo playReleaseSoundPatchOriginal = typeof(FVRInteractiveObject).GetMethod("PlayReleaseSound", BindingFlags.Public | BindingFlags.Instance);
            //MethodInfo playReleaseSoundPatchPrefix = typeof(PlayReleaseSoundPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //PatchController.Verify(playReleaseSoundPatchOriginal, harmony, true);
            //harmony.Patch(playReleaseSoundPatchOriginal, new HarmonyMethod(playReleaseSoundPatchPrefix));

            //// FireArmFirePatch
            //MethodInfo fireArmFirePatchOriginal = typeof(FVRFireArm).GetMethod("Fire", BindingFlags.Public | BindingFlags.Instance);
            //MethodInfo fireArmFirePatchPrefix = typeof(FireArmFirePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //PatchController.Verify(fireArmFirePatchOriginal, harmony, true);
            //harmony.Patch(fireArmFirePatchOriginal, new HarmonyMethod(fireArmFirePatchPrefix));

            //// FireArmRecoilPatch
            //MethodInfo fireArmRecoilPatchOriginal = typeof(FVRFireArm).GetMethod("Recoil", BindingFlags.Public | BindingFlags.Instance);
            //MethodInfo fireArmRecoilPatchPrefix = typeof(FireArmRecoilPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //PatchController.Verify(fireArmRecoilPatchOriginal, harmony, true);
            //harmony.Patch(fireArmRecoilPatchOriginal, new HarmonyMethod(fireArmRecoilPatchPrefix));

            // InteractiveObjectPatch
            MethodInfo interactiveObjectBeginInteractionOriginal = typeof(FVRInteractiveObject).GetMethod("BeginInteraction", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo interactiveObjectBeginInteractionPostfix = typeof(InteractiveObjectPatch).GetMethod("BeginInteractionPostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo interactiveObjectEndInteractionOriginal = typeof(FVRInteractiveObject).GetMethod("EndInteraction", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo interactiveObjectEndInteractionPostfix = typeof(InteractiveObjectPatch).GetMethod("EndInteractionPostfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(interactiveObjectBeginInteractionOriginal, harmony, true);
            PatchController.Verify(interactiveObjectEndInteractionOriginal, harmony, true);
            harmony.Patch(interactiveObjectBeginInteractionOriginal, null, new HarmonyMethod(interactiveObjectBeginInteractionPostfix));
            harmony.Patch(interactiveObjectEndInteractionOriginal, null, new HarmonyMethod(interactiveObjectEndInteractionPostfix));

            // HandEndInteractionIfHeldPatch
            MethodInfo handEndInteractionIfHeldOriginal = typeof(FVRViveHand).GetMethod("EndInteractionIfHeld", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo handEndInteractionIfHeldPrefix = typeof(HandEndInteractionIfHeldPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(handEndInteractionIfHeldOriginal, harmony, true);
            harmony.Patch(handEndInteractionIfHeldOriginal, new HarmonyMethod(handEndInteractionIfHeldPrefix));

            // FVRPhysicalObjectPatch
            MethodInfo endInteractionIntoInventorySlotOriginal = typeof(FVRPhysicalObject).GetMethod("EndInteractionIntoInventorySlot", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo endInteractionIntoInventorySlotPrefix = typeof(FVRPhysicalObjectPatch).GetMethod("EndInteractionIntoInventorySlotPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo endInteractionIntoInventorySlotPostfix = typeof(FVRPhysicalObjectPatch).GetMethod("EndInteractionIntoInventorySlotPostfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(endInteractionIntoInventorySlotOriginal, harmony, true);
            harmony.Patch(endInteractionIntoInventorySlotOriginal, new HarmonyMethod(endInteractionIntoInventorySlotPrefix), new HarmonyMethod(endInteractionIntoInventorySlotPostfix));

            //// SosigLinkDamagePatch
            //MethodInfo sosigLinkDamagePatchOriginal = typeof(SosigLink).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance);
            //MethodInfo sosigLinkDamagePatchPrefix = typeof(SosigLinkDamagePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            //MethodInfo sosigLinkDamagePatchPostfix = typeof(SosigLinkDamagePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            //PatchController.Verify(sosigLinkDamagePatchOriginal, harmony, true);
            //harmony.Patch(sosigLinkDamagePatchOriginal, new HarmonyMethod(sosigLinkDamagePatchPrefix), new HarmonyMethod(sosigLinkDamagePatchPostfix));

            //// PlayerBodyHealPercentPatch
            //MethodInfo playerBodyHealPercentPatchOriginal = typeof(FVRPlayerBody).GetMethod("HealPercent", BindingFlags.Public | BindingFlags.Instance);
            //MethodInfo playerBodyHealPercentPatchPrefix = typeof(PlayerBodyHealPercentPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //PatchController.Verify(playerBodyHealPercentPatchOriginal, harmony, true);
            //harmony.Patch(playerBodyHealPercentPatchOriginal, new HarmonyMethod(playerBodyHealPercentPatchPrefix));
            
            // Internal_CloneSinglePatch
            MethodInfo internal_CloneSinglePatchOriginal = typeof(UnityEngine.Object).GetMethod("Internal_CloneSingle", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo internal_CloneSinglePatchPostfix = typeof(Internal_CloneSinglePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(internal_CloneSinglePatchOriginal, harmony, true);
            harmony.Patch(internal_CloneSinglePatchOriginal, null, new HarmonyMethod(internal_CloneSinglePatchPostfix));

            // Internal_CloneSingleWithParentPatch
            MethodInfo internal_CloneSingleWithParentPatchOriginal = typeof(UnityEngine.Object).GetMethod("Internal_CloneSingleWithParent", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo internal_CloneSingleWithParentPatchPostfix = typeof(Internal_CloneSingleWithParentPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(internal_CloneSingleWithParentPatchOriginal, harmony, true);
            harmony.Patch(internal_CloneSingleWithParentPatchOriginal, null, new HarmonyMethod(internal_CloneSingleWithParentPatchPostfix));

            // Internal_InstantiateSinglePatch
            MethodInfo internal_InstantiateSinglePatchOriginal = typeof(UnityEngine.Object).GetMethod("Internal_InstantiateSingle", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo internal_InstantiateSinglePatchPostfix = typeof(Internal_InstantiateSinglePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(internal_InstantiateSinglePatchOriginal, harmony, true);
            harmony.Patch(internal_InstantiateSinglePatchOriginal, null, new HarmonyMethod(internal_InstantiateSinglePatchPostfix));

            // Internal_InstantiateSingleWithParentPatch
            MethodInfo internal_InstantiateSingleWithParentPatchOriginal = typeof(UnityEngine.Object).GetMethod("Internal_InstantiateSingleWithParent", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo internal_InstantiateSingleWithParentPatchPostfix = typeof(Internal_InstantiateSingleWithParentPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(internal_InstantiateSingleWithParentPatchOriginal, harmony, true);
            harmony.Patch(internal_InstantiateSingleWithParentPatchOriginal, null, new HarmonyMethod(internal_InstantiateSingleWithParentPatchPostfix));

            // WristMenuPatch
            MethodInfo wristMenuPatchUpdateOriginal = typeof(FVRWristMenu2).GetMethod("Update", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo wristMenuPatchUpdatePrefix = typeof(WristMenuPatch).GetMethod("UpdatePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo wristMenuPatchAwakeOriginal = typeof(FVRWristMenu2).GetMethod("Awake", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo wristMenuPatchAwakePrefix = typeof(WristMenuPatch).GetMethod("AwakePrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(wristMenuPatchUpdateOriginal, harmony, true);
            PatchController.Verify(wristMenuPatchAwakeOriginal, harmony, true);
            harmony.Patch(wristMenuPatchUpdateOriginal, new HarmonyMethod(wristMenuPatchUpdatePrefix));
            harmony.Patch(wristMenuPatchAwakeOriginal, new HarmonyMethod(wristMenuPatchAwakePrefix));

            // ModularWorkshopUIPatch
            MethodInfo updateDisplayOriginal = typeof(ModularWorkshopUI).GetMethod("UpdateDisplay", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo updateDisplayPrefix = typeof(ModularWorkshopUIPatch).GetMethod("UpdateDisplayPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo selectOriginal = typeof(ModularWorkshopUI).GetMethod("PButton_Select", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo selectPrefix = typeof(ModularWorkshopUIPatch).GetMethod("PButton_SelectPrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(updateDisplayOriginal, harmony, true);
            PatchController.Verify(selectOriginal, harmony, true);
            harmony.Patch(updateDisplayOriginal, new HarmonyMethod(updateDisplayPrefix));
            harmony.Patch(selectOriginal, new HarmonyMethod(selectPrefix));

            // SosigPatch
            MethodInfo supressionUpdateOriginal = typeof(Sosig).GetMethod("SuppresionUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo supressionUpdatePrefix = typeof(SosigPatch).GetMethod("SuppresionUpdatePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo executeDoorManipulationOriginal = typeof(Sosig).GetMethod("ExecuteDoorManipulation", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo executeDoorManipulationPrefix = typeof(SosigPatch).GetMethod("ExecuteDoorManipulationPrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(supressionUpdateOriginal, harmony, true);
            PatchController.Verify(executeDoorManipulationOriginal, harmony, true);
            harmony.Patch(supressionUpdateOriginal, new HarmonyMethod(supressionUpdatePrefix));
            harmony.Patch(executeDoorManipulationOriginal, new HarmonyMethod(executeDoorManipulationPrefix));

            // SosigHandPatch
            MethodInfo sosigHandHoldOriginal = typeof(SosigHand).GetMethod("Hold", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigHandHoldPrefix = typeof(SosigHandPatch).GetMethod("HoldPrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(sosigHandHoldOriginal, harmony, true);
            harmony.Patch(sosigHandHoldOriginal, new HarmonyMethod(sosigHandHoldPrefix));

            // NavMeshLinkExtensionPatch
            MethodInfo navMeshLinkExtensionInitDoorOriginal = typeof(NavMeshLinkExtension).GetMethod("InitDoor", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo navMeshLinkExtensionInitDoorPrefix = typeof(NavMeshLinkExtensionPatch).GetMethod("InitDoorPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo navMeshLinkExtensionTraverseOriginal = typeof(NavMeshLinkExtension).GetMethod("Traverse_Door", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo navMeshLinkExtensionTraversePrefix = typeof(NavMeshLinkExtensionPatch).GetMethod("TraversePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo navMeshLinkExtensionUpdateOriginal = typeof(NavMeshLinkExtension).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo navMeshLinkExtensionUpdatePrefix = typeof(NavMeshLinkExtensionPatch).GetMethod("UpdatePrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(navMeshLinkExtensionInitDoorOriginal, harmony, true);
            PatchController.Verify(navMeshLinkExtensionTraverseOriginal, harmony, true);
            PatchController.Verify(navMeshLinkExtensionUpdateOriginal, harmony, true);
            harmony.Patch(navMeshLinkExtensionInitDoorOriginal, new HarmonyMethod(navMeshLinkExtensionInitDoorPrefix));
            harmony.Patch(navMeshLinkExtensionTraverseOriginal, new HarmonyMethod(navMeshLinkExtensionTraversePrefix));
            harmony.Patch(navMeshLinkExtensionUpdateOriginal, new HarmonyMethod(navMeshLinkExtensionUpdatePrefix));
        }
    }

    #region GamePatches
    // Patches FVRWristMenu2.Update and Awake to add our EFM section to it
    class WristMenuPatch
    {
        static void UpdatePrefix(FVRWristMenu2 __instance)
        {
            if (!EFMWristMenuSection.init)
            {
                EFMWristMenuSection.init = true;

                AddSection(__instance);

                // Regenerate with our new section
                __instance.RegenerateButtons();
            }
        }

        static void AwakePrefix(FVRWristMenu2 __instance)
        {
            AddSection(__instance);
        }

        private static void AddSection(FVRWristMenu2 __instance)
        {
            GameObject section = new GameObject("Section_EFM", typeof(RectTransform));
            section.transform.SetParent(__instance.MenuGO.transform);
            section.transform.localPosition = new Vector3(0, 300, 0);
            section.transform.localRotation = Quaternion.identity;
            section.transform.localScale = Vector3.one;
            section.GetComponent<RectTransform>().sizeDelta = new Vector2(350, 350);
            FVRWristMenuSection sectionScript = section.AddComponent<EFMWristMenuSection>();
            sectionScript.ButtonText = "EFM";
            __instance.Sections.Add(sectionScript);
            section.SetActive(false);
        }
    }

    // Patches FVRPlayerBody.ConfigureQuickbelt for custom behavior
    // -1: Set to pocket configuration (Gets rid of any slots above)
    //     Making assumption that pockets will always be the first 6 slots
    //     Used when we take off a rig
    //     Note that the items themselves are not attached to the slot,
    //     so they don't get destroyed along with it
    //     Those will simply get disabled when we take off the rig
    // -2: Like -1 but also destroys the items in those slots
    //     Used when we load the hideout from the hideout because we
    //     want to keep pockets, but not other slots, and items on those other slots
    //     should stop existing
    //     Note that this whole case may not be necessary because when we reload the hideout
    //     it is like changing scene, so every gameobject in the scene will get destroyed anyway
    //     and considering that items in QB doesn't get attached to the slot, it should remain a part of
    //     the scene, and will get destroyed along with it
    // -3: Like -2 but also destroys items in pockets
    //     Currently unused, it was supposed to be used on death to clear QBS completely,
    //     but now we instead drop everything on death
    // -4: Destroy all slots, this essentially sets the QBS config to None
    class ConfigureQuickbeltPatch
    {
        public static bool overrideIndex;
        public static int actualConfigIndex;

        static bool Prefix(FVRPlayerBody __instance, ref int index)
        {
            Mod.LogInfo("ConfigureQuickbeltPatch called with index " + index + ":\n" + Environment.StackTrace);
            if (overrideIndex && index != actualConfigIndex)
            {
                Mod.LogWarning("ConfigureQuickbeltPatch called with index " + index + " which did not match actual " + actualConfigIndex + ":\n" + Environment.StackTrace);
                index = actualConfigIndex;
                overrideIndex = false;
            }

            if (!Mod.inMeatovScene)
            {
                return true;
            }

            if (index < 0)
            {
                // Clear the belt above the pockets
                if (__instance.QBSlots_Internal.Count > 6)
                {
                    for (int i = __instance.QBSlots_Internal.Count - 1; i >= 6; --i)
                    {
                        if (__instance.QBSlots_Internal[i] == null)
                        {
                            __instance.QBSlots_Internal.RemoveAt(i);
                        }
                        else if (__instance.QBSlots_Internal[i].IsPlayer)
                        {
                            // Index -2 or -3 will destroy objects associated to the slots
                            if ((index == -2 || index == -3) && __instance.QBSlots_Internal[i].CurObject != null)
                            {
                                GameObject.Destroy(__instance.QBSlots_Internal[i].CurObject.gameObject);
                                __instance.QBSlots_Internal[i].CurObject.ClearQuickbeltState();
                            }
                            UnityEngine.Object.Destroy(__instance.QBSlots_Internal[i].gameObject);
                            __instance.QBSlots_Internal.RemoveAt(i);
                        }
                    }
                }

                // If -3 also destroy objects in pockets, but dont get rid of the slots themselves
                // This is unused but was meant to be in case of death. Instead we jsut detach everything from player upon death and then load base 
                // and set the config to pockets only
                if (index == -3)
                {
                    for (int i = 3; i >= 0; --i)
                    {
                        if (__instance.QBSlots_Internal[i].IsPlayer && __instance.QBSlots_Internal[i].CurObject != null)
                        {
                            GameObject.Destroy(__instance.QBSlots_Internal[i].CurObject.gameObject);
                            __instance.QBSlots_Internal[i].CurObject.ClearQuickbeltState();
                        }
                    }
                }

                // If -4 get rid of any remaining slots
                if (index == -4)
                {
                    for (int i = __instance.QBSlots_Internal.Count - 1; i >= 0; --i)
                    {
                        if (__instance.QBSlots_Internal[i] == null)
                        {
                            __instance.QBSlots_Internal.RemoveAt(i);
                        }
                        else if (__instance.QBSlots_Internal[i].IsPlayer)
                        {
                            // Destroy objects associated to the slots
                            if (__instance.QBSlots_Internal[i].CurObject != null)
                            {
                                GameObject.Destroy(__instance.QBSlots_Internal[i].CurObject.gameObject);
                                __instance.QBSlots_Internal[i].CurObject.ClearQuickbeltState();
                            }
                            UnityEngine.Object.Destroy(__instance.QBSlots_Internal[i].gameObject);
                            __instance.QBSlots_Internal.RemoveAt(i);
                        }
                    }
                }

                Mod.currentQuickBeltConfiguration = Mod.pocketsConfigIndex;
            }
            else if (index > Mod.pocketsConfigIndex) // If index is higher than the pockets configuration index, we must keep the pocket slots intact
            {
                // Only check for slots other than pockets/shoulders
                if (__instance.QBSlots_Internal.Count >  6)
                {
                    for (int i = __instance.QBSlots_Internal.Count - 1; i >= 6; i--)
                    {
                        if (__instance.QBSlots_Internal[i] == null)
                        {
                            __instance.QBSlots_Internal.RemoveAt(i);
                        }
                        else if (__instance.QBSlots_Internal[i].IsPlayer)
                        {
                            if (__instance.QBSlots_Internal[i].CurObject != null)
                            {
                                __instance.QBSlots_Internal[i].CurObject.ClearQuickbeltState();
                            }
                            UnityEngine.Object.Destroy(__instance.QBSlots_Internal[i].gameObject);
                            __instance.QBSlots_Internal.RemoveAt(i);
                        }
                    }
                }
                int num = Mathf.Clamp(index, 0, ManagerSingleton<GM>.Instance.QuickbeltConfigurations.Length - 1);
                GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(ManagerSingleton<GM>.Instance.QuickbeltConfigurations[num], __instance.Torso.position, __instance.Torso.rotation);
                gameObject.transform.SetParent(__instance.Torso.transform);
                gameObject.transform.localPosition = Vector3.zero;
                IEnumerator enumerator = gameObject.transform.GetEnumerator();
                try
                {
                    while (enumerator.MoveNext())
                    {
                        object obj = enumerator.Current;
                        Transform transform = (Transform)obj;
                        if (transform.gameObject.tag == "QuickbeltSlot")
                        {
                            FVRQuickBeltSlot component = transform.GetComponent<FVRQuickBeltSlot>();
                            if (GM.Options.QuickbeltOptions.QuickbeltHandedness > 0)
                            {
                                Vector3 vector = component.PoseOverride.forward;
                                Vector3 vector2 = component.PoseOverride.up;
                                vector = Vector3.Reflect(vector, component.transform.right);
                                vector2 = Vector3.Reflect(vector2, component.transform.right);
                                component.PoseOverride.rotation = Quaternion.LookRotation(vector, vector2);
                            }
                            __instance.QBSlots_Internal.Add(component);
                        }
                    }
                }
                finally
                {
                    IDisposable disposable;
                    if ((disposable = (enumerator as IDisposable)) != null)
                    {
                        disposable.Dispose();
                    }
                }
                for (int j = 0; j < __instance.QBSlots_Internal.Count; j++)
                {
                    if (__instance.QBSlots_Internal[j].IsPlayer)
                    {
                        __instance.QBSlots_Internal[j].transform.SetParent(__instance.Torso);
                        __instance.QBSlots_Internal[j].QuickbeltRoot = null;
                        if (GM.Options.QuickbeltOptions.QuickbeltHandedness > 0)
                        {
                            __instance.QBSlots_Internal[j].transform.localPosition = new Vector3(-__instance.QBSlots_Internal[j].transform.localPosition.x, __instance.QBSlots_Internal[j].transform.localPosition.y, __instance.QBSlots_Internal[j].transform.localPosition.z);
                        }
                    }
                }
                UnityEngine.Object.Destroy(gameObject);

                // Set custom quick belt config index
                Mod.currentQuickBeltConfiguration = index;
            }
            else // Equal to pockets or another vanilla config
            {
                return true;
            }

            return false;
        }

        static void Postfix(int index, ref FVRPlayerBody __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            // Refresh the pocket and shoulder quickbelt slots if necessary, they should always be the first 6 in the list of quickbelt slots
            if (index >= Mod.pocketsConfigIndex)
            {
                for (int i = 0; i < 4; ++i)
                {
                    Mod.pocketSlots[i] = __instance.QBSlots_Internal[i];
                }
                ShoulderStorage currentShoulder = __instance.QBSlots_Internal[4] as ShoulderStorage;
                if(currentShoulder == null)
                {
                    Mod.LogError("DEV: Could not get right shoulder from pocket config slots");
                    return;
                }
                else
                {
                    Mod.rightShoulderSlot = currentShoulder;
                }
                currentShoulder = __instance.QBSlots_Internal[5] as ShoulderStorage;
                if (currentShoulder == null)
                {
                    Mod.LogError("DEV: Could not get left shoulder from pocket config slots");
                    return;
                }
                else
                {
                    Mod.leftShoulderSlot = currentShoulder;
                }
            }
        }
    }

    // Patches FVRViveHand.TestQuickBeltDistances so we also check custom slots and check for equipment incompatibility
    // This completely replaces the original
    class TestQuickbeltPatch
    {
        static bool Prefix(FVRViveHand __instance, FVRViveHand.HandState ___m_state, FVRInteractiveObject ___m_currentInteractable)
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }

            if (__instance.CurrentHoveredQuickbeltSlot != null && !__instance.CurrentHoveredQuickbeltSlot.IsSelectable)
            {
                __instance.CurrentHoveredQuickbeltSlot = null;
            }
            if (__instance.CurrentHoveredQuickbeltSlotDirty != null && !__instance.CurrentHoveredQuickbeltSlotDirty.IsSelectable)
            {
                __instance.CurrentHoveredQuickbeltSlotDirty = null;
            }
            if (__instance.CurrentHoveredQuickbeltSlot != null && __instance.CurrentHoveredQuickbeltSlot.CurObject != null && !__instance.CurrentHoveredQuickbeltSlot.CurObject.IsInteractable())
            {
                __instance.CurrentHoveredQuickbeltSlot = null;
            }
            FVRQuickBeltSlot fvrquickBeltSlot = null;
            bool flag = false;
            Vector3 position = __instance.PoseOverride.position;
            if (__instance.CurrentInteractable != null)
            {
                if (__instance.CurrentInteractable.PoseOverride != null)
                {
                    position = __instance.CurrentInteractable.PoseOverride.position;
                }
                else
                {
                    position = __instance.CurrentInteractable.transform.position;
                }
            }
            for (int i = 0; i < GM.CurrentPlayerBody.QBSlots_Internal.Count; i++)
            {
                if (GM.CurrentPlayerBody.QBSlots_Internal[i].IsPointInsideMe(position))
                {
                    flag = true;
                    fvrquickBeltSlot = GM.CurrentPlayerBody.QBSlots_Internal[i];
                    break;
                }
            }
            if (!flag)
            {
                for (int j = 0; j < GM.CurrentPlayerBody.QBSlots_Added.Count; j++)
                {
                    if (GM.CurrentPlayerBody.QBSlots_Added[j].IsPointInsideMe(position))
                    {
                        flag = true;
                        fvrquickBeltSlot = GM.CurrentPlayerBody.QBSlots_Added[j];
                        break;
                    }
                }
            }
            if (!flag)
            {
                for (int k = 0; k < GM.CurrentPlayerBody.QuickbeltSlots.Count; k++)
                {
                    if (GM.CurrentPlayerBody.QuickbeltSlots[k].IsPointInsideMe(position))
                    {
                        fvrquickBeltSlot = GM.CurrentPlayerBody.QuickbeltSlots[k];
                        break;
                    }
                }
            }

            // Check equip slots if status UI is active
            int equipmentSlotIndex = -1;
            if (fvrquickBeltSlot == null && StatusUI.instance != null && StatusUI.instance.IsOpen() && StatusUI.instance.equipmentSlots != null)
            {
                for (int i = 0; i < StatusUI.instance.equipmentSlots.Length; ++i)
                {
                    if (StatusUI.instance.equipmentSlots[i].IsPointInsideMe(position))
                    {
                        fvrquickBeltSlot = StatusUI.instance.equipmentSlots[i];
                        equipmentSlotIndex = i;
                        break;
                    }
                }
            }

            // Check other active slots
            if (fvrquickBeltSlot == null && Mod.looseRigSlots != null)
            {
                for (int setIndex = 0; setIndex < Mod.looseRigSlots.Count; ++setIndex)
                {
                    for (int slotIndex = 0; slotIndex < Mod.looseRigSlots[setIndex].Count; ++slotIndex)
                    {
                        if (Mod.looseRigSlots[setIndex][slotIndex].IsPointInsideMe(position))
                        {
                            fvrquickBeltSlot = Mod.looseRigSlots[setIndex][slotIndex];
                            break;
                        }
                    }
                }
            }

            // Check area slots
            if (fvrquickBeltSlot == null && HideoutController.instance != null)
            {
                for(int i=0; i < HideoutController.instance.areaController.areas.Length; ++i)
                {
                    Area area = HideoutController.instance.areaController.areas[i];
                    if (area != null && area.currentLevel < area.levels.Length)
                    {
                        for(int j = 0; j < area.levels[area.currentLevel].areaSlots.Length; ++j)
                        {
                            if (area.levels[area.currentLevel].areaSlots[j].IsPointInsideMe(position))
                            {
                                fvrquickBeltSlot = area.levels[area.currentLevel].areaSlots[j];
                                break;
                            }
                        }
                    }
                }
            }

            if (fvrquickBeltSlot == null)
            {
                if (__instance.CurrentHoveredQuickbeltSlot != null)
                {
                    __instance.CurrentHoveredQuickbeltSlot = null;
                }
                __instance.CurrentHoveredQuickbeltSlotDirty = null;
            }
            else
            {
                __instance.CurrentHoveredQuickbeltSlotDirty = fvrquickBeltSlot;
                if (___m_state == FVRViveHand.HandState.Empty)
                {
                    if (fvrquickBeltSlot.CurObject != null && !fvrquickBeltSlot.CurObject.IsHeld && fvrquickBeltSlot.CurObject.IsInteractable())
                    {
                        __instance.CurrentHoveredQuickbeltSlot = fvrquickBeltSlot;
                    }
                    else if (fvrquickBeltSlot is ShoulderStorage 
                        && !(fvrquickBeltSlot as ShoulderStorage).right 
                        && StatusUI.instance.equipmentSlots[0].CurObject != null 
                        && !StatusUI.instance.equipmentSlots[0].CurObject.IsHeld 
                        && StatusUI.instance.equipmentSlots[0].CurObject.IsInteractable())
                    {
                        // Set hovered QB slot to backpack equip slot if it is left shoulder and backpack slot is not empty
                        __instance.CurrentHoveredQuickbeltSlot = StatusUI.instance.equipmentSlots[0];
                    }
                }
                else if (___m_state == FVRViveHand.HandState.GripInteracting && ___m_currentInteractable != null && ___m_currentInteractable is FVRPhysicalObject)
                {
                    FVRPhysicalObject fvrphysicalObject = (FVRPhysicalObject)___m_currentInteractable;
                    if (fvrquickBeltSlot.CurObject == null && fvrquickBeltSlot.SizeLimit >= fvrphysicalObject.Size && fvrphysicalObject.QBSlotType == fvrquickBeltSlot.Type)
                    {
                        // Check for equipment compatibility if slot is an equipment slot
                        MeatovItem item = Mod.meatovItemByInteractive.TryGetValue(fvrphysicalObject, out item) ? item : fvrphysicalObject.GetComponent<MeatovItem>();
                        if (equipmentSlotIndex > -1)
                        {
                            if (item != null)
                            {
                                bool typeCompatible = StatusUI.instance.equipmentSlots[equipmentSlotIndex].equipmentType == item.itemType;
                                bool otherCompatible = true;
                                switch (item.itemType)
                                {
                                    case MeatovItem.ItemType.ArmoredRig:
                                        typeCompatible = StatusUI.instance.equipmentSlots[equipmentSlotIndex].equipmentType == MeatovItem.ItemType.BodyArmor;
                                        otherCompatible = !EquipmentSlot.wearingBodyArmor && !EquipmentSlot.wearingRig;
                                        break;
                                    case MeatovItem.ItemType.BodyArmor:
                                        otherCompatible = !EquipmentSlot.wearingArmoredRig;
                                        break;
                                    case MeatovItem.ItemType.Helmet:
                                        typeCompatible = StatusUI.instance.equipmentSlots[equipmentSlotIndex].equipmentType == MeatovItem.ItemType.Headwear;
                                        otherCompatible = (!EquipmentSlot.wearingEarpiece || !EquipmentSlot.currentEarpiece.blocksHeadwear) &&
                                                          (!EquipmentSlot.wearingFaceCover || !EquipmentSlot.currentFaceCover.blocksHeadwear) &&
                                                          (!EquipmentSlot.wearingEyewear || !EquipmentSlot.currentEyewear.blocksHeadwear);
                                        break;
                                    case MeatovItem.ItemType.Earpiece:
                                        otherCompatible = (!EquipmentSlot.wearingHeadwear || !EquipmentSlot.currentHeadwear.blocksEarpiece) &&
                                                          (!EquipmentSlot.wearingFaceCover || !EquipmentSlot.currentFaceCover.blocksEarpiece) &&
                                                          (!EquipmentSlot.wearingEyewear || !EquipmentSlot.currentEyewear.blocksEarpiece);
                                        break;
                                    case MeatovItem.ItemType.FaceCover:
                                        otherCompatible = (!EquipmentSlot.wearingHeadwear || !EquipmentSlot.currentHeadwear.blocksFaceCover) &&
                                                          (!EquipmentSlot.wearingEarpiece || !EquipmentSlot.currentEarpiece.blocksFaceCover) &&
                                                          (!EquipmentSlot.wearingEyewear || !EquipmentSlot.currentEyewear.blocksFaceCover);
                                        break;
                                    case MeatovItem.ItemType.Eyewear:
                                        otherCompatible = (!EquipmentSlot.wearingHeadwear || !EquipmentSlot.currentHeadwear.blocksEyewear) &&
                                                          (!EquipmentSlot.wearingEarpiece || !EquipmentSlot.currentEarpiece.blocksEyewear) &&
                                                          (!EquipmentSlot.wearingFaceCover || !EquipmentSlot.currentFaceCover.blocksEyewear);
                                        break;
                                    case MeatovItem.ItemType.Rig:
                                        otherCompatible = !EquipmentSlot.wearingArmoredRig;
                                        break;
                                    case MeatovItem.ItemType.Headwear:
                                        otherCompatible = (!EquipmentSlot.wearingEarpiece || !EquipmentSlot.currentEarpiece.blocksHeadwear) &&
                                                          (!EquipmentSlot.wearingFaceCover || !EquipmentSlot.currentFaceCover.blocksHeadwear) &&
                                                          (!EquipmentSlot.wearingEyewear || !EquipmentSlot.currentEyewear.blocksHeadwear);
                                        break;
                                    default:
                                        break;
                                }
                                if (typeCompatible && otherCompatible)
                                {
                                    __instance.CurrentHoveredQuickbeltSlot = fvrquickBeltSlot;
                                }
                            }
                        }
                        else if (fvrquickBeltSlot is ShoulderStorage)
                        {
                            // If left shoulder, make sure item is backpack, and player not already wearing a backpack
                            // If right shoulder, make sure item is a firearm or melee weapon
                            if (!(fvrquickBeltSlot as ShoulderStorage).right && item.itemType == MeatovItem.ItemType.Backpack && EquipmentSlot.currentBackpack == null)
                            {
                                __instance.CurrentHoveredQuickbeltSlot = StatusUI.instance.equipmentSlots[0];
                            }
                            else if ((fvrquickBeltSlot as ShoulderStorage).right && (item.itemType == MeatovItem.ItemType.Weapon || fvrphysicalObject is FVRMeleeWeapon))
                            {
                                __instance.CurrentHoveredQuickbeltSlot = fvrquickBeltSlot;
                            }
                        }
                        else if (fvrquickBeltSlot is AreaSlot)
                        {
                            AreaSlot asAreaSlot = fvrquickBeltSlot as AreaSlot;
                            if(Mod.IDDescribedInList(item.tarkovID, item.parents, asAreaSlot.filter, null))
                            {
                                __instance.CurrentHoveredQuickbeltSlot = fvrquickBeltSlot;
                            }
                        }
                        else
                        {
                            __instance.CurrentHoveredQuickbeltSlot = fvrquickBeltSlot;
                        }
                    }
                }
            }

            return false;
        }
    }

    // Patches FVRPhysicalObject.SetQuickBeltSlot so we can keep track of what goes in and out of rigs
    class SetQuickBeltSlotPatch
    {
        public static int fullSkip;
        public static bool skipPatch;
        public static bool dontProcessRigWeight;

        static void Prefix(ref FVRQuickBeltSlot slot, ref FVRPhysicalObject __instance)
        {
            if (!Mod.inMeatovScene || fullSkip > 0)
            {
                return;
            }
            Mod.LogInfo("SetQuickBeltSlotPatch called");

            // In case that new slot is same as current, we want to skip most of the patch
            // Can be possible in case of harnessed slot
            if (slot == __instance.QuickbeltSlot)
            {
                Mod.LogInfo("Item " + __instance.name + " is already in slot: " + (slot == null ? "null" : slot.name) + ", skipping SetQuickBeltSlotPatch patch");
                skipPatch = true;

                // Even if skipping patch, need to make sure that in the case of the backpack shoulder slot, we still set it as equip slot
                // This is in case the backpack is harnessed to the slot, it will already have this slot assigned but we can't skip this
                if (slot != null)
                {
                    // Need to make sure that a backpack being put into left shoulder slot gets put into backpack equipment slot instead
                    if (slot is ShoulderStorage)
                    {
                        if (!(slot as ShoulderStorage).right)
                        {
                            // Set the slot ref as the backpack equip slot so the original method puts it in this one instead
                            slot = StatusUI.instance.equipmentSlots[0];
                        }
                    }
                }

                return;
            }

            if (slot == null)
            {
                if (__instance.QuickbeltSlot == null)
                {
                    return;
                }

                // Set the size of the object to normal because it may have been scaled to fit the slot
                __instance.transform.localScale = Vector3.one;

                // Prefix will be called before the object's current slot is set to null, so we can check if it was taken from an equipment slot or a rig slot
                MeatovItem item = __instance.GetComponent<MeatovItem>();
                if (__instance.QuickbeltSlot is EquipmentSlot)
                {
                    // Have to remove equipment
                    if (item != null)
                    {
                        EquipmentSlot.TakeOffEquipment(item);
                    }

                    // Also set left shoulder object to null if this is backpack slot
                    if (__instance.QuickbeltSlot.Equals(StatusUI.instance.equipmentSlots[0]))
                    {
                        Mod.leftShoulderObject = null;
                    }

                    // Decrement total weight, it will be readded if item is taken out of slot by hand
                    Mod.weight -= item.currentWeight;
                }
                else if (__instance.QuickbeltSlot is ShoulderStorage)
                {
                    // Manage item begin removed from shoulder slot
                    ShoulderStorage asShoulderSlot = __instance.QuickbeltSlot as ShoulderStorage;
                    if (asShoulderSlot.right)
                    {
                        Mod.rightShoulderObject = null;
                    }
                    else
                    {
                        // Only backpacks fit in left shoulder slot
                        Mod.leftShoulderObject = null;

                        // So make sure we take it off as equipment
                        EquipmentSlot.TakeOffEquipment(item);

                    }

                    // Decrement total weight, it will be readded if item is taken out of slot by hand
                    Mod.weight -= item.currentWeight;
                }
                else if (__instance.QuickbeltSlot is AreaSlot)
                {
                    AreaSlot asAreaSlot = __instance.QuickbeltSlot as AreaSlot;
                    if(asAreaSlot.item == item)
                    {
                        asAreaSlot.item = null;
                    }
                }
                else
                {
                    // Check if in pockets
                    for (int i = 0; i < 4; ++i)
                    {
                        if (Mod.itemsInPocketSlots[i] == item)
                        {
                            Mod.itemsInPocketSlots[i] = null;

                            // Decrement total weight, it will be readded if item is taken out of slot by hand
                            Mod.weight -= item.currentWeight;
                            return;
                        }
                    }

                    // Check if slot in a loose rig
                    if(__instance.QuickbeltSlot is RigSlot)
                    {
                        MeatovItem rig = (__instance.QuickbeltSlot as RigSlot).ownerItem;
                        if (rig != null && (rig.itemType == MeatovItem.ItemType.Rig || rig.itemType == MeatovItem.ItemType.ArmoredRig))
                        {
                            // This slot is owned by a rig, need to update that rig's content
                            for (int slotIndex = 0; slotIndex < rig.rigSlots.Count; ++slotIndex)
                            {
                                if (rig.rigSlots[slotIndex] == __instance.QuickbeltSlot)
                                {
                                    rig.itemsInSlots[slotIndex] = null;
                                    // NOTE that we dont process weight here, it will be handled by parenting system
                                    //if (!dontProcessRigWeight)
                                    //{
                                    //    // Note that this will also decrement from total weight
                                    //    rig.currentWeight -= item.currentWeight;
                                    //}
                                    return;
                                }
                            }
                        }
                    }

                    if (EquipmentSlot.currentRig != null) // Is slot of current rig
                    {
                        // Find item in rig's itemsInSlots and remove it
                        for (int i = 0; i < EquipmentSlot.currentRig.itemsInSlots.Length; ++i)
                        {
                            if (EquipmentSlot.currentRig.itemsInSlots[i] == item)
                            {
                                EquipmentSlot.currentRig.itemsInSlots[i] = null;

                                // NOTE that we dont process weight here, it will be handled by parenting system
                                //if (!dontProcessRigWeight)
                                //{
                                //    // Note that this will also decrement from total weight
                                //    EquipmentSlot.currentRig.currentWeight -= item.currentWeight;
                                //}

                                // The model of the rig in the equipment slot should be updated
                                EquipmentSlot.currentRig.UpdateClosedMode();
                                return;
                            }
                        }
                    }
                }
            }
            else
            {
                // Need to make sure that a backpack being put into left shoulder slot gets put into backpack equipment slot instead
                if (slot is ShoulderStorage)
                {
                    if (!(slot as ShoulderStorage).right)
                    { 
                        // Set the slot ref as the backpack equip slot so the original method puts it in this one instead
                        slot = StatusUI.instance.equipmentSlots[0];
                    }
                }
            }
        }

        static void Postfix(FVRQuickBeltSlot slot, ref FVRPhysicalObject __instance)
        {
            if (!Mod.inMeatovScene || fullSkip > 0)
            {
                return;
            }

            MeatovItem item = __instance.GetComponent<MeatovItem>();

            if (skipPatch)
            {
                skipPatch = false;

                if (slot != null)
                {
                    if (slot is EquipmentSlot && __instance.m_hand == null)
                    {
                        // Update mode in case of a togglable equipment item, because we want to make sure it is
                        // using the correct closed model
                        item.UpdateClosedMode();
                    }
                }

                return;
            }

            if (slot == null)
            {
                return;
            }

            // Ensure the item's parent is null
            // TODO: EndInteractionInInventorySlot is supposed to attach the item to quick belt root but this was only happening to equipment in equip slots and not on rig slots
            // So should we attach them to the slots or no? because it causes extreme lag and is clearly unnecessary
            //__instance.SetParentage(null);

            // Check if pocket slot
            for (int i = 0; i < 4; ++i)
            {
                if (Mod.pocketSlots[i] == slot)
                {
                    Mod.itemsInPocketSlots[i] = item;

                    // Increment total weight, it will have been removed if item is put in there by hand
                    Mod.weight += item.currentWeight;
                    return;
                }
            }

            // Check if shoulder slot
            if (slot is ShoulderStorage)
            {
                ShoulderStorage asShoulderSlot = slot as ShoulderStorage;
                if (asShoulderSlot.right)
                {
                    Mod.rightShoulderObject = __instance.gameObject;
                }
                // else, Note that this can't happen because of how we handle this shoulder in the Prefix

                // Increment total weight, it will have been removed if item is put in there by hand
                Mod.weight += item.currentWeight;

                return;
            }

            if (slot is AreaSlot)
            {
                AreaSlot asAreaSlot = __instance.QuickbeltSlot as AreaSlot;
                asAreaSlot.item = item;

                asAreaSlot.area.UI.PlaySlotInputSound();

                asAreaSlot.UpdatePose();
            }

            if (slot is EquipmentSlot)
            {
                // Make equipment the size of its QBPoseOverride because by default the game only sets rotation
                if (__instance.QBPoseOverride != null)
                {
                    __instance.transform.localScale = __instance.QBPoseOverride.localScale;

                    // Also set the slot's poseoverride to the QBPoseOverride of the item so it get positionned properly
                    // Multiply poseoverride position by 10 because our pose override is set in cm not relative to scale of QBTransform but H3 sets position relative to it
                    slot.PoseOverride.localPosition = __instance.QBPoseOverride.localPosition * 10;
                }

                // If this is backpack slot, also set left shoulder to the object
                if (slot == StatusUI.instance.equipmentSlots[0])
                {
                    Mod.leftShoulderObject = __instance.gameObject;
                }

                // Increment total weight, it will have been removed if item is put in there by hand
                Mod.weight += item.currentWeight;

                EquipmentSlot.WearEquipment(item);
            }
            else if (slot is RigSlot)
            {
                MeatovItem rig = (__instance.QuickbeltSlot as RigSlot).ownerItem;
                if (rig != null && (rig.itemType == MeatovItem.ItemType.Rig || rig.itemType == MeatovItem.ItemType.ArmoredRig))
                {
                    // This slot is owned by a rig, need to update that rig's content
                    for (int slotIndex = 0; slotIndex < rig.rigSlots.Count; ++slotIndex)
                    {
                        if (rig.rigSlots[slotIndex] == __instance.QuickbeltSlot)
                        {
                            rig.itemsInSlots[slotIndex] = item;
                            // NOTE that we dont process weight here, it will be handled by parenting system
                            //if (!dontProcessRigWeight)
                            //{
                            //    // Note that this will also increment total weight
                            //    rig.currentWeight += item.currentWeight;
                            //}
                            rig.UpdateClosedMode();
                            return;
                        }
                    }
                }
            }
            else if (EquipmentSlot.wearingArmoredRig || EquipmentSlot.wearingRig) // We are wearing custom quick belt, check if slot is in there, update if it is
            {
                // Find slot index in config
                for (int slotIndex = 6; slotIndex < GM.CurrentPlayerBody.QBSlots_Internal.Count; ++slotIndex)
                {
                    if (GM.CurrentPlayerBody.QBSlots_Internal[slotIndex] == slot)
                    {
                        MeatovItem parentRigItem = EquipmentSlot.currentRig;
                        parentRigItem.itemsInSlots[slotIndex - 6] = item;
                        // NOTE that we dont process weight here, it will be handled by parenting system
                        //if (!dontProcessRigWeight)
                        //{
                        //    // Note that this will also increment total weight
                        //    parentRigItem.currentWeight += item.currentWeight;
                        //}
                        parentRigItem.UpdateClosedMode();
                        break;
                    }
                }
            }
        }
    }

    // Patches FVRPhysicalObject.BeginInteraction() to manage which pose override to use on specific types of items, and prevent weight management for loose rigs
    class BeginInteractionPatch
    {
        static void Prefix(FVRViveHand hand, FVRPhysicalObject __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            MeatovItem item = __instance.GetComponent<MeatovItem>();
            // If we are taking this item from a loose rig/backpack that is in player inventory, we don't want to remove/add item weight to total weight
            //if (__instance.QuickbeltSlot != null)
            //{
            //    if(__instance.QuickbeltSlot is RigSlot)
            //    {
            //        SetQuickBeltSlotPatch.dontProcessTotalWeight = ((RigSlot)__instance.QuickbeltSlot).ownerItem.locationIndex == 0;
            //        MeatovItem.parentChangeDontManageWeight = SetQuickBeltSlotPatch.dontProcessTotalWeight;
            //    }
            //    else if(EquipmentSlot.wearingRig || EquipmentSlot.wearingArmoredRig)
            //    {
            //        // Find slot in config
            //        for (int slotIndex = 6; slotIndex < GM.CurrentPlayerBody.QBSlots_Internal.Count; ++slotIndex)
            //        {
            //            if (GM.CurrentPlayerBody.QBSlots_Internal[slotIndex] == __instance.QuickbeltSlot)
            //            {
            //                SetQuickBeltSlotPatch.dontProcessTotalWeight = true;
            //                MeatovItem.parentChangeDontManageWeight = true;
            //                break;
            //            }
            //        }
            //    }
            //}
            //else if(item != null && item.parentVolume != null && item.parentVolume is ContainerVolume)
            //{
            //    MeatovItem.parentChangeDontManageWeight = (item.parentVolume as ContainerVolume).ownerItem.locationIndex == 0;
            //}

            if (item != null)
            {
                // Set pose override depending on item type and hand side
                // Must be done in prefix(?) to make sure vanilla handles interaction with correct pose override
                if (item.itemType == MeatovItem.ItemType.ArmoredRig || item.itemType == MeatovItem.ItemType.Rig || item.itemType == MeatovItem.ItemType.Backpack)
                {
                    __instance.PoseOverride = hand.IsThisTheRightHand ? item.rightHandPoseOverride : item.leftHandPoseOverride;

                    //// If taken out of shoulderStorage, align with hand. Only do this if the item is not currently being held
                    //// because if the backpack is harnessed, held, and then switched between hands, it will align with the new hand
                    //// This is prefix so m_hand should still be null if newly grabbed
                    //if (Mod.leftShoulderObject != null && Mod.leftShoulderObject.Equals(item.gameObject) && __instance.m_hand == null)
                    //{
                    //    FVRViveHand.AlignChild(__instance.transform, __instance.PoseOverride, hand.transform);
                    //}
                }
            }
        }

        static void Postfix(FVRViveHand hand, FVRPhysicalObject __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            //SetQuickBeltSlotPatch.dontProcessTotalWeight = false;
            //MeatovItem.parentChangeDontManageWeight = false;
        }
    }

    // Patches FVRPlayerHitbox.Damage(Damage) in order to implement our own health system
    class DamagePatch
    {
        static bool Prefix(Damage d, ref FVRPlayerHitbox __instance, ref AudioSource ___m_aud)
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }

            if (!GM.CurrentSceneSettings.DoesDamageGetRegistered)
            {
                return false;
            }
            if (d.Dam_Blinding > 0f && __instance.Type == FVRPlayerHitbox.PlayerHitBoxType.Head)
            {
                float num = Vector3.Angle(d.strikeDir, GM.CurrentPlayerBody.Head.forward);
                if (num > 90f)
                {
                    GM.CurrentPlayerBody.BlindPlayer(d.Dam_Blinding);
                }
            }
            if (GM.CurrentPlayerBody.IsBlort)
            {
                d.Dam_TotalEnergetic = 0f;
            }
            else if (GM.CurrentPlayerBody.IsDlort)
            {
                d.Dam_TotalEnergetic *= 3f;
            }
            float damage = d.Dam_TotalKinetic + d.Dam_TotalEnergetic;

            // Apply damage resist
            damage *= 0.05f; // For 500 to become 25

            Mod.LogInfo("Player took " + damage + " damage (Damage)");

            // Apply damage resist/multiplier based on equipment and body part
            object[] damageData = Damage(damage, __instance);

            float actualDamage = (float)damageData[1];
            if (actualDamage > 0.1f && __instance.IsActivated)
            {
                if (RegisterPlayerHit((int)damageData[0], actualDamage, false))
                {
                    ___m_aud.PlayOneShot(__instance.AudClip_Reset, 0.4f);
                }
                else if (!GM.IsDead())
                {
                    ___m_aud.PlayOneShot(__instance.AudClip_Hit, 0.4f);
                }
            }

            return false;
        }

        public static object[] Damage(float amount, FVRPlayerHitbox hitbox = null, int partIndex = -1)
        {
            if (hitbox == null && partIndex == -1)
            {
                Mod.LogError("Damage() called without hitbox nor partindex specified");
                return null;
            }

            // TODO: Apply drug multipliers

            int actualPartIndex = partIndex;
            float actualAmount = amount;
            float vitalityLevel = Mod.skills[2].currentProgress / 100;
            float bleedingChanceModifier = 0.012f * vitalityLevel;
            float healthLevel = Mod.skills[3].currentProgress / 100;
            float fractureChanceModifier = 0.012f * healthLevel;
            if (hitbox != null)
            {
                // Apply damage resist/multiplier based on equipment and body part
                if (hitbox.Type == FVRPlayerHitbox.PlayerHitBoxType.Head)
                {
                    Mod.LogInfo("\tTo Head");
                    if (Mod.GetHealth(0) <= 0)
                    {
                        Mod.LogInfo("\t\tHealth 0, killing player");
                        //Mod.currentRaidManager.KillPlayer();
                    }

                    actualPartIndex = 0;

                    // Add a headshot damage multiplier
                    //damage *= Mod.headshotDamageMultiplier;

                    // We will actually be applying normal damage to the head, considering if health <= 0 is instant death and it only has 35 HP

                    // Process damage resist from EFM_EquipmentSlot.CurrentHelmet
                    float heavyBleedChance = 0.05f - 0.05f * bleedingChanceModifier;
                    float lightBleedChance = 0.15f - 0.15f * bleedingChanceModifier;
                    if (EquipmentSlot.currentHeadwear != null && EquipmentSlot.currentHeadwear.armor > 0 && UnityEngine.Random.value <= EquipmentSlot.currentHeadwear.coverage)
                    {
                        heavyBleedChance /= 3;
                        lightBleedChance /= 3;
                        EquipmentSlot.currentHeadwear.armor -= actualAmount - actualAmount * EquipmentSlot.currentHeadwear.damageResist;
                        actualAmount *= EquipmentSlot.currentHeadwear.damageResist;
                    }

                    // TODO: Maybe we can check which ammo the sosig is using, and when we create the Damage object we pass here,
                    // we could set Cutting/Piercing to define bleed chance. Until then, every shot has chance of bleed.
                    float chance = UnityEngine.Random.value;
                    if (chance <= heavyBleedChance)
                    {
                        new Effect(Effect.EffectType.HeavyBleeding, 0, 0, 0, null, false, 0, -1, true);
                    }
                    else if (chance <= lightBleedChance)
                    {
                        new Effect(Effect.EffectType.LightBleeding, 0, 0, 0, null, false, 0, -1, true);
                    }
                }
                else if (hitbox.Type == FVRPlayerHitbox.PlayerHitBoxType.Torso)
                {
                    // These numbers are cumulative
                    float thoraxChance = 0.3f;
                    float stomachChance = 0.55f;
                    float rightArmChance = 0.625f;
                    float leftArmChance = 0.7f;
                    float rightLegChance = 0.85f;
                    float leftLegChance = 1f;

                    float partChance = UnityEngine.Random.value;
                    if (partChance >= 0 && partChance <= thoraxChance)
                    {
                        Mod.LogInfo("\tTo thorax");
                        if (Mod.GetHealth(1) <= 0)
                        {
                            Mod.LogInfo("\t\tHealth 0, killing player");
                            //Mod.currentRaidManager.KillPlayer();
                        }

                        actualPartIndex = 1;

                        // Process damage resist from EFM_EquipmentSlot.CurrentArmor
                        float heavyBleedChance = 0.05f - 0.05f * bleedingChanceModifier;
                        float lightBleedChance = 0.15f - 0.15f * bleedingChanceModifier;
                        if (EquipmentSlot.currentArmor != null && EquipmentSlot.currentArmor.armor > 0 && UnityEngine.Random.value <= EquipmentSlot.currentArmor.coverage)
                        {
                            heavyBleedChance /= 3;
                            lightBleedChance /= 3;
                            EquipmentSlot.currentArmor.armor -= actualAmount - actualAmount * EquipmentSlot.currentArmor.damageResist;
                            actualAmount *= EquipmentSlot.currentArmor.damageResist;
                        }

                        // TODO: Maybe we can check which ammo the sosig is using, and when we create the Damage object we pass here,
                        // we could set Cutting/Piercing to define bleed chance. Until then, every shot has chance of bleed.
                        float bleedValue = UnityEngine.Random.value;
                        if (bleedValue <= heavyBleedChance)
                        {
                            new Effect(Effect.EffectType.HeavyBleeding, 0, 0, 0, null, false, actualPartIndex, -1, true);
                        }
                        else if (bleedValue <= lightBleedChance)
                        {
                            new Effect(Effect.EffectType.LightBleeding, 0, 0, 0, null, false, actualPartIndex, -1, true);
                        }
                    }
                    else if (partChance > thoraxChance && partChance <= stomachChance)
                    {
                        Mod.LogInfo("\tTo stomach");
                        actualPartIndex = 2;

                        // Process damage resist from EFM_EquipmentSlot.CurrentArmor
                        float heavyBleedChance = 0.1f - 0.1f * bleedingChanceModifier;
                        float lightBleedChance = 0.25f - 0.25f * bleedingChanceModifier;
                        if (EquipmentSlot.currentArmor != null && EquipmentSlot.currentArmor.armor > 0 && UnityEngine.Random.value <= EquipmentSlot.currentArmor.coverage)
                        {
                            heavyBleedChance /= 3;
                            lightBleedChance /= 3;
                            EquipmentSlot.currentArmor.armor -= actualAmount - actualAmount * EquipmentSlot.currentArmor.damageResist;
                            actualAmount *= EquipmentSlot.currentArmor.damageResist;
                        }

                        // TODO: Maybe we can check which ammo the sosig is using, and when we create the Damage object we pass here,
                        // we could set Cutting/Piercing to define bleed chance. Until then, every shot has chance of bleed.
                        float bleedValue = UnityEngine.Random.value;
                        if (bleedValue <= heavyBleedChance)
                        {
                            new Effect(Effect.EffectType.HeavyBleeding, 0, 0, 0, null, false, actualPartIndex, -1, true);
                        }
                        else if (bleedValue <= lightBleedChance)
                        {
                            new Effect(Effect.EffectType.LightBleeding, 0, 0, 0, null, false, actualPartIndex, -1, true);
                        }
                    }
                    else if (partChance > stomachChance && partChance <= rightArmChance)
                    {
                        Mod.LogInfo("\tTo right arm");
                        actualPartIndex = 4;

                        float heavyBleedChance = 0.05f - 0.05f * bleedingChanceModifier;
                        float lightBleedChance = 0.15f - 0.15f * bleedingChanceModifier;
                        float fractureChance = 0.02f - 0.02f * fractureChanceModifier;

                        // TODO: Maybe we can check which ammo the sosig is using, and when we create the Damage object we pass here,
                        // we could set Cutting/Piercing to define bleed chance. Until then, every shot has chance of bleed.
                        float bleedValue = UnityEngine.Random.value;
                        if (bleedValue <= heavyBleedChance)
                        {
                            new Effect(Effect.EffectType.HeavyBleeding, 0, 0, 0, null, false, actualPartIndex, -1, true);
                        }
                        else if (bleedValue <= lightBleedChance)
                        {
                            new Effect(Effect.EffectType.LightBleeding, 0, 0, 0, null, false, actualPartIndex, -1, true);
                        }

                        if (UnityEngine.Random.value < fractureChance)
                        {
                            new Effect(Effect.EffectType.Fracture, 0, 0, 0, null, false, actualPartIndex, -1, true);
                        }
                    }
                    else if (partChance > rightArmChance && partChance <= leftArmChance)
                    {
                        Mod.LogInfo("\tTo left arm");
                        actualPartIndex = 3;

                        float heavyBleedChance = 0.05f - 0.05f * bleedingChanceModifier;
                        float lightBleedChance = 0.15f - 0.15f * bleedingChanceModifier;
                        float fractureChance = 0.02f - 0.02f * fractureChanceModifier;

                        // TODO: Maybe we can check which ammo the sosig is using, and when we create the Damage object we pass here,
                        // we could set Cutting/Piercing to define bleed chance. Until then, every shot has chance of bleed.
                        float bleedValue = UnityEngine.Random.value;
                        if (bleedValue <= heavyBleedChance)
                        {
                            new Effect(Effect.EffectType.HeavyBleeding, 0, 0, 0, null, false, actualPartIndex, -1, true);
                        }
                        else if (bleedValue <= lightBleedChance)
                        {
                            new Effect(Effect.EffectType.LightBleeding, 0, 0, 0, null, false, actualPartIndex, -1, true);
                        }

                        if (UnityEngine.Random.value < fractureChance)
                        {
                            new Effect(Effect.EffectType.Fracture, 0, 0, 0, null, false, actualPartIndex, -1, true);
                        }
                    }
                    else if (partChance > leftArmChance && partChance <= rightLegChance)
                    {
                        Mod.LogInfo("\tTo right leg");
                        actualPartIndex = 6;

                        float heavyBleedChance = 0.05f - 0.05f * bleedingChanceModifier;
                        float lightBleedChance = 0.15f - 0.15f * bleedingChanceModifier;
                        float fractureChance = 0.02f - 0.02f * fractureChanceModifier;

                        // TODO: Maybe we can check which ammo the sosig is using, and when we create the Damage object we pass here,
                        // we could set Cutting/Piercing to define bleed chance. Until then, every shot has chance of bleed.
                        float bleedValue = UnityEngine.Random.value;
                        if (bleedValue <= heavyBleedChance)
                        {
                            new Effect(Effect.EffectType.HeavyBleeding, 0, 0, 0, null, false, actualPartIndex, -1, true);
                        }
                        else if (bleedValue <= lightBleedChance)
                        {
                            new Effect(Effect.EffectType.LightBleeding, 0, 0, 0, null, false, actualPartIndex, -1, true);
                        }

                        if (UnityEngine.Random.value < fractureChance)
                        {
                            new Effect(Effect.EffectType.Fracture, 0, 0, 0, null, false, actualPartIndex, -1, true);
                        }
                    }
                    else if (partChance > rightLegChance && partChance <= leftLegChance)
                    {
                        Mod.LogInfo("\tTo left leg");
                        actualPartIndex = 5;

                        float heavyBleedChance = 0.05f - 0.05f * bleedingChanceModifier;
                        float lightBleedChance = 0.15f - 0.15f * bleedingChanceModifier;
                        float fractureChance = 0.02f - 0.02f * fractureChanceModifier;

                        // TODO: Maybe we can check which ammo the sosig is using, and when we create the Damage object we pass here,
                        // we could set Cutting/Piercing to define bleed chance. Until then, every shot has chance of bleed.
                        float bleedValue = UnityEngine.Random.value;
                        if (bleedValue <= heavyBleedChance)
                        {
                            new Effect(Effect.EffectType.HeavyBleeding, 0, 0, 0, null, false, actualPartIndex, -1, true);
                        }
                        else if (bleedValue <= lightBleedChance)
                        {
                            new Effect(Effect.EffectType.LightBleeding, 0, 0, 0, null, false, actualPartIndex, -1, true);
                        }

                        if (UnityEngine.Random.value < fractureChance)
                        {
                            new Effect(Effect.EffectType.Fracture, 0, 0, 0, null, false, actualPartIndex, -1, true);
                        }
                    }
                }
                else if (hitbox.Type == FVRPlayerHitbox.PlayerHitBoxType.Hand)
                {
                    actualPartIndex = hitbox.Hand.IsThisTheRightHand ? 3 : 4;
                    Mod.LogInfo("\tTo hand, actual part: " + actualPartIndex);

                    // Add a damage resist because should do less damage when hit to hand than when hit to torso
                    //damage *= Mod.handDamageResist;

                    float heavyBleedChance = 0.05f - 0.05f * bleedingChanceModifier;
                    float lightBleedChance = 0.15f - 0.15f * bleedingChanceModifier;
                    float fractureChance = 0.02f - 0.02f * fractureChanceModifier;

                    // TODO: Maybe we can check which ammo the sosig is using, and when we create the Damage object we pass here,
                    // we could set Cutting/Piercing to define bleed chance. Until then, every shot has chance of bleed.
                    float bleedValue = UnityEngine.Random.value;
                    if (bleedValue <= heavyBleedChance)
                    {
                        new Effect(Effect.EffectType.HeavyBleeding, 0, 0, 0, null, false, actualPartIndex, -1, true);
                    }
                    else if (bleedValue <= lightBleedChance)
                    {
                        new Effect(Effect.EffectType.LightBleeding, 0, 0, 0, null, false, actualPartIndex, -1, true);
                    }

                    if (UnityEngine.Random.value < fractureChance)
                    {
                        new Effect(Effect.EffectType.Fracture, 0, 0, 0, null, false, actualPartIndex, -1, true);
                    }
                }
            }
            else
            {
                switch (actualPartIndex)
                {
                    case 0: // Head
                        if (Mod.GetHealth(0) <= 0)
                        {
                            //Mod.currentRaidManager.KillPlayer();
                        }

                        // Process damage resist from EFM_EquipmentSlot.CurrentHelmet
                        float heavyBleedChance0 = 0.05f - 0.05f * bleedingChanceModifier;
                        float lightBleedChance0 = 0.15f - 0.15f * bleedingChanceModifier;
                        if (EquipmentSlot.currentHeadwear != null && EquipmentSlot.currentHeadwear.armor > 0 && UnityEngine.Random.value <= EquipmentSlot.currentHeadwear.coverage)
                        {
                            heavyBleedChance0 /= 3;
                            lightBleedChance0 /= 3;
                            EquipmentSlot.currentHeadwear.armor -= actualAmount - actualAmount * EquipmentSlot.currentHeadwear.damageResist;
                            actualAmount *= EquipmentSlot.currentHeadwear.damageResist;
                        }

                        // Apply possible effects
                        float bleedValue0 = UnityEngine.Random.value;
                        if (bleedValue0 <= heavyBleedChance0)
                        {
                            new Effect(Effect.EffectType.HeavyBleeding, 0, 0, 0, null, false, 0, -1, true);
                        }
                        else if (bleedValue0 <= lightBleedChance0)
                        {
                            new Effect(Effect.EffectType.LightBleeding, 0, 0, 0, null, false, 0, -1, true);
                        }
                        break;
                    case 1: // Thorax
                        if (Mod.GetHealth(1) <= 0)
                        {
                            //Mod.currentRaidManager.KillPlayer();
                        }

                        // Process damage resist from EFM_EquipmentSlot.CurrentArmor
                        float heavyBleedChance1 = 0.05f - 0.05f * bleedingChanceModifier;
                        float lightBleedChance1 = 0.15f - 0.15f * bleedingChanceModifier;
                        if (EquipmentSlot.currentArmor != null && EquipmentSlot.currentArmor.armor > 0 && UnityEngine.Random.value <= EquipmentSlot.currentArmor.coverage)
                        {
                            heavyBleedChance1 /= 3;
                            lightBleedChance1 /= 3;
                            EquipmentSlot.currentArmor.armor -= actualAmount - actualAmount * EquipmentSlot.currentArmor.damageResist;
                            actualAmount *= EquipmentSlot.currentArmor.damageResist;
                        }

                        // Apply possible effects
                        float bleedValue1 = UnityEngine.Random.value;
                        if (bleedValue1 <= heavyBleedChance1)
                        {
                            new Effect(Effect.EffectType.HeavyBleeding, 0, 0, 0, null, false, actualPartIndex, -1, true);
                        }
                        else if (bleedValue1 <= lightBleedChance1)
                        {
                            new Effect(Effect.EffectType.LightBleeding, 0, 0, 0, null, false, actualPartIndex, -1, true);
                        }
                        break;
                    case 2: // Stomach
                        // Process damage resist from EFM_EquipmentSlot.CurrentArmor
                        float heavyBleedChance2 = 0.1f - 0.1f * bleedingChanceModifier;
                        float lightBleedChance2 = 0.25f - 0.25f * bleedingChanceModifier;
                        if (EquipmentSlot.currentArmor != null && EquipmentSlot.currentArmor.armor > 0 && UnityEngine.Random.value <= EquipmentSlot.currentArmor.coverage)
                        {
                            heavyBleedChance2 /= 3;
                            lightBleedChance2 /= 3;
                            EquipmentSlot.currentArmor.armor -= actualAmount - actualAmount * EquipmentSlot.currentArmor.damageResist;
                            actualAmount *= EquipmentSlot.currentArmor.damageResist;
                        }

                        // Apply possible effects
                        float bleedValue2 = UnityEngine.Random.value;
                        if (bleedValue2 <= heavyBleedChance2)
                        {
                            new Effect(Effect.EffectType.HeavyBleeding, 0, 0, 0, null, false, actualPartIndex, -1, true);
                        }
                        else if (bleedValue2 <= lightBleedChance2)
                        {
                            new Effect(Effect.EffectType.LightBleeding, 0, 0, 0, null, false, actualPartIndex, -1, true);
                        }
                        break;
                    case 3: // Left arm
                    case 4: // Right arm
                        float heavyBleedChanceArm = 0.05f - 0.05f * bleedingChanceModifier;
                        float lightBleedChanceArm = 0.15f - 0.15f * bleedingChanceModifier;
                        float fractureChanceArm = 0.02f - 0.02f * fractureChanceModifier;

                        // Apply possible effects
                        float bleedValue3 = UnityEngine.Random.value;
                        if (bleedValue3 <= heavyBleedChanceArm)
                        {
                            new Effect(Effect.EffectType.HeavyBleeding, 0, 0, 0, null, false, actualPartIndex, -1, true);
                        }
                        else if (bleedValue3 <= lightBleedChanceArm)
                        {
                            new Effect(Effect.EffectType.LightBleeding, 0, 0, 0, null, false, actualPartIndex, -1, true);
                        }

                        if (UnityEngine.Random.value < fractureChanceArm)
                        {
                            new Effect(Effect.EffectType.Fracture, 0, 0, 0, null, false, actualPartIndex, -1, true);
                        }
                        break;
                    case 5:
                    case 6:
                        float heavyBleedChanceLeg = 0.05f - 0.05f * bleedingChanceModifier;
                        float lightBleedChanceLeg = 0.15f - 0.15f * bleedingChanceModifier;
                        float fractureChanceLeg = 0.02f - 0.02f * fractureChanceModifier;

                        // Apply possible effects
                        float bleedValueLeg = UnityEngine.Random.value;
                        if (bleedValueLeg <= heavyBleedChanceLeg)
                        {
                            new Effect(Effect.EffectType.HeavyBleeding, 0, 0, 0, null, false, actualPartIndex, -1, true);
                        }
                        else if (bleedValueLeg <= lightBleedChanceLeg)
                        {
                            new Effect(Effect.EffectType.LightBleeding, 0, 0, 0, null, false, actualPartIndex, -1, true);
                        }

                        if (UnityEngine.Random.value < fractureChanceLeg)
                        {
                            new Effect(Effect.EffectType.Fracture, 0, 0, 0, null, false, actualPartIndex, -1, true);
                        }
                        break;
                }
            }

            return new object[] { actualPartIndex, actualAmount };
        }

        public static bool RegisterPlayerHit(int partIndex, float totalDamage, bool FromSelf)
        {
            if (GM.CurrentSceneSettings.DoesDamageGetRegistered && !GM.IsDead())
            {
                Mod.AddSkillExp(Skill.damageTakenAction * totalDamage, 2);

                GM.CurrentPlayerBody.HitEffect();

                // Parts other than head and thorax at zero distribute damage over all other parts
                float[] destroyedMultiplier = new float[] { 0, 0, 1.5f, 0.7f, 0.7f, 1, 1 };
                float actualTotalDamage = 0;
                if (partIndex >= 2)
                {
                    if (Mod.GetHealth(partIndex) <= 0)
                    {
                        for (int i = 0; i < Mod.GetHealthCount(); ++i)
                        {
                            if (i != partIndex)
                            {
                                float actualDamage = Mathf.Min(totalDamage / 6 * destroyedMultiplier[partIndex], Mod.GetHealth(i));
                                Mod.SetHealth(i, Mathf.Clamp(Mod.GetHealth(i) - actualDamage, 0, Mod.GetCurrentMaxHealth(i)));
                                actualTotalDamage += actualDamage;

                                if (i == 0 || i == 1)
                                {
                                    if (Mod.GetHealth(0) <= 0 || Mod.GetHealth(1) <= 0)
                                    {
                                        if(RaidManager.instance != null)
                                        {
                                            RaidManager.instance.EndRaid(RaidManager.RaidStatus.KIA);
                                        }
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        actualTotalDamage = totalDamage;
                        Mod.SetHealth(partIndex, Mathf.Clamp(Mod.GetHealth(partIndex) - totalDamage, 0, Mod.GetCurrentMaxHealth(partIndex)));
                    }
                }
                else if (Mod.GetHealth(partIndex) <= 0) // Part is head or thorax, destroyed
                {
                    if (RaidManager.instance != null)
                    {
                        RaidManager.instance.EndRaid(RaidManager.RaidStatus.KIA);
                    }
                    return true;
                }
                else // Part is head or thorax, not yet destroyed
                {
                    actualTotalDamage = totalDamage;
                    Mod.SetHealth(partIndex, Mathf.Clamp(Mod.GetHealth(partIndex) - totalDamage, 0, Mod.GetCurrentMaxHealth(partIndex)));
                }
                GM.CurrentSceneSettings.OnPlayerTookDamage(actualTotalDamage / 440f);

                float totalHealth = 0;
                for (int i = 0; i < Mod.GetHealthCount(); ++i)
                {
                    totalHealth += Mod.GetHealth(i);
                }
                if (totalHealth <= 0f)
                {
                    if (RaidManager.instance != null)
                    {
                        RaidManager.instance.EndRaid(RaidManager.RaidStatus.KIA);
                    }
                    return true;
                }
            }
            return false;
        }
    }

    // Patches FVRPlayerHitbox.Damage(float) in order to implement our own health system
    class DamageFloatPatch
    {
        static bool Prefix(float i, ref FVRPlayerHitbox __instance, ref AudioSource ___m_aud)
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }

            if (!GM.CurrentSceneSettings.DoesDamageGetRegistered)
            {
                return false;
            }
            float damage = Mathf.Clamp(i * __instance.DamageMultiplier - __instance.DamageResist, 0f, 10000f);

            // Apply damage resist
            damage *= 0.05f; // For 500 to become 25

            Mod.LogInfo("Player took " + damage + " damage (float)");

            // Apply damage resist/multiplier based on equipment and body part
            object[] damageData = DamagePatch.Damage(damage, __instance);

            float actualDamage = (float)damageData[1];
            if (actualDamage > 0.1f && __instance.IsActivated)
            {
                if (DamagePatch.RegisterPlayerHit((int)damageData[0], actualDamage, false))
                {
                    ___m_aud.PlayOneShot(__instance.AudClip_Reset, 0.4f);
                }
                else if (!GM.IsDead())
                {
                    ___m_aud.PlayOneShot(__instance.AudClip_Hit, 0.4f);
                }
            }

            return false;
        }
    }

    // Patches FVRPlayerHitbox.Damage(DamageDealt) in order to implement our own health system
    class DamageDealtPatch
    {
        static bool Prefix(DamageDealt dam, FVRPlayerHitbox __instance, AudioSource ___m_aud)
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }

            if (!GM.CurrentSceneSettings.DoesDamageGetRegistered)
            {
                return false;
            }
            float damage = Mathf.Clamp(dam.PointsDamage * __instance.DamageMultiplier - __instance.DamageResist, 0f, 10000f);

            // Apply damage resist
            damage *= 0.05f; // For 500 to become 25

            Mod.LogInfo("Player took " + damage + " damage (Dealt)");

            // Apply damage resist/multiplier based on equipment and body part
            object[] damageData = DamagePatch.Damage(damage, __instance);

            float actualDamage = (float)damageData[1];
            if (actualDamage > 0.1f && __instance.IsActivated)
            {
                if (DamagePatch.RegisterPlayerHit((int)damageData[0], actualDamage, false))
                {
                    ___m_aud.PlayOneShot(__instance.AudClip_Reset, 0.4f);
                }
                else if (!GM.IsDead())
                {
                    ___m_aud.PlayOneShot(__instance.AudClip_Hit, 0.4f);
                }
            }

            return false;
        }
    }

    // Patches FVRViveHand.TestCollider() in order to be able to have interactable colliders on other gameobjects than the root
    // by use of OtherInteractable
    // This completely replaces the original 
    class HandTestColliderPatch
    {
        static bool Prefix(Collider collider, bool isEnter, bool isPalm, ref FVRViveHand.HandState ___m_state, ref bool ___m_isClosestInteractableInPalm, ref FVRViveHand __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }

            FVRInteractiveObject interactiveObject = collider.gameObject.GetComponent<FVRInteractiveObject>();
            OtherInteractable otherInteractable = collider.gameObject.GetComponent<OtherInteractable>();

            FVRInteractiveObject interactiveObjectToUse = otherInteractable != null ? otherInteractable.interactiveObject : interactiveObject;

            // Could be an interactable layer object without FVRInteractiveObject attached, if so we skip
            // For example, backpacks' MainContainer
            if (interactiveObjectToUse == null)
            {
                return false;
            }

            if (isEnter)
            {
                FVRInteractiveObject component = interactiveObjectToUse;
                component.Poke(__instance);
                return false;
            }

            if (___m_state == FVRViveHand.HandState.Empty && interactiveObjectToUse != null)
            {
                if (interactiveObjectToUse != null && interactiveObjectToUse.IsInteractable() && !interactiveObjectToUse.IsSelectionRestricted())
                {
                    float num = Vector3.Distance(interactiveObjectToUse.transform.position, __instance.Display_InteractionSphere.transform.position);
                    float num2 = Vector3.Distance(interactiveObjectToUse.transform.position, __instance.Display_InteractionSphere_Palm.transform.position);
                    if (__instance.ClosestPossibleInteractable == null)
                    {
                        __instance.ClosestPossibleInteractable = interactiveObjectToUse;
                        if (num < num2)
                        {
                            ___m_isClosestInteractableInPalm = false;
                        }
                        else
                        {
                            ___m_isClosestInteractableInPalm = true;
                        }
                    }
                    else if (__instance.ClosestPossibleInteractable != interactiveObjectToUse)
                    {
                        float num3 = Vector3.Distance(__instance.ClosestPossibleInteractable.transform.position, __instance.Display_InteractionSphere.transform.position);
                        float num4 = Vector3.Distance(__instance.ClosestPossibleInteractable.transform.position, __instance.Display_InteractionSphere_Palm.transform.position);
                        bool flag = true;
                        if (num < num2)
                        {
                            flag = false;
                        }
                        if (flag && num2 < num4 && ___m_isClosestInteractableInPalm)
                        {
                            ___m_isClosestInteractableInPalm = true;
                            __instance.ClosestPossibleInteractable = interactiveObjectToUse;
                        }
                        else if (!flag && num < num3)
                        {
                            ___m_isClosestInteractableInPalm = false;
                            __instance.ClosestPossibleInteractable = interactiveObjectToUse;
                        }
                    }
                }
            }

            return false;
        }
    }

    // Patches FVRViveHand.HandTriggerExit() in order to be able to have interactable colliders on other gameobjects than the root
    // This completely replaces the original 
    class HandTriggerExitPatch
    {
        static bool Prefix(Collider collider, ref FVRViveHand __instance, ref bool ___m_isClosestInteractableInPalm)
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }

            FVRInteractiveObject interactiveObject = collider.gameObject.GetComponent<FVRInteractiveObject>();
            OtherInteractable otherInteractable = collider.gameObject.GetComponent<OtherInteractable>();
            FVRInteractiveObject interactiveObjectToUse = otherInteractable != null ? otherInteractable.interactiveObject : interactiveObject;

            if (interactiveObjectToUse != null)
            {
                FVRInteractiveObject component = interactiveObjectToUse;
                if (__instance.ClosestPossibleInteractable == component)
                {
                    __instance.ClosestPossibleInteractable = null;
                    ___m_isClosestInteractableInPalm = false;
                }
            }

            return false;
        }
    }

    // Patches FVRInteractiveObject.SetAllCollidersToLayer to make sure it doesn't set the layer of GOs with layer already set to NonBlockingSmoke
    // because layer is used by open backpacks and rigs in order to prevent items from colliding with them so its easier to put items in the container
    // This completely replaces the original
    class InteractiveSetAllCollidersToLayerPatch
    {
        static bool Prefix(bool triggersToo, string layerName, ref Collider[] ___m_colliders, ref FVRInteractiveObject __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }

            if (triggersToo)
            {
                foreach (Collider collider in ___m_colliders)
                {
                    if (collider != null)
                    {
                        collider.gameObject.layer = LayerMask.NameToLayer(layerName);
                    }
                }
            }
            else
            {
                int nonBlockingSmokeLayer = LayerMask.NameToLayer("NonBlockingSmoke");
                foreach (Collider collider2 in ___m_colliders)
                {
                    // Also check current layer so we dont set it to default if NonBlockingSmoke
                    if (collider2 != null && !collider2.isTrigger && collider2.gameObject.layer != nonBlockingSmokeLayer)
                    {
                        collider2.gameObject.layer = LayerMask.NameToLayer(layerName);
                    }
                }
            }

            return false;
        }
    }

    // Patches FVRViveHand.Update to add description input
    // Completely replaces the original
    // Note that this will override H3MP's transpiler, but the transpiler only prevent TNH spectators from using grab laser
    class HandUpdatePatch
    {
        static bool Prefix(FVRViveHand __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }


            if (__instance.m_initState == FVRViveHand.HandInitializationState.Uninitialized)
            {
                return false;
            }
            if (__instance.m_selectedObj != null && __instance.m_selectedObj.IsHeld)
            {
                __instance.m_selectedObj = null;
                __instance.m_reset = 0f;
                __instance.m_isObjectInTransit = false;
            }
            if (__instance.m_reset >= 0f && __instance.m_isObjectInTransit)
            {
                if (__instance.m_selectedObj != null && Vector3.Distance(__instance.m_selectedObj.transform.position, __instance.transform.position) < 0.4f)
                {
                    Vector3 b = __instance.transform.position - __instance.m_selectedObj.transform.position;
                    Vector3 vector = Vector3.Lerp(__instance.m_selectedObj.RootRigidbody.velocity, b, Time.deltaTime * 2f);
                    __instance.m_selectedObj.RootRigidbody.velocity = Vector3.ClampMagnitude(vector, __instance.m_selectedObj.RootRigidbody.velocity.magnitude);
                    __instance.m_selectedObj.RootRigidbody.velocity = vector;
                    __instance.m_selectedObj.RootRigidbody.drag = 1f;
                    __instance.m_selectedObj.RootRigidbody.angularDrag = 8f;
                    __instance.m_reset -= Time.deltaTime * 0.4f;
                }
                else
                {
                    __instance.m_reset -= Time.deltaTime;
                }
                if (__instance.m_reset <= 0f)
                {
                    __instance.m_isObjectInTransit = false;
                    if (__instance.m_selectedObj != null)
                    {
                        __instance.m_selectedObj.RecoverDrag();
                        __instance.m_selectedObj = null;
                    }
                }
            }
            __instance.HapticBuzzUpdate();
            __instance.TestQuickBeltDistances();
            __instance.PollInput();
            if (__instance.m_hasOverrider && __instance.m_overrider != null)
            {
                __instance.m_overrider.Process(ref __instance.Input);
            }
            else
            {
                __instance.m_hasOverrider = false;
            }
            if (!(__instance.m_currentInteractable != null) || __instance.Input.TriggerPressed)
            {
            }
            if (__instance.ClosestPossibleInteractable != null && !__instance.ClosestPossibleInteractable.IsInteractable())
            {
                __instance.ClosestPossibleInteractable = null;
            }
            if (__instance.ClosestPossibleInteractable == null)
            {
                if (__instance.m_touchSphereMatInteractable)
                {
                    __instance.m_touchSphereMatInteractable = false;
                    __instance.TouchSphere.material = __instance.TouchSphereMat_NoInteractable;
                }
                if (__instance.m_touchSphereMatInteractablePalm)
                {
                    __instance.m_touchSphereMatInteractablePalm = false;
                    __instance.TouchSphere_Palm.material = __instance.TouchSphereMat_NoInteractable;
                }
            }
            else if (!__instance.m_touchSphereMatInteractable && !__instance.m_isClosestInteractableInPalm)
            {
                __instance.m_touchSphereMatInteractable = true;
                __instance.TouchSphere.material = __instance.TouchSpheteMat_Interactable;
                __instance.m_touchSphereMatInteractablePalm = false;
                __instance.TouchSphere_Palm.material = __instance.TouchSphereMat_NoInteractable;
            }
            else if (!__instance.m_touchSphereMatInteractablePalm && __instance.m_isClosestInteractableInPalm)
            {
                __instance.m_touchSphereMatInteractablePalm = true;
                __instance.TouchSphere_Palm.material = __instance.TouchSpheteMat_Interactable;
                __instance.m_touchSphereMatInteractable = false;
                __instance.TouchSphere.material = __instance.TouchSphereMat_NoInteractable;
            }
            float d = 1f / GM.CurrentPlayerBody.transform.localScale.x;
            ////////////////////////////////////////////////////////////////////////////
            // Start patch
            if ((Mod.stackSplitUI == null || !Mod.stackSplitUI.gameObject.activeSelf) && __instance.m_state == FVRViveHand.HandState.Empty && !__instance.Input.BYButtonPressed && !__instance.Input.TouchpadPressed && __instance.ClosestPossibleInteractable == null && __instance.CurrentHoveredQuickbeltSlot == null && __instance.CurrentInteractable == null && !__instance.m_isWristMenuActive)
            {
                if (Physics.Raycast(__instance.Input.OneEuroPointingPos, __instance.Input.OneEuroPointRotation * Vector3.forward, out __instance.m_pointingHit, GM.CurrentSceneSettings.MaxPointingDistance, __instance.PointingLayerMask, QueryTriggerInteraction.Collide) && __instance.m_pointingHit.collider.gameObject.GetComponent<FVRPointable>())
                {
                    FVRPointable component = __instance.m_pointingHit.collider.gameObject.GetComponent<FVRPointable>();
                    if (__instance.m_pointingHit.distance <= component.MaxPointingRange)
                    {
                        __instance.CurrentPointable = component;
                        __instance.PointingLaser.position = __instance.Input.OneEuroPointingPos;
                        __instance.PointingLaser.rotation = __instance.Input.OneEuroPointRotation;
                        __instance.PointingLaser.localScale = new Vector3(0.002f, 0.002f, __instance.m_pointingHit.distance) * d;
                        __instance.CurrentPointable.SetLastPointHitWorldPos(__instance.m_pointingHit.point);
                    }
                    else
                    {
                        __instance.CurrentPointable = null;
                    }
                }
                else
                {
                    __instance.CurrentPointable = null;
                }
            }
            else
            {
                __instance.CurrentPointable = null;
            }
            __instance.MovementManager.UpdateMovementWithHand(__instance);
            if (__instance.MovementManager.ShouldFlushTouchpad(__instance))
            {
                __instance.FlushTouchpadData();
            }

            bool flag;
            bool flag2;
            bool descriptionInput = false;
            if (__instance.IsInStreamlinedMode)
            {
                flag = __instance.Input.BYButtonDown;
                flag2 = __instance.Input.BYButtonPressed;

                // This should work for both Index and Oculus, the only two CModes that can use streamlined
                descriptionInput = __instance.Input.TouchpadTouched && __instance.Input.TouchpadAxes.y < 0;
            }
            else
            {
                flag = __instance.Input.TouchpadDown;
                flag2 = __instance.Input.TouchpadPressed;

                // As it stands, Vive and WMR will not have a dedicated description input
                // To get a description of an item with those controllers, the item must not be held, and instead pointed with grab laser
                switch (__instance.CMode)
                {
                    case ControlMode.Index:
                    case ControlMode.Oculus:
                        descriptionInput = __instance.Input.AXButtonPressed;
                        break;
                    case ControlMode.Vive:
                    case ControlMode.WMR:
                        descriptionInput = false;
                        break;
                }
            }
            if (__instance.m_state == FVRViveHand.HandState.Empty && __instance.CurrentHoveredQuickbeltSlot == null)
            {
                if (flag2 || descriptionInput)
                {
                    if (!__instance.GrabLaser.gameObject.activeSelf)
                    {
                        __instance.GrabLaser.gameObject.SetActive(true);
                    }
                    bool flag3 = false;
                    FVRPhysicalObject fvrphysicalObject = null;
                    IDescribable describable = null;
                    if (Physics.Raycast(__instance.Input.OneEuroPointingPos, __instance.Input.OneEuroPointRotation * Vector3.forward, out __instance.m_grabHit, 3f, __instance.GrabLaserMask, QueryTriggerInteraction.Collide))
                    {
                        // Describable will not necessarily have a rigidbody and physicalObject script, so check for IDescribable before
                        describable = __instance.m_grabHit.collider.GetComponentInParents<IDescribable>();
                        if (describable != null)
                        {
                            if (__instance.IsThisTheRightHand)
                            {
                                Mod.rightHand.SetDescribable(describable);
                            }
                            else
                            {
                                Mod.leftHand.SetDescribable(describable);
                            }
                        }

                        if (__instance.m_grabHit.collider.attachedRigidbody != null && __instance.m_grabHit.collider.attachedRigidbody.gameObject.GetComponent<FVRPhysicalObject>())
                        {
                            fvrphysicalObject = __instance.m_grabHit.collider.attachedRigidbody.gameObject.GetComponent<FVRPhysicalObject>();
                            if (fvrphysicalObject != null && !fvrphysicalObject.IsHeld && fvrphysicalObject.IsDistantGrabbable())
                            {
                                flag3 = true;
                            }

                        }
                        __instance.GrabLaser.localScale = new Vector3(0.004f, 0.004f, __instance.m_grabHit.distance) * d;
                    }
                    else
                    {
                        __instance.GrabLaser.localScale = new Vector3(0.004f, 0.004f, 3f) * d;
                    }
                    __instance.GrabLaser.position = __instance.Input.OneEuroPointingPos;
                    __instance.GrabLaser.rotation = __instance.Input.OneEuroPointRotation;
                    if (flag3)
                    {
                        if (!__instance.BlueLaser.activeSelf)
                        {
                            __instance.BlueLaser.SetActive(true);
                        }
                        if (__instance.RedLaser.activeSelf)
                        {
                            __instance.RedLaser.SetActive(false);
                        }
                        if (__instance.Input.IsGrabDown && fvrphysicalObject != null)
                        {
                            __instance.RetrieveObject(fvrphysicalObject);
                            if (__instance.GrabLaser.gameObject.activeSelf)
                            {
                                __instance.GrabLaser.gameObject.SetActive(false);
                            }
                        }
                    }
                    else
                    {
                        if (__instance.BlueLaser.activeSelf)
                        {
                            __instance.BlueLaser.SetActive(false);
                        }
                        if (!__instance.RedLaser.activeSelf)
                        {
                            __instance.RedLaser.SetActive(true);
                        }
                    }

                    // Update description visibility
                    Hand handToUse = __instance.IsThisTheRightHand ? Mod.rightHand : Mod.leftHand;
                    if (handToUse != null && handToUse.description != null)
                    {
                        if(describable == null)
                        {
                            if (handToUse.description.gameObject.activeSelf)
                            {
                                handToUse.description.gameObject.SetActive(false);

                                // We set currentDescribable to null on deactivation
                                // This will cause describable's GetDescriptionPack to be called every time description 
                                // is reactivated. The point is that if an itemView's item changes but it was already our previous
                                // describable, the description pack will be refetched when we get description again
                                handToUse.currentDescribable = null;
                            }
                        }
                        else
                        {
                            if (!handToUse.description.gameObject.activeSelf)
                            {
                                handToUse.description.gameObject.SetActive(true);
                            }
                        }
                    }
                }
                else // No laser or description input
                {
                    if (__instance.GrabLaser.gameObject.activeSelf)
                    {
                        __instance.GrabLaser.gameObject.SetActive(false);
                    }

                    // Also disable description if necessary
                    // Note, we check for nullity on hand because this will be state of hand by default before we go into Meatov
                    // So hands will not exist yet
                    Hand hand = __instance.IsThisTheRightHand ? Mod.rightHand : Mod.leftHand;
                    if (hand != null && hand.description != null && hand.description.gameObject.activeSelf)
                    {
                        hand.description.gameObject.SetActive(false);
                        hand.currentDescribable = null;
                    }
                }
            }
            else
            {
                if (__instance.GrabLaser.gameObject.activeSelf)
                {
                    __instance.GrabLaser.gameObject.SetActive(false);
                }

                // If hand is holding something, we want to activate description if have description input
                Hand hand = __instance.IsThisTheRightHand ? Mod.rightHand : Mod.leftHand;
                if (hand != null)
                {
                    if (__instance.m_state == FVRViveHand.HandState.GripInteracting)
                    {
                        if (descriptionInput)
                        {
                            if (hand.heldItem != null)
                            {
                                hand.SetDescribable(hand.heldItem);
                                if (!hand.description.gameObject.activeSelf)
                                {
                                    hand.description.gameObject.SetActive(true);
                                }
                            }
                        }
                        else if (hand.description != null && hand.description.gameObject.activeSelf)
                        {
                            hand.description.gameObject.SetActive(false);
                            hand.currentDescribable = null;
                        }
                    }
                    else if (hand.description != null && hand.description.gameObject.activeSelf) // Not holding anything, no grab laser, deactivate description
                    {
                        hand.description.gameObject.SetActive(false);
                        hand.currentDescribable = null;
                    }
                }
            }
            if (__instance.Mode == FVRViveHand.HandMode.Neutral && __instance.m_state == FVRViveHand.HandState.Empty && flag)
            {
                bool isSpawnLockingEnabled = GM.CurrentSceneSettings.IsSpawnLockingEnabled;
                if (__instance.ClosestPossibleInteractable != null && __instance.ClosestPossibleInteractable is FVRPhysicalObject)
                {
                    FVRPhysicalObject fvrphysicalObject2 = __instance.ClosestPossibleInteractable as FVRPhysicalObject;
                    if (((fvrphysicalObject2.SpawnLockable && isSpawnLockingEnabled) || fvrphysicalObject2.Harnessable) && fvrphysicalObject2.QuickbeltSlot != null)
                    {
                        fvrphysicalObject2.ToggleQuickbeltState();
                    }
                }
                else if (__instance.CurrentHoveredQuickbeltSlot != null && __instance.CurrentHoveredQuickbeltSlot.HeldObject != null)
                {
                    FVRPhysicalObject fvrphysicalObject3 = __instance.CurrentHoveredQuickbeltSlot.HeldObject as FVRPhysicalObject;
                    if ((fvrphysicalObject3.SpawnLockable && isSpawnLockingEnabled) || fvrphysicalObject3.Harnessable)
                    {
                        fvrphysicalObject3.ToggleQuickbeltState();
                    }
                }
            }
            __instance.UpdateGrabityDisplay();
            if (__instance.Mode == FVRViveHand.HandMode.Neutral)
            {
                if (__instance.m_state == FVRViveHand.HandState.Empty)
                {
                    bool flag4 = false;
                    if (__instance.Input.IsGrabDown)
                    {
                        if (__instance.CurrentHoveredQuickbeltSlot != null && __instance.CurrentHoveredQuickbeltSlot.CurObject != null)
                        {
                            __instance.CurrentInteractable = __instance.CurrentHoveredQuickbeltSlot.CurObject;
                            __instance.m_state = FVRViveHand.HandState.GripInteracting;
                            __instance.CurrentInteractable.BeginInteraction(__instance);
                            __instance.Buzz(__instance.Buzzer.Buzz_BeginInteraction);
                            flag4 = true;
                        }
                        else if (__instance.ClosestPossibleInteractable != null && !__instance.ClosestPossibleInteractable.IsSimpleInteract)
                        {
                            __instance.CurrentInteractable = __instance.ClosestPossibleInteractable;
                            __instance.m_state = FVRViveHand.HandState.GripInteracting;
                            __instance.CurrentInteractable.BeginInteraction(__instance);
                            __instance.Buzz(__instance.Buzzer.Buzz_BeginInteraction);
                            flag4 = true;
                        }
                    }
                    bool flag5 = false;
                    if (!flag4 && __instance.Input.TriggerDown)
                    {
                        if (!(__instance.CurrentHoveredQuickbeltSlot != null) || !(__instance.CurrentHoveredQuickbeltSlot.CurObject != null))
                        {
                            if (__instance.ClosestPossibleInteractable != null && __instance.ClosestPossibleInteractable.IsSimpleInteract)
                            {
                                __instance.ClosestPossibleInteractable.SimpleInteraction(__instance);
                                flag5 = true;
                            }
                        }
                    }
                    bool flag6 = false;
                    if (!flag4 && !flag5 && __instance.Input.IsGrabDown)
                    {
                        __instance.m_rawGrabCols = Physics.OverlapSphere(__instance.transform.position, 0.01f, __instance.LM_RawGrab, QueryTriggerInteraction.Ignore);
                        if (__instance.m_rawGrabCols.Length > 0)
                        {
                            for (int i = 0; i < __instance.m_rawGrabCols.Length; i++)
                            {
                                if (!(__instance.m_rawGrabCols[i].attachedRigidbody == null))
                                {
                                    if (__instance.m_rawGrabCols[i].attachedRigidbody.gameObject.CompareTag("RawGrab"))
                                    {
                                        FVRInteractiveObject component2 = __instance.m_rawGrabCols[i].attachedRigidbody.gameObject.GetComponent<FVRInteractiveObject>();
                                        if (component2 != null && component2.IsInteractable())
                                        {
                                            flag6 = true;
                                            __instance.CurrentInteractable = component2;
                                            __instance.m_state = FVRViveHand.HandState.GripInteracting;
                                            __instance.CurrentInteractable.BeginInteraction(__instance);
                                            __instance.Buzz(__instance.Buzzer.Buzz_BeginInteraction);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (GM.Options.ControlOptions.WIPGrabbityState == ControlOptions.WIPGrabbity.Enabled && !flag4 && !flag5 && !flag6)
                    {
                        if (__instance.m_selectedObj == null)
                        {
                            __instance.CastToFindHover();
                        }
                        else
                        {
                            __instance.SetGrabbityHovered(null);
                        }
                        bool flag7;
                        bool flag8;
                        if (GM.Options.ControlOptions.WIPGrabbityButtonState == ControlOptions.WIPGrabbityButton.Grab)
                        {
                            flag7 = __instance.Input.GripDown;
                            flag8 = __instance.Input.GripUp;
                        }
                        else
                        {
                            flag7 = __instance.Input.TriggerDown;
                            flag8 = __instance.Input.TriggerUp;
                        }
                        if (flag7 && __instance.m_grabityHoveredObject != null && __instance.m_selectedObj == null)
                        {
                            __instance.CastToGrab();
                        }
                        if (flag8 && !__instance.m_isObjectInTransit)
                        {
                            __instance.m_selectedObj = null;
                        }
                        if (__instance.m_selectedObj != null && !__instance.m_isObjectInTransit)
                        {
                            float num = 3.5f;
                            if (Mathf.Abs(__instance.Input.VelAngularLocal.x) > num || Mathf.Abs(__instance.Input.VelAngularLocal.y) > num)
                            {
                                __instance.BeginFlick(__instance.m_selectedObj);
                            }
                        }
                    }
                    else
                    {
                        __instance.SetGrabbityHovered(null);
                    }
                    if (GM.Options.ControlOptions.WIPGrabbityState == ControlOptions.WIPGrabbity.Enabled && !flag4 && !flag5 && __instance.Input.IsGrabDown && __instance.m_isObjectInTransit && __instance.m_selectedObj != null)
                    {
                        float num2 = Vector3.Distance(__instance.transform.position, __instance.m_selectedObj.transform.position);
                        if (num2 < 0.5f)
                        {
                            if (__instance.m_selectedObj.UseGripRotInterp)
                            {
                                __instance.CurrentInteractable = __instance.m_selectedObj;
                                __instance.CurrentInteractable.BeginInteraction(__instance);
                                __instance.m_state = FVRViveHand.HandState.GripInteracting;
                            }
                            else
                            {
                                __instance.RetrieveObject(__instance.m_selectedObj);
                            }
                            __instance.m_selectedObj = null;
                            __instance.m_isObjectInTransit = false;
                            __instance.SetGrabbityHovered(null);
                        }
                    }
                }
                else if (__instance.m_state == FVRViveHand.HandState.GripInteracting)
                {
                    __instance.SetGrabbityHovered(null);
                    bool flag9 = false;
                    if (__instance.CurrentInteractable != null)
                    {
                        ControlMode controlMode = __instance.CMode;
                        if (GM.Options.ControlOptions.GripButtonToHoldOverride == ControlOptions.GripButtonToHoldOverrideMode.OculusOverride)
                        {
                            controlMode = ControlMode.Oculus;
                        }
                        else if (GM.Options.ControlOptions.GripButtonToHoldOverride == ControlOptions.GripButtonToHoldOverrideMode.ViveOverride)
                        {
                            controlMode = ControlMode.Vive;
                        }
                        if (controlMode == ControlMode.Vive || controlMode == ControlMode.WMR)
                        {
                            if (__instance.CurrentInteractable.ControlType == FVRInteractionControlType.GrabHold)
                            {
                                if (__instance.Input.TriggerUp)
                                {
                                    flag9 = true;
                                }
                            }
                            else if (__instance.CurrentInteractable.ControlType == FVRInteractionControlType.GrabToggle)
                            {
                                ControlOptions.ButtonControlStyle gripButtonDropStyle = GM.Options.ControlOptions.GripButtonDropStyle;
                                if (gripButtonDropStyle != ControlOptions.ButtonControlStyle.Instant)
                                {
                                    if (gripButtonDropStyle != ControlOptions.ButtonControlStyle.Hold1Second)
                                    {
                                        if (gripButtonDropStyle == ControlOptions.ButtonControlStyle.DoubleClick)
                                        {
                                            if (!__instance.Input.TriggerPressed && __instance.Input.GripDown && __instance.m_timeSinceLastGripButtonDown > 0.05f && __instance.m_timeSinceLastGripButtonDown < 0.4f)
                                            {
                                                flag9 = true;
                                            }
                                        }
                                    }
                                    else if (!__instance.Input.TriggerPressed && __instance.m_timeGripButtonHasBeenHeld > 1f)
                                    {
                                        flag9 = true;
                                    }
                                }
                                else if (!__instance.Input.TriggerPressed && __instance.Input.GripDown)
                                {
                                    flag9 = true;
                                }
                            }
                        }
                        else if (__instance.Input.IsGrabUp)
                        {
                            flag9 = true;
                        }
                        if (flag9)
                        {
                            if (__instance.CurrentInteractable is FVRPhysicalObject && ((FVRPhysicalObject)__instance.CurrentInteractable).QuickbeltSlot == null && !((FVRPhysicalObject)__instance.CurrentInteractable).IsPivotLocked && __instance.CurrentHoveredQuickbeltSlot != null && __instance.CurrentHoveredQuickbeltSlot.GetAffixedTo() != (FVRPhysicalObject)__instance.CurrentInteractable && __instance.CurrentHoveredQuickbeltSlot.HeldObject == null && ((FVRPhysicalObject)__instance.CurrentInteractable).QBSlotType == __instance.CurrentHoveredQuickbeltSlot.Type && __instance.CurrentHoveredQuickbeltSlot.SizeLimit >= ((FVRPhysicalObject)__instance.CurrentInteractable).Size)
                            {
                                ((FVRPhysicalObject)__instance.CurrentInteractable).EndInteractionIntoInventorySlot(__instance, __instance.CurrentHoveredQuickbeltSlot);
                            }
                            else
                            {
                                __instance.CurrentInteractable.EndInteraction(__instance);
                            }
                            __instance.CurrentInteractable = null;
                            __instance.m_state = FVRViveHand.HandState.Empty;
                        }
                        else
                        {
                            __instance.CurrentInteractable.UpdateInteraction(__instance);
                        }
                    }
                    else
                    {
                        __instance.m_state = FVRViveHand.HandState.Empty;
                    }
                }
            }
            if (__instance.Input.GripPressed)
            {
                __instance.m_timeSinceLastGripButtonDown = 0f;
                __instance.m_timeGripButtonHasBeenHeld += Time.deltaTime;
            }
            else
            {
                __instance.m_timeGripButtonHasBeenHeld = 0f;
            }
            __instance.m_canMadeGrabReleaseSoundThisFrame = true;

            return false;
        }
    }

    // Patches FVRMovementManager.Jump to make it use stamina or to prevent it altogether if not enough stamina
    // This completely replaces the original
    class MovementManagerJumpPatch
    {
        static bool Prefix(FVRMovementManager __instance, ref bool ___m_isGrounded, ref Vector3 ___m_smoothLocoVelocity)
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }

            // Return if not enough stamina
            if (Mod.stamina < Mod.jumpStaminaDrain)
            {
                return false;
            }

            if (__instance.Mode == FVRMovementManager.MovementMode.Armswinger || __instance.Mode == FVRMovementManager.MovementMode.SingleTwoAxis || __instance.Mode == FVRMovementManager.MovementMode.TwinStick)
            {
                if (!___m_isGrounded)
                {
                    return false;
                }

                __instance.DelayGround(0.1f);
                float num = 4.615f; // Corresponds to Realistic gravity mode in original * 0.65
                __instance.DelayGround(0.25f);
                ___m_smoothLocoVelocity.y = Mathf.Clamp(___m_smoothLocoVelocity.y, 0f, ___m_smoothLocoVelocity.y);
                ___m_smoothLocoVelocity.y = num;
                ___m_isGrounded = false;

                // Use stamina
                Mod.stamina = Mathf.Max(Mod.stamina - Mod.jumpStaminaDrain, 0);
                StaminaUI.instance.barFill.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mod.stamina / Mod.currentMaxStamina * 100);

                // Reset stamina timer
                Mod.staminaTimer = 2;
            }

            return false;
        }
    }

    // Patches FVRMovementManager to prevent sprinting in case of lack of stamina and limit velocity
    public class MovementManagerPatch
    {
        public static bool ShouldEngageSprint(bool originalValue)
        {
            if (originalValue)
            {
                // Can't sprint if,
                // No stamina
                // Fatigued
                // Heavy fatigued
                // Weight above max carry weight
                return Mod.stamina > 0 && Effect.fatigue == null && Effect.heavyFatigue == null && Mod.weight < Mod.currentWeightLimit;
            }
            else
            {
                return false;
            }
        }

        public static Vector3 LimitVelocity(Vector3 originalValue)
        {
            // Note that smoothLocoVelocity's magnitude in xz plane is limited to GM.Options.MovementOptions.TPLocoSpeeds[GM.Options.MovementOptions.TPLocoSpeedIndex]
            // A good default is MovementOptions.TPLocoSpeedIndex = 2, for a TPLocoSpeeds of 1.8
            // Is sprintingEngaged, vector is multiplied by 2 for total xz velocity of 3.6
            float maxMagnitude = GM.Options.MovementOptions.TPLocoSpeeds[GM.Options.MovementOptions.TPLocoSpeedIndex] * 2;
            float magnitude = Mathf.Sqrt(originalValue.x * originalValue.x + originalValue.z * originalValue.z);
            if (magnitude > maxMagnitude)
            {
                float mult = maxMagnitude / magnitude;
                return new Vector3(originalValue.x * mult, originalValue.y, originalValue.z * mult);
            }
            else
            {
                return originalValue;
            }
        }

        // Patches HandUpdateTwinstick
        static IEnumerable<CodeInstruction> TwinstickTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MovementManagerPatch), "ShouldEngageSprint")));

            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stfld && instruction.operand.ToString().Contains("m_sprintingEngaged") &&
                    (instructionList[i - 1].opcode == OpCodes.Ldc_I4_1 || instructionList[i - 1].opcode == OpCodes.Ceq))
                {
                    instructionList.InsertRange(i, toInsert);
                }
            }
            return instructionList;
        }

        // Patches HandUpdateTwoAxis
        static IEnumerable<CodeInstruction> TwoAxisTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MovementManagerPatch), "ShouldEngageSprint")));

            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stfld && instruction.operand.ToString().Contains("m_sprintingEngaged") &&
                    instructionList[i - 1].opcode == OpCodes.Ldc_I4_1)
                {
                    instructionList.InsertRange(i, toInsert);
                }
            }
            return instructionList;
        }

        // Patches UpdateSmoothLocomotion
        // We set a vector to this.m_smoothLocoVelocity * Time.deltaTime
        // Before this, we want to limit m_smoothLocoVelocity to below sprinting speed
        static IEnumerable<CodeInstruction> UpdateTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MovementManagerPatch), "LimitVelocity")));

            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_S && instruction.operand.ToString().Contains("108"))
                {
                    instructionList.InsertRange(i - 2, toInsert);
                    break;
                }
            }
            return instructionList;
        }
    }

    // Patches FVRFireArmChamber.SetRound(round, bool) to keep track of weight in chamber
    class ChamberSetRoundPatch
    {
        static void Prefix(FVRFireArmRound round, FVRFireArmChamber __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            MeatovItem meatovItem = __instance.GetComponentInParent<MeatovItem>();
            if (meatovItem != null)
            {
                if(round == null)
                {
                    if (__instance.GetRound() != null)
                    {
                        meatovItem.currentWeight -= Mod.GetRoundWeight(__instance.GetRound().RoundType);
                    }
                }
                else
                {
                    if (__instance.GetRound() == null)
                    {
                        meatovItem.currentWeight += Mod.GetRoundWeight(round.RoundType);
                    }
                }
            }
        }
    }

    // Patches FVRFireArmChamber.SetRound(round, vector3, quaternion) to keep track of weight in chamber
    class ChamberSetRoundGivenPatch
    {
        static void Prefix(FVRFireArmRound round, FVRFireArmChamber __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            MeatovItem meatovItem = __instance.GetComponentInParent<MeatovItem>();
            if (meatovItem != null)
            {
                if (round == null)
                {
                    if (__instance.GetRound() != null)
                    {
                        meatovItem.currentWeight -= Mod.GetRoundWeight(__instance.GetRound().RoundType);
                    }
                }
                else
                {
                    if (__instance.GetRound() == null)
                    {
                        meatovItem.currentWeight += Mod.GetRoundWeight(round.RoundType);
                    }
                }
            }
        }
    }

    // Patches FVRFireArmChamber.SetRound(class, vector3, quaternion) to keep track of weight in chamber
    class ChamberSetRoundClassPatch
    {
        static void Prefix(FireArmRoundClass rclass, FVRFireArmChamber __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            MeatovItem meatovItem = __instance.GetComponentInParent<MeatovItem>();
            if (meatovItem != null)
            {
                GameObject gameObject = AM.GetRoundSelfPrefab(__instance.RoundType, rclass).GetGameObject();
                FVRFireArmRound component = gameObject.GetComponent<FVRFireArmRound>();
                if (component == null)
                {
                    if (__instance.GetRound() != null)
                    {
                        meatovItem.currentWeight -= Mod.GetRoundWeight(__instance.GetRound().RoundType);
                    }
                }
                else
                {
                    if (__instance.GetRound() == null)
                    {
                        meatovItem.currentWeight += Mod.GetRoundWeight(component.RoundType);
                    }
                }
            }
        }
    }

    // Patches FVRFireArmMagazine.RemoveRound() to track ammo box ammo
    class MagRemoveRoundPatch
    {
        static void Prefix(FVRFireArmMagazine __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if(__instance.m_numRounds > 0)
            {
                MeatovItem meatovItem = __instance.GetComponent<MeatovItem>();
                if (meatovItem != null)
                {
                    // Manage weight
                    meatovItem.currentWeight -= Mod.GetRoundWeight(__instance.RoundType);

                    // Manage ammobox ammo
                    if (meatovItem.itemType == MeatovItem.ItemType.AmmoBox)
                    {
                        Dictionary<FireArmRoundType, Dictionary<FireArmRoundClass, Dictionary<MeatovItem, int>>> dictToUse = null;
                        if (meatovItem.locationIndex == 0) // Player
                        {
                            dictToUse = Mod.ammoBoxesByRoundClassByRoundType;
                        }
                        else if (meatovItem.locationIndex == 1) // Hideout
                        {
                            dictToUse = HideoutController.instance.ammoBoxesByRoundClassByRoundType;
                        }
                        else // Raid
                        {
                            return;
                        }

                        FVRLoadedRound lr = __instance.LoadedRounds[__instance.m_numRounds - 1];
                        --dictToUse[__instance.RoundType][lr.LR_Class][meatovItem];
                        if (dictToUse[__instance.RoundType][lr.LR_Class][meatovItem] == 0)
                        {
                            dictToUse[__instance.RoundType][lr.LR_Class].Remove(meatovItem);
                            if (dictToUse[__instance.RoundType][lr.LR_Class].Count == 0)
                            {
                                dictToUse[__instance.RoundType].Remove(lr.LR_Class);
                                if (dictToUse[__instance.RoundType].Count == 0)
                                {
                                    dictToUse.Remove(__instance.RoundType);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    // Patches FVRFireArmMagazine.RemoveRound(bool) to track ammo box ammo
    // TODO: See if this could be used to do what we do in MagazineUpdateInteractionPatch instead
    class MagRemoveRoundBoolPatch
    {
        static void Prefix(FVRFireArmMagazine __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (__instance.m_numRounds > 0)
            {
                MeatovItem meatovItem = __instance.GetComponent<MeatovItem>();
                if (meatovItem != null)
                {
                    // Manage weight
                    meatovItem.currentWeight -= Mod.GetRoundWeight(__instance.RoundType);

                    // Manage ammobox ammo
                    if (meatovItem.itemType == MeatovItem.ItemType.AmmoBox)
                    {
                        Dictionary<FireArmRoundType, Dictionary<FireArmRoundClass, Dictionary<MeatovItem, int>>> dictToUse = null;
                        if (meatovItem.locationIndex == 0) // Player
                        {
                            dictToUse = Mod.ammoBoxesByRoundClassByRoundType;
                        }
                        else if (meatovItem.locationIndex == 1) // Hideout
                        {
                            dictToUse = HideoutController.instance.ammoBoxesByRoundClassByRoundType;
                        }
                        else // Raid
                        {
                            return;
                        }

                        FVRLoadedRound lr = __instance.LoadedRounds[__instance.m_numRounds - 1];
                        --dictToUse[__instance.RoundType][lr.LR_Class][meatovItem];
                        if (dictToUse[__instance.RoundType][lr.LR_Class][meatovItem] == 0)
                        {
                            dictToUse[__instance.RoundType][lr.LR_Class].Remove(meatovItem);
                            if (dictToUse[__instance.RoundType][lr.LR_Class].Count == 0)
                            {
                                dictToUse[__instance.RoundType].Remove(lr.LR_Class);
                                if (dictToUse[__instance.RoundType].Count == 0)
                                {
                                    dictToUse.Remove(__instance.RoundType);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    // Patches FVRFireArmMagazine.RemoveRound(int) to track ammo box ammo
    class MagRemoveRoundIntPatch
    {
        static void Prefix(FVRFireArmMagazine __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (__instance.m_numRounds > 0)
            {
                MeatovItem meatovItem = __instance.GetComponent<MeatovItem>();
                if (meatovItem != null)
                {
                    // Manage weight
                    meatovItem.currentWeight -= Mod.GetRoundWeight(__instance.RoundType);

                    // Manage ammobox ammo
                    if (meatovItem.itemType == MeatovItem.ItemType.AmmoBox)
                    {
                        Dictionary<FireArmRoundType, Dictionary<FireArmRoundClass, Dictionary<MeatovItem, int>>> dictToUse = null;
                        if (meatovItem.locationIndex == 0) // Player
                        {
                            dictToUse = Mod.ammoBoxesByRoundClassByRoundType;
                        }
                        else if (meatovItem.locationIndex == 1) // Hideout
                        {
                            dictToUse = HideoutController.instance.ammoBoxesByRoundClassByRoundType;
                        }
                        else // Raid
                        {
                            return;
                        }

                        FVRLoadedRound lr = __instance.LoadedRounds[__instance.m_numRounds - 1];
                        --dictToUse[__instance.RoundType][lr.LR_Class][meatovItem];
                        if (dictToUse[__instance.RoundType][lr.LR_Class][meatovItem] == 0)
                        {
                            dictToUse[__instance.RoundType][lr.LR_Class].Remove(meatovItem);
                            if (dictToUse[__instance.RoundType][lr.LR_Class].Count == 0)
                            {
                                dictToUse[__instance.RoundType].Remove(lr.LR_Class);
                                if (dictToUse[__instance.RoundType].Count == 0)
                                {
                                    dictToUse.Remove(__instance.RoundType);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    // Patches FVRFireArmClip.RemoveRound() to keep track of weight of ammo in clip
    class ClipRemoveRoundPatch
    {
        static void Prefix(FVRFireArmClip __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (__instance.m_numRounds > 0)
            {
                MeatovItem meatovItem = __instance.GetComponent<MeatovItem>();
                if (meatovItem != null)
                {
                    // Manage weight
                    meatovItem.currentWeight -= Mod.GetRoundWeight(__instance.RoundType);
                }
            }
        }
    }

    // Patches FVRFireArmClip.RemoveRound(bool) to keep track of weight of ammo in clip
    // TODO: See if this could be used to do what we do in ClipUpdateInteractionPatch instead
    class ClipRemoveRoundBoolPatch
    {
        static void Prefix(FVRFireArmClip __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (__instance.m_numRounds > 0)
            {
                MeatovItem meatovItem = __instance.GetComponent<MeatovItem>();
                if (meatovItem != null)
                {
                    // Manage weight
                    meatovItem.currentWeight -= Mod.GetRoundWeight(__instance.RoundType);
                }
            }
        }
    }

    // Patches FVRFireArmClip.RemoveRoundReturnClass to keep track of weight of ammo in clip
    class ClipRemoveRoundClassPatch
    {
        static void Prefix(FVRFireArmClip __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (__instance.m_numRounds > 0)
            {
                MeatovItem meatovItem = __instance.GetComponent<MeatovItem>();
                if (meatovItem != null)
                {
                    // Manage weight
                    meatovItem.currentWeight -= Mod.GetRoundWeight(__instance.RoundType);
                }
            }
        }
    }

    // Patches FVRFirearmMagazine.ReloadMagWithType to track ammo in mag
    class MagReloadMagWithTypePatch
    {
        static void Prefix(FVRFireArmMagazine __instance, FireArmRoundClass rClass)
        {
            ++MagAddRoundPatch.magAddRoundSkip;

            if (!Mod.inMeatovScene)
            {
                return;
            }

            MeatovItem meatovItem = __instance.GetComponent<MeatovItem>();
            if(meatovItem != null)
            {
                // Manage weight
                meatovItem.currentWeight = meatovItem.weight + Mod.GetRoundWeight(__instance.RoundType) * __instance.m_capacity;

                // Manage ammoBox ammo
                if (meatovItem.itemType == MeatovItem.ItemType.AmmoBox)
                {
                    Dictionary<FireArmRoundType, Dictionary<FireArmRoundClass, Dictionary<MeatovItem, int>>> dictToUse = null;
                    if (meatovItem.locationIndex == 0) // Player
                    {
                        dictToUse = Mod.ammoBoxesByRoundClassByRoundType;
                    }
                    else if (meatovItem.locationIndex == 1) // Hideout
                    {
                        dictToUse = HideoutController.instance.ammoBoxesByRoundClassByRoundType;
                    }
                    else // Raid
                    {
                        return;
                    }

                    if (dictToUse != null)
                    {
                        // Remove current rounds
                        for(int i=0; i < __instance.LoadedRounds.Length; ++i)
                        {
                            FVRLoadedRound lr = __instance.LoadedRounds[i];
                            if (lr != null)
                            {
                                --dictToUse[__instance.RoundType][lr.LR_Class][meatovItem];
                                if (dictToUse[__instance.RoundType][lr.LR_Class][meatovItem] == 0)
                                {
                                    dictToUse[__instance.RoundType][lr.LR_Class].Remove(meatovItem);
                                    if (dictToUse[__instance.RoundType][lr.LR_Class].Count == 0)
                                    {
                                        dictToUse[__instance.RoundType].Remove(lr.LR_Class);
                                        if (dictToUse[__instance.RoundType].Count == 0)
                                        {
                                            dictToUse.Remove(__instance.RoundType);
                                        }
                                    }
                                }
                            }
                        }

                        // Add new rounds
                        if (dictToUse.TryGetValue(__instance.RoundType, out Dictionary<FireArmRoundClass, Dictionary<MeatovItem, int>> midDict))
                        {
                            if (midDict.TryGetValue(rClass, out Dictionary<MeatovItem, int> boxDict))
                            {
                                int count = 0;
                                if (boxDict.TryGetValue(meatovItem, out count))
                                {
                                    boxDict[meatovItem] += __instance.m_capacity;
                                }
                                else
                                {
                                    boxDict.Add(meatovItem, __instance.m_capacity);
                                }
                            }
                            else
                            {
                                Dictionary<MeatovItem, int> newBoxDict = new Dictionary<MeatovItem, int>();
                                newBoxDict.Add(meatovItem, __instance.m_capacity);
                                midDict.Add(rClass, newBoxDict);
                            }
                        }
                        else
                        {
                            Dictionary<MeatovItem, int> newBoxDict = new Dictionary<MeatovItem, int>();
                            newBoxDict.Add(meatovItem, __instance.m_capacity);
                            Dictionary<FireArmRoundClass, Dictionary<MeatovItem, int>> newMidDict = new Dictionary<FireArmRoundClass, Dictionary<MeatovItem, int>>();
                            newMidDict.Add(rClass, newBoxDict);
                            dictToUse.Add(__instance.RoundType, newMidDict);
                        }
                    }
                }
            }
        }

        static void Postfix()
        {
            --MagAddRoundPatch.magAddRoundSkip;
        }
    }

    // Patches FVRFirearmClip.ReloadClipWithType to track ammo in clip
    class ClipReloadClipWithTypePatch
    {
        static void Prefix(FVRFireArmClip __instance, FireArmRoundClass rClass)
        {
            ++ClipAddRoundPatch.clipAddRoundSkip;

            if (!Mod.inMeatovScene)
            {
                return;
            }

            MeatovItem meatovItem = __instance.GetComponent<MeatovItem>();
            if(meatovItem != null)
            {
                // Manage weight
                meatovItem.currentWeight = meatovItem.weight + Mod.GetRoundWeight(__instance.RoundType) * __instance.m_capacity;
            }
        }

        static void Postfix()
        {
            --ClipAddRoundPatch.clipAddRoundSkip;
        }
    }

    // Patches FVRFirearmMagazine.ReloadMagWithList to track ammo in mag
    class MagReloadMagWithListPatch
    {
        static void Prefix(FVRFireArmMagazine __instance, List<FireArmRoundClass> list)
        {
            ++MagAddRoundPatch.magAddRoundSkip;

            if (!Mod.inMeatovScene)
            {
                return;
            }

            MeatovItem meatovItem = __instance.GetComponent<MeatovItem>();
            if(meatovItem != null)
            {
                // Manage weight
                meatovItem.currentWeight = meatovItem.weight + Mod.GetRoundWeight(__instance.RoundType) * Mathf.Min(list.Count, __instance.m_capacity);

                // Manage ammoBox ammo
                if (meatovItem.itemType == MeatovItem.ItemType.AmmoBox)
                {
                    Dictionary<FireArmRoundType, Dictionary<FireArmRoundClass, Dictionary<MeatovItem, int>>> dictToUse = null;
                    if (meatovItem.locationIndex == 0) // Player
                    {
                        dictToUse = Mod.ammoBoxesByRoundClassByRoundType;
                    }
                    else if (meatovItem.locationIndex == 1) // Hideout
                    {
                        dictToUse = HideoutController.instance.ammoBoxesByRoundClassByRoundType;
                    }
                    else // Raid
                    {
                        return;
                    }

                    if (dictToUse != null)
                    {
                        // Remove current rounds
                        for(int i=0; i < __instance.LoadedRounds.Length; ++i)
                        {
                            FVRLoadedRound lr = __instance.LoadedRounds[i];
                            if (lr != null)
                            {
                                --dictToUse[__instance.RoundType][lr.LR_Class][meatovItem];
                                if (dictToUse[__instance.RoundType][lr.LR_Class][meatovItem] == 0)
                                {
                                    dictToUse[__instance.RoundType][lr.LR_Class].Remove(meatovItem);
                                    if (dictToUse[__instance.RoundType][lr.LR_Class].Count == 0)
                                    {
                                        dictToUse[__instance.RoundType].Remove(lr.LR_Class);
                                        if (dictToUse[__instance.RoundType].Count == 0)
                                        {
                                            dictToUse.Remove(__instance.RoundType);
                                        }
                                    }
                                }
                            }
                        }

                        // Add new rounds
                        for(int i=0; i < list.Count; ++i)
                        {
                            if (dictToUse.TryGetValue(__instance.RoundType, out Dictionary<FireArmRoundClass, Dictionary<MeatovItem, int>> midDict))
                            {
                                if (midDict.TryGetValue(list[i], out Dictionary<MeatovItem, int> boxDict))
                                {
                                    int count = 0;
                                    if (boxDict.TryGetValue(meatovItem, out count))
                                    {
                                        ++boxDict[meatovItem];
                                    }
                                    else
                                    {
                                        boxDict.Add(meatovItem, 1);
                                    }
                                }
                                else
                                {
                                    Dictionary<MeatovItem, int> newBoxDict = new Dictionary<MeatovItem, int>();
                                    newBoxDict.Add(meatovItem, 1);
                                    midDict.Add(list[i], newBoxDict);
                                }
                            }
                            else
                            {
                                Dictionary<MeatovItem, int> newBoxDict = new Dictionary<MeatovItem, int>();
                                newBoxDict.Add(meatovItem, 1);
                                Dictionary<FireArmRoundClass, Dictionary<MeatovItem, int>> newMidDict = new Dictionary<FireArmRoundClass, Dictionary<MeatovItem, int>>();
                                newMidDict.Add(list[i], newBoxDict);
                                dictToUse.Add(__instance.RoundType, newMidDict);
                            }
                        }
                    }
                }
            }
        }

        static void Postfix()
        {
            --MagAddRoundPatch.magAddRoundSkip;
        }
    }

    // Patches FVRFirearmClip.ReloadClipWithList to track ammo in mag
    class ClipReloadClipWithListPatch
    {
        static void Prefix(FVRFireArmClip __instance, List<FireArmRoundClass> list)
        {
            ++ClipAddRoundPatch.clipAddRoundSkip;

            if (!Mod.inMeatovScene)
            {
                return;
            }

            MeatovItem meatovItem = __instance.GetComponent<MeatovItem>();
            if(meatovItem != null)
            {
                // Manage weight
                List<FireArmRoundClass> list2 = new List<FireArmRoundClass>();
                int num = list.Count - __instance.m_capacity;
                for (int i = num; i < list.Count; i++)
                {
                    list2.Add(list[i]);
                }
                int num2 = Mathf.Min(list2.Count, __instance.m_capacity);
                meatovItem.currentWeight = meatovItem.weight + Mod.GetRoundWeight(__instance.RoundType) * num2;
            }
        }

        static void Postfix()
        {
            --ClipAddRoundPatch.clipAddRoundSkip;
        }
    }

    // Patches ModularWeaponPart
    class ModularWeaponPartPatch
    {
        public static MeatovItem overrideItem;
        public static bool overrideItemNone;

        // To track activation to apply stats to weapon
        // Note that by now, we assume the part's attachment point's selectedPart and modularWeaponPartsAttachmentPoint.ModularPartPoint fields have been set
        static void EnablePrefix(ModularWeaponPart __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }
            Mod.LogInfo("Enable prefix on "+ __instance.Name);

            if (overrideItem == null)
            {
                Mod.LogInfo("\tNo override item");
                if (overrideItemNone)
                {
                    overrideItemNone = false;
                }
                else
                {
                    Mod.LogInfo("\t\tNot none");
                    TODO: // This is way too complicated, having ModularWeaponPart just keep a ref to the point it is currently attached to would
                    //       let us skip having to find the point by iterating over all of them, fix it, make pull request
                    //       The problem is that the part doesn't store which group it is a part of 
                    //       But we need this to get its data
                    //       Our next best option is to find which point it is attached to, which stores that data

                    // Get the modular weapon this part is attached to
                    IModularWeapon modularWeapon = __instance.GetComponentInParent<IModularWeapon>();
                    if (modularWeapon != null)
                    {
                        Mod.LogInfo("\t\t\tGot modular weapon in parents");
                        bool foundData = false;
                        // Go through all of the attachment points to find which one this part is attached to
                        foreach (KeyValuePair<string, ModularWeaponPartsAttachmentPoint> pointEntry in modularWeapon.AllAttachmentPoints)
                        {
                            Mod.LogInfo("\t\t\t\tChecking point "+ pointEntry.Key+":"+ pointEntry.Value.SelectedModularWeaponPart+":"+ pointEntry.Value.ModularPartPoint);
                            // Find data for this part
                            if (pointEntry.Value.ModularPartPoint == __instance.transform
                                && Mod.modItemsByPartByGroup.TryGetValue(pointEntry.Key, out Dictionary<string, List<MeatovItemData>> groupDict)
                                && groupDict.TryGetValue(pointEntry.Value.SelectedModularWeaponPart, out List<MeatovItemData> partList))
                            {
                                Mod.LogInfo("\t\t\t\t\tDefault part enabled: " + pointEntry.Key+":"+ pointEntry.Value.SelectedModularWeaponPart);
                                // Here, we assume the first part in the list is the default a modul weapon would spawn with
                                MeatovItemData partData = partList[0];
                                MeatovItem partItem = __instance.gameObject.GetComponent<MeatovItem>();
                                if(partItem == null)
                                {
                                    partItem = __instance.gameObject.AddComponent<MeatovItem>();
                                }
                                partItem.SetData(partData);
                                // Since the item got instantiated directly on an already parented object, we want to make sure we set the meatov parenting properly
                                // Note that despite this being here, it usually (if not always?) does not work because the parent weapon gets its meatovitem after
                                partItem.OnTransformParentChanged();
                                foundData = true;
                                break;
                            }
                        }
                        if (!foundData)
                        {
                            Mod.LogWarning("Could not find data for part "+__instance.name+" on "+modularWeapon.ToString()+". This is probably because tarkov doesn't have this item");
                        }
                    }
                }
            }
            else
            {
                MeatovItem partItem = __instance.gameObject.GetComponent<MeatovItem>();
                if (partItem == null)
                {
                    partItem = __instance.gameObject.AddComponent<MeatovItem>();
                }
                partItem.SetData(overrideItem.itemData);

                MeatovItem.Copy(overrideItem, partItem);

                overrideItem.Destroy();

                overrideItem = null;

                // Since the item got instantiated directly on an already parented object, we want to make sure we set the meatov parenting properly
                partItem.OnTransformParentChanged();
            }
        }

        // To track deactivation to unapply stats from weapon
        static void DisablePrefix(ModularWeaponPart __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            MeatovItem partItem = __instance.GetComponent<MeatovItem>();
            if(partItem != null)
            {
                // We want to keep the data that was on the modular part so make delegate to copy it
                Area workbench = HideoutController.instance.areaController.areas[10];
                ContainmentVolume.SpawnItemReturnDelegate del = itemsSpawned =>
                {
                    MeatovItem.Copy(partItem, itemsSpawned[0]);
                };
                workbench.levels[workbench.currentLevel].areaVolumes[0].SpawnItem(partItem.itemData, 1, false, del);
            }
        }
    }

    // Patches FVRFirearmMagazine.ReloadMagWithTypeUpToPercentage to track ammo in mag
    class MagReloadMagWithTypeUpToPercentagePatch
    {
        static void Prefix(FVRFireArmMagazine __instance, FireArmRoundClass rClass, float percentage)
        {
            ++MagAddRoundPatch.magAddRoundSkip;

            if (!Mod.inMeatovScene)
            {
                return;
            }

            MeatovItem meatovItem = __instance.GetComponent<MeatovItem>();
            if (meatovItem != null)
            {
                // Manage weight
                int amount = Mathf.Clamp((int)((float)__instance.m_capacity * percentage), 1, __instance.m_capacity);
                meatovItem.currentWeight = meatovItem.weight + Mod.GetRoundWeight(__instance.RoundType) * amount;

                // Manage ammoBox ammo
                if (meatovItem.itemType == MeatovItem.ItemType.AmmoBox)
                {
                    Dictionary<FireArmRoundType, Dictionary<FireArmRoundClass, Dictionary<MeatovItem, int>>> dictToUse = null;
                    if (meatovItem.locationIndex == 0) // Player
                    {
                        dictToUse = Mod.ammoBoxesByRoundClassByRoundType;
                    }
                    else if (meatovItem.locationIndex == 1) // Hideout
                    {
                        dictToUse = HideoutController.instance.ammoBoxesByRoundClassByRoundType;
                    }
                    else // Raid
                    {
                        return;
                    }

                    if (dictToUse != null)
                    {
                        // Remove current rounds
                        for (int i = 0; i < __instance.LoadedRounds.Length; ++i)
                        {
                            FVRLoadedRound lr = __instance.LoadedRounds[i];
                            if (lr != null)
                            {
                                --dictToUse[__instance.RoundType][lr.LR_Class][meatovItem];
                                if (dictToUse[__instance.RoundType][lr.LR_Class][meatovItem] == 0)
                                {
                                    dictToUse[__instance.RoundType][lr.LR_Class].Remove(meatovItem);
                                    if (dictToUse[__instance.RoundType][lr.LR_Class].Count == 0)
                                    {
                                        dictToUse[__instance.RoundType].Remove(lr.LR_Class);
                                        if (dictToUse[__instance.RoundType].Count == 0)
                                        {
                                            dictToUse.Remove(__instance.RoundType);
                                        }
                                    }
                                }
                            }
                        }

                        // Add new rounds
                        if (dictToUse.TryGetValue(__instance.RoundType, out Dictionary<FireArmRoundClass, Dictionary<MeatovItem, int>> midDict))
                        {
                            if (midDict.TryGetValue(rClass, out Dictionary<MeatovItem, int> boxDict))
                            {
                                int count = 0;
                                if (boxDict.TryGetValue(meatovItem, out count))
                                {
                                    boxDict[meatovItem] += amount;
                                }
                                else
                                {
                                    boxDict.Add(meatovItem, amount);
                                }
                            }
                            else
                            {
                                Dictionary<MeatovItem, int> newBoxDict = new Dictionary<MeatovItem, int>();
                                newBoxDict.Add(meatovItem, amount);
                                midDict.Add(rClass, newBoxDict);
                            }
                        }
                        else
                        {
                            Dictionary<MeatovItem, int> newBoxDict = new Dictionary<MeatovItem, int>();
                            newBoxDict.Add(meatovItem, amount);
                            Dictionary<FireArmRoundClass, Dictionary<MeatovItem, int>> newMidDict = new Dictionary<FireArmRoundClass, Dictionary<MeatovItem, int>>();
                            newMidDict.Add(rClass, newBoxDict);
                            dictToUse.Add(__instance.RoundType, newMidDict);
                        }
                    }
                }
            }
        }

        static void Postfix()
        {
            --MagAddRoundPatch.magAddRoundSkip;
        }
    }

    // Patches FVRFirearmMagazine.AddRound(Round) to track ammo in ammo boxes
    class MagAddRoundPatch
    {
        public static int magAddRoundSkip;

        static void Prefix(FVRFireArmMagazine __instance, FVRFireArmRound round)
        {
            if (!Mod.inMeatovScene || magAddRoundSkip > 0)
            {
                return;
            }

            if (__instance.m_numRounds < __instance.m_capacity)
            {
                MeatovItem meatovItem = __instance.GetComponent<MeatovItem>();
                if(meatovItem != null)
                {
                    // Manage weight
                    meatovItem.currentWeight += Mod.GetRoundWeight(__instance.RoundType);

                    // Manage ammoBox ammo
                    if (meatovItem.itemType == MeatovItem.ItemType.AmmoBox)
                    {
                        Dictionary<FireArmRoundType, Dictionary<FireArmRoundClass, Dictionary<MeatovItem, int>>> dictToUse = null;
                        if (meatovItem.locationIndex == 0) // Player
                        {
                            dictToUse = Mod.ammoBoxesByRoundClassByRoundType;
                        }
                        else if (meatovItem.locationIndex == 1) // Hideout
                        {
                            dictToUse = HideoutController.instance.ammoBoxesByRoundClassByRoundType;
                        }
                        else // Raid
                        {
                            return;
                        }

                        if (dictToUse != null)
                        {
                            if (dictToUse.TryGetValue(__instance.RoundType, out Dictionary<FireArmRoundClass, Dictionary<MeatovItem, int>> midDict))
                            {
                                if (midDict.TryGetValue(round.RoundClass, out Dictionary<MeatovItem, int> boxDict))
                                {
                                    int count = 0;
                                    if (boxDict.TryGetValue(meatovItem, out count))
                                    {
                                        ++boxDict[meatovItem];
                                    }
                                    else
                                    {
                                        boxDict.Add(meatovItem, 1);
                                    }
                                }
                                else
                                {
                                    Dictionary<MeatovItem, int> newBoxDict = new Dictionary<MeatovItem, int>();
                                    newBoxDict.Add(meatovItem, 1);
                                    midDict.Add(round.RoundClass, newBoxDict);
                                }
                            }
                            else
                            {
                                Dictionary<MeatovItem, int> newBoxDict = new Dictionary<MeatovItem, int>();
                                newBoxDict.Add(meatovItem, 1);
                                Dictionary<FireArmRoundClass, Dictionary<MeatovItem, int>> newMidDict = new Dictionary<FireArmRoundClass, Dictionary<MeatovItem, int>>();
                                newMidDict.Add(round.RoundClass, newBoxDict);
                                dictToUse.Add(__instance.RoundType, newMidDict);
                            }
                        }
                    }
                }
            }
        }
    }

    // Patches FVRFirearmMagazine.AddRound(Class) to track ammo in ammo boxes
    class MagAddRoundClassPatch
    {
        static void Prefix(FVRFireArmMagazine __instance, FireArmRoundClass rClass)
        {
            if (!Mod.inMeatovScene || MagAddRoundPatch.magAddRoundSkip > 0)
            {
                return;
            }

            if (__instance.m_numRounds < __instance.m_capacity)
            {
                MeatovItem meatovItem = __instance.GetComponent<MeatovItem>();
                if (meatovItem != null)
                {
                    // Manage weight
                    meatovItem.currentWeight += Mod.GetRoundWeight(__instance.RoundType);

                    // Manage ammoBox ammo
                    if (meatovItem.itemType == MeatovItem.ItemType.AmmoBox)
                    {
                        Dictionary<FireArmRoundType, Dictionary<FireArmRoundClass, Dictionary<MeatovItem, int>>> dictToUse = null;
                        if (meatovItem.locationIndex == 0) // Player
                        {
                            dictToUse = Mod.ammoBoxesByRoundClassByRoundType;
                        }
                        else if (meatovItem.locationIndex == 1) // Hideout
                        {
                            dictToUse = HideoutController.instance.ammoBoxesByRoundClassByRoundType;
                        }
                        else // Raid
                        {
                            return;
                        }

                        if (dictToUse != null)
                        {
                            if (dictToUse.TryGetValue(__instance.RoundType, out Dictionary<FireArmRoundClass, Dictionary<MeatovItem, int>> midDict))
                            {
                                if (midDict.TryGetValue(rClass, out Dictionary<MeatovItem, int> boxDict))
                                {
                                    int count = 0;
                                    if (boxDict.TryGetValue(meatovItem, out count))
                                    {
                                        ++boxDict[meatovItem];
                                    }
                                    else
                                    {
                                        boxDict.Add(meatovItem, 1);
                                    }
                                }
                                else
                                {
                                    Dictionary<MeatovItem, int> newBoxDict = new Dictionary<MeatovItem, int>();
                                    newBoxDict.Add(meatovItem, 1);
                                    midDict.Add(rClass, newBoxDict);
                                }
                            }
                            else
                            {
                                Dictionary<MeatovItem, int> newBoxDict = new Dictionary<MeatovItem, int>();
                                newBoxDict.Add(meatovItem, 1);
                                Dictionary<FireArmRoundClass, Dictionary<MeatovItem, int>> newMidDict = new Dictionary<FireArmRoundClass, Dictionary<MeatovItem, int>>();
                                newMidDict.Add(rClass, newBoxDict);
                                dictToUse.Add(__instance.RoundType, newMidDict);
                            }
                        }
                    }
                }
            }
        }
    }

    // Patches FVRFirearmClip.AddRound(Round) to keep track of weight
    class ClipAddRoundPatch
    {
        public static int clipAddRoundSkip;

        static void Prefix(FVRFireArmClip __instance, FVRFireArmRound round)
        {
            if (!Mod.inMeatovScene || clipAddRoundSkip > 0)
            {
                return;
            }

            if (__instance.m_numRounds < __instance.m_capacity)
            {
                MeatovItem meatovItem = __instance.GetComponent<MeatovItem>();
                if (meatovItem != null)
                {
                    // Manage weight
                    meatovItem.currentWeight += Mod.GetRoundWeight(__instance.RoundType);
                }
            }
        }
    }

    // Patches FVRFirearmClip.AddRound(Class) to keep track of weight
    class ClipAddRoundClassPatch
    {
        static void Prefix(FVRFireArmClip __instance)
        {
            if (!Mod.inMeatovScene || ClipAddRoundPatch.clipAddRoundSkip > 0)
            {
                return;
            }

            if (__instance.m_numRounds < __instance.m_capacity)
            {
                MeatovItem meatovItem = __instance.GetComponent<MeatovItem>();
                if (meatovItem != null)
                {
                    // Manage weight
                    meatovItem.currentWeight += Mod.GetRoundWeight(__instance.RoundType);
                }
            }
        }
    }

    // Patches FVRInteractiveObject.PlayGrabSound to use custom item sounds 
    // This completely replaces the original
    class PlayGrabSoundPatch
    {
        static bool Prefix(ref FVRInteractiveObject __instance, bool isHard, FVRViveHand hand)
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }

            if (hand.CanMakeGrabReleaseSound)
            {
                if (__instance.HandlingGrabSound != HandlingGrabType.None)
                {
                    SM.PlayHandlingGrabSound(__instance.HandlingGrabSound, hand.Input.Pos, isHard);
                    hand.HandMadeGrabReleaseSound();
                }
                else
                {
                    MeatovItem MI = __instance.GetComponent<MeatovItem>();
                    //string[] soundCategories = new string[] { "drop", "pickup", "offline_use", "open", "use", "use_loop" };
                    if (MI != null && MI.itemSounds != null && MI.itemSounds[1] != null)
                    {
                        AudioEvent audioEvent = new AudioEvent();
                        audioEvent.Clips.Add(MI.itemSounds[1]);
                        SM.PlayCoreSound(FVRPooledAudioType.GenericClose, audioEvent, hand.Input.Pos);
                        hand.HandMadeGrabReleaseSound();
                    }
                }
            }

            return false;
        }
    }

    // Patches FVRInteractiveObject.PlayReleaseSound to use custom item sounds 
    // This completely replaces the original
    class PlayReleaseSoundPatch
    {
        static bool Prefix(ref FVRInteractiveObject __instance, FVRViveHand hand)
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }

            if (hand.CanMakeGrabReleaseSound)
            {
                if (__instance.HandlingReleaseSound != HandlingReleaseType.None)
                {
                    SM.PlayHandlingReleaseSound(__instance.HandlingReleaseSound, hand.Input.Pos);
                    hand.HandMadeGrabReleaseSound();
                }
                else
                {
                    MeatovItem CIW = __instance.GetComponent<MeatovItem>();
                    //string[] soundCategories = new string[] { "drop", "pickup", "offline_use", "open", "use", "use_loop" };
                    if (CIW != null && CIW.itemSounds != null && CIW.itemSounds[0] != null)
                    {
                        AudioEvent audioEvent = new AudioEvent();
                        audioEvent.Clips.Add(CIW.itemSounds[0]);
                        SM.PlayCoreSound(FVRPooledAudioType.GenericClose, audioEvent, hand.Input.Pos);
                        hand.HandMadeGrabReleaseSound();
                    }
                }
            }

            return false;
        }
    }

    // Patches FVRFirearm.Fire to know when a weapon is fired
    class FireArmFirePatch
    {
        static void Prefix(ref FVRFireArm __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (__instance.m_hand != null)
            {
                Mod.AddSkillExp(Skill.recoilAction, 25);
                Mod.AddSkillExp(Skill.weaponShotAction, 26);

                MeatovItem fireArmMI = __instance.GetComponent<MeatovItem>();
                switch (fireArmMI.weaponClass)
                {
                    case MeatovItem.WeaponClass.Pistol:
                        Mod.AddSkillExp(Skill.pistolWeaponShotAction, 12);
                        break;
                    case MeatovItem.WeaponClass.Revolver:
                        Mod.AddSkillExp(Skill.revolverWeaponShotAction, 13);
                        break;
                    case MeatovItem.WeaponClass.SMG:
                        Mod.AddSkillExp(Skill.SMGWeaponShotAction, 14);
                        break;
                    case MeatovItem.WeaponClass.Assault:
                        Mod.AddSkillExp(Skill.assaultWeaponShotAction, 15);
                        break;
                    case MeatovItem.WeaponClass.Shotgun:
                        Mod.AddSkillExp(Skill.shotgunWeaponShotAction, 16);
                        break;
                    case MeatovItem.WeaponClass.Sniper:
                        Mod.AddSkillExp(Skill.sniperWeaponShotAction, 17);
                        break;
                    case MeatovItem.WeaponClass.LMG:
                        Mod.AddSkillExp(Skill.LMGWeaponShotAction, 18);
                        break;
                    case MeatovItem.WeaponClass.HMG:
                        Mod.AddSkillExp(Skill.HMGWeaponShotAction, 19);
                        break;
                    case MeatovItem.WeaponClass.Launcher:
                        Mod.AddSkillExp(Skill.launcherWeaponShotAction, 20);
                        break;
                    case MeatovItem.WeaponClass.AttachedLauncher:
                        Mod.AddSkillExp(Skill.attachedLauncherWeaponShotAction, 21);
                        break;
                    case MeatovItem.WeaponClass.DMR:
                        Mod.AddSkillExp(Skill.DMRWeaponShotAction, 24);
                        break;
                }
            }

            FireArmRecoilPatch.fromFire = true;
        }
    }

    // Patches FVRFirearm.Recoil to control recoil strengh
    class FireArmRecoilPatch
    {
        public static bool fromFire = false;

        static void Prefix(ref FVRFireArm __instance, ref float VerticalRecoilMult)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            // If this recoil is caused by firing firearm (Could be cause by tremors)
            if (fromFire)
            {
                if (__instance.m_hand != null)
                {
                    float originalRecoilMult = VerticalRecoilMult;
                    VerticalRecoilMult -= originalRecoilMult * (Skill.recoilBonusPerLevel * (Mod.skills[25].currentProgress / 100));

                    MeatovItem fireArmMI = __instance.GetComponent<MeatovItem>();
                    switch (fireArmMI.weaponClass)
                    {
                        case MeatovItem.WeaponClass.Pistol:
                            VerticalRecoilMult -= originalRecoilMult * (0.003f * (Mod.skills[12].currentProgress / 100));
                            break;
                        case MeatovItem.WeaponClass.Revolver:
                            VerticalRecoilMult -= originalRecoilMult * (0.003f * (Mod.skills[13].currentProgress / 100));
                            break;
                        case MeatovItem.WeaponClass.SMG:
                            VerticalRecoilMult -= originalRecoilMult * (0.003f * (Mod.skills[14].currentProgress / 100));
                            break;
                        case MeatovItem.WeaponClass.Assault:
                            VerticalRecoilMult -= originalRecoilMult * (0.003f * (Mod.skills[15].currentProgress / 100));
                            break;
                        case MeatovItem.WeaponClass.Shotgun:
                            VerticalRecoilMult -= originalRecoilMult * (0.003f * (Mod.skills[16].currentProgress / 100));
                            break;
                        case MeatovItem.WeaponClass.Sniper:
                            VerticalRecoilMult -= originalRecoilMult * (0.003f * (Mod.skills[17].currentProgress / 100));
                            break;
                        case MeatovItem.WeaponClass.LMG:
                            VerticalRecoilMult -= originalRecoilMult * (0.003f * (Mod.skills[18].currentProgress / 100));
                            break;
                        case MeatovItem.WeaponClass.HMG:
                            VerticalRecoilMult -= originalRecoilMult * (0.003f * (Mod.skills[19].currentProgress / 100));
                            break;
                        case MeatovItem.WeaponClass.Launcher:
                            VerticalRecoilMult -= originalRecoilMult * (0.003f * (Mod.skills[20].currentProgress / 100));
                            break;
                        case MeatovItem.WeaponClass.AttachedLauncher:
                            VerticalRecoilMult -= originalRecoilMult * (0.003f * (Mod.skills[21].currentProgress / 100));
                            break;
                        case MeatovItem.WeaponClass.DMR:
                            VerticalRecoilMult -= originalRecoilMult * (0.003f * (Mod.skills[24].currentProgress / 100));
                            break;
                    }

                    VerticalRecoilMult -= originalRecoilMult * 0.005f * (Mod.skills[26].currentProgress / 100);
                }

                fromFire = false;
            }
        }
    }

    // Patches FVRInteractiveObject to keep track of item held
    class InteractiveObjectPatch
    {
        static void BeginInteractionPostfix(FVRInteractiveObject __instance, FVRViveHand hand)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }
            Mod.LogInfo("BeginInteractionPostfix on " + __instance.name);

            if (Mod.meatovItemByInteractive.TryGetValue(__instance, out MeatovItem meatovItem))
            {
                meatovItem.BeginInteraction(hand.IsThisTheRightHand ? Mod.rightHand : Mod.leftHand);
            }
        }

        static void EndInteractionPostfix(FVRInteractiveObject __instance, FVRViveHand hand)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }
            Mod.LogInfo("EndInteractionPostfix on " + __instance.name);

            if (Mod.meatovItemByInteractive.TryGetValue(__instance, out MeatovItem meatovItem))
            {
                meatovItem.EndInteraction(hand.IsThisTheRightHand ? Mod.rightHand : Mod.leftHand);
            }
        }
    }

    // Patches FVRFirearmAttachment.EndInteraction to UpdateInventories upon attachment
    class AttachmentEndInteractionPatch
    {
        static void Postfix(FVRFireArmAttachment __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }
            Mod.LogInfo("AttachmentEndInteractionPatch postfix on " + __instance.name);

            if (__instance.Sensor.CurHoveredMount != null && Mod.meatovItemByInteractive.TryGetValue(__instance, out MeatovItem meatovItem))
            {
                Mod.LogInfo("\tUpdating inventories");
                // Note that we pass false to manage weight, because of the order of parenting when attaching an attachment
                // The attachment will be parented after end interaction meaning that by the time when endinteraction
                // makes its call to UpdateInventories, the item is not on the attachment parent
                // But when we attach the attachment to the parent, UpdateInventories is never called
                // As it gets parented though, the parent weight is adjusted properly
                // But the attachment remains out of the player inventory
                // So here we update inventories, and ensure we don't add the weight again considering it will already have
                // been added by currentWeight change of the parent
                meatovItem.UpdateInventories(false, false, false);
            }
        }
    }

    // Patches FVRViveHand.EndInteractionIfHeld to keep track of item held
    class HandEndInteractionIfHeldPatch
    {
        static void Prefix(FVRViveHand __instance, FVRInteractiveObject inter)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if(inter == __instance.CurrentInteractable)
            {
                if (Mod.meatovItemByInteractive.TryGetValue(inter, out MeatovItem meatovItem))
                {
                    meatovItem.EndInteraction(__instance.IsThisTheRightHand ? Mod.rightHand : Mod.leftHand);
                }
            }
        }
    }

    // Patches FVRPhysicalObject
    class FVRPhysicalObjectPatch
    {
        // EndInteractionIntoInventorySlot to manage weight, prevent weight from being added to player if slot parent is already on player
        static void EndInteractionIntoInventorySlotPrefix(FVRPhysicalObject __instance, FVRViveHand hand, FVRQuickBeltSlot slot)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            //if(slot is RigSlot)
            //{
            //     Dont add to total weight if owner rig already in player inventory
            //    SetQuickBeltSlotPatch.dontProcessTotalWeight = ((RigSlot)slot).ownerItem.locationIndex == 0;
            //    MeatovItem.parentChangeDontManageWeight = SetQuickBeltSlotPatch.dontProcessTotalWeight;
            //}
            //else if (EquipmentSlot.wearingRig || EquipmentSlot.wearingArmoredRig)
            //{
            //     Find slot in config
            //    for (int slotIndex = 6; slotIndex < GM.CurrentPlayerBody.QBSlots_Internal.Count; ++slotIndex)
            //    {
            //        if (GM.CurrentPlayerBody.QBSlots_Internal[slotIndex] == slot)
            //        {
            //            SetQuickBeltSlotPatch.dontProcessTotalWeight = true;
            //            MeatovItem.parentChangeDontManageWeight = true;
            //            break;
            //        }
            //    }
            //}
        }

        // EndInteractionIntoInventorySlot to manage weight, reset flags
        static void EndInteractionIntoInventorySlotPostfix(FVRPhysicalObject __instance, FVRViveHand hand, FVRQuickBeltSlot slot)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            //SetQuickBeltSlotPatch.dontProcessTotalWeight = false;
            //MeatovItem.parentChangeDontManageWeight = false;
        }
    }

    // Patches SosigLink.Damage to keep track of player shots on AI
    class SosigLinkDamagePatch
    {
        static void Prefix(ref SosigLink __instance, Damage d)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (d.Source_IFF == 0)
            {
                AI AIScript = __instance.S.GetComponent<AI>();
                //AISpawn.AISpawnType AIType = AIScript.type;
                bool AIUsec = AIScript.USEC;
                //switch (__instance.BodyPart)
                //{
                //    case SosigLink.SosigBodyPart.Head:
                //        UpdateShotsCounterConditions(TraderTaskCounterCondition.CounterConditionTargetBodyPart.Head, d.point, AIType, AIUsec);
                //        break;
                //    case SosigLink.SosigBodyPart.Torso:
                //        float thoraxChance = 0.5f; // 50%
                //        float leftArmChance = 0.65f; // 15%
                //        float rightArmChance = 0.8f; // 15%
                //        // float stomachChance = 1f; // 20%
                //        TraderTaskCounterCondition.CounterConditionTargetBodyPart chosenBodyPart = TraderTaskCounterCondition.CounterConditionTargetBodyPart.Thorax;
                //        float rand = UnityEngine.Random.value;
                //        if (rand <= thoraxChance)
                //        {
                //            chosenBodyPart = TraderTaskCounterCondition.CounterConditionTargetBodyPart.Thorax;
                //        }
                //        else if (rand <= leftArmChance)
                //        {
                //            chosenBodyPart = TraderTaskCounterCondition.CounterConditionTargetBodyPart.LeftArm;
                //        }
                //        else if (rand <= rightArmChance)
                //        {
                //            chosenBodyPart = TraderTaskCounterCondition.CounterConditionTargetBodyPart.RightArm;
                //        }
                //        else
                //        {
                //            chosenBodyPart = TraderTaskCounterCondition.CounterConditionTargetBodyPart.Stomach;
                //        }
                //        UpdateShotsCounterConditions(chosenBodyPart, d.point, AIType, AIUsec);
                //        break;
                //    case SosigLink.SosigBodyPart.UpperLink:
                //        float stomachChance = 0.5f; // 50%
                //        float upperLeftLegChance = 0.65f; // 15%
                //        float upperRightLegChance = 0.8f; // 15%
                //        // float thoraxChance = 1f; // 20%
                //        TraderTaskCounterCondition.CounterConditionTargetBodyPart upperChosenBodyPart = TraderTaskCounterCondition.CounterConditionTargetBodyPart.Thorax;
                //        float upperRand = UnityEngine.Random.value;
                //        if (upperRand <= stomachChance)
                //        {
                //            upperChosenBodyPart = TraderTaskCounterCondition.CounterConditionTargetBodyPart.Stomach;
                //        }
                //        else if (upperRand <= upperLeftLegChance)
                //        {
                //            upperChosenBodyPart = TraderTaskCounterCondition.CounterConditionTargetBodyPart.LeftLeg;
                //        }
                //        else if (upperRand <= upperRightLegChance)
                //        {
                //            upperChosenBodyPart = TraderTaskCounterCondition.CounterConditionTargetBodyPart.RightLeg;
                //        }
                //        else
                //        {
                //            upperChosenBodyPart = TraderTaskCounterCondition.CounterConditionTargetBodyPart.Thorax;
                //        }
                //        UpdateShotsCounterConditions(upperChosenBodyPart, d.point, AIType, AIUsec);
                //        break;
                //    case SosigLink.SosigBodyPart.LowerLink:
                //        float lowerStomachChance = 0.20f; // 20%
                //        float leftLegChance = 0.6f; // 40%
                //        // float rightLegChance = 1f; // 40%
                //        TraderTaskCounterCondition.CounterConditionTargetBodyPart lowerChosenBodyPart = TraderTaskCounterCondition.CounterConditionTargetBodyPart.Thorax;
                //        float lowerRand = UnityEngine.Random.value;
                //        if (lowerRand <= lowerStomachChance)
                //        {
                //            lowerChosenBodyPart = TraderTaskCounterCondition.CounterConditionTargetBodyPart.Stomach;
                //        }
                //        else if (lowerRand <= leftLegChance)
                //        {
                //            lowerChosenBodyPart = TraderTaskCounterCondition.CounterConditionTargetBodyPart.LeftLeg;
                //        }
                //        else
                //        {
                //            lowerChosenBodyPart = TraderTaskCounterCondition.CounterConditionTargetBodyPart.RightLeg;
                //        }
                //        UpdateShotsCounterConditions(lowerChosenBodyPart, d.point, AIType, AIUsec);
                //        break;
                //}
            }
        }

        static void Postfix(ref SosigLink __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (__instance.S.Mustard <= 0)
            {
                __instance.S.KillSosig();
            }
        }

        //static void UpdateShotsCounterConditions(TraderTaskCounterCondition.CounterConditionTargetBodyPart bodyPart, Vector3 hitPoint, AISpawn.AISpawnType AIType, bool USEC)
        //{
        //    if (!Mod.currentShotsCounterConditionsByBodyPart.ContainsKey(bodyPart))
        //    {
        //        return;
        //    }

        //    foreach (TraderTaskCounterCondition counterCondition in Mod.currentShotsCounterConditionsByBodyPart[bodyPart])
        //    {
        //        // Check condition state validity
        //        if (!counterCondition.parentCondition.visible)
        //        {
        //            continue;
        //        }

        //        // Check enemy type
        //        if (!((counterCondition.counterConditionTargetEnemy == TraderTaskCounterCondition.CounterConditionTargetEnemy.Any) ||
        //              (counterCondition.counterConditionTargetEnemy == TraderTaskCounterCondition.CounterConditionTargetEnemy.Scav && AIType == AISpawn.AISpawnType.Scav) ||
        //              (counterCondition.counterConditionTargetEnemy == TraderTaskCounterCondition.CounterConditionTargetEnemy.Usec && AIType == AISpawn.AISpawnType.PMC && USEC) ||
        //              (counterCondition.counterConditionTargetEnemy == TraderTaskCounterCondition.CounterConditionTargetEnemy.Bear && AIType == AISpawn.AISpawnType.PMC && !USEC) ||
        //              (counterCondition.counterConditionTargetEnemy == TraderTaskCounterCondition.CounterConditionTargetEnemy.PMC && AIType == AISpawn.AISpawnType.PMC)))
        //        {
        //            continue;
        //        }

        //        // Check weapon
        //        if (counterCondition.allowedWeaponIDs != null && counterCondition.allowedWeaponIDs.Count > 0)
        //        {
        //            bool isHoldingAllowedWeapon = false;
        //            FVRInteractiveObject rightInteractable = Mod.rightHand.fvrHand.CurrentInteractable;
        //            if (rightInteractable != null)
        //            {
        //                MeatovItem MI = rightInteractable.GetComponent<MeatovItem>();
        //                if (MI != null)
        //                {
        //                    foreach (string parent in MI.parents)
        //                    {
        //                        if (counterCondition.allowedWeaponIDs.Contains(parent))
        //                        {
        //                            isHoldingAllowedWeapon = true;
        //                            break;
        //                        }
        //                    }
        //                }
        //            }
        //            if (!isHoldingAllowedWeapon)
        //            {
        //                FVRInteractiveObject leftInteractable = Mod.leftHand.fvrHand.CurrentInteractable;
        //                if (leftInteractable != null)
        //                {
        //                    MeatovItem MI = leftInteractable.GetComponent<MeatovItem>();
        //                    if (MI != null)
        //                    {
        //                        foreach (string parent in MI.parents)
        //                        {
        //                            if (counterCondition.allowedWeaponIDs.Contains(parent))
        //                            {
        //                                isHoldingAllowedWeapon = true;
        //                                break;
        //                            }
        //                        }
        //                    }
        //                }
        //            }

        //            if (!isHoldingAllowedWeapon)
        //            {
        //                continue;
        //            }
        //        }

        //        // Check distance
        //        if (counterCondition.distance != -1)
        //        {
        //            if (counterCondition.distanceCompareMode == 0)
        //            {
        //                if (Vector3.Distance(GM.CurrentPlayerBody.transform.position, hitPoint) < counterCondition.distance)
        //                {
        //                    continue;
        //                }
        //            }
        //            else
        //            {
        //                if (Vector3.Distance(GM.CurrentPlayerBody.transform.position, hitPoint) > counterCondition.distance)
        //                {
        //                    continue;
        //                }
        //            }
        //        }

        //        // Check constraint counters (Location, Equipment, HealthEffect, InZone)
        //        bool constrained = false;
        //        foreach (TraderTaskCounterCondition otherCounterCondition in counterCondition.parentCondition.counters)
        //        {
        //            if (!TraderStatus.CheckCounterConditionConstraint(otherCounterCondition))
        //            {
        //                constrained = true;
        //                break;
        //            }
        //        }
        //        if (constrained)
        //        {
        //            continue;
        //        }

        //        // Successful shot, increment count and update fulfillment 
        //        ++counterCondition.shotCount;
        //        TraderStatus.UpdateCounterConditionFulfillment(counterCondition);
        //    }
        //}
    }

    // Patches FVRPlayerBody.HealPercent to keep track of player's healing from H3 sources like dings and other powerups
    class PlayerBodyHealPercentPatch
    {
        static void Prefix(ref FVRPlayerBody __instance, float f, ref float ___m_startingHealth)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            float amountHealed = Mathf.Max(___m_startingHealth * f, ___m_startingHealth - __instance.Health);
            for (int i = 0; i < 7; ++i)
            {
                if (Mod.GetHealth(i) != 0)
                {
                    Mod.SetHealth(i, Mathf.Clamp(Mod.GetHealth(i) + amountHealed / 7, Mod.GetHealth(i), Mod.GetCurrentMaxHealth(i)));
                }
            }
        }
    }

    // Patches Object.Internal_CloneSingle to keep track of this type of instantiation
    class Internal_CloneSinglePatch
    {
        static void Postfix(ref UnityEngine.Object __result)
        {
            if (Mod.skipNextInstantiation || __result == null || !Mod.inMeatovScene)
            {
                Mod.skipNextInstantiation = false;
                return;
            }

            if (__result is GameObject)
            {
                GameObject go = __result as GameObject;
                if(Mod.instantiatedItem == go)
                {
                    // This item was already setup in Mod.H3MP_OnInstantiationTrack
                    Mod.instantiatedItem = null;
                }
                else
                {
                    // Item not yet setup if possible, continue
                    MeatovItem meatovItem = go.GetComponent<MeatovItem>();
                    FVRPhysicalObject physicalObject = go.GetComponent<FVRPhysicalObject>();
                    if (physicalObject != null && physicalObject.ObjectWrapper != null && meatovItem == null)
                    {
                        MeatovItem.Setup(physicalObject);
                        Mod.instantiatedItem = go;
                    }
                }
            }
        }
    }

    // Patches Object.Internal_CloneSingleWithParent to keep track of this type of instantiation
    class Internal_CloneSingleWithParentPatch
    {
        static void Postfix(ref UnityEngine.Object __result, Transform parent)
        {
            if (Mod.skipNextInstantiation || __result == null || !Mod.inMeatovScene)
            {
                Mod.skipNextInstantiation = false;
                return;
            }

            if (__result is GameObject)
            {
                GameObject go = __result as GameObject;
                if (Mod.instantiatedItem == go)
                {
                    // This item was already setup in Mod.H3MP_OnInstantiationTrack
                    Mod.instantiatedItem = null;
                }
                else
                {
                    // Item not yet setup if possible, continue
                    MeatovItem meatovItem = go.GetComponent<MeatovItem>();
                    FVRPhysicalObject physicalObject = go.GetComponent<FVRPhysicalObject>();
                    if (physicalObject != null && physicalObject.ObjectWrapper != null && meatovItem == null)
                    {
                        MeatovItem.Setup(physicalObject);
                        Mod.instantiatedItem = go;
                    }
                }
            }
        }
    }

    // Patches Object.Internal_InstantiateSingle to keep track of this type of instantiation
    class Internal_InstantiateSinglePatch
    {
        static void Postfix(ref UnityEngine.Object __result)
        {
            if (Mod.skipNextInstantiation || __result == null || !Mod.inMeatovScene)
            {
                Mod.skipNextInstantiation = false;
                return;
            }

            if (__result is GameObject)
            {
                GameObject go = __result as GameObject;
                if (Mod.instantiatedItem == go)
                {
                    // This item was already setup in Mod.H3MP_OnInstantiationTrack
                    Mod.instantiatedItem = null;
                }
                else
                {
                    // Item not yet setup if possible, continue
                    MeatovItem meatovItem = go.GetComponent<MeatovItem>();
                    FVRPhysicalObject physicalObject = go.GetComponent<FVRPhysicalObject>();
                    if (physicalObject != null && physicalObject.ObjectWrapper != null && meatovItem == null)
                    {
                        MeatovItem.Setup(physicalObject);
                        Mod.instantiatedItem = go;
                    }
                }
            }
        }
    }

    // Patches Object.Internal_InstantiateSingleWithParent to keep track of this type of instantiation
    class Internal_InstantiateSingleWithParentPatch
    {
        static void Postfix(ref UnityEngine.Object __result, Transform parent)
        {
            if (Mod.skipNextInstantiation || __result == null || !Mod.inMeatovScene)
            {
                Mod.skipNextInstantiation = false;
                return;
            }

            if (__result is GameObject)
            {
                GameObject go = __result as GameObject;
                if (Mod.instantiatedItem == go)
                {
                    // This item was already setup in Mod.H3MP_OnInstantiationTrack
                    Mod.instantiatedItem = null;
                }
                else
                {
                    // Item not yet setup if possible, continue
                    MeatovItem meatovItem = go.GetComponent<MeatovItem>();
                    FVRPhysicalObject physicalObject = go.GetComponent<FVRPhysicalObject>();
                    if (physicalObject != null && physicalObject.ObjectWrapper != null && meatovItem == null)
                    {
                        MeatovItem.Setup(physicalObject);
                        Mod.instantiatedItem = go;
                    }
                }
            }
        }
    }

    // Patches Sosig
    class SosigPatch
    {
        static bool ExecuteDoorManipulationPrefix(Sosig __instance, ref float linkTime)
        {
            if (!Mod.inMeatovScene || !(__instance.m_currentlinkExtension is NavMeshLinkDoor))
            {
                return true;
            }

            NavMeshLinkDoor doorLink = __instance.m_currentlinkExtension as NavMeshLinkDoor;
            bool isFromInside = doorLink.IsFromInside;
            bool closed = doorLink.EFMDoor.closedMinRot ? doorLink.EFMDoor.rotAngle == doorLink.EFMDoor.minRot : doorLink.EFMDoor.rotAngle == doorLink.EFMDoor.maxRot;
            bool cantManipulate = closed && doorLink.EFMDoor.lockScript != null && doorLink.EFMDoor.lockScript.locked;
            bool flag4 = __instance.HasABrain;
            flag4 = (__instance.m_aggrolevel <= 0.5f && __instance.m_pathWith != null && __instance.m_pathWith.Count <= 1);
            if (!__instance.HasABrain)
            {
                flag4 = false;
            }
            float num = linkTime + Time.deltaTime;
            if (cantManipulate) // Door closed and locked 
            {
                AI AIScript = __instance.GetComponent<AI>();
                int keyCount = 0;
                if (AIScript.botInventory.inventory.TryGetValue(doorLink.EFMDoor.lockScript.keyID, out keyCount) && keyCount > 0) // Have key
                {
                    doorLink.EFMDoor.lockScript.UnlockAction(true);
                    doorLink.EFMDoor.Open();
                }
                else if (doorLink.EFMDoor.breachable) // Missing key, but breachable, try to breach if on correct side
                {
                    for (int i = 0; i < doorLink.EFMDoor.breachers.Length; ++i)
                    {
                        if (doorLink.EFMDoor.breachers[i].correctSide)
                        {
                            Vector3 sosigVector = __instance.Links[1].transform.position - doorLink.EFMDoor.breachers[i].directionCheckTransform.position;
                            if (Vector3.Angle(doorLink.EFMDoor.breachers[i].directionCheckTransform.forward, sosigVector) < 90)
                            {
                                doorLink.EFMDoor.AttemptBreach(doorLink.EFMDoor.breachers[i].correctSide);
                            }
                        }
                    }
                }
            }
            else // Door manipulatable
            {
                // Open fully if not already
                if(doorLink.EFMDoor.closedMinRot ? doorLink.EFMDoor.rotAngle != doorLink.EFMDoor.maxRot : doorLink.EFMDoor.rotAngle != doorLink.EFMDoor.minRot)
                {
                    doorLink.EFMDoor.Open();
                }
            }
            linkTime = num;

            // Fail the link traversal if door is still closed
            if (doorLink.EFMDoor.closedMinRot ? doorLink.EFMDoor.rotAngle == doorLink.EFMDoor.minRot : doorLink.EFMDoor.rotAngle == doorLink.EFMDoor.maxRot)
            {
                __instance.EndLink(false);
            }

            return false;
        }

        static bool SuppresionUpdatePrefix(Sosig __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }

            if (__instance.m_isInvuln || __instance.m_isDamResist)
            {
                __instance.m_suppressionLevel = 0f;
            }
            // No matter body state, decrease suppression level faster
            if (__instance.m_suppressionLevel > 0f)
            {
                __instance.m_suppressionLevel -= Time.deltaTime * 5f;
            }

            return false;
        }
    }

    // Patches SosigHand
    class SosigHandPatch
    {
        static bool HoldPrefix(SosigHand __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }

            if (!__instance.IsHoldingObject || __instance.HeldObject == null)
            {
                return false;
            }
            if (__instance.Root == null)
            {
                return false;
            }
            __instance.UpdateGunHandlingPose();
            Vector3 position = __instance.Target.position;
            Quaternion rotation = __instance.Target.rotation;
            Vector3 position2 = __instance.HeldObject.RecoilHolder.position;
            Quaternion rotation2 = __instance.HeldObject.RecoilHolder.rotation;
            if (__instance.HeldObject.O.IsHeld)
            {
                float num = Vector3.Distance(position, position2);
                if (num > 0.7f)
                {
                    __instance.DropHeldObject();
                    return false;
                }
            }
            else
            {
                float num2 = Vector3.Distance(position, position2);
                if (num2 < 0.2f)
                {
                    __instance.m_timeAwayFromTarget = 0f;
                }
                else
                {
                    __instance.m_timeAwayFromTarget += Time.deltaTime;
                    if (__instance.m_timeAwayFromTarget > 1f)
                    {
                        __instance.HeldObject.O.RootRigidbody.position = position;
                        __instance.HeldObject.O.RootRigidbody.rotation = rotation;
                    }
                }
            }
            if ((__instance.HeldObject.Type == SosigWeapon.SosigWeaponType.Melee || __instance.HeldObject.Type == SosigWeapon.SosigWeaponType.Grenade) && __instance.HeldObject.O.MP.IsMeleeWeapon)
            {
                Vector3 vector = __instance.Target.position - __instance.m_lastPos;
                vector *= 1f / Time.deltaTime;
                __instance.HeldObject.O.SetFakeHand(vector, __instance.Target.position);
            }
            float d = 0f;
            float d2 = 0f;
            float num3 = 0f;
            if (__instance.m_posedToward != null && __instance.Pose != SosigHand.SosigHandPose.Melee)
            {
                if (__instance.HasActiveAimPoint)
                {
                    if (__instance.Pose == SosigHand.SosigHandPose.Aimed)
                    {
                        d = __instance.vertOffsets[__instance.m_curFiringPose_Aimed];
                        d2 = __instance.forwardOffsets[__instance.m_curFiringPose_Aimed];
                        num3 = __instance.tiltLerpOffsets[__instance.m_curFiringPose_Aimed];
                    }
                    else if (__instance.Pose == SosigHand.SosigHandPose.HipFire)
                    {
                        d = __instance.vertOffsets[__instance.m_curFiringPose_Hip];
                        d2 = __instance.forwardOffsets[__instance.m_curFiringPose_Hip];
                        num3 = __instance.tiltLerpOffsets[__instance.m_curFiringPose_Hip];
                    }
                }
                Transform transform = __instance.S.Links[1].transform;
                float num4 = 4f;
                if (__instance.S.IsFrozen)
                {
                    num4 = 0.25f;
                }
                if (__instance.S.IsSpeedUp)
                {
                    num4 = 8f;
                }
                __instance.Target.position = Vector3.Lerp(position, __instance.m_posedToward.position + transform.up * d + __instance.m_posedToward.forward * d2, Time.deltaTime * num4);
                __instance.Target.rotation = Quaternion.Slerp(rotation, __instance.m_posedToward.rotation, Time.deltaTime * num4);
            }
            Vector3 b = position2;
            Quaternion rotation3 = rotation2;
            Vector3 a = position;
            Quaternion lhs = rotation;
            if (__instance.HasActiveAimPoint && (__instance.Pose == SosigHand.SosigHandPose.HipFire || __instance.Pose == SosigHand.SosigHandPose.Aimed))
            {
                float num5 = 0f;
                float num6 = 0f;
                if (__instance.Pose == SosigHand.SosigHandPose.HipFire)
                {
                    num5 = __instance.HeldObject.Hipfire_HorizontalLimit;
                    num6 = __instance.HeldObject.Hipfire_VerticalLimit;
                }
                if (__instance.Pose == SosigHand.SosigHandPose.Aimed)
                {
                    num5 = __instance.HeldObject.Aim_HorizontalLimit;
                    num6 = __instance.HeldObject.Aim_VerticalLimit;
                }
                Vector3 vector2 = __instance.m_aimTowardPoint - position;
                Vector3 forward = __instance.Target.forward;
                Vector3 current = Vector3.RotateTowards(forward, Vector3.ProjectOnPlane(vector2, __instance.Target.right), num6 * 0.0174533f, 0f);
                Vector3 forward2 = Vector3.RotateTowards(current, vector2, num5 * 0.0174533f, 0f);
                if (num3 > 0f)
                {
                    Vector3 localPosition = __instance.Target.transform.localPosition;
                    localPosition.z = 0f;
                    localPosition.y = 0f;
                    localPosition.Normalize();
                    Vector3 upwards = Vector3.Slerp(__instance.Target.up, localPosition.x * -__instance.Target.right, num3);
                    lhs = Quaternion.LookRotation(forward2, upwards);
                }
                else
                {
                    lhs = Quaternion.LookRotation(forward2, __instance.Target.up);
                }
            }
            Vector3 a2 = a - b;
            Quaternion quaternion = lhs * Quaternion.Inverse(rotation3);
            float deltaTime = Time.deltaTime;
            float num7;
            Vector3 a3;
            quaternion.ToAngleAxis(out num7, out a3);
            float d3 = 0.5f;
            if (__instance.S.IsConfused)
            {
                d3 = 0.1f;
            }
            if (__instance.S.IsStunned || __instance.S.IsUnconscious)
            {
                d3 = 0.02f;
            }
            if (num7 > 180f)
            {
                num7 -= 360f;
            }
            // Make ADS faster
            if (num7 != 0f)
            {
                Vector3 target = deltaTime * num7 * a3 * __instance.S.AttachedRotationMultiplier * __instance.HeldObject.PosRotMult * d3;
                __instance.HeldObject.O.RootRigidbody.angularVelocity = Vector3.MoveTowards(__instance.HeldObject.O.RootRigidbody.angularVelocity, target, __instance.S.AttachedRotationFudge * 5 * Time.fixedDeltaTime);
            }
            Vector3 target2 = a2 * __instance.S.AttachedPositionMultiplier * 0.5f * __instance.HeldObject.PosStrengthMult * deltaTime;
            __instance.HeldObject.O.RootRigidbody.velocity = Vector3.MoveTowards(__instance.HeldObject.O.RootRigidbody.velocity, target2, __instance.S.AttachedPositionFudge * 5 * deltaTime);
            __instance.m_lastPos = __instance.Target.position;

            return false;
        }
    }

    // Patches ModularWorkshopUI
    public class ModularWorkshopUIPatch
    {
        public static List<KeyValuePair<int, MeatovItem>> toDisplay;

        public static ModularWorkshopUI debugInstance;

        // Patches UpdateDisplay to prevent display of parts player doesn't have in workbench volume
        static bool UpdateDisplayPrefix(ModularWorkshopUI __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }
            debugInstance = __instance;

            if (__instance.DisplayNameText != null && !__instance._skinOnlyMode)
            {
                __instance.DisplayNameText.text = ModularWorkshopManager.ModularWorkshopPartsGroupsDictionary[__instance.ModularPartsGroupID].DisplayName;
            }
            else if (__instance.DisplayNameText != null && __instance._skinOnlyMode)
            {
                __instance.DisplayNameText.text = "Receiver Skins";
            }
            if (__instance._isShowingUI)
            {
                GameObject mainCanvas = __instance.MainCanvas;
                if (mainCanvas != null)
                {
                    mainCanvas.SetActive(true);
                }
                GameObject showButton = __instance.ShowButton;
                if (showButton != null)
                {
                    showButton.SetActive(false);
                }
                GameObject hideButton = __instance.HideButton;
                if (hideButton != null)
                {
                    hideButton.SetActive(true);
                }
                int maxPage = 0;
                if (!__instance._isShowingSkins)
                {
                    // Build list of parts we want to display
                    int count = 0;
                    toDisplay = new List<KeyValuePair<int, MeatovItem>>();
                    for (int i = 0; i < __instance._partNames.Length; ++i)
                    {
                        byte zero = 0;
                        // Note that here, we assume that there can only be a single none part to a group and it that it is the first in the part list
                        if (Mod.noneModulParts.TryGetValue(__instance.ModularPartsGroupID, out Dictionary<string, byte> noneParts) 
                            && noneParts.TryGetValue(__instance._partNames[i], out zero))
                        {
                            toDisplay.Add(new KeyValuePair<int, MeatovItem>(i, null));
                            count = 1;
                        }

                        // Ensure we have currently selected part in the list to display
                        if (i == __instance._selectedPart)
                        {
                            MeatovItem partMeatovItem = null;
                            switch (__instance.PartType)
                            {
                                case ModularWorkshopUI.EPartType.Barrel:
                                    partMeatovItem = __instance.ModularWeapon.ModularBarrelPoint.GetComponentInChildren<MeatovItem>();
                                    break;
                                case ModularWorkshopUI.EPartType.Handguard:
                                    partMeatovItem = __instance.ModularWeapon.ModularHandguardPoint.GetComponentInChildren<MeatovItem>();
                                    break;
                                case ModularWorkshopUI.EPartType.Stock:
                                    partMeatovItem = __instance.ModularWeapon.ModularStockPoint.GetComponentInChildren<MeatovItem>();
                                    break;
                                case ModularWorkshopUI.EPartType.MainWeaponGeneralAttachmentPoint:
                                    __instance.ModularWeapon.ModularWeaponPartsAttachmentPoints.Single((ModularWeaponPartsAttachmentPoint obj) => obj.ModularPartsGroupID == __instance.ModularPartsGroupID).ModularPartPoint.GetComponentInChildren<MeatovItem>();
                                    break;
                                case ModularWorkshopUI.EPartType.SubAttachmentPoint:
                                    __instance.ModularWeapon.SubAttachmentPoints.Single((ModularWeaponPartsAttachmentPoint obj) => obj.ModularPartsGroupID == __instance.ModularPartsGroupID).ModularPartPoint.GetComponentInChildren<MeatovItem>();
                                    break;
                            }

                            if(partMeatovItem != null)
                            {
                                toDisplay.Add(new KeyValuePair<int, MeatovItem>(i, partMeatovItem));
                                ++count;
                            }
                        }

                        if (HideoutController.instance.areaController.areas[10].availableModulParts.TryGetValue(__instance.ModularPartsGroupID, out Dictionary<string, List<MeatovItem>> partsDict)
                            && partsDict.TryGetValue(__instance._partNames[i], out List<MeatovItem> itemList))
                        {
                            for (int j=itemList.Count - 1; j >= 0; --j)
                            {
                                if (itemList[j] == null)
                                {
                                    itemList.RemoveAt(j);
                                }
                                else
                                {
                                    toDisplay.Add(new KeyValuePair<int, MeatovItem>(i, itemList[j]));
                                    ++count;
                                }
                            }
                        }
                    }

                    // Calculate indices
                    maxPage = count / __instance.PartButtons.Length; // 0 based
                    int startIndex = maxPage * __instance.PartButtons.Length; // 0 based

                    // Adjust page if necessary
                    if (maxPage < __instance._pageIndex)
                    {
                        __instance._pageIndex = maxPage;
                    }

                    // Display parts
                    for (int i = startIndex, j = 0; j < __instance.PartButtons.Length; ++i, ++j)
                    {
                        if (i < toDisplay.Count)
                        {
                            __instance.PartButtons[j].SetActive(true);
                            __instance.PartTexts[j].text = __instance._partNames[toDisplay[i].Key];
                            __instance.PartImages[j].sprite = __instance._partSprites[toDisplay[i].Key];
                        }
                        else
                        {
                            __instance.PartButtons[j].SetActive(false);
                        }
                    }
                }
                else
                {
                    for (int j = 0; j < __instance.PartButtons.Length; j++)
                    {
                        if (j + __instance.PartButtons.Length * __instance._skinPageIndex < __instance._skinDictionary.Count)
                        {
                            __instance.PartButtons[j].SetActive(true);
                            __instance.PartTexts[j].text = __instance._skinDisplayNames[j + __instance.PartButtons.Length * __instance._skinPageIndex];
                            __instance.PartImages[j].sprite = __instance._skinSprites[j + __instance.PartButtons.Length * __instance._skinPageIndex];
                        }
                        else
                        {
                            __instance.PartButtons[j].SetActive(false);
                        }
                    }
                }
                if (!__instance._isShowingSkins && __instance._pageIndex == 0)
                {
                    __instance.BackButton.SetActive(false);
                }
                else if (__instance._isShowingSkins && __instance._skinPageIndex == 0)
                {
                    __instance.BackButton.SetActive(false);
                }
                else
                {
                    __instance.BackButton.SetActive(true);
                }
                if (!__instance._isShowingSkins && __instance._pageIndex < maxPage)
                {
                    __instance.NextButton.SetActive(true);
                }
                else if (__instance._isShowingSkins && __instance._skinDictionary.Count > __instance.PartButtons.Length * (1 + __instance._skinPageIndex))
                {
                    __instance.NextButton.SetActive(true);
                }
                else
                {
                    __instance.NextButton.SetActive(false);
                }
                __instance.ButtonSet.SetSelectedButton(__instance._selectedButton);
                if (!__instance._skinOnlyMode)
                {
                    string str = __instance._partNames[__instance._selectedPart];
                    ModularWorkshopSkinsDefinition modularWorkshopSkinsDefinition;
                    if (!__instance._isShowingSkins && ModularWorkshopManager.ModularWorkshopSkinsDictionary.TryGetValue(__instance.ModularPartsGroupID + "/" + str, out modularWorkshopSkinsDefinition) && modularWorkshopSkinsDefinition.SkinDictionary.Count > 1)
                    {
                        __instance.ShowSkinsButton.SetActive(true);
                    }
                    else
                    {
                        __instance.ShowSkinsButton.SetActive(false);
                    }
                }
                if (__instance._isShowingSkins && !__instance._skinOnlyMode)
                {
                    __instance.HideSkinsButton.SetActive(true);
                }
                else
                {
                    __instance.HideSkinsButton.SetActive(false);
                }
                if (__instance.PageIndex != null)
                {
                    __instance.PageIndex.text = (__instance._isShowingSkins ? string.Format("{0}/{1}", 1 + __instance._skinPageIndex, Mathf.CeilToInt((float)(1 + (__instance._skinDictionary.Count - 1) / __instance.PartButtons.Length))) : string.Format("{0}/{1}", 1 + __instance._pageIndex, Mathf.CeilToInt((float)(1 + (__instance._partDictionary.Count - 1) / __instance.PartButtons.Length))));
                    return false;
                }
            }
            else
            {
                __instance.MainCanvas.SetActive(false);
                if (!__instance._skinOnlyMode && __instance._partDictionary.Count > 1)
                {
                    __instance.ShowButton.SetActive(true);
                }
                else if (__instance._skinOnlyMode && __instance._skinDictionary.Count > 1)
                {
                    __instance.ShowButton.SetActive(true);
                }
                else
                {
                    __instance.ShowButton.SetActive(false);
                }
                __instance.HideButton.SetActive(false);
                if (!__instance._skinOnlyMode)
                {
                    __instance.ShowButtonText.text = ModularWorkshopManager.ModularWorkshopPartsGroupsDictionary[__instance.ModularPartsGroupID].DisplayName;
                    return false;
                }
                __instance.ShowButtonText.text = "Receiver Skin";
            }

            return false;
        }

        // Patches PButton_Select to ensure _selectedPart is set correctly
        static bool PButton_SelectPrefix(ModularWorkshopUI __instance, int i)
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }

            __instance._selectedButton = i;
            if (!__instance._isShowingSkins)
            {
                __instance._selectedPart = toDisplay == null ? 0 : toDisplay[__instance.PartButtons.Length * __instance._pageIndex + i].Key;
                ModularWeaponPartPatch.overrideItem = toDisplay[__instance.PartButtons.Length * __instance._pageIndex + i].Value;

                // If this is a none item, make sure we don't try to add a meatov item to it and find data for it
                if(ModularWeaponPartPatch.overrideItem == null)
                {
                    ModularWeaponPartPatch.overrideItemNone = true;
                }
                __instance.PBButton_ApplyPart();
            }
            else
            {
                __instance._selectedSkin = __instance._selectedButton + __instance._skinPageIndex * __instance.PartButtons.Length;
                __instance.PBButton_ApplySkin();
            }
            __instance.UpdateDisplay();

            return false;
        }
    }

    public class NavMeshLinkExtensionPatch
    {
        static bool InitDoorPrefix(NavMeshLinkExtension __instance)
        {
            if (!Mod.inMeatovScene || !(__instance is NavMeshLinkDoor))
            {
                return true;
            }

            __instance.m_hasInit = true;

            return false;
        }

        static bool TraversePrefix(NavMeshLinkExtension __instance, ref float lerp, float speed, Sosig S, Vector3 endPos, Vector3 startPos)
        {
            if (!Mod.inMeatovScene || !(__instance is NavMeshLinkDoor))
            {
                return true;
            }

            speed = Mathf.Clamp(speed, 0f, 1.5f);
            S.Agent.transform.position = Vector3.MoveTowards(S.Agent.transform.position, endPos, Time.deltaTime * speed);
            lerp = __instance.InverseLerp(startPos, endPos, S.Agent.transform.position);
            lerp = Mathf.Clamp(lerp, 0f, 1f);
            if (Vector3.Distance(S.Agent.transform.position, endPos) < 0.0025f)
            {
                S.EndLink(true);
            }

            return false;
        }

        static bool UpdatePrefix(NavMeshLinkExtension __instance)
        {
            if (!Mod.inMeatovScene || !(__instance is NavMeshLinkDoor))
            {
                return true;
            }

            if (__instance.Type == NavMeshLinkExtension.NavMeshLinkType.Door)
            {
                NavMeshLinkDoor doorLink = __instance as NavMeshLinkDoor;
                if (doorLink.EFMDoor.closedMinRot ? doorLink.EFMDoor.rotAngle == doorLink.EFMDoor.minRot : doorLink.EFMDoor.rotAngle == doorLink.EFMDoor.maxRot)
                {
                    if (doorLink.EFMDoor.lockScript == null || !doorLink.EFMDoor.lockScript.locked)
                    {
                        __instance.Link.costOverride = __instance.DoorLinkCosts.y;
                    }
                    else
                    {
                        __instance.Link.costOverride = __instance.DoorLinkCosts.z;
                    }
                }
                else
                {
                    __instance.Link.costOverride = __instance.DoorLinkCosts.x;
                    __instance.m_doorOpenAttempts = 0;
                }
                if (!__instance.m_hasInit)
                {
                    __instance.initDelay -= Time.deltaTime;
                    if (__instance.initDelay <= 0f)
                    {
                        __instance.InitDoor();
                    }
                }
            }

            return false;
        }
    }
    #endregion
}

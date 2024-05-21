using FistVR;
using H3MP.Tracking;
using H3MP;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.SceneManagement;
using H3MP.Patches;
using H3MP.Scripts;
using System.Net.Mail;
using static FistVR.SosigInventory;
using FFmpeg.AutoGen;

namespace EFM
{
    public class GamePatches
    {
        public static void DoPatching(Harmony harmony)
        {
            //// LoadLevelBeginPatch
            //MethodInfo loadLevelBeginPatchOriginal = typeof(SteamVR_LoadLevel).GetMethod("Begin", BindingFlags.Public | BindingFlags.Static);
            //MethodInfo loadLevelBeginPatchPrefix = typeof(LoadLevelBeginPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //PatchController.Verify(loadLevelBeginPatchOriginal, harmony, true);
            //harmony.Patch(loadLevelBeginPatchOriginal, new HarmonyMethod(loadLevelBeginPatchPrefix));

            //// EndInteractionPatch
            //MethodInfo endInteractionPatchOriginal = typeof(FVRInteractiveObject).GetMethod("EndInteraction", BindingFlags.Public | BindingFlags.Instance);
            //MethodInfo endInteractionPatchPostfix = typeof(EndInteractionPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            //PatchController.Verify(endInteractionPatchOriginal, harmony, true);
            //harmony.Patch(endInteractionPatchOriginal, null, new HarmonyMethod(endInteractionPatchPostfix));

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

            //// DamagePatch
            //MethodInfo damagePatchOriginal = typeof(FVRPlayerHitbox).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(Damage) }, null);
            //MethodInfo damagePatchPrefix = typeof(DamagePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //PatchController.Verify(damagePatchOriginal, harmony, true);
            //harmony.Patch(damagePatchOriginal, new HarmonyMethod(damagePatchPrefix));

            //// DamageFloatPatch
            //MethodInfo damageFloatPatchOriginal = typeof(FVRPlayerHitbox).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(float) }, null);
            //MethodInfo damageFloatPatchPrefix = typeof(DamageFloatPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //PatchController.Verify(damageFloatPatchOriginal, harmony, true);
            //harmony.Patch(damageFloatPatchOriginal, new HarmonyMethod(damageFloatPatchPrefix));

            //// DamageDealtPatch
            //MethodInfo damageDealtPatchOriginal = typeof(FVRPlayerHitbox).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(DamageDealt) }, null);
            //MethodInfo damageDealtPatchPrefix = typeof(DamageDealtPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //PatchController.Verify(damageDealtPatchOriginal, harmony, true);
            //harmony.Patch(damageDealtPatchOriginal, new HarmonyMethod(damageDealtPatchPrefix));

            //// HandTestColliderPatch
            //MethodInfo handTestColliderPatchOriginal = typeof(FVRViveHand).GetMethod("TestCollider", BindingFlags.Public | BindingFlags.Instance);
            //MethodInfo handTestColliderPatchPrefix = typeof(HandTestColliderPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //PatchController.Verify(handTestColliderPatchOriginal, harmony, true);
            //harmony.Patch(handTestColliderPatchOriginal, new HarmonyMethod(handTestColliderPatchPrefix));

            //// HandTriggerExitPatch
            //MethodInfo handTriggerExitPatchOriginal = typeof(FVRViveHand).GetMethod("HandTriggerExit", BindingFlags.Public | BindingFlags.Instance);
            //MethodInfo handTriggerExitPatchPrefix = typeof(HandTriggerExitPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //PatchController.Verify(handTriggerExitPatchOriginal, harmony, true);
            //harmony.Patch(handTriggerExitPatchOriginal, new HarmonyMethod(handTriggerExitPatchPrefix));

            //// KeyForwardBackPatch
            //MethodInfo keyForwardBackPatchOriginal = typeof(SideHingedDestructibleDoorDeadBoltKey).GetMethod("KeyForwardBack", BindingFlags.Public | BindingFlags.Instance);
            //MethodInfo keyForwardBackPatchPrefix = typeof(KeyForwardBackPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //PatchController.Verify(keyForwardBackPatchOriginal, harmony, true);
            //harmony.Patch(keyForwardBackPatchOriginal, new HarmonyMethod(keyForwardBackPatchPrefix));

            //// UpdateDisplayBasedOnTypePatch
            //MethodInfo updateDisplayBasedOnTypePatchOriginal = typeof(SideHingedDestructibleDoorDeadBoltKey).GetMethod("UpdateDisplayBasedOnType", BindingFlags.NonPublic | BindingFlags.Instance);
            //MethodInfo updateDisplayBasedOnTypePatchPrefix = typeof(UpdateDisplayBasedOnTypePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //PatchController.Verify(updateDisplayBasedOnTypePatchOriginal, harmony, true);
            //harmony.Patch(updateDisplayBasedOnTypePatchOriginal, new HarmonyMethod(updateDisplayBasedOnTypePatchPrefix));

            //// DoorInitPatch
            //MethodInfo doorInitPatchOriginal = typeof(SideHingedDestructibleDoor).GetMethod("Init", BindingFlags.Public | BindingFlags.Instance);
            //MethodInfo doorInitPatchPrefix = typeof(DoorInitPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //PatchController.Verify(doorInitPatchOriginal, harmony, true);
            //harmony.Patch(doorInitPatchOriginal, new HarmonyMethod(doorInitPatchPrefix));

            //// DeadBoltAwakePatch
            //MethodInfo deadBoltAwakePatchOriginal = typeof(SideHingedDestructibleDoorDeadBolt).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
            //MethodInfo deadBoltAwakePatchPostfix = typeof(DeadBoltAwakePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            //PatchController.Verify(deadBoltAwakePatchOriginal, harmony, true);
            //harmony.Patch(deadBoltAwakePatchOriginal, null, new HarmonyMethod(deadBoltAwakePatchPostfix));

            //// DeadBoltFVRFixedUpdatePatch
            //MethodInfo deadBoltFVRFixedUpdatePatchOriginal = typeof(SideHingedDestructibleDoorDeadBolt).GetMethod("FVRFixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            //MethodInfo deadBoltFVRFixedUpdatePatchPostfix = typeof(DeadBoltFVRFixedUpdatePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            //PatchController.Verify(deadBoltFVRFixedUpdatePatchOriginal, harmony, true);
            //harmony.Patch(deadBoltFVRFixedUpdatePatchOriginal, null, new HarmonyMethod(deadBoltFVRFixedUpdatePatchPostfix));

            // InteractiveSetAllCollidersToLayerPatch
            MethodInfo interactiveSetAllCollidersToLayerOriginal = typeof(FVRInteractiveObject).GetMethod("SetAllCollidersToLayer", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo interactiveSetAllCollidersToLayerPrefix = typeof(InteractiveSetAllCollidersToLayerPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(interactiveSetAllCollidersToLayerOriginal, harmony, true);
            harmony.Patch(interactiveSetAllCollidersToLayerOriginal, new HarmonyMethod(interactiveSetAllCollidersToLayerPrefix));

            //// HandUpdatePatch
            //MethodInfo handUpdatePatchOriginal = typeof(FVRViveHand).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            //MethodInfo handUpdatePatchPrefix = typeof(HandUpdatePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //PatchController.Verify(handUpdatePatchOriginal, harmony, true);
            //harmony.Patch(handUpdatePatchOriginal, new HarmonyMethod(handUpdatePatchPrefix));

            //// MagazineUpdateInteractionPatch
            //MethodInfo magazineUpdateInteractionPatchOriginal = typeof(FVRFireArmMagazine).GetMethod("UpdateInteraction", BindingFlags.Public | BindingFlags.Instance);
            //MethodInfo magazineUpdateInteractionPatchPostfix = typeof(MagazineUpdateInteractionPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);
            //MethodInfo magazineUpdateInteractionPatchTranspiler = typeof(MagazineUpdateInteractionPatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            //PatchController.Verify(magazineUpdateInteractionPatchOriginal, harmony, true);
            //harmony.Patch(magazineUpdateInteractionPatchOriginal, null, new HarmonyMethod(magazineUpdateInteractionPatchPostfix), new HarmonyMethod(magazineUpdateInteractionPatchTranspiler));

            //// ClipUpdateInteractionPatch
            //MethodInfo clipUpdateInteractionPatchOriginal = typeof(FVRFireArmClip).GetMethod("UpdateInteraction", BindingFlags.Public | BindingFlags.Instance);
            //MethodInfo clipUpdateInteractionPatchPostfix = typeof(ClipUpdateInteractionPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);
            //MethodInfo clipUpdateInteractionPatchTranspiler = typeof(ClipUpdateInteractionPatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            //PatchController.Verify(clipUpdateInteractionPatchOriginal, harmony, true);
            //harmony.Patch(clipUpdateInteractionPatchOriginal, null, new HarmonyMethod(clipUpdateInteractionPatchPostfix), new HarmonyMethod(clipUpdateInteractionPatchTranspiler));

            //// MovementManagerJumpPatch
            //MethodInfo movementManagerJumpPatchOriginal = typeof(FVRMovementManager).GetMethod("Jump", BindingFlags.NonPublic | BindingFlags.Instance);
            //MethodInfo movementManagerJumpPatchPrefix = typeof(MovementManagerJumpPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //PatchController.Verify(movementManagerJumpPatchOriginal, harmony, true);
            //harmony.Patch(movementManagerJumpPatchOriginal, new HarmonyMethod(movementManagerJumpPatchPrefix));

            //// MovementManagerTwinstickPatch
            //MethodInfo movementManagerTwinstickPatchOriginal = typeof(FVRMovementManager).GetMethod("HandUpdateTwinstick", BindingFlags.NonPublic | BindingFlags.Instance);
            //MethodInfo movementManagerTwinstickPatchPrefix = typeof(MovementManagerUpdatePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //PatchController.Verify(movementManagerTwinstickPatchOriginal, harmony, true);
            //harmony.Patch(movementManagerTwinstickPatchOriginal, new HarmonyMethod(movementManagerTwinstickPatchPrefix));

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

            //// ClipRemoveRoundPatch
            //MethodInfo clipRemoveRoundPatchOriginal = typeof(FVRFireArmClip).GetMethod("RemoveRound", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { }, null);
            //MethodInfo clipRemoveRoundPatchPrefix = typeof(ClipRemoveRoundPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            //MethodInfo clipRemoveRoundPatchPostfix = typeof(ClipRemoveRoundPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            //PatchController.Verify(clipRemoveRoundPatchOriginal, harmony, true);
            //harmony.Patch(clipRemoveRoundPatchOriginal, new HarmonyMethod(clipRemoveRoundPatchPrefix), new HarmonyMethod(clipRemoveRoundPatchPostfix));

            //// ClipRemoveRoundBoolPatch
            //MethodInfo clipRemoveRoundBoolPatchOriginal = typeof(FVRFireArmClip).GetMethod("RemoveRound", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(bool) }, null);
            //MethodInfo clipRemoveRoundBoolPatchPrefix = typeof(ClipRemoveRoundBoolPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            //MethodInfo clipRemoveRoundBoolPatchPostfix = typeof(ClipRemoveRoundBoolPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            //PatchController.Verify(clipRemoveRoundBoolPatchOriginal, harmony, true);
            //harmony.Patch(clipRemoveRoundBoolPatchOriginal, new HarmonyMethod(clipRemoveRoundBoolPatchPrefix), new HarmonyMethod(clipRemoveRoundBoolPatchPostfix));

            //// ClipRemoveRoundClassPatch
            //MethodInfo clipRemoveRoundClassPatchOriginal = typeof(FVRFireArmClip).GetMethod("RemoveRoundReturnClass", BindingFlags.Public | BindingFlags.Instance);
            //MethodInfo clipRemoveRoundClassPatchPrefix = typeof(ClipRemoveRoundClassPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            //MethodInfo clipRemoveRoundClassPatchPostfix = typeof(ClipRemoveRoundClassPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            //PatchController.Verify(clipRemoveRoundClassPatchOriginal, harmony, true);
            //harmony.Patch(clipRemoveRoundClassPatchOriginal, new HarmonyMethod(clipRemoveRoundClassPatchPrefix), new HarmonyMethod(clipRemoveRoundClassPatchPostfix));

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

            //// ClipAddRoundPatch
            //MethodInfo clipAddRoundPatchOriginal = typeof(FVRFireArmClip).GetMethod("AddRound", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(FVRFireArmRound), typeof(bool), typeof(bool) }, null);
            //MethodInfo clipAddRoundPatchPrefix = typeof(ClipAddRoundPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            //MethodInfo clipAddRoundPatchPostfix = typeof(ClipAddRoundPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            //PatchController.Verify(clipAddRoundPatchOriginal, harmony, true);
            //harmony.Patch(clipAddRoundPatchOriginal, new HarmonyMethod(clipAddRoundPatchPrefix), new HarmonyMethod(clipAddRoundPatchPostfix));

            //// ClipAddRoundClassPatch
            //MethodInfo clipAddRoundClassPatchOriginal = typeof(FVRFireArmClip).GetMethod("AddRound", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(FireArmRoundClass), typeof(bool), typeof(bool) }, null);
            //MethodInfo clipAddRoundClassPatchPrefix = typeof(ClipAddRoundClassPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            //MethodInfo clipAddRoundClassPatchPostfix = typeof(ClipAddRoundClassPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            //PatchController.Verify(clipAddRoundClassPatchOriginal, harmony, true);
            //harmony.Patch(clipAddRoundClassPatchOriginal, new HarmonyMethod(clipAddRoundClassPatchPrefix), new HarmonyMethod(clipAddRoundClassPatchPostfix));

            //// AttachmentMountRegisterPatch
            //MethodInfo attachmentMountRegisterPatchOriginal = typeof(FVRFireArmAttachmentMount).GetMethod("RegisterAttachment", BindingFlags.Public | BindingFlags.Instance);
            //MethodInfo attachmentMountRegisterPatchPrefix = typeof(AttachmentMountRegisterPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //PatchController.Verify(attachmentMountRegisterPatchOriginal, harmony, true);
            //harmony.Patch(attachmentMountRegisterPatchOriginal, new HarmonyMethod(attachmentMountRegisterPatchPrefix));

            //// AttachmentMountDeRegisterPatch
            //MethodInfo attachmentMountDeRegisterPatchOriginal = typeof(FVRFireArmAttachmentMount).GetMethod("DeRegisterAttachment", BindingFlags.Public | BindingFlags.Instance);
            //MethodInfo attachmentMountDeRegisterPatchPrefix = typeof(AttachmentMountDeRegisterPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //PatchController.Verify(attachmentMountDeRegisterPatchOriginal, harmony, true);
            //harmony.Patch(attachmentMountDeRegisterPatchOriginal, new HarmonyMethod(attachmentMountDeRegisterPatchPrefix));

            //// EntityCheckPatch
            //MethodInfo entityCheckPatchOriginal = typeof(AIManager).GetMethod("EntityCheck", BindingFlags.NonPublic | BindingFlags.Instance);
            //MethodInfo entityCheckPatchPrefix = typeof(EntityCheckPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //PatchController.Verify(entityCheckPatchOriginal, harmony, true);
            //harmony.Patch(entityCheckPatchOriginal, new HarmonyMethod(entityCheckPatchPrefix));

            //// ChamberEjectRoundPatch
            //MethodInfo chamberEjectRoundPatchOriginal = typeof(FVRFireArmChamber).GetMethod("EjectRound", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(bool) }, null);
            //MethodInfo chamberEjectRoundPatchAnimationOriginal = typeof(FVRFireArmChamber).GetMethod("EjectRound", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(Quaternion), typeof(bool) }, null);
            //MethodInfo chamberEjectRoundPatchPostfix = typeof(ChamberEjectRoundPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            //PatchController.Verify(chamberEjectRoundPatchOriginal, harmony, true);
            //PatchController.Verify(chamberEjectRoundPatchAnimationOriginal, harmony, true);
            //harmony.Patch(chamberEjectRoundPatchOriginal, null, new HarmonyMethod(chamberEjectRoundPatchPostfix));
            //harmony.Patch(chamberEjectRoundPatchAnimationOriginal, null, new HarmonyMethod(chamberEjectRoundPatchPostfix));

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

            // HandCurrentInteractableSetPatch
            MethodInfo handCurrentInteractableSetPatchOriginal = typeof(FVRViveHand).GetMethod("set_CurrentInteractable", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo handCurrentInteractableSetPatchPostfix = typeof(HandCurrentInteractableSetPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo handCurrentInteractableSetPatchPrefix = typeof(HandCurrentInteractableSetPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(handCurrentInteractableSetPatchOriginal, harmony, true);
            harmony.Patch(handCurrentInteractableSetPatchOriginal, new HarmonyMethod(handCurrentInteractableSetPatchPrefix), new HarmonyMethod(handCurrentInteractableSetPatchPostfix));

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

    // Patches SteamVR_LoadLevel.Begin() So we can keep certain objects from other scenes
    class LoadLevelBeginPatch
    {
        private static void SecureObject(GameObject objectToSecure)
        {
            Mod.LogInfo("Securing " + objectToSecure.name);
            Mod.securedObjects.Add(objectToSecure);
            GameObject.DontDestroyOnLoad(objectToSecure);
            MeatovItem CIW = objectToSecure.GetComponent<MeatovItem>();
            if (CIW != null)
            {
                // Items inside a rig will not be attached to the rig, so much secure them separately
                if (CIW.itemType == MeatovItem.ItemType.Rig || CIW.itemType == MeatovItem.ItemType.ArmoredRig)
                {
                    foreach (MeatovItem innerItem in CIW.itemsInSlots)
                    {
                        if (innerItem != null)
                        {
                            SecureObject(innerItem.gameObject);
                        }
                    }
                }

                // Secure any laser pointer hit objects
                LaserPointer[] laserPointers = objectToSecure.GetComponentsInChildren<LaserPointer>();
                foreach (LaserPointer laserPointer in laserPointers)
                {
                    if (laserPointer.BeamHitPoint != null && laserPointer.BeamHitPoint.transform.parent == null)
                    {
                        SecureObject(laserPointer.BeamHitPoint);
                    }
                }
            }
        }

        public static void SecureObjects(bool secureEquipment = false)
        {
            if (Mod.securedObjects == null)
            {
                Mod.securedObjects = new List<GameObject>();
            }
            Mod.securedObjects.Clear();

            // Secure the cameraRig
            GameObject cameraRig = GameObject.Find("[CameraRig]Fixed");
            Mod.securedObjects.Add(cameraRig);
            GameObject.DontDestroyOnLoad(cameraRig);

            if (secureEquipment)
            {
                // Secure held objects
                if (Mod.leftHand != null && Mod.leftHand.fvrHand != null)
                {
                    // harnessed will be a root item and will be secured alongside the rig
                    // not harnessed will be dropped in the world following endInteraction and needs to be secured separately
                    Mod.securedLeftHandInteractable = Mod.leftHand.fvrHand.CurrentInteractable;
                    if (Mod.securedLeftHandInteractable != null)
                    {
                        Mod.securedLeftHandInteractable.EndInteraction(Mod.leftHand.fvrHand);
                        if (Mod.securedLeftHandInteractable is FVRPhysicalObject && !(Mod.securedLeftHandInteractable as FVRPhysicalObject).m_isHardnessed)
                        {
                            SecureObject(Mod.securedLeftHandInteractable.gameObject);
                        }
                    }

                    Mod.securedRightHandInteractable = Mod.rightHand.fvrHand.CurrentInteractable;
                    if (Mod.securedRightHandInteractable != null)
                    {
                        Mod.securedRightHandInteractable.EndInteraction(Mod.rightHand.fvrHand);
                        if (Mod.securedRightHandInteractable is FVRPhysicalObject && !(Mod.securedRightHandInteractable as FVRPhysicalObject).m_isHardnessed)
                        {
                            SecureObject(Mod.securedRightHandInteractable.gameObject);
                        }
                    }
                }

                // Secure equipment
                if (StatusUI.instance.equipmentSlots != null)
                {
                    foreach (EquipmentSlot equipSlot in StatusUI.instance.equipmentSlots)
                    {
                        if (equipSlot != null && equipSlot.CurObject != null)
                        {
                            SecureObject(equipSlot.CurObject.gameObject);
                        }
                    }
                }

                // Secure pocket contents
                if (Mod.itemsInPocketSlots != null)
                {
                    foreach (MeatovItem itemInPocket in Mod.itemsInPocketSlots)
                    {
                        if (itemInPocket != null)
                        {
                            SecureObject(itemInPocket.gameObject);
                        }
                    }
                }

                // Secure right shoulder content
                if (Mod.rightShoulderObject != null)
                {
                    SecureObject(Mod.rightShoulderObject);
                }
            }

            // If leaving hideout and dont want to secure equipment
            // Or if just coming back from raid (which will secure equipment) and it was scav raid
            if ((!Mod.justFinishedRaid && !secureEquipment) || (Mod.justFinishedRaid && Mod.chosenCharIndex == 1)) // Make sure all items are removed from player logically
            {
                Mod.scavRaidReturnItems = new GameObject[15];

                // Drop items in hand
                if (GM.CurrentMovementManager.Hands[0].CurrentInteractable != null && !(GM.CurrentMovementManager.Hands[0].CurrentInteractable is FVRPhysicalObject))
                {
                    Mod.scavRaidReturnItems[0] = GM.CurrentMovementManager.Hands[0].CurrentInteractable.gameObject;
                    GM.CurrentMovementManager.Hands[0].CurrentInteractable.ForceBreakInteraction();
                }
                if (GM.CurrentMovementManager.Hands[1].CurrentInteractable != null && !(GM.CurrentMovementManager.Hands[1].CurrentInteractable is FVRPhysicalObject))
                {
                    Mod.scavRaidReturnItems[1] = GM.CurrentMovementManager.Hands[0].CurrentInteractable.gameObject;
                    GM.CurrentMovementManager.Hands[1].CurrentInteractable.ForceBreakInteraction();
                }

                // Unequip all equipment
                if (EquipmentSlot.wearingBackpack)
                {
                    Mod.scavRaidReturnItems[2] = EquipmentSlot.currentBackpack.gameObject;
                    MeatovItem backpackCIW = EquipmentSlot.currentBackpack;
                    FVRPhysicalObject backpackPhysObj = backpackCIW.GetComponent<FVRPhysicalObject>();
                    backpackPhysObj.SetQuickBeltSlot(null);
                    EquipmentSlot.TakeOffEquipment(backpackCIW);
                }
                if (EquipmentSlot.wearingBodyArmor)
                {
                    Mod.scavRaidReturnItems[3] = EquipmentSlot.currentArmor.gameObject;
                    MeatovItem bodyArmorCIW = EquipmentSlot.currentArmor;
                    FVRPhysicalObject bodyArmorPhysObj = bodyArmorCIW.GetComponent<FVRPhysicalObject>();
                    bodyArmorPhysObj.SetQuickBeltSlot(null);
                    EquipmentSlot.TakeOffEquipment(bodyArmorCIW);
                }
                if (EquipmentSlot.wearingEarpiece)
                {
                    Mod.scavRaidReturnItems[4] = EquipmentSlot.currentEarpiece.gameObject;
                    MeatovItem earPieceCIW = EquipmentSlot.currentEarpiece;
                    FVRPhysicalObject earPiecePhysObj = earPieceCIW.GetComponent<FVRPhysicalObject>();
                    earPiecePhysObj.SetQuickBeltSlot(null);
                    EquipmentSlot.TakeOffEquipment(earPieceCIW);
                }
                if (EquipmentSlot.wearingHeadwear)
                {
                    Mod.scavRaidReturnItems[5] = EquipmentSlot.currentHeadwear.gameObject;
                    MeatovItem headWearCIW = EquipmentSlot.currentHeadwear;
                    FVRPhysicalObject headWearPhysObj = headWearCIW.GetComponent<FVRPhysicalObject>();
                    headWearPhysObj.SetQuickBeltSlot(null);
                    EquipmentSlot.TakeOffEquipment(headWearCIW);
                }
                if (EquipmentSlot.wearingFaceCover)
                {
                    Mod.scavRaidReturnItems[6] = EquipmentSlot.currentFaceCover.gameObject;
                    MeatovItem faceCoverCIW = EquipmentSlot.currentFaceCover;
                    FVRPhysicalObject faceCoverPhysObj = faceCoverCIW.GetComponent<FVRPhysicalObject>();
                    faceCoverPhysObj.SetQuickBeltSlot(null);
                    EquipmentSlot.TakeOffEquipment(faceCoverCIW);
                }
                if (EquipmentSlot.wearingEyewear)
                {
                    Mod.scavRaidReturnItems[7] = EquipmentSlot.currentEyewear.gameObject;
                    MeatovItem eyeWearCIW = EquipmentSlot.currentEyewear;
                    FVRPhysicalObject eyeWearPhysObj = eyeWearCIW.GetComponent<FVRPhysicalObject>();
                    eyeWearPhysObj.SetQuickBeltSlot(null);
                    EquipmentSlot.TakeOffEquipment(eyeWearCIW);
                }
                if (EquipmentSlot.wearingRig)
                {
                    Mod.scavRaidReturnItems[8] = EquipmentSlot.currentRig.gameObject;
                    MeatovItem rigCIW = EquipmentSlot.currentRig;
                    FVRPhysicalObject rigPhysObj = rigCIW.GetComponent<FVRPhysicalObject>();
                    rigPhysObj.SetQuickBeltSlot(null);
                    EquipmentSlot.TakeOffEquipment(rigCIW);
                }
                if (EquipmentSlot.wearingPouch)
                {
                    Mod.scavRaidReturnItems[9] = EquipmentSlot.currentPouch.gameObject;
                    MeatovItem pouchCIW = EquipmentSlot.currentPouch;
                    FVRPhysicalObject pouchPhysObj = pouchCIW.GetComponent<FVRPhysicalObject>();
                    pouchPhysObj.SetQuickBeltSlot(null);
                    EquipmentSlot.TakeOffEquipment(pouchCIW);
                }

                // Right shoulder object
                if (Mod.rightShoulderObject != null)
                {
                    Mod.scavRaidReturnItems[10] = Mod.rightShoulderObject;
                    MeatovItem MI = Mod.rightShoulderObject.GetComponent<MeatovItem>();
                    FVRPhysicalObject rightShoulderPhysObj = MI.GetComponent<FVRPhysicalObject>();
                    rightShoulderPhysObj.SetQuickBeltSlot(null);
                    Mod.rightShoulderObject = null;
                }

                // Remove pockets' contents
                if (GM.CurrentPlayerBody.QBSlots_Internal != null && GM.CurrentPlayerBody.QBSlots_Internal.Count >= 4)
                {
                    for (int i = 0; i < 4; ++i)
                    {
                        if (GM.CurrentPlayerBody.QBSlots_Internal[i].CurObject != null)
                        {
                            Mod.scavRaidReturnItems[10 + i] = GM.CurrentPlayerBody.QBSlots_Internal[i].CurObject.gameObject;
                            FVRPhysicalObject pocketItemPhysObj = GM.CurrentPlayerBody.QBSlots_Internal[i].CurObject;
                            pocketItemPhysObj.SetQuickBeltSlot(null);
                        }
                    }
                }
            }

            // Secure sceneSettings
            GameObject sceneSettings = GameObject.Find("[SceneSettings_ModBlank_Simple]");
            Mod.securedObjects.Add(sceneSettings);
            GameObject.DontDestroyOnLoad(sceneSettings);

            // Secure Pooled sources
            FVRPooledAudioSource[] pooledAudioSources = FindObjectsOfTypeIncludingDisabled<FVRPooledAudioSource>();
            foreach (FVRPooledAudioSource pooledAudioSource in pooledAudioSources)
            {
                Mod.securedObjects.Add(pooledAudioSource.gameObject);
                GameObject.DontDestroyOnLoad(pooledAudioSource.gameObject);
            }

            // Secure grabbity spheres
            FVRViveHand rightViveHand = cameraRig.transform.GetChild(0).gameObject.GetComponent<FVRViveHand>();
            FVRViveHand leftViveHand = cameraRig.transform.GetChild(1).gameObject.GetComponent<FVRViveHand>();
            Mod.securedObjects.Add(rightViveHand.Grabbity_HoverSphere.gameObject);
            Mod.securedObjects.Add(rightViveHand.Grabbity_GrabSphere.gameObject);
            GameObject.DontDestroyOnLoad(rightViveHand.Grabbity_HoverSphere.gameObject);
            GameObject.DontDestroyOnLoad(rightViveHand.Grabbity_GrabSphere.gameObject);
            Mod.securedObjects.Add(leftViveHand.Grabbity_HoverSphere.gameObject);
            Mod.securedObjects.Add(leftViveHand.Grabbity_GrabSphere.gameObject);
            GameObject.DontDestroyOnLoad(leftViveHand.Grabbity_HoverSphere.gameObject);
            GameObject.DontDestroyOnLoad(leftViveHand.Grabbity_GrabSphere.gameObject);

            // Secure MovementManager objects
            Mod.securedObjects.Add(GM.CurrentMovementManager.MovementRig.gameObject);
            GameObject.DontDestroyOnLoad(GM.CurrentMovementManager.MovementRig.gameObject);
            // Movement arrows could be attached to movement manager if they are activated when we start loading
            // So only add them to the list if their parent is null
            GameObject touchPadArrows = (GameObject)(typeof(FVRMovementManager).GetField("m_touchpadArrows", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(GM.CurrentMovementManager));
            if (touchPadArrows.transform.parent == null)
            {
                Mod.securedObjects.Add(touchPadArrows);
                GameObject.DontDestroyOnLoad(touchPadArrows);
            }
            GameObject joystickTPArrows = (GameObject)(typeof(FVRMovementManager).GetField("m_joystickTPArrows", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(GM.CurrentMovementManager));
            if (joystickTPArrows.transform.parent == null)
            {
                Mod.securedObjects.Add(joystickTPArrows);
                GameObject.DontDestroyOnLoad(joystickTPArrows);
            }
            GameObject twinStickArrowsLeft = (GameObject)(typeof(FVRMovementManager).GetField("m_twinStickArrowsLeft", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(GM.CurrentMovementManager));
            if (twinStickArrowsLeft.transform.parent == null)
            {
                Mod.securedObjects.Add(twinStickArrowsLeft);
                GameObject.DontDestroyOnLoad(twinStickArrowsLeft);
            }
            GameObject twinStickArrowsRight = (GameObject)(typeof(FVRMovementManager).GetField("m_twinStickArrowsRight", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(GM.CurrentMovementManager));
            if (twinStickArrowsRight.transform.parent == null)
            {
                Mod.securedObjects.Add(twinStickArrowsRight);
                GameObject.DontDestroyOnLoad(twinStickArrowsRight);
            }
            GameObject floorHelper = (GameObject)(typeof(FVRMovementManager).GetField("m_floorHelper", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(GM.CurrentMovementManager));
            Mod.securedObjects.Add(floorHelper);
            GameObject.DontDestroyOnLoad(floorHelper);

            if (Mod.doorLeftPrefab == null)
            {
                // Secure doors
                Mod.doorLeftPrefab = GameObject.Instantiate(GameObject.Find("Door_KnobBolt_Left_Cherry"));
                Mod.doorLeftPrefab.SetActive(false);
                Mod.doorLeftPrefab.name = "Door_KnobBolt_Left_Cherry";
                GameObject.DontDestroyOnLoad(Mod.doorLeftPrefab);
                Mod.doorRightPrefab = GameObject.Instantiate(GameObject.Find("Door_KnobBolt_Right_Cherry"));
                Mod.doorRightPrefab.SetActive(false);
                Mod.doorRightPrefab.name = "Door_KnobBolt_Right_Cherry";
                GameObject.DontDestroyOnLoad(Mod.doorRightPrefab);
                Mod.doorDoublePrefab = GameObject.Instantiate(GameObject.Find("Door_KnobBolt_Double_Cherry"));
                Mod.doorDoublePrefab.SetActive(false);
                Mod.doorDoublePrefab.name = "Door_KnobBolt_Double_Cherry";
                GameObject.DontDestroyOnLoad(Mod.doorDoublePrefab);
            }
        }

        static T[] FindObjectsOfTypeIncludingDisabled<T>()
        {
            var ActiveScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var RootObjects = ActiveScene.GetRootGameObjects();
            var MatchObjects = new List<T>();

            foreach (var ro in RootObjects)
            {
                var Matches = ro.GetComponentsInChildren<T>(true);
                MatchObjects.AddRange(Matches);
            }

            return MatchObjects.ToArray();
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
    //     but now we instead drop everything on death instead
    // -4: Destroy all slots, this essentially sets the QBS config to None
    class ConfigureQuickbeltPatch
    {
        static bool Prefix(int index, ref FVRPlayerBody __instance)
        {
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
                // This is unused but was meant to be in case of death. Instead we jsut detach everything from player upon death and then laod base 
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
                    if (area.areaSlotsPerLevel != null && area.currentLevel < area.areaSlotsPerLevel.Length)
                    {
                        for(int j = 0; j < area.areaSlotsPerLevel[area.currentLevel].Length; ++j)
                        {
                            if (area.areaSlotsPerLevel[area.currentLevel][j].IsPointInsideMe(position))
                            {
                                fvrquickBeltSlot = area.areaSlotsPerLevel[area.currentLevel][j];
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
                            else if ((fvrquickBeltSlot as ShoulderStorage).right && (item.itemType == MeatovItem.ItemType.Firearm || fvrphysicalObject is FVRMeleeWeapon))
                            {
                                __instance.CurrentHoveredQuickbeltSlot = fvrquickBeltSlot;
                            }
                        }
                        else if (fvrquickBeltSlot is AreaSlot)
                        {
                            AreaSlot asAreaSlot = fvrquickBeltSlot as AreaSlot;
                            if(Mod.IDDescribedInList(item.H3ID, item.parents, asAreaSlot.filter, null))
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
        public static bool skipPatch;
        public static bool dontProcessRigWeight;

        static void Prefix(ref FVRQuickBeltSlot slot, ref FVRPhysicalObject __instance)
        {
            if (!Mod.inMeatovScene)
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
                }
                else if (__instance.QuickbeltSlot is AreaSlot)
                {
                    AreaSlot asAreaSlot = __instance.QuickbeltSlot as AreaSlot;
                    if(asAreaSlot.item == item)
                    {
                        asAreaSlot.item = null;
                        asAreaSlot.area.OnSlotContentChangedInvoke();
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
                                    rig.currentWeight -= item.currentWeight;
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
                            if (EquipmentSlot.currentRig.itemsInSlots[i] == item.gameObject)
                            {
                                EquipmentSlot.currentRig.itemsInSlots[i] = null;

                                if (!dontProcessRigWeight)
                                {
                                    EquipmentSlot.currentRig.currentWeight -= item.currentWeight;
                                }

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
            if (!Mod.inMeatovScene)
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

                return;
            }

            if (slot is AreaSlot)
            {
                AreaSlot asAreaSlot = __instance.QuickbeltSlot as AreaSlot;
                asAreaSlot.item = item;
                asAreaSlot.area.OnSlotContentChangedInvoke();

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
                            rig.currentWeight += item.currentWeight;
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
                        if (!dontProcessRigWeight)
                        {
                            parentRigItem.currentWeight += item.currentWeight;
                        }
                        parentRigItem.UpdateClosedMode();
                        break;
                    }
                }
            }
        }
    }

    // Patches FVRPhysicalObject.BeginInteraction() to manage which pose override to use
    class BeginInteractionPatch
    {
        static void Prefix(FVRViveHand hand, ref FVRPhysicalObject __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            MeatovItem item = __instance.GetComponent<MeatovItem>();
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
    }

    // Patches FVRPlayerHitbox.Damage(Damage) in order to implement our own armor's damage resistance
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
            if (hitbox == null & partIndex == -1)
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
                    if (Mod.health[0] <= 0)
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
                        Mod.LogInfo("\t\tCaused heavy bleed");
                        Effect heavyBleedEffect = new Effect();
                        heavyBleedEffect.effectType = Effect.EffectType.HeavyBleeding;
                        heavyBleedEffect.partIndex = 0;
                        if (vitalityLevel >= 51)
                        {
                            heavyBleedEffect.hasTimer = true;
                            heavyBleedEffect.timer = 30;
                        }
                        Effect.effects.Add(heavyBleedEffect);
                    }
                    else if (chance <= lightBleedChance)
                    {
                        Mod.LogInfo("\t\tCaused light bleed");
                        Effect lightBleedEffect = new Effect();
                        lightBleedEffect.effectType = Effect.EffectType.LightBleeding;
                        lightBleedEffect.partIndex = 0;
                        if (vitalityLevel >= 51)
                        {
                            lightBleedEffect.hasTimer = true;
                            lightBleedEffect.timer = 20;
                        }
                        Effect.effects.Add(lightBleedEffect);
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
                        if (Mod.health[1] <= 0)
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
                            Mod.LogInfo("\t\tCaused heavy bleed");
                            Effect heavyBleedEffect = new Effect();
                            heavyBleedEffect.effectType = Effect.EffectType.HeavyBleeding;
                            heavyBleedEffect.partIndex = actualPartIndex;
                            if (vitalityLevel >= 51)
                            {
                                heavyBleedEffect.hasTimer = true;
                                heavyBleedEffect.timer = 30;
                            }
                            Effect.effects.Add(heavyBleedEffect);
                        }
                        else if (bleedValue <= lightBleedChance)
                        {
                            Mod.LogInfo("\t\tCaused light bleed");
                            Effect lightBleedEffect = new Effect();
                            lightBleedEffect.effectType = Effect.EffectType.LightBleeding;
                            lightBleedEffect.partIndex = actualPartIndex;
                            if (vitalityLevel >= 51)
                            {
                                lightBleedEffect.hasTimer = true;
                                lightBleedEffect.timer = 20;
                            }
                            Effect.effects.Add(lightBleedEffect);
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
                            Mod.LogInfo("\t\tCaused heavy bleed");
                            Effect heavyBleedEffect = new Effect();
                            heavyBleedEffect.effectType = Effect.EffectType.HeavyBleeding;
                            heavyBleedEffect.partIndex = actualPartIndex;
                            if (vitalityLevel >= 51)
                            {
                                heavyBleedEffect.hasTimer = true;
                                heavyBleedEffect.timer = 30;
                            }
                            Effect.effects.Add(heavyBleedEffect);
                        }
                        else if (bleedValue <= lightBleedChance)
                        {
                            Mod.LogInfo("\t\tCaused light bleed");
                            Effect lightBleedEffect = new Effect();
                            lightBleedEffect.effectType = Effect.EffectType.LightBleeding;
                            lightBleedEffect.partIndex = actualPartIndex;
                            if (vitalityLevel >= 51)
                            {
                                lightBleedEffect.hasTimer = true;
                                lightBleedEffect.timer = 20;
                            }
                            Effect.effects.Add(lightBleedEffect);
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
                            Mod.LogInfo("\t\tCaused heavy bleed");
                            Effect heavyBleedEffect = new Effect();
                            heavyBleedEffect.effectType = Effect.EffectType.HeavyBleeding;
                            heavyBleedEffect.partIndex = actualPartIndex;
                            if (vitalityLevel >= 51)
                            {
                                heavyBleedEffect.hasTimer = true;
                                heavyBleedEffect.timer = 30;
                            }
                            Effect.effects.Add(heavyBleedEffect);
                        }
                        else if (bleedValue <= lightBleedChance)
                        {
                            Mod.LogInfo("\t\tCaused light bleed");
                            Effect lightBleedEffect = new Effect();
                            lightBleedEffect.effectType = Effect.EffectType.LightBleeding;
                            lightBleedEffect.partIndex = actualPartIndex;
                            if (vitalityLevel >= 51)
                            {
                                lightBleedEffect.hasTimer = true;
                                lightBleedEffect.timer = 20;
                            }
                            Effect.effects.Add(lightBleedEffect);
                        }

                        if (UnityEngine.Random.value < fractureChance)
                        {
                            Mod.LogInfo("\t\tCaused fracture");
                            Effect fractureEffect = new Effect();
                            fractureEffect.effectType = Effect.EffectType.Fracture;
                            fractureEffect.partIndex = actualPartIndex;
                            Effect.effects.Add(fractureEffect);
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
                            Mod.LogInfo("\t\tCaused heavy bleed");
                            Effect heavyBleedEffect = new Effect();
                            heavyBleedEffect.effectType = Effect.EffectType.HeavyBleeding;
                            heavyBleedEffect.partIndex = actualPartIndex;
                            if (vitalityLevel >= 51)
                            {
                                heavyBleedEffect.hasTimer = true;
                                heavyBleedEffect.timer = 30;
                            }
                            Effect.effects.Add(heavyBleedEffect);
                        }
                        else if (bleedValue <= lightBleedChance)
                        {
                            Mod.LogInfo("\t\tCaused light bleed");
                            Effect lightBleedEffect = new Effect();
                            lightBleedEffect.effectType = Effect.EffectType.LightBleeding;
                            lightBleedEffect.partIndex = actualPartIndex;
                            if (vitalityLevel >= 51)
                            {
                                lightBleedEffect.hasTimer = true;
                                lightBleedEffect.timer = 20;
                            }
                            Effect.effects.Add(lightBleedEffect);
                        }

                        if (UnityEngine.Random.value < fractureChance)
                        {
                            Mod.LogInfo("\t\tCaused fracture");
                            Effect fractureEffect = new Effect();
                            fractureEffect.effectType = Effect.EffectType.Fracture;
                            fractureEffect.partIndex = actualPartIndex;
                            Effect.effects.Add(fractureEffect);
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
                            Mod.LogInfo("\t\tCaused heavy bleed");
                            Effect heavyBleedEffect = new Effect();
                            heavyBleedEffect.effectType = Effect.EffectType.HeavyBleeding;
                            heavyBleedEffect.partIndex = actualPartIndex;
                            if (vitalityLevel >= 51)
                            {
                                heavyBleedEffect.hasTimer = true;
                                heavyBleedEffect.timer = 30;
                            }
                            Effect.effects.Add(heavyBleedEffect);
                        }
                        else if (bleedValue <= lightBleedChance)
                        {
                            Mod.LogInfo("\t\tCaused light bleed");
                            Effect lightBleedEffect = new Effect();
                            lightBleedEffect.effectType = Effect.EffectType.LightBleeding;
                            lightBleedEffect.partIndex = actualPartIndex;
                            if (vitalityLevel >= 51)
                            {
                                lightBleedEffect.hasTimer = true;
                                lightBleedEffect.timer = 20;
                            }
                            Effect.effects.Add(lightBleedEffect);
                        }

                        if (UnityEngine.Random.value < fractureChance)
                        {
                            Mod.LogInfo("\t\tCaused fracture");
                            Effect fractureEffect = new Effect();
                            fractureEffect.effectType = Effect.EffectType.Fracture;
                            fractureEffect.partIndex = actualPartIndex;
                            Effect.effects.Add(fractureEffect);
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
                            Mod.LogInfo("\t\tCaused heavy bleed");
                            Effect heavyBleedEffect = new Effect();
                            heavyBleedEffect.effectType = Effect.EffectType.HeavyBleeding;
                            heavyBleedEffect.partIndex = actualPartIndex;
                            if (vitalityLevel >= 51)
                            {
                                heavyBleedEffect.hasTimer = true;
                                heavyBleedEffect.timer = 30;
                            }
                            Effect.effects.Add(heavyBleedEffect);
                        }
                        else if (bleedValue <= lightBleedChance)
                        {
                            Mod.LogInfo("\t\tCaused light bleed");
                            Effect lightBleedEffect = new Effect();
                            lightBleedEffect.effectType = Effect.EffectType.LightBleeding;
                            lightBleedEffect.partIndex = actualPartIndex;
                            if (vitalityLevel >= 51)
                            {
                                lightBleedEffect.hasTimer = true;
                                lightBleedEffect.timer = 20;
                            }
                            Effect.effects.Add(lightBleedEffect);
                        }

                        if (UnityEngine.Random.value < fractureChance)
                        {
                            Mod.LogInfo("\t\tCaused fracture");
                            Effect fractureEffect = new Effect();
                            fractureEffect.effectType = Effect.EffectType.Fracture;
                            fractureEffect.partIndex = actualPartIndex;
                            Effect.effects.Add(fractureEffect);
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
                        Mod.LogInfo("\t\tCaused heavy bleed");
                        Effect heavyBleedEffect = new Effect();
                        heavyBleedEffect.effectType = Effect.EffectType.HeavyBleeding;
                        heavyBleedEffect.partIndex = actualPartIndex;
                        if (vitalityLevel >= 51)
                        {
                            heavyBleedEffect.hasTimer = true;
                            heavyBleedEffect.timer = 30;
                        }
                        Effect.effects.Add(heavyBleedEffect);
                    }
                    else if (bleedValue <= lightBleedChance)
                    {
                        Mod.LogInfo("\t\tCaused light bleed");
                        Effect lightBleedEffect = new Effect();
                        lightBleedEffect.effectType = Effect.EffectType.LightBleeding;
                        lightBleedEffect.partIndex = actualPartIndex;
                        if (vitalityLevel >= 51)
                        {
                            lightBleedEffect.hasTimer = true;
                            lightBleedEffect.timer = 20;
                        }
                        Effect.effects.Add(lightBleedEffect);
                    }

                    if (UnityEngine.Random.value < fractureChance)
                    {
                        Mod.LogInfo("\t\tCaused fracture");
                        Effect fractureEffect = new Effect();
                        fractureEffect.effectType = Effect.EffectType.Fracture;
                        fractureEffect.partIndex = actualPartIndex;
                        Effect.effects.Add(fractureEffect);
                    }
                }
            }
            else
            {
                switch (actualPartIndex)
                {
                    case 0: // Head
                        if (Mod.health[0] <= 0)
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
                            Effect heavyBleedEffect = new Effect();
                            heavyBleedEffect.effectType = Effect.EffectType.HeavyBleeding;
                            heavyBleedEffect.partIndex = 0;
                            if (vitalityLevel >= 51)
                            {
                                heavyBleedEffect.hasTimer = true;
                                heavyBleedEffect.timer = 30;
                            }
                            Effect.effects.Add(heavyBleedEffect);
                        }
                        else if (bleedValue0 <= lightBleedChance0)
                        {
                            Effect lightBleedEffect = new Effect();
                            lightBleedEffect.effectType = Effect.EffectType.LightBleeding;
                            lightBleedEffect.partIndex = 0;
                            if (vitalityLevel >= 51)
                            {
                                lightBleedEffect.hasTimer = true;
                                lightBleedEffect.timer = 20;
                            }
                            Effect.effects.Add(lightBleedEffect);
                        }
                        break;
                    case 1: // Thorax
                        if (Mod.health[1] <= 0)
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
                            Effect heavyBleedEffect = new Effect();
                            heavyBleedEffect.effectType = Effect.EffectType.HeavyBleeding;
                            heavyBleedEffect.partIndex = actualPartIndex;
                            if (vitalityLevel >= 51)
                            {
                                heavyBleedEffect.hasTimer = true;
                                heavyBleedEffect.timer = 30;
                            }
                            Effect.effects.Add(heavyBleedEffect);
                        }
                        else if (bleedValue1 <= lightBleedChance1)
                        {
                            Effect lightBleedEffect = new Effect();
                            lightBleedEffect.effectType = Effect.EffectType.LightBleeding;
                            lightBleedEffect.partIndex = actualPartIndex;
                            if (vitalityLevel >= 51)
                            {
                                lightBleedEffect.hasTimer = true;
                                lightBleedEffect.timer = 20;
                            }
                            Effect.effects.Add(lightBleedEffect);
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
                            Effect heavyBleedEffect = new Effect();
                            heavyBleedEffect.effectType = Effect.EffectType.HeavyBleeding;
                            heavyBleedEffect.partIndex = actualPartIndex;
                            if (vitalityLevel >= 51)
                            {
                                heavyBleedEffect.hasTimer = true;
                                heavyBleedEffect.timer = 30;
                            }
                            Effect.effects.Add(heavyBleedEffect);
                        }
                        else if (bleedValue2 <= lightBleedChance2)
                        {
                            Effect lightBleedEffect = new Effect();
                            lightBleedEffect.effectType = Effect.EffectType.LightBleeding;
                            lightBleedEffect.partIndex = actualPartIndex;
                            if (vitalityLevel >= 51)
                            {
                                lightBleedEffect.hasTimer = true;
                                lightBleedEffect.timer = 20;
                            }
                            Effect.effects.Add(lightBleedEffect);
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
                            Effect heavyBleedEffect = new Effect();
                            heavyBleedEffect.effectType = Effect.EffectType.HeavyBleeding;
                            heavyBleedEffect.partIndex = actualPartIndex;
                            if (vitalityLevel >= 51)
                            {
                                heavyBleedEffect.hasTimer = true;
                                heavyBleedEffect.timer = 30;
                            }
                            Effect.effects.Add(heavyBleedEffect);
                        }
                        else if (bleedValue3 <= lightBleedChanceArm)
                        {
                            Effect lightBleedEffect = new Effect();
                            lightBleedEffect.effectType = Effect.EffectType.LightBleeding;
                            lightBleedEffect.partIndex = actualPartIndex;
                            if (vitalityLevel >= 51)
                            {
                                lightBleedEffect.hasTimer = true;
                                lightBleedEffect.timer = 20;
                            }
                            Effect.effects.Add(lightBleedEffect);
                        }

                        if (UnityEngine.Random.value < fractureChanceArm)
                        {
                            Effect fractureEffect = new Effect();
                            fractureEffect.effectType = Effect.EffectType.Fracture;
                            fractureEffect.partIndex = actualPartIndex;
                            Effect.effects.Add(fractureEffect);
                            // TODO: Player fracture sound
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
                            Effect heavyBleedEffect = new Effect();
                            heavyBleedEffect.effectType = Effect.EffectType.HeavyBleeding;
                            heavyBleedEffect.partIndex = actualPartIndex;
                            if (vitalityLevel >= 51)
                            {
                                heavyBleedEffect.hasTimer = true;
                                heavyBleedEffect.timer = 30;
                            }
                            Effect.effects.Add(heavyBleedEffect);
                        }
                        else if (bleedValueLeg <= lightBleedChanceLeg)
                        {
                            Effect lightBleedEffect = new Effect();
                            lightBleedEffect.effectType = Effect.EffectType.LightBleeding;
                            lightBleedEffect.partIndex = actualPartIndex;
                            if (vitalityLevel >= 51)
                            {
                                lightBleedEffect.hasTimer = true;
                                lightBleedEffect.timer = 20;
                            }
                            Effect.effects.Add(lightBleedEffect);
                        }

                        if (UnityEngine.Random.value < fractureChanceLeg)
                        {
                            Effect fractureEffect = new Effect();
                            fractureEffect.effectType = Effect.EffectType.Fracture;
                            fractureEffect.partIndex = actualPartIndex;
                            Effect.effects.Add(fractureEffect);
                            // TODO: Play fracture sound
                        }
                        break;
                }
            }

            return new object[] { actualPartIndex, actualAmount };
        }

        public static bool RegisterPlayerHit(int partIndex, float totalDamage, bool FromSelf)
        {
            if (GM.CurrentSceneSettings.DoesDamageGetRegistered && GM.CurrentSceneSettings.DeathResetPoint != null && !GM.IsDead())
            {
                Mod.AddSkillExp(Skill.damageTakenAction * totalDamage, 2);

                GM.CurrentPlayerBody.Health -= totalDamage;

                GM.CurrentPlayerBody.HitEffect();
                if (GM.CurrentPlayerBody.Health <= 0f)
                {
                    //Mod.currentRaidManager.KillPlayer();
                    return true;
                }

                // Parts other than head and thorax at zero distribute damage over all other parts
                float[] destroyedMultiplier = new float[] { 0, 0, 1.5f, 0.7f, 0.7f, 1, 1 };
                float actualTotalDamage = 0;
                if (partIndex >= 2)
                {
                    if (Mod.health[partIndex] <= 0)
                    {
                        for (int i = 0; i < Mod.health.Length; ++i)
                        {
                            if (i != partIndex)
                            {
                                float actualDamage = Mathf.Min(totalDamage / 6 * destroyedMultiplier[partIndex], Mod.health[i]);
                                Mod.health[i] = Mathf.Clamp(Mod.health[i] - actualDamage, 0, Mod.currentMaxHealth[i]);
                                actualTotalDamage += actualDamage;

                                if (i == 0 || i == 1)
                                {
                                    if (Mod.health[0] <= 0 || Mod.health[1] <= 0)
                                    {
                                        //Mod.currentRaidManager.KillPlayer();
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        actualTotalDamage = totalDamage;
                        Mod.health[partIndex] = Mathf.Clamp(Mod.health[partIndex] - totalDamage, 0, Mod.currentMaxHealth[partIndex]);
                    }
                }
                else if (Mod.health[partIndex] <= 0) // Part is head or thorax, destroyed
                {
                    //Mod.currentRaidManager.KillPlayer();
                    return true;
                }
                else // Part is head or thorax, not yet destroyed
                {
                    actualTotalDamage = totalDamage;
                    Mod.health[partIndex] = Mathf.Clamp(Mod.health[partIndex] - totalDamage, 0, Mod.currentMaxHealth[partIndex]);
                }
                GM.CurrentSceneSettings.OnPlayerTookDamage(actualTotalDamage / 440f);
            }
            return false;
        }
    }

    // Patches FVRPlayerHitbox.Damage(float) in order to implement our own armor's damage resistance
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
                if ( DamagePatch.RegisterPlayerHit((int)damageData[0], actualDamage, false))
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

    // Patches FVRPlayerHitbox.Damage(DamageDealt) in order to implement our own armor's damage resistance
    class DamageDealtPatch
    {
        static bool Prefix(DamageDealt dam, ref FVRPlayerHitbox __instance, ref AudioSource ___m_aud)
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
                if ( DamagePatch.RegisterPlayerHit((int)damageData[0], actualDamage, false))
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
                FVRInteractiveObject component2 = interactiveObjectToUse;
                if (component2 != null && component2.IsInteractable() && !component2.IsSelectionRestricted())
                {
                    float num = Vector3.Distance(interactiveObjectToUse.transform.position, __instance.Display_InteractionSphere.transform.position);
                    float num2 = Vector3.Distance(interactiveObjectToUse.transform.position, __instance.Display_InteractionSphere_Palm.transform.position);
                    if (__instance.ClosestPossibleInteractable == null)
                    {
                        __instance.ClosestPossibleInteractable = component2;
                        if (num < num2)
                        {
                            ___m_isClosestInteractableInPalm = false;
                        }
                        else
                        {
                            ___m_isClosestInteractableInPalm = true;
                        }
                    }
                    else if (__instance.ClosestPossibleInteractable != component2)
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
                            __instance.ClosestPossibleInteractable = component2;
                        }
                        else if (!flag && num < num3)
                        {
                            ___m_isClosestInteractableInPalm = false;
                            __instance.ClosestPossibleInteractable = component2;
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

    // Patches SideHingedDestructibleDoorDeadBoltKey.KeyForwardBack to bypass H3VR key type functionality and to make sure it uses correct key prefab
    // This completely replaces the original
    class KeyForwardBackPatch
    {
        static bool Prefix(float ___distBetween, ref float ___m_keyLerp, ref SideHingedDestructibleDoorDeadBoltKey __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }

            DoorWrapper doorWrapper = __instance.transform.parent.parent.parent.parent.parent.GetComponent<DoorWrapper>();

            Vector3 pos = __instance.m_hand.Input.Pos;
            Vector3 vector = pos - __instance.KeyIn.position;
            Vector3 vector2 = Vector3.ProjectOnPlane(vector, __instance.DeadBolt.Mount.up);
            vector2 = Vector3.ProjectOnPlane(vector2, __instance.DeadBolt.Mount.right);
            Vector3 a = __instance.KeyIn.position + vector2;
            float num = Vector3.Distance(a, pos);
            float num2 = Vector3.Distance(a, __instance.KeyIn.position);
            float num3 = Vector3.Distance(a, __instance.KeyOut.position);
            if (num3 <= ___distBetween && num2 <= ___distBetween)
            {
                float num4 = (___distBetween - num3) / ___distBetween;
                // Use object ID instead of type
                if ( !doorWrapper.keyID.Equals(__instance.KeyFO))
                {
                    num4 = Mathf.Clamp(num4, 0.7f, 1f);
                    if (num4 <= 0.71f && (double)___m_keyLerp > 0.711)
                    {
                        SM.PlayCoreSound(FVRPooledAudioType.Generic, __instance.AudEvent_KeyStop, __instance.transform.position);
                    }
                }
                if (num4 < 0.3f)
                {
                    if (___m_keyLerp >= 0.3f)
                    {
                        SM.PlayCoreSound(FVRPooledAudioType.Generic, __instance.AudEvent_KeyInsert, __instance.transform.position);
                    }
                    num4 = 0f;
                }
                __instance.transform.position = Vector3.Lerp(__instance.KeyIn.position, __instance.KeyOut.position, num4);
                ___m_keyLerp = num4;
            }
            else if (num2 > ___distBetween && num2 > num3 && __instance.DeadBolt.m_timeSinceKeyInOut > 1f)
            {
                __instance.DeadBolt.m_timeSinceKeyInOut = 0f;
                FVRViveHand hand = __instance.m_hand;
                // Use correct key item prefab
                GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(int.TryParse(__instance.KeyFO, out int result) ? Mod.GetItemPrefab(result) : IM.OD[__instance.KeyFO].GetGameObject(), __instance.transform.position, __instance.transform.rotation);
                LockKey component = gameObject.GetComponent<LockKey>();
                ___m_keyLerp = 1f;
                __instance.ForceBreakInteraction();
                component.BeginInteraction(hand);
                hand.CurrentInteractable = component;
                SM.PlayCoreSound(FVRPooledAudioType.Generic, __instance.AudEvent_KeyExtract, __instance.transform.position);
                __instance.DeadBolt.SetKeyState(false);
            }
            else if (num > ___distBetween)
            {
                __instance.ForceBreakInteraction();
            }

            return false;
        }
    }

    // Patches SideHingedDestructibleDoorDeadBoltKey.UpdateDisplayBasedOnType to make sure it uses correct key prefab
    // This completely replaces the original
    class UpdateDisplayBasedOnTypePatch
    {
        static bool Prefix(ref SideHingedDestructibleDoorDeadBoltKey __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }

            DoorWrapper doorWrapper = __instance.transform.parent.parent.parent.parent.parent.GetComponent<DoorWrapper>();

            GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(int.TryParse(__instance.KeyFO, out int result) ? Mod.GetItemPrefab(result) : IM.OD[__instance.KeyFO].GetGameObject(), __instance.transform.position, __instance.transform.rotation);
            LockKey component = gameObject.GetComponent<LockKey>();
            __instance.KeyMesh.mesh = component.KeyMesh.mesh;
            __instance.TagMesh.mesh = component.TagMesh.mesh;

            return false;
        }
    }

    // Patches SideHingedDestructibleDoor.Init to prevent door initialization when using grillhouse ones
    class DoorInitPatch
    {
        static bool Prefix(ref SideHingedDestructibleDoor __instance)
        {
            // If grillhouseSecure, it means we are currently loading into grillhouse but it is not a meatov scene, so need to check also because
            // although this is not a meatov scene, we still dont want to init doors if initDoors == false
            //if (!Mod.inMeatovScene && !Mod.grillHouseSecure)
            //{
            //    return true;
            //}

            if (!Mod.initDoors)
            {
                return false;
            }

            return true;
        }
    }

    // Patches SideHingedDestructibleDoorDeadBolt.TurnBolt to make sure we need to rotate the hand the right way if flipped
    // This completely replaces the original
    class DeadBoltPatch
    {
        static bool Prefix(Vector3 upVec, ref Vector3 ___lastHandForward, ref float ___m_curRot, ref SideHingedDestructibleDoorDeadBolt __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }

            // First check if lock is flipped
            Vector3 dirVecToUse = __instance.Mount.forward;
            DoorWrapper doorWrapper = __instance.transform.parent.parent.parent.GetComponent<DoorWrapper>();
            if (doorWrapper != null && doorWrapper.flipLock)
            {
                dirVecToUse *= -1; // Negate forward vector
            }

            Vector3 lhs = Vector3.ProjectOnPlane(upVec, dirVecToUse);
            Vector3 rhs = Vector3.ProjectOnPlane(___lastHandForward, dirVecToUse);
            float num = Mathf.Atan2(Vector3.Dot(dirVecToUse, Vector3.Cross(lhs, rhs)), Vector3.Dot(lhs, rhs)) * 57.29578f;
            ___m_curRot -= num;
            ___m_curRot = Mathf.Clamp(___m_curRot, __instance.MinRot, __instance.MaxRot);
            ___lastHandForward = lhs;

            return false;
        }
    }

    // Patches SideHingedDestructibleDoorDeadBolt.SetStartingLastHandForward to make sure we need to rotate the hand the right way if flipped
    // This completely replaces the original
    class DeadBoltLastHandPatch
    {
        static bool Prefix(Vector3 upVec, ref Vector3 ___lastHandForward, ref SideHingedDestructibleDoorDeadBolt __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }

            // First check if lock is flipped
            Vector3 dirVecToUse = __instance.Mount.forward;
            DoorWrapper doorWrapper = __instance.transform.parent.parent.parent.GetComponent<DoorWrapper>();
            if (doorWrapper != null && doorWrapper.flipLock)
            {
                dirVecToUse *= -1; // Negate forward vector
            }

            ___lastHandForward = Vector3.ProjectOnPlane(upVec, dirVecToUse);

            return false;
        }
    }

    // Patches SideHingedDestructibleDoorDeadBolt.Awake to set correct vizRot if lock is flipped
    class DeadBoltAwakePatch
    {
        static void Postfix(ref float ___m_vizRot, ref SideHingedDestructibleDoorDeadBolt __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            // First check if lock is flipped
            float yAngleToUse = 0;
            DoorWrapper doorWrapper = __instance.transform.parent.parent.parent.parent.GetComponent<DoorWrapper>();
            if (doorWrapper != null && doorWrapper.flipLock)
            {
                yAngleToUse = 180;
            }

            __instance.transform.localEulerAngles = new Vector3(0f, yAngleToUse, ___m_vizRot);

        }
    }

    // Patches SideHingedDestructibleDoorDeadBolt.FVRFixedUpdate to set correct vizRot if lock is flipped
    class DeadBoltFVRFixedUpdatePatch
    {
        static void Postfix(ref float ___m_vizRot, ref SideHingedDestructibleDoorDeadBolt __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            // First check if lock is flipped
            float yAngleToUse = 0;
            DoorWrapper doorWrapper = __instance.transform.parent.parent.parent.parent.GetComponent<DoorWrapper>();
            if (doorWrapper != null && doorWrapper.flipLock)
            {
                yAngleToUse = 180;
            }

            __instance.transform.localEulerAngles = new Vector3(0f, yAngleToUse, ___m_vizRot);
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

    // Patches FVRViveHand.Update to change the grab laser input
    // This completely replaces the original
    class HandUpdatePatch
    {
        public static bool fullDescDirectionDown = false; // The direction in which to slide finger on touchpad to open full description for index and oculus

        static bool leftTouchWithinDescRange;
        static bool rightTouchWithinDescRange;
        static float rightPreviousFrameTPAxisY;
        static float leftPreviousFrameTPAxisY;

        //flag2 = __instance.Input.TouchpadTouched && __instance.Input.TouchpadAxes.magnitude < 0.2f;
        static bool Prefix(ref FVRViveHand.HandInitializationState ___m_initState, ref FVRPhysicalObject ___m_selectedObj,
                           ref float ___m_reset, ref bool ___m_isObjectInTransit, ref bool ___m_hasOverrider, ref InputOverrider ___m_overrider,
                           ref bool ___m_touchSphereMatInteractable, ref bool ___m_touchSphereMatInteractablePalm,
                           ref bool ___m_isClosestInteractableInPalm, ref FVRViveHand.HandState ___m_state, ref RaycastHit ___m_pointingHit,
                           ref bool ___m_isWristMenuActive, ref RaycastHit ___m_grabHit, ref Collider[] ___m_rawGrabCols,
                           ref FVRPhysicalObject ___m_grabityHoveredObject, ref float ___m_timeSinceLastGripButtonDown, ref float ___m_timeGripButtonHasBeenHeld,
                           ref bool ___m_canMadeGrabReleaseSoundThisFrame, ref FVRViveHand __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }

            if (___m_initState == FVRViveHand.HandInitializationState.Uninitialized)
            {
                return false;
            }

            if (___m_selectedObj != null && ___m_selectedObj.IsHeld)
            {
                ___m_selectedObj = null;
                ___m_reset = 0f;
                ___m_isObjectInTransit = false;
            }
            if (___m_reset >= 0f && ___m_isObjectInTransit)
            {
                if (___m_selectedObj != null && Vector3.Distance(___m_selectedObj.transform.position, __instance.transform.position) < 0.4f)
                {
                    Vector3 b = __instance.transform.position - ___m_selectedObj.transform.position;
                    Vector3 vector = Vector3.Lerp(___m_selectedObj.RootRigidbody.velocity, b, Time.deltaTime * 2f);
                    ___m_selectedObj.RootRigidbody.velocity = Vector3.ClampMagnitude(vector, ___m_selectedObj.RootRigidbody.velocity.magnitude);
                    ___m_selectedObj.RootRigidbody.velocity = vector;
                    ___m_selectedObj.RootRigidbody.drag = 1f;
                    ___m_selectedObj.RootRigidbody.angularDrag = 8f;
                    ___m_reset -= Time.deltaTime * 0.4f;
                }
                else
                {
                    ___m_reset -= Time.deltaTime;
                }
                if (___m_reset <= 0f)
                {
                    ___m_isObjectInTransit = false;
                    if (___m_selectedObj != null)
                    {
                        ___m_selectedObj.RecoverDrag();
                        ___m_selectedObj = null;
                    }
                }
            }

            typeof(FVRViveHand).GetMethod("HapticBuzzUpdate", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, null);
            typeof(FVRViveHand).GetMethod("TestQuickBeltDistances", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, null);
            __instance.PollInput();

            // If started touching this frame
            if (__instance.Input.TouchpadTouchDown)
            {
                // Store whether we are in range for description so that we can only activate description if we STARTED touching within the range
                if (__instance.CMode == ControlMode.Vive || __instance.CMode == ControlMode.WMR)
                {
                    if (__instance.IsThisTheRightHand)
                    {
                        rightTouchWithinDescRange = __instance.Input.TouchpadAxes.magnitude < 0.3f;
                    }
                    else
                    {
                        leftTouchWithinDescRange = __instance.Input.TouchpadAxes.magnitude < 0.3f;
                    }
                }
                else if (__instance.CMode == ControlMode.Index || __instance.CMode == ControlMode.Oculus)
                {
                    if (__instance.IsThisTheRightHand)
                    {
                        rightTouchWithinDescRange = fullDescDirectionDown ? __instance.Input.TouchpadAxes.y >= 0 : __instance.Input.TouchpadAxes.y <= 0;
                    }
                    else
                    {
                        leftTouchWithinDescRange = fullDescDirectionDown ? __instance.Input.TouchpadAxes.y >= 0 : __instance.Input.TouchpadAxes.y <= 0;
                    }
                }
            }
            else if (__instance.Input.TouchpadTouchUp ||
                    ((__instance.CMode == ControlMode.Vive || __instance.CMode == ControlMode.WMR) && __instance.Input.TouchpadAxes.magnitude >= 0.3f) ||
                    ((__instance.CMode == ControlMode.Index || __instance.CMode == ControlMode.Oculus) && fullDescDirectionDown ? __instance.Input.TouchpadAxes.y < 0 : __instance.Input.TouchpadAxes.y > 0))
            {
                if (__instance.IsThisTheRightHand)
                {
                    rightTouchWithinDescRange = false;
                }
                else
                {
                    leftTouchWithinDescRange = false;
                }
            }

            if (___m_hasOverrider && ___m_overrider != null)
            {
                ___m_overrider.Process(ref __instance.Input);
            }
            else
            {
                ___m_hasOverrider = false;
            }
            //if (!(__instance.m_currentInteractable != null) || __instance.Input.TriggerPressed)
            //{
            //}
            if (__instance.ClosestPossibleInteractable != null && !__instance.ClosestPossibleInteractable.IsInteractable())
            {
                __instance.ClosestPossibleInteractable = null;
            }
            if (__instance.ClosestPossibleInteractable == null)
            {
                if (___m_touchSphereMatInteractable)
                {
                    ___m_touchSphereMatInteractable = false;
                    __instance.TouchSphere.material = __instance.TouchSphereMat_NoInteractable;
                }
                if (___m_touchSphereMatInteractablePalm)
                {
                    ___m_touchSphereMatInteractablePalm = false;
                    __instance.TouchSphere_Palm.material = __instance.TouchSphereMat_NoInteractable;
                }
            }
            else if (!___m_touchSphereMatInteractable && !___m_isClosestInteractableInPalm)
            {
                ___m_touchSphereMatInteractable = true;
                __instance.TouchSphere.material = __instance.TouchSpheteMat_Interactable;
                ___m_touchSphereMatInteractablePalm = false;
                __instance.TouchSphere_Palm.material = __instance.TouchSphereMat_NoInteractable;
            }
            else if (!___m_touchSphereMatInteractablePalm && ___m_isClosestInteractableInPalm)
            {
                ___m_touchSphereMatInteractablePalm = true;
                __instance.TouchSphere_Palm.material = __instance.TouchSpheteMat_Interactable;
                ___m_touchSphereMatInteractable = false;
                __instance.TouchSphere.material = __instance.TouchSphereMat_NoInteractable;
            }
            float d = 1f / GM.CurrentPlayerBody.transform.localScale.x;
            if (___m_state == FVRViveHand.HandState.Empty && !__instance.Input.BYButtonPressed && !__instance.Input.TouchpadPressed && __instance.ClosestPossibleInteractable == null && __instance.CurrentHoveredQuickbeltSlot == null && __instance.CurrentInteractable == null && !___m_isWristMenuActive)
            {
                if (Physics.Raycast(__instance.Input.OneEuroPointingPos, __instance.Input.OneEuroPointRotation * Vector3.forward, out ___m_pointingHit, GM.CurrentSceneSettings.MaxPointingDistance, __instance.PointingLayerMask, QueryTriggerInteraction.Collide) && ___m_pointingHit.collider.gameObject.GetComponent<FVRPointable>())
                {
                    FVRPointable component = ___m_pointingHit.collider.gameObject.GetComponent<FVRPointable>();
                    if (___m_pointingHit.distance <= component.MaxPointingRange)
                    {
                        __instance.CurrentPointable = component;
                        __instance.PointingLaser.position = __instance.Input.OneEuroPointingPos;
                        __instance.PointingLaser.rotation = __instance.Input.OneEuroPointRotation;
                        __instance.PointingLaser.localScale = new Vector3(0.002f, 0.002f, ___m_pointingHit.distance) * d;
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

            // Might have to cancel movement if touching TP for description
            // Should only be applicable with Vive since movement and description share the touchpad
            if (__instance.CMode == ControlMode.Vive || __instance.CMode == ControlMode.WMR)
            {
                if (!(__instance.IsThisTheRightHand ? rightTouchWithinDescRange : leftTouchWithinDescRange))
                {
                    __instance.MovementManager.UpdateMovementWithHand(__instance);
                }
                else // Started touching within desc range, want to stop movement, sprinting, and smooth turning
                {
                    typeof(FVRMovementManager).GetField("m_isTwinStickSmoothTurningClockwise", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(GM.CurrentMovementManager, false);
                    typeof(FVRMovementManager).GetField("m_isTwinStickSmoothTurningCounterClockwise", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(GM.CurrentMovementManager, false);
                    typeof(FVRMovementManager).GetField("m_sprintingEngaged", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(GM.CurrentMovementManager, false);
                    typeof(FVRMovementManager).GetField("m_smoothLocoVelocity", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(GM.CurrentMovementManager, Vector3.zero);
                }
            }
            else // Any other controller, or any that has enough buttons to have description AND movement at the same time, we should update movement
            {
                __instance.MovementManager.UpdateMovementWithHand(__instance);
            }

            // Keep a reference to touchpad touch inputs so we can still use descriptions after touchpad input has been flushed
            bool touchpadTouched = __instance.Input.TouchpadTouched;
            float touchpadAxisMagnitude = __instance.Input.TouchpadAxes.magnitude;
            float touchpadAxisY = __instance.Input.TouchpadAxes.y;
            bool touchpadDown = __instance.Input.TouchpadDown;

            if (__instance.MovementManager.ShouldFlushTouchpad(__instance))
            {
                typeof(FVRViveHand).GetMethod("FlushTouchpadData", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, null);
            }
            bool flag = false;
            bool flag2 = false;
            bool fullDescInput = false;
            if (__instance.CMode == ControlMode.Index || __instance.CMode == ControlMode.Oculus) // Usually we would check if streamlined here, but in meatov, it will always be streamlined if index
            {
                flag = __instance.Input.BYButtonDown;

                // Want grab laser if the default BYButtonPressed (vanilla) (TODO: On main hand, right for now) OR if the description is touched
                flag2 = __instance.IsThisTheRightHand ? rightTouchWithinDescRange : leftTouchWithinDescRange;

                // Check if we move touch from bottom (touch range) to top of touch pad this frame
                fullDescInput = touchpadTouched && (fullDescDirectionDown ? touchpadAxisY < 0 : touchpadAxisY > 0) && (__instance.IsThisTheRightHand ? (fullDescDirectionDown ? rightPreviousFrameTPAxisY >= 0 : rightPreviousFrameTPAxisY <= 0) : (fullDescDirectionDown ? leftPreviousFrameTPAxisY >= 0 : leftPreviousFrameTPAxisY <= 0));
                if (__instance.IsThisTheRightHand)
                {
                    rightPreviousFrameTPAxisY = touchpadAxisY;
                }
                else
                {
                    leftPreviousFrameTPAxisY = touchpadAxisY;
                }
            }
            else if (__instance.CMode == ControlMode.Vive || __instance.CMode == ControlMode.WMR)
            {
                flag = touchpadDown;

                // Here check if only touched and within center of touchpad for grab laser input
                flag2 = touchpadTouched && touchpadAxisMagnitude < 0.3f;
                //Mod.LogInfo("Flag2: " + flag2 + " from touched: " + __instance.Input.TouchpadTouched + " and magnitude: " + __instance.Input.TouchpadAxes.magnitude);

                // Check if we started pressing the center of touchpad this frame
                fullDescInput = touchpadDown && touchpadAxisMagnitude < 0.3f;
            }

            if (flag2)
            {
                if (___m_state == FVRViveHand.HandState.GripInteracting)
                {
                    // Only display description if started touching within desc range, and also check if descriptions have been init yet
                    // Because this will also be checked in meatov menu, the patch will run, but they havent been init yet at that point
                    if ((__instance.IsThisTheRightHand ? rightTouchWithinDescRange : leftTouchWithinDescRange) && Mod.rightDescriptionManager != null)
                    {
                        IDescribable describable = __instance.CurrentInteractable.GetComponent<IDescribable>();
                        if (describable != null)
                        {
                            // Get the description currently on this hand
                            DescriptionManager manager = null;
                            if (__instance.IsThisTheRightHand)
                            {
                                manager = Mod.rightDescriptionManager;
                            }
                            else
                            {
                                manager = Mod.leftDescriptionManager;
                            }

                            // Get item and check if the one we are pointing at is already being described
                            IDescribable describableToUse = null;
                            if (manager.descriptionPack != null)
                            {
                                //if (manager.descriptionPack.isPhysical)
                                //{
                                //    describableToUse = manager.descriptionPack.MI;
                                //}
                                //else
                                //{
                                //    describableToUse = manager.descriptionPack.nonPhysDescribable;
                                //}

                                //// If not already displayed
                                //if (!describable.Equals(describableToUse))
                                //{
                                //    // Update the display to the description of the new item we are pointing at
                                //    manager.SetDescriptionPack(describable.GetDescriptionPack());
                                //}
                            }
                            else
                            {
                                // Set description pack
                                //manager.SetDescriptionPack(describable.GetDescriptionPack());
                            }

                            //if (manager.descriptionPack.itemType == MeatovItem.ItemType.LootContainer)
                            //{
                            //    manager.gameObject.SetActive(false);
                            //}
                            //else
                            //{
                            //    manager.gameObject.SetActive(true);
                            //}
                        }
                    }
                }
            }
            else if (!((__instance.CMode == ControlMode.Index || __instance.CMode == ControlMode.Oculus) && fullDescInput)) // Dont want to disable if we arent in desc range but we have full desc input
            {
                if (Mod.rightDescriptionManager != null)
                {
                    // Get the description currently on this hand
                    DescriptionManager manager = null;
                    if (__instance.IsThisTheRightHand)
                    {
                        manager = Mod.rightDescriptionManager;
                    }
                    else
                    {
                        manager = Mod.leftDescriptionManager;
                    }

                    // Make sure it is not displayed
                    if (manager.gameObject.activeSelf)
                    {
                        manager.gameObject.SetActive(false);
                    }
                }
            }
            if (fullDescInput && Mod.rightDescriptionManager != null)
            {
                // Get the description currently on this hand
                DescriptionManager manager = null;
                if (__instance.IsThisTheRightHand)
                {
                    manager = Mod.rightDescriptionManager;
                }
                else
                {
                    manager = Mod.leftDescriptionManager;
                }

                // If displayed, open fully and replace this hand's with new description
                if (manager.gameObject.activeSelf)
                {
                    //manager.OpenFull();

                    if (__instance.IsThisTheRightHand)
                    {
                        Mod.rightDescriptionUI = GameObject.Instantiate(Mod.itemDescriptionUIPrefab, GM.CurrentPlayerBody.RightHand);
                        Mod.rightDescriptionManager = Mod.rightDescriptionUI.AddComponent<DescriptionManager>();
                        Mod.rightDescriptionManager.Init();
                    }
                    else
                    {
                        Mod.leftDescriptionUI = GameObject.Instantiate(Mod.itemDescriptionUIPrefab, GM.CurrentPlayerBody.LeftHand);
                        Mod.leftDescriptionManager = Mod.leftDescriptionUI.AddComponent<DescriptionManager>();
                        Mod.leftDescriptionManager.Init();
                    }
                }
            }
            if (___m_state == FVRViveHand.HandState.Empty && __instance.CurrentHoveredQuickbeltSlot == null)
            {
                // Dont have the grab laser if we didnt start touching the touchpad within desc range
                if (((__instance.CMode == ControlMode.Index || __instance.CMode == ControlMode.Oculus) && flag2) ||
                    ((__instance.CMode == ControlMode.Vive || __instance.CMode == ControlMode.WMR) && flag2 && (__instance.IsThisTheRightHand ? rightTouchWithinDescRange : leftTouchWithinDescRange)))
                {
                    if (!__instance.GrabLaser.gameObject.activeSelf)
                    {
                        __instance.GrabLaser.gameObject.SetActive(true);
                    }
                    bool flag3 = false;
                    bool pointNonGrabbableDescribable = false;
                    FVRPhysicalObject fvrphysicalObject = null;
                    IDescribable nonGrabbableDescribable = null;
                    if (Physics.Raycast(__instance.Input.OneEuroPointingPos, __instance.Input.OneEuroPointRotation * Vector3.forward, out ___m_grabHit, 3f, __instance.GrabLaserMask, QueryTriggerInteraction.Collide))
                    {
                        if (___m_grabHit.collider.attachedRigidbody != null && ___m_grabHit.collider.attachedRigidbody.gameObject.GetComponent<FVRPhysicalObject>())
                        {
                            fvrphysicalObject = ___m_grabHit.collider.attachedRigidbody.gameObject.GetComponent<FVRPhysicalObject>();
                            if (fvrphysicalObject != null && !fvrphysicalObject.IsHeld && fvrphysicalObject.IsDistantGrabbable())
                            {
                                flag3 = true;
                            }
                        }
                        else if (___m_grabHit.collider.GetComponent<IDescribable>() != null)
                        {
                            nonGrabbableDescribable = ___m_grabHit.collider.GetComponent<IDescribable>();
                            pointNonGrabbableDescribable = true;
                        }
                        __instance.GrabLaser.localScale = new Vector3(0.004f, 0.004f, ___m_grabHit.distance) * d;
                    }
                    else
                    {
                        __instance.GrabLaser.localScale = new Vector3(0.004f, 0.004f, 3f) * d;
                    }
                    __instance.GrabLaser.position = __instance.Input.OneEuroPointingPos;
                    __instance.GrabLaser.rotation = __instance.Input.OneEuroPointRotation;
                    if (flag3)
                    {
                        // Display summary description of object if describable and if not already displayed
                        IDescribable describable = fvrphysicalObject.GetComponent<IDescribable>();
                        if (describable != null && Mod.rightDescriptionManager != null)
                        {
                            // Get the description currently on this hand
                            DescriptionManager manager = null;
                            if (__instance.IsThisTheRightHand)
                            {
                                manager = Mod.rightDescriptionManager;
                            }
                            else
                            {
                                manager = Mod.leftDescriptionManager;
                            }

                            // Get item and check if the one we are pointing at is already being described
                            IDescribable describableToUse = null;
                            if (manager.descriptionPack != null)
                            {
                                //if (manager.descriptionPack.isPhysical)
                                //{
                                //    describableToUse = manager.descriptionPack.MI;
                                //}
                                //else
                                //{
                                //    describableToUse = manager.descriptionPack.nonPhysDescribable;
                                //}

                                //// If not already displayed
                                //if (!describable.Equals(describableToUse))
                                //{
                                //    // Update the display to the description of the new item we are pointing at
                                //    manager.SetDescriptionPack(describable.GetDescriptionPack());
                                //}
                            }
                            else
                            {
                                // Set description pack
                                //manager.SetDescriptionPack(describable.GetDescriptionPack());
                            }

                            //if (manager.descriptionPack.itemType == MeatovItem.ItemType.LootContainer)
                            //{
                            //    manager.gameObject.SetActive(false);
                            //}
                            //else
                            //{
                            //    manager.gameObject.SetActive(true);
                            //}
                        }

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
                    else if (pointNonGrabbableDescribable)
                    {
                        // Display summary description of object if describable and if not already displayed
                        if (nonGrabbableDescribable != null && Mod.rightDescriptionManager != null)
                        {
                            // Get the description currently on this hand
                            DescriptionManager manager = null;
                            if (__instance.IsThisTheRightHand)
                            {
                                manager = Mod.rightDescriptionManager;
                            }
                            else
                            {
                                manager = Mod.leftDescriptionManager;
                            }

                            // Get item and check if the one we are pointing at is already being described
                            IDescribable describableToUse = null;
                            if (manager.descriptionPack != null)
                            {
                                //if (manager.descriptionPack.isPhysical)
                                //{
                                //    describableToUse = manager.descriptionPack.MI;
                                //}
                                //else
                                //{
                                //    describableToUse = manager.descriptionPack.nonPhysDescribable;
                                //}

                                //// If not already displayed
                                //if (!nonGrabbableDescribable.Equals(describableToUse))
                                //{
                                //    // Update the display to the description of the new item we are pointing at
                                //    manager.SetDescriptionPack(nonGrabbableDescribable.GetDescriptionPack());
                                //}
                            }
                            else
                            {
                                // Set description pack
                                //manager.SetDescriptionPack(nonGrabbableDescribable.GetDescriptionPack());
                            }

                            //if (manager.descriptionPack.itemType == MeatovItem.ItemType.LootContainer)
                            //{
                            //    manager.gameObject.SetActive(false);
                            //}
                            //else
                            //{
                            //    manager.gameObject.SetActive(true);
                            //}
                        }

                        if (!__instance.BlueLaser.activeSelf)
                        {
                            __instance.BlueLaser.SetActive(true);
                        }
                        if (__instance.RedLaser.activeSelf)
                        {
                            __instance.RedLaser.SetActive(false);
                        }
                    }
                    else
                    {
                        if (Mod.rightDescriptionManager != null)
                        {
                            // Hide summary description of object
                            DescriptionManager manager = null;
                            if (__instance.IsThisTheRightHand)
                            {
                                manager = Mod.rightDescriptionManager;
                            }
                            else
                            {
                                manager = Mod.leftDescriptionManager;
                            }
                            if (manager.gameObject.activeSelf)
                            {
                                manager.gameObject.SetActive(false);
                            }
                        }

                        if (__instance.BlueLaser.activeSelf)
                        {
                            __instance.BlueLaser.SetActive(false);
                        }
                        if (!__instance.RedLaser.activeSelf)
                        {
                            __instance.RedLaser.SetActive(true);
                        }
                    }
                }
                else if (__instance.GrabLaser.gameObject.activeSelf)
                {
                    __instance.GrabLaser.gameObject.SetActive(false);
                }
            }
            else if (__instance.GrabLaser.gameObject.activeSelf)
            {
                __instance.GrabLaser.gameObject.SetActive(false);
            }
            if (__instance.Mode == FVRViveHand.HandMode.Neutral && ___m_state == FVRViveHand.HandState.Empty && flag)
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
            typeof(FVRViveHand).GetMethod("UpdateGrabityDisplay", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, null);
            if (__instance.Mode == FVRViveHand.HandMode.Neutral)
            {
                if (___m_state == FVRViveHand.HandState.Empty)
                {
                    bool flag4 = false;
                    if (__instance.Input.IsGrabDown)
                    {
                        if (__instance.CurrentHoveredQuickbeltSlot != null && __instance.CurrentHoveredQuickbeltSlot.CurObject != null)
                        {
                            __instance.CurrentInteractable = __instance.CurrentHoveredQuickbeltSlot.CurObject;
                            ___m_state = FVRViveHand.HandState.GripInteracting;
                            __instance.CurrentInteractable.BeginInteraction(__instance);
                            __instance.Buzz(__instance.Buzzer.Buzz_BeginInteraction);
                            flag4 = true;
                        }
                        else if (__instance.CurrentHoveredQuickbeltSlot != null &&
                                 __instance.CurrentHoveredQuickbeltSlot is ShoulderStorage &&
                                 !(__instance.CurrentHoveredQuickbeltSlot as ShoulderStorage).right &&
                                 StatusUI.instance.equipmentSlots[0].CurObject != null)
                        {
                            // If we are hovering over left shoulder slot and backpack slot is not empty we want to grab backpack
                            __instance.CurrentInteractable = StatusUI.instance.equipmentSlots[0].CurObject;
                            ___m_state = FVRViveHand.HandState.GripInteracting;
                            __instance.CurrentInteractable.BeginInteraction(__instance);
                            __instance.Buzz(__instance.Buzzer.Buzz_BeginInteraction);
                            Mod.leftShoulderObject = null;
                            flag4 = true;
                        }
                        else if (__instance.ClosestPossibleInteractable != null && !__instance.ClosestPossibleInteractable.IsSimpleInteract)
                        {
                            __instance.CurrentInteractable = __instance.ClosestPossibleInteractable;
                            ___m_state = FVRViveHand.HandState.GripInteracting;
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
                        ___m_rawGrabCols = Physics.OverlapSphere(__instance.transform.position, 0.01f, __instance.LM_RawGrab, QueryTriggerInteraction.Ignore);
                        if (___m_rawGrabCols.Length > 0)
                        {
                            for (int i = 0; i < ___m_rawGrabCols.Length; i++)
                            {
                                if (!(___m_rawGrabCols[i].attachedRigidbody == null))
                                {
                                    if (___m_rawGrabCols[i].attachedRigidbody.gameObject.CompareTag("RawGrab"))
                                    {
                                        FVRInteractiveObject component2 = ___m_rawGrabCols[i].attachedRigidbody.gameObject.GetComponent<FVRInteractiveObject>();
                                        if (component2 != null && component2.IsInteractable())
                                        {
                                            flag6 = true;
                                            __instance.CurrentInteractable = component2;
                                            ___m_state = FVRViveHand.HandState.GripInteracting;
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
                        if (___m_selectedObj == null)
                        {
                            typeof(FVRViveHand).GetMethod("CastToFindHover", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, null);
                        }
                        else
                        {
                            typeof(FVRViveHand).GetMethod("SetGrabbityHovered", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { null });
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
                        if (flag7 && ___m_grabityHoveredObject != null && ___m_selectedObj == null)
                        {
                            typeof(FVRViveHand).GetMethod("CastToGrab", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, null);
                        }
                        if (flag8 && !___m_isObjectInTransit)
                        {
                            ___m_selectedObj = null;
                        }
                        if (___m_selectedObj != null && !___m_isObjectInTransit)
                        {
                            float num = 3.5f;
                            if (Mathf.Abs(__instance.Input.VelAngularLocal.x) > num || Mathf.Abs(__instance.Input.VelAngularLocal.y) > num)
                            {
                                typeof(FVRViveHand).GetMethod("BeginFlick", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { ___m_selectedObj });
                            }
                        }
                    }
                    else
                    {
                        typeof(FVRViveHand).GetMethod("SetGrabbityHovered", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { null });
                    }
                    if (GM.Options.ControlOptions.WIPGrabbityState == ControlOptions.WIPGrabbity.Enabled && !flag4 && !flag5 && __instance.Input.IsGrabDown && ___m_isObjectInTransit && ___m_selectedObj != null)
                    {
                        float num2 = Vector3.Distance(__instance.transform.position, ___m_selectedObj.transform.position);
                        if (num2 < 0.5f)
                        {
                            if (___m_selectedObj.UseGripRotInterp)
                            {
                                __instance.CurrentInteractable = ___m_selectedObj;
                                __instance.CurrentInteractable.BeginInteraction(__instance);
                                ___m_state = FVRViveHand.HandState.GripInteracting;
                            }
                            else
                            {
                                __instance.RetrieveObject(___m_selectedObj);
                            }
                            ___m_selectedObj = null;
                            ___m_isObjectInTransit = false;
                            typeof(FVRViveHand).GetMethod("SetGrabbityHovered", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { null });
                        }
                    }
                }
                else if (___m_state == FVRViveHand.HandState.GripInteracting)
                {
                    typeof(FVRViveHand).GetMethod("SetGrabbityHovered", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { null });
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
                                            if (!__instance.Input.TriggerPressed && __instance.Input.GripDown && ___m_timeSinceLastGripButtonDown > 0.05f && ___m_timeSinceLastGripButtonDown < 0.4f)
                                            {
                                                flag9 = true;
                                            }
                                        }
                                    }
                                    else if (!__instance.Input.TriggerPressed && ___m_timeGripButtonHasBeenHeld > 1f)
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
                            if (__instance.CurrentInteractable is FVRPhysicalObject && ((FVRPhysicalObject)__instance.CurrentInteractable).QuickbeltSlot == null &&
                                !((FVRPhysicalObject)__instance.CurrentInteractable).IsPivotLocked && __instance.CurrentHoveredQuickbeltSlot != null &&
                                __instance.CurrentHoveredQuickbeltSlot.GetAffixedTo() != (FVRPhysicalObject)__instance.CurrentInteractable &&
                                __instance.CurrentHoveredQuickbeltSlot.HeldObject == null &&
                                ((FVRPhysicalObject)__instance.CurrentInteractable).QBSlotType == __instance.CurrentHoveredQuickbeltSlot.Type &&
                                __instance.CurrentHoveredQuickbeltSlot.SizeLimit >= ((FVRPhysicalObject)__instance.CurrentInteractable).Size)
                            {
                                // Note: This will call set quick belt slot twice, this is by vanilla design and is not a bug
                                ((FVRPhysicalObject)__instance.CurrentInteractable).EndInteractionIntoInventorySlot(__instance, __instance.CurrentHoveredQuickbeltSlot);
                            }
                            else
                            {
                                __instance.CurrentInteractable.EndInteraction(__instance);
                            }
                            __instance.CurrentInteractable = null;
                            ___m_state = FVRViveHand.HandState.Empty;
                        }
                        else
                        {
                            __instance.CurrentInteractable.UpdateInteraction(__instance);
                        }
                    }
                    else
                    {
                        ___m_state = FVRViveHand.HandState.Empty;
                    }
                }
            }
            if (__instance.Input.GripPressed)
            {
                ___m_timeSinceLastGripButtonDown = 0f;
                ___m_timeGripButtonHasBeenHeld += Time.deltaTime;
            }
            else
            {
                ___m_timeGripButtonHasBeenHeld = 0f;
            }
            ___m_canMadeGrabReleaseSoundThisFrame = true;

            return false;
        }
    }

    // Patches FVRFireArmMagazine to get the created round item when ejected from the magazine so we can set its location index and update the lists accordingly
    class MagazineUpdateInteractionPatch
    {
        static GameObject latestEjectedRound;
        static int latestEjectedRoundLocation = 0; // IGNORE WARNING, Will be written by transpiler

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(typeof(MagazineUpdateInteractionPatch), "latestEjectedRound")));
            toInsert.Add(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(MagazineUpdateInteractionPatch), "latestEjectedRound")));

            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Call && instruction.operand.ToString().Equals("UnityEngine.GameObject RemoveRound(Boolean)") &&
                    instructionList[i + 1].opcode == OpCodes.Stloc_S)
                {
                    if (instructionList[i + 1].operand.ToString().Equals("UnityEngine.GameObject (13)"))
                    {
                        instructionList.InsertRange(i + 1, toInsert);

                        // Now in hand
                        instructionList.Insert(i + 5, new CodeInstruction(OpCodes.Ldc_I4_0));
                        instructionList.Insert(i + 6, new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(typeof(MagazineUpdateInteractionPatch), "latestEjectedRoundLocation")));
                    }
                    else if (instructionList[i + 1].operand.ToString().Equals("UnityEngine.GameObject (18)"))
                    {
                        instructionList.InsertRange(i + 1, toInsert);

                        // Now in slot, could be in raid or base so can just take the one in mod
                        instructionList.Insert(i + 5, new CodeInstruction(OpCodes.Ldc_I4_1));
                        instructionList.Insert(i + 6, new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(typeof(MagazineUpdateInteractionPatch), "latestEjectedRoundLocation")));
                    }
                    else if (instructionList[i + 1].operand.ToString().Equals("UnityEngine.GameObject (23)"))
                    {
                        instructionList.InsertRange(i + 1, toInsert);

                        // Now in slot, could be in raid or base so can just take the one in mod
                        instructionList.Insert(i + 5, new CodeInstruction(OpCodes.Ldc_I4_1));
                        instructionList.Insert(i + 6, new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(typeof(MagazineUpdateInteractionPatch), "latestEjectedRoundLocation")));
                    }
                }
            }
            return instructionList;
        }

        static void Postfix()
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (latestEjectedRound != null)
            {
                MeatovItem MI = latestEjectedRound.GetComponent<MeatovItem>();

                MI.UpdateInventories();

                latestEjectedRound = null;
            }
        }
    }

    // Patches FVRFireArmClip to get the created round item when ejected from the magazine so we can set its location index and update the lists accordingly
    class ClipUpdateInteractionPatch
    {
        static GameObject latestEjectedRound;
        static int latestEjectedRoundLocation = 0;

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(typeof(MagazineUpdateInteractionPatch), "latestEjectedRound")));
            toInsert.Add(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(MagazineUpdateInteractionPatch), "latestEjectedRound")));

            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Call && instruction.operand.ToString().Equals("UnityEngine.GameObject RemoveRound(Boolean)"))
                {
                    if (instructionList[i + 1].opcode == OpCodes.Stloc_1)
                    {
                        instructionList.InsertRange(i + 1, toInsert);

                        // Now in hand
                        instructionList.Insert(i + 5, new CodeInstruction(OpCodes.Ldc_I4_0));
                        instructionList.Insert(i + 6, new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(typeof(MagazineUpdateInteractionPatch), "latestEjectedRoundLocation")));
                    }
                    else if (instructionList[i + 1].opcode == OpCodes.Stloc_S)
                    {
                        if (instructionList[i + 1].operand.ToString().Equals("UnityEngine.GameObject (6)"))
                        {
                            instructionList.InsertRange(i + 1, toInsert);

                            // Now in slot, could be in raid or base so can just take the one in mod
                            instructionList.Insert(i + 5, new CodeInstruction(OpCodes.Ldc_I4_1));
                            instructionList.Insert(i + 6, new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(typeof(MagazineUpdateInteractionPatch), "latestEjectedRoundLocation")));
                        }
                        else if (instructionList[i + 1].operand.ToString().Equals("UnityEngine.GameObject (11)"))
                        {
                            instructionList.InsertRange(i + 1, toInsert);

                            // Now in slot, could be in raid or base so can just take the one in mod
                            instructionList.Insert(i + 5, new CodeInstruction(OpCodes.Ldc_I4_1));
                            instructionList.Insert(i + 6, new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(typeof(MagazineUpdateInteractionPatch), "latestEjectedRoundLocation")));
                        }
                    }
                }
            }
            return instructionList;
        }

        static void Postfix()
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (latestEjectedRound != null)
            {
                MeatovItem MI = latestEjectedRound.GetComponent<MeatovItem>();

                MI.UpdateInventories();

                latestEjectedRound = null;
            }
        }
    }

    // Patches FVRMovementManager.Jump to make it use stamina or to prevent it altogether if not enough stamina
    // This completely replaces the original
    class MovementManagerJumpPatch
    {
        static bool Prefix(ref bool ___m_isGrounded, ref Vector3 ___m_smoothLocoVelocity, ref FVRMovementManager __instance)
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

            if ((__instance.Mode == FVRMovementManager.MovementMode.Armswinger || __instance.Mode == FVRMovementManager.MovementMode.SingleTwoAxis || __instance.Mode == FVRMovementManager.MovementMode.TwinStick) && !___m_isGrounded)
            {
                return false;
            }
            __instance.DelayGround(0.1f);
            float num = 0f;
            switch (GM.Options.SimulationOptions.PlayerGravityMode)
            {
                case SimulationOptions.GravityMode.Realistic:
                    num = 7.1f;
                    break;
                case SimulationOptions.GravityMode.Playful:
                    num = 5f;
                    break;
                case SimulationOptions.GravityMode.OnTheMoon:
                    num = 3f;
                    break;
                case SimulationOptions.GravityMode.None:
                    num = 0.001f;
                    break;
            }
            num *= 0.65f;
            num += num * (0.004f * (Mod.skills[1].currentProgress / 100));
            if (__instance.Mode == FVRMovementManager.MovementMode.Armswinger || __instance.Mode == FVRMovementManager.MovementMode.SingleTwoAxis || __instance.Mode == FVRMovementManager.MovementMode.TwinStick)
            {
                __instance.DelayGround(0.25f);
                ___m_smoothLocoVelocity.y = Mathf.Clamp(___m_smoothLocoVelocity.y, 0f, ___m_smoothLocoVelocity.y);
                ___m_smoothLocoVelocity.y = num;
                ___m_isGrounded = false;
            }

            // Use stamina
            Mod.stamina = Mathf.Max(Mod.stamina - (Mod.jumpStaminaDrain - Mod.jumpStaminaDrain * (0.006f * (Mod.skills[0].progress / 100))), 0);
            Mod.staminaBarUI.transform.GetChild(0).GetChild(1).GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mod.stamina);

            Mod.AddSkillExp(UnityEngine.Random.Range(Skill.pushUpMin, Skill.pushUpMax), 1);

            // Reset stamina timer
            Mod.staminaTimer = 2;

            return false;
        }
    }

    // Patches FVRMovementManager.HandUpdateTwinstick to prevent sprinting in case of lack of stamina
    // This completely replaces the original
    class MovementManagerUpdatePatch
    {
        private static bool wasGrounded = true;
        private static Vector3 previousVelocity;
        public static float damagePerMeter = 9;
        public static float safeHeight = 3;

        static bool Prefix(FVRViveHand hand, ref bool ___m_isRightHandActive, ref bool ___m_isLeftHandActive, ref GameObject ___m_twinStickArrowsRight,
                           ref bool ___m_isTwinStickSmoothTurningCounterClockwise, ref bool ___m_isTwinStickSmoothTurningClockwise, ref GameObject ___m_twinStickArrowsLeft,
                           ref float ___m_timeSinceSprintDownClick, ref float ___m_timeSinceSnapTurn, ref bool ___m_sprintingEngaged, ref bool ___m_isGrounded,
                           ref Vector3 ___m_smoothLocoVelocity, ref Vector3 ___worldTPAxis, ref FVRMovementManager __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }

            bool flag = hand.IsThisTheRightHand;
            if (GM.Options.MovementOptions.TwinStickLeftRightState == MovementOptions.TwinStickLeftRightSetup.RightStickMove)
            {
                flag = !flag;
            }
            if (!hand.IsInStreamlinedMode && (hand.CMode == ControlMode.Vive || hand.CMode == ControlMode.Oculus))
            {
                // In meatov, the secondary hand should always be in movement mode if constols not streamlined
                if (!___m_isLeftHandActive)
                {
                    ___m_isLeftHandActive = true;
                }

                if (hand.Input.BYButtonDown)
                {
                    if (flag)
                    {
                        ___m_isRightHandActive = !___m_isRightHandActive;
                    }
                    //if (!flag)
                    //{
                    //    ___m_isLeftHandActive = !___m_isLeftHandActive;
                    //}
                }
            }
            else
            {
                ___m_isLeftHandActive = true;
                ___m_isRightHandActive = true;
            }
            if (flag && !___m_isRightHandActive)
            {
                if (___m_twinStickArrowsRight.activeSelf)
                {
                    ___m_twinStickArrowsRight.SetActive(false);
                }
                ___m_isTwinStickSmoothTurningCounterClockwise = false;
                ___m_isTwinStickSmoothTurningClockwise = false;
                return false;
            }
            if (!flag && !___m_isLeftHandActive)
            {
                if (___m_twinStickArrowsLeft.activeSelf)
                {
                    ___m_twinStickArrowsLeft.SetActive(false);
                }
                return false;
            }
            if (!hand.IsInStreamlinedMode && (hand.CMode == ControlMode.Vive || hand.CMode == ControlMode.Oculus))
            {
                if (flag)
                {
                    if (!___m_twinStickArrowsRight.activeSelf)
                    {
                        ___m_twinStickArrowsRight.SetActive(true);
                    }
                    if (___m_twinStickArrowsRight.transform.parent != hand.TouchpadArrowTarget)
                    {
                        ___m_twinStickArrowsRight.transform.SetParent(hand.TouchpadArrowTarget);
                        ___m_twinStickArrowsRight.transform.localPosition = Vector3.zero;
                        ___m_twinStickArrowsRight.transform.localRotation = Quaternion.identity;
                    }
                }
                else
                {
                    if (!___m_twinStickArrowsLeft.activeSelf)
                    {
                        ___m_twinStickArrowsLeft.SetActive(true);
                    }
                    if (___m_twinStickArrowsLeft.transform.parent != hand.TouchpadArrowTarget)
                    {
                        ___m_twinStickArrowsLeft.transform.SetParent(hand.TouchpadArrowTarget);
                        ___m_twinStickArrowsLeft.transform.localPosition = Vector3.zero;
                        ___m_twinStickArrowsLeft.transform.localRotation = Quaternion.identity;
                    }
                }
            }
            if (___m_timeSinceSprintDownClick < 2f)
            {
                ___m_timeSinceSprintDownClick += Time.deltaTime;
            }
            if (___m_timeSinceSnapTurn < 2f)
            {
                ___m_timeSinceSnapTurn += Time.deltaTime;
            }
            bool flag3;
            bool flag4;
            Vector2 vector;
            bool flag5;
            bool flag6;
            if (hand.CMode == ControlMode.Vive || hand.CMode == ControlMode.Oculus)
            {
                bool flag2 = hand.Input.TouchpadUp;
                flag3 = hand.Input.TouchpadDown;
                flag4 = hand.Input.TouchpadPressed;
                vector = hand.Input.TouchpadAxes;
                flag5 = hand.Input.TouchpadNorthDown;
                flag6 = hand.Input.TouchpadNorthPressed;
            }
            else
            {
                bool flag2 = hand.Input.Secondary2AxisInputUp;
                flag3 = hand.Input.Secondary2AxisInputDown;
                flag4 = hand.Input.Secondary2AxisInputPressed;
                vector = hand.Input.Secondary2AxisInputAxes;
                flag5 = hand.Input.Secondary2AxisNorthDown;
                flag6 = hand.Input.Secondary2AxisNorthPressed;
            }
            if (flag)
            {
                ___m_isTwinStickSmoothTurningCounterClockwise = false;
                ___m_isTwinStickSmoothTurningClockwise = false;
                if (GM.Options.MovementOptions.TwinStickSnapturnState == MovementOptions.TwinStickSnapturnMode.Enabled)
                {
                    if (hand.CMode == ControlMode.Oculus)
                    {
                        if (hand.Input.TouchpadWestDown)
                        {
                            __instance.TurnCounterClockWise();
                        }
                        else if (hand.Input.TouchpadEastDown)
                        {
                            __instance.TurnClockWise();
                        }
                    }
                    else if (hand.CMode == ControlMode.Vive)
                    {
                        if (GM.Options.MovementOptions.Touchpad_Confirm == FVRMovementManager.TwoAxisMovementConfirm.OnClick)
                        {
                            if (hand.Input.TouchpadDown)
                            {
                                if (hand.Input.TouchpadWestPressed)
                                {
                                    __instance.TurnCounterClockWise();
                                }
                                else if (hand.Input.TouchpadEastPressed)
                                {
                                    __instance.TurnClockWise();
                                }
                            }
                        }
                        else if (hand.Input.TouchpadWestDown)
                        {
                            __instance.TurnCounterClockWise();
                        }
                        else if (hand.Input.TouchpadEastDown)
                        {
                            __instance.TurnClockWise();
                        }
                    }
                    else if (hand.Input.Secondary2AxisWestDown)
                    {
                        __instance.TurnCounterClockWise();
                    }
                    else if (hand.Input.Secondary2AxisEastDown)
                    {
                        __instance.TurnClockWise();
                    }
                }
                else if (GM.Options.MovementOptions.TwinStickSnapturnState == MovementOptions.TwinStickSnapturnMode.Smooth)
                {
                    if (hand.CMode == ControlMode.Oculus)
                    {
                        if (hand.Input.TouchpadWestPressed)
                        {
                            ___m_isTwinStickSmoothTurningCounterClockwise = true;
                        }
                        else if (hand.Input.TouchpadEastPressed)
                        {
                            ___m_isTwinStickSmoothTurningClockwise = true;
                        }
                    }
                    else if (hand.CMode == ControlMode.Vive)
                    {
                        if (GM.Options.MovementOptions.Touchpad_Confirm == FVRMovementManager.TwoAxisMovementConfirm.OnClick)
                        {
                            if (hand.Input.TouchpadPressed)
                            {
                                if (hand.Input.TouchpadWestPressed)
                                {
                                    ___m_isTwinStickSmoothTurningCounterClockwise = true;
                                }
                                else if (hand.Input.TouchpadEastPressed)
                                {
                                    ___m_isTwinStickSmoothTurningClockwise = true;
                                }
                            }
                        }
                        else if (hand.Input.TouchpadWestPressed)
                        {
                            ___m_isTwinStickSmoothTurningCounterClockwise = true;
                        }
                        else if (hand.Input.TouchpadEastPressed)
                        {
                            ___m_isTwinStickSmoothTurningClockwise = true;
                        }
                    }
                    else if (hand.Input.Secondary2AxisWestPressed)
                    {
                        ___m_isTwinStickSmoothTurningCounterClockwise = true;
                    }
                    else if (hand.Input.Secondary2AxisEastPressed)
                    {
                        ___m_isTwinStickSmoothTurningClockwise = true;
                    }
                }
                MethodInfo jumpMethod = typeof(FVRMovementManager).GetMethod("Jump", BindingFlags.NonPublic | BindingFlags.Instance);
                if (GM.Options.MovementOptions.TwinStickJumpState == MovementOptions.TwinStickJumpMode.Enabled)
                {
                    if (hand.CMode == ControlMode.Oculus)
                    {
                        if (hand.Input.TouchpadSouthDown)
                        {
                            jumpMethod.Invoke(__instance, null);
                        }
                    }
                    else if (hand.CMode == ControlMode.Vive)
                    {
                        if (GM.Options.MovementOptions.Touchpad_Confirm == FVRMovementManager.TwoAxisMovementConfirm.OnClick)
                        {
                            if (hand.Input.TouchpadDown && hand.Input.TouchpadSouthPressed)
                            {
                                jumpMethod.Invoke(__instance, null);
                            }
                        }
                        else if (hand.Input.TouchpadSouthDown)
                        {
                            jumpMethod.Invoke(__instance, null);
                        }
                    }
                    else if (hand.Input.Secondary2AxisSouthDown)
                    {
                        jumpMethod.Invoke(__instance, null);
                    }
                }
                if (GM.Options.MovementOptions.TwinStickSprintState == MovementOptions.TwinStickSprintMode.RightStickForward)
                {
                    if (GM.Options.MovementOptions.TwinStickSprintToggleState == MovementOptions.TwinStickSprintToggleMode.Disabled)
                    {
                        // Also check stamina for sprinting
                        if (flag6 && Mod.stamina > 0)
                        {
                            ___m_sprintingEngaged = true;
                        }
                        else
                        {
                            ___m_sprintingEngaged = false;
                        }
                    }
                    else if (flag5)
                    {
                        ___m_sprintingEngaged = !___m_sprintingEngaged;
                    }
                }
            }
            else
            {
                if (GM.Options.MovementOptions.TwinStickSprintState == MovementOptions.TwinStickSprintMode.LeftStickClick)
                {
                    if (GM.Options.MovementOptions.TwinStickSprintToggleState == MovementOptions.TwinStickSprintToggleMode.Disabled)
                    {
                        // Also check stamina for sprinting
                        if (flag4 && Mod.stamina > 0)
                        {
                            ___m_sprintingEngaged = true;
                        }
                        else
                        {
                            ___m_sprintingEngaged = false;
                        }
                    }
                    else if (flag3)
                    {
                        ___m_sprintingEngaged = !___m_sprintingEngaged;
                    }
                }
                ___worldTPAxis = Vector3.zero;
                float y = vector.y;
                float x = vector.x;
                switch (GM.Options.MovementOptions.Touchpad_MovementMode)
                {
                    case FVRMovementManager.TwoAxisMovementMode.Standard:
                        ___worldTPAxis = y * hand.PointingTransform.forward + x * hand.PointingTransform.right * 0.75f;
                        ___worldTPAxis.y = 0f;
                        break;
                    case FVRMovementManager.TwoAxisMovementMode.Onward:
                        ___worldTPAxis = y * hand.Input.Forward + x * hand.Input.Right * 0.75f;
                        break;
                    case FVRMovementManager.TwoAxisMovementMode.LeveledHand:
                        {
                            Vector3 forward = hand.Input.Forward;
                            forward.y = 0f;
                            forward.Normalize();
                            Vector3 right = hand.Input.Right;
                            right.y = 0f;
                            right.Normalize();
                            ___worldTPAxis = y * forward + x * right * 0.75f;
                            break;
                        }
                    case FVRMovementManager.TwoAxisMovementMode.LeveledHead:
                        {
                            Vector3 forward2 = GM.CurrentPlayerBody.Head.forward;
                            forward2.y = 0f;
                            forward2.Normalize();
                            Vector3 right2 = GM.CurrentPlayerBody.Head.right;
                            right2.y = 0f;
                            right2.Normalize();
                            ___worldTPAxis = y * forward2 + x * right2 * 0.75f;
                            break;
                        }
                }
                Vector3 normalized = ___worldTPAxis.normalized;
                ___worldTPAxis *= GM.Options.MovementOptions.TPLocoSpeeds[GM.Options.MovementOptions.TPLocoSpeedIndex];
                if (hand.CMode == ControlMode.Vive && GM.Options.MovementOptions.Touchpad_Confirm == FVRMovementManager.TwoAxisMovementConfirm.OnClick)
                {
                    if (!flag4)
                    {
                        ___worldTPAxis = Vector3.zero;
                    }
                    else if (___m_sprintingEngaged && GM.Options.MovementOptions.TPLocoSpeedIndex < 5)
                    {
                        ___worldTPAxis += normalized * 2f;
                    }
                }
                else if (___m_sprintingEngaged && GM.Options.MovementOptions.TPLocoSpeedIndex < 5)
                {
                    ___worldTPAxis += normalized * 2f;
                }
                if (Mod.skills != null)
                {
                    ___worldTPAxis += ___worldTPAxis * (0.004f * (Mod.skills[1].currentProgress / 100));
                }
                if (___m_isGrounded)
                {
                    ___m_smoothLocoVelocity.x = ___worldTPAxis.x;
                    ___m_smoothLocoVelocity.z = ___worldTPAxis.z;
                    if (GM.CurrentSceneSettings.UsesMaxSpeedClamp)
                    {
                        Vector2 vector2 = new Vector2(___m_smoothLocoVelocity.x, ___m_smoothLocoVelocity.z);
                        if (vector2.magnitude > GM.CurrentSceneSettings.MaxSpeedClamp)
                        {
                            vector2 = vector2.normalized * GM.CurrentSceneSettings.MaxSpeedClamp;
                            ___m_smoothLocoVelocity.x = vector2.x;
                            ___m_smoothLocoVelocity.z = vector2.y;
                        }
                    }
                }
                else if (GM.CurrentSceneSettings.DoesAllowAirControl)
                {
                    Vector3 vector3 = new Vector3(___m_smoothLocoVelocity.x, 0f, ___m_smoothLocoVelocity.z);
                    ___m_smoothLocoVelocity.x = ___m_smoothLocoVelocity.x + ___worldTPAxis.x * Time.deltaTime;
                    ___m_smoothLocoVelocity.z = ___m_smoothLocoVelocity.z + ___worldTPAxis.z * Time.deltaTime;
                    Vector3 vector4 = new Vector3(___m_smoothLocoVelocity.x, 0f, ___m_smoothLocoVelocity.z);
                    float maxLength = Mathf.Max(1f, vector3.magnitude);
                    vector4 = Vector3.ClampMagnitude(vector4, maxLength);
                    ___m_smoothLocoVelocity.x = vector4.x;
                    ___m_smoothLocoVelocity.z = vector4.z;
                }
                else
                {
                    Vector3 vector5 = new Vector3(___m_smoothLocoVelocity.x, 0f, ___m_smoothLocoVelocity.z);
                    ___m_smoothLocoVelocity.x = ___m_smoothLocoVelocity.x + ___worldTPAxis.x * Time.deltaTime * 0.3f;
                    ___m_smoothLocoVelocity.z = ___m_smoothLocoVelocity.z + ___worldTPAxis.z * Time.deltaTime * 0.3f;
                    Vector3 vector6 = new Vector3(___m_smoothLocoVelocity.x, 0f, ___m_smoothLocoVelocity.z);
                    float maxLength2 = Mathf.Max(1f, vector5.magnitude);
                    vector6 = Vector3.ClampMagnitude(vector6, maxLength2);
                    ___m_smoothLocoVelocity.x = vector6.x;
                    ___m_smoothLocoVelocity.z = vector6.z;
                }
                if (flag3)
                {
                    ___m_timeSinceSprintDownClick = 0f;
                }
            }

            // Update fall damage depending on grounded and previous velocity
            if (Mod.currentLocationIndex == 2)
            {
                UpdateFallDamage(___m_isGrounded);
            }

            if (Mod.skills != null)
            {
                UpdateMovementAction(___m_smoothLocoVelocity, ___m_sprintingEngaged);
            }

            wasGrounded = ___m_isGrounded;
            previousVelocity = ___m_smoothLocoVelocity;

            return false;
        }

        private static void UpdateMovementAction(Vector3 velocity, bool sprinting)
        {
            Vector3 sideMovement = velocity * Time.deltaTime;
            sideMovement.y = 0;

            if (sprinting)
            {
                Mod.distanceTravelledSprinting += sideMovement.magnitude;
            }
            else if (sideMovement.magnitude > 0)
            {
                Mod.distanceTravelledWalking += sideMovement.magnitude;
            }
            // TODO: else if do covert movement
        }

        private static void UpdateFallDamage(bool grounded)
        {
            if (grounded && !wasGrounded)
            {
                // Considering realistic 1g of acceleration, t = (Vf-Vi)/a, and s = Vi * t + 0.5 * a * t ^ 2, s being distance fallen
                float t = previousVelocity.y / -9.806f; // Note that here, velocity and a are negative, giving a positive time
                float s = 4.903f  * t * t; // Here a is positive to have a positive distance fallen
                if (s > safeHeight)
                {
                    float damage = s * damagePerMeter;
                    float distribution = UnityEngine.Random.value;
                    if (UnityEngine.Random.value < 0.125 * (s - safeHeight)) // 100% chance of fracture 8+ meters fall above safe height
                    {
                        Effect fractureEffect = new Effect();
                        fractureEffect.effectType = Effect.EffectType.Fracture;
                        fractureEffect.partIndex = 5;
                        Effect.effects.Add(fractureEffect);
                        // TODO: Play fracture sound
                    }
                    if (UnityEngine.Random.value < 0.125 * (s - safeHeight)) // 100% chance of fracture 8+ meters fall above safe height
                    {
                        Effect fractureEffect = new Effect();
                        fractureEffect.effectType = Effect.EffectType.Fracture;
                        fractureEffect.partIndex = 6;
                        Effect.effects.Add(fractureEffect);
                        // TODO: Play fracture sound
                    }

                    DamagePatch.RegisterPlayerHit(5, distribution * damage, true);
                    DamagePatch.RegisterPlayerHit(6, (1 - distribution) * damage, true);
                }
            }
        }
    }

    // Patches FVRFireArmChamber.SetRound(round, bool) to keep track of weight in chamber
    class ChamberSetRoundPatch
    {
        static void Prefix(ref FVRFireArmRound round, ref FVRFireArmChamber __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }


            // TODO: Ammo container weight management will have to be reviewed. If we want to manage it, we will need to also keep track of the round and container's
            // locationIndex so we know when to add/remove weight from player also
            //if (__instance.IsFull && round == null)
            //{
            //    EFM_VanillaItemDescriptor VID = __instance.Firearm.GetComponent<EFM_VanillaItemDescriptor>();
            //    VID.currentWeight -= 15;
            //    if(VID.locationIndex == 0)
            //    {
            //        Mod.weight -= 15;
            //    }
            //}
            //else
            //{
            //    EFM_VanillaItemDescriptor VID = __instance.Firearm.GetComponent<EFM_VanillaItemDescriptor>();
            //    VID.currentWeight += 15;
            //    if (VID.locationIndex == 0)
            //    {
            //        Mod.weight += 15;
            //    }
            //}
        }
    }

    // Patches FVRFireArmChamber.SetRound(round, vector3, quaternion) to keep track of weight in chamber
    class ChamberSetRoundGivenPatch
    {
        static void Prefix(ref FVRFireArmRound round, ref FVRFireArmChamber __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }


            // TODO: Ammo container weight management will have to be reviewed. If we want to manage it, we will need to also keep track of the round and container's
            // locationIndex so we know when to add/remove weight from player also
            //if (__instance.IsFull && round == null)
            //{
            //    EFM_VanillaItemDescriptor VID = __instance.Firearm.GetComponent<EFM_VanillaItemDescriptor>();
            //    VID.currentWeight -= 15;
            //    if(VID.locationIndex == 0)
            //    {
            //        Mod.weight -= 15;
            //    }
            //}
            //else
            //{
            //    EFM_VanillaItemDescriptor VID = __instance.Firearm.GetComponent<EFM_VanillaItemDescriptor>();
            //    VID.currentWeight += 15;
            //    if (VID.locationIndex == 0)
            //    {
            //        Mod.weight += 15;
            //    }
            //}
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
                MeatovItem ammoBox = __instance.GetComponent<MeatovItem>();
                if (ammoBox != null && ammoBox.itemType == MeatovItem.ItemType.AmmoBox)
                {
                    Dictionary<FireArmRoundType, Dictionary<FireArmRoundClass, Dictionary<MeatovItem, int>>> dictToUse = null;
                    if (ammoBox.locationIndex == 0) // Player
                    {
                        dictToUse = Mod.ammoBoxesByRoundClassByRoundType;
                    }
                    else if (ammoBox.locationIndex == 1) // Hideout
                    {
                        dictToUse = HideoutController.instance.ammoBoxesByRoundClassByRoundType;
                    }
                    else // Raid
                    {
                        return;
                    }

                    FVRLoadedRound lr = __instance.LoadedRounds[__instance.m_numRounds - 1];
                    --dictToUse[__instance.RoundType][lr.LR_Class][ammoBox];
                    if (dictToUse[__instance.RoundType][lr.LR_Class][ammoBox] == 0)
                    {
                        dictToUse[__instance.RoundType][lr.LR_Class].Remove(ammoBox);
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
                MeatovItem ammoBox = __instance.GetComponent<MeatovItem>();
                if (ammoBox != null && ammoBox.itemType == MeatovItem.ItemType.AmmoBox)
                {
                    Dictionary<FireArmRoundType, Dictionary<FireArmRoundClass, Dictionary<MeatovItem, int>>> dictToUse = null;
                    if (ammoBox.locationIndex == 0) // Player
                    {
                        dictToUse = Mod.ammoBoxesByRoundClassByRoundType;
                    }
                    else if (ammoBox.locationIndex == 1) // Hideout
                    {
                        dictToUse = HideoutController.instance.ammoBoxesByRoundClassByRoundType;
                    }
                    else // Raid
                    {
                        return;
                    }

                    FVRLoadedRound lr = __instance.LoadedRounds[__instance.m_numRounds - 1];
                    --dictToUse[__instance.RoundType][lr.LR_Class][ammoBox];
                    if (dictToUse[__instance.RoundType][lr.LR_Class][ammoBox] == 0)
                    {
                        dictToUse[__instance.RoundType][lr.LR_Class].Remove(ammoBox);
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
                MeatovItem ammoBox = __instance.GetComponent<MeatovItem>();
                if (ammoBox != null && ammoBox.itemType == MeatovItem.ItemType.AmmoBox)
                {
                    Dictionary<FireArmRoundType, Dictionary<FireArmRoundClass, Dictionary<MeatovItem, int>>> dictToUse = null;
                    if (ammoBox.locationIndex == 0) // Player
                    {
                        dictToUse = Mod.ammoBoxesByRoundClassByRoundType;
                    }
                    else if (ammoBox.locationIndex == 1) // Hideout
                    {
                        dictToUse = HideoutController.instance.ammoBoxesByRoundClassByRoundType;
                    }
                    else // Raid
                    {
                        return;
                    }

                    FVRLoadedRound lr = __instance.LoadedRounds[__instance.m_numRounds - 1];
                    --dictToUse[__instance.RoundType][lr.LR_Class][ammoBox];
                    if (dictToUse[__instance.RoundType][lr.LR_Class][ammoBox] == 0)
                    {
                        dictToUse[__instance.RoundType][lr.LR_Class].Remove(ammoBox);
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

    // Patches FVRFireArmClip.RemoveRound() to keep track of weight of ammo in clip
    class ClipRemoveRoundPatch
    {
        static int preNumRounds = 0;

        static void Prefix(int ___m_numRounds, ref FVRFireArmClip __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            preNumRounds = ___m_numRounds;
        }

        static void Postfix(int ___m_numRounds, ref FVRFireArmClip __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            int postNumRounds = ___m_numRounds;

            if (__instance.m_hand != null)
            {
                Mod.AddSkillExp(Skill.raidUnloadedAmmoAction, 31);
            }


            // TODO: Ammo container weight management will have to be reviewed. If we want to manage it, we will need to also keep track of the round and container's
            // locationIndex so we know when to add/remove weight from player also
            //__instance.GetComponent<EFM_VanillaItemDescriptor>().currentWeight -= 15 * (preNumRounds - postNumRounds);
        }
    }

    // Patches FVRFireArmClip.RemoveRound(bool) to keep track of weight of ammo in clip
    // TODO: See if this could be used to do what we do in ClipUpdateInteractionPatch instead
    class ClipRemoveRoundBoolPatch
    {
        static int preNumRounds = 0;

        static void Prefix(int ___m_numRounds, ref FVRFireArmClip __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            preNumRounds = ___m_numRounds;
        }

        static void Postfix(int ___m_numRounds, ref FVRFireArmClip __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            int postNumRounds = ___m_numRounds;

            if (__instance.m_hand != null)
            {
                Mod.AddSkillExp(Skill.raidUnloadedAmmoAction, 31);
            }


            // TODO: Ammo container weight management will have to be reviewed. If we want to manage it, we will need to also keep track of the round and container's
            // locationIndex so we know when to add/remove weight from player also
            //__instance.GetComponent<EFM_VanillaItemDescriptor>().currentWeight -= 15 * (preNumRounds - postNumRounds);
        }
    }

    // Patches FVRFireArmClip.RemoveRoundReturnClass to keep track of weight of ammo in clip
    class ClipRemoveRoundClassPatch
    {
        static int preNumRounds = 0;

        static void Prefix(int ___m_numRounds, ref FVRFireArmClip __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            preNumRounds = ___m_numRounds;
        }

        static void Postfix(int ___m_numRounds, ref FVRFireArmClip __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            int postNumRounds = ___m_numRounds;

            if (__instance.m_hand != null)
            {
                Mod.AddSkillExp(Skill.raidUnloadedAmmoAction, 31);
            }


            // TODO: Ammo container weight management will have to be reviewed. If we want to manage it, we will need to also keep track of the round and container's
            // locationIndex so we know when to add/remove weight from player also
            //__instance.GetComponent<EFM_VanillaItemDescriptor>().currentWeight -= 15 * (preNumRounds - postNumRounds);
        }
    }

    // Patches FVRFireArm.LoadMag to keep track of weight of mag on firearm and its location index
    class FireArmLoadMagPatch
    {
        public static bool ignoreLoadMag;

        static void Prefix(FVRFireArmMagazine mag, ref FVRFireArm __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (ignoreLoadMag)
            {
                ignoreLoadMag = false;
                return;
            }

            if (mag.m_hand != null)
            {
                // TODO: Might have to do this for ammo when putting it into a mag?
                MeatovItem fireArmMI = __instance.GetComponent<MeatovItem>();
                switch (fireArmMI.weaponClass)
                {
                    case MeatovItem.WeaponClass.Pistol:
                        Mod.AddSkillExp(Skill.pistolWeaponReloadAction, 12);
                        break;
                    case MeatovItem.WeaponClass.Revolver:
                        Mod.AddSkillExp(Skill.revolverWeaponReloadAction, 13);
                        break;
                    case MeatovItem.WeaponClass.SMG:
                        Mod.AddSkillExp(Skill.SMGWeaponReloadAction, 14);
                        break;
                    case MeatovItem.WeaponClass.Assault:
                        Mod.AddSkillExp(Skill.assaultWeaponReloadAction, 15);
                        break;
                    case MeatovItem.WeaponClass.Shotgun:
                        Mod.AddSkillExp(Skill.shotgunWeaponReloadAction, 16);
                        break;
                    case MeatovItem.WeaponClass.Sniper:
                        Mod.AddSkillExp(Skill.sniperWeaponReloadAction, 17);
                        break;
                    case MeatovItem.WeaponClass.LMG:
                        Mod.AddSkillExp(Skill.LMGWeaponReloadAction, 18);
                        break;
                    case MeatovItem.WeaponClass.HMG:
                        Mod.AddSkillExp(Skill.HMGWeaponReloadAction, 19);
                        break;
                    case MeatovItem.WeaponClass.Launcher:
                        Mod.AddSkillExp(Skill.launcherWeaponReloadAction, 20);
                        break;
                    case MeatovItem.WeaponClass.AttachedLauncher:
                        Mod.AddSkillExp(Skill.attachedLauncherWeaponReloadAction, 21);
                        break;
                    case MeatovItem.WeaponClass.DMR:
                        Mod.AddSkillExp(Skill.DMRWeaponReloadAction, 24);
                        break;
                }
            }

            if (__instance.Magazine == null && mag != null)
            {
                MeatovItem magMI = mag.GetComponent<MeatovItem>();
                MeatovItem fireArmMI = __instance.GetComponent<MeatovItem>();
                fireArmMI.currentWeight += magMI.currentWeight;

                if (magMI.locationIndex == 0) // Player
                {
                    // Went from player to firearm location index
                    if (fireArmMI.locationIndex == 0) // Player
                    {
                        // Even if transfered from player to player, we don't want to consider it in inventory anymore
                        if (!Mod.preventLoadMagUpdateLists)
                        {
                            magMI.UpdateInventories();
                        }

                        // No difference to weight
                    }
                    else // Hideout/Raid
                    {
                        // Transfered from player to hideout or raid but we dont want to consider it in baseinventory because it is inside a firearm
                        if (!Mod.preventLoadMagUpdateLists)
                        {
                            magMI.UpdateInventories();
                        }
                    }
                }
                else if (magMI.locationIndex == 1) // Hideout
                {
                    // Went from hideout to firearm locationIndex
                    if (fireArmMI.locationIndex == 0) // Player
                    {
                        // Transfered from hideout to player, dont want to consider it in player inventory because it is in firearm
                        if (!Mod.preventLoadMagUpdateLists)
                        {
                            magMI.UpdateInventories();
                        }
                    }
                    else if (fireArmMI.locationIndex == 1) // Hideout
                    {
                        // Transfered from hideout to hideout, dont want to consider it in base inventory because it is in firearm
                        if (!Mod.preventLoadMagUpdateLists)
                        {
                            magMI.UpdateInventories();
                        }
                    }
                    else // Raid
                    {
                        Mod.LogError("Fire arm load mag patch impossible case: Mag loaded from hideout to raid, meaning mag had wrong location index while on player");
                    }
                }
                else // Raid
                {
                    if (fireArmMI.locationIndex == 0) // Player
                    {
                        // Transfered from raid to player, dont want to add to inventory because it is in firearm
                    }
                }
            }
        }
    }

    // Patches FVRFireArm.EjectMag to keep track of weight of mag on firearm and its location index
    class FireArmEjectMagPatch
    {
        static int preLocationIndex;
        static MeatovItem preMagMI;

        static void Prefix(ref FVRFireArm __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (__instance.Magazine != null)
            {
                MeatovItem magMI = __instance.Magazine.GetComponent<MeatovItem>();

                preLocationIndex = magMI.locationIndex;
                preMagMI = magMI;
            }
        }

        static void Postfix(ref FVRFireArm __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }
            MeatovItem fireArmMI = __instance.GetComponent<MeatovItem>();
            fireArmMI.currentWeight -= preMagMI.currentWeight;

            preMagMI.UpdateInventories();
        }
    }

    // Patches FVRFireArm.LoadClip to keep track of weight of clip on firearm and its location index
    class FireArmLoadClipPatch
    {
        public static bool ignoreLoadClip;

        static void Prefix(FVRFireArmClip clip, ref FVRFireArm __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (ignoreLoadClip)
            {
                ignoreLoadClip = false;
                return;
            }

            if (clip.m_hand != null)
            {
                MeatovItem fireArmMI = __instance.GetComponent<MeatovItem>();
                switch (fireArmMI.weaponClass)
                {
                    case MeatovItem.WeaponClass.Pistol:
                        Mod.AddSkillExp(Skill.pistolWeaponReloadAction, 12);
                        break;
                    case MeatovItem.WeaponClass.Revolver:
                        Mod.AddSkillExp(Skill.revolverWeaponReloadAction, 13);
                        break;
                    case MeatovItem.WeaponClass.SMG:
                        Mod.AddSkillExp(Skill.SMGWeaponReloadAction, 14);
                        break;
                    case MeatovItem.WeaponClass.Assault:
                        Mod.AddSkillExp(Skill.assaultWeaponReloadAction, 15);
                        break;
                    case MeatovItem.WeaponClass.Shotgun:
                        Mod.AddSkillExp(Skill.shotgunWeaponReloadAction, 16);
                        break;
                    case MeatovItem.WeaponClass.Sniper:
                        Mod.AddSkillExp(Skill.sniperWeaponReloadAction, 17);
                        break;
                    case MeatovItem.WeaponClass.LMG:
                        Mod.AddSkillExp(Skill.LMGWeaponReloadAction, 18);
                        break;
                    case MeatovItem.WeaponClass.HMG:
                        Mod.AddSkillExp(Skill.HMGWeaponReloadAction, 19);
                        break;
                    case MeatovItem.WeaponClass.Launcher:
                        Mod.AddSkillExp(Skill.launcherWeaponReloadAction, 20);
                        break;
                    case MeatovItem.WeaponClass.AttachedLauncher:
                        Mod.AddSkillExp(Skill.attachedLauncherWeaponReloadAction, 21);
                        break;
                    case MeatovItem.WeaponClass.DMR:
                        Mod.AddSkillExp(Skill.DMRWeaponReloadAction, 24);
                        break;
                }
            }

            if (__instance.Clip == null && clip != null)
            {
                MeatovItem clipMI = clip.GetComponent<MeatovItem>();
                MeatovItem fireArmMI = __instance.GetComponent<MeatovItem>();
                fireArmMI.currentWeight += clipMI.currentWeight;

                clipMI.UpdateInventories();
            }
        }
    }

    // Patches FVRFireArm.EjectClip to keep track of weight of clip on firearm and its location index
    class FireArmEjectClipPatch
    {
        static int preLocationIndex;
        static MeatovItem preClipMI;

        static void Prefix(ref FVRFireArm __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (__instance.Clip != null)
            {
                MeatovItem clipMI = __instance.Clip.GetComponent<MeatovItem>();

                preLocationIndex = clipMI.locationIndex;
                preClipMI = clipMI;
            }
        }

        static void Postfix(ref FVRFireArm __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            MeatovItem fireArmMI = __instance.GetComponent<MeatovItem>();
            fireArmMI.currentWeight -= preClipMI.currentWeight;

            preClipMI.UpdateInventories();
        }
    }

    // Patches FVRFirearmMagazine.AddRound(Round) to track ammo in ammo boxes
    class MagAddRoundPatch
    {
        static void Prefix(FVRFireArmMagazine __instance, FVRFireArmRound round)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (__instance.m_numRounds < __instance.m_capacity)
            {
                MeatovItem ammoBox = __instance.GetComponent<MeatovItem>();
                if(ammoBox != null && ammoBox.itemType == MeatovItem.ItemType.AmmoBox)
                {
                    Dictionary<FireArmRoundType, Dictionary<FireArmRoundClass, Dictionary<MeatovItem, int>>> dictToUse = null;
                    if (ammoBox.locationIndex == 0) // Player
                    {
                        dictToUse = Mod.ammoBoxesByRoundClassByRoundType;
                    }
                    else if(ammoBox.locationIndex == 1) // Hideout
                    {
                        dictToUse = HideoutController.instance.ammoBoxesByRoundClassByRoundType;
                    }
                    else // Raid
                    {
                        return;
                    }

                    if(dictToUse != null)
                    {
                        if (dictToUse.TryGetValue(__instance.RoundType, out Dictionary<FireArmRoundClass, Dictionary<MeatovItem, int>> midDict))
                        {
                            if (midDict.TryGetValue(round.RoundClass, out Dictionary<MeatovItem, int> boxDict))
                            {
                                int count = 0;
                                if (boxDict.TryGetValue(ammoBox, out count))
                                {
                                    ++boxDict[ammoBox];
                                }
                                else
                                {
                                    boxDict.Add(ammoBox, 1);
                                }
                            }
                            else
                            {
                                Dictionary<MeatovItem, int> newBoxDict = new Dictionary<MeatovItem, int>();
                                newBoxDict.Add(ammoBox, 1);
                                midDict.Add(round.RoundClass, newBoxDict);
                            }
                        }
                        else
                        {
                            Dictionary<MeatovItem, int> newBoxDict = new Dictionary<MeatovItem, int>();
                            newBoxDict.Add(ammoBox, 1);
                            Dictionary<FireArmRoundClass, Dictionary<MeatovItem, int>> newMidDict = new Dictionary<FireArmRoundClass, Dictionary<MeatovItem, int>>();
                            newMidDict.Add(round.RoundClass, newBoxDict);
                            dictToUse.Add(__instance.RoundType, newMidDict);
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
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (__instance.m_numRounds < __instance.m_capacity)
            {
                MeatovItem ammoBox = __instance.GetComponent<MeatovItem>();
                if (ammoBox != null && ammoBox.itemType == MeatovItem.ItemType.AmmoBox)
                {
                    Dictionary<FireArmRoundType, Dictionary<FireArmRoundClass, Dictionary<MeatovItem, int>>> dictToUse = null;
                    if (ammoBox.locationIndex == 0) // Player
                    {
                        dictToUse = Mod.ammoBoxesByRoundClassByRoundType;
                    }
                    else if (ammoBox.locationIndex == 1) // Hideout
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
                                if (boxDict.TryGetValue(ammoBox, out count))
                                {
                                    ++boxDict[ammoBox];
                                }
                                else
                                {
                                    boxDict.Add(ammoBox, 1);
                                }
                            }
                            else
                            {
                                Dictionary<MeatovItem, int> newBoxDict = new Dictionary<MeatovItem, int>();
                                newBoxDict.Add(ammoBox, 1);
                                midDict.Add(rClass, newBoxDict);
                            }
                        }
                        else
                        {
                            Dictionary<MeatovItem, int> newBoxDict = new Dictionary<MeatovItem, int>();
                            newBoxDict.Add(ammoBox, 1);
                            Dictionary<FireArmRoundClass, Dictionary<MeatovItem, int>> newMidDict = new Dictionary<FireArmRoundClass, Dictionary<MeatovItem, int>>();
                            newMidDict.Add(rClass, newBoxDict);
                            dictToUse.Add(__instance.RoundType, newMidDict);
                        }
                    }
                }
            }
        }
    }

    // Patches FVRFirearmClip.AddRound(Round) to keep track of weight
    class ClipAddRoundPatch
    {
        static bool addedRound = false;

        static void Prefix(ref FVRFireArmClip __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (__instance.m_numRounds < __instance.m_capacity)
            {
                addedRound = true;
            }
        }

        static void Postfix(ref FVRFireArmClip __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            Mod.AddSkillExp(Skill.raidLoadedAmmoAction, 31);

            if (addedRound)
            {
                addedRound = false;

                // TODO: Ammo container weight management will have to be reviewed. If we want to manage it, we will need to also keep track of the round and container's
                // locationIndex so we know when to add/remove weight from player also
                //__instance.GetComponent<EFM_VanillaItemDescriptor>().currentWeight += 15;
            }
        }
    }

    // Patches FVRFirearmClip.AddRound(Class) to keep track of weight
    class ClipAddRoundClassPatch
    {
        static bool addedRound = false;

        static void Prefix(ref FVRFireArmClip __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (__instance.m_numRounds < __instance.m_capacity)
            {
                addedRound = true;
            }
        }

        static void Postfix(ref FVRFireArmClip __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (addedRound)
            {
                addedRound = false;

                // TODO: Ammo container weight management will have to be reviewed. If we want to manage it, we will need to also keep track of the round and container's
                // locationIndex so we know when to add/remove weight from player also
                //__instance.GetComponent<EFM_VanillaItemDescriptor>().currentWeight += 15;
            }
        }
    }

    // Patches FVRFireArmAttachmentMount.RegisterAttachment to keep track of weight
    // This completely replaces the original
    class AttachmentMountRegisterPatch
    {
        static bool Prefix(FVRFireArmAttachment attachment, ref HashSet<FVRFireArmAttachment> ___AttachmentsHash, ref FVRFireArmAttachmentMount __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }

            if (___AttachmentsHash.Add(attachment))
            {
                __instance.AttachmentsList.Add(attachment);
                if (__instance.HasHoverDisablePiece && __instance.DisableOnHover.activeSelf)
                {
                    __instance.DisableOnHover.SetActive(false);
                }

                // Add weight to parent
                MeatovItem parentMI = __instance.Parent.GetComponent<MeatovItem>();
                MeatovItem attachmentMI = __instance.Parent.GetComponent<MeatovItem>();
                parentMI.currentWeight += attachmentMI.currentWeight;

                attachmentMI.UpdateInventories();
            }

            return false;
        }
    }

    // Patches FVRFireArmAttachmentMount.DeRegisterAttachment to keep track of weight
    // This completely replaces the original
    class AttachmentMountDeRegisterPatch
    {
        static bool Prefix(FVRFireArmAttachment attachment, ref HashSet<FVRFireArmAttachment> ___AttachmentsHash, ref FVRFireArmAttachmentMount __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }

            if (___AttachmentsHash.Remove(attachment))
            {
                __instance.AttachmentsList.Remove(attachment);

                // Remove weight from parent
                MeatovItem parentMI = __instance.Parent.GetComponent<MeatovItem>();
                MeatovItem attachmentMI = __instance.Parent.GetComponent<MeatovItem>();
                parentMI.currentWeight -= attachmentMI.currentWeight;

                attachmentMI.UpdateInventories();
            }

            return false;
        }
    }

    // Patches AIManager.EntityCheck to use our own entity lists instead of OverlapSphere to check for other entities
    // This completely replaces the original
    class EntityCheckPatch
    {
        static bool Prefix(AIEntity e)
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }

            e.ResetTick();
            if (e.ReceivesEvent_Visual)
            {
                Vector3 pos = e.GetPos();
                Vector3 forward = e.SensoryFrame.forward;
                //if (Raid_Manager.entities.Count > 0)
                //{
                //    for (int i = 0; i < Raid_Manager.entities.Count; i++)
                //    {
                //        AIEntity component = Raid_Manager.entities[i];
                //        if (!(component == null))
                //        {
                //            if (!(component == e))
                //            {
                //                if (component.IFFCode >= -1)
                //                {
                //                    if (!component.IsPassiveEntity || e.PerceivesPassiveEntities)
                //                    {
                //                        Vector3 pos2 = component.GetPos();
                //                        Vector3 to = pos2 - pos;
                //                        float num = to.magnitude;
                //                        float dist = num;
                //                        float num2 = e.MaximumSightRange;
                //                        if (num <= component.MaxDistanceVisibleFrom)
                //                        {
                //                            if (component.VisibilityMultiplier <= 2f)
                //                            {
                //                                if (component.VisibilityMultiplier > 1f)
                //                                {
                //                                    num = Mathf.Lerp(num, num2, component.VisibilityMultiplier - 1f);
                //                                }
                //                                else
                //                                {
                //                                    num = Mathf.Lerp(0f, num, component.VisibilityMultiplier);
                //                                }
                //                                if (!e.IsVisualCheckOmni)
                //                                {
                //                                    float num3 = Vector3.Angle(forward, to);
                //                                    num2 = e.MaximumSightRange * e.SightDistanceByFOVMultiplier.Evaluate(num3 / e.MaximumSightFOV);
                //                                }
                //                                if (num <= num2)
                //                                {
                //                                    if (!Physics.Linecast(pos, pos2, e.LM_VisualOcclusionCheck, QueryTriggerInteraction.Collide))
                //                                    {
                //                                        float v = num / e.MaximumSightRange * component.DangerMultiplier;
                //                                        AIEvent e2 = new AIEvent(component, AIEvent.AIEType.Visual, v, dist);
                //                                        e.OnAIEventReceive(e2);
                //                                    }
                //                                }
                //                            }
                //                        }
                //                    }
                //                }
                //            }
                //        }
                //    }
                //}
            }

            return false;
        }
    }

    // Patches FVRFireArmChamber.EjectRound to keep track of the ejected round
    class ChamberEjectRoundPatch
    {
        static void Postfix(ref FVRFireArmRound __result)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (__result != null && !__result.IsSpent)
            {
                MeatovItem MI = __result.GetComponent<MeatovItem>();

                MI.UpdateInventories();
            }
        }
    }

    // Patches FVRInteractiveObject.GlobalFixedUpdate to fix positioning of attachments after hideout load
    class GlobalFixedUpdatePatch
    {
        static void Postfix()
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (Mod.physObjColResetNeeded >= 0)
            {
                --Mod.physObjColResetNeeded;

                if (Mod.physObjColResetNeeded == 0)
                {
                    foreach (FVRPhysicalObject physObj in Mod.physObjColResetList)
                    {
                        physObj.SetAllCollidersToLayer(false, "Default");
                    }
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

    // Patches FVRViveHand.CurrentInteractable.set to keep track of item held
    class HandCurrentInteractableSetPatch
    {
        static void Prefix(ref FVRViveHand __instance, FVRInteractiveObject value, FVRInteractiveObject ___m_currentInteractable)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (___m_currentInteractable != null)
            {
                if (Mod.meatovItemByInteractive.TryGetValue(___m_currentInteractable, out MeatovItem meatovItem))
                {
                    meatovItem.EndInteraction(__instance.IsThisTheRightHand ? Mod.rightHand : Mod.leftHand);
                }
            }

            if(value != null)
            {
                if (Mod.meatovItemByInteractive.TryGetValue(value, out MeatovItem meatovItem))
                {
                    meatovItem.BeginInteraction(__instance.IsThisTheRightHand ? Mod.rightHand : Mod.leftHand);
                }
            }
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
                if (Mod.health[i] != 0)
                {
                    Mod.health[i] = Mathf.Clamp(Mod.health[i] + amountHealed / 7, Mod.health[i], Mod.currentMaxHealth[i]);
                }
            }
        }
    }

    // Patches Object.Internal_CloneSingle to keep track of this type of instantiation
    class Internal_CloneSinglePatch
    {
        static void Postfix(ref UnityEngine.Object __result)
        {
            if (__result == null || !Mod.inMeatovScene)
            {
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
                    FVRPhysicalObject physicalObject = go.GetComponent<FVRPhysicalObject>();
                    if (physicalObject != null && physicalObject.ObjectWrapper != null)
                    {
                        // Must setup the item if it is vanilla
                        if (!physicalObject.ObjectWrapper.ItemID.StartsWith("Meatov"))
                        {
                            MeatovItem.Setup(physicalObject);
                        }
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
            if (__result == null || !Mod.inMeatovScene)
            {
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
                    FVRPhysicalObject physicalObject = go.GetComponent<FVRPhysicalObject>();
                    if (physicalObject != null && physicalObject.ObjectWrapper != null)
                    {
                        // Must setup the item if it is vanilla
                        if (!physicalObject.ObjectWrapper.ItemID.StartsWith("Meatov"))
                        {
                            MeatovItem.Setup(physicalObject);
                        }
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
            if (__result == null || !Mod.inMeatovScene)
            {
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
                    FVRPhysicalObject physicalObject = go.GetComponent<FVRPhysicalObject>();
                    if (physicalObject != null && physicalObject.ObjectWrapper != null)
                    {
                        // Must setup the item if it is vanilla
                        if (!physicalObject.ObjectWrapper.ItemID.StartsWith("Meatov"))
                        {
                            MeatovItem.Setup(physicalObject);
                        }
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
            if (__result == null || !Mod.inMeatovScene)
            {
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
                    FVRPhysicalObject physicalObject = go.GetComponent<FVRPhysicalObject>();
                    if (physicalObject != null && physicalObject.ObjectWrapper != null)
                    {
                        // Must setup the item if it is vanilla
                        if (!physicalObject.ObjectWrapper.ItemID.StartsWith("Meatov"))
                        {
                            MeatovItem.Setup(physicalObject);
                        }
                    }
                }
            }
        }
    }
    #endregion
}

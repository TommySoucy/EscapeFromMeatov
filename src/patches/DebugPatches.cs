using FistVR;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine;

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

    #region DebugPatches
    class EventSystemUpdateDebugPatch
    {
        static void Prefix(ref EventSystem __instance)
        {
            Mod.LogInfo("Update called on event sys in scene " + __instance.gameObject.scene.name + "  physically at: ");
            Transform parent = __instance.transform.parent;
            while (parent != null)
            {
                Mod.LogInfo(parent.name);
                parent = parent.parent;
            }

            BaseInputModule sim = __instance.GetComponent<BaseInputModule>();
            if (sim != null)
            {
                sim.enabled = false;
                GameObject.Destroy(sim);
            }
            GameObject.Destroy(__instance);
        }
    }

    class InteractiveGlobalUpdateDebugPatch
    {
        static bool Prefix()
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }

            return false;
        }
    }

    class inputModuleProcessDebugPatch
    {
        static void Prefix(ref StandaloneInputModule __instance)
        {
            Mod.LogInfo("Process called on standalone input module in scene " + __instance.gameObject.scene.name + " physically at: ");
            Transform parent = __instance.transform.parent;
            while (parent != null)
            {
                Mod.LogInfo(parent.name);
                parent = parent.parent;
            }

            GameObject.Destroy(__instance);
        }
    }

    class DequeueAndPlayDebugPatch
    {
        //AudioSourcePool private instance
        /*private FVRPooledAudioSource DequeueAndPlay(AudioEvent clipSet, Vector3 pos, Vector2 pitch, Vector2 volume, AudioMixerGroup mixerOverride = null)
			{
				FVRPooledAudioSource fvrpooledAudioSource = this.SourceQueue_Disabled.Dequeue();
				fvrpooledAudioSource.gameObject.SetActive(true);
				fvrpooledAudioSource.Play(clipSet, pos, pitch, volume, mixerOverride);
				this.ActiveSources.Add(fvrpooledAudioSource);
				return fvrpooledAudioSource;
			}*/

        static bool Prefix(AudioEvent clipSet, Vector3 pos, Vector2 pitch, Vector2 volume, AudioMixerGroup mixerOverride, ref SM.AudioSourcePool __instance, ref FVRPooledAudioSource __result)
        {
            try
            {
                FVRPooledAudioSource fvrpooledAudioSource = __instance.SourceQueue_Disabled.Dequeue();
                fvrpooledAudioSource.gameObject.SetActive(true);
                fvrpooledAudioSource.Play(clipSet, pos, pitch, volume, mixerOverride);
                __instance.ActiveSources.Add(fvrpooledAudioSource);
                __result = fvrpooledAudioSource;
            }
            catch (NullReferenceException e)
            {
                Mod.LogError("DequeueAndPlayDebugPatch called but threw null exception, __instance.SourceQueue_Disabled null?: " + (__instance.SourceQueue_Disabled == null) + ", __instance.ActiveSources null?: " + (__instance.ActiveSources == null) + ":\n" + e.StackTrace);
            }
            return false;
        }
    }

    class UpdateModeTwoAxisPatch
    {
        static bool Prefix(ref FVRMovementManager __instance, bool IsTwinstick, ref Vector3 ___CurNeckPos, ref Vector3 ___LastNeckPos, ref Vector3 ___correctionDir,
                           ref bool ___m_isLeftHandActive, ref bool ___m_isGrounded, ref Vector3 ___m_smoothLocoVelocity, ref FVRViveHand ___m_authoratativeHand,
                           ref float ___m_armSwingerStepHeight, ref float ___m_delayGroundCheck, ref RaycastHit ___m_hit_ray, ref Vector3 ___m_groundPoint,
                           ref bool ___m_isTwinStickSmoothTurningClockwise, ref bool ___m_isTwinStickSmoothTurningCounterClockwise, ref bool ___IsGrabHolding)
        {
            ___CurNeckPos = GM.CurrentPlayerBody.NeckJointTransform.position;
            Vector3 vector = ___LastNeckPos - ___CurNeckPos;
            Vector3 lastNeckPos = ___LastNeckPos;
            Vector3 a = ___CurNeckPos - ___LastNeckPos;
            RaycastHit raycastHit;
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            if (Physics.SphereCast(___LastNeckPos, 0.15f, a.normalized, out raycastHit, a.magnitude, __instance.LM_TeleCast))
            {
                ___correctionDir = -a * 1f;
            }
            if (IsTwinstick)
            {
                if (!___m_isLeftHandActive && ___m_isGrounded)
                {
                    ___m_smoothLocoVelocity.x = 0f;
                    ___m_smoothLocoVelocity.z = 0f;
                }
            }
            else if (___m_authoratativeHand == null && ___m_isGrounded)
            {
                ___m_smoothLocoVelocity.x = 0f;
                ___m_smoothLocoVelocity.z = 0f;
            }
            Vector3 vector2 = lastNeckPos;
            Vector3 b = vector2;
            vector2.y = Mathf.Max(vector2.y, __instance.transform.position.y + ___m_armSwingerStepHeight);
            b.y = __instance.transform.position.y;
            float num = Vector3.Distance(vector2, b);
            if (___m_delayGroundCheck > 0f)
            {
                num *= 0.5f;
            }
            bool flag = false;
            Vector3 planeNormal = Vector3.up;
            bool flag2 = false;
            Vector3 vector3 = Vector3.up;
            Vector3 groundPoint = vector2 + -Vector3.up * num;
            Vector3 groundPoint2 = vector2 + -Vector3.up * num;
            float num2 = 90f;
            float a2 = -1000f;
            if (Physics.SphereCast(vector2, 0.2f, -Vector3.up, out ___m_hit_ray, num, __instance.LM_TeleCast))
            {
                vector3 = ___m_hit_ray.normal;
                groundPoint = ___m_hit_ray.point;
                groundPoint2 = ___m_hit_ray.point;
                num2 = Vector3.Angle(Vector3.up, ___m_hit_ray.normal);
                a2 = groundPoint.y;
                flag2 = true;
            }
            if (Physics.Raycast(vector2, -Vector3.up, out ___m_hit_ray, num, __instance.LM_TeleCast))
            {
                num2 = Mathf.Min(num2, Vector3.Angle(Vector3.up, ___m_hit_ray.normal));
                vector3 = ___m_hit_ray.normal;
                groundPoint.y = Mathf.Max(groundPoint.y, ___m_hit_ray.point.y);
                groundPoint2.y = Mathf.Min(groundPoint.y, ___m_hit_ray.point.y);
                a2 = Mathf.Max(a2, ___m_hit_ray.point.y);
                flag2 = true;
            }
            Vector3 vector4 = __instance.Head.forward;
            vector4.y = 0f;
            vector4.Normalize();
            vector4 = Vector3.ClampMagnitude(vector4, 0.1f);
            Vector3 vector5 = __instance.Head.right;
            vector5.y = 0f;
            vector5.Normalize();
            vector5 = Vector3.ClampMagnitude(vector5, 0.1f);
            Vector3 b2 = -vector4;
            Vector3 b3 = -vector5;
            if (Physics.Raycast(vector2 + vector4, -Vector3.up, out ___m_hit_ray, num, __instance.LM_TeleCast))
            {
                num2 = Mathf.Min(num2, Vector3.Angle(Vector3.up, ___m_hit_ray.normal));
                groundPoint.y = Mathf.Max(groundPoint.y, ___m_hit_ray.point.y);
                groundPoint2.y = Mathf.Min(groundPoint.y, ___m_hit_ray.point.y);
                a2 = Mathf.Max(a2, ___m_hit_ray.point.y);
                if (!flag2)
                {
                    vector3 = ___m_hit_ray.normal;
                    flag2 = true;
                }
            }
            if (Physics.Raycast(vector2 + vector5, -Vector3.up, out ___m_hit_ray, num, __instance.LM_TeleCast))
            {
                num2 = Mathf.Min(num2, Vector3.Angle(Vector3.up, ___m_hit_ray.normal));
                groundPoint.y = Mathf.Max(groundPoint.y, ___m_hit_ray.point.y);
                groundPoint2.y = Mathf.Min(groundPoint.y, ___m_hit_ray.point.y);
                a2 = Mathf.Max(a2, ___m_hit_ray.point.y);
                if (!flag2)
                {
                    vector3 = ___m_hit_ray.normal;
                    flag2 = true;
                }
            }
            if (Physics.Raycast(vector2 + b2, -Vector3.up, out ___m_hit_ray, num, __instance.LM_TeleCast))
            {
                num2 = Mathf.Min(num2, Vector3.Angle(Vector3.up, ___m_hit_ray.normal));
                groundPoint.y = Mathf.Max(groundPoint.y, ___m_hit_ray.point.y);
                groundPoint2.y = Mathf.Min(groundPoint.y, ___m_hit_ray.point.y);
                a2 = Mathf.Max(a2, ___m_hit_ray.point.y);
                if (!flag2)
                {
                    vector3 = ___m_hit_ray.normal;
                    flag2 = true;
                }
            }
            if (Physics.Raycast(vector2 + b3, -Vector3.up, out ___m_hit_ray, num, __instance.LM_TeleCast))
            {
                num2 = Mathf.Min(num2, Vector3.Angle(Vector3.up, ___m_hit_ray.normal));
                groundPoint.y = Mathf.Max(groundPoint.y, ___m_hit_ray.point.y);
                groundPoint2.y = Mathf.Min(groundPoint.y, ___m_hit_ray.point.y);
                a2 = Mathf.Max(a2, ___m_hit_ray.point.y);
                if (!flag2)
                {
                    vector3 = ___m_hit_ray.normal;
                    flag2 = true;
                }
            }
            if (flag2)
            {
                if (num2 > 70f)
                {
                    flag = true;
                    ___m_isGrounded = false;
                    planeNormal = vector3;
                    ___m_groundPoint = groundPoint2;
                }
                else
                {
                    ___m_isGrounded = true;
                    ___m_groundPoint = groundPoint;
                }
            }
            else
            {
                ___m_isGrounded = false;
                ___m_groundPoint = vector2 - Vector3.up * num;
            }
            Vector3 vector6 = lastNeckPos;
            Vector3 b4 = vector6;
            b4.y = __instance.transform.position.y + 2.15f * GM.CurrentPlayerBody.transform.localScale.y;
            float maxDistance = Vector3.Distance(vector6, b4);
            float num3 = vector6.y + 0.15f;
            if (Physics.SphereCast(vector6, 0.15f, Vector3.up, out ___m_hit_ray, maxDistance, __instance.LM_TeleCast))
            {
                Vector3 point = ___m_hit_ray.point;
                float num4 = Vector3.Distance(vector6, new Vector3(vector6.x, point.y, vector6.z));
                num3 = ___m_hit_ray.point.y - 0.15f;
                float num5 = Mathf.Clamp(GM.CurrentPlayerBody.Head.localPosition.y, 0.3f, 2.5f);
                float y = ___m_groundPoint.y;
                float min = y - (num5 - 0.2f);
                float y2 = Mathf.Clamp(num3 - num5 - 0.15f, min, y);
                ___m_groundPoint.y = y2;
            }
            if (___m_isGrounded)
            {
                ___m_smoothLocoVelocity.y = 0f;
            }
            else
            {
                float num6 = 5f;
                switch (GM.Options.SimulationOptions.PlayerGravityMode)
                {
                    case SimulationOptions.GravityMode.Realistic:
                        num6 = 9.81f;
                        break;
                    case SimulationOptions.GravityMode.Playful:
                        num6 = 5f;
                        break;
                    case SimulationOptions.GravityMode.OnTheMoon:
                        num6 = 1.62f;
                        break;
                    case SimulationOptions.GravityMode.None:
                        num6 = 0.001f;
                        break;
                }
                if (!flag)
                {
                    ___m_smoothLocoVelocity.y = ___m_smoothLocoVelocity.y - num6 * Time.deltaTime;
                }
                else
                {
                    Vector3 a3 = Vector3.ProjectOnPlane(-Vector3.up * num6, planeNormal);
                    ___m_smoothLocoVelocity += a3 * Time.deltaTime;
                    ___m_smoothLocoVelocity = Vector3.ProjectOnPlane(___m_smoothLocoVelocity, planeNormal);
                }
            }
            float num7 = Mathf.Abs(lastNeckPos.y - GM.CurrentPlayerBody.transform.position.y);
            Vector3 point2 = lastNeckPos;
            Vector3 point3 = lastNeckPos;
            point2.y = Mathf.Min(point2.y, num3 - 0.01f);
            point3.y = Mathf.Max(__instance.transform.position.y, ___m_groundPoint.y) + (___m_armSwingerStepHeight + 0.2f);
            point2.y = Mathf.Max(point2.y, point3.y);
            Vector3 vector7 = ___m_smoothLocoVelocity;
            float maxLength = ___m_smoothLocoVelocity.magnitude * Time.deltaTime;
            if (Physics.CapsuleCast(point2, point3, 0.15f, ___m_smoothLocoVelocity, out ___m_hit_ray, ___m_smoothLocoVelocity.magnitude * Time.deltaTime + 0.1f, __instance.LM_TeleCast))
            {
                vector7 = Vector3.ProjectOnPlane(___m_smoothLocoVelocity, ___m_hit_ray.normal);
                maxLength = ___m_hit_ray.distance * 0.5f;
                if (___m_isGrounded)
                {
                    vector7.y = 0f;
                }
                RaycastHit raycastHit2;
                if (Physics.CapsuleCast(point2, point3, 0.15f, vector7, out raycastHit2, vector7.magnitude * Time.deltaTime + 0.1f, __instance.LM_TeleCast))
                {
                    maxLength = raycastHit2.distance * 0.5f;
                }
            }
            ___m_smoothLocoVelocity = vector7;
            if (___m_isGrounded)
            {
                ___m_smoothLocoVelocity.y = 0f;
            }
            Vector3 a4 = __instance.transform.position;
            Vector3 vector8 = ___m_smoothLocoVelocity * Time.deltaTime;
            vector8 = Vector3.ClampMagnitude(vector8, maxLength);
            a4 = __instance.transform.position + vector8;
            if (___m_isGrounded)
            {
                a4.y = Mathf.MoveTowards(a4.y, ___m_groundPoint.y, 8f * Time.deltaTime * Mathf.Abs(__instance.transform.position.y - ___m_groundPoint.y));
            }
            Vector3 a5 = ___CurNeckPos + vector8;
            a = a5 - ___LastNeckPos;
            if (Physics.SphereCast(___LastNeckPos, 0.15f, a.normalized, out raycastHit, a.magnitude, __instance.LM_TeleCast))
            {
                ___correctionDir = -a * 1f;
            }
            if (GM.Options.MovementOptions.AXButtonSnapTurnState == MovementOptions.AXButtonSnapTurnMode.Smoothturn)
            {
                for (int i = 0; i < __instance.Hands.Length; i++)
                {
                    if (!__instance.Hands[i].IsInStreamlinedMode)
                    {
                        if (__instance.Hands[i].IsThisTheRightHand)
                        {
                            if (__instance.Hands[i].Input.AXButtonPressed)
                            {
                                ___m_isTwinStickSmoothTurningClockwise = true;
                            }
                        }
                        else if (__instance.Hands[i].Input.AXButtonPressed)
                        {
                            ___m_isTwinStickSmoothTurningCounterClockwise = true;
                        }
                    }
                }
            }
            if (!___m_isTwinStickSmoothTurningClockwise && !___m_isTwinStickSmoothTurningCounterClockwise)
            {
                __instance.transform.position = a4 + ___correctionDir;
            }
            else
            {
                Vector3 vector9 = a4 + ___correctionDir;
                Vector3 vector10 = GM.CurrentPlayerBody.transform.forward;
                float num8 = GM.Options.MovementOptions.SmoothTurnMagnitudes[GM.Options.MovementOptions.SmoothTurnMagnitudeIndex] * Time.deltaTime;
                if (___m_isTwinStickSmoothTurningCounterClockwise)
                {
                    num8 = -num8;
                }
                vector9 = __instance.RotatePointAroundPivotWithEuler(vector9, ___CurNeckPos, new Vector3(0f, num8, 0f));
                vector10 = Quaternion.AngleAxis(num8, Vector3.up) * vector10;
                __instance.transform.SetPositionAndRotation(vector9, Quaternion.LookRotation(vector10, Vector3.up));
            }
            typeof(FVRMovementManager).GetMethod("SetTopSpeedLastSecond", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { ___m_smoothLocoVelocity });
            //__instance.SetTopSpeedLastSecond(___m_smoothLocoVelocity);
            if (!___IsGrabHolding)
            {
                typeof(FVRMovementManager).GetMethod("SetFrameSpeed", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { ___m_smoothLocoVelocity });
                //__instance.SetFrameSpeed(___m_smoothLocoVelocity);
            }
            ___LastNeckPos = GM.CurrentPlayerBody.NeckJointTransform.position;
            return false;
        }
    }

    class IsPointInsideSphereGeoPatch
    {
        static bool Prefix(ref FVRQuickBeltSlot __instance, ref bool __result, Vector3 p)
        {
            try
            {
                __result = __instance.HoverGeo.transform.InverseTransformPoint(p).magnitude < 0.5f;
            }
            catch (Exception e)
            {
                Mod.LogError("Exception in IsPointInsideSphereGeo called on " + __instance.name + ":\n" + e.StackTrace);
                __result = false;
            }
            return false;
        }
    }

    class SetParentagePatch
    {
        static void Prefix(ref FVRPhysicalObject __instance, Transform t)
        {
            Mod.LogInfo("SetParentage called on " + __instance.name + ", setting parent to " + (t == null ? "null" : t.name));
        }
    }

    class SetActivePatch
    {
        static void Prefix(ref GameObject __instance, bool value)
        {
            Mod.LogInfo("SetActive called on " + __instance.name + ", with bool: " + value + ":\n" + Environment.StackTrace);
        }
    }

    class DestroyPatch
    {
        static void Prefix(UnityEngine.Object obj)
        {
            if (obj != null)
            {
                Mod.LogInfo("Destroy called on " + obj.name + ", stack:\n " + Environment.StackTrace);
            }
        }
    }

    class PlayClipDebugPatch
    {
        //AudioSourcePool public instance
        /*public FVRPooledAudioSource PlayClip(AudioEvent clipSet, Vector3 pos, AudioMixerGroup mixerOverride = null)
			{
				if (clipSet.Clips.Count <= 0)
				{
					return null;
				}
				if (this.SourceQueue_Disabled.Count > 0)
				{
					return this.DequeueAndPlay(clipSet, pos, clipSet.PitchRange, clipSet.VolumeRange, mixerOverride);
				}
				if (this.m_curSize < this.m_maxSize)
				{
					GameObject prefabForType = SM.GetPrefabForType(this.Type);
					this.InstantiateAndEnqueue(prefabForType, true);
					return this.DequeueAndPlay(clipSet, pos, clipSet.PitchRange, clipSet.VolumeRange, mixerOverride);
				}
				FVRPooledAudioSource fvrpooledAudioSource = this.ActiveSources[0];
				this.ActiveSources.RemoveAt(0);
				if (!fvrpooledAudioSource.gameObject.activeSelf)
				{
					fvrpooledAudioSource.gameObject.SetActive(true);
				}
				fvrpooledAudioSource.Play(clipSet, pos, clipSet.PitchRange, clipSet.VolumeRange, mixerOverride);
				this.ActiveSources.Add(fvrpooledAudioSource);
				return fvrpooledAudioSource;
			}*/
        static bool Prefix(AudioEvent clipSet, Vector3 pos, AudioMixerGroup mixerOverride, ref SM.AudioSourcePool __instance, ref FVRPooledAudioSource __result)
        {
            Mod.LogInfo("PlayClip debug prefix called on AudioSourcePool with type: " + __instance.Type + " with SourceQueue_Disabled count: " + __instance.SourceQueue_Disabled.Count);
            MethodInfo dequeueAndPlayMethod = typeof(SM.AudioSourcePool).GetMethod("DequeueAndPlay", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo curSizeField = typeof(SM.AudioSourcePool).GetField("m_curSize", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo maxSizeField = typeof(SM.AudioSourcePool).GetField("m_maxSize", BindingFlags.NonPublic | BindingFlags.Instance);
            if (clipSet.Clips.Count <= 0)
            {
                __result = null;
                return false;
            }
            if (__instance.SourceQueue_Disabled.Count > 0)
            {
                Mod.LogInfo("Calling dequeue and play with SourceQueue_Disabled count: " + __instance.SourceQueue_Disabled.Count);
                __result = (FVRPooledAudioSource)dequeueAndPlayMethod.Invoke(__instance, new object[] { clipSet, pos, clipSet.PitchRange, clipSet.VolumeRange, mixerOverride });
                return false;
            }
            if ((int)curSizeField.GetValue(__instance) < (int)maxSizeField.GetValue(__instance))
            {
                GameObject prefabForType = SM.GetPrefabForType(__instance.Type);
                __instance.InstantiateAndEnqueue(prefabForType, true);
                Mod.LogInfo("Calling dequeue and play after with SourceQueue_Disabled count: " + __instance.SourceQueue_Disabled.Count);
                __result = (FVRPooledAudioSource)dequeueAndPlayMethod.Invoke(__instance, new object[] { clipSet, pos, clipSet.PitchRange, clipSet.VolumeRange, mixerOverride });
                return false;
            }
            FVRPooledAudioSource fvrpooledAudioSource = __instance.ActiveSources[0];
            __instance.ActiveSources.RemoveAt(0);
            if (!fvrpooledAudioSource.gameObject.activeSelf)
            {
                fvrpooledAudioSource.gameObject.SetActive(true);
            }
            fvrpooledAudioSource.Play(clipSet, pos, clipSet.PitchRange, clipSet.VolumeRange, mixerOverride);
            __instance.ActiveSources.Add(fvrpooledAudioSource);
            __result = fvrpooledAudioSource;
            return false;
        }
    }

    class InstantiateAndEnqueueDebugPatch
    {
        /*public void InstantiateAndEnqueue(GameObject prefab, bool active)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(prefab);
				FVRPooledAudioSource component = gameObject.GetComponent<FVRPooledAudioSource>();
				if (!active)
				{
					gameObject.SetActive(false);
				}
				this.SourceQueue_Disabled.Enqueue(component);
				this.m_curSize++;
			}*/

        static bool Prefix(GameObject prefab, bool active, ref SM.AudioSourcePool __instance)
        {
            GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(prefab);
            FVRPooledAudioSource component = gameObject.GetComponent<FVRPooledAudioSource>();
            if (!active)
            {
                gameObject.SetActive(false);
            }
            __instance.SourceQueue_Disabled.Enqueue(component);
            FieldInfo curSizeField = typeof(SM.AudioSourcePool).GetField("m_curSize", BindingFlags.NonPublic | BindingFlags.Instance);
            int curSizeVal = (int)curSizeField.GetValue(__instance);
            curSizeField.SetValue(__instance, curSizeVal + 1);
            return false;
        }
    }

    class ChamberFireDebugPatch
    {
        /*public bool Fire()
		    {
			    if (this.IsFull && this.m_round != null && !this.IsSpent)
			    {
				    this.IsSpent = true;
				    this.UpdateProxyDisplay();
				    return true;
			    }
			    return false;
		    }*/

        static bool Prefix(ref bool __result, ref FVRFireArmChamber __instance)
        {
            Mod.LogInfo("Chamber fire prefix called");
            FieldInfo m_roundField = typeof(FVRFireArmChamber).GetField("m_round", BindingFlags.NonPublic | BindingFlags.Instance);
            if (__instance.IsFull && m_roundField.GetValue(__instance) != null && !__instance.IsSpent)
            {
                Mod.LogInfo("\tFire successful");
                __instance.IsSpent = true;
                __instance.UpdateProxyDisplay();
                __result = true;
                return false;
            }
            Mod.LogInfo("\tFire unsuccessful");
            __result = false;
            return false;
        }
    }

    class DropHammerDebugPatch
    {
        /*public void DropHammer()
		{
			if (this.m_isHammerCocked)
			{
				this.m_isHammerCocked = false;
				base.PlayAudioEvent(FirearmAudioEventType.HammerHit, 1f);
				this.Fire();
			}
		}*/

        static bool Prefix(ref bool __result, ref FVRFireArmChamber __instance)
        {
            Mod.LogInfo("Chamber fire prefix called");
            FieldInfo m_roundField = typeof(FVRFireArmChamber).GetField("m_round", BindingFlags.NonPublic | BindingFlags.Instance);
            if (__instance.IsFull && m_roundField.GetValue(__instance) != null && !__instance.IsSpent)
            {
                Mod.LogInfo("\tFire successful");
                __instance.IsSpent = true;
                __instance.UpdateProxyDisplay();
                __result = true;
                return false;
            }
            Mod.LogInfo("\tFire unsuccessful");
            __result = false;
            return false;
        }
    }
    #endregion
}

using BepInEx.Bootstrap;
using BepInEx;
using HarmonyLib.Public.Patching;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using Valve.Newtonsoft.Json.Linq;
using System.IO;

namespace EFM
{
    public class PatchController
    {
        // Patch verification stuff
        static Type ILManipulatorType;
        static MethodInfo getInstructionsMethod;

        public static Dictionary<string, int> hashes;
        public static bool writeWhenDone;
        public static int breakingPatchVerify = 0;
        public static int warningPatchVerify = 0;

        // Verifies patch integrity by comparing original method's hash with stored hash
        public static void Verify(MethodInfo methodInfo, Harmony harmony, bool breaking)
        {
            if (hashes == null)
            {
                if (File.Exists(Mod.path + "/PatchHashes.json"))
                {
                    hashes = JObject.Parse(File.ReadAllText(Mod.path + "/PatchHashes.json")).ToObject<Dictionary<string, int>>();
                }
                else
                {
                    hashes = new Dictionary<string, int>();
                    writeWhenDone = true;
                }
            }

            if (ILManipulatorType == null)
            {
                ILManipulatorType = typeof(HarmonyManipulator).Assembly.GetType("HarmonyLib.Internal.Patching.ILManipulator");
                getInstructionsMethod = ILManipulatorType.GetMethod("GetInstructions", BindingFlags.Public | BindingFlags.Instance);
            }

            string identifier = methodInfo.DeclaringType.Name + "." + methodInfo.Name + GetParamArrHash(methodInfo.GetParameters()).ToString();

            // Get IL instructions of the method
            ILGenerator generator = PatchProcessor.CreateILGenerator(methodInfo);
            Mono.Cecil.Cil.MethodBody bodyCopy = PatchManager.GetMethodPatcher(methodInfo).CopyOriginal().Definition.Body;
            object ilManipulator = Activator.CreateInstance(ILManipulatorType, bodyCopy, false);
            object[] paramArr = new object[] { generator, null };
            List<CodeInstruction> instructions = (List<CodeInstruction>)getInstructionsMethod.Invoke(ilManipulator, paramArr);

            // Build hash from all instructions
            string s = "";
            for (int i = 0; i < instructions.Count; ++i)
            {
                CodeInstruction instruction = instructions[i];
                OpCode oc = instruction.opcode;
                if (oc == null)
                {
                    s += "null opcode" + (instruction.operand == null ? "null operand" : instruction.operand.ToString());
                }
                else
                {
                    // This is done because the code changes if a mod is loaded using MonoMod loader? Some calls become virtual
                    s += (oc == OpCodes.Call || oc == OpCodes.Callvirt ? "c" : oc.ToString()) + (instruction.operand == null ? "null operand" : instruction.operand.ToString());
                }
            }
            int hash = s.GetHashCode();

            // Verify hash
            if (hashes.TryGetValue(identifier, out int originalHash))
            {
                if (originalHash != hash)
                {
                    if (breaking)
                    {
#if DEBUG
                        Mod.LogError("PatchVerify: " + identifier + " failed patch verify, this will most probably break H3MP! Update the mod.\nOriginal hash: " + originalHash + ", new hash: " + hash);
#endif
                        ++breakingPatchVerify;
                    }
                    else
                    {
#if DEBUG
                        Mod.LogWarning("PatchVerify: " + identifier + " failed patch verify, this will most probably break some part of H3MP. Update the mod.\nOriginal hash: " + originalHash + ", new hash: " + hash);
#endif
                        ++warningPatchVerify;
                    }

                    hashes[identifier] = hash;
                }
            }
            else
            {
                hashes.Add(identifier, hash);
                if (!writeWhenDone)
                {
#if DEBUG
                    Mod.LogWarning("PatchVerify: " + identifier + " not found in hashes. Most probably a new patch. This warning will remain until new hash file is written.");
#endif
                }
            }
        }

        private static int GetParamArrHash(ParameterInfo[] paramArr)
        {
            int hash = 0;
            foreach (ParameterInfo t in paramArr)
            {
                hash += t.ParameterType.Name.GetHashCode();
            }
            return hash;
        }

        public static void DoPatching()
        {
            Harmony harmony = new Harmony("VIP.TommySoucy.EscapeFromMeatov");

            GamePatches.DoPatching(harmony);
            DebugPatches.DoPatching(harmony);

            ProcessPatchResult();
        }

        private static void ProcessPatchResult()
        {
            if (writeWhenDone)
            {
                File.WriteAllText(Mod.path + "/PatchHashes.json", JObject.FromObject(hashes).ToString());
            }

            if (breakingPatchVerify > 0)
            {
                Mod.LogError("PatchVerify report: " + breakingPatchVerify + " breaking, " + warningPatchVerify + " warnings.\nIf you have other mods installed this may be normal. Refer to H3MP mod compatibility list in case things break.");
            }
            else if (warningPatchVerify > 0)
            {
                Mod.LogWarning("PatchVerify report: 0 breaking, " + warningPatchVerify + " warnings.\nIf you have other mods installed this may be normal. Refer to H3MP mod compatibility list in case things break.");
            }
        }

        // This is a copy of HarmonyX's AccessTools extension method EnumeratorMoveNext (i think)
        // Gets MoveNext() of a Coroutine
        public static MethodInfo EnumeratorMoveNext(MethodBase method)
        {
            if (method is null)
            {
                return null;
            }

            var codes = PatchProcessor.ReadMethodBody(method).Where(pair => pair.Key == OpCodes.Newobj);
            if (codes.Count() != 1)
            {
                return null;
            }
            var ctor = codes.First().Value as ConstructorInfo;
            if (ctor == null)
            {
                return null;
            }
            var type = ctor.DeclaringType;
            if (type == null)
            {
                return null;
            }
            return AccessTools.Method(type, nameof(IEnumerator.MoveNext));
        }
    }
}

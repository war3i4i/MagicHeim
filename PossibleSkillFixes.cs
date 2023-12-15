using System.Reflection.Emit;
using JetBrains.Annotations;
using MagicHeim.UI_s;

namespace MagicHeim;

public class PossibleSkillFixes
{
    [HarmonyPatch(typeof(Player), nameof(Player.SetControls))]
    static class Player_TakeInput_Patch
    {
        static void Prefix(ref bool attack, ref bool attackHold, ref bool secondaryAttack,
            ref bool secondaryAttackHold, ref bool block, ref bool blockHold, ref bool jump)
        {
            if (SkillChargeUI.IsCharging)
            {
                attack = false;
                attackHold = false;
                secondaryAttack = false;
                secondaryAttackHold = false;
                block = false;
                blockHold = false; 
                jump = false;
            }
        }
    } 


    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    static class AltHotkey
    {
        public static bool CheckKeyDown(bool activeKey)
        {
            return activeKey && !Input.GetKey(KeyCode.LeftAlt);
        }

        private static readonly MethodInfo CheckInputKey =
            AccessTools.DeclaredMethod(typeof(AltHotkey), nameof(CheckKeyDown));

        private static readonly MethodInfo InputKey =
            AccessTools.DeclaredMethod(typeof(Input), nameof(Input.GetKeyDown), new[] { typeof(KeyCode) });

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                yield return instruction;
                if (instruction.opcode == System.Reflection.Emit.OpCodes.Call && instruction.OperandIs(InputKey))
                    yield return new CodeInstruction(System.Reflection.Emit.OpCodes.Call, CheckInputKey);
            }
        }
    }


    [HarmonyPatch(typeof(Character), nameof(Character.CustomFixedUpdate))]
    private static class MageCancelTP
    {
        private static void Postfix(Character __instance)
        {
            if (__instance == Player.m_localPlayer && __instance.m_nview.m_zdo.GetBool("MH_HideCharacter"))
            {
                __instance.m_body.useGravity = false;
                __instance.m_body.velocity = Vector3.zero;
                __instance.m_currentVel = Vector3.zero;
                __instance.m_body.angularVelocity = Vector3.zero;
            }
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.IsTeleportable))]
    private static class TeleportDebugFix
    {
        private static void Postfix(Humanoid __instance, ref bool __result)
        {
            if (__instance != Player.m_localPlayer) return;
            __result = __result && !__instance.m_nview.m_zdo.GetBool("MH_HideCharacter")
                                && !__instance.m_nview.m_zdo.GetBool("MH_Druid_FishForm")
                                && !__instance.m_nview.m_zdo.GetBool("MH_Druid_WolfForm");
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.TeleportTo))]
    private static class TeleportDebugFix2
    {
        private static bool Prefix(Player __instance)
        {
            if (__instance != Player.m_localPlayer) return true;
            return !__instance.m_nview.m_zdo.GetBool("MH_HideCharacter") &&
                   !__instance.m_nview.m_zdo.GetBool("MH_Druid_FishForm") &&
                   !__instance.m_nview.m_zdo.GetBool("MH_Druid_WolfForm");
        }
    }

    [HarmonyPatch(typeof(FootStep), nameof(FootStep.UpdateFootstep))]
    private static class WalkCancelRaven2
    {
        private static bool Prefix(FootStep __instance)
        {
            if (__instance.m_character != Player.m_localPlayer) return true;
            if (__instance.m_nview.m_zdo.GetBool("MH_HideCharacter")) return false;
            return true;
        }
    }

    private static readonly int Wakeup = Animator.StringToHash("wakeup");

    private static IEnumerator DelayInvokeSMR(Player p)
    {
        yield return new WaitForEndOfFrame();
        if (p)
        {
            p.m_visual.SetActive(false);
            p.m_animator.SetBool(Wakeup, false);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
    private static class Player_ModelHide
    {
        private static void Postfix(Player __instance)
        {
            __instance.m_nview.Register("MH_HideCharacter", (long _, bool tf) =>
            {
                tf = !tf;
                __instance.m_visual.SetActive(tf);
                __instance.m_animator.SetBool(Wakeup, false);
            });

            if (__instance.m_nview?.m_zdo?.GetBool("MH_HideCharacter") == true)
                MagicHeim._thistype.StartCoroutine(DelayInvokeSMR(__instance));
        }
    }


    [HarmonyPatch(typeof(CharacterAnimEvent), nameof(CharacterAnimEvent.Awake))]
    private static class Fix1
    {
        private static bool Prefix(CharacterAnimEvent __instance)
        {
            if (__instance.GetComponentInParent<Character>() != null) return true;
            UnityEngine.Object.Destroy(__instance);
            return false;
        }
    }

    [HarmonyPatch(typeof(LevelEffects), nameof(LevelEffects.Start))]
    private static class Fix2
    {
        private static bool Prefix(LevelEffects __instance)
        {
            if (__instance.GetComponentInParent<Character>() == null)
            {
                UnityEngine.Object.Destroy(__instance);
                return false;
            }

            return true;
        }
    }
    
    [HarmonyPatch(typeof(Player), nameof(Player.UseHotbarItem))]
    static class Player_UseHotbarItem_Patch
    {
        static bool Prefix()
        {
            return !Input.GetKey(KeyCode.LeftAlt);
        }
    }
    
    [HarmonyPatch(typeof(Settings),nameof(Settings.ApplyQualitySettings))]
    private static class Settings_ApplyQualitySettings_Patch
    {
        [UsedImplicitly]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)
        {
            CodeMatcher matcher = new(code);
            MethodInfo target = AccessTools.Method(typeof(Shader), nameof(Shader.EnableKeyword), new[]{typeof(string)});
            MethodInfo replaceDisable = AccessTools.Method(typeof(Shader), nameof(Shader.DisableKeyword), new[]{typeof(string)});
            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldstr, "TESSELATION_ON"), new CodeMatch(OpCodes.Call, target));
            if (matcher.IsInvalid) return matcher.Instructions();
            matcher.Advance(1);
            matcher.Set(OpCodes.Call, replaceDisable);
            return matcher.Instructions();
        }
    }
}
using MagicHeim.MH_Enums;

namespace MagicHeim.KillHooks
{
    public static class KillHooks
    {
        private static readonly Dictionary<Character, long> CharacterLastDamageList = new();

        [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
        private static class QuestEnemyKill
        {
            private static void Prefix(Character __instance, long sender, HitData hit)
            {
                if (__instance.GetHealth() <= 0) return;
                var attacker = hit.GetAttacker();
                if (attacker)
                {
                    if (attacker.IsPlayer())
                    {
                        CharacterLastDamageList[__instance] = sender;
                    }
                    else
                    {
                        if (!attacker.IsTamed())
                        {
                            CharacterLastDamageList[__instance] = 100;
                        }
                    }
                }
            }

            private static void Postfix(Character __instance)
            {
                if (__instance.GetHealth() <= 0f && CharacterLastDamageList.ContainsKey(__instance))
                {
                    ZRoutedRpc.instance.InvokeRoutedRPC(CharacterLastDamageList[__instance], "MH KillHook",
                        global::Utils.GetPrefabName(__instance.gameObject), __instance.GetLevel());
                    CharacterLastDamageList.Remove(__instance);
                }
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.ApplyDamage))]
        private static class Character_ApplyDamage_Patch
        {
            private static void Postfix(Character __instance)
            {
                if (__instance.GetHealth() <= 0f && CharacterLastDamageList.ContainsKey(__instance))
                {
                    ZRoutedRpc.instance.InvokeRoutedRPC(CharacterLastDamageList[__instance], "MH KillHook",
                        global::Utils.GetPrefabName(__instance.gameObject), __instance.GetLevel());
                    CharacterLastDamageList.Remove(__instance);
                }
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.OnDestroy))]
        private static class Character_OnDestroy_Patch
        {
            private static void Postfix(Character __instance)
            {
                if (CharacterLastDamageList.ContainsKey(__instance)) CharacterLastDamageList.Remove(__instance);
            }
        }


        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
        private static class ZNetScene_Awake_Patch_QuestsInit
        {
            private static void Postfix()
            {
                if (!ZNet.instance.IsDedicated())
                {
                    ZRoutedRpc.instance.Register("MH KillHook", new Action<long, string, int>(OnCharacterKill));
                    ZRoutedRpc.instance.Register("MH PartyAddEXP", new Action<long, ZPackage>(OnExpGain));
                }
            }

            private static void OnExpGain(long sender, ZPackage pkg)
            {
                if(ClassManager.CurrentClass == Class.None) return;
                int exp = pkg.ReadInt();
                Vector3 pos = pkg.ReadVector3();
                if (!Player.m_localPlayer) return;
                if (Vector3.Distance(Player.m_localPlayer.transform.position, pos) > 50f) return;
                ClassManager.AddExp(exp);
            }

            private static void OnCharacterKill(long sender, string prefab, int level)
            {
                ClassManager.OnCharacterKill?.Invoke(prefab, level);
            }
        }
    }
}
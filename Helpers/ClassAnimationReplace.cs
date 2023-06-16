namespace MagicHeim.AnimationHelpers;

public static class ClassAnimationReplace
{
    private static bool FirstInit;
    private static RuntimeAnimatorController VanillaController;
    private static RuntimeAnimatorController MH_Controller;
    private static readonly Dictionary<string, AnimationClip> ExternalAnimations = new();
    private static readonly Dictionary<string, string> ReplacementMap = new();

    public static RuntimeAnimatorController MH_WolfController;

    public enum MH_Animation
    {
        MageSummon,
        MageSlam,
        MageProjectile,
        MageWave,
        TwoHandedProjectile,
        TwoHandedSummon,
        TwoHandedTransform
    }

    public static readonly Dictionary<MH_Animation, string> MH_AnimationNames = new()
    {
        { MH_Animation.MageSummon, "emote_cheer" },
        { MH_Animation.MageSlam, "emote_nonono" },
        { MH_Animation.MageProjectile, "emote_thumbsup" },
        { MH_Animation.MageWave, "emote_wave" },
        { MH_Animation.TwoHandedProjectile, "emote_comehere" },
        { MH_Animation.TwoHandedSummon, "emote_flex" },
        { MH_Animation.TwoHandedTransform, "emote_despair" },
    };

    public static void InitAnimations()
    {
        ExternalAnimations.Add("MageSlam", MagicHeim.asset.LoadAsset<AnimationClip>("MageSlam"));
        ExternalAnimations.Add("MageProjectile", MagicHeim.asset.LoadAsset<AnimationClip>("MageProjectileEdited"));
        ExternalAnimations.Add("MageWave", MagicHeim.asset.LoadAsset<AnimationClip>("MageWave"));
        ExternalAnimations.Add("MageSummon", MagicHeim.asset.LoadAsset<AnimationClip>("MageSummon"));
        ExternalAnimations.Add("NewJump", MagicHeim.asset.LoadAsset<AnimationClip>("Jump_External"));
        ExternalAnimations.Add("MH_Wolf_Jump", MagicHeim.asset.LoadAsset<AnimationClip>("MH_Wolf_Jump"));
        ExternalAnimations.Add("ProjectileTwoHanded", MagicHeim.asset.LoadAsset<AnimationClip>("TwoHandedCast"));
        ExternalAnimations.Add("SummonTwoHanded", MagicHeim.asset.LoadAsset<AnimationClip>("TwoHandedCast2"));
        ExternalAnimations.Add("TransformTwoHanded",
            MagicHeim.asset.LoadAsset<AnimationClip>("TwoHandedCastTransform"));
        ReplacementMap.Add("Cheer", "MageSummon");
        ReplacementMap.Add("No no no", "MageSlam");
        ReplacementMap.Add("Thumbsup", "MageProjectile");
        ReplacementMap.Add("Wave", "MageWave");
        ReplacementMap.Add("GetOverHere", "ProjectileTwoHanded");
        ReplacementMap.Add("Flex", "SummonTwoHanded");
        ReplacementMap.Add("Despair", "TransformTwoHanded");
    }

    private static void ReplacePlayerRAC(Animator anim, RuntimeAnimatorController rac)
    {
        if (anim.runtimeAnimatorController == rac) return;
        anim.runtimeAnimatorController = rac;
        anim.Update(0f);
    }


    public static RuntimeAnimatorController MakeAOC(Dictionary<string, string> replacement,
        RuntimeAnimatorController ORIGINAL)
    {
        var aoc = new AnimatorOverrideController(ORIGINAL);
        var anims = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        foreach (var animation in aoc.animationClips)
        {
            string name = animation.name;
            if (replacement.ContainsKey(name))
            {
                var newClip = UnityEngine.Object.Instantiate(ExternalAnimations[replacement[name]]);
                anims.Add(new KeyValuePair<AnimationClip, AnimationClip>(animation, newClip));
            }
            else
            {
                anims.Add(new KeyValuePair<AnimationClip, AnimationClip>(animation, animation));
            }
        }

        aoc.ApplyOverrides(anims);
        return aoc;
    }

    [HarmonyPatch(typeof(Player), nameof(Player.Start))]
    [HarmonyPriority(5000)]
    private static class TESTPATCHPLAYERANIMS
    {
        private static void Postfix(ref Player __instance)
        {
            if (!FirstInit)
            {
                FirstInit = true;
                MH_Controller = MakeAOC(ReplacementMap, __instance.m_animator.runtimeAnimatorController);
                VanillaController = MakeAOC(new(), __instance.m_animator.runtimeAnimatorController);
            }
        }
    }

    [HarmonyPatch(typeof(ZSyncAnimation), nameof(ZSyncAnimation.RPC_SetTrigger))]
    static class ZSyncAnimation_RPC_SetTrigger_Patch
    {
        static void Prefix(ZSyncAnimation __instance, string name)
        {
            if (name.Contains("emote_")) ReplacePlayerRAC(__instance.m_animator, MH_Controller);
        }
    }

    /*[HarmonyPatch(typeof(Player), nameof(Player.StartEmote))] 
    private static class DISABLEALLEMOTES
    { 
        private static bool Prefix(string emote)
        {
            return emote == "sit";
        }
    }*/


    //wolf animator

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    static class ZNetScene_Awake_Patch
    {
        static void Postfix(ZNetScene __instance)
        {
            var animation = MagicHeim.asset.LoadAsset<AnimationClip>("MH_Wolf_Jump");
            var wolf = __instance.GetPrefab("Wolf");
            Dictionary<string, string> test = new() { { "Jump", "MH_Wolf_Jump" } };
            MH_WolfController = MakeAOC(test, wolf.GetComponentInChildren<Animator>().runtimeAnimatorController);
        }
    }
}
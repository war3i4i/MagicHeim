using MagicHeim.MH_Interfaces;
using MagicHeim.SkillsDatabase.DruidSkills;
using MagicHeim.UI_s;

namespace MagicHeim.AnimationHelpers;

public class SkillCastHelper
{
    private static SkillCastHelper _instance;

    private static SkillCastHelper instance
    {
        get { return _instance ??= new SkillCastHelper(); }
    }

    private static bool IsInDelayedInvoke;

    private IEnumerator DelayedInvoke(MH_Skill skill, Func<bool> cond)
    {
        if (skill.Toggled)
        {
            skill.Execute(cond);
            IsInDelayedInvoke = false;
            yield break;
        }

        float time = skill.AnimationTime / 1.5f;
        PlayAnimation(skill);
        if (time > 0)
        {
            IsInDelayedInvoke = true;
            while (time > 0)
            {
                time -= Time.deltaTime;
                yield return null;
            }
        }

        if (Player.m_localPlayer && !Player.m_localPlayer.IsDead())
            skill.Execute(cond);
        IsInDelayedInvoke = false;
    }


    public static void CastSkill(MH_Skill skill, Func<bool> cond = null, bool skipInput = false)
    {
        if (IsInDelayedInvoke || skill.GetCooldown() > 0 || !skill.CanExecute() || SkillChargeUI.IsCharging || (!Player.m_localPlayer.TakeInput() && !skipInput)) return;
        if (skill.Toggled || (DEBUG_PreventUsages(skill) && skill.TryUseCost()))
            MagicHeim._thistype.StartCoroutine(instance.DelayedInvoke(skill, cond));
    }

    public static void PlayAnimation(MH_Skill skill)
    {
        if (skill.Animation != null) Player.m_localPlayer.m_zanim.SetTrigger(skill.Animation);
    }


    private static bool DEBUG_PreventUsages(MH_Skill skill)
    {
        if (!Player.m_localPlayer) return false;
        if (Player.m_localPlayer.IsTeleporting())
        {
            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, $"Can't use skills while teleporting");
            return false;
        }

        if (Player.m_localPlayer.IsAttached())
        {
            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, $"Can't use skills while attached");
            return false;
        }

        if (ClassManager.CurrentClassDef is { } cl)
        {
            if (cl.GetSkill(Druid_Eagle.CachedKey) is { Toggled: true })
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, $"Can't use skills in Eagle form");
                return false;
            }

            if (cl.GetSkill(Druid_Fish.CachedKey) is { Toggled: true })
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, $"Can't use skills in Fish form");
                return false;
            }

            if (cl.GetSkill(Druid_Wolf.CachedKey) is { Toggled: true })
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, $"Can't use skills in Wolf form");
                return false;
            }
        }

        return API.API.CanUseAbilities();
    }
}
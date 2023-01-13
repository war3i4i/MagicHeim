using MagicHeim.ClassesDatabase.Classes;
using MagicHeim.MH_Interfaces;
using MagicHeim.SkillsDatabase.DruidSkills;
using MagicHeim.SkillsDatabase.MageSkills;

namespace MagicHeim.SkillsDatabase;

public static class SkillsDatabase
{
    private static readonly Dictionary<int, MH_Skill> GLOBAL_SkillDefinitions = new();

    public static bool TryGetSkillDefinition(string skillID, out MH_Skill skill)
    {
        if (GLOBAL_SkillDefinitions.TryGetValue(skillID.GetStableHashCode(), out skill))
        {
            return true;
        }

        MagicHeim_Logger.Logger.Log($"Skill with ID {skillID} not found!");
        return false;
    }

    public static List<MH_Skill> GetAllSkill()
    {
        return GLOBAL_SkillDefinitions.Values.ToList();
    }

    private static void AddSkill(MH_Skill skill)
    {
        GLOBAL_SkillDefinitions.Add(skill.Key, skill);
    }

    public static void Init()
    {
        AddSkill(new Mage_EnergyBlast());
        AddSkill(new Mage_Teleport());
        AddSkill(new Mage_ManaFlow());
        AddSkill(new Mage_ElementalKnowledge());
        AddSkill(new Mage_EitrControl());
        AddSkill(new Mage_ElementalResistance());
        AddSkill(new Mage_MasterOfTime());
        AddSkill(new Mage_EitrEconomy());
        AddSkill(new Mage_ElementalTimescale());
        AddSkill(new Mage_ElementalVampirism());
        AddSkill(new Mage_BluntMastery());
        AddSkill(new Mage_ElementalMastery());
        AddSkill(new Mage_ThunderWrath());
        AddSkill(new Mage_ThunderShock());
        AddSkill(new Mage_FrostBeam());
        AddSkill(new Mage_Frostball());
        AddSkill(new Mage_Fireball());
        AddSkill(new Mage_Lightningball());
        AddSkill(new Mage_WaterWalk());
        AddSkill(new Mage_IceWall());
        AddSkill(new Mage_IceShield());
        AddSkill(new Mage_EitrSphere());
        AddSkill(new Mage_RandomStrike());
        AddSkill(new Mage_ArcaneSpikes());
        AddSkill(new Mage_BlackHole());
        AddSkill(new Mage_Portal());
        AddSkill(new Mage_WeaponEnchantFire());
        AddSkill(new Mage_WeaponEnchantLightning());
        AddSkill(new Mage_WeaponEnchantFrost());
        AddSkill(new Mage_Meteor());
        AddSkill(new Mage_FireShield());
        AddSkill(new Mage_EnergyStorm());
        AddSkill(new Mage_WaveOfFlame());
        AddSkill(new Mage_ArcaneShield());
        AddSkill(new Mage_AncientTotem());

        AddSkill(new Druid_Eagle());
        AddSkill(new Druid_Wolf());
        AddSkill(new Druid_Fish());
        AddSkill(new Druid_ManaFlow());
        AddSkill(new Druid_Heal());
        AddSkill(new Druid_NatureBuff());
        AddSkill(new Druid_Rootball());
        AddSkill(new Druid_Moonfire());
    }
}
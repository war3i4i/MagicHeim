using MagicHeim.MH_Interfaces;

namespace MagicHeim.ClassesDatabase.Classes;

public class Druid : MH_ClassDefinition
{
    public Druid(string name, string description) : base(name, description)
    {
        OnSelect_VFX = MagicHeim.asset.LoadAsset<GameObject>("Druid_Select");
    }

    public override void Init()
    {
        AddSkill("Druid_Eagle");
        AddSkill("Druid_Wolf");
        AddSkill("Druid_Fish");
        AddSkill("Druid_Manaflow");
        AddSkill("Druid_Heal"); 
        AddSkill("Druid_NatureBuff");  
        AddSkill("Druid_Rootball"); 
        AddSkill("Druid_Spiritwave"); 
        AddSkill("Druid_Moonfire");  
        AddSkill("Druid_Weaponenchantpoison");  
        AddSkill("Druid_Weaponenchantspirit");
        AddSkill("Druid_Natureprotection"); 
        AddSkill("Druid_Shield"); 
        AddSkill("Druid_TrollPower");
        AddSkill("Druid_EikthyrPower");
        AddSkill("Druid_ElderPower"); 
        AddSkill("Druid_Exchange"); 
        AddSkill("Druid_Eclipse");  
        AddSkill("Druid_Grenade"); 
        AddSkill("Druid_SelfHeal"); 
        AddSkill("Druid_Connection"); 
        AddSkill("Druid_Tame"); 
        AddSkill("Druid_CreatuesBuff"); 
        AddSkill("Druid_StaminaSphere"); 
        AddSkill("Druid_Crystals"); 
        AddSkill("Druid_AspeedBuff"); 
        AddSkill("Druid_HardenSkin"); 
        AddSkill("Druid_FoodKnowledge_Health"); 
        AddSkill("Druid_FoodKnowledge_Stamina"); 
        AddSkill("Druid_FoodKnowledge_Eitr"); 
        AddSkill("Druid_BodyOfPoison"); 
        AddSkill("Druid_InnerControl");
        AddSkill("Druid_BluntMastery"); 
        AddSkill("Druid_PoisonTouch"); 
        AddSkill("Druid_HealingBonus"); 
        SortSkills();
    }
 

    protected internal override Color GetColor => Color.green;
}
using System.Text;
using JetBrains.Annotations;
using MagicHeim.MH_Enums;
using MagicHeim.MH_Interfaces;

namespace MagicHeim.SkillsDatabase.DruidSkills;

public sealed class Druid_FoodKnowledge_Health : MH_Skill
{
    public Druid_FoodKnowledge_Health()
    {
        _definition._InternalName = "Druid_FoodKnowledge_Health";
        _definition.Name = "$mh_druid_foodknowledge_health";
        _definition.Description = "$mh_druid_foodknowledge_health_desc";

        _definition.MinLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Percent", 5f,
            "Percentage (Min Lvl)");
        _definition.MaxLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Percent", 100f,
            "Percentage (Max Lvl)");
        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            "Max Level", 10,
            "Max Skill Level");
        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            "Required Level To Learn", 
            1, "Required Level");
        
        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Druid_FoodKnowledge_Health_Icon");
        CachedKey = _definition.Key;

        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            "Leveling Step", 1,
            "Leveling Step"); 

        this.InitRequiredItemFirstHalf("Wood", 10, 1.88f);
        this.InitRequiredItemSecondHalf("Coins", 10, 1.88f);
        this.InitRequiredItemFinal("MH_Tome_Mistlands", 3);
    } 

    public static int CachedKey;

    public override void Execute(Func<bool> Cond){}
 
    public override bool CanExecute()
    {
        return false;
    }

    public override string GetSpecialTags()
    {
        return "<color=red>Passive, Food Health Increase</color>";
    }

    public override string BuildDescription()
    {
        StringBuilder builder = new();
        builder.AppendLine(Localization.instance.Localize(Description));
        builder.AppendLine("\n");

        int maxLevel = MaxLevel;
        int forLevel = Level > 0 ? Level : 1;
        float currentValue = this.CalculateSkillValue(forLevel);
        builder.AppendLine($"Food health bonus: {Math.Round(currentValue, 1)}%");
        if (Level < maxLevel && Level > 0)
        {
            float nextValue = this.CalculateSkillValue(forLevel + 1);
            float valueDiff = nextValue - currentValue;
 
            double roundedValueDiff = Math.Round(valueDiff, 1);

            builder.AppendLine("\nNext Level:");
            builder.AppendLine($"Food health bonus: {Math.Round(nextValue, 1)}% <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color>");
        }
        return builder.ToString();
    }
    

    public override bool CanRightClickCast => false;
    public override bool IsPassive => true;
    public override CostType _costType => CostType.None;
    public override Color SkillColor => new Color(1f, 0.18f, 0.28f);
}

public sealed class Druid_FoodKnowledge_Stamina : MH_Skill
{
    public Druid_FoodKnowledge_Stamina()
    {
        _definition._InternalName = "Druid_FoodKnowledge_Stamina";
        _definition.Name = "$mh_druid_foodknowledge_stamina";
        _definition.Description = "$mh_druid_foodknowledge_stamina_desc";

        _definition.MinLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Percent", 5f,
            "Percentage (Min Lvl)");
        _definition.MaxLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Percent", 100f,
            "Percentage (Max Lvl)");
        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            "Max Level", 10,
            "Max Skill Level");
        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            "Required Level To Learn",
            1, "Required Level");
        
        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Druid_FoodKnowledge_Stamina_Icon");
        CachedKey = _definition.Key;

        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            "Leveling Step", 1,
            "Leveling Step"); 

        this.InitRequiredItemFirstHalf("Wood", 10, 1.88f);
        this.InitRequiredItemSecondHalf("Coins", 10, 1.88f);
        this.InitRequiredItemFinal("MH_Tome_Mistlands", 3);
    } 

    public static int CachedKey;

    public override void Execute(Func<bool> Cond){}
 
    public override bool CanExecute()
    {
        return false;
    }

    public override string GetSpecialTags()
    {
        return "<color=red>Passive, Food Stamina Increase</color>";
    }

    public override string BuildDescription()
    {
        StringBuilder builder = new();
        builder.AppendLine(Localization.instance.Localize(Description));
        builder.AppendLine("\n");

        int maxLevel = MaxLevel;
        int forLevel = Level > 0 ? Level : 1;
        float currentValue = this.CalculateSkillValue(forLevel);
        builder.AppendLine($"Food stamina bonus: {Math.Round(currentValue, 1)}%");
        if (Level < maxLevel && Level > 0)
        {
            float nextValue = this.CalculateSkillValue(forLevel + 1);
            float valueDiff = nextValue - currentValue;
 
            double roundedValueDiff = Math.Round(valueDiff, 1);

            builder.AppendLine("\nNext Level:");
            builder.AppendLine($"Food stamina bonus: {Math.Round(nextValue, 1)}% <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color>");
        }
        return builder.ToString();
    }
    

    public override bool CanRightClickCast => false;
    public override bool IsPassive => true;
    public override CostType _costType => CostType.None;
    public override Color SkillColor => new Color(1f, 0.86f, 0.33f);
}

public sealed class Druid_FoodKnowledge_Eitr : MH_Skill
{
    public Druid_FoodKnowledge_Eitr()
    {
        _definition._InternalName = "Druid_FoodKnowledge_Eitr";
        _definition.Name = "$mh_druid_foodknowledge_eitr";
        _definition.Description = "$mh_druid_foodknowledge_eitr_desc";

        _definition.MinLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Percent", 5f,
            "Percentage (Min Lvl)");
        _definition.MaxLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Percent", 100f,
            "Percentage (Max Lvl)");
        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            "Max Level", 10,
            "Max Skill Level");
        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            "Required Level To Learn",
            1, "Required Level");
        
        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Druid_FoodKnowledge_Eitr_Icon");
        CachedKey = _definition.Key;

        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            "Leveling Step", 1,
            "Leveling Step"); 

        this.InitRequiredItemFirstHalf("Wood", 10, 1.88f);
        this.InitRequiredItemSecondHalf("Coins", 10, 1.88f);
        this.InitRequiredItemFinal("MH_Tome_Mistlands", 3);
    } 

    public static int CachedKey;

    public override void Execute(Func<bool> Cond){}
 
    public override bool CanExecute()
    {
        return false;
    }

    public override string GetSpecialTags()
    {
        return "<color=red>Passive, Food Eitr Increase</color>";
    }

    public override string BuildDescription()
    {
        StringBuilder builder = new();
        builder.AppendLine(Localization.instance.Localize(Description));
        builder.AppendLine("\n");

        int maxLevel = MaxLevel;
        int forLevel = Level > 0 ? Level : 1;
        float currentValue = this.CalculateSkillValue(forLevel);
        builder.AppendLine($"Food eitr bonus: {Math.Round(currentValue, 1)}%");
        if (Level < maxLevel && Level > 0)
        {
            float nextValue = this.CalculateSkillValue(forLevel + 1);
            float valueDiff = nextValue - currentValue;
 
            double roundedValueDiff = Math.Round(valueDiff, 1);

            builder.AppendLine("\nNext Level:");
            builder.AppendLine($"Food eitr bonus: {Math.Round(nextValue, 1)}% <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color>");
        }
        return builder.ToString();
    }
    

    public override bool CanRightClickCast => false;
    public override bool IsPassive => true;
    public override CostType _costType => CostType.None;
    public override Color SkillColor => new Color(1f, 0.36f, 0.88f);
}

[HarmonyPatch(typeof(Player),nameof(Player.GetTotalFoodValue))]
static class Player_GetTotalFoodValue_Patch
{
    static void Postfix(ref float hp, ref float stamina, ref float eitr)
    {
        if (ClassManager.CurrentClass == Class.None) return;
        MH_Skill hpSkill = ClassManager.CurrentClassDef.GetSkill(Druid_FoodKnowledge_Health.CachedKey);
        if (hpSkill is { Level: > 0 })
        {
            float hpToAdd = 0;
            float multiplier = hpSkill.CalculateSkillValue() / 100f;
            foreach (Player.Food food in Player.m_localPlayer.m_foods)
                hpToAdd += food.m_health * multiplier;
            hp += hpToAdd;
        }
        MH_Skill staminaSkill = ClassManager.CurrentClassDef.GetSkill(Druid_FoodKnowledge_Stamina.CachedKey);
        if (staminaSkill is { Level: > 0 })
        {
            float staminaToAdd = 0;
            float multiplier = staminaSkill.CalculateSkillValue() / 100f;
            foreach (Player.Food food in Player.m_localPlayer.m_foods)
                staminaToAdd += food.m_stamina * multiplier;
            stamina += staminaToAdd;
        }
        MH_Skill eitrSkill = ClassManager.CurrentClassDef.GetSkill(Druid_FoodKnowledge_Eitr.CachedKey);
        if (eitrSkill is { Level: > 0 }) 
        {
            float eitrToAdd = 0;
            float multiplier = eitrSkill.CalculateSkillValue() / 100f;
            foreach (Player.Food food in Player.m_localPlayer.m_foods)
                eitrToAdd += food.m_eitr * multiplier;
            eitr += eitrToAdd;
        }
    }
}
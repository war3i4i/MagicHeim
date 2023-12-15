namespace MagicHeim.MH_Classes;

public class MH_SkillDefinition
{
    public ConfigEntry<float> MinLvlValue { get; set; }
    public ConfigEntry<float> MaxLvlValue { get; set; }
    public ConfigEntry<float> MinLvlManacost { get; set; }
    public ConfigEntry<float> MaxLvlManacost { get; set; }
    public ConfigEntry<float> MinLvlCooldown { get; set; }
    public ConfigEntry<float> MaxLvlCooldown { get; set; }
    public ConfigEntry<float> MinLvlDuration { get; set; }
    public ConfigEntry<float> MaxLvlDuration { get; set; }
    public ConfigEntry<int> RequiredLevel { get; set; }
    public ConfigEntry<int> MaxLevel { get; set; }
    public ConfigEntry<int> MinLvlAoE { get; set; }
    public ConfigEntry<int> MaxLvlAoE { get; set; }
    public ConfigEntry<int> MinLvlChargeTime { get; set; }
    public ConfigEntry<int> MaxLvlChargeTime { get; set; }
    public ConfigEntry<int> LevelingStep { get; set; }
    public ConfigEntry<string> RequiredItemFirstHalfToUpgrade { get; set; }
    public ConfigEntry<int> RequiredItemFirstHalfAmountToUpgrade { get; set; }
    public ConfigEntry<float> RequiredItemFirstHalfAmountToUpgrade_Step { get; set; }
    public ConfigEntry<string> RequiredItemSecondHalfToUpgrade { get; set; }
    public ConfigEntry<int> RequiredItemSecondHalfAmountToUpgrade { get; set; }
    public ConfigEntry<float> RequiredItemSecondHalfAmountToUpgrade_Step { get; set; }
    public ConfigEntry<string> RequiredItemFinalToUpgrade { get; set; }
    public ConfigEntry<int> RequiredItemFinalAmountToUpgrade { get; set; }
    public ConfigEntry<int> AbilityStartLevel { get; set; }
    public List<ConfigEntry<float>> ExternalValues { get; set; }
    public string Animation { get; set; }
    public float AnimationTime { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public Sprite Icon { get; set; }
    public string Video { get; set; }
    public string _InternalName { get; set; }
    public int Key => _InternalName.GetStableHashCode();
}
namespace MagicHeim.MH_Interfaces;

public abstract class MH_ClassDefinition
{
    private readonly Dictionary<int, MH_Skill> _currentSkillDefinitions = new();

    protected MH_ClassDefinition(string name, string description)
    {
        Name = name;
        Description = description;
    }

    public string Name { get; }

    public string Description { get; }
    

    public void Reset()
    {
        foreach (KeyValuePair<int, MH_Skill> mhSkill in GetSkills())
        {
            mhSkill.Value.Toggled = false;
            mhSkill.Value.StartCooldown(0, true);
            mhSkill.Value.SetLevel(mhSkill.Value.AbilityStartLevel);
        }
    }

    public bool CanChangeClass()
    {
        foreach (KeyValuePair<int, MH_Skill> mhSkill in GetSkills())
        {
            if (mhSkill.Value.Toggled || mhSkill.Value.GetCooldown() > 0)
                return false;
        }
        return true;
    }

    private void AddSkill(MH_Skill skill) => _currentSkillDefinitions[skill.Key] = skill.Clone();

    protected void AddSkill(string key) =>
        (SkillsDatabase.SkillsDatabase.TryGetSkillDefinition(key, out MH_Skill skillDefinition) ? (Action<MH_Skill>)AddSkill : null)?.Invoke(skillDefinition);

    public Dictionary<int, MH_Skill> GetSkills() => _currentSkillDefinitions;
    public MH_Skill GetSkill(int key) => _currentSkillDefinitions.TryGetValue(key, out MH_Skill skill) ? skill : null;


    public abstract void Init();
    protected internal abstract Color GetColor { get; }
    public GameObject OnSelect_VFX { get; set; }


    protected void SortSkills()
    {
        List<MH_Skill> sortedSkills = _currentSkillDefinitions.Values.OrderBy(skill => skill.IsPassive)
            .ThenBy(skill => skill.RequiredLevel).ToList();
        _currentSkillDefinitions.Clear();
        foreach (MH_Skill skill in sortedSkills)
            _currentSkillDefinitions[skill.Key] = skill;
    }
}
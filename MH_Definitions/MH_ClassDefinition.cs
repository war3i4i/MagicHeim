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

    protected void ResetSkills() => _currentSkillDefinitions.Clear();

    public void Reset()
    {
        foreach (var mhSkill in GetSkills())
        {
            mhSkill.Value.Toggled = false;
            mhSkill.Value.StartCooldown(0, true);
            mhSkill.Value.SetLevel(0);
        }
    }

    public bool CanChangeClass()
    {
        foreach (var mhSkill in GetSkills())
        {
            if (mhSkill.Value.Toggled || mhSkill.Value.GetCooldown() > 0)
                return false;
        }
        return true;
    }

    private void AddSkill(MH_Skill skill) => _currentSkillDefinitions[skill.Key] = skill.Clone();

    protected void AddSkill(string key) =>
        (SkillsDatabase.SkillsDatabase.TryGetSkillDefinition(key, out var skillDefinition) ? (Action<MH_Skill>)AddSkill : null)?.Invoke(skillDefinition);

    public Dictionary<int, MH_Skill> GetSkills() => _currentSkillDefinitions;
    public MH_Skill GetSkill(int key) => _currentSkillDefinitions.TryGetValue(key, out var skill) ? skill : null;


    public abstract void Init();
    protected internal abstract Color GetColor { get; }
    public GameObject OnSelect_VFX { get; set; }


    protected void SortSkills()
    {
        var sortedSkills = _currentSkillDefinitions.Values.OrderBy(skill => skill.IsPassive)
            .ThenBy(skill => skill.RequiredLevel).ToList();
        _currentSkillDefinitions.Clear();
        foreach (var skill in sortedSkills)
            _currentSkillDefinitions[skill.Key] = skill;
    }
}
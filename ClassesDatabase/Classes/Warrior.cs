using MagicHeim.MH_Interfaces;

namespace MagicHeim.ClassesDatabase.Classes;

public class Warrior : MH_ClassDefinition
{
    public Warrior(string name, string description) : base(name, description)
    {
        OnSelect_VFX = MagicHeim.asset.LoadAsset<GameObject>("Warrior_Select");
    }

    public override void Init()
    {
        ResetSkills();
    }


    protected internal override Color GetColor => Color.red;
}
using MagicHeim.MH_Interfaces;

namespace MagicHeim.ClassesDatabase.Classes;

public class Archer : MH_ClassDefinition
{
    public Archer(string name, string description) : base(name, description)
    {
        OnSelect_VFX = MagicHeim.asset.LoadAsset<GameObject>("Archer_Select");
    }

    public override void Init()
    {
        
    }


    protected internal override Color GetColor => Color.yellow;
}
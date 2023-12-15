using MagicHeim.MH_Interfaces;

namespace MagicHeim.ClassesDatabase.Classes;

public class Assassin : MH_ClassDefinition
{
    public Assassin(string name, string description) : base(name, description)
    {
        OnSelect_VFX = MagicHeim.asset.LoadAsset<GameObject>("Assassin_Select");
    }

    public override void Init()
    {
        
    }


    protected internal override Color GetColor => Color.cyan;
}
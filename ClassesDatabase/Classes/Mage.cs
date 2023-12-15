using MagicHeim.MH_Interfaces;

namespace MagicHeim.ClassesDatabase.Classes;

public class Mage : MH_ClassDefinition
{
    public Mage(string name, string description) : base(name, description)
    {
        OnSelect_VFX = MagicHeim.asset.LoadAsset<GameObject>("Mage_Select");
    }

    public override void Init()
    {
        AddSkill("Mage_Energyblast");
        AddSkill("Mage_Teleport");
        AddSkill("Mage_Manaflow");
        AddSkill("Mage_Elementalknowledge");
        AddSkill("Mage_Eitrcontrol");
        AddSkill("Mage_Elementalresistance");
        AddSkill("Mage_Masteroftime");
        AddSkill("Mage_Eitreconomy");
        AddSkill("Mage_Elementalvampirism");
        AddSkill("Mage_Elementaltimescale");
        AddSkill("Mage_Elementalmastery");
        AddSkill("Mage_Bluntmastery");
        AddSkill("Mage_Thunderwrath");
        AddSkill("Mage_Thundershock");
        AddSkill("Mage_Frostbeam");
        AddSkill("Mage_Fireball");
        AddSkill("Mage_Frostball");
        AddSkill("Mage_Lightningball");
        AddSkill("Mage_Waterwalk");
        AddSkill("Mage_Icewall");
        AddSkill("Mage_Iceshield");
        AddSkill("Mage_Fireshield");
        AddSkill("Mage_Arcaneshield");
        AddSkill("Mage_Eitrsphere");
        AddSkill("Mage_Randomstrike");
        AddSkill("Mage_Arcanespikes");
        AddSkill("Mage_Blackhole");
        AddSkill("Mage_Portal");
        AddSkill("Mage_Weaponenchantfire");
        AddSkill("Mage_Weaponenchantlightning");
        AddSkill("Mage_Weaponenchantfrost");
        AddSkill("Mage_Meteor");
        AddSkill("Mage_Energystorm");
        AddSkill("Mage_Waveofflame");
        AddSkill("Mage_Ancienttotem");
        SortSkills();
    }


    protected internal override Color GetColor => Color.blue;
}
namespace MagicHeim;

public class Exp_Configs
{
    public static ConfigEntry<string> ExpMap;
    public static ConfigEntry<int> MaxLevel;
    public static ConfigEntry<int> StartExp;
    public static ConfigEntry<float> Exp_Stepping;
    public static ConfigEntry<int> SkillpointsPerLevel;
    public static ConfigEntry<float> GLOBAL_EXP_MULTIPLIER;
    public static ConfigEntry<float> GLOBAL_DAMAGE_MULTIPLIER;
    public static ConfigEntry<ProgressionType> Exp_ProgressionType;

    public static void Init()
    {
        Exp_Configs.ExpMap = MagicHeim.config("ExpMap", "Exp Map", Exp_Configs.DefaultValue, "");
        Exp_Configs.MaxLevel = MagicHeim.config("LevelSystem", "Max Level", 85, ""); 
        Exp_Configs.StartExp = MagicHeim.config("LevelSystem", "Start EXP", 100, "");
        Exp_Configs.Exp_Stepping = MagicHeim.config("LevelSystem", "EXP Stepping", 1.125f, "");
        Exp_Configs.Exp_ProgressionType = MagicHeim.config("LevelSystem", "Progression Type", Exp_Configs.ProgressionType.Geometric, "");
        Exp_Configs.SkillpointsPerLevel = MagicHeim.config("LevelSystem", "Skillpoints Per Level", 2, "");
        Exp_Configs.GLOBAL_EXP_MULTIPLIER = MagicHeim.config("GLOBALS", "Global EXP Multiplier", 1f, "");
        Exp_Configs.GLOBAL_DAMAGE_MULTIPLIER = MagicHeim.config("GLOBALS", "Global Damage Multiplier", 1f, "");
    }

    public enum ProgressionType
    {
        Arithmetic,
        Geometric
    }

    public static string DefaultValue =
        "Neck:15, " +
        "Boar:15, " +
        "Greyling:25, " +
        "Greydwarf:50, " +
        "Greydwarf_Elite:100, " +
        "Greydwarf_Shaman:80, " +
        "Skeleton:60, " +
        "Skeleton_Poison:100, " +
        "Skeleton_NoArcher:60, " +
        "Ghost:80, Troll:300, " +
        "Draugr:100, " +
        "Draugr_Ranged:180, " +
        "Blob:120, " +
        "BlobElite:180, " +
        "Draugr_Elite:300, " +
        "Leech:120, " +
        "Surtling:100, " +
        "Wraith:200, " +
        "Abomination:400, " +
        "Wolf:250, " +
        "Hatchling:250, " +
        "Fenring:300, " +
        "StoneGolem:600, " +
        "Bat:140, " +
        "Ulv:300, " +
        "Cultist:400, " +
        "Deathsquito:200, " +
        "Lox:500, " +
        "Goblin:350, " +
        "GoblinArcher:380, " +
        "GoblinBrute:450, " +
        "GoblinShaman:300, " +
        "BlobTar:350, " +
        "Serpent:400, " +
        "Seeker:700, " +
        "SeekerBrood:400, " +
        "SeekerBrute:950, " +
        "Tick:180, " +
        "Gjall:1000, " +
        "Dverger:500, " +
        "DvergerMage:500, " +
        "DvergerMageFire:500, " +
        "DvergerMageIce:500, " +
        "DvergerMageSupport:500, " +
        "Hare:10, " +
        "Chick:5, " +
        "Hen:10, " +
        "SeekerQueen:3200, " +
        "Eikthyr:150, " +
        "gb_king:300, " +
        "Bonemass:700, " +
        "Dragon:1000, " +
        "GoblinKing:1500";

    /*public static string DefaultValue =
        "Neck:4, " +
        "Boar:6, " +
        "Greyling:7, " +
        "Greydwarf:9, " +
        "Greydwarf_Elite:11, " + 
        "Skeleton:8, " +
        "Skeleton_Poison:10, " +
        "Ghost:12, " +
        "Troll:20, " + 
        "Draugr:15, " +
        "Blob:14, " +
        "BlobElite:20, " +
        "Draugr_Elite:15, " +
        "Leech:15, " +
        "Surtling:25, " +
        "Wraith:30, " +
        "Wolf:25, " +
        "Hatchling:20, " +
        "Fenring:30, " +
        "StoneGolem:40, " +
        "Bat:20, " +
        "Ulv:35, " +
        "Cultist:40, " +
        "Deathsquito:25, " +
        "Lox:50, " +
        "Goblin:40, " +
        "GoblinBrute:55, " +
        "GoblinShaman:55, " +
        "BlobTar:30, " +
        "Serpent:120, " +
        "Seeker:60, " +
        "SeekerBrute:90, " +
        "BabySeeker:25, " +
        "Gjall:100, " +
        "Dverger:50, " +
        "DvergerMage:50, " +
        "Hare:10 ";
        */
}
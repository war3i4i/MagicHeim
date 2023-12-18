using Groups;
using MagicHeim.MH_Enums;
using MagicHeim.MH_Interfaces;
using MagicHeim.UI_s;
using MonoMod.Utils;
using Logger = MagicHeim_Logger.Logger;

namespace MagicHeim;

public static class ClassManager
{
    private const string _savedClass = "MH_Class";
    private const string _savedLevel = "MH_Level";
    private const string _savedExp = "MH_Exp"; 
    private const string _savedData = "MH_Data";
    private const string _savedSkillPoints = "MH_SkillPoints"; 
    private const string _savedPanel = "MH_Panel";

    public static readonly Action<string, int> OnCharacterKill = MonsterKilled;
    public static MH_ClassDefinition CurrentClassDef { get; private set; }
    public static Class CurrentClass { get; private set; } = Class.None;
    public static int Level { get; private set; } = 1; 
    public static long EXP { get; private set; }
    public static int SkillPoints { get; private set; }

    public static void SetClass(Class newClass)
    {
        CurrentClass = newClass;
        SkillPoints = Exp_Configs.SkillpointsPerLevel.Value;
        Level = 1;
        EXP = 0;
        SkillPanelUI.Hide();
        CurrentClassDef?.Reset();
        if (CurrentClass == Class.None) return;
        CurrentClassDef = ClassesDatabase.ClassesDatabase.GetClassDefinition(CurrentClass);
        CurrentClassDef?.Reset();
        SkillPanelUI.Show(CurrentClassDef);
    }

    public static void ResetSkills()
    {
        SkillPoints = Level * Exp_Configs.SkillpointsPerLevel.Value;
        SkillPanelUI.Reset();
        CurrentClassDef?.Reset();
        SaveAll(); 
    }

    public static bool CanUpgradeSkill(MH_Skill skill)
    {
        int requiredLevel = skill.RequiredLevel + (skill.LevelingStep * skill.Level);
        return SkillPoints > 0 && skill.Level < skill.MaxLevel &&
               requiredLevel <= Level;
    }

    public static bool HaveEnoughItemsForSkillUpgrade(MH_Skill skill, out int requiredAmount,
        bool removeOnSuccess = false)
    {
        string prefab;
        if (skill.Level + 1 >= skill.MaxLevel) prefab = skill.RequiredItemToUpgradeFinal;
        else if (skill.Level > skill.MaxLevel / 2) prefab = skill.RequiredItemToUpgradeSecondHalf;
        else prefab = skill.RequiredItemToUpgrade;

        requiredAmount = 0;
        GameObject upradeItem = ObjectDB.instance.GetItemPrefab(prefab);
        if (!upradeItem) return true;

        int initialAmount;
        float step;
        if (skill.Level + 1 >= skill.MaxLevel) initialAmount = skill.RequiredItemAmountToUpgradeFinal;
        else if (skill.Level > skill.MaxLevel / 2) initialAmount = skill.RequiredItemAmountToUpgradeSecondHalf;
        else initialAmount = skill.RequiredItemAmountToUpgrade;

        if (skill.Level + 1 >= skill.MaxLevel) step = 1f;
        else if (skill.Level > skill.MaxLevel / 2) step = skill.RequiredItemAmountToUpgradeSecondHalf_Step;
        else step = skill.RequiredItemAmountToUpgrade_Step;

        requiredAmount = Mathf.RoundToInt(initialAmount * Mathf.Pow(step, skill.Level));
        if (Player.m_debugMode) return true;
        int inventoryAmount = Utils.CountItems(upradeItem.name);
        bool result = inventoryAmount >= requiredAmount;

        if (result && removeOnSuccess)
            Utils.RemoveItems(upradeItem.name, requiredAmount);

        return result;
    }

    public static bool TryUpgradeSkill(MH_Skill skill, int points = 1)
    {
        bool result = false;
        while (points > 0)
        {
            if (CanUpgradeSkill(skill) && HaveEnoughItemsForSkillUpgrade(skill, out int amount, true))
            {
                skill.Level++;
                SkillPoints--;
                SkillPanelUI.Status = SkillPanelUI.Change.ToUpdate;
                result = true;
            }
            else
            {
                break;
            }

            --points;
        }

        return result;
    }

    public class SaveLoad : ISerializableParameter
    {
        public Class Class;
        public int Level;
        public long EXP;
        public int SkillPoints;
        public string SkillsData;
        public string PanelData;
        public void Serialize(ref ZPackage pkg)
        {
            pkg.Write((int)Class);
            pkg.Write(Level);
            pkg.Write(EXP);
            pkg.Write(SkillPoints);
            pkg.Write(SkillsData != null);
            if (SkillsData != null)
                pkg.Write(SkillsData);
            pkg.Write(PanelData != null);
            if (PanelData != null)
                pkg.Write(PanelData);
        }

        public void Deserialize(ref ZPackage pkg)
        {
            Class = (Class)pkg.ReadInt();
            Level = pkg.ReadInt();
            EXP = pkg.ReadLong();
            SkillPoints = pkg.ReadInt();
            if (pkg.ReadBool())
                SkillsData = pkg.ReadString();
            if (pkg.ReadBool())
                PanelData = pkg.ReadString();
        }
    }
    
    public static SaveLoad GetSaveData()
    {
        SaveLoad save = new()
        {
            Class = CurrentClass,
            Level = Level,
            EXP = EXP,
            PanelData = SkillPanelUI.Serialize(),
            SkillPoints = SkillPoints
        };
        if (CurrentClassDef != null)
        {
            Dictionary<int, MH_Skill> skills = CurrentClassDef.GetSkills();
            string data = "";
            foreach (KeyValuePair<int, MH_Skill> skill in skills)
            {
                if (skill.Value.Level <= 0) continue;
                int spellCD = Mathf.Max(0, (int)skill.Value.GetCooldown());
                if (spellCD > 0)
                    data += skill.Key + ":" + skill.Value.Level + ":" + spellCD + ";";
                else
                    data += skill.Key + ":" + skill.Value.Level + ";";
            }
            data = data.TrimEnd(';');
            save.SkillsData = data;
        }
        return save;
    }
    
    private static void SaveAll()
    {
        Player p = Player.m_localPlayer;
        if (!p) return;
        p.m_customData[_savedClass] = CurrentClass.ToString();
        p.m_customData[_savedLevel] = Level.ToString();
        p.m_customData[_savedExp] = EXP.ToString();
        p.m_customData[_savedPanel] = SkillPanelUI.Serialize();
        if (CurrentClassDef != null)
        {
            Dictionary<int, MH_Skill> skills = CurrentClassDef.GetSkills();
            string data = "";
            foreach (KeyValuePair<int, MH_Skill> skill in skills)
            {
                if (skill.Value.Level <= 0) continue;
                int spellCD = Mathf.Max(0, (int)skill.Value.GetCooldown());
                if (spellCD > 0)
                    data += skill.Key + ":" + skill.Value.Level + ":" + spellCD + ";";
                else
                    data += skill.Key + ":" + skill.Value.Level + ";";
            }

            data = data.TrimEnd(';');
            p.m_customData[_savedData] = data;
            p.m_customData[_savedSkillPoints] = SkillPoints.ToString();
        }
        else
        {
            if (p.m_customData.ContainsKey(_savedData)) p.m_customData.Remove(_savedData);
        }
    }


    private static void LoadPlayerSkillData(string data)
    {
        if (data == null) return;
        Dictionary<int, MH_Skill> classSkills = CurrentClassDef.GetSkills();
        string[] split = data.Split(';');
        foreach (string s in split)
        {
            string[] split2 = s.Split(':');
            if (split2.Length == 2)
            {
                int skill = int.Parse(split2[0]);
                int lvl = int.Parse(split2[1]);
                if (classSkills.ContainsKey(skill))
                {
                    classSkills[skill].SetLevel(lvl);
                }
            }
            else if (split2.Length == 3)
            {
                int skill = int.Parse(split2[0]);
                int lvl = int.Parse(split2[1]);
                int cd = int.Parse(split2[2]);
                if (classSkills.ContainsKey(skill))
                {
                    classSkills[skill].SetLevel(lvl);
                    classSkills[skill].StartCooldown(cd, true);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.Save))]
    static class Player_Save_Patch
    {
        static void Prefix(Player __instance)
        {
            if (!Player.m_localPlayer) return;
            SaveAll();
        }
    }
    
    public static void Load(SaveLoad data)
    {
        CurrentClass = data.Class;
        SkillPoints = data.SkillPoints;
        Level = data.Level;
        EXP = data.EXP;
        SkillPanelUI.Hide();
        CurrentClassDef?.Reset();
        if (CurrentClass == Class.None) return;
        CurrentClassDef = ClassesDatabase.ClassesDatabase.GetClassDefinition(CurrentClass);
        CurrentClassDef.Reset();
        LoadPlayerSkillData(data.SkillsData);
        SkillPanelUI.Show(CurrentClassDef, data.PanelData);
    }

    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    static class PlayerLoad
    {
        static void Postfix()
        {
            Player p = Player.m_localPlayer;
            if (!p) return;
            CurrentClass = Class.None;
            Level = 0;
            EXP = 0;
            InitLevelSystem();
            SaveLoad save = new();
            if (p.m_customData.TryGetValue(_savedClass, out var value))
                save.Class = (Class)Enum.Parse(typeof(Class), value);
            if (p.m_customData.TryGetValue(_savedLevel, out var value1))
                save.Level = int.Parse(value1);
            if (p.m_customData.TryGetValue(_savedExp, out var value2))
                save.EXP = long.Parse(value2);
            if (p.m_customData.TryGetValue(_savedData, out var value3))
                save.SkillsData = value3;
            if (p.m_customData.TryGetValue(_savedPanel, out var value4))
                save.PanelData = value4;
            if (p.m_customData.TryGetValue(_savedSkillPoints, out var value5))
                save.SkillPoints = int.Parse(value5);
            Load(save);
        }
    }
 

    /////// class level system
    ///
    private static long[] LevelUpExp;

    private static readonly Dictionary<string, int> _expMap = new();

    public static void InitLevelSystem()
    {
        LevelUpExp = new long[Exp_Configs.MaxLevel.Value + 5];
        _expMap.Clear();
        try
        { 
            _expMap.AddRange(Exp_Configs.ExpMap.Value.Replace(" ", "").TrimEnd(',')
                .Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':'))
                .ToDictionary(x => x[0], x => int.Parse(x[1])));
        }
        catch(Exception ex) 
        {
            Logger.Log($"Got exception while parsing exp map:\n{ex}");
        }

        float exp = Exp_Configs.StartExp.Value;
        for (int i = 0; i < Exp_Configs.MaxLevel.Value + 5; ++i)
        {
            LevelUpExp[i] = (long)exp;
            if (Exp_Configs.Exp_ProgressionType.Value == Exp_Configs.ProgressionType.Arithmetic)
                exp += Exp_Configs.Exp_Stepping.Value;
            else if (Exp_Configs.Exp_ProgressionType.Value == Exp_Configs.ProgressionType.Geometric)
                exp *= Exp_Configs.Exp_Stepping.Value;
        }
    }

    public static long GetExpForLevel(int level)
    {
        level--;
        if (level < 0 || level >= Exp_Configs.MaxLevel.Value) return int.MaxValue;
        return LevelUpExp[level];
    }

    public static void AddExp(long exp)
    {
        if (Level >= Exp_Configs.MaxLevel.Value) return;
        Utils.FloatingText($"<color=yellow>+ {exp}</color><color=#00FFFF> exp</color>");
        while (exp > 0)
        {
            if (Level >= Exp_Configs.MaxLevel.Value) break;
            long expForLevel = GetExpForLevel(Level);
            if (expForLevel > exp + EXP)
            {
                EXP += exp;
                exp = 0;
            }
            else
            {
                exp -= expForLevel - EXP;
                EXP = 0;
                Level++;
                SkillPoints += Exp_Configs.SkillpointsPerLevel.Value;
            }
        }
    }

    public static void SetLevel(int level)
    {
        int currentLevel = Level;
        int howManyPointsToAdd = level - currentLevel;
        SkillPoints += howManyPointsToAdd * Exp_Configs.SkillpointsPerLevel.Value;
        Level = level;
    }

    private static void MonsterKilled(string name, int level)
    {
        if (!_expMap.ContainsKey(name)) return;
        int exp = _expMap[name];
        exp = (int)(exp / Exp_Configs.GLOBAL_EXP_MULTIPLIER.Value);
        exp = (int)(exp * (1 + (level - 1) * 0.2f));
        if (Groups.API.IsLoaded())
        {
            List<PlayerReference> group = Groups.API.GroupPlayers();
            if (group.Count <= 1)
            {
                AddExp(exp);
                return;
            }

            if (group.Count > 1) exp /= (group.Count - 1);
            Vector3 pos = Player.m_localPlayer.transform.position;
            ZPackage pkg = new();
            pkg.Write(exp);
            pkg.Write(pos);
            foreach (PlayerReference member in group)
                ZRoutedRpc.instance.InvokeRoutedRPC(member.peerId, "MH PartyAddEXP", pkg);
            return;
        }

        AddExp(exp);
    }
}
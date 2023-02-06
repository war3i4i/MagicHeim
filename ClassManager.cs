using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using fastJSON;
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


    private static Class _currentClass = Class.None;
    public static Action<string, int> OnCharacterKill = MonsterKilled;
    private static MH_ClassDefinition _currentClassDefinition;

    public static MH_ClassDefinition CurrentClassDef
    {
        get { return _currentClassDefinition; }
    }

    private static int _currentLevel = 1;
    private static long _currentExp;
    private static int _skillPoints;


    public static Class CurrentClass
    {
        get { return _currentClass; }
        set { _currentClass = value; }
    }

    public static int Level
    {
        get { return _currentLevel; }
        private set { _currentLevel = value; }
    }

    public static long EXP
    {
        get { return _currentExp; }
        set { _currentExp = value; }
    }

    public static int SkillPoints
    {
        get { return _skillPoints; }
        set { _skillPoints = value; }
    }

    public static void SetClass(Class newClass)
    {
        _currentClass = newClass;
        _currentClassDefinition = ClassesDatabase.ClassesDatabase.GetClassDefinition(_currentClass);
        _currentClassDefinition.Init();
        SkillPanelUI.Show(_currentClassDefinition);
        SkillPoints = Exp_Configs.SkillpointsPerLevel.Value;
        Level = 1;
        EXP = 0;
    }

    public static void ResetSkills()
    {
        _currentClassDefinition.Init();
        SkillPoints = Level * Exp_Configs.SkillpointsPerLevel.Value;
        SkillPanelUI.Reset();
    }

    public static bool CanUpgradeSkill(MH_Skill skill)
    {
        var requiredLevel = skill.RequiredLevel + (skill.LevelingStep * skill.Level);
        return ClassManager.SkillPoints > 0 && skill.Level < skill.MaxLevel &&
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
        var inventoryAmount = Utils.CountItems(upradeItem.name);
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
            if (CanUpgradeSkill(skill) && HaveEnoughItemsForSkillUpgrade(skill, out var amount, true))
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

    private static void SaveAll()
    {
        Player p = Player.m_localPlayer;
        if (!p) return;
        //class
        p.m_customData[_savedClass] = _currentClass.ToString();
        //level
        p.m_customData[_savedLevel] = _currentLevel.ToString();
        //exp
        p.m_customData[_savedExp] = _currentExp.ToString();
        //skills data
        if (_currentClassDefinition != null)
        {
            var skills = _currentClassDefinition.GetSkills();
            string data = "";
            foreach (var skill in skills)
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
            p.m_customData[_savedSkillPoints] = _skillPoints.ToString();
        }
        else
        {
            if (p.m_customData.ContainsKey(_savedData)) p.m_customData.Remove(_savedData);
        }

        //layout
        p.m_customData[_savedPanel] = SkillPanelUI.Serialize();
    }


    private static void LoadPlayerSkillData(string data)
    {
        if (data == null) return;
        var classSkills = _currentClassDefinition.GetSkills();
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
                var cd = int.Parse(split2[2]);
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

    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    static class PlayerLoad
    {
        static void Postfix()
        {
            Player p = Player.m_localPlayer;
            if (!p) return;
            _currentClass = Class.None;
            Level = 0;
            EXP = 0;
            InitLevelSystem();
            string SkillsData = null;
            string PanelData = null;
            if (p.m_customData.ContainsKey(_savedClass))
            {
                CurrentClass = (Class)Enum.Parse(typeof(Class), p.m_customData[_savedClass]);
            }

            if (p.m_customData.ContainsKey(_savedLevel))
            {
                Level = int.Parse(p.m_customData[_savedLevel]);
            }

            if (p.m_customData.ContainsKey(_savedExp))
            {
                EXP = long.Parse(p.m_customData[_savedExp]);
            }

            if (p.m_customData.ContainsKey(_savedData))
            {
                SkillsData = p.m_customData[_savedData];
            }

            if (p.m_customData.ContainsKey(_savedPanel))
            {
                PanelData = p.m_customData[_savedPanel];
            }

            if (p.m_customData.ContainsKey(_savedSkillPoints))
            {
                SkillPoints = int.Parse(p.m_customData[_savedSkillPoints]);
            }

            SkillPanelUI.Hide();
            if (_currentClass == Class.None) return;
            _currentClassDefinition = ClassesDatabase.ClassesDatabase.GetClassDefinition(_currentClass);
            _currentClassDefinition.Init();
            LoadPlayerSkillData(SkillsData);
            SkillPanelUI.Show(_currentClassDefinition, PanelData);
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
        Utils.FloatingText($"<color=yellow>+ {exp}</color><color=cyan> exp</color>");
        while (exp > 0)
        {
            if (Level >= Exp_Configs.MaxLevel.Value) break;
            long expForLevel = GetExpForLevel(Level);
            if (expForLevel > exp + _currentExp)
            {
                EXP += exp;
                exp = 0;
            }
            else
            {
                exp -= expForLevel - _currentExp;
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
            var group = Groups.API.GroupPlayers();
            if (group.Count <= 1)
            {
                AddExp(exp);
                return;
            }

            if (group.Count > 1) exp /= (group.Count - 1);
            var pos = Player.m_localPlayer.transform.position;
            ZPackage pkg = new();
            pkg.Write(exp);
            pkg.Write(pos);
            foreach (var member in group)
                ZRoutedRpc.instance.InvokeRoutedRPC(member.peerId, $"MH PartyAddEXP", pkg);
            return;
        }

        AddExp(exp);
    }
}
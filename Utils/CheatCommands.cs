using MagicHeim.MH_Enums;
using MagicHeim.MH_Interfaces;

namespace MagicHeim;

public class CheatCommands
{
    [HarmonyPatch(typeof(Chat), nameof(Chat.InputText))]
    static class Chat_InputText_Patch
    {
        const string resetSkills = "/mh cd";
        const string setLevel = "/mh level ";
        const string addExp = "/mh exp ";
        const string testRoom = "/mh testroom";
        const string setClass = "/mh class ";

        private const string showExp = "/exp";

        static bool Prefix(Chat __instance) 
        {
            string text = __instance.m_input.text;
            //non cheat

            if (text == showExp && ClassManager.CurrentClass != Class.None)
            {
                __instance.AddString(
                    $"<color=#00FFFF>Magic</color><color=yellow>Heim</color> EXP: <color=#00FFFF>{ClassManager.EXP}</color> / <color=yellow>{ClassManager.GetExpForLevel(ClassManager.Level)}</color>");
                return false;
            }


            if (!Player.m_debugMode) return true;
            
            

            if (text.Trim() == testRoom)
            {
                GameObject room = UnityEngine.Object.Instantiate(MagicHeim.asset.LoadAsset<GameObject>("TestRoom"),
                    Player.m_localPlayer.transform.position + Vector3.up * 100f, Quaternion.identity);
                Transform spawn = room.transform.Find("Spawn");
                Player.m_localPlayer.transform.position = spawn.position;
            }

            if (text.Trim() == resetSkills)
            {
                MH_ClassDefinition classDef = ClassManager.CurrentClassDef;
                if (classDef != null)
                {
                    Dictionary<int, MH_Skill> skills = classDef.GetSkills();
                    foreach (KeyValuePair<int, MH_Skill> skill in skills)
                    {
                        skill.Value.StartCooldown(0, true); 
                    } 

                    __instance.AddString("<color=#00FF00>CD's resetted</color>");
                }
                else
                {
                    __instance.AddString("You don't have a class yet");
                }

                return false;
            }

            if (text.StartsWith(setLevel))
            {
                string level = text.Substring(setLevel.Length);
                if (int.TryParse(level, out int levelInt))
                {
                    ClassManager.SetLevel(levelInt);
                    __instance.AddString($"<color=#00FF00>Level Set To {levelInt}</color>");
                }
                else
                {
                    __instance.AddString("Invalid level");
                }

                return false;
            }

            if (text.StartsWith(addExp))
            {
                string exp = text.Substring(addExp.Length);
                if (int.TryParse(exp, out int expInt))
                {
                    ClassManager.AddExp(expInt);
                    __instance.AddString($"<color=#00FF00>Added {expInt} exp</color>");
                }
                else
                {
                    __instance.AddString("Invalid exp");
                }

                return false;
            }

            if (text.StartsWith(setClass))
            {
                string className = text.Substring(setClass.Length);
                if (Enum.TryParse(className, true, out Class classEnum))
                {
                    ClassManager.SetClass(classEnum);
                    __instance.AddString($"<color=#00FF00>Class Set To {classEnum}</color>");
                }
                else
                {
                    __instance.AddString("Invalid class");
                }

                return false;
            }


            return true;
        }
    }
}
using System.Text;
using MagicHeim.MH_Enums;

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
            var text = __instance.m_input.text;
            //non cheat

            if (text == showExp && ClassManager.CurrentClass != Class.None)
            {
                __instance.AddString(
                    $"<color=cyan>Magic</color><color=yellow>Heim</color> EXP: <color=cyan>{ClassManager.EXP}</color> / <color=yellow>{ClassManager.GetExpForLevel(ClassManager.Level)}</color>");
                return false;
            }


            if (!Player.m_debugMode) return true;
            
            

            if (text.Trim() == testRoom)
            {
                var room = UnityEngine.Object.Instantiate(MagicHeim.asset.LoadAsset<GameObject>("TestRoom"),
                    Player.m_localPlayer.transform.position + Vector3.up * 100f, Quaternion.identity);
                var spawn = room.transform.Find("Spawn");
                Player.m_localPlayer.transform.position = spawn.position;
            }

            if (text.Trim() == resetSkills)
            {
                var classDef = ClassManager.CurrentClassDef;
                if (classDef != null)
                {
                    var skills = classDef.GetSkills();
                    foreach (var skill in skills)
                    {
                        skill.Value.StartCooldown(0, true); 
                    } 

                    __instance.AddString("<color=lime>CD's resetted</color>");
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
                if (int.TryParse(level, out var levelInt))
                {
                    ClassManager.SetLevel(levelInt);
                    __instance.AddString($"<color=lime>Level Set To {levelInt}</color>");
                }
                else
                {
                    __instance.AddString("Invalid level");
                }

                return false;
            }

            if (text.StartsWith(addExp))
            {
                var exp = text.Substring(addExp.Length);
                if (int.TryParse(exp, out var expInt))
                {
                    ClassManager.AddExp(expInt);
                    __instance.AddString($"<color=lime>Added {expInt} exp</color>");
                }
                else
                {
                    __instance.AddString("Invalid exp");
                }

                return false;
            }

            if (text.StartsWith(setClass))
            {
                var className = text.Substring(setClass.Length);
                if (Enum.TryParse(className, true, out Class classEnum))
                {
                    ClassManager.SetClass(classEnum);
                    __instance.AddString($"<color=lime>Class Set To {classEnum}</color>");
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
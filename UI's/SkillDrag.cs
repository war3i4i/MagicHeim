using MagicHeim.MH_Interfaces;

namespace MagicHeim.UI_s;

public static class SkillDrag
{
    static SkillDrag()
    {
        Init();
    }

    private static GameObject UI;
    private static GameObject DragObject;

    private static void Init()
    {
        UI = MagicHeim.asset.LoadAsset<GameObject>("DragUI");
    }

    public static float LastDragTime;
    public static MH_Skill LastDragSkill;
    private static Coroutine Routine;

    public static void StartDrag(MH_Skill skill, int button = -1)
    {
        LastDragSkill = skill;
        if (DragObject) UnityEngine.Object.Destroy(DragObject);
        if (Routine != null) MagicHeim._thistype.StopCoroutine(Routine);
        if (skill == null || skill.Level <= 0) return;
        LastDragTime = Time.unscaledTime;
        DragObject = UnityEngine.Object.Instantiate(UI);
        DragObject.transform.Find("Canvas/icon").GetComponent<Image>().sprite = skill.Icon;
        DragObject.transform.Find("Canvas/icon").GetComponent<Image>().color = new Color(1, 1, 1, 0.5f);
        if (button >= 0)
        {
            SkillPanelUI.SetSkill(button, -1);
        }
        Routine = MagicHeim._thistype.StartCoroutine(Drag());
    }


    private static IEnumerator Drag()
    {
        while (DragObject && Cursor.visible)
        {
            if (Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.Escape)) 
            {
                break;
            }
            DragObject.transform.Find("Canvas/icon").position = Input.mousePosition;
            LastDragTime = Time.unscaledTime;
            yield return null;
        }
        if (DragObject) UnityEngine.Object.DestroyImmediate(DragObject);
    }
}
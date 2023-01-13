namespace MagicHeim.SkillsDatabase.GlobalMechanics;

public class MH_FollowTargetComponent : MonoBehaviour
{
    public Character enemy;
    public void Setup(Character enemy)
    {
        this.enemy = enemy;
    }
    
    private void FixedUpdate()
    {
        if (enemy != null && !enemy.IsDead())
        {
            transform.position = enemy.transform.position;
        }
    }
}


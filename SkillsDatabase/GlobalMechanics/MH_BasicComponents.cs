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

public class MH_FollowCameraRotation : MonoBehaviour
{
    private void FixedUpdate()
    {
        if(!GameCamera.instance) return;
        transform.rotation = GameCamera.instance.transform.rotation;
    }
}
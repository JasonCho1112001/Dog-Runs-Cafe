using UnityEngine;

public class OmeletteController : MonoBehaviour
{
    [HideInInspector] public bool hasKetchup = false;

    // Called by KetchupCollisionHandler when hit
    public void ApplyKetchup()
    {
        if (!hasKetchup) hasKetchup = true;
    }
}

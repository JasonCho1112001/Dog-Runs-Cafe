using UnityEngine;
using UnityEngine.InputSystem;

public class gameManagerScript : MonoBehaviour
{

    void Start()
    {
        //Set Cursor to be invisible and locked at start
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

    }

    void Update()
    {
        //Press P to toggle cursor visibility and lock state
        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            ToggleCursor();
        }
    }

    public void ToggleCursor()
    {
        Cursor.visible = !Cursor.visible;
        Cursor.lockState = Cursor.visible ? CursorLockMode.None : CursorLockMode.Locked;
    }
}

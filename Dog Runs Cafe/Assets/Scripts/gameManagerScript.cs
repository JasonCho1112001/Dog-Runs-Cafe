using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

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

        // Press R to restart the current scene
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

    }

    public void ToggleCursor()
    {
        Cursor.visible = !Cursor.visible;
        Cursor.lockState = Cursor.visible ? CursorLockMode.None : CursorLockMode.Locked;
    }
}

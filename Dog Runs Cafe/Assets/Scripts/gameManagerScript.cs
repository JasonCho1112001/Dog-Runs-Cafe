using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class gameManagerScript : MonoBehaviour
{

    public GameObject[] miniGames;

    void Start()
    {
        //Set Cursor to be invisible and locked at start
        if (SceneManager.GetActiveScene().name == "Main Game")
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        

        //Disable all minigames except 2nd one at start
        miniGames[0].SetActive(false);
        miniGames[1].SetActive(true);
        miniGames[2].SetActive(false);
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

        // Press 1,2,3... to load mini-games (I know this code is spaghetti)
        if (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            miniGames[0].SetActive(true);
            miniGames[1].SetActive(false);
            miniGames[2].SetActive(false);
        }
        else if (Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            miniGames[0].SetActive(false);
            miniGames[1].SetActive(true);
            miniGames[2].SetActive(false);
        }
        else if (Keyboard.current != null && Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            miniGames[0].SetActive(false);
            miniGames[1].SetActive(false);
            miniGames[2].SetActive(true);
        }


    }

    public void ToggleCursor()
    {
        Cursor.visible = !Cursor.visible;
        Cursor.lockState = Cursor.visible ? CursorLockMode.None : CursorLockMode.Locked;
    }

    public void StartGame()
    {
        SceneManager.LoadScene("Main Game");
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}

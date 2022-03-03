using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuitGame : MonoBehaviour
{
    public void ExitGame()
    {
        Application.Quit();
        // check if the game is actually quiting
        Debug.Log("Returning to Desktop...");
    }
}

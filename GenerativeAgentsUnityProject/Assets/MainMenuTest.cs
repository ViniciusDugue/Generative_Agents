using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuTest : MonoBehaviour
{
    public void StartSim() {
        SceneManager.LoadScene("Sim Environment");
    }

    public void QuitGame () {
        Debug.Log("QUIT!");
        Application.Quit();
    }
}

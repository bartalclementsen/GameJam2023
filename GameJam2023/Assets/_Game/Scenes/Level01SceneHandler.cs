using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Level01SceneHandler : MonoBehaviour
{
    public void WinGame()
    {
        SceneManager.LoadSceneAsync(2);
    }

    public void LoseGame()
    {
        SceneManager.LoadSceneAsync(3);
    }
}

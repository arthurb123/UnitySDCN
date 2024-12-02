using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneSwitcher : MonoBehaviour
{
    void Update()
    {
        // F1: Switch to the Bare demo
        if (Input.GetKeyDown(KeyCode.F1))
            UnityEngine.SceneManagement.SceneManager.LoadScene("Bare-Demo");

        // F2: Switch to the Interactive demo
        if (Input.GetKeyDown(KeyCode.F2))
            UnityEngine.SceneManagement.SceneManager.LoadScene("Interactive-Demo");
    }
}

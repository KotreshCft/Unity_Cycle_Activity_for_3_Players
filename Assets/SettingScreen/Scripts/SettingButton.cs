using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class SettingButton : MonoBehaviour
{
    // Name of the scene to load

    public string SettingSceen = "SettingScene";  //Assets/SettingScreen/SettingScene.unity

    // This function will be called when the button is clicked
    public void LoadNewScene()
    {
        // Check if the scene name is not empty
        if (!string.IsNullOrEmpty(SettingSceen))
        {
            // Load the specified scene
            SceneManager.LoadScene(SettingSceen); //Assets / setting_screen / Setting_scene.unity
        }
        else
        {
            Debug.LogError("Scene name is not set!");
        }
    }
}

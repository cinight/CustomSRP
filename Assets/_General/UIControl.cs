using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIControl : MonoBehaviour
{
    public void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }

  void OnGUI()
    {
        float scale = Screen.height / 1080f;

        GUI.skin.label.fontSize = Mathf.RoundToInt ( 16 * scale );
        GUI.backgroundColor = new Color(0, 0, 0, .80f);
        GUI.color = new Color(1, 1, 1, 1);
        float w = 500 * scale, h = 90 * scale;
        GUILayout.BeginArea(new Rect(Screen.width - w, Screen.height - h, w, h), GUI.skin.box);

        GUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(1, 1, 1, .80f);
        if(GUILayout.Button("\n Prev \n")) PrevScene();
        if(GUILayout.Button("\n Next \n")) NextScene();
        GUILayout.EndHorizontal();

        int currentpage = SceneManager.GetActiveScene().buildIndex +1;
        GUILayout.Label( currentpage + " / " + SceneManager.sceneCountInBuildSettings + " " + SceneManager.GetActiveScene().name );

        GUILayout.EndArea();
    }

    public void NextScene()
    {
        int sceneIndex = SceneManager.GetActiveScene().buildIndex;

        if (sceneIndex < SceneManager.sceneCountInBuildSettings - 1)
            SceneManager.LoadScene(sceneIndex + 1);
        else
            SceneManager.LoadScene(1);
    }

    public void PrevScene()
    {
        int sceneIndex = SceneManager.GetActiveScene().buildIndex;

        if (sceneIndex > 0)
            SceneManager.LoadScene(sceneIndex - 1);
        else
            SceneManager.LoadScene(SceneManager.sceneCountInBuildSettings - 1);
    }
}

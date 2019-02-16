using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SRP0104_CamSwitch : MonoBehaviour
{
    public GameObject[] cameras;

    private GUIStyle style;
    private float scale = 1;

    void Start()
    {
        float scale = Screen.height / 1080f;


    }

    void OnGUI()
    {
        style = GUI.skin.GetStyle("Button");
        style.fontSize = Mathf.RoundToInt ( 30 * scale );
        style.fixedHeight = 120 * scale;

        //GUI.skin.label.fontSize = 
        GUI.backgroundColor = new Color(1, 1, 1, .80f);
        GUI.color = new Color(1, 1, 1, 1);
        float w = 200 * cameras.Length * scale, h = style.fixedHeight+10;
        GUILayout.BeginArea(new Rect(0, 0, w, h), GUI.skin.box);

        GUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(1, 1, 1, .80f);
        for(int i=0; i< cameras.Length; i++)
        {
            if(GUILayout.Button("\n"+cameras[i].name+"\n",style)) SwitchCamera(cameras[i]);
        }
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
    }

    private void SwitchCamera(GameObject cam)
    {
        for(int i=0; i< cameras.Length; i++)
        {
            if(cameras[i] == cam) cameras[i].SetActive(true);
            else cameras[i].SetActive(false);
        }
    }
}

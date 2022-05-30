using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SRP0406_GUILabel : MonoBehaviour
{
    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 50;

        string message = "Click on the color objects to and check Console messages" + "\n";
        GUILayout.Label(message, style);
    }
}

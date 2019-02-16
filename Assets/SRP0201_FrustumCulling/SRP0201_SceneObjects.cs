using UnityEngine;

[ExecuteInEditMode]
public class SRP0201_SceneObjects : MonoBehaviour
{
    public TextMesh tm;
    public Light[] lights;
    public Renderer[] rens;
    public ReflectionProbe[] refl;
   // public Camera MainCam;
   // public Camera AllCam;
   // public Camera NoneCam;

    void Start ()
    {
        SRP0201Instance.textMesh = tm;
        SRP0201Instance.lights = lights;
        SRP0201Instance.reflprobes = refl;
        SRP0201Instance.rens = rens;
        //SRP03Rendering.MainCam = MainCam;
       // SRP03Rendering.AllCam = AllCam;
       // SRP03Rendering.NoneCam = NoneCam;

        //SRP03Rendering.update = true;
    }
    

}

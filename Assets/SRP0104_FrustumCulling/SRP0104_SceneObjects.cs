using UnityEngine;

[ExecuteInEditMode]
public class SRP0104_SceneObjects : MonoBehaviour
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
        SRP0104Instance.textMesh = tm;
        SRP0104Instance.lights = lights;
        SRP0104Instance.reflprobes = refl;
        SRP0104Instance.rens = rens;
        //SRP03Rendering.MainCam = MainCam;
       // SRP03Rendering.AllCam = AllCam;
       // SRP03Rendering.NoneCam = NoneCam;

        //SRP03Rendering.update = true;
    }
    

}

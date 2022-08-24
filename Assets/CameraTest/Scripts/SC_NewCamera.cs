using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SC_NewCamera : MonoBehaviour
{
    public Shader Unlit = null;

    Camera main_cam_;
    Camera sec_cam_;

    public void CaptureScreen()
    {
        OnCaptureScreen(sec_cam_, "sec_cam.png");
        OnCaptureScreen(main_cam_, "main_cam.png");
    }
    void OnCaptureScreen(Camera came,string name = "capture0.png")
    {
        Rect r = new Rect(0, 0, Screen.width, Screen.height);
        RenderTexture rt = new RenderTexture((int)r.width, (int)r.height, 1);

        came.targetTexture = rt;
        came.RenderWithShader(Shader.Find("Custom/Unlit"), "RenderType");

        RenderTexture.active = rt;
        Texture2D screenShot = new Texture2D((int)r.width, (int)r.height, TextureFormat.RGB24, false);

        screenShot.ReadPixels(r, 0, 0);
        screenShot.Apply();

        came.targetTexture = null;
        RenderTexture.active = null;
        GameObject.Destroy(rt);

        byte[] bytes = screenShot.EncodeToPNG();
        string filename = Application.dataPath + "/Common/ScreenCaptures/" + name;
        Debug.Log(filename);
        System.IO.File.WriteAllBytes(filename, bytes);
    }
    private void OnEnable()
    {
        Debug.Log("OnEnable");
        if (Unlit)
            Camera.main.SetReplacementShader(Unlit, "");
        else
            Debug.LogWarning("shader missing!!!!!!!!!!!!!!");
    }
    private void OnDisable()
    {
        Debug.Log("OnDisable");
        Camera.main.ResetReplacementShader();
    }
    // Start is called before the first frame update
    void Start()
    {
        main_cam_ = Camera.main;
        sec_cam_ = gameObject.GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

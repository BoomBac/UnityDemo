using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[ExecuteInEditMode]
public class SC_TransfScene : MonoBehaviour
{
    public enum EFadeMethod
    { 
        Blend,Mask
    }
    public Texture2D NoiseTexture;
    [Header("FadeCurve")]
    [Tooltip("X = Change in mouse position.\nY = Multiplicative factor for camera rotation.")]
    public AnimationCurve EnterCurve = new AnimationCurve(new Keyframe(0,0),new Keyframe(1,1));
    public AnimationCurve LeaveCurve = new AnimationCurve(new Keyframe(0,1), new Keyframe(1,0));


    public EFadeMethod FadeInMethod = EFadeMethod.Blend;
    public EFadeMethod FadeOutMethod = EFadeMethod.Blend;

    float PreInOutValue = 0;

    public string NonCharacterLayerName;

    [Tooltip("0->1为淡入新场景")]
    [Range(0, 1)]
    public float FadeInOutAlpha = 0;

    public Vector4 ScreenMaskParam = new Vector4(0.5f,0.5f,1.98f,20);

    public Material SkillSkyMat;
    public Material SkillPlaneMat;

    Camera main_cam_;
    Camera skill_cam_ = null;
    float blender_factor_ = 0;
    int NonCharacterLayerID;
    float[] FadeDistances;
    RenderTexture ExitSkillRT;

    public void CaptureScreen(Camera came)
    {
        came.targetTexture = ExitSkillRT;
        came.Render();
        RenderTexture.active = ExitSkillRT;
        came.targetTexture = null;
        RenderTexture.active = null;
    }

    public void Enter()
    {
        Debug.Log("Enter Skill Scene");
        SwitchCamera(false);
        Shader.SetGlobalFloat("_Mask", 1);
        ScreenMaskParam.z = 1.98f;
        StartCoroutine(BeDark());
    }
    public void Leave()
    {
        Debug.Log("Leave Skill Scene");
        if (FadeOutMethod == EFadeMethod.Blend)
            StartCoroutine(BeBright());
        else
        {
            blender_factor_ = 0;
            Shader.SetGlobalFloat("_BlenderFactor", blender_factor_);
            Shader.SetGlobalFloat("_Mask", -1);
            StartCoroutine(BeBrightHigh());
        }
        SwitchCamera(false);
    }
    IEnumerator BeBrightHigh()//渐暗
    {
        float cur_time = 0;
        while (ScreenMaskParam.z <= 1.99 && ScreenMaskParam.z > 0)
        {
            ScreenMaskParam.z = Mathf.Min(LeaveCurve.Evaluate(cur_time) * 2,1.98f);
            cur_time += Time.deltaTime;
            Shader.SetGlobalVector("_RadialParam", ScreenMaskParam);
            yield return null;
        }
    }
    IEnumerator BeDark()//渐暗
    {
        float cur_time = 0;
        while (blender_factor_ < 0.99)
        {
            cur_time += Time.deltaTime;
            blender_factor_ = EnterCurve.Evaluate(cur_time);
            Shader.SetGlobalFloat("_BlenderFactor", blender_factor_);
            yield return null;
        }
    }

    IEnumerator BeBright()//渐明
    {
        float cur_time = 0;
        while (blender_factor_ >= 0.01f)
        {
            cur_time += Time.deltaTime;
            blender_factor_ = LeaveCurve.Evaluate(cur_time);
            Shader.SetGlobalFloat("_BlenderFactor", blender_factor_);
            yield return null;
        }
    }

    private void CreateSkillCamera()
    {
        if (skill_cam_ == null)
        {
            skill_cam_ = gameObject.AddComponent(typeof(Camera)) as Camera;
            skill_cam_.CopyFrom(main_cam_);
        }
        //skill_cam_.cullingMask &= ~(1 << LayerMask.NameToLayer("MainScene"));
    }

    void SwitchCamera(bool to_main)
    {
        if (to_main)
        {
            skill_cam_.enabled = false;
            main_cam_.enabled = true;
        }
        else
        {
            main_cam_.enabled = false;
            skill_cam_.enabled = true;
        }

    }

    void ProcessFade(float var, EFadeMethod method)
    {
        if (method == EFadeMethod.Blend)
        {
            Shader.SetGlobalInt("_Method", 1);
            Shader.SetGlobalFloat("_BlenderFactor", var);
        }
        else if(method == EFadeMethod.Mask)
        {
            Shader.SetGlobalInt("_Method", 2);
            ScreenMaskParam.z = var * 2;
            Shader.SetGlobalVector("_RadialParam", ScreenMaskParam);
        }
    }

    void FadeInOut(float var)
    {
        bool bSame = FadeInMethod == FadeOutMethod;
        if (bSame)
        {
            ProcessFade(var, FadeInMethod);
        }
        else
        {
            //淡入
            if (PreInOutValue < var)
            {
                ProcessFade(var, FadeInMethod);
            }
            //淡出
            else if (PreInOutValue > var)
            {
                ProcessFade(var, FadeOutMethod);
            }
            PreInOutValue = var;
        }
    }

    // Start is called before the first frame update
    void Start()
    {

        main_cam_ = Camera.main;
        blender_factor_ = 0;
        Shader.SetGlobalFloat("_BlenderFactor", blender_factor_);
        NonCharacterLayerID = LayerMask.NameToLayer(NonCharacterLayerName);
        FadeDistances = new float[32];
        FadeDistances[NonCharacterLayerID] = 100;
        main_cam_.layerCullDistances = FadeDistances;
        CreateSkillCamera();
    }

    // Update is called once per frame
    void Update()
    {
        Shader.SetGlobalTexture("_NoiseTexture", NoiseTexture);
        FadeInOut(FadeInOutAlpha);
    }
}

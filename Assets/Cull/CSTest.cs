using UnityEngine;

public class CSTest : MonoBehaviour
{
    public ComputeShader cs;
    public RenderTexture buffer;

    // Start is called before the first frame update
    void Start()
    {
        if (buffer == null)
        {
            buffer = new RenderTexture(256, 256, 24);
            buffer.enableRandomWrite = true;
            buffer.Create();
        }

        cs.SetTexture(0, "Result", buffer);
        cs.Dispatch(0, 256 / 8, 256 / 8, 1);
    }

    // Update is called once per frame
    void Update()
    {

    }
}

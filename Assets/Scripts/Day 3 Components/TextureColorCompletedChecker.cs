using UnityEngine;

public class TextureColorCompletedChecker : MonoBehaviour
{
    [SerializeField]
    protected float checkInterval = 1;

    [SerializeField, Range(0, 1)]
    protected float percentageNeeded = .9f;

    [SerializeField]
    protected Mesh meshToCheck;

    [SerializeField]
    protected RenderTexture renderTexture;

    [SerializeField]
    protected Effect CompletedEffect = null;

    protected float amountOfVertices;
    protected Texture2D verifyTexture;

    void Start()
    {
        verifyTexture = new Texture2D(renderTexture.width, renderTexture.height);
        amountOfVertices = meshToCheck.vertexCount;
        InvokeRepeating("CheckIfDone", checkInterval, checkInterval);
    }

    protected void CheckIfDone()
    {
        RenderTexture.active = renderTexture;
        verifyTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        verifyTexture.Apply();
        RenderTexture.active = null;

        int verticiesUpdate = 0;
        for (int i = 0; i < amountOfVertices; i++)
        {
            Vector2 uv = meshToCheck.uv[i];
            var centerX = Mathf.FloorToInt(uv.x * verifyTexture.width);
            var centerY = Mathf.FloorToInt(uv.y * verifyTexture.height);

            Color colorCheck = verifyTexture.GetPixel(centerX, centerY);

            if (colorCheck.r > 0)
            {
                verticiesUpdate++;
            }
        }

        if (verticiesUpdate / amountOfVertices >= percentageNeeded)
        {
            CancelInvoke("CheckIfDone");
            CompletedEffect?.Play();
        }
    }
}

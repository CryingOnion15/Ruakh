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
    protected Texture2D verifyTexture;

    [SerializeField]
    protected Effect CompletedEffect = null;

    protected float amountOfVertices;

    void Start()
    {
        amountOfVertices = meshToCheck.vertexCount;
        InvokeRepeating("CheckIfDone", checkInterval, checkInterval);
    }

    protected void CheckIfDone()
    {
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

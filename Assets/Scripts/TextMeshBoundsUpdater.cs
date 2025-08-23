using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
[ExecuteAlways]
public class TextMeshBoundsUpdater : MonoBehaviour
{
    protected TMP_Text tmpText;
    protected MaterialPropertyBlock mpb;
    protected Renderer textRenderer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        tmpText = GetComponent<TMP_Text>();
        if (tmpText)
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(TextUpdated);
            textRenderer = GetComponent<Renderer>();
            mpb = new MaterialPropertyBlock();
            UpdateBounds();
        }
    }

    protected void OnValidate()
    {
        tmpText = GetComponent<TMP_Text>();
        textRenderer = GetComponent<Renderer>();
        mpb = mpb ?? new MaterialPropertyBlock();

        if (tmpText && textRenderer)
        {
            UpdateBounds();
        }
    }


    private void OnDestroy()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(TextUpdated);
    }

    protected void TextUpdated(Object obj)
    {
        if (obj == tmpText)
        {
            UpdateBounds();
        }
    }

    protected void UpdateBounds()
    {
        tmpText.ForceMeshUpdate();
        textRenderer.GetPropertyBlock(mpb);
        mpb.SetVector("_BoundsMin", tmpText.textBounds.min);
        mpb.SetVector("_BoundsMax", tmpText.textBounds.max);
        textRenderer.SetPropertyBlock(mpb);
    }
}

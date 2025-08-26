using TMPro;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
[ExecuteAlways]
public class TextMeshBoundsUpdater : MonoBehaviour
{
    protected TMP_Text tmpText;
    protected MaterialPropertyBlock mpb;
    protected Renderer textRenderer;

    public bool generate = false;

    void OnValidate()
    {
        if (generate)
        {
            Start();

#if UNITY_EDITOR
            EditorApplication.delayCall += () =>
            {
                if (this != null) // make sure object wasn’t destroyed
                    generate = false;
            };
#endif
        }
    }

    void Start()
    {
        tmpText = GetComponent<TMP_Text>();
        if (tmpText)
        {
            textRenderer = GetComponent<Renderer>();
            mpb = new MaterialPropertyBlock();
            UpdateBounds();
        }
    }

    protected void UpdateBounds()
    {
        if (tmpText)
        {
            tmpText.ForceMeshUpdate();
            textRenderer.GetPropertyBlock(mpb);
            mpb.SetVector("_BoundsMin", tmpText.textBounds.min);
            mpb.SetVector("_BoundsMax", tmpText.textBounds.max);
            textRenderer.SetPropertyBlock(mpb);
        }
    }
}

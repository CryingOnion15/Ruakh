using System.Collections.Generic;
using UnityEngine;

public class RenderTextureToSolidEffect : Effect
{
    [SerializeField]
    protected List<RenderTexture> textures = new List<RenderTexture>();

    [SerializeField]
    protected Color color = Color.white;

    // Start is called before the first frame update
    protected override void playAction()
    {
        textures.ForEach(texture =>
        {
            RenderTexture.active = texture;
            GL.Clear(true, true, color);
            RenderTexture.active = null;
        });

        Complete();
    }
}

using System.Collections.Generic;
using UnityEngine;

public class TextureResetter : MonoBehaviour
{
    [SerializeField]
    protected List<Texture2D> textures = new List<Texture2D>();

    // Start is called before the first frame update
    void Start()
    {
        textures.ForEach(texture =>
        {
            for (int i = 0; i < texture.width; i++)
            {
                for (int j = 0; j < texture.height; j++)
                {
                    texture.SetPixel(i, j, Color.black);
                }
            }

            texture.Apply();
        });
    }

    public void Final()
    {
        textures.ForEach(texture =>
        {
            for (int i = 0; i < texture.width; i++)
            {
                for (int j = 0; j < texture.height; j++)
                {
                    texture.SetPixel(i, j, Color.red);
                }
            }

            texture.Apply();
        });
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using NUnit.Framework.Constraints;
using NUnit.Framework.Internal;
using UnityEngine;
using UnityEngine.Rendering;

public struct PixelCoord
{
    public int x;
    public int y;

    public PixelCoord(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
}

public class Day3RaycastController : MonoBehaviour
{
    public LayerMask layerMask;
    public Mesh meshData;
    public Texture2D texture;
    public float increaseAmount = .01f;

    List<PixelCoord> currentPixels = new List<PixelCoord>();

    void Start()
    {
        for (int i = 0; i < texture.width; i++)
        {
            for (int j = 0; j < texture.height; j++)
            {
                texture.SetPixel(i, j, Color.black);
            }
        }

        texture.Apply();
    }

    void Update()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, -transform.up, out hit, Mathf.Infinity, layerMask))
        {
            var uv1 = meshData.uv[meshData.triangles[hit.triangleIndex * 3]];
            var uv2 = meshData.uv[meshData.triangles[hit.triangleIndex * 3 + 1]];
            var uv3 = meshData.uv[meshData.triangles[hit.triangleIndex * 3 + 2]];

            Vector3 vert1 = meshData.vertices[meshData.triangles[hit.triangleIndex * 3]];
            Vector3 vert2 = meshData.vertices[meshData.triangles[hit.triangleIndex * 3 + 1]];
            Vector3 vert3 = meshData.vertices[meshData.triangles[hit.triangleIndex * 3 + 2]];

            vert1 = hit.collider.transform.TransformPoint(vert1);
            vert2 = hit.collider.transform.TransformPoint(vert2);
            vert3 = hit.collider.transform.TransformPoint(vert3);
            Debug.DrawLine(vert1, vert2, Color.blue, 5f);
            Debug.DrawLine(vert2, vert3, Color.blue, 5f);
            Debug.DrawLine(vert3, vert1, Color.blue, 5f);

            if (checkPixelIsBlack(uv1))
            {
                AddPixelFromUV(uv1);
            }

            if (checkPixelIsBlack(uv2))
            {
                AddPixelFromUV(uv2);
            }

            if (checkPixelIsBlack(uv3))
            {
                AddPixelFromUV(uv3);
            }
        }

        if (currentPixels.Count > 0)
        {
            List<PixelCoord> markedForRemoval = new List<PixelCoord>();
            currentPixels.ForEach(pixel =>
            {
                Color col = texture.GetPixel(pixel.x, pixel.y);
                col.r += increaseAmount;
                if (col.r >= 1)
                {
                    col.r = 1;
                    markedForRemoval.Add(pixel);
                }
                texture.SetPixel(pixel.x, pixel.y, col);
            });
            currentPixels.RemoveAll(pixel => markedForRemoval.Contains(pixel));

            texture.Apply();
        }
    }

    //Set Star of Red
    protected void AddPixelFromUV(Vector2 uv)
    {
        var centerX = Mathf.FloorToInt(uv.x * texture.width);
        var centerY = Mathf.FloorToInt(uv.y * texture.height);

        //texture.SetPixel(centerX, centerY, Color.red);
        currentPixels.Add(new PixelCoord(centerX, centerY));

        if (centerX > 0)
        {
            //texture.SetPixel(centerX - 1, centerY, Color.red);
            currentPixels.Add(new PixelCoord(centerX - 1, centerY));
        }

        if (centerX < texture.width - 1)
        {
            //texture.SetPixel(centerX + 1, centerY, Color.red);
            currentPixels.Add(new PixelCoord(centerX + 1, centerY));
        }

        if (centerY > 0)
        {
            //texture.SetPixel(centerX, centerY - 1, Color.red);
            currentPixels.Add(new PixelCoord(centerX, centerY - 1));
        }

        if (centerY < texture.height - 1)
        {
            //texture.SetPixel(centerX, centerY + 1, Color.red);
            currentPixels.Add(new PixelCoord(centerX, centerY + 1));
        }
    }

    protected bool checkPixelIsBlack(Vector2 uv)
    {
        var centerX = Mathf.FloorToInt(uv.x * texture.width);
        var centerY = Mathf.FloorToInt(uv.y * texture.height);

        var color = texture.GetPixel(centerX, centerY);

        return color == Color.black;
    }
}

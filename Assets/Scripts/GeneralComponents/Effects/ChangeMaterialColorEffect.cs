using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeMaterialColorEffect : Effect
{
    [SerializeField]
    protected Renderer rend;

    [SerializeField]
    protected Color newColor;

    [SerializeField]
    protected float duration = 0;

    protected Material currentMaterial = null;
    protected Color startingColor;
    protected float finalColor;
    protected bool isPlaying = false;
    protected float totalTime = 0;

    protected void Start()
    {
        if (rend)
        {
            currentMaterial = rend.material;
        }
    }

    // Update is called once per frame
    protected void Update()
    {
        if (isPlaying)
        {
            totalTime += Time.deltaTime;

            if (totalTime < duration)
            {
                currentMaterial.color = Color.Lerp(startingColor, newColor, totalTime / duration);
            }
            else
            {
                currentMaterial.color = newColor;
                Complete();
            }
        }
    }

    protected override void playAction()
    {
        if (currentMaterial != null)
        {
            if (duration != 0)
            {
                isPlaying = true;
                startingColor = currentMaterial.color;
            }
            else
            {
                Complete();
            }
        }
        else
        {
            Complete();
        }
    }

    protected override void onComplete()
    {
        isPlaying = false;
        base.onComplete();
    }
}

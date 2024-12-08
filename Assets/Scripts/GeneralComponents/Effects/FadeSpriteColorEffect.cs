using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UIElements;

public class FadeSpriteColorEffect : Effect
{
    [SerializeField]
    protected SpriteRenderer spriteRenderer;

    [SerializeField]
    protected Color newColor;

    [SerializeField]
    protected float duration = 0;

    protected bool isPlaying = false;
    protected float timePassed = 0;
    protected Color startingColor;

    void Update() {
        if(isPlaying) {
            timePassed += Time.deltaTime;

            if(timePassed >= duration) {
                spriteRenderer.color = UnityEngine.Vector4.Lerp(startingColor, newColor, 1);
                isPlaying = false;
                Complete();
            } else {
                spriteRenderer.color = Color.Lerp(startingColor, newColor, timePassed/duration);
            }
        }
    }

    protected override void playAction()
    {
        if(!isPlaying) {
            if(duration == 0) {
                spriteRenderer.color = newColor;
                Complete();
            } else {
                timePassed = 0; 
                startingColor = spriteRenderer.color;
                isPlaying = true;
            }
        }
    }    
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeScaleEffect : Effect
{
    [SerializeField]
    protected GameObject objectToScale;

    [SerializeField]
    protected Vector3 newScale = Vector3.zero;

    [SerializeField]
    protected float duration = 0;

    protected Vector3 startScale;
    protected bool isPlaying = false;
    protected float totalTime = 0;

    // Update is called once per frame
    void Update()
    {
        if (isPlaying)
        {
            totalTime += Time.deltaTime;

            if (totalTime < duration)
            {
                objectToScale.transform.localScale = Vector3.Lerp(
                    startScale,
                    newScale,
                    totalTime / duration
                );
            }
            else
            {
                objectToScale.transform.localScale = newScale;
                Complete();
            }
        }
    }

    protected override void playAction()
    {
        if (objectToScale != null)
        {
            if (duration != 0)
            {
                isPlaying = true;
                startScale = objectToScale.transform.localScale;
                totalTime = 0;
            }
            else
            {
                objectToScale.transform.localScale = newScale;
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

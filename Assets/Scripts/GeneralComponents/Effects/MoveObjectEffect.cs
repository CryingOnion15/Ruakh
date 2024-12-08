using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveObjectEffect : Effect
{
    [SerializeField]
    protected GameObject objectToMove;

    [SerializeField]
    protected bool useLocalCoords = false;

    [SerializeField]
    protected Vector3 newPosition = Vector3.zero;

    [SerializeField]
    protected float duration = 0;

    protected Vector3 startPosition;
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
                if (useLocalCoords)
                {
                    objectToMove.transform.localPosition = Vector3.Lerp(
                        startPosition,
                        newPosition,
                        totalTime / duration
                    );
                }
                else
                {
                    objectToMove.transform.position = Vector3.Lerp(
                        startPosition,
                        newPosition,
                        totalTime / duration
                    );
                }
            }
            else
            {
                if (useLocalCoords)
                {
                    objectToMove.transform.localPosition = newPosition;
                }
                else
                {
                    objectToMove.transform.position = newPosition;
                }
                Complete();
            }
        }
    }

    protected override void playAction()
    {
        if (objectToMove != null)
        {
            if (duration != 0)
            {
                isPlaying = true;
                startPosition = useLocalCoords
                    ? objectToMove.transform.localPosition
                    : objectToMove.transform.position;
                totalTime = 0;
            }
            else
            {
                if (useLocalCoords)
                {
                    objectToMove.transform.localPosition = newPosition;
                }
                else
                {
                    objectToMove.transform.position = newPosition;
                }
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

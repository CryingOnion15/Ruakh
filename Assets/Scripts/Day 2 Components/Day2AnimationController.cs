using System.ComponentModel;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class Day2AnimationController : MonoBehaviour
{
    [SerializeField]
    protected Animation anim = null;

    [SerializeField]
    protected AnimationClip clip = null;

    [SerializeField]
    protected InputAction holdAction = null;

    [SerializeField]
    protected float holdThreshold = 2.5f;

    [SerializeField, ReadOnly(true)]
    protected float currentThreshold = 0f;
    protected float currentTime = 0f;
    protected bool isCompleted = false;

    void OnEnable()
    {
        holdAction.Enable();
        SetClip();
    }

    void OnDisable()
    {
        holdAction.Disable();
    }

    // Update is called once per frame
    void Update()
    {
        if (isCompleted)
            return;

        if (holdAction.IsPressed())
        {
            if (currentThreshold < holdThreshold)
            {
                if (anim.isPlaying)
                {
                    anim[clip.name].speed = 0;
                }

                currentThreshold += Time.deltaTime;
                if (currentThreshold > holdThreshold)
                {
                    currentThreshold = holdThreshold;
                }
            }

            if (currentThreshold == holdThreshold && anim[clip.name].speed != 1)
            {
                anim[clip.name].speed = 1;
            }

            if (anim[clip.name].time >= anim[clip.name].length)
            {
                Complete();
            }
        }
        else
        {
            if (anim[clip.name].speed == 1)
            {
                anim[clip.name].speed = -1;
            }

            if (anim[clip.name].time <= 0)
            {
                anim[clip.name].time = 0;
                anim[clip.name].speed = 0;
            }

            if (currentThreshold > 0)
            {
                currentThreshold -= Time.deltaTime;

                if (currentThreshold < 0)
                {
                    currentThreshold = 0;
                }
            }
        }
    }

    public void SetClip()
    {
        anim.clip = clip;
        anim[clip.name].speed = 0;
        anim.Play();
    }

    public void Complete()
    {
        isCompleted = true;
        anim.Stop();
        //Play Effect Here.
    }
}

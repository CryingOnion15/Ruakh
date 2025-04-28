using System.Collections.Generic;
using System.ComponentModel;
using NUnit.Framework.Constraints;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class AnimationMilestone
{
    public InputAction milestoneAction;

    public Effect milestoneHitEffect;

    public Effect milestoneReverseEffect;

    public bool QueryInput()
    {
        return milestoneAction.IsPressed();
    }

    public void EnableInput()
    {
        milestoneAction.Enable();
    }

    public void DisableInput()
    {
        milestoneAction.Disable();
    }
}

public class Day2AnimationController : MonoBehaviour
{
    [SerializeField]
    protected Animation anim = null;

    [SerializeField]
    protected AnimationClip clip = null;

    [SerializeField]
    protected List<AnimationMilestone> milestones = new List<AnimationMilestone>();

    [SerializeField]
    protected float reverseSpeed = -.5f;
    protected bool isCompleted = false;
    protected int currentMilestone = 0;
    protected AnimationState animState;
    protected float milestonePercentTime = 0;

    void OnEnable()
    {
        SetClip();
    }

    void OnDisable()
    {
        milestones.ForEach(milestone =>
        {
            milestone.DisableInput();
        });
    }

    // Update is called once per frame
    void Update()
    {
        if (isCompleted)
            return;

        if (CheckProgressInput())
        {
            //Debug.Log("HERE");
            if (animState.speed != 1)
            {
                animState.speed = 1;
            }

            if (animState.time >= animState.length)
            {
                Complete();
            }
            else if (animState.time >= milestonePercentTime * (currentMilestone + 1))
            {
                milestones[currentMilestone].milestoneHitEffect?.PlayFromEvent();
                currentMilestone++;
                milestones[currentMilestone].EnableInput();
            }
        }
        else if (CheckStaticInput())
        {
            if (animState.time > milestonePercentTime * currentMilestone)
            {
                animState.speed = reverseSpeed;
            }
            else
            {
                animState.speed = 0;
            }
        }
        else
        {
            if (currentMilestone > 0 && animState.time < milestonePercentTime * currentMilestone)
            {
                milestones[currentMilestone].DisableInput();
                currentMilestone--;
                milestones[currentMilestone].milestoneReverseEffect?.PlayFromEvent();
            }

            if (animState.speed != reverseSpeed)
            {
                animState.speed = reverseSpeed;
            }

            if (animState.time <= 0)
            {
                animState.time = 0;
                animState.speed = 0;
            }
        }
    }

    public void SetClip()
    {
        anim.clip = clip;
        animState = anim[clip.name];
        animState.speed = 0;
        milestonePercentTime = animState.length / milestones.Count;
        milestones[0].EnableInput();
        anim.Play();
        animState.time = 0;
    }

    public void Complete()
    {
        isCompleted = true;
        anim.Stop();
        milestones[milestones.Count - 1].milestoneHitEffect?.Play();
    }

    /*
    Check if the input to progress the animation is being pressed.
    */
    protected bool CheckProgressInput()
    {
        bool test = true;

        for (int i = currentMilestone; i >= 0 && test; i--)
        {
            test = test && milestones[i].QueryInput();
        }

        return test;
    }

    /*
    Checks if the input to maintain the animation is being pressed.
    */
    protected bool CheckStaticInput()
    {
        bool test = currentMilestone > 0;

        for (int i = 0; i < currentMilestone && test; i++)
        {
            test = test && milestones[i].QueryInput();
        }

        return test;
    }
}

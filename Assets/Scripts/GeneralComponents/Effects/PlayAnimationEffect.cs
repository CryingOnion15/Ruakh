using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayAnimationEffect : Effect
{
    [SerializeField]
    protected Animation anim;

    [SerializeField]
    protected AnimationClip clip;

    protected override void playAction()
    {
        if (anim != null)
        {
            if (clip != null)
            {
                anim.Play(clip.name);
                Invoke("CompleteAnimation", clip.length);
            }
            else
            {
                anim.Play();
                Invoke("CompleteAnimation", anim.clip.length);
            }
        }
        else
        {
            Complete();
        }
    }

    protected void CompleteAnimation()
    {
        Complete();
    }
}

using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class EffectSequence : Effect
{
    //TODO: need to figure this out. Probably needs a custom editor.
    // [SerializeField]
    // protected bool useChildren = false;

    // #if !useChildren
    [SerializeField]
    protected List<Effect> effects = new List<Effect>();
    //#endif

    protected int currentEffect = 0;

    protected override void playAction()
    {
        currentEffect = 0;

        effects[currentEffect].Play(onSubEffectCompleted);
    }

    public override void Stop()
    {
        base.Stop();

        // Remove the effect callback and stop the current effect
        effects[currentEffect].onCompleteCallback -= onSubEffectCompleted;
        effects[currentEffect].Stop();

        Complete();
    }

    protected void onSubEffectCompleted()
    {
        currentEffect++;

        if (currentEffect != effects.Count)
        {
            effects[currentEffect].Play(onSubEffectCompleted);
        }
        else
        {
            Complete();
        }
    }
}

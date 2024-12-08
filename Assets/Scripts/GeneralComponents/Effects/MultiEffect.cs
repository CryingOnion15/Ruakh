using System.Collections.Generic;
using UnityEngine;

public class MultiEffect : Effect
{
    //TODO: need to figure this out. Probably needs a custom editor.
    // [SerializeField]
    // protected bool useChildren = false;

    // #if !useChildren
    [SerializeField]
    protected List<Effect> effects = new List<Effect>();
    //#endif

    protected int effectsComplete = 0;

    protected override void playAction()
    {
        effectsComplete = 0;

        foreach (Effect effect in effects)
        {
            effect.Play(onSubEffectCompleted);
        }
    }

    public override void Stop()
    {
        base.Stop();
        foreach (Effect effect in effects)
        {
            effect.Stop();
        }
        Complete();
    }

    protected void onSubEffectCompleted()
    {
        effectsComplete++;

        if (effectsComplete == effects.Count)
        {
            Complete();
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleSystemEffect : Effect
{
    [SerializeField]
    protected ParticleSystem particleSys;

    [SerializeField]
    protected bool play = true;

    protected override void playAction()
    {
        if (play)
        {
            particleSys.Play();
            Complete();
        }
        else
        {
            particleSys.Stop();
            Complete();
        }
    }
}

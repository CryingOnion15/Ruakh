using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ParticleSystemTransterComponent : MonoBehaviour
{
    [SerializeField]
    protected ParticleSystem toSystem = null;

    [SerializeField]
    protected int amountToCollect = 500;

    [SerializeField]
    protected UnityEvent collectionDoneEvent = new UnityEvent();

    protected ParticleSystem fromSystem = null;
    protected List<ParticleSystem.Particle> fromEnterParticles =
        new List<ParticleSystem.Particle>();
    protected ParticleSystem.Particle[] toParticles;
    protected ParticleSystem.Particle[] fromParticles;

    protected int currentToParticle = 0;
    protected int amountCollected = 0;
    protected bool isCollecting = true;

    protected void Start()
    {
        fromSystem = GetComponent<ParticleSystem>();
        if (toSystem)
        {
            toParticles = new ParticleSystem.Particle[toSystem.main.maxParticles];
            fromParticles = new ParticleSystem.Particle[fromSystem.main.maxParticles];
        }

        currentToParticle = 0;
        isCollecting = true;
    }

    protected void OnParticleTrigger()
    {
        if (fromSystem && toSystem && isCollecting)
        {
            fromEnterParticles.Clear();

            int enterCount = fromSystem.GetTriggerParticles(
                ParticleSystemTriggerEventType.Enter,
                fromEnterParticles,
                out var enterColliderData
            );

            toSystem.GetParticles(toParticles);

            for (int i = 0; i < enterCount; i++)
            {
                if (enterColliderData.GetColliderCount(i) > 0)
                {
                    ParticleSystem.Particle p = fromEnterParticles[i];
                    ParticleSystem.Particle newP = fromEnterParticles[i];

                    newP.position = enterColliderData
                        .GetCollider(i, 0)
                        .transform.InverseTransformPoint(newP.position);

                    p.remainingLifetime = -1f;

                    toParticles[currentToParticle] = newP;
                    currentToParticle = (currentToParticle + 1) % toSystem.main.maxParticles;
                    amountCollected++;

                    fromEnterParticles[i] = p;
                }
            }

            fromSystem.SetTriggerParticles(
                ParticleSystemTriggerEventType.Enter,
                fromEnterParticles
            );

            toSystem.SetParticles(toParticles);

            if (amountCollected >= amountToCollect)
            {
                isCollecting = false;
                collectionDoneEvent.Invoke();
            }
        }
    }
}

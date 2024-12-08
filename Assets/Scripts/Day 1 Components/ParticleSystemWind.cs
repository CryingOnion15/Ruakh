using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

[ExecuteInEditMode]
public class ParticleSystemWind : MonoBehaviour
{
    [SerializeField]
    protected float windForce = 1;

    protected ParticleSystem particleSys;

    protected void OnEnable()
    {
        particleSys = GetComponent<ParticleSystem>();
    }

    protected void OnParticleTrigger()
    {
        if (particleSys)
        {
            List<ParticleSystem.Particle> enterParticles = new List<ParticleSystem.Particle>();
            List<ParticleSystem.Particle> outsideParticles = new List<ParticleSystem.Particle>();

            int enterCount = particleSys.GetTriggerParticles(
                ParticleSystemTriggerEventType.Enter,
                enterParticles,
                out var enterColliderData
            );

            int outsideCount = particleSys.GetTriggerParticles(
                ParticleSystemTriggerEventType.Outside,
                outsideParticles
            );

            Debug.Log("" + outsideCount);

            for (int i = 0; i < enterCount; i++)
            {
                if (enterColliderData.GetColliderCount(i) > 0)
                {
                    ParticleSystem.Particle p = enterParticles[i];
                    var collider = enterColliderData.GetCollider(i, 0);
                    p.velocity += collider.transform.forward * windForce;
                    enterParticles[i] = p;
                }
            }

            for (int i = 0; i < outsideCount; i++)
            {
                ParticleSystem.Particle p = outsideParticles[i];
                p.velocity = p.velocity - p.velocity * .2f * Time.deltaTime;
                outsideParticles[i] = p;
            }

            particleSys.SetTriggerParticles(ParticleSystemTriggerEventType.Enter, enterParticles);
            particleSys.SetTriggerParticles(
                ParticleSystemTriggerEventType.Outside,
                outsideParticles
            );
        }
    }
}

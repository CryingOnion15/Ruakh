using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnStartEffectTrigger : MonoBehaviour
{
    [SerializeField]
    protected Effect effect;

    // Start is called before the first frame update
    void Start()
    {
        effect.Play();
    }
}

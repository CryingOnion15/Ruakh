using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnButtonPressTrigger : MonoBehaviour
{
    [SerializeField]
    protected Effect effectToTrigger;

    public void OnFireEffect() {
        if(effectToTrigger != null) {
            effectToTrigger.Play();
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

//public class ColliderEvent : UnityEvent;

public class CollisionEvent : MonoBehaviour
{
    [SerializeField]
    protected GameObject objectToCollideWith;

    [SerializeField]
    protected UnityEvent collisionEvent;

    protected void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject == objectToCollideWith)
        {
            collisionEvent.Invoke();
        }
    }

    void OnCollisionEnter(Collision collider)
    {
        if (collider.gameObject == objectToCollideWith)
        {
            collisionEvent.Invoke();
        }
    }
}

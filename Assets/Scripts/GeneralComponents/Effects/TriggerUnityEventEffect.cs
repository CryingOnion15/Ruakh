using UnityEngine;
using UnityEngine.Events;

public class TriggerUnityEventEffect : Effect
{
    [SerializeField]
    protected UnityEvent effectEvent;

    protected override void playAction()
    {
        effectEvent.Invoke();
        Complete();
    }
}

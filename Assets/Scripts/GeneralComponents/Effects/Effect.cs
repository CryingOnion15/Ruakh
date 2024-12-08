using UnityEngine;

public abstract class Effect : MonoBehaviour
{
    [SerializeField]
    protected float startDelay = 0;

    [SerializeField]
    protected float completeDelay = 0;

    // Delegate
    public delegate void CompletedDelegate();
    public CompletedDelegate onCompleteCallback;

    /**
    * Function used to invoke the play actions after the start delay.
    */
    public void Play(CompletedDelegate completedCallback = null)
    {
        onCompleteCallback += completedCallback;
        Invoke("playAction", startDelay);
    }

    public void PlayFromEvent()
    {
        Play();
    }

    /**
    * Abstract Function used to execute the action of the effect.
    */
    protected abstract void playAction();

    /**
    * Abstract Function used to stop the effect.
    */
    public virtual void Stop()
    {
        CancelInvoke();
    }

    /**
    * Fuction used to invoke the onComplete function.
    * An effect should call this when it is done.
    */
    public void Complete()
    {
        Invoke("onComplete", completeDelay);
    }

    /**
    * Function used to trigger the completed delegate functions.
    * Can be overridden for extra functionality.
    */
    protected virtual void onComplete()
    {
        onCompleteCallback?.Invoke();
    }
}

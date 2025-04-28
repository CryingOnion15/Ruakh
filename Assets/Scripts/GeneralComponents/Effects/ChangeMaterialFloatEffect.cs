using UnityEngine;

public class ChangeMaterialFloatEffect : Effect
{
    [SerializeField]
    protected Renderer rend;

    [SerializeField]
    protected string propertyName = "";

    [SerializeField]
    protected float newValue = 0;

    [SerializeField]
    protected float duration = 0;

    protected Material currentMaterial = null;
    protected float startingValue;
    protected bool isPlaying = false;
    protected float totalTime = 0;

    protected void Start()
    {
        if (rend)
        {
            currentMaterial = rend.material;
        }
    }

    // Update is called once per frame
    protected void Update()
    {
        if (isPlaying)
        {
            totalTime += Time.deltaTime;

            if (totalTime < duration)
            {
                currentMaterial.SetFloat(
                    "_" + propertyName,
                    Mathf.Lerp(startingValue, newValue, totalTime / duration)
                );
            }
            else
            {
                currentMaterial.SetFloat("_" + propertyName, newValue);
                Complete();
            }
        }
    }

    protected override void playAction()
    {
        if (currentMaterial != null)
        {
            if (duration != 0)
            {
                isPlaying = true;
                startingValue = currentMaterial.GetFloat("_" + propertyName);
            }
            else
            {
                Complete();
            }
        }
        else
        {
            currentMaterial.SetFloat("_" + propertyName, newValue);
            Complete();
        }
    }

    protected override void onComplete()
    {
        isPlaying = false;
        base.onComplete();
    }
}

using TMPro;
using UnityEngine;

public class FadeUITextColorEffect : Effect
{
    [SerializeField]
    protected TMP_Text textRenderer;

    [SerializeField]
    protected Color newColor;

    [SerializeField]
    protected float duration = 0;

    protected bool isPlaying = false;
    protected float timePassed = 0;
    protected Color startingColor;

    void Update()
    {
        if (isPlaying)
        {
            timePassed += Time.deltaTime;

            if (timePassed >= duration)
            {
                textRenderer.color = UnityEngine.Vector4.Lerp(startingColor, newColor, 1);
                isPlaying = false;
                Complete();
            }
            else
            {
                textRenderer.color = Color.Lerp(startingColor, newColor, timePassed / duration);
            }
        }
    }

    protected override void playAction()
    {
        if (!isPlaying)
        {
            if (duration == 0)
            {
                textRenderer.color = newColor;
                Complete();
            }
            else
            {
                timePassed = 0;
                startingColor = textRenderer.color;
                isPlaying = true;
            }
        }
    }
}

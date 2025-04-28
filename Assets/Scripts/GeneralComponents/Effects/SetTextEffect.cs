using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SetTextEffect : Effect
{
    [SerializeField]
    protected TMP_Text textField = null;

    [SerializeField, TextArea(3, 5)]
    protected string newText = "";

    protected override void playAction()
    {
        if (textField != null)
        {
            textField.text = newText;
        }

        Complete();
    }
}

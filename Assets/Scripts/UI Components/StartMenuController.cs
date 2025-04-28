using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class StartMenuController : MonoBehaviour
{
    public InputAction upAction;
    public InputAction downAction;
    public InputAction selectAction;

    [SerializeField]
    protected Color selectedColor = Color.white;

    [SerializeField]
    protected Color baseColor = Color.white;

    protected List<TMP_Text> menuOptions = new List<TMP_Text>();
    protected int selectedIndex = 0;

    void Start()
    {
        menuOptions = GetComponentsInChildren<TMP_Text>().ToList();

        //Debug.Log(menuOptions);

        upAction.performed += OnUp;
        downAction.performed += OnDown;
        selectAction.performed += OnSelect;

        SetTextColors();
    }

    void OnEnable()
    {
        upAction.Enable();
        downAction.Enable();
        selectAction.Enable();
    }

    void OnDisable()
    {
        upAction.Disable();
        downAction.Disable();
        selectAction.Disable();
    }

    protected void SetTextColors()
    {
        for (int i = 0; i < menuOptions.Count; i++)
        {
            if (i == selectedIndex)
            {
                menuOptions[i].color = selectedColor;
            }
            else
            {
                menuOptions[i].color = baseColor;
            }
        }
    }

    protected void OnUp(InputAction.CallbackContext context)
    {
        selectedIndex = (selectedIndex + 1) % menuOptions.Count;
        SetTextColors();
    }

    protected void OnDown(InputAction.CallbackContext context)
    {
        selectedIndex--;
        if (selectedIndex < 0)
            selectedIndex = menuOptions.Count - 1;

        SetTextColors();
    }

    protected void OnSelect(InputAction.CallbackContext context)
    {
        menuOptions[selectedIndex].GetComponent<Effect>()?.Play();
    }
}

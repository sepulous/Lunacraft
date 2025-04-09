using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ModeButton : MonoBehaviour
{
    public Sprite disabledSprite;
    public Sprite enabledSprite;
    public GameObject otherModeButton;
    public bool creativeMode = false;
    public GameObject modeDescription;
    private bool enabled = false;

    void Awake()
    {
        enabled = creativeMode;
    }

    public void ToggleEnabled()
    {
        SetEnabled(!enabled);
        otherModeButton.GetComponent<ModeButton>().SetEnabled(false);
    }

    public void SetEnabled(bool val)
    {
        enabled = val;
        if (enabled)
        {
            gameObject.GetComponent<Image>().sprite = enabledSprite;
            if (creativeMode)
                modeDescription.GetComponent<Text>().text = "Create without limits, safely";
            else
                modeDescription.GetComponent<Text>().text = "Survive on an alien moon";
        }
        else
        {
            gameObject.GetComponent<Image>().sprite = disabledSprite;
        }
    }

    public bool IsEnabled()
    {
        return enabled;
    }
}

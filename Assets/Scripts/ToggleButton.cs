using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToggleButton : MonoBehaviour
{
    public Sprite disabledSprite;
    public Sprite enabledSprite;

    private bool enabled = false;

    public void ToggleEnabled()
    {
        enabled = !enabled;
        if (enabled)
            gameObject.GetComponent<Image>().sprite = enabledSprite;
        else
            gameObject.GetComponent<Image>().sprite = disabledSprite;
    }

    public void SetEnabled(bool val)
    {
        if (val != enabled)
            ToggleEnabled();
    }

    public bool IsEnabled()
    {
        return enabled;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleMenuElement : MonoBehaviour
{
    public void ToggleActive()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }
}

using UnityEngine;

public class HelpButton : MonoBehaviour
{
    public void OpenWiki()
    {
        Application.OpenURL("https://mooncraft.fandom.com/wiki/Lunacraft_Wiki");
    }

    public void Quit() // I know it doesn't belong here, but fuck it
    {
        Application.Quit();
    }
}

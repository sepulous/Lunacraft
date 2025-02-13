using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuBG : MonoBehaviour
{
    private int bgIndex = 0;
    public GameObject[] bgObjects;

    void Start()
    {
        StartCoroutine(Animate());
    }

    IEnumerator Animate()
    {
        while (true)
        {
            GameObject currentImage = bgObjects[bgIndex];
            RectTransform currentTransform = currentImage.GetComponent<RectTransform>();
            Vector3 currentScale = currentTransform.localScale;
            Color currentColor = currentImage.GetComponent<Image>().color;

            currentTransform.localScale = new Vector3(currentScale.x * 1.0002F, currentScale.y * 1.0002F, currentScale.z); // Scale current image up

            if (currentTransform.localScale.x > 1.2F)
            {
                GameObject nextImage = bgObjects[(bgIndex + 1) % bgObjects.Length];
                RectTransform nextTransform = nextImage.GetComponent<RectTransform>();
                Vector3 nextScale = nextTransform.localScale;
                Color nextColor = nextImage.GetComponent<Image>().color;
                
                currentImage.GetComponent<Image>().color = new Color(currentColor.r, currentColor.g, currentColor.b, currentColor.a - 0.01F); // Lower current image opacity
                nextTransform.localScale = new Vector3(nextScale.x * 1.0002F, nextScale.y * 1.0002F, nextScale.z); // Scale next image up
                if (currentImage.GetComponent<Image>().color.a < 0.01F)
                {
                    // Move current image behind, reset scale, load next image
                    currentImage.GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
                    currentImage.transform.SetSiblingIndex(0);
                    currentImage.GetComponent<Image>().color = new Color(currentColor.r, currentColor.g, currentColor.b, 1);
                    bgIndex = (bgIndex + 1) % bgObjects.Length;
                    continue;
                }
            }

            yield return new WaitForSeconds(0.5F * Time.fixedDeltaTime);
        }
    }
}

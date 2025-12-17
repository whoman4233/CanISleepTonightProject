using UnityEngine;
using UnityEngine.UI;

public class CrosshairController : MonoBehaviour
{
    [SerializeField] private RectTransform crosshair;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color interactColor = Color.yellow;
    [SerializeField] private float interactScale = 1.3f;

    public void SetInteractable(bool isInteractable)
    {
        crosshair.localScale = isInteractable ? Vector3.one * interactScale : Vector3.one;

        crosshair.GetComponent<Image>().color = isInteractable ? interactColor : normalColor;
    }
}

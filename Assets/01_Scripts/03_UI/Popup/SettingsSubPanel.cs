using UnityEngine;

public class SettingsSubPanel : MonoBehaviour
{
    [SerializeField] private GameObject categoryRoot;

    public void BackToCategory()
    {
        gameObject.SetActive(false);

        if (categoryRoot != null)
            categoryRoot.SetActive(true);
    }
}

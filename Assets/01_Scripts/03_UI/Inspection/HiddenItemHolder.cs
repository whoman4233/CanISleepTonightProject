using UnityEngine;

public class HiddenItemHolder : MonoBehaviour
{
    [SerializeField] private HiddenItemStateSO[] hiddenItems;

    private HiddenItemStateSO[] runtimeItems;

    private void Awake()
    {
        runtimeItems = new HiddenItemStateSO[hiddenItems.Length];

        for (int i = 0; i < hiddenItems.Length; i++)
        {
            runtimeItems[i] = Instantiate(hiddenItems[i]);

            if (runtimeItems[i] is KnifeStateSO knife)
            {
                knife.ResetState();
            }
        }
    }

    public T GetItem<T>() where T : HiddenItemStateSO
    {
        if (runtimeItems == null)
            return null;

        foreach (var item in runtimeItems)
        {
            if (item is T typed)
                return typed;
        }

        return null;
    }
}



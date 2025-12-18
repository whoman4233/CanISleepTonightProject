using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class InspectionHelper
{
    public static void EnterInspection(Player player, IInspectable target)
    {
        var manager = player.GetComponentInChildren<InspectionManager>();
        if (manager == null)
        {
            Debug.LogError("InspectionManager not found");
            return;
        }

        manager.EnterInspection(target);
    }
}


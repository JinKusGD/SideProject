using UnityEngine;

public static class UnityUtility
{
    public static bool CheckReference(Object reference, string fieldName, Object owner)
    {
        if (reference != null)
        {
            return true;
        }

        DebugUtility.LogError($"{owner.GetType().Name}: {fieldName} is not assigned.");
        return false;
    }
}

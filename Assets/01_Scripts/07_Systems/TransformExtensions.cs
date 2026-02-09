using UnityEngine;
public static class TransformExtensions
{
    public static Transform FindDeepChild(this Transform parent, string name)
    {
        // 1. 먼저 직계 자식 중에서 찾기
        Transform result = parent.Find(name);
        if (result != null) return result;

        // 2. 못 찾았다면, 자식들의 자식들까지 재귀(Recursive)적으로 탐색
        foreach (Transform child in parent)
        {
            result = child.FindDeepChild(name);
            if (result != null) return result;
        }

        return null;
    }
}

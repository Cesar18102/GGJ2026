using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Helpers
{
    public static class GameObjectExtensions
    {
        public static GameObject[] GetChildrenWithTag(this GameObject parent, string tag)
        {
            List<GameObject> result = new List<GameObject>();
            Transform t = parent.transform;

            for (int i = 0; i < t.childCount; i++)
            {
                if (t.GetChild(i).gameObject.tag == tag)
                {
                    result.Add(t.GetChild(i).gameObject);
                }
            }

            return result.ToArray();
        }
    }
}

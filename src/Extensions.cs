using UnityEngine;

namespace EFM
{
    public static class Extensions
    {
        public static T GetComponentInParents<T>(this Component c, int steps = -1)
        {
            Transform current = c.transform;
            T t = default(T);
            int step = 0;

            do
            {
                t = c.GetComponent<T>();
                current = current.parent;
            }
            while (t == null && current != null && (steps == -1 || step++ < steps));

            return t;
        }
    }
}

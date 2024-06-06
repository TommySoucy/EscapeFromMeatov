using UnityEngine;

namespace EFM
{
    public static class Extensions
    {
        /// <summary>
        /// Alternative to GetComponentInParent
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="c">The calling component</param>
        /// <param name="checkCurrent">Whether we want to check the calling component as well</param>
        /// <param name="steps">How high up the hierarchy we want to go. 0 is first parent</param>
        /// <returns></returns>
        public static T GetComponentInParents<T>(this Component c, bool checkCurrent = true, int steps = -1)
        {
            Transform current = checkCurrent ? c.transform : c.transform.parent;
            T t = default(T);
            if (current == null)
            {
                return t;
            }

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

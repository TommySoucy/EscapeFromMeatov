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
        /// <returns></returns>
        public static T GetComponentInParents<T>(this Component c, bool checkCurrent = true)
        {
            if (checkCurrent)
            {
                return c.GetComponentInParent<T>();
            }
            else
            {
                if(c.transform.parent == null)
                {
                    return default(T);
                }
                else
                {
                    return c.transform.parent.GetComponentInParent<T>();
                }
            }
        }
    }
}

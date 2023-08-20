using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EFM
{
    public class DestroyTimer : MonoBehaviour
    {
        public float timer;
        public void Update()
        {
            timer-= Time.deltaTime;
            if(timer <= 0)
            {
                Destroy(gameObject);
            }
        }
    }
}

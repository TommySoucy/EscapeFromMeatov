using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EFM
{
    public class EFM_DamageVolume : MonoBehaviour
    {
        public AudioSource audioSource;
        public AudioClip[] audioClips;
        public float damage = 5;
        public float damageDistance = 0.1f;

        public List<Collider> headColliders;
        public List<float> headColliderDistances;
        public List<Vector3> headColliderPreviousPos;

        public List<Collider> handColliders;
        public List<float> handColliderDistances;
        public List<Vector3> handColliderPreviousPos;

        public void Init()
        {
            audioSource = GetComponent<AudioSource>();
            if (gameObject.name.Equals("bruno_helix"))
            {
                audioClips = Mod.barbedWireClips;
                this.damage = 5;
                this.damageDistance = 0.1f;
            }
            else
            {
                Mod.instance.LogWarning("Unknown damage volume type, will have no audio");
            }
        }

        public void OnTriggerEnter(Collider other)
        {
            Mod.instance.LogInfo("Damage volume, on trigger enter: " + other.name);
            if (headColliders == null)
            {
                headColliders = new List<Collider>();
                headColliderDistances = new List<float>();
                headColliderPreviousPos = new List<Vector3>();

                handColliders = new List<Collider>();
                handColliderDistances = new List<float>();
                handColliderPreviousPos = new List<Vector3>();
            }

            if (other.gameObject.layer == 15) // PlayerHead (includes torso, head, and neck)
            {
                headColliders.Add(other);
                headColliderDistances.Add(0);
                headColliderPreviousPos.Add(other.transform.position);
            }
            else if(other.gameObject.layer == 9) // HandTrigger
            {
                handColliders.Add(other);
                handColliderDistances.Add(0);
                handColliderPreviousPos.Add(other.transform.position);
            }
        }

        public void OnTriggerStay(Collider other)
        {
            if (other.gameObject.layer == 15)
            {
                for (int i = 0; i < headColliders.Count; ++i)
                {
                    if (headColliders[i] == other)
                    {
                        headColliderDistances[i] += Vector3.Distance(headColliderPreviousPos[i], other.transform.position);
                        while (headColliderDistances[i] > damageDistance)
                        {
                            headColliderDistances[i] -= damageDistance;
                            object[] damageData = DamagePatch.Damage(UnityEngine.Random.value * damage, other.GetComponent<FVRPlayerHitbox>());
                            DamagePatch.RegisterPlayerHit((int)damageData[0], (float)damageData[1], false);
                            if(audioClips != null)
                            {
                                audioSource.PlayOneShot(audioClips[UnityEngine.Random.Range(0, audioClips.Length + 1)], 0.4f);
                            }
                        }
                        headColliderPreviousPos[i] = other.transform.position;
                        break;
                    }
                }
            }
            else if(other.gameObject.layer == 9)
            {
                for (int i = 0; i < handColliders.Count; ++i)
                {
                    if (handColliders[i] == other)
                    {
                        handColliderDistances[i] += Vector3.Distance(handColliderPreviousPos[i], other.transform.position);
                        while (handColliderDistances[i] > 0.1f)
                        {
                            handColliderDistances[i] -= damageDistance;
                            object[] damageData = DamagePatch.Damage(UnityEngine.Random.value * damage, null, UnityEngine.Random.Range(3, 5));
                            DamagePatch.RegisterPlayerHit((int)damageData[0], (float)damageData[1], false);
                            if (audioClips != null)
                            {
                                audioSource.PlayOneShot(audioClips[UnityEngine.Random.Range(0, audioClips.Length + 1)], 0.4f);
                            }
                        }
                        handColliderPreviousPos[i] = other.transform.position;
                        break;
                    }
                }
            }
        }

        public void OnTriggerExit(Collider other)
        {
            Mod.instance.LogInfo("Damage volume, on trigger exit: " + other.name);
            if (other.gameObject.layer == 15)
            {
                for (int i = 0; i < headColliders.Count; ++i)
                {
                    if (headColliders[i] == other)
                    {
                        headColliders.RemoveAt(i);
                        headColliderDistances.RemoveAt(i);
                        headColliderPreviousPos.RemoveAt(i);
                        break;
                    }
                }
            }
            else if (other.gameObject.layer == 9)
            {
                for (int i = 0; i < handColliders.Count; ++i)
                {
                    if (handColliders[i] == other)
                    {
                        handColliders.RemoveAt(i);
                        handColliderDistances.RemoveAt(i);
                        handColliderPreviousPos.RemoveAt(i);
                        break;
                    }
                }
            }
        }
    }
}

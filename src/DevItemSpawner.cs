using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Valve.Newtonsoft.Json.Linq;

namespace EFM
{
    public class DevItemSpawner : MonoBehaviour
    {
        public static DevItemSpawner instance;

        public Transform listParent;
        public GameObject itemEntryPrefab;
        public Text text;
        public GameObject nextButton;
        public GameObject previousButton;

        public int index;
        public List<MeatovItemData> items;

        public void Awake()
        {
            instance = this;
        }

        public void Search()
        {
            // Build new list
            index = 0;
            BuildList(text.text);

            // Add entries
            DisplayList();
        }

        public void BuildList(string s)
        {
            // Reset items list
            if (items == null)
            {
                items = new List<MeatovItemData>();
            }
            else
            {
                items.Clear();
            }

            foreach (KeyValuePair<string, MeatovItemData> defaultItemDataEntry in Mod.defaultItemData)
            {
                if (defaultItemDataEntry.Key.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0 || defaultItemDataEntry.Value.name.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0
                    || defaultItemDataEntry.Value.H3ID.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0 || (defaultItemDataEntry.Value.modGroup != null && defaultItemDataEntry.Value.modGroup.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0)
                    || (defaultItemDataEntry.Value.modPart != null && defaultItemDataEntry.Value.modPart.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    items.Add(defaultItemDataEntry.Value);
                }
            }
        }

        public void DisplayList()
        {
            // Clear previous entries
            while (listParent.childCount > 1)
            {
                Transform currentChild = listParent.GetChild(1);
                currentChild.parent = null;
                DestroyImmediate(currentChild.gameObject);
            }

            // Create new entries
            for(int i=0; i<8; ++i)
            {
                if(index + i >= items.Count)
                {
                    break;
                }
                MeatovItemData data = items[index+i];
                DevItemSpawnerEntry entry = Instantiate(itemEntryPrefab, listParent).GetComponent<DevItemSpawnerEntry>();
                entry.text.text = data.name;
                entry.item = data;
                entry.gameObject.SetActive(true);
            }

            // Set buttons
            nextButton.SetActive(index + 8 < items.Count);
            previousButton.SetActive(false);
        }

        public void Close()
        {
            Destroy(gameObject);
        }

        public void Next()
        {
            index += 8;
            nextButton.SetActive(index + 8 < items.Count);
            previousButton.SetActive(true);

            DisplayList();
        }

        public void Previous()
        {
            index -= 8;
            nextButton.SetActive(true);
            previousButton.SetActive(index - 8 >= 0);

            DisplayList();
        }
    }
}

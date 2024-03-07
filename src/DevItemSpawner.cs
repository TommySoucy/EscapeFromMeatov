using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

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

            // Set buttons
            nextButton.SetActive(true);
            previousButton.SetActive(false);
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

            // Handle custom ID
            int parsedID = 0;
            if(int.TryParse(s, out parsedID))
            {
                if(parsedID < Mod.customItemData.Length && Mod.customItemData[parsedID] != null)
                {
                    items.Add(Mod.customItemData[parsedID]);
                }
            }

            // Handle vanilla IDs and names
            for(int i = 0; i < Mod.customItemData.Length; ++i)
            {
                if(Mod.customItemData[i] != null && Mod.customItemData[i].name.IndexOf(text.text, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    items.Add(Mod.customItemData[i]);
                }
            }
            foreach(KeyValuePair<string, MeatovItemData> dataEntry in Mod.vanillaItemData)
            {
                if (dataEntry.Value != null && (dataEntry.Key.IndexOf(text.text, StringComparison.OrdinalIgnoreCase) >= 0 || dataEntry.Value.name.IndexOf(text.text, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    items.Add(dataEntry.Value);
                }
            }
        }

        public void DisplayList()
        {
            // Clear previous entries
            while (listParent.childCount > 1)
            {
                DestroyImmediate(listParent.GetChild(1).gameObject);
            }

            // Create new entries
            for(int i=0; i<8; ++i)
            {
                if(index + i > items.Count)
                {
                    break;
                }
                MeatovItemData data = items[index+i];
                DevItemSpawnerEntry entry = Instantiate(itemEntryPrefab, listParent).GetComponent<DevItemSpawnerEntry>();
                entry.text.text = data.name;
                entry.item = data;
            }
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
        }

        public void Previous()
        {
            index -= 8;
            nextButton.SetActive(true);
            previousButton.SetActive(index - 8 >= 0);
        }
    }
}

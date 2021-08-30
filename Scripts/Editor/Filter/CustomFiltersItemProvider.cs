using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ConsoleTiny
{
    internal class CustomFiltersItemProvider : IFlexibleMenuItemProvider
    {
        private readonly EntryWrapped.CustomFiltersGroup m_Groups;

        public CustomFiltersItemProvider(EntryWrapped.CustomFiltersGroup groups)
        {
            m_Groups = groups;
        }

        public int Count()
        {
            return m_Groups.filters.Count;
        }

        public object GetItem(int index)
        {
            return m_Groups.filters[index];
        }

        public int Add(object obj)
        {
            m_Groups.filters.Add(new EntryWrapped.CustomFiltersItem() { filter = (string)obj, changed = false });
            m_Groups.Save();
            return Count() - 1;
        }

        public void Replace(int index, object newPresetObject)
        {
            m_Groups.filters[index].filter = (string)newPresetObject;
            m_Groups.Save();
        }

        public void Remove(int index)
        {
            if (m_Groups.filters[index].toggle)
            {
                m_Groups.changed = true;
            }
            m_Groups.filters.RemoveAt(index);
            m_Groups.Save();
        }

        public object Create()
        {
            return "log";
        }

        public void Move(int index, int destIndex, bool insertAfterDestIndex)
        {
            Debug.LogError("Missing impl");
        }

        public string GetName(int index)
        {
            return m_Groups.filters[index].filter;
        }

        public bool IsModificationAllowed(int index)
        {
            return true;
        }

        public int[] GetSeperatorIndices()
        {
            return new int[0];
        }
    }
}

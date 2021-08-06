using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;



internal class CustomFiltersModifyItemUI : FlexibleMenuModifyItemUI
{
    private static class Styles
    {
        public static GUIContent headerAdd = EditorGUIUtility.TextContent("Add");
        public static GUIContent headerEdit = EditorGUIUtility.TextContent("Edit");
        public static GUIContent optionalText = EditorGUIUtility.TextContent("Search");
        public static GUIContent ok = EditorGUIUtility.TextContent("OK");
        public static GUIContent cancel = EditorGUIUtility.TextContent("Cancel");
    }

    private string m_TextSearch;

    public override void OnClose()
    {
        m_TextSearch = null;
        base.OnClose();
    }

    public override Vector2 GetWindowSize()
    {
        return new Vector2(330f, 80f);
    }

    public override void OnGUI(Rect rect)
    {
        string itemValue = m_Object as string;
        if (itemValue == null)
        {
            Debug.LogError("Invalid object");
            return;
        }

        if (m_TextSearch == null)
        {
            m_TextSearch = itemValue;
        }

        const float kColumnWidth = 70f;
        const float kSpacing = 10f;

        GUILayout.Space(3);
        GUILayout.Label(m_MenuType == MenuType.Add ? Styles.headerAdd : Styles.headerEdit, EditorStyles.boldLabel);

        Rect seperatorRect = GUILayoutUtility.GetRect(1, 1);
        FlexibleMenu.DrawRect(seperatorRect,
            (EditorGUIUtility.isProSkin)
                ? new Color(0.32f, 0.32f, 0.32f, 1.333f)
                : new Color(0.6f, 0.6f, 0.6f, 1.333f));                      // dark : light
        GUILayout.Space(4);

        // Optional text
        GUILayout.BeginHorizontal();
        GUILayout.Label(Styles.optionalText, GUILayout.Width(kColumnWidth));
        GUILayout.Space(kSpacing);
        m_TextSearch = EditorGUILayout.TextField(m_TextSearch);
        GUILayout.EndHorizontal();

        GUILayout.Space(5f);

        // Cancel, Ok
        GUILayout.BeginHorizontal();
        GUILayout.Space(10);
        if (GUILayout.Button(Styles.cancel))
        {
            editorWindow.Close();
        }

        if (GUILayout.Button(Styles.ok))
        {
            var textSearch = m_TextSearch.Trim();
            if (!string.IsNullOrEmpty(textSearch))
            {
                m_Object = m_TextSearch;
                Accepted();
                editorWindow.Close();
            }
        }
        GUILayout.Space(10);
        GUILayout.EndHorizontal();
    }
}

using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;

public class ComboSelectorAttribute : PropertyAttribute {}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ComboSelectorAttribute))]
public class ComboSelectorDrawer : PropertyDrawer
{
    private const float VerticalSpacing = 2f;
    private readonly float LineHeight = EditorGUIUtility.singleLineHeight;
    

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        Rect buttonRect = new(position.x, position.y, position.width, LineHeight);

        string text = property.managedReferenceValue == null ? "Select Combo Type" : property.managedReferenceValue.GetType().Name;

        if (GUI.Button(buttonRect, text))
        {
            GenericMenu menu = new();

            Type baseType = typeof(SlideCombo);

            Type[] types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).Where(t => baseType.IsAssignableFrom(t) && !t.IsAbstract).ToArray();

            foreach (Type type in types)
            {
                menu.AddItem(new GUIContent(type.Name), false, () =>
                {
                    property.managedReferenceValue = Activator.CreateInstance(type);
                    property.isExpanded = true;
                    property.serializedObject.ApplyModifiedProperties();
                });
            }

            menu.ShowAsContext();
        }

        if (property.managedReferenceValue != null && property.isExpanded)
        {
            Rect childRect = new(position.x, position.y + LineHeight + VerticalSpacing, position.width, position.height - LineHeight );

            EditorGUI.indentLevel++;

            SerializedProperty iterator = property.Copy();
            SerializedProperty end = iterator.GetEndProperty();

            iterator.NextVisible(true);
            while (!SerializedProperty.EqualContents(iterator, end))
            {
                float height = EditorGUI.GetPropertyHeight(iterator, true);
                Rect r = new(childRect.x, childRect.y, childRect.width, height);

                EditorGUI.PropertyField(r, iterator, true);
                childRect.y += height + VerticalSpacing;

                iterator.NextVisible(false);
            }

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = LineHeight;

        if (property.managedReferenceValue != null && property.isExpanded)
        {
            SerializedProperty iterator = property.Copy();
            SerializedProperty end = iterator.GetEndProperty();

            iterator.NextVisible(true);

            while (!SerializedProperty.EqualContents(iterator, end))
            {
                height += EditorGUI.GetPropertyHeight(iterator, true) + VerticalSpacing;
                iterator.NextVisible(false);
            }
        }

        return height;
    }
}
#endif

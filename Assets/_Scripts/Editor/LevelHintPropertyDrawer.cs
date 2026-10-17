using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(LevelControlHintDisplay.LevelHint))]
public sealed class LevelHintPropertyDrawer : PropertyDrawer
{
    private const float Spacing = 2f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight;

        SerializedProperty useDeviceSpecificText = property.FindPropertyRelative("useDeviceSpecificText");
        float height = EditorGUIUtility.singleLineHeight * 3f + Spacing * 3f;

        if (useDeviceSpecificText.boolValue)
        {
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("keyboardText"), true) + Spacing;
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("gamepadText"), true);
        }
        else
        {
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("text"), true);
        }

        return height;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        Rect row = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(row, property.isExpanded, label, true);
        if (!property.isExpanded)
        {
            EditorGUI.EndProperty();
            return;
        }

        EditorGUI.indentLevel++;
        row.y += EditorGUIUtility.singleLineHeight + Spacing;
        SerializedProperty levelName = property.FindPropertyRelative("levelName");
        EditorGUI.PropertyField(row, levelName, new GUIContent("关卡名"));

        row.y += EditorGUIUtility.singleLineHeight + Spacing;
        SerializedProperty useDeviceSpecificText = property.FindPropertyRelative("useDeviceSpecificText");
        EditorGUI.PropertyField(row, useDeviceSpecificText, new GUIContent("按设备区分"));

        row.y += EditorGUIUtility.singleLineHeight + Spacing;
        if (useDeviceSpecificText.boolValue)
        {
            DrawTextField(ref row, property.FindPropertyRelative("keyboardText"), "键鼠提示");
            DrawTextField(ref row, property.FindPropertyRelative("gamepadText"), "手柄提示");
        }
        else
        {
            DrawTextField(ref row, property.FindPropertyRelative("text"), "通用提示");
        }

        EditorGUI.indentLevel--;
        EditorGUI.EndProperty();
    }

    private static void DrawTextField(ref Rect row, SerializedProperty property, string label)
    {
        float height = EditorGUI.GetPropertyHeight(property, true);
        row.height = height;
        EditorGUI.PropertyField(row, property, new GUIContent(label), true);
        row.y += height + Spacing;
    }
}

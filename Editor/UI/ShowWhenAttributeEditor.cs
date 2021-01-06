using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SpritesheetImporter {

    [CustomPropertyDrawer(typeof(ShowWhenAttribute))]
    public class ShowWhenAttributeEditor : PropertyDrawer {

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            if (ShouldBeShown(property)) {
                EditorGUI.PropertyField(position, property, label);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            if (ShouldBeShown(property)) {
                return EditorGUI.GetPropertyHeight(property, label);
            }
            else {
                // Negative value: undo the spacing which would be here from this field
                return -EditorGUIUtility.standardVerticalSpacing;
            }
        }

        private bool ShouldBeShown(SerializedProperty property) {
            ShowWhenAttribute showWhenAttribute = attribute as ShowWhenAttribute;

            if (showWhenAttribute.OtherPropertyValue != null && showWhenAttribute.OtherPropertyValues != null) {
                throw new System.Exception("Both OtherPropertyValue and OtherPropertyValues are set in a [ShowWhen] attribute, which is not valid");
            }

            if (showWhenAttribute.OtherPropertyValue == null && showWhenAttribute.OtherPropertyValues == null) {
                throw new System.Exception("Neither one of OtherPropertyValue and OtherPropertyValues are set in a [ShowWhen] attribute, which is not valid");
            }

            object[] allowedValues = showWhenAttribute.OtherPropertyValues ?? new object[] { showWhenAttribute.OtherPropertyValue };

            object targetObject = property.serializedObject.targetObject;
            object dependentPropertyValue = targetObject.GetType().GetFieldOrPropertyValue(showWhenAttribute.OtherPropertyPath, targetObject);

            if (dependentPropertyValue == null) {
                // null is never allowed to match anything
                return false;
            }

            for (var i = 0; i < allowedValues.Length; i++) {
                if (dependentPropertyValue.Equals(allowedValues[i])) {
                    return true;
                }
            }

            return false;
        }
    }
}
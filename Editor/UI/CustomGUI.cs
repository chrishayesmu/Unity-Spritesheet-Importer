using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SpritesheetImporter {

    public static class CustomGUI
    {
        public static void DrawTexture(Rect rect, Texture2D texture, string labelBeneath = null) {
            GUIStyle miniLabelStyle = new GUIStyle(EditorStyles.miniLabel) {
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Overflow
            };

            float reservedLabelHeight = labelBeneath != null ? EditorGUIUtility.singleLineHeight : 0.0f;
            Rect baseTextureRect = new Rect(rect.x, rect.y, rect.width, rect.height - reservedLabelHeight);
            EditorGUI.DrawTextureTransparent(baseTextureRect, texture, ScaleMode.ScaleToFit);

            if (labelBeneath != null) {
                using (new EditorGUIIndentOverride(0)) {
                    // For some reason LabelField respects the indent level even though we're passing it the rect to use
                    Rect baseTextureLabelRect = new Rect(baseTextureRect.xMin, baseTextureRect.yMax, baseTextureRect.width, EditorGUIUtility.singleLineHeight);
                    EditorGUI.LabelField(baseTextureLabelRect, labelBeneath, miniLabelStyle);
                }
            }
        }
    }
}

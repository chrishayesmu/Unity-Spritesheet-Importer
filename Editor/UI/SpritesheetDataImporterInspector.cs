using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

namespace SpritesheetImporter {
    [CustomEditor(typeof(SpritesheetDataImporter))]
    internal class SpritesheetDataImporterInspector : ScriptedImporterEditor {
        public override void OnInspectorGUI() {
            DrawDefaultInspector();
            EditorGUILayout.Space();

            var refreshIcon = EditorGUIUtility.IconContent("d_refresh").image;
            GUIContent buttonContent = new GUIContent(" Reset to Project Defaults", refreshIcon, "tooltip");
            if (GUILayout.Button(buttonContent)) {
                var importer = target as SpritesheetDataImporter;
                importer.ReloadFromDefaults();
            }

            base.ApplyRevertGUI();
        }
    }
}

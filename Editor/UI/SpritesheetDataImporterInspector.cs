using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

namespace SpritesheetImporter {
    [CustomEditor(typeof(SpritesheetDataImporter))]
    internal class SpritesheetDataImporterInspector : ScriptedImporterEditor {
        public override void OnInspectorGUI() {
            SpritesheetDataImporter importer = target as SpritesheetDataImporter;

            serializedObject.Update();

            // Animations
            EditorGUILayout.PropertyField(serializedObject.FindProperty("createAnimations"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("placeAnimationsInSubfolders"));

            // Slicing
            EditorGUILayout.PropertyField(serializedObject.FindProperty("trimIndividualSprites"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("trimAlphaThreshold"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sliceSecondaryTextures"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sliceUnidentifiedTextures"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("subdivideSprites"));

            if (importer.subdivideSprites) {
                using (new EditorGUILayout.HorizontalScope()) {
                    EditorGUILayout.PrefixLabel("Subdivisions");
                    using (new EditorGUILayout.VerticalScope()) {
                        EditorGUI.BeginChangeCheck();
                        using (new EditorGUILayout.HorizontalScope()) {
                            importer.subdivisions.y = EditorGUILayout.IntField(Mathf.Max(importer.subdivisions.y, 0));
                            EditorGUILayout.LabelField("rows");
                        }

                        using (new EditorGUILayout.HorizontalScope()) {
                            importer.subdivisions.x = EditorGUILayout.IntField(Mathf.Max(importer.subdivisions.x, 0));
                            EditorGUILayout.LabelField("columns");
                        }

                        if (EditorGUI.EndChangeCheck()) {
                            EditorUtility.SetDirty(target);
                        }
                    }
                }

                SpritesheetData data = AssetDatabase.LoadAssetAtPath<SpritesheetData>(importer.assetPath);

                if (data != null) {
                    int rowRemainder = data.spriteHeight % importer.subdivisions.y;
                    int colRemainder = data.spriteWidth % importer.subdivisions.x;

                    if (rowRemainder != 0 || colRemainder != 0) {
                        EditorGUILayout.HelpBox($"Chosen subdivision values do not divide evenly into the sprite size of {data.spriteWidth}x{data.spriteHeight}. " +
                                                $"There will be a remainder of {rowRemainder} pixels per row and {colRemainder} pixels per column.", MessageType.Warning);
                    }

                    if (data.animations != null && data.animations.Count > 0) {
                        EditorGUILayout.HelpBox("Subdivisions will not be applied to animations; animations will use the full sprite as defined in the ssdata file.", MessageType.Warning);
                    }
                }
                else {
                    Debug.Log($"Data is null for asset path {importer.assetPath}");
                }
            }

            // Pivots
            EditorGUILayout.PropertyField(serializedObject.FindProperty("pivotPlacement"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("customPivotMode"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("tilemapGridSize"));

            EditorGUILayout.Space();

            // Functionality
            var refreshIcon = EditorGUIUtility.IconContent("d_refresh").image;
            GUIContent buttonContent = new GUIContent(" Reset to Project Defaults", refreshIcon, "Resets many import settings to their defaults found in Project Settings. " +
                                                                                                 "Not all settings have defaults, so some won't be changed.");
            if (GUILayout.Button(buttonContent)) {
                importer.ReloadFromDefaults();
            }

            serializedObject.ApplyModifiedProperties();
            base.ApplyRevertGUI();
        }
    }
}

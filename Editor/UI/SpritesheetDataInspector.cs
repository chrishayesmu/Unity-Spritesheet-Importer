﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// TODO make a CustomPreview class with options to pick material, animation, etc
namespace SpritesheetImporter {
    [CustomEditor(typeof(SpritesheetData))]
    internal class SpritesheetDataInspector : Editor {
        private static bool expandAnimations = false;
        private static bool expandMaterials = true;

        private const float texturePreviewWidth = 115.0f;
        private const float texturePreviewHeight = 115.0f;

        // Texture caches so that we don't reload every time we draw the inspector.
        // This won't get out of sync, since even if an asset is changed outside of Unity
        // and a reimport is triggered, the inspector is recreated and the cache erased.
        private Dictionary<string, Texture2D> textureAssetCache; // cache based on asset path
        private Dictionary<Hash128, Texture2D> trimmedTextureCache; // cache based on texture asset's hash

        public SpritesheetDataInspector() {
            textureAssetCache = new Dictionary<string, Texture2D>();
            trimmedTextureCache = new Dictionary<Hash128, Texture2D>();
        }

        public override void OnInspectorGUI() {
            GUI.enabled = true;
            SpritesheetData data = target as SpritesheetData;

            string assetPath = AssetDatabase.GetAssetPath(target);
            string assetDirectory = Path.GetDirectoryName(assetPath);
            SpritesheetDataImporter importer = AssetImporter.GetAtPath(assetPath) as SpritesheetDataImporter;

            GUIStyle labelStyle = new GUIStyle(EditorStyles.label) {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Overflow
            };

            GUIStyle miniLabelStyle = new GUIStyle(EditorStyles.miniLabel) {
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Overflow
            };

            if (importer.subdivideSprites) {
                // Sprite size
                int columnWidth = data.spriteWidth / importer.subdivisions.x;
                int columnRemainder = data.spriteWidth % importer.subdivisions.x;
                int rowHeight = data.spriteHeight / importer.subdivisions.y;
                int rowRemainder = data.spriteHeight % importer.subdivisions.y;

                EditorGUILayout.LabelField("Sprite Size", $"{data.spriteWidth}x{data.spriteHeight} px (base)");
                EditorGUILayout.LabelField(" ", $"{columnWidth}x{rowHeight} px (after subdividing)");

                // Sheet size (# of sprites)
                int totalColumns = importer.subdivisions.x * data.numColumns;
                int totalRows = importer.subdivisions.y * data.numRows;

                EditorGUILayout.LabelField("Sheet Size", $"{data.numRows} rows by {data.numColumns} columns (base)");
                EditorGUILayout.LabelField(" ", $"{totalRows} rows by {totalColumns} columns (after subdividing)");
            }
            else {
                EditorGUILayout.LabelField("Sprite Size", $"{data.spriteWidth}x{data.spriteHeight} px");
                EditorGUILayout.LabelField("Sheet Size", $"{data.numRows} rows by {data.numColumns} columns");
            }

            EditorGUILayout.LabelField("Image Padding", $"{data.paddingWidth}x{data.paddingHeight} px");

            #region Show material data
            expandMaterials = EditorGUILayout.BeginFoldoutHeaderGroup(expandMaterials, $"Materials ({data.materialData.Count})");

            if (expandMaterials) {
                using (new EditorGUI.IndentLevelScope()) {
                    for (int i = 0; i < data.materialData.Count; i++) {
                        var material = data.materialData[i];

                        using (new EditorGUILayout.HorizontalScope()) {

                            using (new EditorGUILayout.VerticalScope()) {
                                EditorGUILayout.LabelField($"Material {i + 1}", labelStyle, GUILayout.Width(50));
                                EditorGUILayout.LabelField($"Role: {material.MaterialRole}", labelStyle, GUILayout.Width(50));
                            }

                            string texturePath = Path.Combine(assetDirectory, material.file);
                            Texture2D texture = LoadTexture(texturePath);
                            Texture2D trimmedTexture = GetTrimmedTexture(texture);

                            Rect texturePreviewArea = EditorGUILayout.GetControlRect(GUILayout.Height(texturePreviewHeight + EditorGUIUtility.singleLineHeight));

                            Rect baseTextureRect = new Rect(texturePreviewArea.x + 11 * EditorGUI.indentLevel, texturePreviewArea.y, texturePreviewWidth, texturePreviewHeight);
                            CustomGUI.DrawTexture(baseTextureRect, texture, labelBeneath: $"Original ({texture.width}x{texture.height})");

                            Rect trimmedTextureRect = new Rect(baseTextureRect.xMax + 20.0f, texturePreviewArea.y, texturePreviewWidth, texturePreviewHeight);
                            CustomGUI.DrawTexture(trimmedTextureRect, trimmedTexture, labelBeneath: $"Trimmed ({trimmedTexture.width}x{trimmedTexture.height})");

                            EditorGUILayout.Separator();
                        }
                    }
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            #endregion

            #region Show animation data
            expandAnimations = EditorGUILayout.BeginFoldoutHeaderGroup(expandAnimations, $"Animations ({data.animations.Count})");

            if (expandAnimations) {
                EditorGUI.indentLevel++;

                for (int i = 0; i < data.animations.Count; i++) {
                    var animation = data.animations[i];
                    bool animationIsPartOfRotationSet = data.animations.Count(a => a.name == animation.name) > 1;

                    if (i != 0) {
                        EditorGUILayout.Space();
                    }

                    string animationLabel = animation.name;
                    if (animationIsPartOfRotationSet) {
                        animationLabel += $" ({(int) animation.rotation}°)";
                    }

                    EditorGUILayout.LabelField($"Animation {i+1}", animationLabel, EditorStyles.boldLabel);

                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField("Length", $"{animation.numFrames} frames");
                    EditorGUILayout.LabelField("Frame rate", $"{animation.frameRate} fps");
                    EditorGUILayout.LabelField("Frame skip", $"{animation.frameSkip}");
                    EditorGUILayout.LabelField("Starting frame", animation.startFrame.ToString());
                    EditorGUI.indentLevel--;
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            #endregion
        }

        private Texture2D GetTrimmedTexture(Texture2D texture) {
            Hash128 hash = texture.imageContentsHash;

            if (!trimmedTextureCache.ContainsKey(hash)) {
                trimmedTextureCache[hash] = texture.TrimmedCopy(0.0f);
            }

            return trimmedTextureCache[hash];
        }

        private Texture2D LoadTexture(string assetPath) {
            if (!textureAssetCache.ContainsKey(assetPath)) {
                textureAssetCache[assetPath] = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath).ReadableView();
            }

            return textureAssetCache[assetPath];
        }
    }
}
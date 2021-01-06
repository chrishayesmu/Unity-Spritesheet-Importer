using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SpritesheetImporter {
    internal static class UtilityMethods {
        public static object GetFieldOrPropertyValue(this Type type, string fieldOrPropertyPath, object obj) {
            return type.GetProperty(fieldOrPropertyPath)?.GetValue(obj) ?? type.GetField(fieldOrPropertyPath).GetValue(obj);
        }

        /// <summary>
        /// Creates a readable version of the given texture, unless the texture is already readable, in which case
        /// it is simply returned. The texture returned by this method should not be written to, as it may be an
        /// actual texture in use and not just a copy.
        /// </summary>
        /// <remarks>
        /// If the texture's data doesn't need to be read at runtime, this is better than marking
        /// the texture as readable, because readable textures have a performance penalty.
        ///
        /// See https://docs.unity3d.com/ScriptReference/TextureImporter-isReadable.html
        /// </remarks>
        public static Texture2D ReadableView(this Texture2D texture) {
            if (texture.isReadable) {
                return texture;
            }

            RenderTexture temporaryRenderTexture = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            Graphics.Blit(texture, temporaryRenderTexture);

            RenderTexture previousRenderTexture = RenderTexture.active;
            RenderTexture.active = temporaryRenderTexture;

            // Read the active render texture into a new Texture2D, which is readable by default
            Texture2D readableCopy = new Texture2D(texture.width, texture.height);
            readableCopy.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            readableCopy.Apply();

            RenderTexture.active = previousRenderTexture;
            RenderTexture.ReleaseTemporary(temporaryRenderTexture);

            return readableCopy;
        }

        public static Texture2D TrimmedCopy(this Texture2D texture, RectInt? cropArea = null) {
            RectInt subarea = texture.GetTrimRegion(cropArea);

            var subareaPixels = texture.GetPixels(subarea.xMin, subarea.yMin, subarea.width, subarea.height);

            Texture2D subtexture = new Texture2D(subarea.width, subarea.height);
            subtexture.SetPixels(subareaPixels);
            subtexture.Apply();

            return subtexture;
        }

        public static RectInt GetTrimRegion(this Texture2D texture, RectInt? cropArea = null) {
            Color[] pixels = texture.GetPixels();

            int bottomRow = 0;
            int topRow = texture.height - 1;
            int leftCol = 0;
            int rightCol = texture.width - 1;

            if (cropArea != null) {
                bottomRow = cropArea.Value.yMin;
                topRow = cropArea.Value.yMax - 1;
                leftCol = cropArea.Value.xMin;
                rightCol = cropArea.Value.xMax - 1;
            }

            // Row-by-row starting from the bottom
            for (int row = bottomRow; row <= topRow; row++) {
                bool isRowEmpty = true;

                for (int col = leftCol; col <= rightCol; col++) {
                    Color pixel = pixels[row * texture.width + col];

                    if (pixel.a > 0.0f) {
                        isRowEmpty = false;
                        break;
                    }
                }

                if (!isRowEmpty) {
                    bottomRow = Mathf.Max(row - 1, bottomRow);
                    break;
                }
            }

            // Row-by-row starting from the top (stopping at bottomRow)
            for (int row = topRow; row >= bottomRow; row--) {
                bool isRowEmpty = true;

                for (int col = leftCol; col <= rightCol; col++) {
                    Color pixel = pixels[row * texture.width + col];

                    if (pixel.a > 0.0f) {
                        isRowEmpty = false;
                        break;
                    }
                }

                if (!isRowEmpty) {
                    topRow = Mathf.Min(row + 1, topRow);
                    break;
                }
            }

            // Column-by-column starting from the left
            for (int col = leftCol; col <= rightCol; col++) {
                bool isColumnEmpty = true;

                for (int row = bottomRow; row <= topRow; row++) {
                    Color pixel = pixels[row * texture.width + col];

                    if (pixel.a > 0.0f) {
                        isColumnEmpty = false;
                        break;
                    }
                }

                if (!isColumnEmpty) {
                    leftCol = Mathf.Max(col - 1, leftCol);
                    break;
                }
            }

            // Column-by-column starting from the right (stopping at leftCol)
            for (int col = rightCol; col >= leftCol; col--) {
                bool isColumnEmpty = true;

                for (int row = bottomRow; row < topRow; row++) {
                    Color pixel = pixels[row * texture.width + col];

                    if (pixel.a > 0.0f) {
                        isColumnEmpty = false;
                        break;
                    }
                }

                if (!isColumnEmpty) {
                    rightCol = Mathf.Min(col + 1, rightCol);
                    break;
                }
            }

            if (rightCol == leftCol || bottomRow == topRow) {
                return new RectInt(0, 0, 0, 0);
            }

            int subareaWidth = rightCol - leftCol + 1;
            int subareaHeight = topRow - bottomRow + 1;

            return new RectInt(leftCol, bottomRow, subareaWidth, subareaHeight);
        }

        public static RectInt ToRectInt(this Rect rect) {
            return new RectInt((int) rect.x, (int) rect.y, (int) rect.width, (int) rect.height);
        }

        public static Rect ToRect(this RectInt rect) {
            return new Rect(rect.x, rect.y, rect.width, rect.height);
        }
    }

    internal class EditorGUIIndentOverride : IDisposable {
        private readonly int startingIndent;

        public EditorGUIIndentOverride(int indentLevel) {
            startingIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = indentLevel;
        }

        public void Dispose() {
            EditorGUI.indentLevel = startingIndent;
        }
    }
}

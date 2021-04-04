#if UNITY_2019_2_OR_NEWER
// 2D secondary textures were added in 2019.2: https://unity3d.com/unity/whats-new/2019.2.0
#define SECONDARY_TEXTURES_AVAILABLE
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SpritesheetImporter {

    internal class SpritesheetDataPostprocessor : AssetPostprocessor {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
            List<string> processedSpritesheets = new List<string>();

            var assets = importedAssets.Concat(movedAssets).ToList();

            foreach (string inputAssetPath in assets) {
                string assetPath = inputAssetPath.Replace('/', '\\');

                SpritesheetData spritesheetData;

                #region Find and load spritesheet data file
                // Regardless of whether an .ssdata file or an image file is being imported, we try to load the
                // spritesheet data and process everything it references, to keep all of the files in sync
                if (assetPath.EndsWith(".ssdata")) {
                    spritesheetData = LoadSpritesheetDataFile(assetPath);
                }
                else {
                    // Check if this is a texture which may have a .ssdata associated
                    if (AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath) == null) {
                        continue;
                    }

                    spritesheetData = FindSpritesheetData(assetPath);
                }
                #endregion

                if (spritesheetData == null) {
                    continue;
                }


                // When loading in several images that represent a texture and its secondary textures, we want to avoid
                // repeating work, so we only process each spritesheet data file once per import
                if (processedSpritesheets.Contains(spritesheetData.dataFilePath)) {
                    Log($"Spritesheet at path \"{spritesheetData.dataFilePath} has already been processed in this import operation; skipping", LogLevel.Verbose);
                    continue;
                }

                processedSpritesheets.Add(spritesheetData.dataFilePath);

                var spritesheetImporter = AssetImporter.GetAtPath(spritesheetData.dataFilePath) as SpritesheetDataImporter;

                if (spritesheetImporter == null) {
                    Log($"SpritesheetDataImporter for asset \"{inputAssetPath}\" unavailable; it's probably running later in this import operation", LogLevel.Verbose);
                    continue;
                }

                string successMessage = $"Import of assets referenced in \"{spritesheetData.dataFilePath}\" completed successfully. ";

                SliceSpritesheets(spritesheetData, spritesheetImporter);

                #region Set up secondary textures
#if SECONDARY_TEXTURES_AVAILABLE
                if (spritesheetData.materialData.Count > 0) {
                    SpritesheetMaterialData[] albedoMaterials = spritesheetData.materialData.Where(mat => mat.MaterialRole == MaterialRole.Albedo).ToArray();
                    SpritesheetMaterialData[] maskMaterials = spritesheetData.materialData.Where(mat => mat.MaterialRole == MaterialRole.Mask).ToArray();
                    SpritesheetMaterialData[] normalMaterials = spritesheetData.materialData.Where(mat => mat.MaterialRole == MaterialRole.Normal).ToArray();
                    SpritesheetMaterialData[] otherMaterials = spritesheetData.materialData.Where(mat => mat.MaterialRole == MaterialRole.UnassignedOrUnrecognized).ToArray();

                    #region Validate material data
                    if (albedoMaterials.Length != 1) {
                        throw new InvalidOperationException($"There should be exactly 1 albedo material; found {albedoMaterials.Length} for data file {spritesheetData.dataFilePath}");
                    }

                    if (maskMaterials.Length > 1) {
                        throw new InvalidOperationException($"There should be at most 1 mask material; found {maskMaterials.Length} for data file {spritesheetData.dataFilePath}");
                    }

                    if (normalMaterials.Length > 1) {
                        throw new InvalidOperationException($"There should be at most 1 normal material; found {normalMaterials.Length} for data file {spritesheetData.dataFilePath}");
                    }

                    if (otherMaterials.Length > 0) {
                        Log($"Data file {spritesheetData.dataFilePath} references {otherMaterials.Length} materials of unknown purpose. These will need to be configured manually.", LogLevel.Warning);
                    }
                    #endregion

                    SpritesheetMaterialData albedo = albedoMaterials[0];
                    SpritesheetMaterialData mask = maskMaterials.Length > 0 ? maskMaterials[0] : null;
                    SpritesheetMaterialData normal = normalMaterials.Length > 0 ? normalMaterials[0] : null;

                    Log($"Asset has mask material texture: {mask != null}", LogLevel.Verbose);
                    Log($"Asset has normal material texture: {normal != null}", LogLevel.Verbose);

                    #region Create secondary textures
                    List<SecondarySpriteTexture> secondarySpriteTextures = new List<SecondarySpriteTexture>();

                    if (mask != null) {
                        secondarySpriteTextures.Add(CreateSecondarySpriteTexture(spritesheetData.dataFilePath, mask).Value);
                    }

                    if (normal != null) {
                        secondarySpriteTextures.Add(CreateSecondarySpriteTexture(spritesheetData.dataFilePath, normal).Value);
                    }
                    #endregion

                    // Secondary textures are handled through the TextureImporter, and making changes to the importer during OnPostprocessAllAssets
                    // results in them being applied the next time the asset is imported. We therefore have to trigger a reimport ourselves, but
                    // being careful only to do so if something has changed, or else we'll get stuck in an infinite import loop.
                    string albedoTextureAssetPath = Path.Combine(Path.GetDirectoryName(spritesheetData.dataFilePath), albedo.file);
                    TextureImporter albedoTextureImporter = AssetImporter.GetAtPath(albedoTextureAssetPath) as TextureImporter;
                    bool importSettingsChanged = false;

                    #region Check for changes in import settings
                    if (secondarySpriteTextures.Count != albedoTextureImporter.secondarySpriteTextures.Length) {
                        importSettingsChanged = true;
                    }
                    else {
                        // Compare each element between the two sets pairwise. We always import in a consistent
                        // order so we don't need to worry about that.
                        for (int i = 0; i < secondarySpriteTextures.Count; i++) {
                            var newSecondaryTexture = secondarySpriteTextures[i];
                            var oldSecondaryTexture = albedoTextureImporter.secondarySpriteTextures[i];

                            string newAssetPath = AssetDatabase.GetAssetPath(newSecondaryTexture.texture);
                            string oldAssetPath = AssetDatabase.GetAssetPath(oldSecondaryTexture.texture);

                            importSettingsChanged = importSettingsChanged || (newSecondaryTexture.name != oldSecondaryTexture.name) || (newAssetPath != oldAssetPath);
                        }
                    }
                    #endregion

                    if (importSettingsChanged) {
                        Log("A change has occurred in the secondary textures; triggering a reimport", LogLevel.Verbose);
                        albedoTextureImporter.secondarySpriteTextures = secondarySpriteTextures.ToArray();
                        albedoTextureImporter.SaveAndReimport();
                    }

                    successMessage += $"Configured {secondarySpriteTextures.Count} secondary textures. ";
                }
#endif
                #endregion

                #region Create/update animations
                if (spritesheetImporter.createAnimations && spritesheetData.animations.Count > 0) {
                    Log($"Going to create or replace {spritesheetData.animations.Count} animation clips for data file at \"{spritesheetData.dataFilePath}\"", LogLevel.Verbose);

                    string mainImageAssetPath = GetMainImagePath(spritesheetData);
                    string assetDirectory = Path.GetDirectoryName(mainImageAssetPath);
                    UnityEngine.Object[] sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(mainImageAssetPath);

                    Log($"Loaded {sprites.Length} sprites from main image asset path \"{mainImageAssetPath}\"", LogLevel.Verbose);

                    foreach (var animationData in spritesheetData.animations) {
                        bool multipleAnimationsFromSameSource = spritesheetData.animations.Where(anim => anim.name == animationData.name).Count() > 1;

                        // Only include rotation in name if needed to disambiguate
                        string rotation = multipleAnimationsFromSameSource ? "_rot" + animationData.rotation.ToString().PadLeft(3, '0') : "";
                        string clipName = FormatAssetName(spritesheetData.baseObjectName + "_" + animationData.name + rotation + ".anim");

                        string clipPath = assetDirectory;
                        if (multipleAnimationsFromSameSource && spritesheetImporter.placeAnimationsInSubfolders) {
                            string subfolderName = SpritesheetImporterSettings.animationSubfolderNameFormat.value
                                .Replace("{anim}", FormatAssetName(animationData.name))
                                .Replace("{obj}", FormatAssetName(spritesheetData.baseObjectName));

                            clipPath = Path.Combine(clipPath, subfolderName);
                            Log($"Creating subfolder at \"{clipPath}\" to contain animations named \"{animationData.name}\"", LogLevel.Verbose);
                            Directory.CreateDirectory(clipPath);
                        }

                        clipPath = Path.Combine(clipPath, clipName);
                        AnimationClip clip = CreateAnimationClip(spritesheetData, animationData, sprites);

                        Log($"Animation clip saving at path \"{clipPath}\"", LogLevel.Verbose);
                        AssetDatabase.CreateAsset(clip, clipPath);
                    }

                    Log("Done creating/replacing animation assets; now saving asset database", LogLevel.Verbose);
                    AssetDatabase.SaveAssets();

                    successMessage += $"Created or updated {spritesheetData.animations.Count} animation clips. ";
                }
                #endregion

                Log(successMessage, LogLevel.Info);
            }
        }

        private static AnimationClip CreateAnimationClip(SpritesheetData data, SpritesheetAnimationData animationData, UnityEngine.Object[] sprites) {
            AnimationClip clip = new AnimationClip {
                frameRate = animationData.frameRate
            };

            Log($"Creating animation clip with frame rate {animationData.frameRate}, frame skip {animationData.frameSkip}, and {animationData.numFrames} total frames", LogLevel.Verbose);

            EditorCurveBinding spriteBinding = new EditorCurveBinding {
                type = typeof(SpriteRenderer),
                path = "",
                propertyName = "m_Sprite"
            };

            var keyframes = new ObjectReferenceKeyframe[animationData.numFrames];

            for (int i = 0; i < animationData.numFrames; i++) {
                int frameNum = i * (animationData.frameSkip + 1);
                keyframes[i] = new ObjectReferenceKeyframe {
                    time = ((float) frameNum) / animationData.frameRate,
                    value = sprites[i + animationData.startFrame]
                };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyframes);

            return clip;
        }

#if SECONDARY_TEXTURES_AVAILABLE
        private static SecondarySpriteTexture? CreateSecondarySpriteTexture(string dataAssetPath, SpritesheetMaterialData materialData) {
            if (materialData == null) {
                return null;
            }

            string textureName;

            // Only a couple of roles are supported for secondary textures
            switch (materialData.MaterialRole) {
                case MaterialRole.Mask:
                    textureName = "_MaskTex";
                    break;
                case MaterialRole.Normal:
                    textureName = "_NormalMap";
                    break;
                default:
                    Log($"Material role {materialData.MaterialRole} is unrecognized and the associated image won't be processed", LogLevel.Warning);
                    return null;
            }

            Log($"Material \"{materialData.name}\" with role {materialData.role} will have the secondary texture name \"{textureName}\"", LogLevel.Verbose);

            string secondaryTexturePath = Path.Combine(Path.GetDirectoryName(dataAssetPath), materialData.file);
            Texture2D secondaryTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(secondaryTexturePath);

            if (secondaryTexture == null) {
                throw new InvalidOperationException($"Expected to find a secondary texture at asset path \"{secondaryTexturePath}\" based on .ssdata file at \"{dataAssetPath}\"");
            }

            return new SecondarySpriteTexture {
                name = textureName,
                texture = secondaryTexture
            };
        }
#endif

        private static SpriteMetaData CreateSpriteMetaData(int frame, SpritesheetData data, string name, TextureImporter textureImporter, SpritesheetDataImporter spritesheetImporter, Texture2D texture) {
            int rowSubdivisions = spritesheetImporter.subdivideSprites ? spritesheetImporter.subdivisions.y : 1;
            int columnSubdivisions = spritesheetImporter.subdivideSprites ? spritesheetImporter.subdivisions.x : 1;
            int numRows = data.numRows * rowSubdivisions;
            int numColumns = data.numColumns * columnSubdivisions;
            int columnWidth = data.spriteWidth / columnSubdivisions;
            int rowHeight = data.spriteHeight / rowSubdivisions;

            // Our spritesheets and Unity's sprite coordinate system are both row-major; however, Unity's origin is in the bottom left
            // of the texture, and ours is in the top left. We just have to do a small transformation to match.
            int row = numRows - frame / numColumns - 1;
            int column = frame % numColumns;

            // The input texture might have padding applied, which would be at the right and the bottom. Since our coordinate system
            // and Unity's both start on the left, we don't care about the horizontal padding.
            Rect spriteRect = new Rect(columnWidth * column, data.paddingHeight + rowHeight * row, columnWidth, rowHeight);

            Log($"Frame {frame} ({name}) is in row {row} and column {column} (Unity coordinates); its subrect is {spriteRect}", LogLevel.Verbose);

            if (spritesheetImporter.trimIndividualSprites) {
                RectInt trimmedRect = texture.GetTrimRegion(spritesheetImporter.trimAlphaThreshold, spriteRect.ToRectInt());
                Log($"Trimmed sprite rect from {spriteRect} to {trimmedRect}", LogLevel.Verbose);

                spriteRect = trimmedRect.ToRect();
            }

            var metadata = new SpriteMetaData() {
                alignment = (int) spritesheetImporter.pivotPlacement,
                border = Vector4.zero,
                name = name,
                rect = spriteRect
            };

            if (spritesheetImporter.pivotPlacement == SpriteAlignment.Custom) {
                metadata.pivot = FindCustomPivotPoint(metadata, textureImporter, spritesheetImporter);
            }

            return metadata;
        }

        private static Vector2 FindCustomPivotPoint(SpriteMetaData sprite, TextureImporter importer, SpritesheetDataImporter spritesheetImporter) {
            if (spritesheetImporter.customPivotMode == CustomPivotMode.Tilemap) {
                float pixelsPerUnit = importer.spritePixelsPerUnit;

                float x = sprite.rect.width / (pixelsPerUnit * spritesheetImporter.tilemapGridSize.x);
                float y = sprite.rect.height / (pixelsPerUnit * spritesheetImporter.tilemapGridSize.y);

                return new Vector2(1 / (2 * x), 1 / (2 * y));
            }
            else {
                throw new Exception($"Unknown customPivotMode value {spritesheetImporter.customPivotMode}");
            }
        }

        /// <summary>
        /// Attempts to locate and load spritesheet data from a .ssdata file in the same directory as
        /// the asset at <paramref name="imageAssetPath"/>.
        /// </summary>
        /// <param name="imageAssetPath">The asset path of a Texture2D asset.</param>
        /// <returns>Loaded spritesheet data if any is found in the same directory, else null.</returns>
        /// <exception cref="InvalidOperationException">If multiple .ssdata files reference the given asset.</exception>
        private static SpritesheetData FindSpritesheetData(string imageAssetPath) {
            Log($"Searching for .ssdata files referencing the image asset \"{imageAssetPath}\"", LogLevel.Verbose);

            // Check out all .ssdata files in the same directory and try to find one referencing this asset
            string assetDirectory = Path.GetDirectoryName(imageAssetPath);
            string assetFileName = Path.GetFileName(imageAssetPath);

            string[] paths = Directory.GetFiles(assetDirectory, "*.ssdata", SearchOption.TopDirectoryOnly);
            Log($"There are {paths.Length} .ssdata files in the image asset directory", LogLevel.Verbose);

            List<SpritesheetData> matchingSpritesheetData = new List<SpritesheetData>();

            foreach (string path in paths) {
                SpritesheetData dataObj = LoadSpritesheetDataFile(path);

                if (dataObj.imageFile == assetFileName) {
                    Log($"Data file {path} references image asset via imageFile field", LogLevel.Verbose);
                    matchingSpritesheetData.Add(dataObj);
                    continue;
                }

                foreach (SpritesheetMaterialData material in dataObj.materialData) {
                    if (material.file == assetFileName) {
                        Log($"Data file {path} references image asset via a material", LogLevel.Verbose);
                        matchingSpritesheetData.Add(dataObj);
                    }
                }
            }

            if (matchingSpritesheetData.Count > 1) {
                string allPaths = string.Join(", ", matchingSpritesheetData.Select(d => d.dataFilePath));

                Log($"Multiple .ssdata files reference asset at {imageAssetPath}.", LogLevel.Error);
                Log($"Data file paths: {allPaths}", LogLevel.Error);

                throw new InvalidOperationException($"Found multiple data objects referencing asset at {imageAssetPath}");
            }
            else if (matchingSpritesheetData.Count == 1) {
                Log($"Found a matching spritesheet data file at \"{matchingSpritesheetData[0].dataFilePath}\"", LogLevel.Verbose);
                return matchingSpritesheetData[0];
            }
            else {
                Log($"Did not find a matching spritesheet data file for asset", LogLevel.Verbose);
                return null;
            }
        }

        private static string FormatAssetName(string name) {
            if (!SpritesheetImporterSettings.formatFileNames) {
                return name;
            }

            return name.Replace(' ', '_').Replace('-', '_').ToLower();
        }

        private static string GetMainImagePath(SpritesheetData data) {
            string path;
            if (data.imageFile != null) {
                path = data.imageFile;
            }
            else if (data.materialData.Count > 0) {
                path = data.materialData.Where(mat => mat.MaterialRole == MaterialRole.Albedo).Single().file;
            }
            else {
                throw new ArgumentException($"SpritesheetData at {data.dataFilePath} has no definitive main texture");
            }

            // Image file paths are all relative to the data file; make them absolute
            return Path.Combine(Path.GetDirectoryName(data.dataFilePath), path);
        }

        private static Vector2Int GetTextureSize(TextureImporter importer) {
            // TextureImporter doesn't expose this information so we have to use reflection
            // From https://github.com/theloneplant/blender-spritesheets/blob/master/unity-importer/Assets/Scripts/Editor/TextureImporterExtension.cs
            MethodInfo method = typeof(TextureImporter).GetMethod("GetWidthAndHeight", BindingFlags.NonPublic | BindingFlags.Instance);
            object[] outputs = new object[] { 0, 0 };
            method.Invoke(importer, outputs);
            return new Vector2Int {
                x = (int) outputs[0],
                y = (int) outputs[1],
            };
        }

        private static SpritesheetData LoadSpritesheetDataFile(string dataFileAssetPath) {
            Log($"Attempting to load .ssdata file from path \"{dataFileAssetPath}\"", LogLevel.Verbose);

            SpritesheetData dataObj = AssetDatabase.LoadAssetAtPath<SpritesheetData>(dataFileAssetPath);

            if (dataObj != null) {
                return dataObj;
            }

            string jsonData = File.ReadAllText(dataFileAssetPath);
            dataObj = JsonUtility.FromJson<SpritesheetData>(jsonData);
            dataObj.dataFilePath = dataFileAssetPath;

            return dataObj;
        }

        private static void Log(string message, LogLevel level) {
            if (!SpritesheetImporterSettings.logLevel.value.Includes(level)) {
                return;
            }

            message = "[Spritesheet Importer] " + message;

            if (level == LogLevel.Error) {
                Debug.LogError(message);
            }
            else if (level == LogLevel.Warning) {
                Debug.LogWarning(message);
            }
            else {
                Debug.Log(message);
            }
        }

        private static void SliceSpritesheets(SpritesheetData data, SpritesheetDataImporter spritesheetImporter) {
            string assetDirectory = Path.GetDirectoryName(data.dataFilePath);
            List<string> textures = new List<string>();

            if (!string.IsNullOrEmpty(data.imageFile)) {
                textures.Add(data.imageFile);
            }

            if (data.materialData != null) {
                foreach (var material in data.materialData) {
                    if (!spritesheetImporter.sliceSecondaryTextures && material.IsSecondaryTexture) {
                        Log($"Asset \"{material.file}\" is a secondary texture and slicing of secondary textures is disabled; no action taken", LogLevel.Verbose);
                        continue;
                    }

                    if (!spritesheetImporter.sliceUnidentifiedTextures && material.IsUnidentifiedTexture) {
                        Log($"Asset \"{material.file}\" is an unidentified texture and slicing of unidentified textures is disabled; no action taken", LogLevel.Verbose);
                        continue;
                    }

                    textures.Add(material.file);
                }
            }

            foreach (string texturePath in textures) {
                string assetPath = Path.Combine(assetDirectory, texturePath);
                SliceTexture(data, assetPath, applyChanges: false);
            }
        }

        private static void SliceTexture(SpritesheetData data, string assetPath, bool applyChanges) {
            TextureImporter textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            SpritesheetDataImporter spritesheetImporter = AssetImporter.GetAtPath(data.dataFilePath) as SpritesheetDataImporter;
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath).ReadableView();
            Vector2Int textureSize = GetTextureSize(textureImporter);

            var existingSprites = textureImporter.spritesheet;
            List<SpriteMetaData> newSprites = new List<SpriteMetaData>();

            int numStills = data.stills.Count;

            if (spritesheetImporter.subdivideSprites) {
                numStills *= spritesheetImporter.subdivisions.x * spritesheetImporter.subdivisions.y;
            }

            // TODO: might be helpful to name sprites differently if subdivisions are on, to group them by their original undivided sprite
            for (int i = 0; i < numStills; i++) {
                string name = data.baseObjectName + "_" + i;
                Log($"Creating sprite still for frame {i} with sprite name {name}", LogLevel.Verbose);
                newSprites.Add(CreateSpriteMetaData(i, data, FormatAssetName(name), textureImporter, spritesheetImporter, texture));
            }

            foreach (SpritesheetAnimationData animation in data.animations) {
                for (int i = animation.startFrame; i < animation.startFrame + animation.numFrames; i++) {
                    string name = data.baseObjectName + "_" + animation.name + "_rot" + animation.rotation + "_" + (i - animation.startFrame);
                    Log($"Creating sprite slice for animation frame {i} with sprite name {name}", LogLevel.Verbose);
                    newSprites.Add(CreateSpriteMetaData(i, data, FormatAssetName(name), textureImporter, spritesheetImporter, texture));
                }
            }

            Log($"Asset \"{assetPath}\" has been sliced into {newSprites.Count} sprites", LogLevel.Info);

            // Even if there's only one sprite, we import in Multiple mode, both for consistency
            // and so that we can apply trimming to that sprite
            bool importSettingsChanged = textureImporter.spriteImportMode != SpriteImportMode.Multiple;

            if (newSprites.Count != existingSprites.Length) {
                importSettingsChanged = true;
            }
            else {
                // Check each individual sprite to look for changes
                for (int i = 0; i < newSprites.Count; i++) {
                    var newSprite = newSprites[i];
                    var oldSprite = existingSprites[i];

                    if (!newSprite.Equals(oldSprite)) {
                        importSettingsChanged = true;
                        break;
                    }
                }
            }

            if (importSettingsChanged) {
                Log($"Asset at \"{assetPath}\" will be re-imported due to new import settings", LogLevel.Verbose);
                var newAsArray = newSprites.ToArray();

                textureImporter.spriteImportMode = SpriteImportMode.Multiple;
                textureImporter.spritesheet = newAsArray;

                EditorUtility.SetDirty(textureImporter); // For some reason the importer is too dumb to notice it has a new spritesheet
                textureImporter.SaveAndReimport();
            }
            else {
                Log($"Asset at \"{assetPath}\" was already sliced correctly; not flagging for re-import", LogLevel.Verbose);
            }
        }
    }
}
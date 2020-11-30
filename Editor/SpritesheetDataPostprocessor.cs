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
        void OnPreprocessTexture() {
            // During OnPreprocessTexture, we just slice the spritesheet into individual sprites based on the JSON data.
            // Other steps, such as creating animations or configuring secondary textures, occur in OnPostprocessAllAssets.
            SpritesheetData data = FindSpritesheetData(assetPath);

            if (data == null) {
                Log($"Asset path \"{assetPath}\" is not referenced by any .ssdata file in the same directory", LogLevel.Verbose);
                return;
            }

            // Check what type of image this is and whether we want to slice it or not
            string assetFileName = Path.GetFileName(assetPath);
            SpritesheetMaterialData material = data.materialData.Where(mat => mat.file == assetFileName).SingleOrDefault();

            if (material != null) {
                if (!SpritesheetImporterSettings.sliceSecondaryTextures && material.IsSecondaryTexture) {
                    Log($"Asset \"{assetPath}\" is a secondary texture and slicing of secondary textures is disabled; no action taken", LogLevel.Verbose);
                    return;
                }

                if (!SpritesheetImporterSettings.sliceUnidentifiedTextures && material.IsUnidentifiedTexture) {
                    Log($"Asset \"{assetPath}\" is an unidentified texture and slicing of unidentified textures is disabled; no action taken", LogLevel.Verbose);
                    return;
                }
            }

            TextureImporter importer = assetImporter as TextureImporter;
            Vector2Int textureSize = GetTextureSize(importer);

            List<SpriteMetaData> subsprites = new List<SpriteMetaData>();

            foreach (SpritesheetStillData still in data.stills) {
                string name = data.baseObjectName + "_" + still.frame;
                Log($"Creating sprite still for frame {still.frame} with sprite name {name}", LogLevel.Verbose);
                subsprites.Add(CreateSpriteMetaData(still.frame, data, FormatAssetName(name)));
            }

            foreach (SpritesheetAnimationData animation in data.animations) {
                for (int i = animation.startFrame; i < animation.startFrame + animation.numFrames; i++) {
                    string name = data.baseObjectName + "_" + animation.name + "_rot" + animation.rotation + "_" + (i - animation.startFrame);
                    Log($"Creating sprite slice for animation frame {i} with sprite name {name}", LogLevel.Verbose);
                    subsprites.Add(CreateSpriteMetaData(i, data, FormatAssetName(name)));
                }
            }

            Log($"Asset \"{assetPath}\" has been sliced into {subsprites.Count} sprites", LogLevel.Info);

            if (subsprites.Count > 1) {
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.spritesheet = subsprites.ToArray();
            }
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
            if (SpritesheetImporterSettings.placeAnimationsInSubfolders && string.IsNullOrWhiteSpace(SpritesheetImporterSettings.animationSubfolderNameFormat)) {
                Log("Unable to process assets because the \"Subfolder Name Format\" project setting is blank", LogLevel.Error);
                return;
            }

            List<string> processedSpritesheets = new List<string>();

            foreach (string assetPath in importedAssets) {
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

                // When loading in several images that represent a texture and its secondary textures, we want to avoid
                // repeating work, so we only process each spritesheet data file once per import
                if (spritesheetData == null) {
                    continue;
                }

                if (processedSpritesheets.Contains(spritesheetData.dataFilePath)) {
                    Log($"Spritesheet at path \"{spritesheetData.dataFilePath} has already been processed in this import operation; skipping", LogLevel.Verbose);
                    continue;
                }

                string successMessage = $"Import of assets referenced in \"{spritesheetData.dataFilePath}\" completed successfully. ";

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
                if (SpritesheetImporterSettings.createAnimations && spritesheetData.animations.Count > 0) {
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
                        if (multipleAnimationsFromSameSource && SpritesheetImporterSettings.placeAnimationsInSubfolders) {
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

            Log($"Creating animation clip with frame rate {animationData.frameRate} and {animationData.numFrames} total frames", LogLevel.Verbose);

            EditorCurveBinding spriteBinding = new EditorCurveBinding {
                type = typeof(SpriteRenderer),
                path = "",
                propertyName = "m_Sprite"
            };

            var keyframes = new ObjectReferenceKeyframe[animationData.numFrames];

            for (int i = 0; i < animationData.numFrames; i++) {
                keyframes[i] = new ObjectReferenceKeyframe {
                    time = ((float) i) / animationData.frameRate,
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

        private static SpriteMetaData CreateSpriteMetaData(int frame, SpritesheetData data, string name) {
            // Our spritesheets and Unity's sprite coordinate system are both row-major; however, Unity's origin is in the bottom left
            // of the texture, and ours is in the top left. We just have to do a small transformation to match.
            int row = data.numRows - frame / data.numColumns - 1;
            int column = frame % data.numColumns;

            // The input texture might have padding applied, which would be at the right and the bottom. Since our coordinate system
            // and Unity's both start on the left, we don't care about the horizontal padding.
            Rect spriteRect = new Rect(data.spriteWidth * column, data.paddingHeight + data.spriteHeight * row, data.spriteWidth, data.spriteHeight);

            Log($"Frame {frame} ({name}) is in row {row} and column {column} (Unity coordinates); its subrect is {spriteRect}", LogLevel.Verbose);

            return new SpriteMetaData() {
                alignment = (int) SpriteAlignment.Center,
                border = Vector4.zero,
                name = name,
                pivot = 0.5f * Vector2.one,
                rect = spriteRect
            };
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

            string jsonData = File.ReadAllText(dataFileAssetPath);
            SpritesheetData dataObj = JsonUtility.FromJson<SpritesheetData>(jsonData);
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

        #region Data classes
        // All of the data classes have their fields assigned to default because otherwise older versions
        // of Unity will fill up the debug window with useless warnings
        [Serializable]
        public class SpritesheetData {
            public string dataFilePath = default; // Not from JSON; filled in manually

            public string baseObjectName = default;
            public string imageFile = default;

            public int spriteWidth = default;
            public int spriteHeight = default;

            public int paddingWidth = default;
            public int paddingHeight = default;

            public int numColumns = default;
            public int numRows = default;

            public List<SpritesheetAnimationData> animations = default;
            public List<SpritesheetMaterialData> materialData = default;
            public List<SpritesheetStillData> stills = default;
        }

        [Serializable]
        public class SpritesheetAnimationData {
            public int frameRate = default;
            public string name = default;
            public int numFrames = default;
            public float rotation = default;
            public int startFrame = default;
        }

        [Serializable]
        public class SpritesheetMaterialData {
            public string name = default;
            public string file = default;
            public string role = default;

            public MaterialRole MaterialRole {
                get {
                    switch (role) {
                        case "albedo":
                            return MaterialRole.Albedo;
                        case "mask_unity":
                            return MaterialRole.Mask;
                        case "normal_unity":
                            return MaterialRole.Normal;
                        default:
                            return MaterialRole.UnassignedOrUnrecognized;
                    }
                }
            }

            public bool IsSecondaryTexture => MaterialRole == MaterialRole.Mask || MaterialRole == MaterialRole.Normal;

            public bool IsUnidentifiedTexture => MaterialRole == MaterialRole.UnassignedOrUnrecognized;
        }

        public enum MaterialRole {
            Albedo,
            Mask,
            Normal,
            UnassignedOrUnrecognized
        }

        [Serializable]
        public class SpritesheetStillData {
            public int frame = default;
            public float rotation = default;
        }
        #endregion
    }
}
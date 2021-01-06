using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

namespace SpritesheetImporter {
    [ScriptedImporter(1, "ssdata")]
    internal class SpritesheetDataImporter : ScriptedImporter {
        private const int latestImporterVersion = 1;

        [Header("Animations")]
        [Tooltip("Whether to create Animation Clip assets for animations in this spritesheet, if any.")]
        public bool createAnimations;

        [ShowWhen(OtherPropertyPath = nameof(createAnimations), OtherPropertyValue = true)]
        [Tooltip("Whether created Animation Clips should be placed in subfolders based on the animation name if multiple exist for the same name.")]
        public bool placeAnimationsInSubfolders;

        [Header("Sheet Slicing")]
        [Tooltip("If set, empty space will be trimmed from each sprite individually when slicing textures.")]
        public bool trimIndividualSprites;

        [Range(0.0f, 1.0f)]
        [ShowWhen(OtherPropertyPath = nameof(trimIndividualSprites), OtherPropertyValue = true)]
        [Tooltip("Pixels with an alpha value equal or less than this will be considered empty when trimming sprites.")]
        public float trimAlphaThreshold;

        [Tooltip("Whether secondary textures should be sliced into sprites. This is generally not needed for built-in Unity functionality.")]
        public bool sliceSecondaryTextures;

        [Tooltip("Whether textures with an unknown role should be sliced into sprites.")]
        public bool sliceUnidentifiedTextures;

        [Tooltip("Where the pivot for individual sprites should be placed when slicing.")]
        public SpriteAlignment spritePivot;

        [ShowWhen(OtherPropertyPath = nameof(spritePivot), OtherPropertyValue = SpriteAlignment.Custom)]
        [Tooltip("How to determine the custom pivot point.")]
        public CustomPivotMode customPivotMode;

        [SerializeField] [HideInInspector] private int lastImporterVersion = -1;

        void OnValidate() {
            CheckVersion();
        }

        public override void OnImportAsset(AssetImportContext ctx) {
            CheckVersion();

            SpritesheetData dataObj = ScriptableObject.CreateInstance<SpritesheetData>();
            string jsonData = File.ReadAllText(ctx.assetPath);
            JsonUtility.FromJsonOverwrite(jsonData, dataObj);
            dataObj.dataFilePath = ctx.assetPath;

            ctx.AddObjectToAsset("data", dataObj);
            ctx.SetMainObject(dataObj);
        }

        internal void ReloadFromDefaults() {
            lastImporterVersion = -1;
            CheckVersion();
        }

        private void CheckVersion() {
            if (lastImporterVersion == latestImporterVersion) {
                return;
            }

            EditorUtility.SetDirty(this);

            if (lastImporterVersion < 1) {
                createAnimations = SpritesheetImporterSettings.createAnimations;
                customPivotMode = SpritesheetImporterSettings.customPivotMode;
                placeAnimationsInSubfolders = SpritesheetImporterSettings.placeAnimationsInSubfolders;
                sliceSecondaryTextures = SpritesheetImporterSettings.sliceSecondaryTextures;
                sliceUnidentifiedTextures = SpritesheetImporterSettings.sliceUnidentifiedTextures;
                spritePivot = SpritesheetImporterSettings.spritePivot;
                trimAlphaThreshold = SpritesheetImporterSettings.trimAlphaThreshold;
                trimIndividualSprites = SpritesheetImporterSettings.trimSprites;
            }

            lastImporterVersion = latestImporterVersion;
        }
    }
}
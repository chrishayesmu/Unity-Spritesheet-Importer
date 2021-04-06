using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

namespace SpritesheetImporter {
    [ScriptedImporter(1, "ssdata")]
    internal class SpritesheetDataImporter : ScriptedImporter {
        [Header("Animations")]
        [Tooltip("Whether to create Animation Clip assets for animations in this spritesheet, if any.")]
        public bool createAnimations = true;

        [ShowWhen(OtherPropertyPath = nameof(createAnimations), OtherPropertyValue = true)]
        [Tooltip("Whether created Animation Clips should be placed in subfolders based on the animation name if multiple exist for the same name.")]
        public bool placeAnimationsInSubfolders = true;

        [Header("Sheet Slicing")]
        [Tooltip("If set, empty space will be trimmed from each sprite individually when slicing textures.")]
        public bool trimIndividualSprites = false;

        [ShowWhen(OtherPropertyPath = nameof(trimIndividualSprites), OtherPropertyValue = true)]
        [Tooltip("Pixels with an alpha value equal or less than this will be considered empty when trimming sprites.")]
        public float trimAlphaThreshold;

        [Tooltip("Whether secondary textures should be sliced into sprites. This is generally not needed for built-in Unity functionality.")]
        public bool sliceSecondaryTextures = false;

        [Tooltip("Whether textures with an unknown role should be sliced into sprites.")]
        public bool sliceUnidentifiedTextures = true;

        [Tooltip("Set this to subdivide individual sprites further than the data file states. This is useful if you've rendered one model into a spritesheet, but it should be split into parts.")]
        public bool subdivideSprites = false;

        [ShowWhen(OtherPropertyPath = nameof(subdivideSprites), OtherPropertyValue = true)]
        [Tooltip("How to subdivide the sprites. This represents how many columns (x) and rows (y) each individual sprite contains. If set to (1, 1), for example, this would result in a single sprite.")]
        public Vector2Int subdivisions = Vector2Int.one;

        [Header("Sprite Pivots")]
        [Tooltip("Where the pivot for individual sprites should be placed when slicing.")]
        public SpriteAlignment pivotPlacement = SpriteAlignment.Center;

        [ShowWhen(OtherPropertyPath = nameof(pivotPlacement), OtherPropertyValue = SpriteAlignment.Custom)]
        [Tooltip("How to determine the custom pivot point.")]
        public CustomPivotMode customPivotMode = CustomPivotMode.Tilemap;

        [ShowWhen(OtherPropertyPath = nameof(customPivotMode), OtherPropertyValue = CustomPivotMode.Tilemap)]
        [Tooltip("The size of the tilemap grid the sprites will be used in.")]
        public Vector2 tilemapGridSize = Vector2.one;

        public override void OnImportAsset(AssetImportContext ctx) {
            SpritesheetData dataObj = ScriptableObject.CreateInstance<SpritesheetData>();
            string jsonData = File.ReadAllText(ctx.assetPath);
            JsonUtility.FromJsonOverwrite(jsonData, dataObj);
            dataObj.dataFilePath = ctx.assetPath;

            ctx.AddObjectToAsset("data", dataObj);
            ctx.SetMainObject(dataObj);
        }
    }
}
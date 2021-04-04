using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// All of the data classes have their fields assigned to default because otherwise older versions
// of Unity will fill up the debug window with useless warnings
[Serializable]
public class SpritesheetData: ScriptableObject {
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
    public int frameSkip = default;
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
- [Overview](#overview)
- [Usage](#usage)
  - [Functionality](#functionality)
  - [Configuration](#configuration)
  - [Supported Unity versions](#supported-unity-versions)
- [Dependencies](#dependencies)
- [Installation](#installation)
  - [As Unity package (recommended)](#as-unity-package-recommended)
  - [Manually](#manually)
- [License](#license)
- [Acknowledgements](#acknowledgements)

# Overview

A Unity asset importer intended to be used with the output of my [Blender Spritesheet Renderer](https://github.com/chrishayesmu/Blender-Spritesheet-Renderer).

# Usage

Simply import your spritesheet asset to your Unity project alongside the JSON file describing it, and the importer will kick in automatically. Shortly after, your image will be sliced and animations created. 

Note that any `.ssdata` file **must** be located in the same directory as the images it is associated with, or import will fail.

> :bulb: If you're going to be working with 2D animations a lot, you may wish to check out the free asset [Sprite Animation Preview](https://assetstore.unity.com/packages/tools/utilities/sprite-animation-preview-37611) on the Asset Store. I have found it very useful in testing this importer and in developing my own game, though in Unity 2020.1 it does seem to cause the occasional NullReferenceException.

## Functionality

This package simply adds an [AssetPostprocessor](https://docs.unity3d.com/ScriptReference/AssetPostprocessor.html) which watches for changes to [Texture2D](https://docs.unity3d.com/ScriptReference/Texture2D.html) assets, and assets ending in the extension `.ssdata`. This extension, meaning "spritesheet data", is a JSON format output by the Blender addon that describes how to parse a spritesheet. Whenever a change occurs to a supported asset, the AssetPostprocessor will:

* Slice the primary image (and optionally, other images) into individual sprites.
* Associate any [secondary textures](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@7.0/manual/SecondaryTextures.html) with the primary image texture (currently Unity only supports Mask and Normal textures) - note that secondary textures are not supported in all render pipelines!
* Create [AnimationClips](https://docs.unity3d.com/ScriptReference/AnimationClip.html) based on the animation data in the `.ssdata` file.

The package also includes an inspector for the `.ssdata` asset and its importer.

## Configuration

Several configuration options are available under `Edit > Project Settings... > Spritesheet Importer`. The majority of configuration  are on the importer itself, so they will be visible in the inspector when you have selected a `.ssdata` file (similar to how you see texture import settings when selecting a texture).

Every option has a detailed tooltip; just hover the option name to see it.

## Supported Unity versions

I am personally using this for a project on Unity 2020.1.15f1, so that version has the most extensive usage. I have also done cursory testing on Unity 2019.3.13f1 and 2018.4.29f1 with no issues. Older versions may also work, but I have not tested any.

> :memo: **Secondary textures** were added in [Unity 2019.2](https://unity3d.com/unity/whats-new/2019.2.0), so in versions older than this, that functionality will be missing. Slicing secondary textures is still supported by this package, in case you have your own shaders in place; they just won't be associated with the main image texture in any way.

# Dependencies

The only dependency is on Unity's [Settings Manager](https://docs.unity3d.com/Packages/com.unity.settings-manager@1.0/manual/index.html) package, for showing and persisting configuration options in the Project Settings menu. All other functionality uses only classes built-in to Unity.

# Installation

## As Unity package (recommended)

1. Navigate to your project's root directory.
2. Open the "Packages" folder, then open `manifest.json` in any text editor.
3. Add the following line in the "dependencies" section:  

    `"com.chrishayesmu.spritesheet-importer": "https://github.com/chrishayesmu/Unity-Spritesheet-Importer.git#main"`

4. Save the file, and the next time you focus Unity, it should detect the change and automatically install the package.
5. No further setup is necessary, but you can confirm installation was successful by checking out the configuration options under `Edit > Project Settings... > Spritesheet Importer`.

<!-- omit in toc -->
### Updating the package

While this project is unlikely to be updated much, there may be the occasional bug fix or minor feature. Unfortunately the update process for custom packages is [not very user friendly](https://forum.unity.com/threads/custom-package-not-updating.696226/), but it's simple enough:

1. Open the `manifest.json` file in your project's "Packages" folder in any text editor.
2. Look for the section called `lock`. You should see a subsection like this:

```
"com.chrishayesmu.spritesheet-importer": {
  "revision": "main",
  "hash": "bdb7a97b14695496a152604bf3ace2de7dd02ed1"
}
```

3. The `hash` key refers to the exact commit which your project is locked to. Simply delete the entire section with the key `"com.chrishayesmu.spritesheet-importer"`. (Make sure you don't make the JSON invalid, e.g. by leaving a trailing comma on the line above.)
4. The next time you focus Unity, it will download the latest package version.

## Manually

:warning: This is **not** a recommended method. If you're reluctant to rely on a stranger's GitHub repository because it may change or be deleted, I'd recommend forking the repo and following the Unity package installation process using your own repo URL. This not only simplifies installation, but makes it easier later to merge in any upstream changes you're interested in.

If you really want to install outside of the package manager, it should be as simple as downloading the code from GitHub and placing it in your project's Assets directory. I haven't tested this personally and do not provide support for it.

# License

This project is available under the [MIT License](LICENSE), and you're free to do pretty much whatever under the terms of that license. If you make a fix or change that you think may be useful for others, I'd appreciate if you open an issue or pull request for review, but you are by no means obligated to.

# Acknowledgements

As with my [Blender Spritesheet Renderer](https://github.com/chrishayesmu/Blender-Spritesheet-Renderer), this project is inspired by [theloneplant/blender-spritesheets](https://github.com/theloneplant/blender-spritesheets/).
--------------------
     FairyGUI
Copyright © 2014-2022 FairyGUI.com
Version 4.3.0
support@fairygui.com
--------------------
FairyGUI is a flexible UI framework for Unity, working with the professional FREE Game UI Editor: FairyGUI Editor. 
Download the editor from here: https://www.fairygui.com/

--------------------
Get Started
--------------------
Run demos in Assets/FairyGUI/Examples/Scenes.
The UI project is in Examples-UIProject.zip, unzip it anywhere. Use FairyGUI Editor to open it.

Using FairyGUI in Unity:
* Place a UIPanel in scene by using editor menu GameObject/FairyGUI/UIPanel.
* Or using UIPackage.CreateObject to create UI component dynamically, and then use GRoot.inst.AddChild to show it on screen.

-----------------
 Version History
-----------------
4.3.0
1. Support truncating text with ellipsis.
2. Add UIConfig.defaultScrollSnappingThreshold/defaultScrollPagingThreshold.
3. Add support for changing skeleton animation in controller and transition.
4. Increase compatibility of package path detection.
5. Provide a better way for cloning spine materials.
6. Ensure GList.defaultItem to be in normalized format for performance issue.
7. Fixed a shape hit test bug.
8. Fixed a scrolling bug if custom input is enabled.

4.2.0
1. Improve GoWrapper clipping.
2. Fix a bitmap font bug.
3. Add enter/exit sound for GComponent.
4. Add click sound for GCombobox/GLabel/GProgressbar.
5. Fix emoji align issue.
6. Adapt to new TextMeshPro version.
7. Fix a text shrinking behavior bug.

4.1.0
1. Add Stage.GetTouchTarget.
2. Add CustomEase.
3. Add Atlas reference manage mechanism.
4. Fixed: the line settings of polygon is missing.
5. Fixed: nested transitions may not be played.
6. Fixed: wrong parameter for loading Spine/Dragonbones by bundle.
7. Fixed: exceptions when a UIPanel in prefab is being destroyed in editor, since 2018.3.
8. Upgrade example project to Unity 2018.

4.0.1
- FIXED: Selection bug in InputTextField.
- FIXED: Wrong TextMeshPro text color in linear color space.
- FIXED: Set initial color to white of GLoader3D.
- FIXED: Recover text after cancelling typing effect.
- FIXED: Invalid command key status in windows.

4.0.0
- New: New GLoader3D object for loading Spine and DragonBones.
- New: Support TextMeshPro.
- New: Mouse cursor manager.
- New: Key navigation support.
- New: Async loading mechanism of UIPackage (Convenient for Addressable).
- IMPROVED: User's experience of mouse wheel.

3.5.1
- IMPROVED: Outline support for polygon shape.
- FIXED: Branch mechanism not works for GComponent.
- FIXED: Rotated sprite in atlas display incorrectly.

3.5.0
- NEW: Editor 5.0 functions.
- FIXED: Nested stencil masks work incorrectly.
- FIXED: TouchScreenKeyboard bug.
- FIXED: Some issues in RTLSupport.

3.4.1
- FIXED: Text order issue in fairybatching.
- FIXED: A virtual list bug.
- FIXED: A bug in color control of circle shape.
- FIXED: Fixed a bug in fill mesh.

3.4.0
- NEW: Add multi-display support.
- NEW: Add API DynamicFont(name, font).
- IMPROVED: Compatibility with 2018.3.
- FIXED: Incorrect letter spacing on mobile platform.
- FIXED: Same transition hook may be called twice.
- FIXED: Exception raised when texture was disposed before object was disposed.

3.3.0
- NEW: Add textfield maxwidth feature.
- NEW: Add API to query package dependencies.
- IMPROVED: Graphics module refactor. Now it is more convenient to create various shapes(pie, lines, polygon etc) and do mesh deform. Memory usage on building mesh is also dropped. Also supports automatically setup uv for arbitrary quad to avoid seam between 2 triangles. All shaders are updated, don't forget to replace shaders in your project.
- IMPROVED: Text-Brighter mechanism is removed, so FairyGUI-Text-Brighter.shader is removed. 
- IMPROVED: Add support for shrinking multi-line text.
- IMPROVED: Improve Lua support.

3.2.0
- NEW: Add DisplayObjectInfo component. Define script symbol FAIRYGUI_TEST to enable it.
- FIXED: A virtual list scrolling bug.
- FIXED: A BlendMode bug.

3.1.0
- NEW: Draw extra 8 directions instead of 4 directions to archive text outline effect. Toggle option is UIConfig.enhancedTextOutlineEffect.
- IMPROVED: Eexecution efficiency optimizations.
- IMPROVED: GoWrapper now supports multiple materials.
- FIXED: Correct cleanup action for RemovePackage.

3.0.0
From this version, we change package data format to binary. Editor version 3.9.0  with 'binary format' option checked in publish dialog is required to generating this kind of format. Old XML format is not supported anymore.

- NEW: Add UIPackage.UnloadAssets and UIPackage.ReloadAssets to allow unload package resources without removing the package. 
- NEW: Add TransitionActionType.Text and TransitionActionType.Icon.

2.4.0
- NEW: GTween is introduced, DOTween is no longer used inside FairyGUI.
- NEW: Transitions now support playing partially and pausing.
- IMPROVED: Change the way of registering bitmap font. 
- FIXED: A GButton pivot issue.
- FIXED: Correct text align behavior.

2.3.0
- NEW: Allow loader to load component.
- NEW: Add text template feature.
- FIXED: Exclude invisible object in FairyBatching.

2.2.0
- NEW: Modify shaders to fit linear color space.
- IMPROVED: Improve relation system to handle conditions that anchor is set.
- IMPROVED: Eliminate GC in transition.
- FIXED: Fixed a bug of unloading bundle in UIPackage.
- FIXED: Fixed issue that some blend mode(multiply, screen) works improperly.

2.1.0
- NEW: Add GGraph.DrawRoundRect.
- NEW: Add FillType.ScaleNoBorder.
- FIXED: Repair potential problems of virtual list.
- FIXED: Fixed a bug in handling shared materials.

2.0.0
- NEW: RTL Text rendering support. Define scripting symbols RTL_TEXT_SUPPORT to enabled it.
- NEW: Support for setting GObject.data in editor.
- NEW: Support for setting selectedIcon of list item in editor.
- IMPROVED: Add UIConfig.depthSupportForPaitingMode.
- IMPROVED: Set sorting order of popup automatically.
- FIXED: Fixed a text layout bug when line spacing is negative.
- FIXED: Reset ScrollPane.draggingPane when an active scrollPane is being disposed.
- FIXED: Fixed a bug of skew rendering.
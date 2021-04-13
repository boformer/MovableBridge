Movable Bridge Mod: Documentation for Asset Creators
====================================================

A movable bridge is an animated building with integrated network segments. Usually it consists of the following elements:

* A main building, consisting of the moving part of the bridge
* Custom Animation Loader `animations.unity3d` file for the opening/closing animation of main building
* Sub-buildings for the static bridge parts
* An invisible road or train bridge network for the moving part of the bridge
* A visible road or train bridge network for the static parts of the bridge
* A shader-animated barrier or traffic prop

## Required Tools & Mods

* [Blender](https://www.blender.org/) (modeling, rigging and animation)
* [Unity 5.6.7f1](https://unity3d.com/get-unity/download/archive) (animation setup and bundling)
* [ModTools](https://steamcommunity.com/sharedfiles/filedetails/?id=2434651215)
* [Custom Animation Loader](https://steamcommunity.com/sharedfiles/filedetails/?id=1664509314)
* [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2040656402)
* Movable Bridge Mod
* Movable Bridge Template

## Barrier/Traffic Light Prop

You can either use one of the vanilla traffic lights or level crossing props, or create your own.

TODO

https://gist.github.com/ronyx69/b14b6da14529f3b5c43c95031a2101eb

## Networks

The next step is the creation of two networks: One visible network for the static "waiting area" of the bridge, one invisible network (with no nodes or segments) for the movable part of the bridge.

m_UIEditorCategory

MovableBridge_Static

MovableBridge_Movable

Change m_placementStyle back to Procedural

m_useFixedHeight = true

## Static Sub-Buildings

Use a network pillar template

Set m_placementMode to OnWater

## Animated Main Building

Make sure that the Movable Bridge mod is enabled while creating the main building

Use draw bridge template

"Movable Bridge" section in the properties window

Pre Opening Duration

Opening Duration

Closing Duration

Bridge Clearance


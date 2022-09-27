# com.krisrok.vfxpath

Enables using complex splines in combination with Unity's Visual Effect Graph to easily author effects with curves live in the Unity Editor:

https://user-images.githubusercontent.com/3404365/192530794-e9793054-1ff8-43bd-ba6d-7f3158724cd3.mp4

## Rationale

Sometimes you might need huge amounts of particles but want to control their flow even more precisely as you traditionally can with flow maps or other techniques.

In VFX Graph there is already a `SampleBezier` node included (see [here](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@10.2/manual/Operator-SampleBezier.html)) but it is limited to just one segment and hard to handle.

## Installation

Via `manifest.json`:
```
{
  "dependencies": {
    "com.krisrok.vfxpath": "https://github.com/krisrok/vfxpath.git",
    ...
```

## Dependencies

There are two spline providers supported and you _have to_ install (at least) one of them.

### Preferred option: Unity's Splines
https://docs.unity3d.com/Packages/com.unity.splines@1.0/manual/index.html

Depending on your Unity version you might need to add the dependency manually to your manifest or you can just install it via Package Manager.

### Also supported: SebLague's PathCreator
https://github.com/SebLague/Path-Creator

There are several ways to install it.

## Usage

There are two parts to this: Providing the spline's data to the VFX graph (via [VFXPropertyBinder](#VFXPropertyBinder)) and enabling access to the data inside the graphs (via custom Nodes, or better [Subgraphs](#Subgraphs)).

Have a look at the included [Samples](#Samples) for some more concrete usage pointers.

### VFXPropertyBinder

Use VFXGraph's own VFXPropertyBinder and add a VFXPath property binding. There are bindings available for SplineContainer and for PathCreator, but they will only show up if you correctly installed any of the [dependencies](#Dependencies), see above.

![image](https://user-images.githubusercontent.com/3404365/192542095-3daa3384-c7db-4aa9-96ec-9008b3dcb550.png)

This will create small textures containing the spline's sampled positions and rotations, just like other point caches.

The bindings will also automatically transfer some meta info about the spline's bounds and the point count to the VFX Graph.

You can increase/decrease the `PointCount` to get a tighter/looser fit:

https://user-images.githubusercontent.com/3404365/192543004-e49a71f2-c3fa-4fe2-9e84-bd6a51ee2940.mp4

Add `VFXPATH_DEBUG` to your Scripting Define Symbols to display the sampled positions like seen above.

### Subgraphs

![image](https://user-images.githubusercontent.com/3404365/192536902-2bf1e39a-e8fb-4490-a0e7-8c7a68a58d9c.png)

There are some subgraphs to simplify accessing and processing the spline's data to be usable. 

## Samples

There are a few simpler sample graphs included:

1. Import the "Common Graphs" sample via Package Manager.
2. Import the "Spline Scene" sample if you want to use Unity's Spline package and/or import the "PathCreator Scene" if you want to use PathCreator.
3. Find and open the imported scenes below `Assets\Samples\VFX Path`
4. Tweak the existing splines and graphs, create your own!

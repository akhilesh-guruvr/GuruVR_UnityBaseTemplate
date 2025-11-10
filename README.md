# GuruVR_UnityBaseTemplate
GuruVR_UnityBaseTemplate is a base Unity project designed as a starting point for building XR applications. This project serves as the groundwork for future GuruVR development, providing a clean, organized environment to build upon.

# 🕹️ Hand and Controller Locomotion Setup Guide

This guide explains how to add **hand and controller locomotion** (teleportation and hand gestures) to any Unity scene using the **LocomotionTemplate** as a reference.

---

## 📦 Prerequisites

- Unity project with **Oculus Integration** (or equivalent XR setup).
- A functional **LocomotionTemplate** scene included in your project.
- Basic understanding of Unity’s Scene Hierarchy and components.

---

## 🚀 Steps to Add Locomotion to a Scene

### 1. Open the Template Scene

1. Open the scene:
Assets/Scenes/LocomotionTemplate.unity

2. In the Hierarchy, locate the following GameObjects:
- `OVRCameraRig`
- `LGestureTutorials`
- `Teleport NavMesh`

---

### 2. Copy Required GameObjects

1. Select and copy these GameObjects from the **LocomotionTemplate** scene:
- ✅ `OVRCameraRig` — Handles head tracking, controller input, and hand tracking.
- ✅ `LGestureTutorials` — Contains hand gesture locomotion logic.
- ✅ `Teleport NavMesh` — Handles teleportation logic and NavMesh setup.
2. Open your **target scene**.
3. Paste the copied GameObjects into the Hierarchy.

---

### 3. Set Up the NavMesh Surface

Your existing `Teleport NavMesh` GameObject will host the **NavMeshSurface** component used for teleportation.

#### 🧭 Why Attach It Here?

Keeping the **NavMeshSurface** under the `Teleport NavMesh` object helps organize all teleport-related systems in one place and ensures consistent reference management.

#### 🧱 Steps

1. In your target scene, **expand** the `Teleport NavMesh` GameObject.
2. Create a new child GameObject:
Right-click → Create Empty → Rename to "NavMeshSurface"
3. With `NavMeshSurface` selected, add the component:
Component → AI → NavMesh Surface
4. Configure the **NavMeshSurface** component:
- **Agent Type:** `Humanoid`
- **Include Layers:** Select the layers containing your walkable ground/floor.
- **Default Area:** `Walkable`
- *(Optional)* Adjust **Bake Settings** if your environment is large or complex.
5. Click **Bake** to generate the NavMesh.

> ✅ **Note:** You only need **one NavMeshSurface** in your scene — it can bake navigation data for multiple ground meshes as long as their layers are included.

---

### 4. Verify Teleportation and Locomotion

1. Enter **Play Mode**.
2. Use your controller or perform a teleport gesture:
- Point to a walkable area.
- A teleport reticle should appear.
- Release the trigger or complete the gesture to teleport.

#### 🧩 If Teleportation Doesn’t Work:
- Ensure the **NavMeshSurface** is baked successfully (blue walkable areas visible in Scene view).
- Confirm the `Teleport NavMesh` GameObject is **active**.
- Verify your ground meshes are on the **included layers**.

---

### 5. Optional Adjustments

- Modify teleport settings and reticle visuals under `Teleport NavMesh`.
- Tune gesture behavior in `LGestureTutorials`.
- Adjust head and hand tracking in `OVRCameraRig`.

---

## ✅ Summary

| Task | Description |
|------|--------------|
| Copy Objects | `OVRCameraRig`, `LGestureTutorials`, `Teleport NavMesh` from `LocomotionTemplate` |
| Add NavMeshSurface | Create as a **child** of `Teleport NavMesh` |
| Agent Type | **Humanoid** |
| Default Area | **Walkable** |
| Bake NavMesh | Include all walkable layers |
| Test Teleportation | Use controller or hand gestures to verify teleport works |

---

## 🧩 Notes

- The **NavMeshSurface** can be rebaked whenever the environment changes.
- You only need **one NavMeshSurface** per scene (even if multiple walkable meshes exist).
- For multi-level environments, consider adding **NavMesh Links** between floors.

---

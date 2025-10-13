<div align="center">
  <img src="https://github.com/user-attachments/assets/57902f5f-a6f7-4091-a4d4-b5da3bcca07d"
       alt="ML2InfraredTracking logo" width="192">
</div>

<h1 align="center">ML2InfraredTracking</h1>

<p align="center">
  Magic Leap 2 plugin for tracking tools equipped with infrared retroreflective markers using the Depth (RAW) sensor.
  <br/>
  <b>Status:</b> tracking is <b>stable with 5 co-planar markers</b>. <b>4-marker</b> mode is under active development.
</p>

<div align="center">
  <img
    src="https://github.com/user-attachments/assets/0c1ac69a-985b-450e-a2c6-fcc5e9d3a6b6"
    alt="IR tracking demo GIF"
    width="360"
    style="margin-right:16px;"
  />
  <img
    src="https://github.com/user-attachments/assets/70be2177-1cf8-489d-9b29-31e20ef32820"
    alt="IR tracking demo GIF"
    width="360"
    style="margin-left:16px;"
  />
  <br/>
  <p>Example: a tool tracked by the Magic Leap 2 Depth RAW (IR) sensor</p>
</div>

---

## Table of Contents
- [Overview](#overview)
- [Before You Start (Critical Sync Note)](#before-you-start-critical-sync-note)
- [Using in Another Unity Project](#using-in-another-unity-project)
- [What This Sample Provides](#what-this-sample-provides)
- [Project Setup](#project-setup)
  - [Define Your Tool (TrackedTool GameObject)](#define-your-tool-trackedtool-gameobject)
  - [Scripts](#scripts)
  - [(Optional) Visualize the Depth Frame](#optional-visualize-the-depth-frame)
- [Versions & Requirements](#versions--requirements)
- [Quick Start](#quick-start)
- [Notes & Tips](#notes--tips)
- [Roadmap](#roadmap)
- [License](#license)

---

## Overview

**ML2InfraredTracking** is a Unity sample/plugin that demonstrates robust **infrared retroreflective marker** tracking on **Magic Leap 2** using the **Depth RAW** stream.  
You define a custom tool geometry and its marker constellation (in meters) and the plugin estimates the tool pose from IR depth data.

> **Current stability:** Solid with **5 co-planar markers**.  
> **4-marker mode:** Under hardening.

---

## Before You Start (Critical Sync Note)

To prevent hologram drift you **must synchronize** the **depth sensor pose** with the **exact depth frame** you process.  
This requires a **small SDK change**: modify the function that retrieves the depth sensor pose so it accepts an **additional property** in the `GetPose` call.

Follow the steps in the Magic Leap forum thread:  
https://forum.magicleap.cloud/t/hologram-drift-issue-when-tracking-object-with-retroreflective-markers-using-depth-camera-raw-data/5618/5

> Without this sync, pose/frame misalignment will cause visible drift.

---

## Using in Another Unity Project

1) **Apply the SDK change**  
   Update the SDK function that fetches the depth sensor pose to pass the extra property to `GetPose` (see link above).

2) **Copy the native plugin**  
   In this repo the binary is at:
Assets/Plugins/Android/libs/x86_64/libml2irtrackingplugin.so

sql
Copy code
Copy **all required `.so` files** into your new project under:
Assets/Plugins/Android/libs/x86_64/

yaml
Copy code

3) **Verify Unity importer settings**  
- Select each `.so` in Unity: **Platform = Android**, **CPU = x86_64**.  
- Uncheck **Any Platform** if you only target Android.

4) **Rebuild & run** on Magic Leap 2.

---

## What This Sample Provides

- A minimal pipeline to ingest **Depth RAW** frames and estimate tool pose from **retroreflective IR markers**.
- A **TrackedTool** prefab where you provide:
- the tool’s **3D geometry** (for visualization), and
- the **marker coordinates** in the tool’s frame (**meters**).
- Example materials and a basic **Depth RAW** visualization path.

---

## Project Setup

### Define Your Tool (TrackedTool GameObject)

- **Tool** — the tool’s **3D mesh** (purely for visualization).
- **Marker** — the **marker coordinates** in the tool’s reference frame (**meters**).

<div align="center">
<img width="251" height="114" alt="TrackedTool inspector" src="https://github.com/user-attachments/assets/879c8c8d-8cd7-445a-908a-792d5310fba9" />
</div>

> ⚠️ Keep units in **meters** to obtain a correct pose scale.

### Scripts

#### 1) `IRToolManager` — tracking logic
Attach and configure **IRToolManager** to run the IR tool tracking.

<div align="center">
<img width="559" height="437" alt="IRToolManager inspector" src="https://github.com/user-attachments/assets/da191758-b1dd-4c8a-a33d-dc4c09d012db" />
</div>

- Drag & drop the **Markers** GameObject list.  
- Set the **Tool** to be tracked.

#### 2) `DepthSensor` API — enable Depth RAW streaming
This component enables depth streaming.  
Set **XROrigin** (Camera Offset) and assign **IRToolManager** to the stream visualizer field.

#### 3) `ReprojectionTest` — stabilize holograms
Attach to any GameObject to enforce **reprojection to depth**, improving hologram stability (especially for the tracked tool).

### (Optional) Visualize the Depth Frame

Render the depth image on a quad/plane:

- Material: **`DepthMat`**  
- Frame type: **Depth RAW**  
- Frame Type Material: **`DepthRawMat`**

Materials are included in the project’s **Materials** folder.

<div align="center">
<img width="540" height="196" alt="Depth material setup" src="https://github.com/user-attachments/assets/a5d3fc9f-9858-4d6b-b60b-bf3842cb9ced" />
</div>

---

## Versions & Requirements

- **Unity**: 2022.3.61f1 (LTS recommended)  
- **Magic Leap 2**: ML2 OS **1.12.0**, MLSDK **1.12.0**  
- App permissions to access **Depth**  
- (Recommended) Proper **depth camera calibration** (e.g., checkerboard workflow)

---

## Quick Start

1. Clone the repo and open it in Unity.  
2. Apply the **SDK pose function modification** (see [Before You Start](#before-you-start-critical-sync-note)).  
3. In the scene, select **`TrackedTool`**:
- Assign the **Tool** mesh.
- Enter **Marker** positions (meters) in the tool frame.
4. On **`IRToolManager`**:
- Assign the **Markers** list.
- Select the target **Tool**.
5. (Optional) Add a quad with **`DepthMat`** to preview **Depth RAW**.
6. Build & run on **Magic Leap 2**.

---

## License and Citations

If you use this project or the library contained within, please cite:

```bibtex
@misc{Albanesi2025ML2InfraredTracking,
  author       = {Alessandro Albanesi},
  title        = {ML2InfraredTracking: Magic Leap 2 Infrared Tracking},
  howpublished = {\url{https://github.com/aless98/ML2InfraredTracking}},
  year         = {2025}
}
```

A. Albanesi, ML2InfraredTracking: Magic Leap 2 Infrared Tracking. 2025. [Online]. Available: https://github.com/aless98/ML2InfraredTracking



<p align="center">
  <img src="https://github.com/user-attachments/assets/57902f5f-a6f7-4091-a4d4-b5da3bcca07d"
       alt="logo black repo github" width="256">
</p>

# ML2InfraredTracking
Magic Leap 2 plugin for IR-marker tool tracking. tracking is stable with 5 co-planar markers. 4-marker mode under active development. 

<div align="center">
  <img src="https://github.com/user-attachments/assets/0c1ac69a-985b-450e-a2c6-fcc5e9d3a6b6"
       alt="demo gif">
  <p>Example of tool tracked by MagicLeap2 Depth Raw (IR) sensor</p>
</div>


<div align="center">
  <h1>ML2InfraredTracking</h1>
  <p><em>Magic Leap 2 plugin for tracking tools equipped with infrared retroreflective markers using the Depth (RAW) sensor.</em></p>

  <p>
    <strong>Status:</strong> tracking is <b>stable with 5 markers</b>. <b>4-marker</b> mode is under active development.
  </p>
</div>

---

## 🚀 Before starting

If you want to use the plugin in another project please be aware that you need to synchronize the **depth sensor pose** with the **acquired depth frame** (and prevent hologram drift). To achieve that, you must **modify the SDK function** that retrieves the depth sensor pose so it accepts an additional property in the `GetPose` call. The plugin libml2irtrackingplugin.so can be found under Assets/Plugins/Android/libs/x86_64. Clone all the .so files into the Plugin directory of your new project.

👉 Follow the steps detailed in the developer forum:  
https://forum.magicleap.cloud/t/hologram-drift-issue-when-tracking-object-with-retroreflective-markers-using-depth-camera-raw-data/5618/5

---

## 📦 What this sample provides

This repository shows how to use **Depth RAW** to achieve **tool tracking** on Magic Leap 2.  
You will define your custom tool geometry and its retroreflective marker constellation, then run the tracker.

---

## 🧱 Project setup

### 1) Define your tool (TrackedTool GameObject)

- **Tool**: the tool’s **3D geometry** file (mesh to visualize).
- **Marker**: the **marker coordinates** in the tool’s reference frame (**meters**).

<div align="center">
  <img width="251" height="114" alt="TrackedTool inspector" src="https://github.com/user-attachments/assets/879c8c8d-8cd7-445a-908a-792d5310fba9" />
</div>

> ⚠️ Units must be **meters** for pose estimation to be correct.

### 2) IRToolManager (tracking script)

Attach and configure **IRToolManager** to run the tracking logic.

<div align="center">
  <img width="559" height="437" alt="IRToolManager inspector" src="https://github.com/user-attachments/assets/da191758-b1dd-4c8a-a33d-dc4c09d012db" />
</div>

- Drag & drop the **Markers** GameObject list.
- Set the **Tool** to be tracked.

### 3) (Optional) Visualize the depth frame

For visualization, render the depth image on a quad/plane:

- Material: **`DepthMat`**
- Frame type: **Depth RAW**
- Frame Type Material: **`DepthRawMat`**

You can find these in the project’s **Materials** folder.

<div align="center">
  <img width="540" height="196" alt="Depth material setup" src="https://github.com/user-attachments/assets/a5d3fc9f-9858-4d6b-b60b-bf3842cb9ced" />
</div>

---

## 🛠 Versions & Requirements

- **Unity** (2022.3.61f1)
- **Magic Leap 2** ML2 OS Version: 1.12.0, MLSDK Version: 1.12.0.
- Permissions for **Depth** access enabled in your ML2 project
- (Recommended) Proper depth camera calibration. (for this project the calibration was achieved using a chechboard)

---

## ▶️ Quick start

1. Clone the repo and open the project in Unity.
2. Apply the **SDK pose function modification** described in the forum thread (see “Before starting”).
3. In the scene, select **`TrackedTool`** and:
   - Assign **Tool** mesh.
   - Enter **Marker** positions (meters) in the tool’s frame.
4. On **`IRToolManager`**:
   - Assign the **Markers** list.
   - Set the target **Tool**.
5. (Optional) Add a quad with **`DepthMat`** to preview the depth frame.
6. Build & run on Magic Leap 2.

---

## 📌 Notes & tips

- **Units**: keep everything in **meters** (tool geometry and marker coordinates).
- **Pose stability**: robust with **5 markers**; 4-marker (planar/IPPE) mode is being hardened.
- **Depth RAW**: ensure you request and render **Depth RAW** frames if you need visualization.
- **Performance**: avoid excessive allocations in update loops; reuse buffers where possible.

---

## 🧭 Roadmap

- [x] Stable 5-marker tracking  
- [ ] Harden 4-marker planar mode (IPPE dual-solution handling + hysteresis)  
- [ ] Additional sample scenes and prefabs  
- [ ] Optional temporal filters (Kalman/EMA) presets

---

## 📄 License

MIT (or your preferred license)




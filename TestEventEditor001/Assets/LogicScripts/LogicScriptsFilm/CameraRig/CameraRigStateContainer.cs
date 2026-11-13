using System;
using UnityEngine;

[Serializable]
[Config(configId: "logic_state_camera_rig", priority: 100)]
public class CameraRigStateContainer : ConfigDefinition
{
    public CameraRigEntry[] entries;

    [Serializable]
    public class CameraRigEntry : ILogicStateEntry
    {
        public string uuid;
        public string tag;

        public string cameraId = "Camera_Main";

        public float attachDistance = 3f;
        public float attachHeight = 1.5f;

        public float camDistance = 5f;
        public float camYaw = 0f;
        public float camPitch = 20f;
        public float camHeight = 1.5f;

        public float shoulderOffset = 2.0f;

        public float snapshotDelay = 0.15f;
        public float followLerp = 12f;

        public string GetUUID() => uuid;
        public string GetTag() => tag;
    }
}
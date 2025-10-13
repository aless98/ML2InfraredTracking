// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2018-2022) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using UnityEngine;
using TMPro;
using System.Collections;

namespace MagicLeap.MRTK.Samples.KeywordRecognition
{
    public class VoiceEnabledStatus : MonoBehaviour
    {
        [SerializeField]
        private GameObject voiceEnabledVisual;
        [SerializeField]
        private GameObject statusMessage;

#if  !UNITY_EDITOR && UNITY_ANDROID
        void Start()
        {
            checkVoiceInputEnabled();
            StartCoroutine(pollStatus());
        }
#endif

        private IEnumerator pollStatus()
        {
            while (true)
            {
                checkVoiceInputEnabled();
                yield return new WaitForSeconds(1f);
            }
        }

        private void checkVoiceInputEnabled()
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var contentResolver = currentActivity.Call<AndroidJavaObject>("getContentResolver"))
            using (var systemSettings = new AndroidJavaClass("android.provider.Settings$System"))
            {
                int voiceEnabled = systemSettings.CallStatic<int>("getInt", contentResolver, "enable_voice_cmds");
                setVisualColor(voiceEnabled == 1);
                setStatusMessage(voiceEnabled == 1);
            }
        }

        private void setVisualColor(bool enabled)
        {
            if (voiceEnabledVisual != null)
            {
                MeshRenderer meshRenderer = voiceEnabledVisual.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    meshRenderer.material.color = enabled ? Color.green : Color.red;
                }
            }
        }

        private void setStatusMessage(bool enabled)
        {
            if (statusMessage != null)
            {
                TextMeshProUGUI statusText = statusMessage.GetComponent<TextMeshProUGUI>();
                statusText.text = enabled ? "Ok" : "Voice Input is Disabled in OS Settings";
            }
        }
    }
}
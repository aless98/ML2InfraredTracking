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
using UnityEngine.XR.MagicLeap;
using MixedReality.Toolkit.UX;

namespace MagicLeap.MRTK.Samples.GlobalSegmentedDimmer
{
    public class GlobalDimmer : MonoBehaviour
    {

        [SerializeField]
        private PressableButton globalDimmerToggle;
        [SerializeField]
        private Slider globalDimmerSlider;

        private void Start()
        {
            if (globalDimmerToggle == null || globalDimmerSlider == null)
            {
                Debug.LogWarning("MRTK3 GlobalAndSegmentedDimmerSample: GameObject is null disabling script.");
                enabled = false;
            }
        }

        public void OnGlobalDimmerToggleSelect()
        {
            if (globalDimmerToggle.IsToggled)
            {
                MLGlobalDimmer.SetValue(globalDimmerSlider.Value);
            }
            else
            {
                MLGlobalDimmer.SetValue(0.0f);
            }
        }

        public void OnGlobalDimmerSliderUpdate(SliderEventData eventData)
        {
            if (globalDimmerToggle.IsToggled)
            {
                MLGlobalDimmer.SetValue(eventData.NewValue);
            }
        }
    }
}
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
    public class SegmentedDimmer : MonoBehaviour
    {
        [SerializeField]
        private PressableButton segmentedDimmerToggle;

        // Start is called before the first frame update
        void Start()
        {
            if (segmentedDimmerToggle == null)
            {
                Debug.LogWarning("MRTK3 GlobalAndSegmentedDimmerSample: GameObject is null disabling script.");
                enabled = false;
            }
        }

        public void OnSegmentedDimmerToggleSelect()
        {
            if (segmentedDimmerToggle.IsToggled)
            {
                MLSegmentedDimmer.Activate();
            }
            else
            {
                MLSegmentedDimmer.Deactivate();
            }
        }
    }
}
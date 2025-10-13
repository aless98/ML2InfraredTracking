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

namespace MagicLeap.MRTK.Samples.KeywordRecognition
{
    public class PositionChanger : MonoBehaviour
    {
        private float minX = 0.0f;
        private float maxX = 0.4f;
        private float increment = 0.2f;

        public void SwapPosition()
        {
            Transform objTransform = this.gameObject.transform;
            float currX = objTransform.localPosition.x;
            float currY = objTransform.localPosition.y;
            float currZ = objTransform.localPosition.z;
            float nextX = currX + increment;
            objTransform.localPosition = new Vector3(currX == maxX ? minX : nextX, currY, currZ);
        }
    }
}

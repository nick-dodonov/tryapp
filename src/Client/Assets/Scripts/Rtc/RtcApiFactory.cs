using System;
using Shared.Meta.Api;
using Shared.Rtc;
using UnityEngine;

namespace Client.Rtc
{
    public static class RtcApiFactory
    {
        public static IRtcApi CreateRtcClient(IMeta meta)
        {
#if UNITY_EDITOR
            if (Application.isEditor)
                return new UnityRtcApi(meta);
#endif
            if (Application.platform == RuntimePlatform.WebGLPlayer)
                return new WebglRtcApi(meta);

            throw new NotSupportedException($"Unsupported platform: {Application.platform}");
        }
    }
}
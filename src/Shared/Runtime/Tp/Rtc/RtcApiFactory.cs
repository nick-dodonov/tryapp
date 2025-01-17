using System;
using Shared.Meta.Api;
#if UNITY_5_6_OR_NEWER
using Shared.Tp.Rtc.Unity;
using Shared.Tp.Rtc.Webgl;
using UnityEngine;
#endif

namespace Shared.Tp.Rtc
{
    public static class RtcApiFactory
    {
        public static ITpApi CreateRtcClient(IMeta meta)
        {
#if UNITY_5_6_OR_NEWER
#if UNITY_EDITOR || !UNITY_WEBGL
            if (Application.isEditor)
                return new UnityRtcApi(meta);
#endif
            if (Application.platform == RuntimePlatform.WebGLPlayer)
                return new WebglRtcApi(meta);
            throw new NotSupportedException($"Unsupported platform: {Application.platform}");
#else
            throw new NotSupportedException($"TODO: add Sip");
#endif
        }
    }
}
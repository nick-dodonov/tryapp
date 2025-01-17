using System;
#if UNITY_5_6_OR_NEWER
using Shared.Tp.Rtc.Unity;
using Shared.Tp.Rtc.Webgl;
using UnityEngine;
#endif

namespace Shared.Tp.Rtc
{
    public static class RtcApiFactory
    {
        public static ITpApi CreateApi(IRtcService service)
        {
#if UNITY_5_6_OR_NEWER
#if UNITY_EDITOR || !UNITY_WEBGL
            if (Application.isEditor)
                return new UnityRtcApi(service);
#endif
            if (Application.platform == RuntimePlatform.WebGLPlayer)
                return new WebglRtcApi(service);

            throw new NotSupportedException($"Unsupported platform: {Application.platform}");
#else
            throw new NotSupportedException("TODO: use this factory add SipRtcService to ASP hosting");
#endif
        }
    }
}
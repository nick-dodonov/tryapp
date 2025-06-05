namespace Shared.Tp.Rtc.Unity
{
    public static class RtcApiFactory
    {
        public static ITpApi CreateApi(IRtcService service)
        {
#if UNITY_5_6_OR_NEWER
#if UNITY_EDITOR || !UNITY_WEBGL
            if (UnityEngine.Application.isEditor)
                return new Unity.UnityRtcApi(service);
#endif
            var platform = UnityEngine.Application.platform;
            if (platform == UnityEngine.RuntimePlatform.WebGLPlayer)
                return new Webgl.WebglRtcApi(service);

            // desktop / android / ios
            return new Unity.UnityRtcApi(service);
#else
            throw new System.NotSupportedException("TODO: use this factory add SipRtcService to ASP hosting");
#endif
        }
    }
}
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
#if UNITY_WEBGL
            return new Webgl.WebglRtcApi(service);
#else
            // desktop / android / ios
            return new Unity.UnityRtcApi(service);
#endif
#else
            throw new System.NotSupportedException("TODO: use this factory add SipRtcService to ASP hosting");
#endif
        }
    }
}
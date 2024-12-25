using System;
using System.Threading;
using System.Threading.Tasks;
using Shared.Meta.Api;
using UnityEngine;

namespace Rtc
{
    public interface IRtcClient
    {
        public Task<string> TryCall(IMeta meta, CancellationToken cancellationToken);
    }

    public static class RtcClientFactory
    {
        public static IRtcClient CreateRtcClient()
        {
#if UNITY_EDITOR
            if (Application.isEditor)
                return new UnityRtcClient();
#endif
            if (Application.platform == RuntimePlatform.WebGLPlayer)
                return new WebglRtcClient();

            throw new NotSupportedException($"Unsupported platform: {Application.platform}");
        }
    }
}
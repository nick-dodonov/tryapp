using System;
using System.Threading;
using System.Threading.Tasks;
using Shared.Meta.Api;
using UnityEngine;

namespace Rtc
{
    public interface IRtcLink : IDisposable
    {
        public delegate void ReceivedCallback(byte[] bytes); //null - disconnected
        void Send(byte[] bytes);
    }

    public interface IRtcApi
    {
        Task<IRtcLink> Connect(IRtcLink.ReceivedCallback receivedCallback, CancellationToken cancellationToken);
    }

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
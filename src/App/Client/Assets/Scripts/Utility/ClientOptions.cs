using System;
using System.Threading.Tasks;

namespace Client.Utility
{
    [Serializable]
    public class ClientOptions
    {
        public string Locator;
        public string DebugAbsoluteUrl;

        private static ClientOptions _instance;

        public static ClientOptions Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;
                throw new InvalidOperationException($"{nameof(ClientOptions)}: obtaining before initializing");
            }
        }

        private static Task<ClientOptions> _instanceTask;

        public static ValueTask<ClientOptions> InstanceAsync =>
            _instance != null ? new(_instance) : Read();

        private static ValueTask<ClientOptions> Read()
        {
            if (_instanceTask != null)
                return new(_instanceTask);

            //TODO: use Application.exitCancellationToken
            var valueTask = ClientOptionsReader.ReadOptions();
            if (valueTask.IsCompleted)
            {
                _instance = valueTask.Result;
                return new(_instance);
            }

            _instanceTask = valueTask.AsTask().ContinueWith(
                t => (_instance = t.Result), 
                TaskScheduler.FromCurrentSynchronizationContext());
            return new(_instanceTask);
        }
    }
}
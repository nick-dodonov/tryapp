using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Text;
using Locator.Client;
using Shared.Log;
using Shared.Web;

namespace Client.Utility
{
    internal class ServerItem
    {
        private string _name;
        private string _url;
        private string _kind;
        private bool _default;

        public ServerItem(string name, string url, string kind)
        {
            _name = name;
            _url = url;
            _kind = kind;
        }

        public void SetDefault(bool @default) => _default = @default;

        public void Update(ServerItem item)
        {
            _name = item._name;
            _url = item._url;
            _kind = item._kind;
        }

        public override string ToString() => $"\"{_name}\" ({_kind}) {_url}";

        public string Url => _url;

        public string Text
        {
            get
            {
                var sb = ZString.CreateStringBuilder(true);
                try
                {
                    // align left/right on same line:
                    //  <align=left>localhost<line-height=0>
                    //  <align=right><i>(debug)</i><line-height=1em>

                    sb.Append("<line-height=0>");

                    if (_default)
                        sb.Append("<b>");
                    sb.Append(_name);
                    if (_default)
                        sb.Append("</b>");

                    sb.Append("\n<align=right><line-height=1em>");

                    sb.Append("<i><mspace=0.55em>");
                    sb.Append(_kind);
                    //sb.Append("</mspace></i>");

                    return sb.ToString();
                }
                finally
                {
                    sb.Dispose();
                }
            }
        }
    }

    internal class ServerList : IEnumerable<ServerItem>
    {
        private static readonly Slog.Area _log = new();

        private bool _defaultAdded;
        private readonly List<ServerItem> _items = new();

        public ServerItem this[int index] => _items[index];
        public List<ServerItem>.Enumerator GetEnumerator() => _items.GetEnumerator();
        IEnumerator<ServerItem> IEnumerable<ServerItem>.GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

        public static ServerList CreateDefault()
        {
            var result = new ServerList();
            result.AddLocalhostItems();
            result.AddDefaultItem();
            return result;
        }

        public static async ValueTask<ServerList> CreateStandsAsync(CancellationToken cancellationToken)
        {
            var result = new ServerList();
            result.AddLocalhostItems();
            result.AddDefaultItem();

            await ClientOptions.InstanceAsync;
            await result.AddStandsAsync(cancellationToken);
            return result;
        }

        private void AddLocalhostItems()
        {
            if (!DetectLocalhost(out var localhost))
                return;
            Add(new("local (ide)", $"http://{localhost}:5270", "debug"));
            Add(new("local (http)", $"http://{localhost}", "debug"));
            Add(new("local (ssl)", $"https://{localhost}", "debug"));
        }

        private static bool DetectLocalhost(out string localhost)
        {
            localhost = "localhost";
            var absoluteURL = ClientApp.AbsoluteUrl;
            if (string.IsNullOrEmpty(absoluteURL))
                return true; // running in editor

            if (absoluteURL.Contains("localhost"))
                return true;

            localhost = "127.0.0.1";
            if (absoluteURL.Contains(localhost))
                return true;

            return false;
        }

        private void AddDefaultItem()
        {
            // local build cannot specify default server
            if (DetectLocalhost(out _))
                return;

            var url = ClientApp.AbsoluteUrl;
            var uri = new Uri(url);
            url = uri.GetLeftPart(UriPartial.Path);

            // get rid of index.html if specified and trim trailing '/' if exists
            url = Path.HasExtension(url)
                ? url[..url.LastIndexOf('/')]
                : url.TrimEnd('/');

            // get last path part as stand name
            var name = url[(url.LastIndexOf('/') + 1)..];

            Add(new(name, url, "default"));
        }

        private static string GetLocatorUrlAsync()
        {
            var url = ClientOptions.Instance.Locator;
            if (url != null)
                return url;

            url = ClientApp.AbsoluteUrl;
            if (!string.IsNullOrEmpty(url))
            {
                url = new Uri(url).GetLeftPart(UriPartial.Authority);
                return $"{url}/-/";
            }

            return null;
        }

        private async Task AddStandsAsync(CancellationToken cancellationToken)
        {
            try
            {
                var locatorUrl = GetLocatorUrlAsync();
                if (locatorUrl == null)
                    return;

                using var locatorWebClient = new UnityWebClient(locatorUrl);
                var locator = new ClientLocator(locatorWebClient);
                var stands = await locator.GetStands(cancellationToken);
                foreach (var stand in stands)
                {
                    var kind = "stand";
                    if (!string.IsNullOrEmpty(stand.Sha))
                        kind = stand.Sha[..7];

                    var serverOption = new ServerItem(stand.Name, stand.Url, kind);
                    _log.Info($"add locator server: {serverOption}");
                    AddOrUpdate(serverOption);
                }
            }
            catch (Exception e)
            {
                _log.Error($"{e}");
            }
        }

        private void Add(ServerItem serverItem)
        {
            if (!_defaultAdded)
            {
                _defaultAdded = true;
                serverItem.SetDefault(true);
            }

            _items.Add(serverItem);
        }

        private void AddOrUpdate(ServerItem serverItem)
        {
            foreach (var item in _items)
            {
                if (item.Url != serverItem.Url)
                    continue;

                item.Update(serverItem);
                return;
            }

            Add(serverItem);
        }
    }
}
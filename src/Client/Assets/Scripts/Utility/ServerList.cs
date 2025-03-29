using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Locator.Client;
using Shared.Log;
using Shared.Web;

namespace Client.Utility
{
    internal record ServerItem(string Text, string Url)
    {
        public override string ToString() => $"\"{Text}\" {Url}";
    }

    internal class ServerList : List<ServerItem>
    {
        private static readonly Slog.Area _log = new();

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
            
            await Task.Delay(5000, cancellationToken); //XXXXXXXXXXXXXXXXXXXXXXXXX
            
            await result.AddStandsAsync(cancellationToken);
            return result;
        }

        private void AddLocalhostItems()
        {
            if (DetectLocalhost(out var localhost))
            {
                //<align=left>localhost<line-height=0>
                //<align=right><i>(debug)</i><line-height=1em>
                Add(new("localhost <i>(debug)</i>", $"http://{localhost}:5270"));
                Add(new("localhost <i>(http)</i>", $"http://{localhost}"));
                Add(new("localhost <i>(ssl)</i>", $"https://{localhost}"));
            }
        }

        private static bool DetectLocalhost(out string localhost)
        {
            localhost = "localhost";
            var absoluteURL = Startup.AbsoluteUrl;
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
            // default via deployed client location 
            var url = Startup.AbsoluteUrl;
            if (string.IsNullOrEmpty(url))
                return;

            var uri = new Uri(url);
            url = uri.GetLeftPart(UriPartial.Path);

            // get rid of index.html if specified and trim trailing '/' if exists
            url = Path.HasExtension(url) 
                ? url[..url.LastIndexOf('/')] 
                : url.TrimEnd('/');

            // get last path part as stand name
            var name = url[(url.LastIndexOf('/') + 1)..];

            Add(new($"{name} <i>(hosting)</i>", url));
        }

        private static async ValueTask<string> GetLocatorUrlAsync()
        {
            var options = await OptionsReader.TryReadOptions();
            var url = options?.Locator;
            if (url != null)
                return url;

            url = Startup.AbsoluteUrl;
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
                var locatorUrl = await GetLocatorUrlAsync();
                if (locatorUrl == null)
                    return;

                using var locatorWebClient = new UnityWebClient(locatorUrl);
                var locator = new ClientLocator(locatorWebClient);
                var stands = await locator.GetStands(cancellationToken);
                foreach (var stand in stands)
                {
                    var serverOption = new ServerItem($"{stand.Name} <i>(locator)</i>", stand.Url);
                    _log.Info($"add locator server: {serverOption}");
                    Add(serverOption);
                }
            }
            catch (Exception e)
            {
                _log.Error($"{e}");
            }
        }
        
    }
}
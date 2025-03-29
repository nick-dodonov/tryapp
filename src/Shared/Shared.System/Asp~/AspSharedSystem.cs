namespace Shared.System;

public class AspSharedSystem : ISharedSystem
{
    public Task<string> HttpContent_ReadAsStringAsync(HttpContent content, CancellationToken cancellationToken) =>
        content.ReadAsStringAsync(cancellationToken);
}
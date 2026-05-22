using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Cryptiklemur.RimObs.Collector.Api;

public static class SpaEndpoints {
    private const string ResourcePrefix = "Dashboard.";
    private static readonly Assembly Host = typeof(SpaEndpoints).Assembly;

    private static readonly Dictionary<string, string> ContentTypes = new() {
        [".html"] = "text/html; charset=utf-8",
        [".js"] = "text/javascript; charset=utf-8",
        [".css"] = "text/css; charset=utf-8",
        [".map"] = "application/json; charset=utf-8",
        [".json"] = "application/json; charset=utf-8",
        [".woff"] = "font/woff",
        [".woff2"] = "font/woff2",
        [".svg"] = "image/svg+xml",
        [".png"] = "image/png",
        [".ico"] = "image/x-icon",
    };

    public static IEndpointRouteBuilder MapSpaEndpoints(this IEndpointRouteBuilder endpoints) {
        endpoints.MapGet("/", () => ServeFile("index.html"));
        endpoints.MapGet("/assets/{**path}", (string path) => ServeFile("assets/" + path));
        endpoints.MapFallback((HttpContext context) => {
            if (context.Request.Path.StartsWithSegments("/api")) {
                return Results.NotFound();
            }

            return ServeFile("index.html");
        });

        return endpoints;
    }

    private static IResult ServeFile(string relativePath) {
        Stream? stream = Host.GetManifestResourceStream(ResourcePrefix + relativePath);
        if (stream is null) {
            return Results.NotFound();
        }

        return Results.Stream(stream, ContentTypeFor(relativePath));
    }

    private static string ContentTypeFor(string relativePath) {
        string extension = Path.GetExtension(relativePath);
        return ContentTypes.TryGetValue(extension, out string? type) ? type : "application/octet-stream";
    }
}

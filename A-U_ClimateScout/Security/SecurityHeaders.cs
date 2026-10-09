using System.Security.Cryptography;

namespace A_U_ClimateScout.Security
{
    // Security headers on every response (plan §9). The Content Security Policy lets the browser run only our own
    // script files plus inline scripts carrying this request's nonce (the theme snippet and import map in
    // _HeadAssets), so injected markup can't run code. Styles may be inline (Leaflet and Quill set style attributes);
    // everything else (images, fonts, fetches, forms) must come from this site. Pages may not be framed elsewhere.
    public static class SecurityHeaders
    {
        public const string NonceKey = "csp-nonce";

        public static string Nonce(HttpContext context) => context.Items[NonceKey] as string ?? "";

        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, bool development) =>
            app.Use(async (context, next) =>
            {
                // An error page is rendered by running the pipeline again for the same request: keep its first nonce
                // (and header) rather than adding a second one.
                if (context.Items.ContainsKey(NonceKey))
                {
                    await next();
                    return;
                }
                var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
                context.Items[NonceKey] = nonce;

                context.Response.OnStarting(() =>
                {
                    var headers = context.Response.Headers;
                    // In development, Visual Studio's browser refresh talks over a WebSocket.
                    var connect = development ? "'self' ws: wss:" : "'self'";
                    headers.ContentSecurityPolicy =
                        $"default-src 'self'; script-src 'self' 'nonce-{nonce}'; style-src 'self' 'unsafe-inline'; " +
                        $"img-src 'self' data:; font-src 'self'; connect-src {connect}; object-src 'none'; " +
                        "base-uri 'self'; form-action 'self'; frame-ancestors 'none'";
                    headers.XContentTypeOptions = "nosniff";
                    headers.XFrameOptions = "DENY";
                    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                    headers["Permissions-Policy"] = "geolocation=(self), camera=(), microphone=(), payment=(), usb=()";
                    headers["Cross-Origin-Opener-Policy"] = "same-origin";
                    return Task.CompletedTask;
                });

                await next();
            });
    }
}

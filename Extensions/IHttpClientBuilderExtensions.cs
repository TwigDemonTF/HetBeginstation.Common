using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using OpenTelemetry.Trace;

namespace HetBeginstation.Common.Extensions;

/// <summary>
/// Adds OpenTelemetry tracing to named/typed <see cref="IHttpClientFactory"/>
/// clients with custom span enrichment.
/// </summary>
public static class IHttpClientBuilderExtensions
{
    /// <summary>
    /// Adds a delegating handler that enriches every outgoing HTTP span with
    /// custom attributes.
    /// </summary>
    public static IHttpClientBuilder WithTracing(
        this IHttpClientBuilder builder,
        Action<System.Diagnostics.Activity>? enrichWith = null)
    {
        if (enrichWith is not null)
        {
            builder.AddHttpMessageHandler(sp => new TracingEnrichmentHandler(enrichWith));
        }

        // The global HttpClient instrumentation from AddOtel() already
        // creates spans. This extension is for adding extra attributes.
        return builder;
    }

    private sealed class TracingEnrichmentHandler : DelegatingHandler
    {
        private readonly Action<System.Diagnostics.Activity> _enrichWith;

        public TracingEnrichmentHandler(Action<System.Diagnostics.Activity> enrichWith)
            : base(new HttpClientHandler())
        {
            _enrichWith = enrichWith;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var activity = System.Diagnostics.Activity.Current;
            if (activity is not null)
                _enrichWith(activity);

            return await base.SendAsync(request, cancellationToken);
        }
    }
}

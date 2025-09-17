using MediatR;
using Microsoft.Extensions.Logging;
using SKFProductAssistant.Application.Interfaces;

namespace SKFProductAssistant.Application.Behaviors
{
    public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
        where TResponse : class
    {
        private readonly ICacheService _cacheService;
        private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

        public CachingBehavior(ICacheService cacheService, ILogger<CachingBehavior<TRequest, TResponse>> logger)
        {
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            // Only cache specific query types
            if (request is not ICacheable cacheableRequest)
            {
                return await next();
            }

            var cacheKey = cacheableRequest.CacheKey;

            // Try to get from cache
            var cachedResponse = await _cacheService.GetAsync<TResponse>(cacheKey);
            if (cachedResponse != null)
            {
                _logger.LogInformation("Cache hit for key: {CacheKey}", cacheKey);
                return cachedResponse;
            }

            // Execute the request
            var response = await next();

            // Cache the response
            await _cacheService.SetAsync(cacheKey, response, cacheableRequest.CacheExpiration);
            _logger.LogInformation("Cached response for key: {CacheKey}", cacheKey);

            return response;
        }
    }

    public interface ICacheable
    {
        string CacheKey { get; }
        TimeSpan CacheExpiration { get; }
    }
}

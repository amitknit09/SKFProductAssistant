using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SKFProductAssistant.Application.Interfaces;
using StackExchange.Redis;
using System.Text.Json;

namespace SKFProductAssistant.Infrastructure.Services
{
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IDatabase? _redisDatabase;
        private readonly ILogger<CacheService> _logger;
        private readonly bool _useRedis;

        public CacheService(IMemoryCache memoryCache, IConfiguration configuration, ILogger<CacheService> logger)
        {
            _memoryCache = memoryCache;
            _logger = logger;

            var redisConnection = configuration.GetConnectionString("Redis");
            _useRedis = !string.IsNullOrEmpty(redisConnection);

            if (_useRedis)
            {
                try
                {
                    var redis = ConnectionMultiplexer.Connect(redisConnection);
                    _redisDatabase = redis.GetDatabase();
                    _logger.LogInformation("Redis cache enabled");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to connect to Redis, using memory cache only");
                    _useRedis = false;
                }
            }
        }

        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            try
            {
                // Try memory cache first
                if (_memoryCache.TryGetValue(key, out T? memoryValue))
                {
                    return memoryValue;
                }

                // Try Redis
                if (_useRedis && _redisDatabase != null)
                {
                    var redisValue = await _redisDatabase.StringGetAsync(key);
                    if (redisValue.HasValue)
                    {
                        var deserializedValue = JsonSerializer.Deserialize<T>(redisValue!);
                        // Store in memory for faster access
                        _memoryCache.Set(key, deserializedValue, TimeSpan.FromMinutes(5));
                        return deserializedValue;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving from cache: {Key}", key);
                return null;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
        {
            try
            {
                var exp = expiration ?? TimeSpan.FromHours(1);

                // Set in memory cache
                _memoryCache.Set(key, value, exp);

                // Set in Redis
                if (_useRedis && _redisDatabase != null)
                {
                    var serializedValue = JsonSerializer.Serialize(value);
                    await _redisDatabase.StringSetAsync(key, serializedValue, exp);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cache: {Key}", key);
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                _memoryCache.Remove(key);

                if (_useRedis && _redisDatabase != null)
                {
                    await _redisDatabase.KeyDeleteAsync(key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing from cache: {Key}", key);
            }
        }

        public async Task InvalidatePatternAsync(string pattern)
        {
            try
            {
                if (_useRedis && _redisDatabase != null)
                {
                    var server = _redisDatabase.Multiplexer.GetServer(_redisDatabase.Multiplexer.GetEndPoints().First());
                    var keys = server.Keys(pattern: pattern);

                    foreach (var key in keys)
                    {
                        await _redisDatabase.KeyDeleteAsync(key);
                        _memoryCache.Remove(key.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating cache pattern: {Pattern}", pattern);
            }
        }
    }
}

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using RhSensoWebApi.Core.Interfaces;

namespace RhSensoWebApi.Infrastructure.Services;

public class CacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CacheService> _logger;

    public CacheService(IMemoryCache cache, ILogger<CacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public Task<T> GetAsync<T>(string key)
    {
        try
        {
            if (_cache.TryGetValue(key, out var value) && value is T typedValue)
            {
                return Task.FromResult(typedValue);
            }

            return Task.FromResult(default(T)!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao recuperar item do cache: {Key}", key);
            return Task.FromResult(default(T)!);
        }
    }

    public Task SetAsync<T>(string key, T value, TimeSpan expiration)
    {
        try
        {
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration,
                SlidingExpiration = TimeSpan.FromMinutes(5)
            };

            _cache.Set(key, value, options);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao armazenar item no cache: {Key}", key);
            return Task.CompletedTask;
        }
    }

    public Task RemoveAsync(string key)
    {
        try
        {
            _cache.Remove(key);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao remover item do cache: {Key}", key);
            return Task.CompletedTask;
        }
    }

    public Task RemoveByPatternAsync(string pattern)
    {
        _logger.LogWarning("RemoveByPattern não implementado para MemoryCache: {Pattern}", pattern);
        return Task.CompletedTask;
    }
}
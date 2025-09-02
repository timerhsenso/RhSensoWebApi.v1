using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using RhSensoWebApi.Core.Interfaces;

namespace RhSensoWebApi.Infrastructure.Cache;

public class CacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache? _distributedCache;
    private readonly ILogger<CacheService> _logger;
    
    public CacheService(
        IMemoryCache memoryCache,
        IDistributedCache? distributedCache,
        ILogger<CacheService> logger)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _logger = logger;
    }
    
    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            // L1 Cache (Memory) - Mais rápido
            if (_memoryCache.TryGetValue(key, out T? value))
            {
                return value;
            }
            
            // L2 Cache (Redis) - Para dados distribuídos
            if (_distributedCache != null)
            {
                var distributedValue = await _distributedCache.GetStringAsync(key);
                if (distributedValue != null)
                {
                    value = JsonSerializer.Deserialize<T>(distributedValue);
                    // Armazenar no L1 para próximas consultas
                    _memoryCache.Set(key, value, TimeSpan.FromMinutes(5));
                    return value;
                }
            }
            
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao recuperar item do cache: {Key}", key);
            return default;
        }
    }
    
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        try
        {
            var expiryTime = expiry ?? TimeSpan.FromMinutes(30);
            
            // L1 Cache (Memory)
            _memoryCache.Set(key, value, expiryTime);
            
            // L2 Cache (Redis)
            if (_distributedCache != null && value != null)
            {
                var serializedValue = JsonSerializer.Serialize(value);
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiryTime
                };
                await _distributedCache.SetStringAsync(key, serializedValue, options);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao armazenar item no cache: {Key}", key);
        }
    }
    
    public async Task RemoveAsync(string key)
    {
        try
        {
            _memoryCache.Remove(key);
            
            if (_distributedCache != null)
            {
                await _distributedCache.RemoveAsync(key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao remover item do cache: {Key}", key);
        }
    }
}


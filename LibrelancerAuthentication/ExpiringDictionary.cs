using System.Collections.Concurrent;

namespace LibrelancerAuthentication;

public class ExpiringDictionary<TKey, TValue> : IDisposable where TValue : IExpiringItem
{
    private ConcurrentDictionary<TKey, TValue> backing = new ConcurrentDictionary<TKey, TValue>();

    private PeriodicTimer timer;
    
    public ExpiringDictionary()
    {
        timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        Task.Run(CheckExpiry);    
    }

    public void Set(TKey key, TValue value)
    {
        backing.AddOrUpdate(key, (k) => value, (k,v) => value);
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        if (!backing.TryGetValue(key, out value))
            return false;
        if (value.Expiry < DateTime.UtcNow)
        {
            value = default;
            backing.TryRemove(key, out _);
            return false;
        }
        return true;
    }
    
    public void Remove(TKey key)
    {
        backing.TryRemove(key, out _);
    }

    async void CheckExpiry()
    {
        while (await timer.WaitForNextTickAsync()) {
            foreach (var kv in backing)
            {
                if (kv.Value.Expiry < DateTime.UtcNow)
                    backing.TryRemove(kv);
            }
        }
    }

    public void Dispose()
    {
        timer.Dispose();
    }
}
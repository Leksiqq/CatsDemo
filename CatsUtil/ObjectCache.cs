using Net.Leksi.KeyBox;

namespace CatsUtil;

public class ObjectCache
{
    private readonly Dictionary<Type, Dictionary<object[], object>> _cache = new();

    public bool TryGet<T>(object[] keys, out T? result)
    {
        object? value = null;
        if (_cache.ContainsKey(typeof(T)) && _cache[typeof(T)].TryGetValue(keys, out value))
        {
            result = (T)value;
            return true;
        }
        result = default;
        return false;
    }

    public bool TryGet<T>(IKeyRing keyRing, out T? result)
    {
        return TryGet<T>(keyRing.Values.ToArray(), out result);
    }

    public void Add<T>(object[] keys, T source)
    {
        if (!_cache.ContainsKey(typeof(T)))
        {
            _cache[typeof(T)] = new Dictionary<object[], object>(new KeyEqualityComparer());
        }
        _cache[typeof(T)][keys] = source;
    }

    public void Add<T>(IKeyRing keyRing, T source)
    {
        Add<T>(keyRing.Values.ToArray(), source);
    }

}
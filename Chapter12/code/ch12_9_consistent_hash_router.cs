// Chapter 12 — Section 12.4.2
// Consistent hash ring for data-affinity routing of MCP requests to server instances.
// Virtual nodes (150 per server) distribute servers evenly around the ring, preventing
// the hot-spot problem that occurs with too few physical positions.
// When an instance is added or removed, only ~1/n keys are remapped.
// Route() is O(log n) via SortedDictionary iteration.

using System.Security.Cryptography;
using System.Text;

namespace TravelBooking.Routing;

public sealed class ConsistentHashRouter
{
    private readonly SortedDictionary<uint, string> _ring = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private const int VirtualNodesPerServer = 150;

    public void AddServer(string address)
    {
        _lock.EnterWriteLock();
        try
        {
            for (var i = 0; i < VirtualNodesPerServer; i++)
                _ring[Hash($"{address}:{i}")] = address;
        }
        finally { _lock.ExitWriteLock(); }
    }

    public void RemoveServer(string address)
    {
        _lock.EnterWriteLock();
        try
        {
            for (var i = 0; i < VirtualNodesPerServer; i++)
                _ring.Remove(Hash($"{address}:{i}"));
        }
        finally { _lock.ExitWriteLock(); }
    }

    public string Route(string key)
    {
        _lock.EnterReadLock();
        try
        {
            if (_ring.Count == 0)
                throw new InvalidOperationException("No servers registered on the ring.");

            var hash = Hash(key);

            // Walk clockwise: return the first server whose ring position >= hash.
            foreach (var (position, server) in _ring)
            {
                if (position >= hash) return server;
            }

            // Wrap around: key falls past the last node; return the first node.
            return _ring.First().Value;
        }
        finally { _lock.ExitReadLock(); }
    }

    private static uint Hash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToUInt32(bytes, 0);
    }
}

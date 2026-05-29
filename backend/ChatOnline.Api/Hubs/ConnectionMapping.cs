using System.Collections.Concurrent;

namespace ChatOnline.Api.Hubs;

/// <summary>
/// 维护 userId → 多个 SignalR connectionId 的映射关系
/// 为什么需要它？
///   私聊时用户 A 调用 RequestPrivateChat(userB_Id)，
///   ChatHub 需要找到用户 B 的所有 WebSocket 连接并推送请求。
///   这个类就是做"通过 userId 查 connectionId"这件事。
///
/// 为什么用 ConcurrentDictionary？
///   多个用户可能同时连上/断开，需要线程安全的字典
///   （C++ 类比：std::mutex + std::unordered_map<int, set<string>>）
///
/// 为什么一个用户有多个 connectionId？
///   用户可能在多个浏览器标签页中都连接了 SignalR，每个标签页一个连接
/// </summary>
public class ConnectionMapping
{
    // ConcurrentDictionary = 线程安全的 Dictionary<int, HashSet<string>>
    private readonly ConcurrentDictionary<int, HashSet<string>> _connections = new();

    /// <summary>
    /// 用户连上时调用：把他的 connectionId 加入字典
    /// </summary>
    public void Add(int userId, string connectionId)
    {
        // GetOrAdd：如果有这个 key 就返回已有的 HashSet，没有就 new 一个
        var connections = _connections.GetOrAdd(userId, _ => new HashSet<string>());
        lock (connections)                 // HashSet 本身不是线程安全的，需要 lock
        {
            connections.Add(connectionId);
        }
    }

    /// <summary>
    /// 用户断开时调用：移除他的 connectionId
    /// </summary>
    public void Remove(int userId, string connectionId)
    {
        if (_connections.TryGetValue(userId, out var connections))
        {
            lock (connections)
            {
                connections.Remove(connectionId);
                // 如果该用户没有任何连接了，从字典中删除这个 key
                if (connections.Count == 0)
                    _connections.TryRemove(userId, out _);
            }
        }
    }

    /// <summary>
    /// 获取某个用户的所有 connectionId（用于推送给他的所有设备）
    /// </summary>
    public IEnumerable<string> GetConnections(int userId)
    {
        if (_connections.TryGetValue(userId, out var connections))
        {
            lock (connections)
            {
                return connections.ToList();  // 拷贝一份返回，避免迭代时被修改
            }
        }
        return Enumerable.Empty<string>();
    }

    /// <summary>
    /// 检查用户是否在线（至少有一个连接）
    /// </summary>
    public bool IsOnline(int userId)
    {
        return _connections.ContainsKey(userId);
    }
}

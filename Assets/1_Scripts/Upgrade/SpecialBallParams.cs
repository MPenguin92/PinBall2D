using System.Collections.Generic;

/// <summary>
/// 特殊球（Fire / Ice / Lightning 等）的全局参数容器。
/// 每种 BallType 一份键值字典，由对应升级词条的 paramJson 通过
/// AddOrSet / Add 累加写入；运行时由对应派生球类读取。
/// 特殊球体系正在重新设计，当前仅保留架构，无运行时调用方。
///
/// 约定：以 "Add" 结尾的 key 表示对同名基础 key 做加法增量；
///       例如 explosionRadiusAdd += 0.5 会被 GetMerged("explosionRadius") 累加上去。
/// </summary>
public class SpecialBallParams
{
    private readonly Dictionary<BallType, Dictionary<string, float>> table =
        new Dictionary<BallType, Dictionary<string, float>>();

    public void Reset()
    {
        table.Clear();
    }

    /// <summary>覆盖式写入（首次解锁时使用，提供该球种基础参数）。</summary>
    public void Set(BallType type, string key, float value)
    {
        Ensure(type)[key] = value;
    }

    /// <summary>累加式写入（同 id 升级再次抽到时叠加增量）。</summary>
    public void Add(BallType type, string key, float value)
    {
        var dict = Ensure(type);
        dict.TryGetValue(key, out float current);
        dict[key] = current + value;
    }

    /// <summary>读取 base + base + "Add" 增量；不存在返回 defaultValue。</summary>
    public float Get(BallType type, string key, float defaultValue = 0f)
    {
        if (!table.TryGetValue(type, out var dict)) return defaultValue;

        bool hasBase = dict.TryGetValue(key, out float baseVal);
        dict.TryGetValue(key + "Add", out float add);

        if (!hasBase && add == 0f) return defaultValue;
        return baseVal + add;
    }

    /// <summary>如果已写入过该球种任何 key，则视为已解锁。</summary>
    public bool IsUnlocked(BallType type)
    {
        return table.TryGetValue(type, out var dict) && dict.Count > 0;
    }

    private Dictionary<string, float> Ensure(BallType type)
    {
        if (!table.TryGetValue(type, out var dict))
        {
            dict = new Dictionary<string, float>();
            table[type] = dict;
        }
        return dict;
    }
}

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单个 Ball 某一级（level）的数值：Balls_Level.csv 一行。
/// 目前只有伤害；后续可加弹速、体型等列。
/// </summary>
[System.Serializable]
public class BallLevelData
{
    /// <summary>该等级单发伤害（可为小数，如副弹 0.2）。</summary>
    public float damage;
}

/// <summary>
/// 一种 Ball 的定义：Balls.csv 一行 + 该 id 的逐级数值（Balls_Level.csv）。
/// </summary>
[System.Serializable]
public class BallDefinition
{
    /// <summary>球类型 key（Balls.csv 第一列；发射序列里以此引用）。</summary>
    public string id;

    /// <summary>显示名。</summary>
    public string name;

    /// <summary>出池用 prefab 的 Addressables 短地址（不同球型可共用同一 prefab）。</summary>
    public string prefabAddress;

    /// <summary>外观 Sprite 的 Addressables 短地址（如 Balls/ball_base）。</summary>
    public string spriteAddress;

    /// <summary>拖尾起点颜色（含 alpha）。</summary>
    public Color trailStartColor = new Color(1f, 1f, 1f, 0.55f);

    /// <summary>拖尾终点颜色（含 alpha）。</summary>
    public Color trailEndColor = new Color(1f, 1f, 1f, 0f);

    /// <summary>拖尾起点宽度。</summary>
    public float trailStartWidth = 0.18f;

    /// <summary>拖尾终点宽度。</summary>
    public float trailEndWidth = 0.04f;

    /// <summary>拖尾持续时间（秒）。</summary>
    public float trailTime = 0.12f;

    /// <summary>逐级数值：下标 0 = Lv1；未配置更高等级时取末级。</summary>
    public List<BallLevelData> levels = new List<BallLevelData>();

    /// <summary>把本球拖尾样式应用到 TrailRenderer。</summary>
    public void ApplyTrail(TrailRenderer trail)
    {
        if (trail == null) return;

        trail.time = Mathf.Max(0f, trailTime);
        trail.startWidth = trailStartWidth;
        trail.endWidth = trailEndWidth;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(trailStartColor, 0f),
                new GradientColorKey(trailEndColor, 1f),
            },
            new[]
            {
                new GradientAlphaKey(trailStartColor.a, 0f),
                new GradientAlphaKey(trailEndColor.a, 1f),
            });
        trail.colorGradient = gradient;
    }
}

/// <summary>
/// 弹珠定义表 ScriptableObject：由 DataImporter 从 9_Excel/Balls.csv + Balls_Level.csv 生成。
/// 运行时由 PinBallBase 出池时按 (ballId, 注入等级) 查询该级伤害。
/// </summary>
[CreateAssetMenu(fileName = "BallTable", menuName = "PinBall2D/Data/BallTable", order = 5)]
public class BallTable : ScriptableObject
{
    [SerializeField]
    private List<BallDefinition> balls = new List<BallDefinition>();

    public IReadOnlyList<BallDefinition> Balls => balls;

    public int Count => balls != null ? balls.Count : 0;

    /// <summary>由 Editor 导入工具写入；运行时无需调用。</summary>
    public void SetBalls(List<BallDefinition> list)
    {
        balls = list ?? new List<BallDefinition>();
    }

    /// <summary>按 id 取定义；不存在返回 null。</summary>
    public BallDefinition Get(string id)
    {
        if (balls == null || string.IsNullOrEmpty(id)) return null;
        for (int i = 0; i < balls.Count; i++)
        {
            if (balls[i] != null && balls[i].id == id)
                return balls[i];
        }
        return null;
    }

    /// <summary>
    /// 按 (id, level) 查该级数值；level 超出配表范围时沿用末级；
    /// 命中返回 true，未命中（id 不存在或没有等级数据）返回 false。
    /// </summary>
    public bool TryGetLevel(string id, int level, out BallLevelData data)
    {
        data = null;
        BallDefinition def = Get(id);
        if (def == null || def.levels == null || def.levels.Count == 0) return false;

        int index = Mathf.Clamp(level - 1, 0, def.levels.Count - 1);
        data = def.levels[index];
        return data != null;
    }
}

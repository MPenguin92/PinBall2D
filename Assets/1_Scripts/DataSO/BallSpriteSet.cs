using UnityEngine;

/// <summary>
/// 各 BallType 对应的 UI/弹珠 Sprite 映射表，由 Addressables 加载。
/// </summary>
[CreateAssetMenu(fileName = "BallSpriteSet", menuName = "PinBall2D/Data/BallSpriteSet", order = 4)]
public class BallSpriteSet : ScriptableObject
{
    [SerializeField]
    private Sprite[] sprites = new Sprite[7];

    public Sprite Get(BallType type)
    {
        int index = (int)type;
        if (sprites == null || index < 0 || index >= sprites.Length)
            return null;
        return sprites[index];
    }
}
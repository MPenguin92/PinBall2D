using UnityEngine;
using UnityEngine.UI;

public class PlayerRender : MonoBehaviour
{
    [SerializeField]
    private Image power;

    [SerializeField]
    private Player player;


    private void Update()
    {
        if (power == null || player == null)
        {
            return;
        }

        float max = player.MaxHitPower - player.BaseHitPower;
        power.fillAmount = max > 0f ? Mathf.Clamp01(player.CurrentHitPower / max) : 0f;
    }
}

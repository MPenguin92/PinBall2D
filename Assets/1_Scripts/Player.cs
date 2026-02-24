using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField]
    private float moveSpeed = 8f;

    [SerializeField]
    private float baseHitPower = 0.5f;

    public float BaseHitPower => baseHitPower;

    [SerializeField]
    private float maxHitPower = 2f;

    [SerializeField]
    private float powerChargeSpeed = 1f;

    private float currentHitPower;

    public float Radius => transform.localScale.x * 0.5f;
    public float CurrentHitPower => currentHitPower;
    public float MaxHitPower => maxHitPower;

    private void OnEnable()
    {
        ResetHitPower();
    }

    private void Update()
    {
        HandleMove();
        HandleCharge();
        HandleHit();
    }

    private void HandleMove()
    {
        float horizontalInput = 0f;
        if (Input.GetKey(KeyCode.A))
        {
            horizontalInput -= 1f;
        }

        if (Input.GetKey(KeyCode.D))
        {
            horizontalInput += 1f;
        }

        Vector3 position = transform.position;
        position.x += horizontalInput * moveSpeed * Time.deltaTime;
        position.x = ClampXWithBorders(position.x);
        transform.position = position;
    }

    private float ClampXWithBorders(float targetX)
    {
        if (!TryGetHorizontalLimits(out float minX, out float maxX))
        {
            return targetX;
        }

        return Mathf.Clamp(targetX, minX, maxX);
    }

    private bool TryGetHorizontalLimits(out float minX, out float maxX)
    {
        minX = float.NegativeInfinity;
        maxX = float.PositiveInfinity;

        Border[] borders = GetBorders();
        if (borders == null || borders.Length == 0)
        {
            return false;
        }

        Border leftBorder = null;
        Border rightBorder = null;
        float leftCenterX = float.PositiveInfinity;
        float rightCenterX = float.NegativeInfinity;

        for (int i = 0; i < borders.Length; i++)
        {
            Border border = borders[i];
            if (border == null)
            {
                continue;
            }

            //border.RefreshRect();
            Rect rect = border.BorderRect;
            if (rect.height < rect.width)
            {
                continue;
            }

            float centerX = rect.center.x;
            if (centerX < leftCenterX)
            {
                leftCenterX = centerX;
                leftBorder = border;
            }

            if (centerX > rightCenterX)
            {
                rightCenterX = centerX;
                rightBorder = border;
            }
        }

        if (leftBorder == null || rightBorder == null)
        {
            return false;
        }

        minX = leftBorder.BorderRect.xMax + Radius;
        maxX = rightBorder.BorderRect.xMin - Radius;
        return minX <= maxX;
    }

    private void HandleHit()
    {
        IReadOnlyList<PinBallBase> balls = GetPinBalls();
        for (int i = 0; i < balls.Count; i++)
        {
            PinBallBase ball = balls[i];
            if (ball == null)
            {
                continue;
            }

            float hitDistance = Radius + ball.Radius;
            float distance = Vector2.Distance(transform.position, ball.transform.position);
            if (distance > hitDistance)
            {
                continue;
            }

            Vector2 newSpeed = -ball.Speed * currentHitPower;

            ball.PushBall(newSpeed);
            Debug.Log($"[Player] Hit success. ball={ball.name}, power={currentHitPower:F2}, distance={distance:F3}, hitDistance={hitDistance:F3}");
            break;
        }

        ResetHitPower();
    }

    private void HandleCharge()
    {
        bool isMoving = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D);
        if (isMoving)
        {
            ResetHitPower();
            return;
        }

        currentHitPower = Mathf.Min(currentHitPower + powerChargeSpeed * Time.deltaTime, maxHitPower);
    }

    private Border[] GetBorders()
    {
        GameLogicManager manager = GetGameLogicManager();
        if (manager != null && manager.Borders != null)
        {
            return manager.Borders;
        }

        return Object.FindObjectsByType<Border>(FindObjectsSortMode.None);
    }

    private IReadOnlyList<PinBallBase> GetPinBalls()
    {
        GameLogicManager manager = GetGameLogicManager();
        if (manager != null && manager.PinBalls != null)
        {
            return manager.PinBalls;
        }

        return Object.FindObjectsByType<PinBallBase>(FindObjectsSortMode.None);
    }

    private GameLogicManager GetGameLogicManager()
    {
        if (GameLogicManager.Instance != null)
        {
            return GameLogicManager.Instance;
        }

        return Object.FindFirstObjectByType<GameLogicManager>();
    }

    private void ResetHitPower()
    {
        currentHitPower = Mathf.Clamp(baseHitPower, 0f, maxHitPower);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Vector3 center = transform.position;
        Gizmos.DrawWireSphere(center, transform.localScale.x * 0.5f);
    }
}

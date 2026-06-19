using UnityEngine;

public class WorkerMovementStats : MonoBehaviour
{
    private const float MinMoveSpeed = 0f;
    private const float MinStoppingDistance = 0f;

    [SerializeField] private float _moveSpeed = 2f;
    [SerializeField] private float _stoppingDistance = 0.05f;

    public float MoveSpeed => Mathf.Max(MinMoveSpeed, _moveSpeed);
    public float StoppingDistance => Mathf.Max(MinStoppingDistance, _stoppingDistance);
}

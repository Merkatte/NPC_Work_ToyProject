using UnityEngine;

public class WallPerimeter : MonoBehaviour
{
    [SerializeField] private Transform _workPoint;

    public Vector3 WorkPosition => _workPoint ? _workPoint.position : transform.position;
}

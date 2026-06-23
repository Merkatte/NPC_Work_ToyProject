using UnityEngine;

// 테스트용 적 식별 컴포넌트. 이동·반격 AI 없음.
// 피해·사망은 같은 GameObject에 부착된 Health 컴포넌트가 담당한다.
// 탐지는 Enemy 레이어 + Collider2D로 SeekAction의 Physics 질의에 걸린다.
[RequireComponent(typeof(Health))]
public class Enemy : MonoBehaviour
{
    // 향후 이름표·티어·종류 데이터를 여기에 추가한다.
}

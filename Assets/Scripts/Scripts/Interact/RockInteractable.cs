using UnityEngine;

public class RockInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private RockDataSO data;

    private int hp;

    public float InteractRange => data != null ? data.interactRange : 2f;

    private void Awake()
    {
        if (data == null)
        {
            Debug.LogError($"{name}: RockDataSO가 할당되지 않았습니다.");
            enabled = false;
            return;
        }

        hp = Mathf.Max(1, data.maxHp);
    }

    public void Interact(Transform interactor)
    {
        // 테스트 로그
        Debug.Log($"Hit {data.displayName}: {name} (HP {hp}/{data.maxHp})");
        var shake = GetComponent<Shake>();
        if (shake != null) shake.Play();
        hp--;

        if (hp <= 0)
        {
            Debug.Log($"Break {data.displayName}: {name}");
            Destroy(gameObject);
        }
    }
}

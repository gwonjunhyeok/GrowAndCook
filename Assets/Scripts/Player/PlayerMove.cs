using UnityEngine;

// 역할: 현재 키 바인딩 설정을 기반으로 플레이어 이동을 처리한다.
[RequireComponent(typeof(Rigidbody2D), typeof(PlayerAnimController))]
public class PlayerMove : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private PlayerAnimController animController;

    private Rigidbody2D rb;
    private Vector2 input;
    private GameManager gameManager;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (animController == null)
            animController = GetComponent<PlayerAnimController>();

        gameManager = GameManager.Instance;
    }

    private void Update()
    {
        GetInput();
        if (animController != null)
            animController.UpdateAnimation(input);
    }

    private void FixedUpdate()
    {
        Move();
    }

    private void GetInput()
    {
        if (gameManager == null)
            gameManager = GameManager.Instance;

        if (gameManager != null && gameManager.IsGameplayPaused)
        {
            input = Vector2.zero;
            return;
        }

        KeyBindingSettings keyBindings = gameManager != null
            ? gameManager.CurrentKeyBindings
            : null;

        input = Vector2.zero;

        if (keyBindings == null)
            return;

        if (Input.GetKey(keyBindings.moveLeft))
            input.x -= 1f;

        if (Input.GetKey(keyBindings.moveRight))
            input.x += 1f;

        if (Input.GetKey(keyBindings.moveUp))
            input.y += 1f;

        if (Input.GetKey(keyBindings.moveDown))
            input.y -= 1f;

        if (input.sqrMagnitude > 1f)
            input = input.normalized;
    }

    // 역할: 물리 프레임에서 Rigidbody2D 기반 이동을 적용한다.
    private void Move()
    {
        Vector2 nextPos = rb.position + input * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(nextPos);
    }
}


using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 input;
    private SpriteRenderer sp;
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sp = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        GetInput();
    }

    private void FixedUpdate()
    {
        P_Move();
    }
    private void LateUpdate()
    {
        C_Flip();
    }
    private void GetInput()
    {
        input.x = Input.GetAxisRaw("Horizontal");
        input.y = Input.GetAxisRaw("Vertical");

        if (input.sqrMagnitude > 1f)
            input = input.normalized;
    }

    private void P_Move()
    {
        Vector2 nextPos = rb.position + input * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(nextPos);
    }
    void C_Flip()
    {
        if (input.x != 0)
        {
            sp.flipX = input.x<0;
        }
    }
}

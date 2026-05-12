using UnityEngine;

// 역할: 플레이어 이동 입력에 따라 Animator 파라미터와 좌우 반전을 갱신한다.
[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
public class PlayerAnimController : MonoBehaviour
{
    private const int DirectionFront = 0;
    private const int DirectionBack = 1;
    private const int DirectionSide = 2;

    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private string isMovingParameter = "IsMoving";
    [SerializeField] private string directionParameter = "Direction";

    private int isMovingHash;
    private int directionHash;
    private int lastDirection = DirectionFront;
    private bool lastIsMoving;
    private bool lastFlipX;
    private bool hasAppliedState;

#if UNITY_EDITOR
    private void Reset()
    {
        AutoBind();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
            AutoBind();
    }
#endif

    private void Awake()
    {
        AutoBind();
        CacheParameterHashes();
    }

    // 역할: 이동 입력을 방향/이동 상태로 변환해 Animator와 SpriteRenderer에 반영한다.
    public void UpdateAnimation(Vector2 moveInput)
    {
        bool isMoving = moveInput.sqrMagnitude > 0.0001f;
        int direction = lastDirection;
        bool flipX = direction == DirectionSide && lastFlipX;

        if (isMoving)
        {
            direction = GetDirection(moveInput);
            if (direction == DirectionSide)
                flipX = moveInput.x < 0f;
            else
                flipX = false;
        }

        ApplyFlip(flipX);
        ApplyAnimatorState(isMoving, direction);
    }

    private void AutoBind()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void CacheParameterHashes()
    {
        isMovingHash = Animator.StringToHash(isMovingParameter);
        directionHash = Animator.StringToHash(directionParameter);
    }

    private int GetDirection(Vector2 moveInput)
    {
        float absX = Mathf.Abs(moveInput.x);
        float absY = Mathf.Abs(moveInput.y);

        if (absX >= absY)
            return DirectionSide;

        return moveInput.y > 0f ? DirectionBack : DirectionFront;
    }

    private void ApplyFlip(bool flipX)
    {
        if (spriteRenderer == null || hasAppliedState && lastFlipX == flipX)
            return;

        spriteRenderer.flipX = flipX;
        lastFlipX = flipX;
    }

    private void ApplyAnimatorState(bool isMoving, int direction)
    {
        if (animator == null)
            return;

        if (!hasAppliedState || lastIsMoving != isMoving)
            animator.SetBool(isMovingHash, isMoving);

        if (!hasAppliedState || lastDirection != direction)
            animator.SetInteger(directionHash, direction);

        lastIsMoving = isMoving;
        lastDirection = direction;
        hasAppliedState = true;
    }
}

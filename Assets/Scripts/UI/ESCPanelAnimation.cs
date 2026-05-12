using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 역할: ESC 패널이 열릴 때 판자 묶음을 시작 위치에서 떨어뜨려 최종 위치에 안착시킨다.
public sealed class EscPanelAnimation : MonoBehaviour
{
    [SerializeField] private RectTransform boardMotionRoot;
    [SerializeField] private RectTransform startAnchor;
    [SerializeField] private RectTransform endAnchor;
    [SerializeField] private GraphicRaycaster panelRaycaster;
    [SerializeField] private float overshootDistance = 32f;
    [SerializeField] private float dropDuration = 0.18f;
    [SerializeField] private float settleDuration = 0.12f;

    private Coroutine playRoutine;

    private void Awake()
    {
        ResetToStart();
    }

    private void OnEnable()
    {
        PlayOpenAnimation();
    }

    private void OnDisable()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        SetRaycastEnabled(true);
        ResetToStart();
    }

    // 역할: 패널 열기 연출을 처음부터 다시 재생한다.
    public void PlayOpenAnimation()
    {
        if (!HasRequiredReferences())
            return;

        if (playRoutine != null)
            StopCoroutine(playRoutine);

        ResetToStart();
        playRoutine = StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        SetRaycastEnabled(false);

        Vector2 startPosition = startAnchor.anchoredPosition;
        Vector2 endPosition = endAnchor.anchoredPosition;
        Vector2 overshootPosition = GetOvershootPosition(startPosition, endPosition);

        yield return MoveAnchoredPosition(startPosition, overshootPosition, dropDuration, useBackEase: false);
        yield return MoveAnchoredPosition(overshootPosition, endPosition, settleDuration, useBackEase: true);

        boardMotionRoot.anchoredPosition = endPosition;
        SetRaycastEnabled(true);
        playRoutine = null;
    }

    // 역할: timeScale과 무관하게 UI 위치를 보간한다.
    private IEnumerator MoveAnchoredPosition(
        Vector2 from,
        Vector2 to,
        float duration,
        bool useBackEase)
    {
        if (duration <= 0f)
        {
            boardMotionRoot.anchoredPosition = to;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            if (t > 1f)
                t = 1f;

            float eased = useBackEase ? EaseOutBack(t) : EaseOutCubic(t);
            boardMotionRoot.anchoredPosition = Vector2.LerpUnclamped(from, to, eased);
            yield return null;
        }

        boardMotionRoot.anchoredPosition = to;
    }

    private void ResetToStart()
    {
        if (!HasRequiredReferences())
            return;

        boardMotionRoot.anchoredPosition = startAnchor.anchoredPosition;
    }

    private void SetRaycastEnabled(bool isEnabled)
    {
        if (panelRaycaster != null)
            panelRaycaster.enabled = isEnabled;
    }

    private bool HasRequiredReferences()
    {
        return boardMotionRoot != null
            && startAnchor != null
            && endAnchor != null;
    }

    // 역할: 시작점에서 끝점으로 향하는 방향 기준으로 오버슈트 지점을 계산한다.
    private Vector2 GetOvershootPosition(Vector2 startPosition, Vector2 endPosition)
    {
        if (overshootDistance <= 0f)
            return endPosition;

        Vector2 moveDirection = endPosition - startPosition;
        float moveDistanceSqr = moveDirection.sqrMagnitude;
        if (moveDistanceSqr <= 0.0001f)
            return endPosition;

        moveDirection /= Mathf.Sqrt(moveDistanceSqr);
        return endPosition + (moveDirection * overshootDistance);
    }

    private static float EaseOutCubic(float t)
    {
        float inverse = 1f - t;
        return 1f - (inverse * inverse * inverse);
    }

    private static float EaseOutBack(float t)
    {
        const float overshoot = 1.70158f;
        float shifted = t - 1f;
        return 1f + ((overshoot + 1f) * shifted * shifted * shifted) + (overshoot * shifted * shifted);
    }
}

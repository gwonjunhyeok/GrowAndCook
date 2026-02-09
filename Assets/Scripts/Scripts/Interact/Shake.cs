using System.Collections;
using UnityEngine;

public class Shake : MonoBehaviour
{
    [SerializeField] private float duration = 0.08f;   // Èçµé¸®´Â ÃÑ ½Ã°£
    [SerializeField] private float amplitude = 0.04f;  // ÁÂ¿ì ÀÌµ¿ Æø(¿ùµå À¯´Ö)
    [SerializeField] private int vibrato = 6;          // Èçµé È½¼ö(¿Õº¹ È½¼ö)

    private Coroutine routine;
    private Vector3 baseLocalPos;

    private void Awake()
    {
        baseLocalPos = transform.localPosition;
    }

    private void OnDisable()
    {
        if (routine != null) StopCoroutine(routine);
        transform.localPosition = baseLocalPos;
    }

    public void Play()
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(CoShake());
    }

    private IEnumerator CoShake()
    {
        baseLocalPos = transform.localPosition;

        float stepTime = duration / Mathf.Max(1, vibrato);
        int dir = 1;

        for (int i = 0; i < vibrato; i++)
        {
            transform.localPosition = baseLocalPos + new Vector3(amplitude * dir, 0f, 0f);
            dir *= -1;
            yield return new WaitForSeconds(stepTime);
        }

        transform.localPosition = baseLocalPos;
        routine = null;
    }
}

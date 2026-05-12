using System.Collections.Generic;
using UnityEngine;

// 역할: 월드에서 상태가 바뀌는 오브젝트의 저장용 고유 ID를 보관한다.
public sealed class WorldObjectIdentity : MonoBehaviour
{
    private static readonly List<WorldObjectIdentity> ActiveIdentities = new();

    [SerializeField] private string objectId = string.Empty;

    private bool isRegisteredInList;

    public string ObjectId => objectId;

    public static IReadOnlyList<WorldObjectIdentity> RegisteredIdentities => ActiveIdentities;

    private void OnEnable()
    {
        if (!isRegisteredInList)
        {
            ActiveIdentities.Add(this);
            isRegisteredInList = true;
        }

        if (WorldStateService.Instance != null)
            WorldStateService.Instance.RegisterWorldObject(this);
    }

    private void OnDisable()
    {
        if (isRegisteredInList)
        {
            ActiveIdentities.Remove(this);
            isRegisteredInList = false;
        }

        if (WorldStateService.Instance != null)
            WorldStateService.Instance.UnregisterWorldObject(this);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (objectId == null)
            objectId = string.Empty;
    }
#endif
}

using UnityEngine;

// 역할: 로드 UI가 열릴 때 슬롯 3개의 저장 유무와 요약 정보를 갱신한다.
public sealed class LoadGameUIController : MonoBehaviour
{
    [SerializeField] private StartSceneMenuController menuController;
    [SerializeField] private LoadGameSlotView[] slotViews;

    private void OnEnable()
    {
        RefreshSlots();
    }

    public void RefreshSlots()
    {
        if (slotViews == null)
            return;

        for (int i = 0; i < slotViews.Length; i++)
        {
            LoadGameSlotView slotView = slotViews[i];
            if (slotView == null)
                continue;

            int slotIndex = slotView.SlotIndex;
            if (SaveFileService.TryLoad(slotIndex, out SaveGameData saveData))
            {
                slotView.BindSaved(saveData, menuController);
                continue;
            }

            slotView.BindEmpty(menuController);
        }
    }
}

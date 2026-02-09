using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/ItemData")]
public class ItemData : ScriptableObject//스크립터블오브젝트로 아이템데이터를 에셋으로 직접 추가
{
    public string itemName;       // 아이템 이름
    public int maxStack;          // 최대 개수
    public Sprite icon;           // 아이템 이미지 (UI용)
}
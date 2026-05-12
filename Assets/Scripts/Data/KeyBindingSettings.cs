using System;
using UnityEngine;

// 역할: 게임에서 사용하는 키 바인딩 데이터를 보관한다.
[Serializable]
public sealed class KeyBindingSettings
{
    [Header("Move")]
    public KeyCode moveUp = KeyCode.W;
    public KeyCode moveDown = KeyCode.S;
    public KeyCode moveLeft = KeyCode.A;
    public KeyCode moveRight = KeyCode.D;

    [Header("UI")]
    public KeyCode inventory = KeyCode.E;
    public KeyCode settings = KeyCode.Escape;

    [Header("Mouse")]
    public KeyCode leftClick = KeyCode.Mouse0;
    public KeyCode rightClick = KeyCode.Mouse1;

    public KeyBindingSettings Clone()
    {
        return new KeyBindingSettings
        {
            moveUp = moveUp,
            moveDown = moveDown,
            moveLeft = moveLeft,
            moveRight = moveRight,
            inventory = inventory,
            settings = settings,
            leftClick = leftClick,
            rightClick = rightClick
        };
    }
}

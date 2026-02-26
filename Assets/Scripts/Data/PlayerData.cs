using System;
using UnityEngine;

[Serializable]
public class PlayerData
{
    [SerializeField] private string P_name;
    [SerializeField] private int P_hp;
    [SerializeField] private int P_stamina;
    [SerializeField] private int P_level;
    [SerializeField] private float P_exp;
    [SerializeField] private int P_gold;

    public string Name { get => P_name; set => P_name = value; }
    public int Hp { get => P_hp; set => P_hp = value; }
    public int Stamina { get => P_stamina; set => P_stamina = value; }
    public int Level { get => P_level; set => P_level = value; }
    public float Exp { get => P_exp; set => P_exp = value; }
    public int Gold { get => P_gold; set => P_gold = value; }
}
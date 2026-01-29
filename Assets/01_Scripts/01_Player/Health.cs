using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    private int health // 게임매니저에서 가져도록 변경
    {
        get => GameManager.Instance.PlayerHP;
        set => GameManager.Instance.PlayerHP = value;
    }

    public event Action OnDie;

    public bool IsDie = false;

    //private void Start()
    //{
    //    health = maxHealth;
    //}

    public void TakeDamage(int damage)
    {
        if (health == 0) return;

        int prev = health;
        health = Mathf.Max(health - damage, 0);

        if (health < prev)
        {
            EventBus.Publish(new PlayerDamagedEvent());
        }

        if (health == 0)
        {
            IsDie = true;
            OnDie?.Invoke();
        }

        Debug.Log(health);
    }
}
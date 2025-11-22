using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Configurações de Vida")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Invencibilidade")]
    public float invincibilityDuration = 3.0f;
    private bool isInvincible = false;

    [Header("Efeitos Visuais (Hit Screen)")]
    public Image damageImage; 
    
    [Range(0f, 1f)]
    public float maxAlphaIntensity = 0.4f; // 0.4 = 40% de vermelho (não tampa a visão toda)
    public float fadeSpeed = 1.0f;         // Quanto menor, mais demora para sumir o vermelho

    void Start()
    {
        currentHealth = maxHealth;
        
        // Garante que começa transparente
        if(damageImage != null)
        {
            Color c = damageImage.color;
            c.a = 0f;
            damageImage.color = c;
        }
    }

    void Update()
    {
        // --- LÓGICA DE FADE OUT (SUAVE) ---
        if (damageImage != null)
        {
            // Se a imagem tem alguma opacidade, vamos reduzindo ela gradualmente
            if (damageImage.color.a > 0)
            {
                Color currentColor = damageImage.color;
                
                // Reduz o alpha baseado no tempo e velocidade
                currentColor.a -= Time.deltaTime * fadeSpeed;
                
                // Aplica a nova cor
                damageImage.color = currentColor;
            }
        }
    }

    public void TakeDamage(int amount)
    {
        // 1. Se invencível, sai da função
        if (isInvincible) return;

        // 2. Aplica Dano
        currentHealth -= amount;
        Debug.Log($"Dano recebido! Vida: {currentHealth}");

        // --- EFEITO VISUAL (KICK) ---
        // Em vez de piscar, nós "chutamos" o alpha para o valor máximo instantaneamente.
        // O Update vai cuidar de fazer ele sumir devagar.
        if (damageImage != null)
        {
            Color hitColor = damageImage.color;
            hitColor.a = maxAlphaIntensity; // Define a intensidade do vermelho (ex: 0.4)
            damageImage.color = hitColor;
        }

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // 3. Ativa Invencibilidade
            StartCoroutine(InvincibilityRoutine());
        }
    }

    IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;
        // Opcional: Adicione som de dano aqui
        yield return new WaitForSeconds(invincibilityDuration);
        isInvincible = false;
    }

    void Die()
    {
        Debug.Log("Player Morreu!");

        // Chama o Game Over no controlador de pontuação
        if (ScoreController.Instance != null)
        {
            ScoreController.Instance.TriggerDeath();
        }
        else
        {
            // Fallback caso esqueça de colocar o ScoreController na cena
            Debug.LogError("ScoreController não encontrado para acionar Game Over.");
        }
    }
}
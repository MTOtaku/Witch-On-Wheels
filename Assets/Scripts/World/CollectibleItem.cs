using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class CollectibleItem : MonoBehaviour {
    [Header("Configuração")]
    [SerializeField] private int value = 1;
    [SerializeField] private string playerTag = "Player";

    [Header("Filtro de Colisão")]
    [Tooltip("Se marcado, ignora colisores que estejam configurados como Trigger (ex: sensores/hitboxes secundárias)")]
    [SerializeField] private bool ignoreTriggers = true;

    private BoxCollider2D boxCollider;
    private bool isCollected = false;

    private void Awake(){
        boxCollider = GetComponent<BoxCollider2D>();
        boxCollider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other){
        if (isCollected) return;

        // 1. Ignora caso o colisor que encostou seja outro Trigger (opcional)
        if (ignoreTriggers && other.isTrigger) return;

        // 2. Verifica se o colisor específico é um CapsuleCollider2D
        if (other is CapsuleCollider2D) {
            // 3. Verifica se pertence ao Player
            if (other.CompareTag(playerTag) || other.GetComponentInParent<PlayerCollector>() != null) {
                PlayerCollector player = other.GetComponent<PlayerCollector>() ?? other.GetComponentInParent<PlayerCollector>();

                if (player != null) {
                    isCollected = true;
                    player.AddCollectible(value);
                    Destroy(gameObject);
                }
            }
        }
    }
}
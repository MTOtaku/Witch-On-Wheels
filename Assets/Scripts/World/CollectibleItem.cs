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
        // Evita coletar mais de uma vez no mesmo frame
        if (isCollected) return;

        // Ignora outros colisores Trigger, caso configurado
        if (ignoreTriggers && other.isTrigger) return;

        // Verifica se quem entrou no trigger é o Player (pela Tag ou pelo Script)
        if (other.CompareTag(playerTag) || other.GetComponentInParent<PlayerCollector>() != null) {
            PlayerCollector player = other.GetComponent<PlayerCollector>();

            if (player == null) {
                player = other.GetComponentInParent<PlayerCollector>();
            }

            if (player != null) {
                isCollected = true;
                player.AddCollectible(value);
                Destroy(gameObject);
            }
        }
    }
}
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Área de gatilho (Trigger) que detecta a queda do jogador e o teleporta para um ponto seguro.
/// Suporta tanto colisores 2D (BoxCollider2D) quanto 3D (BoxCollider).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class FallRespawnZone : MonoBehaviour
{
    [Header("Configuração de Teleporte")]
    [Tooltip("Ponto para onde o jogador será teleportado.")]
    [SerializeField] private Transform targetSpawnPoint;

    [Tooltip("Tag usada para identificar o jogador. Se vazio, buscará o componente PlayerController.")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("Zera a velocidade física do jogador ao teleportar para evitar que continue caindo.")]
    [SerializeField] private bool resetVelocity = true;

    [Header("Eventos (Opcional)")]
    [Tooltip("Evento disparado no momento em que o jogador é teleportado.")]
    public UnityEvent onTeleport;

    private void Awake()
    {
        // Garante que o colisor 2D esteja marcado como Trigger
        if (TryGetComponent<Collider2D>(out var col2D))
        {
            if (!col2D.isTrigger)
            {
                col2D.isTrigger = true;
                Debug.LogWarning($"[FallRespawnZone] '{name}': O Collider2D foi configurado automaticamente como 'Is Trigger'.", this);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryTeleport(other.gameObject);
    }

    // Suporte caso o projeto utilize ou venha a utilizar colisores 3D
    private void OnTriggerEnter(Collider other)
    {
        TryTeleport(other.gameObject);
    }

    private void TryTeleport(GameObject incomingObject)
    {
        if (targetSpawnPoint == null)
        {
            Debug.LogWarning($"[FallRespawnZone] '{name}': Nenhum 'Target Spawn Point' foi configurado!", this);
            return;
        }

        // Verifica se é o jogador (por Tag ou pelo componente PlayerController, verificando pais e filhos)
        bool isPlayer = (!string.IsNullOrEmpty(playerTag) && (incomingObject.CompareTag(playerTag) || incomingObject.transform.root.CompareTag(playerTag)))
                     || incomingObject.GetComponentInParent<PlayerController>() != null;

        if (!isPlayer) return;

        // Obtém o objeto raiz/principal do jogador
        Transform playerRoot = incomingObject.transform.root;
        PlayerController playerController = incomingObject.GetComponentInParent<PlayerController>();
        if (playerController != null)
        {
            playerRoot = playerController.transform;
        }

        // 1. Tratamento para física 2D (Rigidbody2D)
        Rigidbody2D rb2D = playerRoot.GetComponent<Rigidbody2D>();
        if (rb2D != null)
        {
            rb2D.position = targetSpawnPoint.position;
            if (resetVelocity)
            {
#if UNITY_6000_0_OR_NEWER
                rb2D.linearVelocity = Vector2.zero;
#else
                rb2D.velocity = Vector2.zero;
#endif
                rb2D.angularVelocity = 0f;
            }
        }

        // 2. Tratamento para física 3D (Rigidbody) caso exista
        Rigidbody rb3D = playerRoot.GetComponent<Rigidbody>();
        if (rb3D != null)
        {
            rb3D.position = targetSpawnPoint.position;
            if (resetVelocity)
            {
#if UNITY_6000_0_OR_NEWER
                rb3D.linearVelocity = Vector3.zero;
#else
                rb3D.velocity = Vector3.zero;
#endif
                rb3D.angularVelocity = Vector3.zero;
            }
        }

        // 3. Tratamento para CharacterController se houver
        CharacterController charController = playerRoot.GetComponent<CharacterController>();
        if (charController != null)
        {
            charController.enabled = false;
            playerRoot.position = targetSpawnPoint.position;
            charController.enabled = true;
        }
        else
        {
            playerRoot.position = targetSpawnPoint.position;
        }

        onTeleport?.Invoke();
    }

    private void OnDrawGizmos()
    {
        // Visualização no Scene View do Unity
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.4f);

        if (TryGetComponent<BoxCollider2D>(out var box2D))
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box2D.offset, box2D.size);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(box2D.offset, box2D.size);
            Gizmos.matrix = Matrix4x4.identity;
        }

        if (targetSpawnPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, targetSpawnPoint.position);
            Gizmos.DrawWireSphere(targetSpawnPoint.position, 0.5f);
        }
    }
}

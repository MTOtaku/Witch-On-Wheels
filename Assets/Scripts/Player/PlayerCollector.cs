using UnityEngine;
using UnityEngine.Events;
    
public class PlayerCollector : MonoBehaviour {
    [Header("Coletáveis")]
    [SerializeField] private int collectedAmount = 0;
    
    // Propriedade pública para consultar a quantidade atual
    public int CollectedAmount => collectedAmount;
    
    // Evento opcional para disparar atualizações (ex: UI de pontuação)
    public UnityEvent<int> onCollectChanged;
    
    public void AddCollectible(int amount = 1){
        collectedAmount += amount;
    
        onCollectChanged?.Invoke(collectedAmount);
    }
}
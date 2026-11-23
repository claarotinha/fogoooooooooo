using UnityEngine;

/// <summary>Oscilação sutil da intensidade para simular a luz real da fogueira.</summary>
[RequireComponent(typeof(Light))]
public sealed class FogueiraLuz : MonoBehaviour
{
    [Range(0.1f, 15f)] public float intensidadeBase = 5.2f;
    [Range(0f, 1f)] public float oscilacao = 0.27f;
    private Light fonte;
    private float semente;
    private void Awake() { fonte = GetComponent<Light>(); semente = Random.Range(1f, 400f); }
    private void Update()
    {
        float t = Time.time;
        float suave = Mathf.PerlinNoise(semente, t * 5.3f);
        float rapido = Mathf.PerlinNoise(semente + 19f, t * 16.7f);
        fonte.intensity = intensidadeBase * (1f - oscilacao + oscilacao * (suave * 1.25f + rapido * 0.75f));
    }
}

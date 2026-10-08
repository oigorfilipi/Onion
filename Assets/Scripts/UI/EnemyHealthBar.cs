using UnityEngine;

/// <summary>
/// Cria e atualiza a barra de vida acima de cada inimigo.
/// </summary>
[DisallowMultipleComponent]
public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private Vector2 offsetAboveSprite = new Vector2(0f, 0.18f);
    [SerializeField, Min(0.1f)] private float barWidth = 0.8f;
    [SerializeField, Min(0.02f)] private float barHeight = 0.12f;
    [SerializeField] private Color backgroundColor = new Color(0.12f, 0.12f, 0.12f);
    [SerializeField] private Color fillColor = new Color(0.85f, 0.15f, 0.15f);
    [SerializeField] private Sprite backgroundSprite;
    [SerializeField] private Sprite fillSprite;

    private static Sprite defaultSprite;
    private EnemyHealth enemyHealth;
    private SpriteRenderer fillRenderer;
    private Transform fillTransform;

    private void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
    }

    private void OnEnable()
    {
        if (enemyHealth != null)
        {
            enemyHealth.HealthChanged += Refresh;
        }
    }

    private void Start()
    {
        CreateBar();
        Refresh();
    }

    private void OnDisable()
    {
        if (enemyHealth != null)
        {
            enemyHealth.HealthChanged -= Refresh;
        }
    }

    private void CreateBar()
    {
        if (fillRenderer != null)
        {
            return;
        }

        SpriteRenderer enemyRenderer = GetComponent<SpriteRenderer>();
        float spriteTop = enemyRenderer != null && enemyRenderer.sprite != null
            ? enemyRenderer.sprite.bounds.max.y
            : 0.5f;
        int sortingLayer = enemyRenderer != null ? enemyRenderer.sortingLayerID : 0;
        int sortingOrder = enemyRenderer != null ? enemyRenderer.sortingOrder : 0;

        GameObject barRoot = new GameObject("Health Bar");
        barRoot.transform.SetParent(transform, false);
        barRoot.transform.localPosition = new Vector3(
            offsetAboveSprite.x,
            spriteTop + offsetAboveSprite.y,
            0f);

        GameObject background = new GameObject("Background");
        background.transform.SetParent(barRoot.transform, false);
        SpriteRenderer backgroundRenderer = background.AddComponent<SpriteRenderer>();
        backgroundRenderer.sprite = backgroundSprite != null ? backgroundSprite : GetDefaultSprite();
        backgroundRenderer.color = backgroundColor;
        backgroundRenderer.sortingLayerID = sortingLayer;
        backgroundRenderer.sortingOrder = sortingOrder + 10;
        Vector3 backgroundSize = backgroundRenderer.sprite.bounds.size;
        background.transform.localScale = new Vector3(
            barWidth / backgroundSize.x,
            barHeight / backgroundSize.y,
            1f);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(barRoot.transform, false);
        fillTransform = fill.transform;
        fillRenderer = fill.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = fillSprite != null ? fillSprite : GetDefaultSprite();
        fillRenderer.color = fillColor;
        fillRenderer.sortingLayerID = sortingLayer;
        fillRenderer.sortingOrder = sortingOrder + 11;
    }

    private void Refresh()
    {
        if (enemyHealth == null || fillRenderer == null)
        {
            return;
        }

        float fraction = enemyHealth.MaxHealth > 0
            ? Mathf.Clamp01((float)enemyHealth.CurrentHealth / enemyHealth.MaxHealth)
            : 0f;
        float fullWidth = barWidth * 0.94f;
        float visibleWidth = fullWidth * fraction;
        fillRenderer.enabled = fraction > 0f;
        Vector3 fillSize = fillRenderer.sprite.bounds.size;
        fillTransform.localScale = new Vector3(
            visibleWidth / fillSize.x,
            barHeight * 0.65f / fillSize.y,
            1f);
        fillTransform.localPosition = new Vector3(
            -fullWidth * 0.5f + visibleWidth * 0.5f,
            0f,
            -0.01f);
    }

    private static Sprite GetDefaultSprite()
    {
        if (defaultSprite == null)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            defaultSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
        }

        return defaultSprite;
    }
}

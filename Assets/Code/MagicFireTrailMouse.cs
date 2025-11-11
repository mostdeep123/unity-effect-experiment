using UnityEngine;
using UnityEngine.Rendering;

public class MagicFireTrailMouse : MonoBehaviour
{
    [Header("Trail Settings")]
    public float followSpeed = 15f;
    public float stopFadeDelay = 0.4f;
    public float distanceEmissionRate = 10f;
    public float zOffset = 0.1f;

    private ParticleSystem coreLayer;
    private ParticleSystem sparksLayer;
    private ParticleSystem embersLayer;
    private Vector3 targetPos;
    private Vector3 lastMousePos;
    private float stillTimer;

    void Start()
    {
        Cursor.visible = true;
        targetPos = transform.position;

        // Core – putih panas → kuning → oranye
        coreLayer = CreateParticleLayer(
            "CoreTrail",
            new Color(1f, 0.95f, 0.8f, 1f),
            new Color(1f, 0.3f, 0f, 0f),
            0.09f, 1.3f, 3.2f, 15f
        );

        // Sparks – kuning → oranye, cepat dan acak
        sparksLayer = CreateParticleLayer(
            "Sparks",
            new Color(1f, 0.9f, 0.4f, 1f),
            new Color(1f, 0.2f, 0f, 0f),
            0.03f, 0.4f, 2.5f, 25f
        );
        var sparkShape = sparksLayer.shape;
        sparkShape.shapeType = ParticleSystemShapeType.Sphere;
        sparkShape.radius = 0.05f;

        var sparkMain = sparksLayer.main;
        sparkMain.gravityModifier = 0.3f;
        var sparkNoise = sparksLayer.noise;
        sparkNoise.enabled = true;
        sparkNoise.strength = 0.5f;

        // Embers – oranye redup, bergerak lambat & berumur panjang
        embersLayer = CreateParticleLayer(
            "Embers",
            new Color(1f, 0.7f, 0.3f, 0.8f),
            new Color(0.25f, 0.05f, 0f, 0f),
            0.1f, 2.2f, 1.2f, 6f
        );
        var embersNoise = embersLayer.noise;
        embersNoise.enabled = true;
        embersNoise.strength = 0.25f;

        // Aktifkan semua
        coreLayer.Play();
        sparksLayer.Play();
        embersLayer.Play();
    }

    void Update()
    {
        if (Camera.main == null) return;

        Vector3 mouse = Input.mousePosition;
        if (Camera.main.orthographic)
            mouse.z = Camera.main.nearClipPlane + zOffset;
        else
            mouse.z = 10f;

        targetPos = Camera.main.ScreenToWorldPoint(mouse);
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);

        // Apakah mouse diam?
        if ((Input.mousePosition - lastMousePos).sqrMagnitude < 1f)
            stillTimer += Time.deltaTime;
        else
            stillTimer = 0f;

        float alpha = Mathf.Clamp01(1f - (stillTimer / stopFadeDelay));

        // Dinamis emission saat bergerak / berhenti
        SetEmission(coreLayer, distanceEmissionRate * alpha);
        SetEmission(sparksLayer, distanceEmissionRate * 2.5f * alpha);
        SetEmission(embersLayer, 5f * alpha);

        lastMousePos = Input.mousePosition;
    }

    private ParticleSystem CreateParticleLayer(string name, Color startColor, Color endColor, float size, float life, float speed, float emissionRate)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform);

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        var emission = ps.emission;
        var shape = ps.shape;
        var colorOverLifetime = ps.colorOverLifetime;
        var sizeOverLifetime = ps.sizeOverLifetime;
        var renderer = ps.GetComponent<ParticleSystemRenderer>();

        // === MAIN ===
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = life;
        main.startSpeed = speed;
        main.startSize = size;
        main.startColor = startColor;
        main.gravityModifier = 0f;
        main.scalingMode = ParticleSystemScalingMode.Local;
        main.maxParticles = 800;

        // === EMISSION ===
        emission.rateOverTime = emissionRate;
        emission.rateOverDistance = emissionRate * 0.5f;

        // === SHAPE ===
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 10f;
        shape.radius = 0.03f;

        // === COLOR OVER LIFETIME ===
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.mode = GradientMode.Blend;
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(1f, 0.9f, 0.4f), 0.25f),
                new GradientColorKey(new Color(1f, 0.6f, 0.1f), 0.6f),
                new GradientColorKey(new Color(1f, 0.2f, 0f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.8f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        // === SIZE OVER LIFETIME ===
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 1f);
        curve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

        // === RENDERER ===
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 3;
        renderer.alignment = ParticleSystemRenderSpace.View;

        // === MATERIAL (URP ADDITIVE) ===
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.SetInt("_Surface", 1);
        mat.SetInt("_Blend", 1);
        mat.SetInt("_ZWrite", 0);
        mat.SetInt("_SrcBlend", (int)BlendMode.One);
        mat.SetInt("_DstBlend", (int)BlendMode.One);
        mat.renderQueue = (int)RenderQueue.Transparent;
        mat.EnableKeyword("_BLENDMODE_ADDITIVE");

        // base tone kuning-oranye supaya gradasi dominan
        mat.SetColor("_BaseColor", new Color(1f, 0.7f, 0.3f, 1f));

        // tekstur soft glow
        Texture2D tex = MakeSoftParticleTexture(128);
        tex.filterMode = FilterMode.Trilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        mat.mainTexture = tex;

        renderer.material = mat;

        ps.Play();
        return ps;
    }

    private void SetEmission(ParticleSystem ps, float rate)
    {
        if (ps == null) return;
        var em = ps.emission;
        em.rateOverTime = rate;
    }

    private Texture2D MakeSoftParticleTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(center, new Vector2(x, y)) / radius;
                float alpha = Mathf.Exp(-dist * dist * 4.5f);
                float brightness = Mathf.Pow(alpha, 0.7f);
                pixels[y * size + x] = new Color(brightness, brightness, brightness, alpha);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        tex.anisoLevel = 4;
        return tex;
    }
}

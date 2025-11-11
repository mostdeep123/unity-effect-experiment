using UnityEngine;

public class FireTrailMouseMerged : MonoBehaviour
{
    [Header("Trail Settings")]
    public float followSpeed = 15f;
    public float stopFadeDelay = 0.4f;
    public float distanceEmissionRate = 8f;
    public float zOffset = 0.1f;

    private ParticleSystem trailMain;
    private ParticleSystem trailSparks;
    private ParticleSystem trailEmbers;

    private Vector3 targetPos;
    private Vector3 lastMousePos;
    private float stillTimer;

    void Start()
    {
        Cursor.visible = true;
        targetPos = transform.position;

        // === CORE KOMET (utama) ===
        trailMain = CreateParticleLayer(
            "MainTrail",
            new Color(1f, 0.9f, 0.6f),   // putih kekuningan di tengah
            new Color(1f, 0.4f, 0.05f),  // oranye di ujung
            0.25f, 1.6f, 2.5f, 10f,
            true // isCore = true (stretch)
        );

        // === PERCiKAN API ===
        trailSparks = CreateParticleLayer(
            "Sparks",
            new Color(1f, 0.8f, 0.2f),
            new Color(1f, 0.3f, 0f),
            0.06f, 0.5f, 3f, 20f,
            false
        );

        // === BARA API ===
        trailEmbers = CreateParticleLayer(
            "Embers",
            new Color(1f, 0.5f, 0.1f),
            new Color(0.2f, 0.05f, 0f),
            0.12f, 2.2f, 1f, 6f,
            false
        );

        // Sedikit variasi tambahan pada sparks dan embers
        var shapeSpark = trailSparks.shape;
        shapeSpark.shapeType = ParticleSystemShapeType.Cone;
        shapeSpark.angle = 25f;
        shapeSpark.radius = 0.05f;

        var embersEmission = trailEmbers.emission;
        embersEmission.rateOverTime = 5f;

        var embersNoise = trailEmbers.noise;
        embersNoise.enabled = true;
        embersNoise.strength = 0.3f;

        // Aktifkan semua layer
        trailMain.Play();
        trailSparks.Play();
        trailEmbers.Play();
    }

    void Update()
    {
        if (Camera.main == null) return;

        // Konversi posisi mouse ke world
        Vector3 mouse = Input.mousePosition;
        if (Camera.main.orthographic)
            mouse.z = Camera.main.nearClipPlane + zOffset;
        else
            mouse.z = 10f;

        targetPos = Camera.main.ScreenToWorldPoint(mouse);
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);

        // Cek apakah mouse diam
        if ((Input.mousePosition - lastMousePos).sqrMagnitude < 1f)
            stillTimer += Time.deltaTime;
        else
            stillTimer = 0f;

        // Kurangi emisi kalau mouse diam
        float alpha = Mathf.Clamp01(1f - (stillTimer / stopFadeDelay));
        SetEmission(trailMain, distanceEmissionRate * alpha);
        SetEmission(trailSparks, distanceEmissionRate * 2f * alpha);
        SetEmission(trailEmbers, 4f * alpha);

        lastMousePos = Input.mousePosition;
    }

    // === Membuat sistem partikel baru ===
    private ParticleSystem CreateParticleLayer(string name, Color startColor, Color endColor, float size, float life, float speed, float emissionRate, bool isCore)
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

        // === EMISSION ===
        emission.rateOverTime = emissionRate;
        emission.rateOverDistance = emissionRate * 0.5f;

        // === SHAPE ===
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = isCore ? 5f : 15f;
        shape.radius = isCore ? 0.02f : 0.05f;

        // === COLOR OVER LIFETIME ===
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(startColor, 0f),
                new GradientColorKey(endColor, 0.5f),
                new GradientColorKey(new Color(endColor.r, endColor.g, endColor.b, 0f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
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
        if (isCore)
        {
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 1.8f;
        }
        else
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }

        renderer.sortingOrder = isCore ? 3 : 2;
        renderer.alignment = ParticleSystemRenderSpace.View;

        // === MATERIAL (URP Additive Glow) ===
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.SetColor("_BaseColor", Color.white);
        mat.SetInt("_Surface", 1);
        mat.SetInt("_Blend", 1);
        mat.SetInt("_ZWrite", 0);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        mat.EnableKeyword("_BLENDMODE_ADDITIVE");

        // === TEXTURE SOFT GLOW ===
        Texture2D softTex = MakeSoftParticleTexture(128);
        softTex.filterMode = FilterMode.Trilinear;
        softTex.wrapMode = TextureWrapMode.Clamp;
        mat.mainTexture = softTex;

        renderer.material = mat;

        ps.Play();
        return ps;
    }

    // === Ubah emisi partikel ===
    private void SetEmission(ParticleSystem ps, float rate)
    {
        if (ps == null) return;
        var em = ps.emission;
        em.rateOverTime = rate;
    }

    // === Membuat soft radial glow ===
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
                float alpha = Mathf.Exp(-dist * dist * 5f);
                float brightness = Mathf.Pow(alpha, 0.6f);
                pixels[y * size + x] = new Color(brightness, brightness * 0.9f, brightness * 0.5f, alpha);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Trilinear;
        tex.anisoLevel = 4;
        tex.wrapMode = TextureWrapMode.Clamp;
        return tex;
    }
}

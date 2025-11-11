using UnityEngine;

public class FireTrailMouseMerged : MonoBehaviour
{
    [Header("Trail Settings")]
    public float followSpeed = 18f;
    public float stopFadeDelay = 0.5f;
    public float distanceEmissionRate = 10f;
    public float zOffset = 0.1f;

    private ParticleSystem coreTrail;
    private ParticleSystem fireSparks;
    private ParticleSystem fireEmbers;

    private Vector3 lastMousePos;
    private float stillTimer;

    void Start()
    {
        Cursor.visible = true;

        // === Inti komet (core flame) ===
        coreTrail = CreateParticleLayer(
            "CoreTrail",
            new Color(1f, 0.95f, 0.7f),  // putih kekuningan
            new Color(1f, 0.4f, 0f),     // oranye ke merah
            0.15f, 1.5f, 3.5f, 15f,
            ParticleSystemRenderMode.Stretch,
            2.2f
        );

        // === Percikan api (sparks) ===
        fireSparks = CreateParticleLayer(
            "Sparks",
            new Color(1f, 0.8f, 0.2f),
            new Color(1f, 0.2f, 0f),
            0.05f, 0.5f, 2.5f, 25f,
            ParticleSystemRenderMode.Billboard,
            1f
        );

        // === Bara api (embers) ===
        fireEmbers = CreateParticleLayer(
            "Embers",
            new Color(1f, 0.4f, 0f),
            new Color(0.3f, 0.05f, 0f),
            0.1f, 2.5f, 0.8f, 6f,
            ParticleSystemRenderMode.Billboard,
            1f
        );

        // Tambah sedikit variasi bentuk sparks
        var shapeSparks = fireSparks.shape;
        shapeSparks.shapeType = ParticleSystemShapeType.Cone;
        shapeSparks.angle = 25f;
        shapeSparks.radius = 0.05f;

        // Bara bergetar lembut
        var noise = fireEmbers.noise;
        noise.enabled = true;
        noise.strength = 0.3f;
    }

    void Update()
    {
        if (Camera.main == null) return;

        // === Konversi posisi mouse ke world ===
        Vector3 mouse = Input.mousePosition;
        mouse.z = 10f;
        Vector3 world = Camera.main.ScreenToWorldPoint(mouse);
        transform.position = Vector3.Lerp(transform.position, world, Time.deltaTime * followSpeed);

        // === Deteksi diam ===
        if ((world - lastMousePos).sqrMagnitude < 0.0005f)
            stillTimer += Time.deltaTime;
        else
            stillTimer = 0f;

        float fade = Mathf.Clamp01(1f - stillTimer / stopFadeDelay);

        SetEmission(coreTrail, distanceEmissionRate * fade);
        SetEmission(fireSparks, distanceEmissionRate * 1.8f * fade);
        SetEmission(fireEmbers, distanceEmissionRate * 0.6f * fade);

        lastMousePos = world;
    }

    private ParticleSystem CreateParticleLayer(string name, Color startColor, Color endColor, float size, float life, float speed, float emissionRate, ParticleSystemRenderMode mode, float stretchLen)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;

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
        emission.rateOverDistance = emissionRate * 0.4f;

        // === SHAPE ===
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 10f;
        shape.radius = 0.02f;

        // === COLOR GRADIENT (gradasi kuning → oranye → merah) ===
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(startColor, 0f),
                new GradientColorKey(new Color(1f, 0.6f, 0.1f), 0.4f),
                new GradientColorKey(endColor, 1f)
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
        renderer.renderMode = mode;
        if (mode == ParticleSystemRenderMode.Stretch)
        {
            renderer.lengthScale = stretchLen;
            renderer.velocityScale = 0.3f;
        }

        renderer.sortingOrder = 3;
        renderer.alignment = ParticleSystemRenderSpace.View;

        // === MATERIAL ADDITIVE URP ===
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.SetInt("_Surface", 1);
        mat.SetInt("_Blend", 1);
        mat.SetInt("_ZWrite", 0);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        mat.EnableKeyword("_BLENDMODE_ADDITIVE");
        mat.SetColor("_BaseColor", Color.white);

        mat.mainTexture = MakeSoftParticleTexture(128);
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
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - size / 2f) / (size / 2f);
                float dy = (y - size / 2f) / (size / 2f);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(1f - dist * dist * 2.2f);
                tex.SetPixel(x, y, new Color(1f, 0.9f, 0.6f, alpha));
            }
        }
        tex.Apply();
        return tex;
    }
}

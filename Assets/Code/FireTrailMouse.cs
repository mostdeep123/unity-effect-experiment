using UnityEngine;

public class FireTrailMouse : MonoBehaviour
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

        // Buat 3 layer efek
        trailMain = CreateParticleLayer("MainTrail", new Color(1f, 0.6f, 0.2f), new Color(1f, 0.2f, 0f), 0.08f, 1.5f, 3f, 12f);
        trailSparks = CreateParticleLayer("Sparks", new Color(1f, 0.9f, 0.4f), new Color(1f, 0.5f, 0f), 0.03f, 0.5f, 2f, 20f);
        trailEmbers = CreateParticleLayer("Embers", new Color(1f, 0.7f, 0.3f), new Color(0.2f, 0.05f, 0f), 0.1f, 2f, 1f, 5f);

        var shapeSpark = trailSparks.shape;
        shapeSpark.shapeType = ParticleSystemShapeType.Cone;
        shapeSpark.angle = 25f;
        shapeSpark.radius = 0.05f;

        var embersEmission = trailEmbers.emission;
        embersEmission.rateOverTime = 5f;
        var embersNoise = trailEmbers.noise;
        embersNoise.enabled = true;
        embersNoise.strength = 0.3f;

        // Pastikan langsung aktif
        trailMain.Play();
        trailSparks.Play();
        trailEmbers.Play();
    }

    void Update()
    {
        if (Camera.main == null) return;

        // Sesuaikan konversi posisi mouse → world, tergantung tipe kamera
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

    // Membuat sistem partikel baru
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

        // MAIN
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = life;
        main.startSpeed = speed;
        main.startSize = size;
        main.startColor = startColor;
        main.gravityModifier = 0f;

        // EMISSION
        emission.rateOverTime = emissionRate;
        emission.rateOverDistance = emissionRate * 0.5f;

        // SHAPE
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 10f;
        shape.radius = 0.02f;

        // COLOR LIFETIME
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

        // SIZE LIFETIME
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 1f);
        curve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

        // RENDERER
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        renderer.trailMaterial = renderer.material;
        renderer.material.SetFloat("_Surface", 1);
        renderer.material.SetFloat("_Blend", 1);
        renderer.material.EnableKeyword("_ADDITIVE");
        renderer.material.SetColor("_BaseColor", Color.white);

        ps.Play();
        return ps;
    }

    private void SetEmission(ParticleSystem ps, float rate)
    {
        if (ps == null) return;
        var em = ps.emission;
        em.rateOverTime = rate;
    }

    //make soft particle textures
    private Texture2D MakeSoftParticleTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float maxDist = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(center, new Vector2(x, y)) / maxDist;
                float alpha = Mathf.Clamp01(1f - dist * dist * dist * 2f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }

}

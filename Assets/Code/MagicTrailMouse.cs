using UnityEngine;
using UnityEngine.Rendering;

public class MagicTrailMouse : MonoBehaviour
{
    private ParticleSystem coreLayer;
    private ParticleSystem trailLayer;
    private Vector3 lastMousePos;

    void Start()
    {
        CreateTrailEffect();
    }

    void Update()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10f;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);

        if (Vector3.Distance(lastMousePos, worldPos) > 0.02f)
        {
            transform.position = worldPos;

            if (!coreLayer.isEmitting)
            {
                coreLayer.Play();
                trailLayer.Play();
            }
        }
        else
        {
            if (coreLayer.isEmitting)
            {
                coreLayer.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                trailLayer.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        lastMousePos = worldPos;
    }

    private void CreateTrailEffect()
    {
        // ===== CORE (putih ke kuning panas) =====
        coreLayer = CreateParticleLayer(
            "Core",
            new Color(1f, 0.95f, 0.8f, 1f),  // putih kekuningan
            new Color(1f, 0.8f, 0.3f, 0f),
            0.09f, 0.22f, 1.2f, 90f,
            true
        );

        // ===== TRAIL (gradasi api oranye ke merah) =====
        trailLayer = CreateParticleLayer(
            "Trail",
            new Color(1f, 0.85f, 0.2f, 0.9f),   // kuning-oranye
            new Color(1f, 0.1f, 0f, 0f),        // merah gelap di ujung
            0.22f, 0.45f, 0.9f, 50f,
            false
        );

        // ===== SPARKS =====
        var spark = CreateParticleLayer(
            "Sparks",
            new Color(1f, 0.9f, 0.4f, 1f),
            new Color(1f, 0.2f, 0f, 0f),
            0.05f, 0.35f, 3f, 100f,
            false
        );

        var sshape = spark.shape;
        sshape.shapeType = ParticleSystemShapeType.Sphere;
        sshape.radius = 0.08f;

        var smain = spark.main;
        smain.gravityModifier = 0.4f;
        smain.startSpeed = 3.2f;

        var slimit = spark.limitVelocityOverLifetime;
        slimit.enabled = true;
        slimit.dampen = 0.25f;
    }

    private ParticleSystem CreateParticleLayer(
        string name,
        Color startColor,
        Color endColor,
        float size,
        float life,
        float speed,
        float emissionRate,
        bool intenseCore = false)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        var emission = ps.emission;
        var shape = ps.shape;
        var col = ps.colorOverLifetime;
        var sizeOver = ps.sizeOverLifetime;
        var renderer = ps.GetComponent<ParticleSystemRenderer>();

        // === MAIN ===
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = life;
        main.startSpeed = speed;
        main.startSize = size;
        main.startColor = startColor;
        main.scalingMode = ParticleSystemScalingMode.Local;
        main.gravityModifier = 0;
        main.maxParticles = 600;

        // === EMISSION ===
        emission.rateOverTime = emissionRate;
        emission.rateOverDistance = emissionRate * 0.25f;

        // === SHAPE ===
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 10f;
        shape.radius = 0.015f;

        // === COLOR OVER LIFETIME ===
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.mode = GradientMode.Blend;
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(1f, 0.9f, 0.4f), 0.25f),
                new GradientColorKey(new Color(1f, 0.6f, 0.1f), 0.6f),
                new GradientColorKey(new Color(1f, 0.2f, 0f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.8f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        col.color = grad;

        // === SIZE OVER LIFETIME ===
        sizeOver.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 1f);
        curve.AddKey(1f, 0f);
        sizeOver.size = new ParticleSystem.MinMaxCurve(1f, curve);

        // === RENDERER ===
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 3;
        renderer.alignment = ParticleSystemRenderSpace.View;

        // === MATERIAL ===
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.SetInt("_Surface", 1);
        mat.SetInt("_Blend", 1);
        mat.SetInt("_ZWrite", 0);
        mat.SetInt("_SrcBlend", (int)BlendMode.One);
        mat.SetInt("_DstBlend", (int)BlendMode.One);
        mat.renderQueue = (int)RenderQueue.Transparent;
        mat.EnableKeyword("_BLENDMODE_ADDITIVE");

        if (intenseCore)
            mat.SetColor("_BaseColor", new Color(1f, 0.8f, 0.4f, 1f)); // kuning terang
        else
            mat.SetColor("_BaseColor", new Color(1f, 0.6f, 0.2f, 1f)); // oranye lembut

        mat.mainTexture = MakeSoftParticleTexture(128);
        renderer.material = mat;

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return ps;
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
                float alpha = Mathf.Exp(-dist * dist * 4f);
                float brightness = Mathf.Pow(alpha, 0.7f);
                tex.SetPixel(x, y, new Color(brightness, brightness, brightness, alpha));
            }
        }
        tex.Apply();
        return tex;
    }
}

using UnityEngine;
using UnityEngine.Rendering;

public class MagicTrailMouse : MonoBehaviour
{
    private ParticleSystem coreLayer;
    private ParticleSystem trailLayer;
    private ParticleSystem sparkLayer;
    private Vector3 lastMousePos;

    void Start()
    {
        CreateTrailEffect();
    }

    void Update()
    {
        if (Camera.main == null) return;

        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10f;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);

        // Gerak threshold kecil biar gak flicker
        bool isMoving = Vector3.Distance(lastMousePos, worldPos) > 0.02f;
        transform.position = worldPos;

        if (isMoving)
        {
            if (!coreLayer.isEmitting)
            {
                coreLayer.Play();
                trailLayer.Play();
                sparkLayer.Play();
            }
        }
        else
        {
            if (coreLayer.isEmitting)
            {
                coreLayer.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                trailLayer.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                sparkLayer.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        lastMousePos = worldPos;
    }

    private void CreateTrailEffect()
    {
        // Inti api putih–kuning
        coreLayer = CreateParticleLayer(
            "Core",
            new Color(1f, 0.95f, 0.8f, 1f),
            new Color(1f, 0.8f, 0.3f, 0f),
            0.09f, 0.25f, 1.5f, 90f);

        // Trail oranye lembut
        trailLayer = CreateParticleLayer(
            "Trail",
            new Color(1f, 0.8f, 0.3f, 0.9f),
            new Color(1f, 0.4f, 0f, 0f),
            0.2f, 0.45f, 0.7f, 60f);

        // Percikan kecil keluar (sparks)
        sparkLayer = CreateParticleLayer(
            "Sparks",
            new Color(1f, 0.9f, 0.3f, 1f),
            new Color(1f, 0.4f, 0f, 0f),
            0.05f, 0.35f, 2.8f, 100f);

        // Spark setting tambahan
        var sparkShape = sparkLayer.shape;
        sparkShape.shapeType = ParticleSystemShapeType.Sphere;
        sparkShape.radius = 0.08f;

        var sparkMain = sparkLayer.main;
        sparkMain.startSpeed = 3.5f;
        sparkMain.gravityModifier = 0.25f;

        var sparkLimit = sparkLayer.limitVelocityOverLifetime;
        sparkLimit.enabled = true;
        sparkLimit.dampen = 0.2f;

        var sparkTrails = sparkLayer.trails;
        sparkTrails.enabled = true;
        sparkTrails.lifetime = 0.15f;
        sparkTrails.dieWithParticles = true;
    }

    private ParticleSystem CreateParticleLayer(string name, Color startColor, Color endColor, float size, float life, float speed, float emissionRate)
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
        main.maxParticles = 512;

        // === EMISSION ===
        emission.rateOverTime = emissionRate;
        emission.rateOverDistance = emissionRate * 0.3f;

        // === SHAPE ===
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 12f;
        shape.radius = 0.015f;

        // === COLOR OVER LIFETIME ===
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(startColor, 0f),
                new GradientColorKey(Color.yellow, 0.3f),
                new GradientColorKey(endColor, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.8f, 0.4f),
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

        // === MATERIAL (URP additive) ===
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.SetInt("_Surface", 1);
        mat.SetInt("_Blend", 1);
        mat.SetInt("_ZWrite", 0);
        mat.SetInt("_SrcBlend", (int)BlendMode.One);
        mat.SetInt("_DstBlend", (int)BlendMode.One);
        mat.renderQueue = (int)RenderQueue.Transparent;
        mat.EnableKeyword("_BLENDMODE_ADDITIVE");
        mat.SetColor("_BaseColor", Color.white);

        // Soft particle texture (glow bulat)
        Texture2D softTex = MakeSoftParticleTexture(128);
        softTex.wrapMode = TextureWrapMode.Clamp;
        softTex.filterMode = FilterMode.Trilinear;
        mat.mainTexture = softTex;

        renderer.material = mat;

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return ps;
    }

    private Texture2D MakeSoftParticleTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(center, new Vector2(x, y)) / radius;
                float alpha = Mathf.Exp(-dist * dist * 5f);
                float brightness = Mathf.Pow(alpha, 0.55f);
                tex.SetPixel(x, y, new Color(brightness, brightness * 0.9f, brightness * 0.5f, alpha));
            }
        }

        tex.Apply();
        tex.filterMode = FilterMode.Trilinear;
        tex.anisoLevel = 4;
        return tex;
    }
}

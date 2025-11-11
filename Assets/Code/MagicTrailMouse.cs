using UnityEngine;
using UnityEngine.Rendering;

public class MagicTrailMouse : MonoBehaviour
{
    private ParticleSystem coreLayer;
    private ParticleSystem trailLayer;
    private Vector3 lastMousePos;
    private bool isMoving;

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
            isMoving = true;
            transform.position = worldPos;

            if (!coreLayer.isEmitting)
            {
                coreLayer.Play();
                trailLayer.Play();
            }
        }
        else
        {
            isMoving = false;
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
        // Kepala putih terang (inti)
        coreLayer = CreateParticleLayer(
            "Core",
            new Color(1f, 0.95f, 0.8f, 1f),
            new Color(1f, 0.9f, 0.3f, 0.4f),
            0.09f, 0.25f, 1f, 100f);

        // Ekor kuning-oranye lembut
        trailLayer = CreateParticleLayer(
            "Trail",
            new Color(1f, 0.8f, 0.2f, 0.8f),
            new Color(1f, 0.4f, 0f, 0f),
            0.25f, 0.45f, 0.6f, 45f);
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
        main.scalingMode = ParticleSystemScalingMode.Local;
        main.gravityModifier = 0;
        main.maxParticles = 500;

        // === EMISSION ===
        emission.rateOverTime = emissionRate;
        emission.rateOverDistance = emissionRate * 0.2f;

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
                new GradientColorKey(Color.white, 0.3f),
                new GradientColorKey(endColor, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.7f, 0.7f),
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

        // === MATERIAL ADDITIVE URP ===
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.SetInt("_Surface", 1);
        mat.SetInt("_Blend", 1);
        mat.SetInt("_ZWrite", 0);
        mat.SetInt("_SrcBlend", (int)BlendMode.One);
        mat.SetInt("_DstBlend", (int)BlendMode.One);
        mat.renderQueue = (int)RenderQueue.Transparent;
        mat.EnableKeyword("_BLENDMODE_ADDITIVE");

        // warna dasar diset putih supaya gradient dominan
        mat.SetColor("_BaseColor", Color.white);

        // tekstur soft glow
        Texture2D softTex = MakeSoftParticleTexture(128);
        softTex.wrapMode = TextureWrapMode.Clamp;
        softTex.filterMode = FilterMode.Bilinear;
        mat.mainTexture = softTex;

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
                // lebih intens di tengah (gaussian)
                float alpha = Mathf.Exp(-dist * dist * 4f);
                float brightness = Mathf.Pow(alpha, 0.6f);
                tex.SetPixel(x, y, new Color(brightness, brightness, brightness, alpha));
            }
        }
        tex.Apply();
        return tex;
    }
}

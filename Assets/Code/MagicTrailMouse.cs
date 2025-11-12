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

        bool isMoving = Vector3.Distance(lastMousePos, worldPos) > 0.01f;
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
        // 🌕 Inti cahaya — besar dan halus
        coreLayer = CreateParticleLayer(
            "Core",
            new Color(1f, 0.95f, 0.8f),
            new Color(1f, 0.7f, 0.2f),
            0.25f, 0.35f, 1.6f, 140f,
            true
        );

        // 🔥 Ekor — memanjang ke belakang
        trailLayer = CreateParticleLayer(
            "Trail",
            new Color(1f, 0.8f, 0.4f, 0.9f),
            new Color(1f, 0.3f, 0f, 0.1f),
            0.22f, 0.45f, 1.2f, 100f,
            false
        );

        // ⚡ Percikan api — menyebar ke luar
        sparkLayer = CreateParticleLayer(
            "Sparks",
            new Color(1f, 0.9f, 0.5f),
            new Color(1f, 0.35f, 0f),
            0.09f, 0.6f, 3.5f, 180f,
            false
        );

        var smain = sparkLayer.main;
        smain.gravityModifier = 0.3f;
        smain.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4.5f);
        smain.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.14f);
        smain.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);

        // Bentuk menyebar — seperti percikan api keluar
        var sshape = sparkLayer.shape;
        sshape.shapeType = ParticleSystemShapeType.Cone;
        sshape.angle = 40f;
        sshape.radius = 0.08f;

        // Membuat percikan melengkung
        var velocity = sparkLayer.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(-1.2f, 1.2f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.5f, 2.4f);
        velocity.z = new ParticleSystem.MinMaxCurve(-1.2f, 1.2f);

        var limit = sparkLayer.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.dampen = 0.35f;

        // Random rotasi & ukuran
        var rotation = sparkLayer.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-180f, 180f);
    }

    private ParticleSystem CreateParticleLayer(
        string name,
        Color startColor,
        Color endColor,
        float size,
        float life,
        float speed,
        float emissionRate,
        bool intenseCore
    )
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
        main.maxParticles = 2000;
        main.scalingMode = ParticleSystemScalingMode.Local;

        // === EMISSION ===
        emission.rateOverTime = emissionRate;

        // === SHAPE ===
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 12f;
        shape.radius = 0.03f;

        // === COLOR OVER LIFETIME ===
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(startColor, 0f),
                new GradientColorKey(new Color(1f, 0.8f, 0.3f), 0.3f),
                new GradientColorKey(endColor, 0.6f),
                new GradientColorKey(new Color(endColor.r, endColor.g, endColor.b, 0f), 1f)
            },
            new GradientAlphaKey[] {
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
        renderer.sortingOrder = intenseCore ? 3 : 2;
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
        mat.SetColor("_BaseColor", intenseCore
            ? new Color(1f, 0.95f, 0.7f, 1f)
            : new Color(1f, 0.6f, 0.2f, 1f));
        mat.mainTexture = MakeSoftCometTexture(192);

        renderer.material = mat;

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return ps;
    }

    private Texture2D MakeSoftCometTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - size / 2f) / (size / 2f);
                float dy = (y - size / 2f) / (size / 2f);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Exp(-dist * dist * 2.4f);
                float brightness = Mathf.Pow(alpha, 0.85f);
                tex.SetPixel(x, y, new Color(
                    brightness,
                    brightness * 0.85f,
                    brightness * 0.45f,
                    alpha
                ));
            }
        }
        tex.Apply();
        return tex;
    }
}

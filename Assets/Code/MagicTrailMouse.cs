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
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10f;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);

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
        // Kepala komet — bulat, intensitas tinggi
        coreLayer = CreateParticleLayer(
            "Core",
            new Color(1f, 0.95f, 0.8f, 1f),
            new Color(1f, 0.6f, 0.1f, 0f),
            0.4f, 0.5f, 1.6f, 150f,
            true,
            ParticleSystemRenderMode.Billboard
        );

        // Ekor komet — masih bulat tapi agak redup & sedikit lebih besar radiusnya
        trailLayer = CreateParticleLayer(
            "Trail",
            new Color(1f, 0.9f, 0.4f, 0.9f),
            new Color(1f, 0.2f, 0f, 0f),
            0.45f, 0.6f, 1.2f, 100f,
            false,
            ParticleSystemRenderMode.Billboard
        );

        // Percikan — lebih besar & banyak, warna oranye kemerahan
        sparkLayer = CreateParticleLayer(
            "Sparks",
            new Color(1f, 0.8f, 0.3f, 1f),
            new Color(1f, 0.2f, 0f, 0f),
            0.15f, 0.5f, 3.5f, 120f,
            false,
            ParticleSystemRenderMode.Billboard
        );

        var smain = sparkLayer.main;
        smain.gravityModifier = 0.4f;
        smain.startSpeed = 4f;

        var sshape = sparkLayer.shape;
        sshape.shapeType = ParticleSystemShapeType.Sphere;
        sshape.radius = 0.2f;

        var slimit = sparkLayer.limitVelocityOverLifetime;
        slimit.enabled = true;
        slimit.dampen = 0.3f;
    }

    private ParticleSystem CreateParticleLayer(
        string name,
        Color startColor,
        Color endColor,
        float size,
        float life,
        float speed,
        float emissionRate,
        bool intenseCore,
        ParticleSystemRenderMode renderMode
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

        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = life;
        main.startSpeed = speed;
        main.startSize = size;
        main.startColor = startColor;
        main.scalingMode = ParticleSystemScalingMode.Local;
        main.maxParticles = 1000;

        emission.rateOverTime = emissionRate;
        emission.rateOverDistance = emissionRate * 0.25f;

        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 25f;
        shape.radius = 0.1f;

        // Gradasi api kuning → oranye → merah
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.mode = GradientMode.Blend;
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(1f, 0.95f, 0.6f), 0.2f),
                new GradientColorKey(new Color(1f, 0.7f, 0.1f), 0.5f),
                new GradientColorKey(new Color(1f, 0.2f, 0f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.8f, 0.4f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        col.color = grad;

        // Mengecil perlahan
        sizeOver.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 1f);
        curve.AddKey(1f, 0f);
        sizeOver.size = new ParticleSystem.MinMaxCurve(1f, curve);

        // Renderer URP additive (tanpa stretch)
        renderer.renderMode = renderMode;
        renderer.sortingOrder = 3;
        renderer.alignment = ParticleSystemRenderSpace.View;

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.SetInt("_Surface", 1);
        mat.SetInt("_Blend", 1);
        mat.SetInt("_ZWrite", 0);
        mat.SetInt("_SrcBlend", (int)BlendMode.One);
        mat.SetInt("_DstBlend", (int)BlendMode.One);
        mat.renderQueue = (int)RenderQueue.Transparent;
        mat.EnableKeyword("_BLENDMODE_ADDITIVE");

        mat.SetColor("_BaseColor", intenseCore
            ? new Color(1f, 0.85f, 0.5f, 1f)
            : new Color(1f, 0.5f, 0.1f, 1f));

        mat.mainTexture = MakeSoftCometTexture(128);
        renderer.material = mat;

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return ps;
    }

    // Tekstur bulat lembut dengan gradiasi cahaya api
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
                float alpha = Mathf.Exp(-dist * dist * 3f);
                float brightness = Mathf.Pow(alpha, 0.8f);
                tex.SetPixel(x, y, new Color(
                    brightness,
                    brightness * 0.9f,
                    brightness * 0.4f,
                    alpha
                ));
            }
        }
        tex.Apply();
        return tex;
    }
}

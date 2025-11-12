using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class MagicTrailMouse : MonoBehaviour
{
    private ParticleSystem coreLayer;
    private ParticleSystem trailLayer;
    private ParticleSystem sparkLayer;
    private ParticleSystem flameLayer;

    private LineRenderer flameLine; // 🔥 garis merah halus menyala
    private List<Vector3> flamePositions = new List<Vector3>();
    private int maxFlamePoints = 5;
    private float smoothSpeed = 14f;

    private Vector3 lastMousePos;

    private Vector3 prevDirection = Vector3.back; // simpan arah sebelumnya agar stabil

    void Start()
    {
        CreateTrailEffect();
        CreateFlameLine(); // 🔥 efek garis merah menyala
    }

    void Update()
    {
        if (Camera.main == null) return;

        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10f;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);

        bool isMoving = Vector3.Distance(lastMousePos, worldPos) > 0.01f;
        transform.position = worldPos;

        // kontrol partikel
        if (isMoving)
        {
            if (!coreLayer.isEmitting)
            {
                coreLayer.Play();
                trailLayer.Play();
                sparkLayer.Play();
                flameLayer.Play();
            }
        }
        else
        {
            if (coreLayer.isEmitting)
            {
                coreLayer.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                trailLayer.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                sparkLayer.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                flameLayer.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        // update garis merah
        UpdateFlameLine(worldPos, isMoving);

        lastMousePos = worldPos;
    }

    // ===========================================================
    // 🔥 LINE RENDERER TRAIL MERAH
    // ===========================================================
   // ===========================================================
    // 🔥 LINE RENDERER TRAIL MERAH MENYALA TERANG
    // ===========================================================
    private void CreateFlameLine()
    {
        GameObject lineObj = new GameObject("FlameLine");
        lineObj.transform.SetParent(transform);
        lineObj.transform.localPosition = Vector3.zero;

        flameLine = lineObj.AddComponent<LineRenderer>();
        flameLine.positionCount = 0;
        flameLine.numCapVertices = 8;
        flameLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        flameLine.receiveShadows = false;
        flameLine.alignment = LineAlignment.View;

        // Lebar lebih besar agar jelas dan taper halus ke ujung
        AnimationCurve widthCurve = new AnimationCurve();
        widthCurve.AddKey(0f, 1.2f);
        widthCurve.AddKey(0.5f, 0.5f);
        widthCurve.AddKey(1f, 0.05f);
        flameLine.widthCurve = widthCurve;
        flameLine.widthMultiplier = 0.09f;

        // Warna gradasi lebih menyala (kuning → oranye → merah)
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(1f, 0.9f, 0.4f), 0f),
                new GradientColorKey(new Color(1f, 0.4f, 0.05f), 0.4f),
                new GradientColorKey(new Color(0.3f, 0.02f, 0f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.6f, 0.4f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        flameLine.colorGradient = grad;

        // 🔥 Material dengan emisi glow additive
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        mat.SetFloat("_Surface", 1);
        mat.SetInt("_Blend", 1);
        mat.SetInt("_ZWrite", 0);
        mat.SetInt("_SrcBlend", (int)BlendMode.One);
        mat.SetInt("_DstBlend", (int)BlendMode.One);
        mat.EnableKeyword("_BLENDMODE_ADDITIVE");
        mat.SetColor("_BaseColor", new Color(1f, 0.4f, 0.1f, 1f)); // oranye menyala
        flameLine.material = mat;
    }

   private void UpdateFlameLine(Vector3 worldPos, bool isMoving)
    {
        if (flameLine == null) return;
        flameLine.enabled = isMoving;

        if (!isMoving)
        {
            flamePositions.Clear();
            flameLine.positionCount = 0;
            prevDirection = Vector3.back;
            return;
        }

        Vector3 rawDir = worldPos - lastMousePos;
        Vector3 dir = rawDir.sqrMagnitude > 1e-6f ? rawDir.normalized : prevDirection;
        prevDirection = dir;

        float headLerp = Mathf.Clamp01(Time.deltaTime * (smoothSpeed * 2f));
        Vector3 headPos = flamePositions.Count > 0
            ? Vector3.Lerp(flamePositions[0], worldPos, headLerp)
            : worldPos;

        float segmentDist = 0.08f;

        if (flamePositions.Count != maxFlamePoints)
        {
            flamePositions.Clear();
            for (int i = 0; i < maxFlamePoints; i++)
                flamePositions.Add(headPos - dir * segmentDist * i);
        }
        else
        {
            for (int i = 0; i < maxFlamePoints; i++)
            {
                Vector3 pos = headPos - dir * segmentDist * i;

                // Efek “nyala hidup” lebih kuat dan sedikit acak
                float wave = Mathf.Sin(Time.time * 30f + i * 0.6f) * 0.015f * (1f - i / (float)maxFlamePoints);
                Vector3 side = Vector3.Cross(dir, Vector3.forward).normalized;
                pos += side * wave;

                flamePositions[i] = pos;
            }
        }

        flameLine.positionCount = flamePositions.Count;
        flameLine.SetPositions(flamePositions.ToArray());

        // ✨ Oscillate width supaya seperti “denyut api”
        float flicker = 1f + Mathf.Sin(Time.time * 8f) * 0.15f;
        flameLine.widthMultiplier = 0.09f * flicker;
    }

    // ===========================================================
    // ✨ LAYER PARTIKEL
    // ===========================================================
    private void CreateTrailEffect()
    {
        // 🌕 Inti cahaya
        coreLayer = CreateParticleLayer(
            "Core",
            new Color(1f, 0.95f, 0.8f),
            new Color(1f, 0.7f, 0.2f),
            0.25f, 0.35f, 1.6f, 140f,
            true
        );

        // 🔥 Ekor oranye lembut
        trailLayer = CreateParticleLayer(
            "Trail",
            new Color(1f, 0.8f, 0.4f, 0.9f),
            new Color(1f, 0.3f, 0f, 0.1f),
            0.22f, 0.45f, 1.2f, 100f,
            false
        );

        // ⚡ Percikan luar
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

        var sshape = sparkLayer.shape;
        sshape.shapeType = ParticleSystemShapeType.Cone;
        sshape.angle = 40f;
        sshape.radius = 0.08f;

        var velocity = sparkLayer.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(-1.2f, 1.2f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.5f, 2.4f);
        velocity.z = new ParticleSystem.MinMaxCurve(-1.2f, 1.2f);

        // 🔥 Lidah api merah
        flameLayer = CreateParticleLayer(
            "Flame",
            new Color(1f, 0.15f, 0f, 0.9f),
            new Color(1f, 0.02f, 0f, 0f),
            0.12f, 0.55f, 0.6f, 160f,
            false
        );

        var fshape = flameLayer.shape;
        fshape.shapeType = ParticleSystemShapeType.Cone;
        fshape.angle = 6f;
        fshape.radius = 0.02f;
        fshape.rotation = new Vector3(180f, 0f, 0f);

        var fmain = flameLayer.main;
        fmain.gravityModifier = 0f;
        fmain.startSpeed = new ParticleSystem.MinMaxCurve(2.2f, 3.5f);
        fmain.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.1f);
        fmain.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.55f);

        var fvel = flameLayer.velocityOverLifetime;
        fvel.enabled = true;
        fvel.space = ParticleSystemSimulationSpace.World;
        fvel.z = new ParticleSystem.MinMaxCurve(-4.5f, -6f);

        var fnoise = flameLayer.noise;
        fnoise.enabled = true;
        fnoise.strength = 0.1f;
        fnoise.frequency = 1.5f;
        fnoise.scrollSpeed = 0.5f;
        fnoise.octaveCount = 1;
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

        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = life;
        main.startSpeed = speed;
        main.startSize = size;
        main.startColor = startColor;
        main.maxParticles = 2000;
        main.scalingMode = ParticleSystemScalingMode.Local;

        emission.rateOverTime = emissionRate;

        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 12f;
        shape.radius = 0.03f;

        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(startColor, 0f),
                new GradientColorKey(endColor, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        col.color = grad;

        sizeOver.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 1f);
        curve.AddKey(1f, 0f);
        sizeOver.size = new ParticleSystem.MinMaxCurve(1f, curve);

        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = intenseCore ? 3 : 2;
        renderer.alignment = ParticleSystemRenderSpace.View;

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.SetInt("_Surface", 1);
        mat.SetInt("_Blend", 1);
        mat.SetInt("_ZWrite", 0);
        mat.SetInt("_SrcBlend", (int)BlendMode.One);
        mat.SetInt("_DstBlend", (int)BlendMode.One);
        mat.renderQueue = (int)RenderQueue.Transparent;
        mat.EnableKeyword("_BLENDMODE_ADDITIVE");
        mat.SetColor("_BaseColor", startColor);
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

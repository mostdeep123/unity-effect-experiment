using UnityEngine;

[ExecuteAlways]
public class MagicTrailMouse : MonoBehaviour
{
    [Header("Material (optional) - leave empty to auto-create URP Unlit Additive")]
    public Material additiveMat;

    [Header("Mouse / World")]
    public float groundY = 0f;           // fallback plane Y if raycast misses
    public float stopDelay = 0.20f;      // how long to wait before stopping emission when mouse stops
    public float moveThreshold = 0.01f;  // minimal mouse movement to consider "moving"

    // particle systems
    private ParticleSystem core, flame, sparks, embers, glow;
    private Vector3 lastMouseWorldPos = Vector3.positiveInfinity;
    private float lastMoveTime;

    void Start()
    {
        if (Application.isPlaying)
        {
            EnsureMaterial();
            CreateParticlesOnce();
        }
    }

    void EnsureMaterial()
    {
        if (additiveMat != null) return;

        Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (s == null)
        {
            Debug.LogWarning("URP Particles/Unlit shader not found. Using default particle shader.");
            additiveMat = new Material(Shader.Find("Particles/Additive"));
            return;
        }

        additiveMat = new Material(s);
        // URP Unlit has properties; we want additive blending look.
        // Many URP particle shaders respect _Surface/_Blend etc when using Graph; keep simple.
        // We'll rely on shader default and set renderQueue lightly higher.
        additiveMat.renderQueue = 3000;
    }

    void CreateParticlesOnce()
    {
        if (core != null) return;
        // Parent will be this GameObject's transform
        core   = MakePS("Core_Streak", 0.35f, 0f, 1.0f, 140f, additiveMat, ParticleSystemRenderMode.Stretch);
        flame  = MakePS("FlameBody", 0.75f, 0.2f, 1.2f, 90f, additiveMat);
        sparks = MakePS("Sparks", 1.0f, 2.0f, 0.08f, 30f, additiveMat);
        embers = MakePS("Embers", 1.4f, 0.4f, 0.1f, 18f, additiveMat);
        glow   = MakePS("SoftGlow", 0.18f, 0f, 4.0f, 1f, additiveMat);

        // customize modules that require struct access
        // CORE: noise + velocity over lifetime + stretched settings
        {
            var noise = core.noise;
            noise.enabled = true;
            noise.strength = 0.09f;
            noise.frequency = 1f;

            var vel = core.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;

            // small outward velocity, use radial via radial component on x,y if wanted (simple form:)
            vel.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            var rend = core.GetComponent<ParticleSystemRenderer>();
            rend.lengthScale = 3f;
            rend.velocityScale = 1.6f;
        }

        // FLAME: noise + color/size over lifetime
        {
            var noise = flame.noise;
            noise.enabled = true;
            noise.strength = 0.35f;
            noise.frequency = 0.8f;

            var col = flame.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(1f,0.85f,0.2f), 0.18f),
                    new GradientColorKey(new Color(1f,0.5f,0.1f), 0.5f),
                    new GradientColorKey(new Color(0.6f,0.18f,0.05f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 0.25f),
                    new GradientAlphaKey(0.6f, 0.6f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            col.color = new ParticleSystem.MinMaxGradient(g);

            var size = flame.sizeOverLifetime;
            size.enabled = true;
            AnimationCurve sc = new AnimationCurve();
            sc.AddKey(0f, 1f);
            sc.AddKey(0.35f, 1.35f);
            sc.AddKey(1f, 0.35f);
            size.size = new ParticleSystem.MinMaxCurve(1f, sc);
        }

        // SPARKS: gravity, bursts / rate over distance fallback
        {
            var main = sparks.main;
            main.gravityModifier = 0.6f;

            var emission = sparks.emission;
            emission.enabled = true;
            // give both bursts and rateOverDistance minimal
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, 6, 8, 1, 0.06f) // emits bursts repeated by cycle via small time? (first param= time)
            });
            // rateOverDistance:
            emission.rateOverDistance = new ParticleSystem.MinMaxCurve(30f);

            var shape = sparks.shape;
            shape.angle = 35f;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.radius = 0.01f;

            var rend = sparks.GetComponent<ParticleSystemRenderer>();
            // optional small trail
            var trails = sparks.trails;
            trails.enabled = false;
        }

        // EMBERS: gentle gravity, slow scatter
        {
            var main = embers.main;
            main.gravityModifier = 0.2f;

            var emission = embers.emission;
            emission.rateOverDistance = new ParticleSystem.MinMaxCurve(18f);

            var col = embers.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1f,0.6f,0.1f), 0f),
                    new GradientColorKey(new Color(0.8f,0.35f,0.08f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            col.color = new ParticleSystem.MinMaxGradient(g);
        }

        // GLOW: single large soft sprite
        {
            var main = glow.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var emission = glow.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, 1)
            });

            var rend = glow.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Billboard;
        }

        // parent all to this transform (so transform.position is emitter)
        core.transform.SetParent(transform, false);
        flame.transform.SetParent(transform, false);
        sparks.transform.SetParent(transform, false);
        embers.transform.SetParent(transform, false);
        glow.transform.SetParent(transform, false);

        // ensure emission initially off until user moves mouse
        SetEmission(false);
    }

    // helper to create PS with basic modules
    ParticleSystem MakePS(string name, float life, float speed, float size, float rateOverDistance, Material mat, ParticleSystemRenderMode renderMode = ParticleSystemRenderMode.Billboard)
    {
        GameObject go = new GameObject(name);
        go.transform.position = transform.position;
        var ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;   
        main.duration = 5f;
        main.startLifetime = life;
        main.startSpeed = speed;
        main.startSize = size;
        main.maxParticles = 2000;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.rateOverDistance = new ParticleSystem.MinMaxCurve(rateOverDistance);

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.01f;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.material = mat;
        renderer.renderMode = renderMode;
        renderer.sortingFudge = 1;

        return ps;
    }


    void Update()
    {
        if (!Application.isPlaying) return;

        // get mouse world position (raycast to collider; if misses use plane at groundY)
        if (!GetMouseWorld(out Vector3 mouseWorld, out bool valid))
        {
            SetEmission(false);
            lastMouseWorldPos = Vector3.positiveInfinity;
            return;
        }

        transform.position = mouseWorld;

        bool moved = false;
        if (lastMouseWorldPos == Vector3.positiveInfinity)
        {
            moved = true;
        }
        else
        {
            float d = Vector3.Distance(mouseWorld, lastMouseWorldPos);
            if (d > moveThreshold) moved = true;
        }

        if (moved)
        {
            SetEmission(true);
            lastMoveTime = Time.time;
        }
        else
        {
            if (Time.time - lastMoveTime > stopDelay)
                SetEmission(false);
        }

        lastMouseWorldPos = mouseWorld;
    }

    // returns false only if something invalid (should not happen)
    bool GetMouseWorld(out Vector3 worldPos, out bool valid)
    {
        worldPos = Vector3.zero;
        valid = false;

        Camera cam = Camera.main;
        if (cam == null) return false;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        // first try physics raycast
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            worldPos = hit.point;
            valid = true;
            return true;
        }

        // fallback: intersect with horizontal plane at groundY
        Plane p = new Plane(Vector3.up, new Vector3(0f, groundY, 0f));
        if (p.Raycast(ray, out float enter))
        {
            worldPos = ray.GetPoint(enter);
            valid = true;
            return true;
        }

        return false;
    }

    void SetEmission(bool on)
    {
        SetEmission(core, on);
        SetEmission(flame, on);
        SetEmission(sparks, on);
        SetEmission(embers, on);
        SetEmission(glow, on);
    }

    void SetEmission(ParticleSystem ps, bool on)
    {
        if (ps == null) return;
        var emission = ps.emission;
        emission.enabled = on;

        // also control plays/stops to avoid stray particles when disabling
        if (on)
        {
            if (!ps.isPlaying) ps.Play();
        }
        else
        {
            if (ps.isPlaying) ps.Stop();
        }
    }

    // ensure created in editor when toggling Play (optional)
    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            // don't create particles in editor to avoid clutter,
            // but keep material creation safe
            if (additiveMat == null)
            {
                Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (s != null) additiveMat = new Material(s);
            }
        }
    }
}

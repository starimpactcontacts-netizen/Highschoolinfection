using System.Collections;
using UnityEngine;

/// <summary>
/// Builds the dark stormy afternoon look/feel the instant Play starts — no manual steps.
/// Covers everything that isn't a shader (see StormPostProcess/ToonOutlineApplier/WetSurfaceApplier
/// for those): dim overcast directional light with soft/no shadows, fog + solid-color sky standing
/// in for storm clouds (no cloud texture available, so a flat dark grey-blue reads as a uniform
/// overcast sky, which is realistically what overcast skies actually look like), a rain particle
/// system that follows the player, a looping rain-hiss audio track, and occasional distant
/// lightning (light flash + delayed thunder rumble) — both audio clips are generated procedurally
/// (filtered noise) since there are no audio assets in the project and none were provided.
/// </summary>
public class StormAtmosphere : MonoBehaviour
{
    [Header("Lighting")]
    [SerializeField] private Color lightColor = new Color(0.62f, 0.65f, 0.68f);
    [SerializeField] private float lightIntensity = 0.85f; // was 0.65 — too dim to read character detail/color

    [Header("Fog / Sky")]
    [SerializeField] private Color stormColor = new Color(0.24f, 0.26f, 0.29f);
    [SerializeField] private float fogStart = 18f;
    [SerializeField] private float fogEnd = 55f;

    [Header("Rain")]
    [SerializeField] private float rainHeight = 12f;
    [SerializeField] private float rainAreaSize = 45f;

    [Header("Lightning")]
    [SerializeField] private float lightningMinInterval = 15f;
    [SerializeField] private float lightningMaxInterval = 40f;

    private Light directionalLight;
    private ParticleSystem rainSystem;
    private Transform rainFollowTarget;
    private AudioSource rainAudioSource;
    private AudioSource thunderAudioSource;

    private void Awake()
    {
        SetupLighting();
        SetupSkyAndFog();
        SetupRain();
        SetupAudio();
        StartCoroutine(LightningLoop());
    }

    private void Update()
    {
        if (rainSystem == null) return;

        if (rainFollowTarget == null)
        {
            var player = GameObject.Find("Player");
            if (player != null) rainFollowTarget = player.transform;
            else if (Camera.main != null) rainFollowTarget = Camera.main.transform;
        }

        if (rainFollowTarget != null)
        {
            Vector3 pos = rainFollowTarget.position;
            pos.y = rainFollowTarget.position.y + rainHeight;
            rainSystem.transform.position = pos;
        }
    }

    // ------------------------------------------------------------------
    //  Lighting
    // ------------------------------------------------------------------

    private void SetupLighting()
    {
        foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (light.type == LightType.Directional) { directionalLight = light; break; }
        }
        if (directionalLight == null)
        {
            var go = new GameObject("Directional Light");
            directionalLight = go.AddComponent<Light>();
            directionalLight.type = LightType.Directional;
            directionalLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        directionalLight.color = lightColor;
        directionalLight.intensity = lightIntensity;
        // Overcast light is diffuse/scattered — no crisp sun shadows.
        directionalLight.shadows = LightShadows.None;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = stormColor * 0.9f;
    }

    // ------------------------------------------------------------------
    //  Sky / Fog
    // ------------------------------------------------------------------

    private void SetupSkyAndFog()
    {
        if (Camera.main != null)
        {
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = stormColor;

            if (Camera.main.GetComponent<StormPostProcess>() == null)
                Camera.main.gameObject.AddComponent<StormPostProcess>();
        }

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = stormColor;
        RenderSettings.fogStartDistance = fogStart;
        RenderSettings.fogEndDistance = fogEnd;
    }

    // ------------------------------------------------------------------
    //  Rain particles
    // ------------------------------------------------------------------

    private void SetupRain()
    {
        var rainGO = new GameObject("RainParticles");
        rainSystem = rainGO.AddComponent<ParticleSystem>();

        var main = rainSystem.main;
        main.loop = true;
        main.startLifetime = 1.5f; // collision usually cuts this short; this is just the max before a despawn with no hit
        main.startSpeed = 0f; // velocity module drives fall speed directly, not this
        main.startSize = 0.025f;
        main.startColor = new Color(0.65f, 0.7f, 0.75f, 0.28f);
        main.maxParticles = 4000;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0f;

        var emission = rainSystem.emission;
        emission.rateOverTime = 1200f;

        var shape = rainSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(rainAreaSize, 0.1f, rainAreaSize);

        var vel = rainSystem.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.y = new ParticleSystem.MinMaxCurve(-22f);

        // Without this, rain is just a world-space effect falling in a straight line regardless of
        // geometry — it doesn't know a roof is overhead, so it fell straight through into any
        // interior space. Colliding against real world geometry (the map's MeshColliders,
        // back-filled in GameBootstrap) and killing the particle on impact stops it at rooftops.
        var collision = rainSystem.collision;
        collision.enabled = true;
        collision.type = ParticleSystemCollisionType.World;
        collision.mode = ParticleSystemCollisionMode.Collision3D;
        collision.quality = ParticleSystemCollisionQuality.Medium;
        collision.dampen = new ParticleSystem.MinMaxCurve(1f);
        collision.lifetimeLoss = new ParticleSystem.MinMaxCurve(1f); // dies immediately on hit — no rain "inside" once it lands on a roof
        collision.maxCollisionShapes = 512;

        var renderer = rainSystem.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.05f;
        renderer.lengthScale = 2f;

        Shader particleShader = Shader.Find("Legacy Shaders/Particles/Alpha Blended") ?? Shader.Find("Particles/Alpha Blended");
        if (particleShader != null)
        {
            var mat = new Material(particleShader);
            renderer.material = mat;
        }

        rainSystem.Play();
    }

    // ------------------------------------------------------------------
    //  Audio (procedurally generated — no audio assets exist in the project)
    // ------------------------------------------------------------------

    private void SetupAudio()
    {
        var rainGO = new GameObject("RainAudio");
        rainGO.transform.SetParent(transform);
        rainAudioSource = rainGO.AddComponent<AudioSource>();
        rainAudioSource.clip = GenerateFilteredNoiseClip("RainHiss", 4f, 22050, 0.08f, null);
        rainAudioSource.loop = true;
        rainAudioSource.spatialBlend = 0f; // constant/ambient, not positional
        rainAudioSource.volume = 0.35f;
        rainAudioSource.Play();

        var thunderGO = new GameObject("ThunderAudio");
        thunderGO.transform.SetParent(transform);
        thunderAudioSource = thunderGO.AddComponent<AudioSource>();
        thunderAudioSource.spatialBlend = 0f;
        thunderAudioSource.volume = 0.5f;
    }

    private static AudioClip GenerateFilteredNoiseClip(string name, float duration, int sampleRate, float smoothing, System.Func<float, float> envelope)
    {
        int sampleCount = Mathf.CeilToInt(duration * sampleRate);
        var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        float[] data = new float[sampleCount];
        var rng = new System.Random(12345);
        float prev = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float white = (float)(rng.NextDouble() * 2.0 - 1.0);
            prev = Mathf.Lerp(prev, white, smoothing);
            data[i] = prev;
        }

        float max = 0.0001f;
        for (int i = 0; i < sampleCount; i++) max = Mathf.Max(max, Mathf.Abs(data[i]));
        for (int i = 0; i < sampleCount; i++)
        {
            float v = data[i] / max * 0.6f;
            if (envelope != null) v *= envelope((float)i / sampleCount);
            data[i] = v;
        }

        clip.SetData(data, 0);
        return clip;
    }

    // ------------------------------------------------------------------
    //  Lightning
    // ------------------------------------------------------------------

    private IEnumerator LightningLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(lightningMinInterval, lightningMaxInterval));
            yield return StartCoroutine(FlashLightning());
        }
    }

    private IEnumerator FlashLightning()
    {
        float baseIntensity = lightIntensity;
        float flashIntensity = baseIntensity + 2.5f;

        // Two quick flickers, like distant lightning.
        for (int i = 0; i < 2; i++)
        {
            directionalLight.intensity = flashIntensity;
            yield return new WaitForSeconds(0.06f);
            directionalLight.intensity = baseIntensity;
            yield return new WaitForSeconds(0.08f);
        }

        // Thunder arrives after a delay — "distant" lightning, sound lags light.
        float delay = Random.Range(0.8f, 2.5f);
        yield return new WaitForSeconds(delay);

        if (thunderAudioSource != null)
        {
            float duration = Random.Range(2f, 3.5f);
            thunderAudioSource.clip = GenerateFilteredNoiseClip("ThunderRumble", duration, 22050, 0.25f,
                t => Mathf.Pow(1f - t, 1.5f) * (t < 0.05f ? t / 0.05f : 1f));
            thunderAudioSource.Play();
        }
    }

    // Runs the instant Play starts — this is the only thing the user has to do.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Object.FindFirstObjectByType<StormAtmosphere>() != null) return;
        new GameObject("StormAtmosphere").AddComponent<StormAtmosphere>();
    }
}

#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.VFX;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Cria automaticamente a cena de demonstração na primeira abertura.
/// Menu manual: Ferramentas > Fogueira > Recriar cena.
/// A cena contém três sistemas de partículas CPU para prévia visual E
/// um VFX Graph GPU (modelo Simple Loop) disponível para edição.
/// </summary>
[InitializeOnLoad]
public static class InstalarFogueira
{
    private const string CaminhoCena = "Assets/Scenes/Fogueira.unity";
    private const string PastaVfx = "Assets/VFX";
    private const string CaminhoGraph = PastaVfx + "/Fogueira_GPU.vfx";
    private const string CaminhoTextura = "Assets/Textures/ParticulaSuave.png";
    private const string CaminhoMaterial = "Assets/Materials/ParticulaSuave.mat";

    static InstalarFogueira()
    {
        EditorApplication.delayCall += IniciarSeNecessario;
    }

    private static void IniciarSeNecessario()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(CaminhoCena) != null) return;
        if (EditorSceneManager.GetActiveScene().isDirty) return;
        try { CriarCena(); }
        catch (Exception e) { Debug.LogError("Não foi possível montar a fogueira: " + e); }
    }

    [MenuItem("Ferramentas/Fogueira/Recriar cena")]
    public static void CriarCena()
    {
        GarantirPasta("Assets/Scenes");
        GarantirPasta("Assets/Materials");
        GarantirPasta("Assets/Textures");
        GarantirPasta(PastaVfx);
        ConfigurarUrp();
        var material = CriarMaterial();
        var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.12f, .10f, .095f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(.038f, .035f, .042f);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = .026f;

        var raiz = new GameObject("FOGUEIRA - DEMONSTRACAO");
        new GameObject("Lenha e brasa").transform.SetParent(raiz.transform);
        var lenha = raiz.transform.Find("Lenha e brasa");

        CriarChao(raiz.transform);
        CriarLenha(lenha);
        CriarBrasas(lenha, material);

        var visual = new GameObject("VFX - Fogo, fumaca e faiscas (preview)");
        visual.transform.SetParent(raiz.transform);
        CriarParticulas(visual.transform, material);

        var gpu = new GameObject("VFX Graph - GPU (editar grafo)");
        gpu.transform.SetParent(raiz.transform);
        gpu.transform.localPosition = new Vector3(0f, .3f, 0f);
        var vfx = gpu.AddComponent<VisualEffect>();
        vfx.visualEffectAsset = CriarGraph();
        if (vfx.visualEffectAsset == null)
            Debug.LogWarning("O Graph GPU não foi copiado. Use Ferramentas > Fogueira > Recriar cena após a importação do VFX Graph.");

        var luzObjeto = new GameObject("Luz da fogueira - tremulante");
        luzObjeto.transform.SetParent(raiz.transform);
        luzObjeto.transform.position = new Vector3(0f, 1.0f, 0f);
        var luz = luzObjeto.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = new Color(1f, .43f, .13f);
        luz.range = 9f;
        luz.intensity = 5.2f;
        luz.shadows = LightShadows.Soft;
        luzObjeto.AddComponent<FogueiraLuz>();

        var cameraObj = new GameObject("Main Camera");
        cameraObj.tag = "MainCamera";
        var camera = cameraObj.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.028f, .027f, .033f);
        camera.fieldOfView = 53f;
        cameraObj.transform.position = new Vector3(3.4f, 2.1f, -6.1f);
        cameraObj.transform.LookAt(new Vector3(0f, 1.1f, 0f));
        cameraObj.AddComponent<AudioListener>();
        
        Selection.activeGameObject = raiz;
        EditorSceneManager.MarkSceneDirty(cena);
        EditorSceneManager.SaveScene(cena, CaminhoCena);
        EditorBuildSettings.scenes = new [] { new EditorBuildSettingsScene(CaminhoCena, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("FOGUEIRA: cena criada. Abra Assets/Scenes/Fogueira.unity e aperte Play.");
    }

    private static void ConfigurarUrp()
    {
        const string p = "Assets/Settings";
        GarantirPasta(p);
        const string rendererPath = p + "/FogueiraRenderer.asset";
        const string pipelinePath = p + "/FogueiraURP.asset";
        var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
        if (renderer == null)
        {
            renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(renderer, rendererPath);
        }
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
        if (pipeline == null)
        {
            pipeline = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            AssetDatabase.CreateAsset(pipeline, pipelinePath);
        }
        var so = new SerializedObject(pipeline);
        var list = so.FindProperty("m_RendererDataList");
        if (list != null)
        {
            list.arraySize = 1;
            list.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        GraphicsSettings.defaultRenderPipeline = pipeline;
        QualitySettings.renderPipeline = pipeline;
        EditorUtility.SetDirty(pipeline);
    }

    private static Material CriarMaterial()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(CaminhoMaterial);
        if (mat != null) return mat;
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(CaminhoTextura) == null)
        {
            const int s = 128;
            var t = new Texture2D(s, s, TextureFormat.RGBA32, false);
            t.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = (x + .5f - s / 2f) / (s / 2f);
                    float dy = (y + .5f - s / 2f) / (s / 2f);
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Pow(Mathf.Clamp01(1f - r), 1.9f);
                    t.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            t.Apply();
            File.WriteAllBytes(CaminhoTextura, t.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(CaminhoTextura);
            var importer = AssetImporter.GetAtPath(CaminhoTextura) as TextureImporter;
            if (importer != null) { importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.SaveAndReimport(); }
        }

        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        mat = new Material(shader);
        mat.name = "ParticulaSuave";
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(CaminhoTextura);
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 1f);
        mat.renderQueue = 3000;
        AssetDatabase.CreateAsset(mat, CaminhoMaterial);
        return mat;
    }

    private static VisualEffectAsset CriarGraph()
    {
        var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(CaminhoGraph);
        if (asset != null) return asset;
        const string origem = "Packages/com.unity.visualeffectgraph/Editor/Templates/Simple_Loop.vfx";
        bool ok = AssetDatabase.CopyAsset(origem, CaminhoGraph);
        if (!ok)
        {
            Debug.LogWarning("Modelo Simple_Loop.vfx não encontrado no pacote. Crie manualmente um VFX Graph pelo menu Create > Visual Effects.");
            return null;
        }
        AssetDatabase.ImportAsset(CaminhoGraph, ImportAssetOptions.ForceSynchronousImport);
        return AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(CaminhoGraph);
    }

    private static void CriarChao(Transform parent)
    {
        var chao = GameObject.CreatePrimitive(PrimitiveType.Plane);
        chao.name = "Chao escuro";
        chao.transform.SetParent(parent);
        chao.transform.position = Vector3.zero;
        chao.transform.localScale = Vector3.one * 4f;
        chao.GetComponent<Renderer>().sharedMaterial = CorMaterial("Terra", new Color(.12f, .09f, .067f));
    }

    private static Material CorMaterial(string nome, Color cor)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var mat = new Material(shader);
        mat.name = nome;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", cor);
        else mat.color = cor;
        return mat;
    }

    private static void CriarLenha(Transform parent)
    {
        var madeira = CorMaterial("Madeira", new Color(.13f, .065f, .034f));
        var ponta = CorMaterial("Carvao", new Color(.043f, .034f, .03f));
        for (int i = 0; i < 9; i++)
        {
            float a = i * Mathf.PI * 2f / 9f;
            var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            g.name = "Tronco " + (i + 1).ToString("00");
            g.transform.SetParent(parent);
            g.transform.position = new Vector3(Mathf.Cos(a) * .25f, .145f, Mathf.Sin(a) * .25f);
            g.transform.rotation = Quaternion.Euler(80f, -a * Mathf.Rad2Deg, 10f);
            g.transform.localScale = new Vector3(.11f, .60f, .11f);
            g.GetComponent<Renderer>().sharedMaterial = i % 3 == 0 ? ponta : madeira;
        }
        for (int i = 0; i < 12; i++)
        {
            float a = i * Mathf.PI * 2f / 12f;
            var pedra = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pedra.name = "Pedra " + i;
            pedra.transform.SetParent(parent);
            pedra.transform.localPosition = new Vector3(Mathf.Cos(a) * .82f, .08f, Mathf.Sin(a) * .82f);
            pedra.transform.localScale = new Vector3(.31f, .14f, .28f);
            pedra.GetComponent<Renderer>().sharedMaterial = ponta;
        }
    }

    private static ParticleSystem NovoSistema(Transform parent, string nome, Material mat, int max, float emission, float durMin, float durMax,
        float velMin, float velMax, float tamMin, float tamMax, Color start, Color end, float ruido)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(parent);
        go.transform.localPosition = new Vector3(0f, .23f, 0f);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = max;
        main.startLifetime = new ParticleSystem.MinMaxCurve(durMin, durMax);
        main.startSpeed = new ParticleSystem.MinMaxCurve(velMin, velMax);
        main.startSize = new ParticleSystem.MinMaxCurve(tamMin, tamMax);
        main.startColor = start;
        main.startRotation = new ParticleSystem.MinMaxCurve(-.35f, .35f);
        main.gravityModifier = 0f;

        var emissor = ps.emission;
        emissor.rateOverTime = emission;
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.radius = .32f;
        shape.angle = 10f;
        shape.length = 0f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(start, 0f), new GradientColorKey(start, .25f), new GradientColorKey(end, 1f) },
            new[] { new GradientAlphaKey(.0f, 0f), new GradientAlphaKey(.95f, .12f), new GradientAlphaKey(.6f, .65f), new GradientAlphaKey(0f, 1f) });
        col.color = gradient;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        var curve = new AnimationCurve(new Keyframe(0f, .35f), new Keyframe(.18f, 1.0f), new Keyframe(.66f, .85f), new Keyframe(1f, .05f));
        size.size = new ParticleSystem.MinMaxCurve(1f, curve);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = ruido;
        noise.frequency = 1.12f;
        noise.scrollSpeed = .5f;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = mat;
        renderer.sortingOrder = 2;
        ps.Play();
        return ps;
    }

    private static void CriarParticulas(Transform parent, Material mat)
    {
        var chamas = NovoSistema(parent, "01 CHAMAS - amarelo laranja vermelho", mat, 700, 170, .40f, .95f,
            1.3f, 2.8f, .35f, .75f, new Color(1f, .92f, .42f), new Color(1f, .12f, .01f), .35f);
        var smoke = NovoSistema(parent, "02 FUMACA - ascendente", mat, 250, 31, 2.1f, 3.8f,
            .6f, 1.3f, .55f, 1.4f, new Color(.17f, .14f, .13f, .42f), new Color(.08f, .075f, .08f, 0f), .5f);
        smoke.transform.localPosition = new Vector3(0f, 1.0f, 0f);
        var smokeShape = smoke.shape;
        smokeShape.radius = .23f;
        smokeShape.angle = 11f;
        var sparks = NovoSistema(parent, "03 FAISCAS - pequenas fagulhas", mat, 160, 20, .7f, 1.6f,
            1.5f, 3.5f, .025f, .065f, new Color(1f, .86f, .34f), new Color(1f, .28f, .03f), .75f);
        var sparksShape = sparks.shape;
        sparksShape.angle = 20f;
        sparksShape.radius = .2f;
    }

    private static void CriarBrasas(Transform parent, Material mat)
    {
        var ps = NovoSistema(parent, "Brasa rente ao chao", mat, 180, 85, .22f, .46f,
            .02f, .19f, .06f, .18f, new Color(1f, .30f, .02f), new Color(.6f, .07f, .01f), .03f);
        ps.transform.localPosition = new Vector3(0f, .11f, 0f);
        var sh = ps.shape;
        sh.radius = .38f;
        sh.angle = 75f;
    }

    private static void GarantirPasta(string caminho)
    {
        if (AssetDatabase.IsValidFolder(caminho)) return;
        var parts = caminho.Split('/');
        var atual = parts[0];
        for (int i = 1; i < parts.Length; ++i)
        {
            var seguinte = atual + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(seguinte))
                AssetDatabase.CreateFolder(atual, parts[i]);
            atual = seguinte;
        }
    }
}
#endif

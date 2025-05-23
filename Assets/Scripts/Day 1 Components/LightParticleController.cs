using UnityEngine;

public class LightParticleController : MonoBehaviour
{
    public struct LightParticle
    {
        public Vector3 rotationMatRow1;
        public Vector3 rotationMatRow2;
        public Vector3 rotationMatRow3;
        public Vector3 position;
        public Vector3 velocity;
        public float theta;
        public float alpha;
    }

    const int PARTICLE_SIZE = 17 * sizeof(float);

    [Header("Intial Values Properties")]
    public int particleCount = 500;

    // Define the spawn bounds of a particle.
    public int spawnBoundsX = 100;
    public int spawnBoundsY = 100;
    public float driftSpeed = 10;
    public float followSpeed = 100f;
    public int particleSize = 10;
    public float orbitSize = 30.0f;
    public float captureRadius = 30.0f;
    public Color particleColor = Color.white;

    [Header("Materials and Shader Properties")]
    public Material particleVertAndFrag;
    public ComputeShader compShader;

    [Header("Player")]
    public Transform playerTransform;

    protected int kernalID;
    protected int spawnKernalID;
    protected ComputeBuffer particleBuffer;
    protected int groupSizeX;
    protected Bounds renderBounds;
    protected LightParticle[] particleArray;

    // Test Variables (Testing spawning of particles)
    protected bool spawningParticles = false;
    protected float spawnTime = 1f;
    protected float internalTimer = 0;

    // Debug/Refactor variables.
    protected int maxSpawnDispatcher = 30;
    protected int spawnCount = 0;

    // Start is called before the first frame update
    void Start()
    {
        particleVertAndFrag.enableInstancing = true;
        IntializeParticles();
        InitializeBuffer();
        InitializeRenderParams();

        particleVertAndFrag.SetVector("cameraRight", Camera.main.transform.right);
        particleVertAndFrag.SetVector("cameraUp", Camera.main.transform.up);
        particleVertAndFrag.SetVector("cameraForward", Camera.main.transform.forward);
    }

    protected void IntializeParticles()
    {
        particleArray = new LightParticle[particleCount];

        for (int i = 0; i < particleCount; i++)
        {
            particleArray[i].position.x = 0;
            particleArray[i].position.y = 0;
            particleArray[i].position.z = 0;

            particleArray[i].velocity.x = 0;
            particleArray[i].velocity.y = 0;
            particleArray[i].velocity.z = 0;

            particleArray[i].rotationMatRow1.x = 0;
            particleArray[i].rotationMatRow1.y = 0;
            particleArray[i].rotationMatRow1.z = 0;

            particleArray[i].rotationMatRow2.x = 0;
            particleArray[i].rotationMatRow2.y = 0;
            particleArray[i].rotationMatRow2.z = 0;

            particleArray[i].rotationMatRow3.x = 0;
            particleArray[i].rotationMatRow3.y = 0;
            particleArray[i].rotationMatRow3.z = 0;

            particleArray[i].theta = 0;
            particleArray[i].alpha = 0;
        }
    }

    protected void InitializeBuffer()
    {
        particleBuffer = new ComputeBuffer(particleCount, PARTICLE_SIZE);
        particleBuffer.SetData(particleArray);

        kernalID = compShader.FindKernel("CSParticleMove");
        spawnKernalID = compShader.FindKernel("SpawnParticleSet");

        uint threadX;
        compShader.GetKernelThreadGroupSizes(kernalID, out threadX, out _, out _);
        groupSizeX = Mathf.CeilToInt((float)particleCount / (float)threadX);

        compShader.SetBuffer(kernalID, "particleBuffer", particleBuffer);
        compShader.SetBuffer(spawnKernalID, "particleBuffer", particleBuffer);
        compShader.SetFloat("driftSpeed", driftSpeed);
        compShader.SetFloat("followSpeed", followSpeed);
        compShader.SetFloat("halfBoundsX", (float)spawnBoundsX / 2);
        compShader.SetFloat("halfBoundsY", (float)spawnBoundsY / 2);
        compShader.SetFloat("orbitSize", orbitSize);
        compShader.SetVector("playerLoc", playerTransform.position);
        compShader.SetFloat("captureRadius", captureRadius);
        compShader.SetInt("randomSeed", (int)Random.Range(0, 9999));
        compShader.SetInt("particleCount", particleCount);
        compShader.SetInt("spawnBurstAmount", 10);

        particleVertAndFrag.SetBuffer("particleBuffer", particleBuffer);
        particleVertAndFrag.SetFloat("particleSize", (float)particleSize);
        particleVertAndFrag.SetVector("_Color", particleColor);
    }

    protected void InitializeRenderParams()
    {
        renderBounds = new Bounds(Vector3.zero, 10000 * Vector3.one);
    }

    void OnDestroy()
    {
        if (particleBuffer != null)
        {
            particleBuffer.Release();
        }
    }

    // Update is called once per frame
    void Update()
    {
        compShader.SetFloat("deltaTime", Time.deltaTime);
        compShader.SetVector("playerLoc", playerTransform.position);

        if (spawnCount < maxSpawnDispatcher)
        {
            if (internalTimer >= spawnTime)
            {
                DispathSpawn();
            }
            else
            {
                internalTimer += Time.deltaTime;
            }
        }

        compShader.Dispatch(kernalID, groupSizeX, 1, 1);

        particleVertAndFrag.SetVector("cameraRight", Camera.main.transform.right);
        particleVertAndFrag.SetVector("cameraUp", Camera.main.transform.up);
        particleVertAndFrag.SetVector("cameraForward", Camera.main.transform.forward);

        Graphics.DrawProcedural(
            particleVertAndFrag,
            renderBounds,
            MeshTopology.Triangles,
            particleCount * 6
        );
    }

    void DispathSpawn() {
        compShader.SetInt("groupID", spawnCount);
        compShader.Dispatch(spawnKernalID, groupSizeX, 1, 1);
        internalTimer = 0;
        spawnCount++;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one * 10000f);

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(spawnBoundsX, spawnBoundsY, 0));
    }
}

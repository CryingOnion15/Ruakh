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

    [Header("Spawning Properties")]
    public int spawnBurstAmount = 10;
    public float burstRate = 1f;

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

    /*** Spawning Variables ***/
    protected float internalSpawnBurstTimer = 0;

    // Maximum amount of time the spawn burst need to happen. Calculated.
    protected int maxSpawnDispatcher = 0;

    // How many times spawn has been dispatched.
    protected int spawnCount = 0;

    /*** Debug Variables ***/
    protected Vector3 NOORBITPOS = Vector3.one * -10000;

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

        maxSpawnDispatcher = Mathf.CeilToInt(particleCount / spawnBurstAmount);
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
        compShader.SetFloat("captureRadius", captureRadius);
        compShader.SetInt("randomSeed", (int)Random.Range(0, 9999));
        compShader.SetInt("particleCount", particleCount);
        compShader.SetInt("spawnBurstAmount", spawnBurstAmount);
        SetPlayerLoc();

        particleVertAndFrag.SetBuffer("particleBuffer", particleBuffer);
        particleVertAndFrag.SetFloat("particleSize", (float)particleSize);
        particleVertAndFrag.SetVector("_Color", particleColor);
    }

    protected void InitializeRenderParams()
    {
        renderBounds = new Bounds(Vector3.zero, 10000 * Vector3.one);
    }

    void SetPlayerLoc()
    {
        if (playerTransform.gameObject.activeSelf)
        {
            compShader.SetVector("playerLoc", playerTransform.position);
        }
        else
        {
            compShader.SetVector("playerLoc", NOORBITPOS);
        }
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
        SetPlayerLoc();

        if (spawnCount < maxSpawnDispatcher)
        {
            checkSpawnDispatch();
        }

        compShader.Dispatch(kernalID, groupSizeX, 1, 1);

        drawParticles();
    }

    protected void checkSpawnDispatch()
    {
        if (internalSpawnBurstTimer >= burstRate)
        {
            DispatchSpawn();
        }
        else
        {
            internalSpawnBurstTimer += Time.deltaTime;
        }
    }

    protected void updateCameraProperties()
    {
        particleVertAndFrag.SetVector("cameraRight", Camera.main.transform.right);
        particleVertAndFrag.SetVector("cameraUp", Camera.main.transform.up);
        particleVertAndFrag.SetVector("cameraForward", Camera.main.transform.forward);
    }

    protected void drawParticles()
    {
        Graphics.DrawProcedural(
            particleVertAndFrag,
            renderBounds,
            MeshTopology.Triangles,
            particleCount * 6
        );
    }

    void DispatchSpawn()
    {
        compShader.SetInt("groupIndex", spawnCount);
        compShader.Dispatch(spawnKernalID, groupSizeX, 1, 1);
        internalSpawnBurstTimer = 0;
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

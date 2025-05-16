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
        public float life;
    }

    const int PARTICLE_SIZE = 17 * sizeof(float);

    [Header("Intial Values Properties")]
    public int particleCount = 500;

    // Define the spawn bounds of a particle.
    public int spawnBoundsX = 100;
    public int spawnBoundsY = 100;
    public int driftSpeed = 10;
    public int particleSize = 10;
    public float orbitSize = 30.0f;
    public Color particleColor = Color.white;

    [Header("Materials and Shader Properties")]
    public Material particleVertAndFrag;
    public ComputeShader compShader;

    protected int kernalID;
    protected ComputeBuffer particleBuffer;
    protected int groupSizeX;
    protected Bounds renderBounds;
    protected LightParticle[] particleArray;

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
            float x = Random.value * spawnBoundsX - (spawnBoundsX / 2);
            float y = Random.value * spawnBoundsY - (spawnBoundsY / 2);
            Vector3 xyz = new Vector3(x, y, 0);

            particleArray[i].position.x = xyz.x;
            particleArray[i].position.y = xyz.y;
            particleArray[i].position.z = xyz.z;

            float velTheta = Random.value * 2 * Mathf.PI;

            particleArray[i].velocity.x = Mathf.Cos(velTheta);
            particleArray[i].velocity.y = Mathf.Sin(velTheta);
            particleArray[i].velocity.z = 0;

            float xTheta = Random.value * 2 * Mathf.PI;
            float yTheta = Random.value * 2 * Mathf.PI;
            float zTheta = Random.value * 2 * Mathf.PI;

            float cosx = Mathf.Cos(xTheta);
            float sinx = Mathf.Sin(xTheta);
            float cosy = Mathf.Cos(yTheta);
            float siny = Mathf.Sin(yTheta);
            float cosz = Mathf.Cos(zTheta);
            float sinz = Mathf.Sin(zTheta);

            particleArray[i].rotationMatRow1.x = cosy * cosz;
            particleArray[i].rotationMatRow1.y = cosy * -sinz;
            particleArray[i].rotationMatRow1.z = siny;

            particleArray[i].rotationMatRow2.x = cosx * sinz;
            particleArray[i].rotationMatRow2.y = cosx * cosz;
            particleArray[i].rotationMatRow2.z = -sinx * cosy;

            particleArray[i].rotationMatRow3.x = cosx * -siny * cosz + sinx * sinz;
            particleArray[i].rotationMatRow3.y = cosx * -siny * -sinz + sinx * cosz;
            particleArray[i].rotationMatRow3.z = cosx * cosy;
            
            particleArray[i].theta = Random.value * 2 * Mathf.PI;

            // Initial life value
            particleArray[i].life = 10;
        }
    }

    protected void InitializeBuffer()
    {
        particleBuffer = new ComputeBuffer(particleCount, PARTICLE_SIZE);
        particleBuffer.SetData(particleArray);

        kernalID = compShader.FindKernel("CSParticleMove");

        uint threadX;
        compShader.GetKernelThreadGroupSizes(kernalID, out threadX, out _, out _);
        groupSizeX = Mathf.CeilToInt((float)particleCount / (float)threadX);

        compShader.SetBuffer(kernalID, "particleBuffer", particleBuffer);
        compShader.SetFloat("driftSpeed", (float)driftSpeed);
        compShader.SetFloat("halfBoundsX", (float)spawnBoundsX / 2);
        compShader.SetFloat("halfBoundsY", (float)spawnBoundsY / 2);
        compShader.SetFloat("orbitSize", orbitSize);
        compShader.SetVector("playerLoc", transform.position);
        compShader.SetFloat("captureRadius", orbitSize + 50f);

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

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one * 10000f);

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(spawnBoundsX, spawnBoundsY, 0));
    }
}

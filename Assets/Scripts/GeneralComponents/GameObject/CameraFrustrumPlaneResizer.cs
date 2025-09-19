using UnityEngine;

[ExecuteInEditMode]
public class CameraFrustrumPlaneResizer : MonoBehaviour
{
    public Camera cameraRef;
    public float planeUnits = 10;

    protected float cameraDistance;
    protected float frustrumHeight;
    protected float frustrumWidth;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    //     void Start()
    //     {
    // #if UNITY_EDITOR
    //         Vector3 direction = transform.position - cameraRef.transform.position;
    //         cameraDistance = Vector3.Dot(cameraRef.transform.forward, direction);
    //         frustrumHeight =
    //             2.0f * cameraDistance * Mathf.Tan(cameraRef.fieldOfView * 0.5f * Mathf.Deg2Rad);
    //         frustrumWidth = frustrumHeight * cameraRef.aspect;

    //         transform.localScale = new Vector3(frustrumWidth / 2, frustrumHeight / 2, 1f);
    // #endif
    //     }

    void OnEnable()
    {
#if UNITY_EDITOR
        if (cameraRef)
        {
            Vector3 direction = transform.position - cameraRef.transform.position;
            cameraDistance = Vector3.Dot(cameraRef.transform.forward, direction);
            frustrumHeight =
                2.0f * cameraDistance * Mathf.Tan(cameraRef.fieldOfView * 0.5f * Mathf.Deg2Rad);
            frustrumWidth = frustrumHeight * cameraRef.aspect;

            transform.localScale = new Vector3(
                frustrumWidth / planeUnits,
                1f,
                frustrumHeight / planeUnits
            );
        }
#endif
    }
}

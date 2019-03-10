using UnityEngine;

/*
 * A Unity component class for controlling the camera and how it reacts to the player's movement while following the player
 */
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Camera Variables")]

    [SerializeField] [Tooltip("Regardless of velocity, how far should the camera look ahead")]
    private float lookAheadConstant = 0.3f;
    [SerializeField] [Tooltip("How much the velocity should influence camera look ahead")]
    private float lookAheadVelocityInfluence = 0.0125f;
    [SerializeField] [Tooltip("How fast does the camera interpolate to the desired position")]
    private float lookAheadChangeSpeed = 5f;


    [Header("Camera Variables")]

    [SerializeField] [Tooltip("How much velocity affects the cameras Z position")]
    private float playerVelocityZInfluence = 0.25f;
    [SerializeField] [Tooltip("How quickly the cameras Z position will move to the desired position")]
    private float zChangeSpeed = 0.2f;
    [SerializeField] [Tooltip("How much outside influence can change the fov from its default value")]
    private Vector2 fovAddLimits = new Vector2(0f, 5f);


    private Camera camera;
    // Currently how far ahead the camera is looking past the player trowards the mouse
    private float currentCameraLookAhead = 0f;
    private float defaultZPosition = 0f;
    private float defaultFOV = 0f;


    /*
     * Executed on start of scene containing gameobject with this CameraController Component
     * Used to intialize the camera, z position, and default field of view
     */
    private void Start ()
    {
        camera = GetComponent<Camera>();
        defaultZPosition = transform.localPosition.z;
        defaultFOV = camera.fieldOfView;
    }


    /*
     * Executed on every update of the game loop
     * Sets the cameras position and field of view
     */
    private void Update ()
    {
        SetPosition ();
        SetFieldOfView ();
    }


    /*
     * Set the cameras position based on the games current state
     */
    private void SetPosition ()
    {
        Vector3 playerVelocity = GameManager.Instance().GetPlayerVelocity();

        currentCameraLookAhead = Mathf.Lerp (
            currentCameraLookAhead,
            lookAheadConstant + playerVelocity.magnitude * lookAheadVelocityInfluence,
            Time.deltaTime * lookAheadChangeSpeed
        );

        Vector3 playerDirection = GetMouseTarget() - GameManager.Instance().GetPlayerPosition();
        playerDirection.Normalize();
        
        // Get a position the camera should be at, but don't overwrite current values yet
        Vector3 goalPosition = (GameManager.Instance().GetPlayerPosition() * 2f + playerDirection * currentCameraLookAhead) / 2f;

        goalPosition.z = Mathf.Lerp(
            transform.localPosition.z,
            (playerVelocity.magnitude * playerVelocityZInfluence) * Mathf.Sign(defaultZPosition) + defaultZPosition,
            Time.deltaTime * zChangeSpeed
        );

        transform.localPosition = goalPosition;
    }


    /*
     * Set the cameras field of view based on the games current state
     */
    private void SetFieldOfView ()
    {
        Vector3 playerVelocity = GameManager.Instance().GetPlayerVelocity();
        
        camera.fieldOfView = Mathf.Lerp(
            camera.fieldOfView,
            defaultFOV + Mathf.Clamp(playerVelocity.magnitude * playerVelocityZInfluence, fovAddLimits.x, fovAddLimits.y),
            Time.deltaTime
        );
    }


    /*
     * Get the world position of the mouse and return it
     */
    public Vector3 GetMouseTarget ()
    {
        Vector3 target = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -Camera.main.transform.position.z));
        target.z = 0f;
        return target;
    }

}

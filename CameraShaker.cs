using System.Collections.Generic;
using UnityEngine;

/*
 * A singleton class for handling camera shaking
 */
public class CameraShaker : MonoBehaviour {

	/*
	 * An element of cameraShakes, used to store current values of this shake to be performed on the camera
	 */
	private class CameraShakeProcess {
		public CameraShakeProcess(Vector2 pDirection, float pIntensity, float pDeltaTimeMultiplier, float startTime) {
			direction = pDirection;
			intensity = pIntensity;
			deltaTimeMultiplier = pDeltaTimeMultiplier;
			currentTime = startTime;
		}
		public Vector2 direction = Vector2.zero;
		public float currentTime = 0f;
		public float intensity = 0f;
		public float deltaTimeMultiplier = 1f;
	}


	[HideInInspector]
	public static CameraShaker instance = null;

    [SerializeField] [Tooltip("Animation curve representing screenshake offset distance (y axis) over time (x axis)")]
	private AnimationCurve offsetBounce = null;
    [SerializeField] [Tooltip("How much velocity affects the cameras Z position")]
	private float maxShake = 2f;

	private List<CameraShakeProcess> cameraShakes; // List of shakes the camera is currently performing


    /*
     * Returns the CameraShaker singleton instance
     */
	public static CameraShaker Instance()
	{
		return instance;
	}


    /*
     * Executed on start of scene containing gameobject with this CameraController Component
     * Used to intialize the camera, z position, and default field of view
     */
	private void Start()
	{
		instance = this;
		cameraShakes = new List<CameraShakeProcess>();

#if UNITY_EDITOR
		bool foundCamera = false;
		for (int i = 0; i < transform.childCount; i++)
		{
			if (transform.GetChild(i).GetComponent<Camera>() != null)
			{
				foundCamera = true;
				break;
			}
		}

		if (!foundCamera)
		{
			Debug.LogError("CameraShaker \"" + gameObject.name + "\" does not have any children with a CameraController!");
		}
#endif

	}


    /*
     * Executed on every update of the game loop
     * Handles any available shake and set the position of the shaker accordingly
     */
	private void Update ()
	{
		transform.localPosition = Vector3.zero;

		// Reverse looping allows in loop removal without affecting the index
		for (int i = cameraShakes.Count - 1; i >= 0; i--)
		{
			cameraShakes[i].currentTime += Time.deltaTime * cameraShakes[i].deltaTimeMultiplier;

			if (cameraShakes[i].currentTime > 0f)
			{
				float offsetBounceValue = offsetBounce.Evaluate(cameraShakes[i].currentTime);

				transform.localPosition += new Vector3(
					cameraShakes[i].direction.x * cameraShakes[i].intensity * offsetBounceValue,
					cameraShakes[i].direction.y * cameraShakes[i].intensity * offsetBounceValue,
					0f
				);

				if (offsetBounce.keys.Length > 0 && cameraShakes[i].currentTime > offsetBounce.keys[offsetBounce.keys.Length - 1].time)
				{
					cameraShakes.RemoveAt(i);
				}
			}
		}

		// Limit magnitude
		if (transform.localPosition.magnitude > maxShake)
		{
			transform.localPosition = transform.localPosition.normalized * maxShake;
		}
	}


	/*
     * Add a new shake to the camera.
     * 
     * @param Vector2 direction - Direction for the camera to shake in
     * @param float intensity - How intense the shake is in meters
     * @param float deltaTimeMultiplier - Deltatime to be used which affects the offsetBounce animation curve
     * @param float startTime - Local starting time for when the shake will occur (defaults to 0, starting immediately)
     */
	public void AddCameraShake(Vector2 direction, float intensity = 1f, float deltaTimeMultiplier = 1f, float startTime = 0f) {
		cameraShakes.Add(new CameraShakeProcess(direction.normalized, intensity, deltaTimeMultiplier, -startTime));
	}

}

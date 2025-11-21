using UnityEngine;

public class OffsetFlashLight : MonoBehaviour
{
    private Vector3 offsetVector3;
    public GameObject followCam;

    [SerializeField] private float moveSpeed = 13f;

    public Light flashLight;

    private bool flashLightIsOn = false;

    //audio

    public AudioSource audioSource; //click da lanterna

    public AudioClip flash_Light_onSound;
    public AudioClip flash_Light_offSound;

    //audio

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        flashLight.enabled = false;
        offsetVector3 = transform.position - followCam.transform.position;
    }

    // Update is called once per frame
    void Update()
    {

        transform.position = followCam.transform.position + offsetVector3;

        transform.rotation = Quaternion.Slerp(transform.rotation, followCam.transform.rotation, moveSpeed * Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.F) || Input.GetMouseButtonDown(1))
        {
            if (flashLightIsOn) audioSource.PlayOneShot(flash_Light_offSound);
            else audioSource.PlayOneShot(flash_Light_onSound);
            
            flashLight.enabled = !flashLightIsOn;
            flashLightIsOn = !flashLightIsOn;
        }
        
    }
}

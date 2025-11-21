using UnityEngine;
[ExecuteInEditMode]
public class FPS_cap : MonoBehaviour
{
    [SerializeField] private int frame_rate = 60;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        #if UNITY_EDITOR
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = frame_rate;
        #endif
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

using UnityEngine;
using UnityEngine.UI;

public class CameraMgr : MonoBehaviour
{
    Camera spiritCam;
    Camera realCam;
    public RenderTexture miniMapTexture;
    public RectTransform map;
    public GameObject SizeButtons;

    [SerializeField]
    private float mapScaleF = 0.2f;

    private Vector3 mapScaleV;

    [SerializeField]
    private float treshold = 2.0f;

    private Vector3 mapOGPos;

    private bool minimapOn = true;

    private bool isSpiritCam = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        spiritCam = GameObject.FindGameObjectWithTag("SpiritCamera").GetComponent<Camera>();
        realCam = GameObject.FindGameObjectWithTag("RealCamera").GetComponent<Camera>();

        mapScaleV = new Vector3(mapScaleF, mapScaleF, 0.0f);
        mapOGPos = map.position;

        realCam.forceIntoRenderTexture = true;
        realCam.targetTexture = miniMapTexture;

        GameMgr.OnPosession += ChangeCamera;
    }

    private void OnDestroy()
    {
        GameMgr.OnPosession -= ChangeCamera;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnPlusButton()
    {
        Debug.Log("Plus button");
        if (map.localScale.x + mapScaleV.x <= treshold)
        {
            map.localScale += mapScaleV;
            map.position += new Vector3(mapScaleF, -mapScaleF, 0.0f) * 2.0f;
        }
        else
        {
            map.localScale = new Vector3(treshold, treshold, 1);
        }

    }

    public void OnMinusButton()
    {
        Debug.Log("Minus button");
        if (map.localScale.x - mapScaleV.x >= 1.0f)
        {
            map.localScale -= mapScaleV;
            map.position -= new Vector3(-mapScaleF, mapScaleF, 0.0f) * 2.0f;
        }
        else
        {
            map.localScale = new Vector3(1, 1, 1);
            map.position = mapOGPos;
        }
    }

    public void OnVisualButton()
    {
        if (minimapOn)
        {
            Debug.Log("Slipt screen mode");
            realCam.rect = new Rect(0, 0, 0.5f, 1.0f);
            spiritCam.rect = new Rect(0.5f, 0, 0.5f, 1.0f);

            realCam.forceIntoRenderTexture = false;
            realCam.targetTexture = null;
            spiritCam.forceIntoRenderTexture = false;
            spiritCam.targetTexture = null;

            minimapOn = false;
            map.gameObject.SetActive(false);
            SizeButtons.SetActive(false);
        }
        else
        {
            Debug.Log("Minimap mode");
            if (isSpiritCam)
            {
                Debug.Log("rendering spirit cam");
                realCam.forceIntoRenderTexture = true;
                realCam.targetTexture = miniMapTexture;
                spiritCam.forceIntoRenderTexture = false;
                spiritCam.targetTexture = null;
            }
            else
            {
                Debug.Log("rendering real cam");
                spiritCam.forceIntoRenderTexture = true;
                spiritCam.targetTexture = miniMapTexture;
                realCam.forceIntoRenderTexture = false;
                realCam.targetTexture = null;
            }

            realCam.rect = new Rect(0, 0, 1.0f, 1.0f);
            spiritCam.rect = new Rect(0, 0, 1.0f, 1.0f);

            minimapOn = true;
            map.gameObject.SetActive(true);
            SizeButtons.SetActive(true);

        }
    }


    private void OnPreRender()
    {
        Camera.current.clearFlags = CameraClearFlags.SolidColor;
    }

    private void ChangeCamera()
    {
        Debug.Log("Camera change invoked");
        //spiritCam.enabled = !spiritCam.enabled;
        //realCam.enabled = !realCam.enabled;

        if (minimapOn)
        {
            // Minimap mode
            if (isSpiritCam)
            {
                realCam.forceIntoRenderTexture = false;
                realCam.targetTexture = null;
                spiritCam.forceIntoRenderTexture = true;
                spiritCam.targetTexture = miniMapTexture;

                isSpiritCam = false;
            }
            else
            {
                spiritCam.forceIntoRenderTexture = false;
                spiritCam.targetTexture = null;
                realCam.forceIntoRenderTexture = true;
                realCam.targetTexture = miniMapTexture;

                isSpiritCam = true;
            }
        }
        else
        {
            // Split screen mode
            isSpiritCam = !isSpiritCam;
        }


    }


}

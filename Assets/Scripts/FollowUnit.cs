using UnityEngine;

public class FollowUnit : MonoBehaviour
{
    public AudioClip MusicClip;
    public AudioSource MusicSource;

    private MapManager mapscript;

    public Transform target;
    public float smoothSpeed = 0.2f;
    public Vector3 offset;

    //Control variable
    private int turn = 0;
    private float Camerarotation = 0;

    private void Start()
    {
        GameObject Map = new GameObject();
        Map.AddComponent<MapManager>();
        Map.name = "Map";
        mapscript = GameObject.Find("Map").GetComponent<MapManager>();

        GameObject.Find("Main Camera").transform.eulerAngles = new Vector3(50, 0, 0);

        MusicSetUp();
    }

    private void Update()
    {
        CkeckCameraSettings();
    }


    private void LateUpdate()
    {
        if (mapscript.SelectedUnit == null)
        {
            if (mapscript.selectionX >= 0 && mapscript.selectionY >= 0)
            {
                target = MapManager.Casillas[mapscript.selectionX, mapscript.selectionY].Quad.transform;

                Vector3 desiredPosition = target.position + offset;
                Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
                transform.position = smoothedPosition;
            }
        }

    }

    //Gets the apropiate camera offset depending on the turn and its rotation
    private void CkeckCameraSettings()
    {
        if (turn != mapscript.PlayerTurn || Camerarotation != Camera.main.transform.eulerAngles.y)
        {
            turn = mapscript.PlayerTurn;
            Camerarotation = Camera.main.transform.eulerAngles.y;
            switch (mapscript.PlayerTurn)
            {
                case 1:
                    if (Camerarotation == 0.0f)
                        offset = new Vector3(0, 4, -3);
                    else
                        offset = new Vector3(0, 4, 3);
                    break;
                case 2:
                    if (Camerarotation == 0.0f)
                        offset = new Vector3(0, 4, -3);
                    else
                        offset = new Vector3(0, 4, 3);
                    break;
                default:
                    break;
            }
        }
    }

    //Sets the AudioSource
    private void MusicSetUp()
    {
        GameObject.Find("Main Camera").AddComponent<AudioSource>();

        string mystring = "Jobs Done";
        MusicClip = (AudioClip)Resources.Load(mystring, typeof(AudioClip));

        MusicSource = GameObject.Find("Main Camera").GetComponent<AudioSource>();

        MusicSource.clip = MusicClip;
    }

}

using UnityEngine;

public class SunLights : MonoBehaviour {

    static float timeCounter = 0;

    public float speed;
    public float width;
    public float height;

    public Vector3 offset;

    public bool continues = true;
    

    public static void SetTimeCounter(float time)
    {
        timeCounter = time;
    }

    // Use this for initialization
    void Start () {
        
        speed = 0.05f;
        width = (MapManager.MAP_WIDTH + 2*MapManager.lateralEdge + 2*MapManager.cubeHeight + 2*MapManager.turnCubeHeight)/2;
        height = (MapManager.MAP_HEIGHT + 2*MapManager.lateralEdge + 2*MapManager.cubeHeight + 2*MapManager.turnCubeHeight) / 2;
        offset = new Vector3(MapManager.MAP_WIDTH/2, 0, MapManager.MAP_HEIGHT/2);

        transform.GetChild(0).GetComponent<Light>().range = Mathf.Max(width, height) + 5;
        transform.GetChild(0).GetComponent<Light>().intensity = (transform.GetChild(0).GetComponent<Light>().range / 2)*0.6f;

        transform.GetComponent<Renderer>().material = Resources.Load("Sun", typeof(Material)) as Material;

    }
	
	// Update is called once per frame
	void Update () {
        timeCounter += Time.deltaTime * speed;

        float x = width*Mathf.Cos(timeCounter);
        float y = height*Mathf.Sin(timeCounter);
        float z = 0;

        float xRot = Mathf.Acos(x/width);
        xRot = xRot * Mathf.Rad2Deg;

        transform.position = new Vector3(x, y, z) + offset;
        transform.GetChild(0).transform.eulerAngles = new Vector3(xRot, -90, 0);


        float var = MapManager.MAP_WIDTH / 2;

        if ((transform.position.x < var) && continues)
        {
            continues = false;
            MapManager.changeTurn = true;
        }


        if(y < 0)
        {
            continues = true;
            MapManager.changeTurn = true;
        }
		
	}

    
}

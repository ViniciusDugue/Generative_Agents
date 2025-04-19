using UnityEngine;

public class FoodScript : MonoBehaviour
{
    public bool respawn;
    public FoodSpawner myArea; //FoodCollectorArea This was the previous type

    public void OnEaten()
    {
        if (respawn)
        {
            transform.position = new Vector3(Random.Range(-myArea.range, myArea.range),
                3f,
                Random.Range(-myArea.range, myArea.range)) + myArea.transform.position;
        }
        else
        {
            Destroy(this.gameObject);
        }
        Debug.Log("Food Respawned.");
    }
}

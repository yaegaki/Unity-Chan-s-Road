using System.Collections;
using UnityEngine;

public class CoinController : MonoBehaviour
{
    public AudioClip getCoin;
    static int count = 0;
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Player")
        {
            StageController.Instance.AddCoin();
            AudioSourceController.instance.PlayOneShot(getCoin);
            Destroy(gameObject);
        }
    }
}

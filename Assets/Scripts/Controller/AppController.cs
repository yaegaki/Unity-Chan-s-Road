using UnityEngine;
using System.Collections;
using UniLinq;
using UniRx;

public class AppController : ObservableMonoBehaviour
{
    private static AppController instance;
    public static AppController Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject();
                go.name = "App";
                instance = go.AddComponent<AppController>();
            }
            return instance;
        }
    }
    public override void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
        base.Awake();
    }

    public static bool IsTitle
    {
        get
        {
            return Application.loadedLevelName == "Title";
        }
    }

    public int Coin
    {
        get;
        private set;
    }

    public int Damage
    {
        get;
        private set;
    }

    public int UsedInk
    {
        get;
        private set;
    }


    public void SetResult(int coin, int damage, int usedInk)
    {
        this.Coin = coin;
        this.Damage = damage;
        this.UsedInk = usedInk;
    }
}

using UnityEngine;
using System;
using System.Collections;
using UniLinq;
using UniRx;

public class StageController : ObservableMonoBehaviour
{
    public static StageController Instance
    {
        get;
        private set;
    }

    [SerializeField]
    private Transform stageClearPoint;

    [SerializeField]
    private GameObject unityChan;

    [SerializeField]
    private UnityEngine.UI.Text coinNumberText;

    [SerializeField]
    private UnityEngine.UI.Text damageNumberText;

    [SerializeField]
    private UnityEngine.UI.Text inkNumberText;

    public bool IsCleared
    {
        get;
        private set;
    }

    public override void Awake()
    {
        StageController.Instance = this;
        var go = GameObject.Find("GameRoot");
        RoadManager roadManager = null;
        if (go != null)
        {
            roadManager = go.GetComponent<RoadManager>();
            if (roadManager != null)
            {
                UpdateInk(roadManager.Ink);
            }
        }

        this.UpdateAsObservable()
            .Where(_ => this.unityChan.transform.position.x >= this.stageClearPoint.position.x)
            .First()
            .Subscribe(_ =>
            {
                this.IsCleared = true;
                Observable.Timer(TimeSpan.FromSeconds(2f))
                    .Subscribe(__ =>
                    {
                        AppController.Instance.SetResult(this.Coin, this.Damage, roadManager.UsedInk);
                        Application.LoadLevel("Result");
                    });
            });

        this.Coin = 0;

        base.Awake();
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

    public void AddCoin()
    {
        this.Coin++;
        coinNumberText.text = string.Format("{0:000}", this.Coin);
    }

    public void AddDamegeCount()
    {
        this.Damage++;
        damageNumberText.text = string.Format("{0:000}", this.Damage);
    }

    public void UpdateInk(int ink)
    {
        this.inkNumberText.text = string.Format("{0:000}", ink);
    }
}

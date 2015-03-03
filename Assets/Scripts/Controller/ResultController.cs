using UnityEngine;
using System.Collections;
using System;
using UniRx;
using UniLinq;

public class ResultController : ObservableMonoBehaviour
{
    [SerializeField]
    private UnityEngine.UI.Text coinNumberText;

    [SerializeField]
    private UnityEngine.UI.Text damageNumberText;

    [SerializeField]
    private UnityEngine.UI.Text usedInkNumberText;

    public override void Awake()
    {
        var app = AppController.Instance;
        this.coinNumberText.text = string.Format("{0:000}", app.Coin);
        this.damageNumberText.text = string.Format("{0:000}", app.Damage);
        this.usedInkNumberText.text = string.Format("{0:000}", app.UsedInk);

        var resources = new CompositeDisposable();
        Action<Vector3, IObservable<Vector3>> startAction = (before, stream) =>
        {
            stream.First()
                .Subscribe(after =>
                {
                    if (Vector3.Distance(before, after) <= 0.3f)
                    {
                        resources.Dispose();
                        Application.LoadLevel("Title");
                    }
                });
        };

        Drag.MouseDownStream()
            .Subscribe(before =>
            {
                startAction(before, Drag.MouseUpStream());
            })
            .AddTo(resources);

        Drag.TouchStartStream()
            .Subscribe(beforeTouch =>
            {
                var before = Camera.main.ScreenToWorldPoint(beforeTouch.Item1.position);
                startAction(before, Drag.TouchUpStream(beforeTouch.Item1.fingerId).Select(touch => Camera.main.ScreenToWorldPoint(touch.Item1.position)));
            })
            .AddTo(resources);

        base.Awake();
    }
}

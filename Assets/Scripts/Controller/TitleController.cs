using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UniLinq;
using UniRx;

public class TitleController : ObservableMonoBehaviour
{
    [SerializeField]
    private GameObject unityChan;

    [SerializeField]
    private AudioClip startClip;

    [SerializeField]
    private UnityEngine.UI.Text inputLabel;

    private CompositeDisposable eventResources = new CompositeDisposable();
    public override void Awake()
    {
        this.LateUpdateAsObservable()
            .Subscribe(_ =>
            {
                var viewPosition = Camera.main.WorldToViewportPoint(this.unityChan.transform.position);
                float? afterX = null;
                float? afterY = null;
                if (viewPosition.x > 1.1f)
                {
                    afterX = Camera.main.ViewportToWorldPoint(new Vector3(-0.1f, 0f)).x;
                }
                else if (viewPosition.x < -0.1f)
                {
                    afterX = Camera.main.ViewportToWorldPoint(new Vector3(1.1f, 0f)).x;
                }

                if (viewPosition.y < 0f)
                {
                    afterY = Camera.main.ViewportToWorldPoint(new Vector3(0f, 1.1f)).y;
                }

                if (afterX != null || afterY != null)
                {
                    var pos = this.unityChan.transform.position;
                    pos.x = afterX.HasValue ? afterX.Value : pos.x;
                    pos.y = afterY.HasValue ? afterY.Value : pos.y;
                    this.unityChan.transform.position = pos;
                }
            })
            .AddTo(this.eventResources);

        if (Input.touchSupported)
        {
            this.inputLabel.text = "Please Touch Screen!";
        }
        else
        {
            this.inputLabel.text = "Please Click Screen!";
        }

        var animator = this.inputLabel.GetComponent<Animator>();
        Action<Vector3, IObservable<Vector3>> startAction = (before, stream) =>
        {
            stream.First()
                .Subscribe(after =>
                {
                    if (Vector3.Distance(before, after) <= 0.3f)
                    {
                        this.eventResources.Dispose();
                        animator.SetTrigger("flash");
                        AudioSourceController.instance.PlayOneShot(this.startClip);
                        Observable.Timer(TimeSpan.FromSeconds(2f))
                            .Subscribe(_ =>
                            {
                                Application.LoadLevel("Intro");
                            });
                    }
                });
        };

        Drag.MouseDownStream()
            .Subscribe(before =>
            {
                startAction(before, Drag.MouseUpStream());
            })
            .AddTo(this.eventResources);

        Drag.TouchStartStream()
            .Subscribe(beforeTouch =>
            {
                var before = Camera.main.ScreenToWorldPoint(beforeTouch.Item1.position);
                startAction(before, Drag.TouchUpStream(beforeTouch.Item1.fingerId).Select(touch => Camera.main.ScreenToWorldPoint(touch.Item1.position)));
            })
            .AddTo(this.eventResources);

        base.Awake();
    }

    public override void OnDestroy()
    {
        this.eventResources.Dispose();
        base.OnDestroy();
    }
}

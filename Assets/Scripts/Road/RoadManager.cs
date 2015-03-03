using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UniLinq;
using UniRx;

public class RoadManager : ObservableMonoBehaviour
{
    private CompositeDisposable eventResources = new CompositeDisposable();
    private float threshold = 0.1f;
    private List<Road> roadList = new List<Road>();

    [SerializeField]
    private GameObject rainbowRoadPrefab;

    public int Ink
    {
        get;
        private set;
    }

    public int UsedInk
    {
        get;
        private set;
    }

    public override void Awake()
    {
        Input.simulateMouseWithTouches = false;
        this.Ink = 40;

        Drag.MouseDownStream()
            .Subscribe(position =>
            {
                SubscribeDragStream(position, Drag.MouseDragStream(threshold));
            })
            .AddTo(this.eventResources);

        Drag.TouchStartStream()
            .Subscribe(touch =>
            {
                SubscribeDragStream(Camera.main.ScreenToWorldPoint(touch.Item1.position), Drag.TouchDragStream(touch.Item1.fingerId, threshold));
            })
            .AddTo(this.eventResources);

        base.Awake();
    }

    private void SubscribeDragStream(Vector3 startPosition, IObservable<Vector3> dragStream)
    {
        var go = Instantiate(this.rainbowRoadPrefab) as GameObject;
        if (go != null)
        {
            go.transform.parent = this.transform.FindChild("Road");
            var road = go.GetComponent<Road>();
            var z = -0.1f;
            if (this.roadList.Count > 0)
            {
                z += this.roadList
                .Min(r => r.transform.position.z);
            }
            this.roadList.Add(road);
            go.transform.position = new Vector3(startPosition.x, startPosition.y, z);

            dragStream.Subscribe(position =>
            {
                if (this.Ink <= 0)
                {
                    road.FinishDrawing();
                }
                else if (!road.IsFinish)
                {
                    RemoveInk();
                    position.z = z;
                    this.UsedInk++;
                    road.UpdatePosition(position);
                }
            },
            () =>
            {
                road.FinishDrawing();
                this.roadList.Remove(road);
            })
            .AddTo(this.eventResources);


            road.OnDequeueAsObservable()
                .Subscribe(_ =>
                {
                    AddInk();
                })
                .AddTo(this.eventResources);
        }
    }

    void AddInk()
    {
        if (AppController.IsTitle)
        {
            return;
        }

        this.Ink++;
        StageController.Instance.UpdateInk(this.Ink);
    }

    void RemoveInk()
    {
        if (AppController.IsTitle)
        {
            return;
        }

        this.Ink--;
        StageController.Instance.UpdateInk(this.Ink);
    }

    public override void OnDestroy()
    {
        this.eventResources.Dispose();

        base.OnDestroy();
    }
}

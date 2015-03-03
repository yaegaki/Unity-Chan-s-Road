using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UniLinq;
using UniRx;

public static class Drag
{
    public static IObservable<Tuple<Touch>> TouchStartStream()
    {
        return Observable.EveryUpdate()
            .SelectMany(_ => Input.touches.WrapValueToClass())
            .Where(touch => touch.Item1.phase == TouchPhase.Began);
    }

    public static IObservable<Tuple<Touch>> TouchUpStream(int fingerId)
    {
        return Observable.EveryUpdate()
            .Where(_ =>
            {
                foreach (var touch in Input.touches)
                {
                    if (touch.fingerId == fingerId)
                    {
                        switch (touch.phase)
                        {
                            case TouchPhase.Canceled:
                            case TouchPhase.Ended:
                                return true;
                            default:
                                return false;
                        }
                    }
                }

                return true;
            })
            .Select(_ =>
            {
                return Input.touches.WrapValueToClass()
                    .Where(touch => touch.Item1.fingerId == fingerId);
            })
            .Select(touches => touches.First());
    }

    public static IObservable<Vector3> MouseDownStream()
    {
        return Observable.EveryUpdate()
            .Where(_ => Input.GetMouseButtonDown(0))
            .Select(_ => Camera.main.ScreenToWorldPoint(Input.mousePosition));
    }
    public static IObservable<Vector3> MouseUpStream()
    {
        return Observable.EveryUpdate()
            .Where(_ => Input.GetMouseButtonUp(0))
            .Select(_ => Camera.main.ScreenToWorldPoint(Input.mousePosition));
    }


    public static IObservable<Vector3> MouseDragStream(float threshold)
    {
        var stream = Observable.EveryUpdate()
            .Where(_ => Input.GetMouseButton(0))
            .TakeUntil(Observable.EveryUpdate().Where(_ => Input.GetMouseButtonUp(0)))
            .Select(_ => Camera.main.ScreenToWorldPoint(Input.mousePosition));
        return AddThreshold(stream, threshold);
    }

    public static IObservable<Vector3> TouchDragStream(int fingerId, float threshold)
    {

        var dragStream = Observable.EveryUpdate()
            .TakeUntil(TouchUpStream(fingerId))
            .Select(_ =>
            {
                return Input.touches.Where(touch => touch.fingerId == fingerId);
            })
            .Select(touches => touches.First())
            .Select(touch =>
            {
                return Camera.main.ScreenToWorldPoint(touch.position);
            });

        return AddThreshold(dragStream, threshold);
    }

    private static IObservable<Vector3> AddThreshold(IObservable<Vector3> stream, float threshold)
    {
        return stream
            .Scan((prev, current) =>
            {
                return Vector3.Distance(prev, current) < threshold ? prev : current;
            })
            .DistinctUntilChanged();
    }
}

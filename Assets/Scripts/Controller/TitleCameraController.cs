using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UniLinq;
using UniRx;

public class CameraController : ObservableMonoBehaviour
{
    [SerializeField]
    private Transform targetTransform;

    public override void LateUpdate()
    {
        var center = this.camera.ViewportToWorldPoint(new Vector3(0.5f, 0));
        if (this.targetTransform.position.x > center.x)
        {
            var pos = this.transform.position;
            pos.x = this.targetTransform.position.x;
            this.transform.position = pos;
        }
        var left = this.camera.ViewportToWorldPoint(new Vector3(0.1f, 0));
        if (this.targetTransform.position.x < left.x)
        {
            var pos = this.transform.position;
            pos.x = this.targetTransform.position.x;
            this.transform.position = pos;
        }

        base.LateUpdate();
    }
}

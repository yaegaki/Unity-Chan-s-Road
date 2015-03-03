using UnityEngine;
using System.Collections;
using UniRx;

public class StageCameraController : ObservableMonoBehaviour
{
    [SerializeField]
    private GameObject unityChan;

    public override void LateUpdate()
    {
        var viewPosition = Camera.main.WorldToViewportPoint(this.unityChan.transform.position);
        float? afterX = null;
        float? afterY = null;
        float? cameraX = null;
        if (!StageController.Instance.IsCleared)
        {
            if (viewPosition.x > 0.5f)
            {
                cameraX = this.unityChan.transform.position.x;
            }
        }


        if (viewPosition.x < 0f)
        {
            afterX = Camera.main.ViewportToWorldPoint(new Vector3(0f, 0f)).x;
        }

        if (viewPosition.y < 0f)
        {
            afterY = Camera.main.ViewportToWorldPoint(new Vector3(0f, 1.1f)).y;
        }

        if (afterX.HasValue || afterY.HasValue)
        {
            var pos = this.unityChan.transform.position;
            pos.x = afterX.HasValue ? afterX.Value : pos.x;
            pos.y = afterY.HasValue ? afterY.Value : pos.y;
            this.unityChan.transform.position = pos;
        }

        if (cameraX.HasValue)
        {
            var pos = this.transform.position;
            pos.x = cameraX.Value;
            this.transform.position = pos;
        }

        base.LateUpdate();
    }
}

using UnityEngine;
using System.Collections;
using UniRx;

public class UnityChanStageController : ObservableMonoBehaviour
{
    [SerializeField]
    private AudioClip damageVoice;

    [SerializeField]
    private AudioClip clearVoice;

    void OnDamage()
    {
        StageController.Instance.AddDamegeCount();
        AudioSourceController.instance.PlayOneShot(this.damageVoice);
    }

    public override void Awake()
    {
        this.UpdateAsObservable()
            .Where(_ => StageController.Instance != null)
            .Where(_ => StageController.Instance.IsCleared)
            .First()
            .Subscribe(_ =>
            {
                AudioSourceController.instance.PlayOneShot(this.clearVoice);
            });
        base.Awake();
    }
}

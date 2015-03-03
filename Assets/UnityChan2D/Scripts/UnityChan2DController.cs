using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniLinq;
using UniRx;

[RequireComponent(typeof(Animator), typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class UnityChan2DController : ObservableMonoBehaviour
{
    public float maxSpeed = 10f;
    public float jumpPower = 1000f;
    public Vector2 backwardForce = new Vector2(-4.5f, 5.4f);

    public LayerMask whatIsGround;

    private Animator m_animator;
    private BoxCollider2D m_boxcollier2D;
    private Rigidbody2D m_rigidbody2D;
    private bool m_isGround;
    private const float m_centerY = 1.5f;

    private State m_state = State.Normal;

    private float changeDirectionThreshold = 1f;
    private float changeDirectionTime = 0.7f;
    private CompositeDisposable eventResources = new CompositeDisposable();

    public override void Reset()
    {
        Awake();

        // UnityChan2DController
        maxSpeed = 10f;
        jumpPower = 1000;
        backwardForce = new Vector2(-4.5f, 5.4f);
        whatIsGround = 1 << LayerMask.NameToLayer("Ground");

        // Transform
        transform.localScale = new Vector3(1, 1, 1);

        // Rigidbody2D
        m_rigidbody2D.gravityScale = 3.5f;
        m_rigidbody2D.fixedAngle = true;

        // BoxCollider2D
        m_boxcollier2D.size = new Vector2(1, 2.5f);
        m_boxcollier2D.center = new Vector2(0, -0.25f);

        // Animator
        m_animator.applyRootMotion = false;

        this.Reset();
    }

    public override void Awake()
    {
        m_animator = GetComponent<Animator>();
        m_boxcollier2D = GetComponent<BoxCollider2D>();
        m_rigidbody2D = GetComponent<Rigidbody2D>();

        float x = 1;
        this.UpdateAsObservable()
            .Where(_ => m_state != State.Damaged)
            .Subscribe(_ =>
            {
                Move(x, false);
            })
            .AddTo(this.eventResources);

        var directionChangeStream = this.UpdateAsObservable()
            .Select(_ => this.transform.forward)
            .DistinctUntilChanged()
            .Select(_ => Unit.Default);


        var xChangeStream = this.UpdateAsObservable()
            .Select(_ => this.transform.position.x)
            .Scan((prev, current) =>
            {
                return Mathf.Abs(prev - current) < this.changeDirectionThreshold ? prev : current;
            })
            .DistinctUntilChanged()
            .Select(_ => Unit.Default);


        xChangeStream
            .Merge(directionChangeStream)
            .Throttle(TimeSpan.FromSeconds(this.changeDirectionTime))
            .Subscribe(_ => x = -x)
            .AddTo(this.eventResources);



        base.Awake();
    }

    void Move(float move, bool jump)
    {
        if (Mathf.Abs(move) > 0)
        {
            Quaternion rot = transform.rotation;
            transform.rotation = Quaternion.Euler(rot.x, Mathf.Sign(move) == 1 ? 0 : 180, rot.z);
        }

        m_rigidbody2D.velocity = new Vector2(move * maxSpeed, m_rigidbody2D.velocity.y);

        m_animator.SetFloat("Horizontal", move);
        m_animator.SetFloat("Vertical", m_rigidbody2D.velocity.y);
        m_animator.SetBool("isGround", m_isGround);

        if (jump && m_isGround)
        {
            m_animator.SetTrigger("Jump");
            SendMessage("Jump", SendMessageOptions.DontRequireReceiver);
            m_rigidbody2D.AddForce(Vector2.up * jumpPower);
        }
    }

    public override void FixedUpdate()
    {
        Vector2 pos = transform.position;
        Vector2 groundCheck = new Vector2(pos.x, pos.y - (m_centerY * transform.localScale.y));
        Vector2 groundArea = new Vector2(m_boxcollier2D.size.x * 0.49f, 0.05f);

        m_isGround = Physics2D.OverlapArea(groundCheck + groundArea, groundCheck - groundArea, whatIsGround);
        m_animator.SetBool("isGround", m_isGround);

        base.FixedUpdate();
    }

    public override void OnTriggerStay2D(Collider2D other)
    {
        if (other.tag == "DamageObject" && m_state == State.Normal)
        {
            m_state = State.Damaged;
            StartCoroutine(INTERNAL_OnDamage());
        }

        base.OnTriggerStay2D(other);
    }

    IEnumerator INTERNAL_OnDamage()
    {
        m_animator.Play(m_isGround ? "Damage" : "AirDamage");
        m_animator.Play("Idle");

        SendMessage("OnDamage", SendMessageOptions.DontRequireReceiver);

        m_rigidbody2D.velocity = new Vector2(transform.right.x * backwardForce.x, transform.up.y * backwardForce.y);

        yield return new WaitForSeconds(.2f);

        while (m_isGround == false)
        {
            yield return new WaitForFixedUpdate();
        }
        m_animator.SetTrigger("Invincible Mode");
        m_state = State.Invincible;
    }

    void OnFinishedInvincibleMode()
    {
        m_state = State.Normal;
    }

    public override void OnDestroy()
    {
        this.eventResources.Dispose();

        base.OnDestroy();
    }

    enum State
    {
        Normal,
        Damaged,
        Invincible,
    }
}

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UniLinq;
using UniRx;

[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter), typeof(EdgeCollider2D))]
public abstract class Road : ObservableMonoBehaviour
{
    [SerializeField]
    private MeshFilter meshFilter;

    [SerializeField]
    private MeshRenderer meshRenderer;

    [SerializeField]
    private EdgeCollider2D edgeCollider;

    [SerializeField]
    private float deleteDelay = 2f;
    public float DeleteDelay
    {
        get
        {
            return deleteDelay;
        }
        set
        {
            DeleteDelay = value;
        }
    }

    [SerializeField]
    private float thickness = 0.35f;
    public float Thickness
    {
        get
        {
            return thickness;
        }
        set
        {
            thickness = value;
        }
    }

    private float uvLoopPerSeconds = 1f;
    private float UVLoopPerSeconds
    {
        get
        {
            return uvLoopPerSeconds;
        }
    }




    private static readonly int CIRCLE_VERTEX_COUNT = 20;
    private float startTime;

    private Subject<Unit> onDequeue = new Subject<Unit>();
    public IObservable<Unit> OnDequeueAsObservable()
    {
        return this.onDequeue;
    }

    public bool IsFinish
    {
        get;
        private set;
    }

    private List<Vector3> positionList = new List<Vector3>();
    private int deleteCount = 0;
    public List<Vector3> ActivePositionList
    {
        get
        {
            return positionList.GetRange(deleteCount, positionList.Count - deleteCount);
        }
    }
    protected CompositeDisposable eventResources = new CompositeDisposable();

    public override void Awake()
    {
        this.startTime = Time.realtimeSinceStartup;
        this.UpdateAsObservable()
            .Subscribe(_ => UpdateUV());
        base.Awake();
    }

    public override void OnDestroy()
    {
        this.onDequeue.OnCompleted();
        this.eventResources.Dispose();

        base.OnDestroy();
    }

    public virtual void UpdateMeshAndCollider()
    {
        var activePositionList = this.ActivePositionList;
        if (activePositionList.Count == 0)
        {
            this.meshRenderer.enabled = false;
            return;
        }
        var mesh = new Mesh();
        var vertexList = new List<Vector3>();
        var triangleList = new List<int>();
        var radius = thickness / 2f;
        var startAngle = 0f;
        var firstPosition = activePositionList.First();
        var lastPosition = activePositionList.Last();
        var leftColliderVertexList = new List<Vector2>();
        var rightColliderVertexList = new List<Vector2>();


        if (activePositionList.Count > 1)
        {
            startAngle = GetAngle(firstPosition, activePositionList[1]) + 90f;
        }

        var endAngle = startAngle + 180f;
        var diff = 360f / CIRCLE_VERTEX_COUNT;
        for (var i = startAngle; i <= endAngle; i += diff)
        {
            var rad = Mathf.PI * i / 180f;
            var vertex = new Vector3(radius * Mathf.Cos(rad), radius * Mathf.Sin(rad)) + firstPosition;
            vertexList.Add(vertex);
            rightColliderVertexList.Add(vertex);
        }

        var semiCirclePoint = CIRCLE_VERTEX_COUNT / 2;
        for (var i = 0; i < semiCirclePoint - 1; i++)
        {
            triangleList.Add(i);
            triangleList.Add(semiCirclePoint);
            triangleList.Add(i + 1);
        }


        var vertexIndex = vertexList.Count;
        if (activePositionList.Count > 1)
        {

            activePositionList
                .Select((position, index) => new { Index = index, Position = position })
                .Aggregate((current, next) =>
                {
                    var index = current.Index;
                    var currentPos = current.Position;
                    var nextPos = next.Position;
                    var cross = Vector3.Cross(nextPos - currentPos, Vector3.forward).normalized * radius;

                    if (index == 0)
                    {
                        vertexList.Add(currentPos - cross);
                        vertexList.Add(currentPos + cross);
                        leftColliderVertexList.Add(currentPos - cross);
                        rightColliderVertexList.Add(currentPos + cross);
                    }


                    vertexList.Add(nextPos - cross);
                    vertexList.Add(nextPos + cross);
                    leftColliderVertexList.Add(nextPos - cross);
                    rightColliderVertexList.Add(nextPos + cross);
                    triangleList.Add(vertexIndex + index * 2);
                    triangleList.Add(vertexIndex + index * 2 + 3);
                    triangleList.Add(vertexIndex + index * 2 + 1);
                    triangleList.Add(vertexIndex + index * 2);
                    triangleList.Add(vertexIndex + index * 2 + 2);
                    triangleList.Add(vertexIndex + index * 2 + 3);
                    return next;
                });

        }

        vertexIndex = vertexList.Count - 1;
        if (activePositionList.Count == 1)
        {
            startAngle = 180f;
        }
        else
        {
            startAngle = GetAngle(activePositionList[activePositionList.Count - 2], lastPosition) + 270f;
        }

        endAngle = startAngle + 180f - diff;
        startAngle += diff;

        for (var i = startAngle; i <= endAngle; i += diff)
        {
            var rad = Mathf.PI * i / 180f;
            var vertex = new Vector3(radius * Mathf.Cos(rad), radius * Mathf.Sin(rad)) + lastPosition;
            vertexList.Add(vertex);
            rightColliderVertexList.Add(vertex);
        }

        var triangleTarget = activePositionList.Count > 1 ? vertexIndex - 1 : 0;

        for (var i = 0; i < semiCirclePoint; i++)
        {
            if (i == 0)
            {
                triangleList.Add(vertexIndex);
            }
            else
            {
                triangleList.Add(vertexIndex + semiCirclePoint - i);
            }
            triangleList.Add(vertexIndex + semiCirclePoint - i - 1);
            triangleList.Add(triangleTarget);
        }

        this.edgeCollider.points = rightColliderVertexList.Concat(leftColliderVertexList.Reverse<Vector2>()).ToArray();
        mesh.vertices = vertexList.ToArray();
        mesh.triangles = triangleList.ToArray();
        this.meshFilter.mesh = mesh;
        UpdateUV();
    }

    private float GetAngle(Vector2 from, Vector2 to)
    {
        var angle = Vector2.Angle(Vector2.right, to - from);
        if (from.y > to.y)
        {
            angle = 360f - angle;
        }
        return angle;
    }

    public virtual void UpdateUV()
    {
        var elapsedSecond = Time.realtimeSinceStartup - this.startTime;
        var offset = (elapsedSecond - Mathf.Floor(elapsedSecond / this.UVLoopPerSeconds) * this.UVLoopPerSeconds) / this.uvLoopPerSeconds;
        var offsetVector = new Vector2(offset, offset);
        var uvList = new List<Vector2>();
        this.meshFilter.mesh.uv = this.meshFilter.mesh.vertices.ToList().Select(pos =>
        {
            return (Vector2)pos - offsetVector; 
        })
        .ToArray();
    }

    public virtual void UpdatePosition(Vector3 position)
    {
        this.positionList.Add(position - this.gameObject.transform.position);
        UpdateMeshAndCollider();
        Observable.Timer(TimeSpan.FromSeconds(this.DeleteDelay))
            .Subscribe(_ =>
            {
                this.deleteCount++;
                this.onDequeue.OnNext(Unit.Default);
                if (this.deleteCount == this.positionList.Count && this.IsFinish)
                {
                    Destroy(this.gameObject);
                }
                else
                {
                    this.UpdateMeshAndCollider();
                }
            })
            .AddTo(this.eventResources);
    }

    public virtual void FinishDrawing()
    {
        if (!this.IsFinish)
        {
            if (this.deleteCount == this.positionList.Count)
            {
                Destroy(this.gameObject);
            }
            this.IsFinish = true;
        }
    }
}

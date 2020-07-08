using System;
using UnityEngine;

public class CurveInterpolation : MonoBehaviour 
{
    public enum Direction { Xpos, Xneg, Ypos, Yneg, Zpos, Zneg}

    public Transform pivot;
    public Transform tip;
    public Transform[] nodes;
    public Direction lookAtUp;

    private int segmentCount;
    private Transform control;

    private void Start()
    {
        float distance = Vector3.Distance(pivot.position, tip.position);
        segmentCount = nodes.Length;
        Vector3 direction = (tip.position - pivot.position).normalized;

        control = new GameObject("Control").transform;
        control.parent = transform;
        control.position = pivot.position + direction * distance / 2f;
        control.rotation = pivot.rotation;
    }

    private void FixedUpdate()
    {
        for (int i = 0; i < segmentCount; i++)
        {
            nodes[i].position = QuadraticBézier(pivot.position, control.position, tip.position,
                                                i / (float)(segmentCount));
            nodes[i].LookAt(nodes[i].position + QuadraticBézierTangent(pivot.position, control.position, tip.position,
                                                i / (float)(segmentCount)), GetDirection(lookAtUp));
        }
    }

    private Vector3 GetDirection(Direction direction)
    {
        switch (direction)
        {
            case Direction.Xpos:
                return transform.right;
            case Direction.Xneg:
                return -transform.right;
            case Direction.Ypos:
                return transform.up;
            case Direction.Yneg:
                return -transform.up;
            case Direction.Zpos:
                return transform.forward;
            case Direction.Zneg:
                return -transform.forward;
        }
        return Vector3.zero;
    }

    private Vector3 QuadraticBézier(Vector3 A, Vector3 B, Vector3 C, float t)
    {
        return Mathf.Pow((1f - t), 2) * A + 2f * (1f - t) * t * B + Mathf.Pow(t, 2) * C;
    }

    private Vector3 QuadraticBézierTangent(Vector3 A, Vector3 B, Vector3 C, float t)
    {
        return 2f * (-A + B + (A - 2 * B + C) * t);
    }

    private void OnDrawGizmos()
    {
        if (nodes != null)
        {
            for (int i = 0; i < segmentCount; i++)
            {
                //node 
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(nodes[i].position, 0.05f);

                //line connecting nodes
                if (i > 0)
                {
                    Gizmos.color = new Color(1, 1, 1, 0.25f);
                    Gizmos.DrawLine(nodes[i - 1].position, nodes[i].position);
                }

                //orientation
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(nodes[i].position, nodes[i].position + nodes[i].forward * 0.5f);
                Gizmos.color = Color.green;
                Gizmos.DrawLine(nodes[i].position, nodes[i].position + nodes[i].up * 0.5f);
                Gizmos.color = Color.red;
                Gizmos.DrawLine(nodes[i].position, nodes[i].position + nodes[i].right * 0.5f);
            }

            //control point
            if (control != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(control.position, 0.1f);
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(nodes[0].position, control.position);
            }
        }
    }
}

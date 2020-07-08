using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using UnityEngine;

namespace KSPSecondaryMotion
{
    public class ModuleQuadraticBezierInterpolation : PartModule
    {
        [KSPField(isPersistant = false)]
        public string pivotName = "pivot";
        [KSPField(isPersistant = false)]
        public string tipName = "tip";
        [KSPField(isPersistant = false)]
        public string lookAtUp = "Zneg";

        private Transform pivot;
        private Transform tip;
        private Transform control;
        private Transform[] segments;
        private ModuleDeployablePart deployablePart;
        private bool isDisabled = false;
        private enum Direction { Xpos, Xneg, Ypos, Yneg, Zpos, Zneg }
        private Direction up;

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            if (HighLogic.LoadedScene == GameScenes.LOADING)
            {
                Debug.Log($"[KSP Secondary Motion - Quadratic Bezier Interpolation]OnLoad");

                ConfigNode[] nodes = node.GetNodes("SEGMENT");
                /*
                   in cfg:
                    SEGMENT
                    {
                        name = segment_0
                        index = 0
                    }
                 */
                if (nodes != null && nodes.Length > 0)
                {
                    List<InterpolationSegmentData.Segment> segments = new List<InterpolationSegmentData.Segment>();
                    foreach (ConfigNode n in nodes)
                    {
                        string name = n.GetValue("name");
                        Debug.Log($"[KSP Secondary Motion - Quadratic Bezier Interpolation] Part {part.name} " +
                            $"has ModuleQuadraticBezierInterpolation with a SEGMENT named {name}");

                        Transform transform = part.FindModelTransform(name);
                        if (transform == null)
                        {
                            Debug.LogError($"[KSP Secondary Motion - Quadratic Bezier Interpolation] Cannot find transform {name}");
                            return;
                        }

                        InterpolationSegmentData.Segment segment = new InterpolationSegmentData.Segment(name, int.Parse(n.GetValue("index")));
                        segments.Add(segment);
                    }

                    segments.Sort((seg1, seg2) => seg1.index.CompareTo(seg2.index)); //sort by index

                    InterpolationSegmentData.segmentsDataBase.Add(part.name, segments);
                }
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            if (!HighLogic.LoadedSceneIsFlight)
                return;

            Initialize();

            GameEvents.onVesselCreate.Add(OnVesselCreate);
        }

        void OnVesselCreate(Vessel v)
        {
            Initialize();
        }

        void OnDestroy()
        {
            GameEvents.onVesselCreate.Remove(OnVesselCreate);
        }

        public void Initialize()
        {
            // find transforms
            pivot = part.FindModelTransform(pivotName);
            if (pivot == null)
            {
                Debug.LogError($"[KSP Secondary Motion - Quadratic Bezier Interpolation] Cannot find transform {pivotName}");
                return;
            }
            tip = part.FindModelTransform(tipName);
            if (tip == null)
            {
                Debug.LogError($"[KSP Secondary Motion - Quadratic Bezier Interpolation] Cannot find transform {tipName}");
                return;
            }

            // get segments
            if (InterpolationSegmentData.segmentsDataBase.TryGetValue(part.name, out List<InterpolationSegmentData.Segment> segmentsData))
            {
                segments = segmentsData.Select(seg => part.FindModelTransform(seg.name)).ToArray();
                //Debug.Log($"[KSP Secondary Motion - Quadratic Bezier Interpolation] Interpolation Segment Data found");
            }
            else
            {
                Debug.LogError($"[KSP Secondary Motion - Quadratic Bezier Interpolation] Interpolation Segment Data for {part.name} not found!");
                return;
            }

            float distance = Vector3.Distance(pivot.position, tip.position);
            //Vector3 direction = (tip.position - pivot.position).normalized;
            control = transform.Find("Bezier Control");
            if (control == null)
            {
                control = new GameObject("Bezier Control").transform;
            }
            control.parent = transform;
            control.position = transform.position + pivot.parent.up * distance / 2f;
            control.localRotation = Quaternion.identity;

            foreach (PartModule m in part.Modules)
            {
                switch (m.moduleName)
                {
                    case "ModuleDeployableSolarPanel":
                    case "ModuleDeployableAntenna":
                    case "ModuleDeployableRadiator":
                    case "ModuleDeployablePart":
                        Debug.Log($"[KSP Secondary Motion - Quadratic Bezier Interpolation] Found {m.moduleName} on Part {part.name}");
                        deployablePart = (ModuleDeployablePart)m;
                        return;
                    default:
                        break;
                }
            }

            string input = lookAtUp.ToLower().Trim();
            switch (input)
            {
                case "xpos":
                    up = Direction.Xpos;
                    break;
                case "xneg":
                    up = Direction.Xneg;
                    break;
                case "ypos":
                    up = Direction.Ypos;
                    break;
                case "yneg":
                    up = Direction.Yneg;
                    break;
                case "zpos":
                    up = Direction.Zpos;
                    break;
                case "zneg":
                    up = Direction.Zneg;
                    break;
            }
        }


        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            if (deployablePart != null)
            {
                if (deployablePart.deployState == ModuleDeployablePart.DeployState.EXTENDED)
                {
                    isDisabled = false;
                }
                else if (isDisabled)
                {
                    return;
                }
                else // not extended but still enable
                {
                    isDisabled = true;
                    return;
                }
            }

            for (int i = 0; i < segments.Length; i++)
            {
                segments[i].position = QuadraticBezier(pivot.position, control.position, tip.position,
                                                       i / (float)(segments.Length));
                segments[i].LookAt(segments[i].position + QuadraticBezierTangent(pivot.position, control.position, tip.position,
                                                       i / (float)(segments.Length)), GetDirection(up));
            }
        }

        private Vector3 QuadraticBezier(Vector3 A, Vector3 B, Vector3 C, float t)
        {
            return Mathf.Pow((1f - t), 2) * A + 2f * (1f - t) * t * B + Mathf.Pow(t, 2) * C;
        }

        private Vector3 QuadraticBezierTangent(Vector3 A, Vector3 B, Vector3 C, float t)
        {
            return 2f * (-A + B + (A - 2 * B + C) * t);
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
    }
}
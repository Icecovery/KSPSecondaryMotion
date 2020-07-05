using System;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPSecondaryMotion
{
    public class ModuleHierarchyReconstructor : PartModule
    {
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            if (HighLogic.LoadedScene == GameScenes.LOADING)
            {
                Debug.Log($"[KSP Secondary Motion - Hierarchy Reconstructor]OnLoad");

                ConfigNode[] nodes = node.GetNodes("RECONSTRUCT");
                if (nodes != null && nodes.Length > 0)
                {
                    foreach (ConfigNode n in nodes)
                    {
                        Debug.Log($"[KSP Secondary Motion - Hierarchy Reconstructor] Part {part.name} has ModuleHierarchyReconstructor with a RECONSTRUCT named {n.GetValue("name")}");

                        Reconstruct reconstruct = new Reconstruct(n.GetValue("name"),
                                                                  bool.Parse(n.GetValue("reparent")),
                                                                  n.GetValue("asChildOf"),
                                                                  n.GetValue("asParentOf"),
                                                                  n.GetValue("whichIsAChildOf"),
                                                                  StringToVector3(n.GetValue("localPosition")),
                                                                  StringToVector3(n.GetValue("localRotationEuler")),
                                                                  StringToVector3(n.GetValue("localScale")));

                        Debug.Log($"[KSP Secondary Motion - Hierarchy Reconstructor] RECONSTRUCT Content:\n{reconstruct}");

                        GameObject obj = new GameObject(reconstruct.name);
                        if (reconstruct.reparent)
                        {
                            //reparent mode
                            Transform child = null;
                            if (reconstruct.whichIsAChildOf == "__ISROOTTRANSFORM__")
                            {
                                child = part.FindModelTransform(reconstruct.asParentOf);
                            }
                            else
                            {
                                child = part.FindModelTransforms(reconstruct.asParentOf).First(candidate => candidate.parent.name == reconstruct.whichIsAChildOf);
                            }

                            
                            obj.transform.parent = child.parent;
                            obj.transform.localPosition = reconstruct.localPosition;
                            obj.transform.localRotation = Quaternion.Euler(reconstruct.localRotationEuler);
                            obj.transform.localScale = reconstruct.localScale;
                            child.parent = obj.transform;
                        }
                        else
                        {
                            //add transform only mode
                            Transform parent = null;
                            if (reconstruct.whichIsAChildOf == "__ISROOTTRANSFORM__")
                            {
                                parent = part.FindModelTransform(reconstruct.asChildOf);
                            }
                            else
                            {
                                parent = part.FindModelTransforms(reconstruct.asChildOf).First(candidate => candidate.parent.name == reconstruct.whichIsAChildOf);
                            }

                            obj.transform.parent = parent;
                            obj.transform.localPosition = reconstruct.localPosition;
                            obj.transform.localRotation = Quaternion.Euler(reconstruct.localRotationEuler);
                            obj.transform.localScale = reconstruct.localScale;
                        }

                        Debug.Log($"[KSP Secondary Motion - Hierarchy Reconstructor] Reconstruction complete");
                    }

                }
            }
        }

        private Vector3 StringToVector3(string input)
        {
            input = input.Replace("(", "").Replace(")", "");
            string[] s = input.Split(',');

            Vector3 result = new Vector3(float.Parse(s[0].Trim()),
                                         float.Parse(s[1].Trim()),
                                         float.Parse(s[2].Trim()));

            return result;
        }

        [Serializable]
        public struct Reconstruct
        {
            public string name;
            public bool reparent;
            public string asChildOf;
            public string asParentOf;
            public string whichIsAChildOf;
            public Vector3 localPosition;
            public Vector3 localRotationEuler;
            public Vector3 localScale;

            public Reconstruct(string name,
                               bool reparent,
                               string asChildOf,
                               string asParentOf,
                               string whichIsAChildOf,
                               Vector3 localPosition,
                               Vector3 localRotationEuler,
                               Vector3 localScale)
            {
                this.name = name;
                this.reparent = reparent;
                this.asChildOf = asChildOf;
                this.asParentOf = asParentOf;
                this.whichIsAChildOf = whichIsAChildOf;
                this.localPosition = localPosition;
                this.localRotationEuler = localRotationEuler;
                this.localScale = localScale;
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"name={name}");
                sb.AppendLine($"reparent={reparent}");
                sb.AppendLine($"asChildOf={asChildOf}");
                sb.AppendLine($"asParentOf={asParentOf}");
                sb.AppendLine($"whichIsAChildOf={whichIsAChildOf}");
                sb.AppendLine($"localPosition={localPosition}");
                sb.AppendLine($"localRotationEuler={localRotationEuler}");
                sb.AppendLine($"localScale={localScale}");
                return sb.ToString();
            }
        }
    }
}

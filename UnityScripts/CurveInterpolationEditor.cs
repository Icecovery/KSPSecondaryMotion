using UnityEngine;
using UnityEditor;
using System.Text;

[CustomEditor(typeof(CurveInterpolation))]
[CanEditMultipleObjects]
public class CurveInterpolationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Generate CFG"))
        {
            CurveInterpolation c = (CurveInterpolation)target;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("MODULE");
            sb.AppendLine("{");
            sb.AppendLine("\tname = ModuleQuadraticBezierInterpolation");
            sb.AppendLine("\tpivotName = " + c.pivot.name);
            sb.AppendLine("\ttipName = " + c.tip.name);
            sb.AppendLine("\tlookAtUp = " + c.lookAtUp.ToString());
            for (int i = 0; i < c.nodes.Length; i++)
            {
                sb.AppendLine("\tSEGMENT");
                sb.AppendLine("\t{");
                sb.AppendLine("\t\tname = " + c.nodes[i].name);
                sb.AppendLine("\t\tindex = " + i);
                sb.AppendLine("\t}");
            }
            sb.AppendLine("}");

            Debug.Log(sb.ToString());
        }
    }
}

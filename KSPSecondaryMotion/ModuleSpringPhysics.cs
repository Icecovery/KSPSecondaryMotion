using System.Text;
using UnityEngine;

namespace KSPSecondaryMotion
{
    public class ModuleSpringPhysics : PartModule
    {
        #region cfg tweakable
        [KSPField(isPersistant = false)]
        public string targetName = "Target";                //root ideal look at position

        [KSPField(isPersistant = false)]
        public string rootName = "Root";                    //rotation root

        [KSPField(isPersistant = false)]
        public float tipMass = 0.2f;                        //mass of the tip

        [KSPField(isPersistant = false)]
        public bool applyGravity = false;                   //apply gravity to the tip?
        #endregion

        #region UI tweakable
        [KSPField(  isPersistant = true,
                    guiActive = true,
                    guiActiveEditor = true,
                    guiName = "Spring Physics Damper Ratio",
                    advancedTweakable = true,
                    groupName = "KSPSecondaryMotion",
                    groupDisplayName = "Secondary Motion Settings",
                    groupStartCollapsed = true),
        UI_FloatRange(
                    minValue = 0.0f, 
                    maxValue = 10.0f, 
                    stepIncrement = 0.1f, 
                    scene = UI_Scene.All)]
        public float damperRatio = 0.5f;                   //drag multiplier

        [KSPField(  isPersistant = true,
                    guiActive = true,
                    guiActiveEditor = true,
                    guiName = "Spring Physics Spring Ratio",
                    advancedTweakable = true,
                    groupName = "KSPSecondaryMotion",
                    groupDisplayName = "Secondary Motion Settings",
                    groupStartCollapsed = true),
        UI_FloatRange(
                    minValue = 10f,
                    maxValue = 500f,
                    stepIncrement = 5.0f,
                    scene = UI_Scene.All)]
        public float springRatio = 80.0f;                   //spring force

        [KSPField(isPersistant = true,
                    guiActive = true,
                    guiActiveEditor = true,
                    guiName = "Failsafe Activate Range",
                    advancedTweakable = true,
                    groupName = "KSPSecondaryMotion",
                    groupDisplayName = "Secondary Motion Settings",
                    groupStartCollapsed = true),
        UI_FloatRange(
                    minValue = 1f,
                    maxValue = 50f,
                    stepIncrement = 1.0f,
                    scene = UI_Scene.All)]
        public float failsafeRange = 5.0f;                  //Failsafe Activate Range

        #endregion

        private Transform target;                           //tip ideal position
        private Transform root;                             //wiggling root

        private Transform springObj;                        //virtual obj where root will look at
        private Rigidbody springRB;                         //rigidbody of the virtual obj
        private Vector3 LocalDistance;                      //distance between ideal position and virtual obj
        private Vector3 LocalVelocity;                      //velocity converted to local space

        private ModuleDeployablePart deployablePart;
        //private ModuleAnimateGeneric animateGeneric;

        private bool isDisabled = false;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            if (!HighLogic.LoadedSceneIsFlight) 
                return;

            InitializationAndActivate();

            GameEvents.onVesselCreate.Add(OnVesselCreate); 
        }

        private void InitializationAndActivate()
        {
            if (InitializeSpring())
                part.force_activate();
            else
                Debug.LogError($"[KSP Secondary Motion - Spring Physics] Initialization failed on part " +
                    $"{part.partInfo.name} at vessel {vessel.GetDisplayName()}, Spring Physics on this " +
                    $"part will not be activated.");
        }

        void OnDestroy()
        {
            GameEvents.onVesselCreate.Remove(OnVesselCreate);
            if (springObj != null)
            {
                Destroy(springObj.gameObject);
            }
        }

        void OnVesselCreate(Vessel v)
        {
            InitializationAndActivate();
        }

        private bool InitializeSpring()
        {
            //Debug.Log($"[KSP Secondary Motion - Spring Physics] Initializing Spring");

            target = part.FindModelTransform(targetName);
            if (!target)
            {
                Debug.Log($"[KSP Secondary Motion - Spring Physics] Can't Find Transform *{targetName}*");
                return false;
            }

            root = part.FindModelTransform(rootName);
            if (!root)
            {
                Debug.Log($"[KSP Secondary Motion - Spring Physics] Can't Find Transform *{rootName}*");
                return false;
            }

            target.rotation = transform.rotation;

            springObj = new GameObject("Spring").transform;
            springObj.transform.position = target.transform.position;
            springObj.transform.rotation = target.transform.rotation;
            springRB = springObj.gameObject.AddComponent<Rigidbody>();
            springRB.mass = tipMass;
            springRB.drag = 0f;
            springRB.angularDrag = 0f;
            springRB.useGravity = false;
            springRB.constraints = (RigidbodyConstraints)((int)RigidbodyConstraints.FreezeRotationX
                                                          + (int)RigidbodyConstraints.FreezeRotationY
                                                          + (int)RigidbodyConstraints.FreezeRotationZ);

            foreach (PartModule m in part.Modules)
            {
                switch (m.moduleName)
                {
                    case "ModuleDeployableSolarPanel":
                    case "ModuleDeployableAntenna":
                    case "ModuleDeployableRadiator":
                    case "ModuleDeployablePart":
                        Debug.Log($"[KSP Secondary Motion - Spring Physics] Found {m.moduleName}");
                        deployablePart = (ModuleDeployablePart)m;
                        goto END_OF_FOREACH;
                    //case "ModuleAnimateGeneric":
                    //    Debug.Log($"[KSP Secondary Motion - Spring Physics] Found {m.moduleName}");
                    //    animateGeneric = (ModuleAnimateGeneric)m;
                    //    goto END_OF_FOREACH;
                    default:
                        break;
                }
            }
            END_OF_FOREACH: //How to break loop from switch //plz don't kill me
            
            return true;
        }

        private void Disable()
        {
            springObj.transform.position = target.transform.position;
            springRB.velocity = Vector3.zero;
            root.LookAt(springObj.position, -transform.forward);

            isDisabled = true;
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
                    Disable();
                    return;
                }
            }
            //else if (animateGeneric != null)
            //{
            //    if (animateGeneric.deployPercent >= 100f) //extended
            //    {
            //        isDisabled = false;
            //    }
            //    else if (isDisabled)
            //    {
            //        return;
            //    }
            //    else // not extended but still enable
            //    {
            //        Disable();
            //        return;
            //    }
            //}

            //modified from https://forum.unity.com/threads/car-antenna-physics.464619/#post-3293313

            //Sync the rotation 
            springRB.transform.rotation = this.transform.rotation;

            //Calculate the distance between the two points
            LocalDistance = target.InverseTransformDirection(target.position - springObj.position);

            // in case something went wrong
            if (LocalDistance.sqrMagnitude > failsafeRange * failsafeRange) 
            {
                //Debug.Log($"[KSP Secondary Motion - Spring Physics] LocalDistance.sqrMagnitude = " +
                //    $"{LocalDistance.sqrMagnitude}, resetting spring");
                springObj.transform.position = target.transform.position;
                springRB.velocity = Vector3.zero;
                LocalDistance = Vector3d.zero;
            }

            springRB.AddRelativeForce((LocalDistance) * springRatio); //Apply Spring

            //Calculate the local velocity of the springObj point
            LocalVelocity = (springObj.InverseTransformDirection(springRB.velocity));
            springRB.AddRelativeForce((-LocalVelocity) * damperRatio); //Apply drag

            if (applyGravity)
            {
                springRB.AddForce(FlightGlobals.getGeeForceAtPosition(transform.position), ForceMode.Acceleration);
            }

            //Aim the visible geo at the spring target
            root.LookAt(springObj.position, -transform.forward);
        }

        public override string GetInfo()
        {
            StringBuilder output = new StringBuilder();
            output.AppendLine($"<b>This part contain Spring Physics Module</b>");
            output.AppendLine($"Default Damper Ratio: {damperRatio}");
            output.AppendLine($"Default Spring Ratio: {springRatio}");
            output.AppendLine($"Default Failsafe Activate Range: {failsafeRange}");
            output.AppendLine($"<i>Above settings can be changed inside VAB/SPH or on flight (Advanced" +
                $" Tweakables enable required)</i>");
            output.AppendLine($"Tip Mass: {tipMass}");
            output.AppendLine($"Applying Gravity: {applyGravity}");
            return output.ToString();
        }
    }
}

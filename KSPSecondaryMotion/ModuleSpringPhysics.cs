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


        //testing

        //[KSPField(isPersistant = false)]
        //public bool reconstructAndModifyHierarchy = false;

        //[KSPField(isPersistant = false)]
        //public string addTargetUnder = "";

        //[KSPField(isPersistant = false)]
        //public string whichIsChildOf = "";

        //[KSPField(isPersistant = false)]

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
                    minValue = 0.1f,
                    maxValue = 10f,
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

        private bool hasDeployablePart;
        private ModuleDeployablePart deployablePart;

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

            ModuleDeployablePart solar = part.FindModelComponent<ModuleDeployableSolarPanel>();
            if (solar != null)
            {
                deployablePart = solar;
                hasDeployablePart = true;
                Debug.Log($"[KSP Secondary Motion - Spring Physics] Found ModuleDeployableSolarPanel");
            }
            else
            {
                ModuleDeployablePart antenna = part.FindModelComponent<ModuleDeployableAntenna>();
                if (antenna != null)
                {
                    deployablePart = antenna;
                    hasDeployablePart = true;
                    Debug.Log($"[KSP Secondary Motion - Spring Physics] Found ModuleDeployableAntenna");
                }
                else
                {
                    ModuleDeployablePart radiator = part.FindModelComponent<ModuleDeployableRadiator>();
                    if (radiator != null)
                    {
                        deployablePart = radiator;
                        hasDeployablePart = true;
                        Debug.Log($"[KSP Secondary Motion - Spring Physics] Found ModuleDeployableRadiator");
                    }
                    else
                    {
                        deployablePart = null;
                        hasDeployablePart = false;
                        Debug.Log($"[KSP Secondary Motion - Spring Physics] No ModuleDeployablePart found");
                    }
                }
            }
            
            return true;
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            if (hasDeployablePart)
            {
                if (deployablePart.deployState != ModuleDeployablePart.DeployState.EXTENDED)
                {
                    if (LocalDistance.sqrMagnitude > 10000)
                    {
                        springObj.transform.position = target.transform.position;
                        springRB.velocity = Vector3.zero;
                        root.LookAt(springObj.position, -transform.forward);
                    }
                    return;
                }
            }

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

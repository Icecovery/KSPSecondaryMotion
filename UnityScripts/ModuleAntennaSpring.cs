using UnityEngine;

public class ModuleAntennaSpring : MonoBehaviour 
{
    public Transform target;                //root ideal look at position
    public Transform pivot;                 //rotation root
    public float springMass = 0.2f;         //mass of the tip
    public float drag = 2.5f;
    public float springForce = 80.0f;

    private Transform springObj;            //virtual obj where antenna tip should be
    private Rigidbody springRB;             //rigidbody of the virtual obj
    private Vector3 LocalDistance;          //distance between the two points
    private Vector3 LocalVelocity;          //velocity converted to local space

    void Start()
    {
        target.rotation = transform.rotation;

        springObj = new GameObject("Spring").transform;
        springObj.transform.position = target.transform.position;
        springObj.transform.rotation = target.transform.rotation;
        springRB = springObj.gameObject.AddComponent<Rigidbody>();
        springRB.mass = springMass;
        springRB.drag = 0;
        springRB.angularDrag = 0;
        springRB.useGravity = false;
        springRB.constraints = (RigidbodyConstraints)((int)RigidbodyConstraints.FreezeRotationX + (int)RigidbodyConstraints.FreezeRotationY + (int)RigidbodyConstraints.FreezeRotationZ);
    }

    void FixedUpdate()
    {
        //Sync the rotation 
        springRB.transform.rotation = this.transform.rotation;

        //Calculate the distance between the two points
        LocalDistance = target.InverseTransformDirection(target.position - springObj.position);
        springRB.AddRelativeForce((LocalDistance) * springForce);//Apply Spring

        //Calculate the local velocity of the springObj point
        LocalVelocity = (springObj.InverseTransformDirection(springRB.velocity));
        springRB.AddRelativeForce((-LocalVelocity) * drag);//Apply drag

        //Aim the visible geo at the spring target
        pivot.LookAt(springObj.position, -transform.forward);
    }
}

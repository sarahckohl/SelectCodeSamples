using UnityEngine;
using System.Collections;
using System.Linq;

public class PawAddForce : MonoBehaviour
{
    public int hitforce = 50;
    public float hitAngle = 25f;
    public float hitDistance = .5f;
    //this script did not previously have a layermask... should we make a hittable layer?
    public LayerMask hittable;
    public LayerMask grabbable;
    public static event System.Action<GameObject> onPawHit;
    private int grabhit;

    private bool _goingToHit = false;

    void Start() {
        grabhit = grabbable.value | hittable.value;
        PawAnimator.onHit += Pawhit;
    }

    void Update()
    {
        if (Input.GetButtonDown("Hit")) {
            _goingToHit = true;
        }

        if (Input.GetButton("Hit") && _goingToHit) {
            PawAnimator.hit();
        }
    }

    /// <summary>
    /// Checks if object is in player's "grab" field of view, defined by the angle grabAngle.
    /// </summary>
    /// <param name="anObject">The game object to be grabbed.</param>
    /// <returns>True if in grabAngle, otherwise false.</returns>
    public bool FieldofView(GameObject anObject)
    {
        bool FoV = false;

        Vector3 targetDir = anObject.transform.position - CharacterMovement.instance.eyes.transform.position;
        Vector3 forward = CharacterMovement.instance.eyes.transform.forward;
        float angle = Vector3.Angle(targetDir, forward);
        if (angle < hitAngle)
        {
            FoV = true;
        }

        return FoV;
    }

    /// <summary>
    /// Filters the input array, returning a new array of only those that satisfy FieldofView.
    /// </summary>
    /// <param name="grabbables">An array of candidate colliders.</param>
    /// <returns>Filtered array satisfying field of view.</returns>
    Collider[] coneConstraint(Collider[] grabbables)
    {
        return grabbables.Where(c => FieldofView(c.gameObject)).ToArray();
    }

    /// <summary>
    /// Returns the closest game object given a list of candidate COLLIDERS, in linear time.
    /// Returns NULL if no grabbale objects are in range.
    /// </summary>
    /// <param name="filteredGrabbables">An array of candidate objects. </param>
    /// <returns>The gameObject of the closest collider.</returns>
    GameObject closestGameObject(Collider[] filteredGrabbables)
    {

        if (filteredGrabbables.Count() < 1)
        {
            return null;
        }

        Collider closest = filteredGrabbables.First<Collider>();

        foreach (Collider candidate in filteredGrabbables)
        {
            if (Vector3.Distance(candidate.gameObject.transform.position, CharacterMovement.instance.eyes.transform.position)
                <
                Vector3.Distance(closest.gameObject.transform.position, CharacterMovement.instance.eyes.transform.position)
                )
            {
                closest = candidate;
            }

        }

        return closest.gameObject;

    }

    /// <summary>
    /// Uses a raycast to check if a non-grabbable object (such as a wall) is blocking LoS to the grabable object.
    /// </summary>
    /// <param name="grabbableObject">The object to be grabbed.</param>
    /// <returns>True if blocked, otherwise false.</returns>
    bool isOccluded(GameObject grabbableObject)
    {
        //A as origin and (pointB - pointA).normalized as direction
        Ray occlusionRay = new Ray(CharacterMovement.instance.eyes.transform.position, (grabbableObject.transform.position - CharacterMovement.instance.eyes.transform.position).normalized);
        RaycastHit occhit;

        if (Physics.Raycast(occlusionRay, out occhit, Vector3.Distance(CharacterMovement.instance.eyes.transform.position, grabbableObject.transform.position), ~(grabhit | (1 << LayerMask.NameToLayer("Ignore Raycast")))))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    void Pawhit()
    {
        _goingToHit = false;

        Vector3 cameraRelativeForward = CharacterMovement.instance.head.transform.forward;

        Collider[] inGrabRange = Physics.OverlapSphere(CharacterMovement.instance.eyes.transform.position, hitDistance, grabhit);
        inGrabRange = coneConstraint(inGrabRange);

        GameObject closest = closestGameObject(inGrabRange);

        if (closest == null || isOccluded(closest)) {
            RaycastHit hitInfo;
            if (Physics.Raycast(CharacterMovement.instance.eyes.transform.position, CharacterMovement.instance.eyes.transform.forward, out hitInfo, hitDistance, grabhit)) {
                closest = hitInfo.collider.gameObject;
            }
        }

        if (closest != null && !isOccluded(closest))
        {
            if (closest.GetComponent<Rigidbody>() != null)
            {
                //hit it with some force
                closest.GetComponent<Rigidbody>().useGravity = true;
                closest.GetComponent<Rigidbody>().AddForce(cameraRelativeForward * hitforce);
            }

            PhysicSoundGenerator soundGenerator = closest.GetComponentInParent<PhysicSoundGenerator>();
            if (soundGenerator != null) {
                soundGenerator.playPhysicSound(2.0f);
            }

            if (onPawHit != null && closest !=null)
            {
                onPawHit(closest);
            }
        }
    }
}
#define DEBUG

using UnityEngine;
using System.Collections;
using System.Linq;

public class MouseController : MonoBehaviour
{

    Mousehole[] mouseholes;
    Mousehole nextHole;
    public float speed = .2f;
    public float minimumWait = 1f;
    private NavMeshAgent agent;
    float pathlength;
    int visionBlock;
    public float hideDistance=1f;


    void Awake()
    {
        mouseholes = GameObject.FindObjectsOfType<Mousehole>();
        agent = gameObject.GetComponent<NavMeshAgent>();

    }

    // Use this for initialization
    void Start()
    {
        nextHole = GameObject.FindObjectOfType<Mousehole>();


        visionBlock = LayerMask.GetMask("BlockVision");
        GetComponent<MouseSounds>().delayBetweenSqueaks = 0;
        StartCoroutine(runToHoles());
        PawAddForce.onPawHit += onPawHitCallback;
        JawPickup.onJaw += onJawCallback;
    }

    // Update is called once per frame
    void Update()
    {

    }


    void OnDestroy()
    {
        PawAddForce.onPawHit -= onPawHitCallback;
        JawPickup.onJaw -= onJawCallback;
    }

    /// <summary>
    /// Spawns a deadMouse prefab which then drops to the ground.
    /// </summary>
    /// <param name="wasHit">The gameobject hit by paws, which should be mouse in this script.</param>
    void onPawHitCallback(GameObject wasHit)
    {    
        if (wasHit == gameObject)
        {
            Debug.Log("Mouse should die.");
            Object.Instantiate(Resources.Load("Prefabs/deadmouse"), gameObject.transform.position, gameObject.transform.rotation);
            Object.Destroy(gameObject);
        }
    }

    /// <summary>
    /// Spawns the deadmouse prefab in blue's mouth (i.e. JawPickup.hitObject)
    /// </summary>
    /// <param name="wasHit">The gameObject hit by jaws, which should be fly in this script.</param>
    void onJawCallback(GameObject wasHit)
    {
        if (wasHit == gameObject)
        {
            Debug.Log("Mouse should die.");
            CharacterMovement.instance.GetComponent<JawPickup>().hitObject = (GameObject)Object.Instantiate(Resources.Load("Prefabs/deadmouse"), gameObject.transform.position, gameObject.transform.rotation);
            Object.Destroy(gameObject);
        }
    }


    IEnumerator onPathInterrupt()
    {
        while (agent.pathStatus == NavMeshPathStatus.PathComplete)
        {
            yield return null;
        }
    
    }


    /// <summary>
    /// Sets a new destination and stops to wait for the path to finish calculating.  Updates pathLength to this new path.
    /// </summary>
    /// <returns>Pathing coroutine.</returns>
    IEnumerator WaitForPath()
    {


            agent.SetDestination(nextHole.transform.Find("Start").position);

			
        agent.Stop();

        while ( agent.pathPending)
        {
            yield return null;
        }

        /*custom, non-truncating version of remainingDistance*/
        pathlength = 0;
        for (int i = 0; i < agent.path.corners.Length - 1; i++)
        {
            if (agent.path.corners[i] == agent.path.corners[agent.path.corners.Length - 1])
            {
                break;
            }

            pathlength += Vector3.Distance(agent.path.corners[i], agent.path.corners[i + 1]);
        }

        

        agent.Resume();

    }


    /// <summary>
    /// Select a random hole, checking to make sure it is a different hole than the current one.
    /// </summary>
    /// <returns>Hole selection coroutine.</returns>
    IEnumerator selectHole()
    {


        int randomHoleNumber;

        int i = 0;
        do
        {
            randomHoleNumber = Random.Range(0, mouseholes.Length);

            i++;
            if (i > 100)
            {
                #if DEBUG
                Debug.Log("FUCK");
                #endif
                break;
            }
            

        }
        while (!mouseholes[randomHoleNumber].canGo() || nextHole == mouseholes[randomHoleNumber]);

        nextHole = mouseholes[randomHoleNumber];
#if DEBUG
        Debug.Log("Hole selected:" + nextHole.transform.parent.name);
#endif
        yield return StartCoroutine(WaitForPath());

    }



    IEnumerator selectHoleMistake()
    {
#if DEBUG
        Debug.Log("Select hole mistake");
#endif


        int randomHoleNumber;
        int i = 0;

        do
        {


            randomHoleNumber = Random.Range(0, mouseholes.Length);

            i++;
            if (i > 100)
            {
#if DEBUG
                Debug.Log("SHIT");
#endif
                break;
                
            }
        }
        while (nextHole == mouseholes[randomHoleNumber]);

        nextHole = mouseholes[randomHoleNumber];

        yield return StartCoroutine(WaitForPath());

    }


    /// <summary>
    /// Similar to select hole, this method picks a random hole to warp to,
    /// instead of just navigating to it.
    /// </summary>
    void warpToHole()
    {




        int randomHoleNumber;


        int i = 0;
        do
        {

            randomHoleNumber = Random.Range(0, mouseholes.Length);

            if (Mousehole.holesRemaining == 1)
                return;

            if (Mousehole.holesRemaining <= 0)
                return;

            i++;
            if (i > 100)
            {
                throw new System.Exception("warpToHole broke mouse.");
            }

        }
        while (!mouseholes[randomHoleNumber].canGo() || !agent.CalculatePath(mouseholes[randomHoleNumber].transform.position, agent.path));  


        nextHole = mouseholes[randomHoleNumber];

        agent.Warp(mouseholes[randomHoleNumber].transform.position);

    }


    /// <summary>
    /// Decide whether mouse is safe to go or on the current path. 
    /// </summary>
    /// <returns>False if mouse would be caught, otherwise true.</returns>
    bool pathIsSafe()
    {



        pathlength = 0;
        for (int i = 0; i < agent.path.corners.Length - 1; i++)
        {
            if (agent.path.corners[i] == agent.path.corners[agent.path.corners.Length - 1])
            {
                break;
            }

            pathlength += Vector3.Distance(agent.path.corners[i], agent.path.corners[i + 1]);

            float mouseDistanceTo = pathlength;
            float catDistanceTo = Vector3Extensions.horizontalDistance(CharacterMovement.instance.transform.position, agent.path.corners[i]);

            float mouseArrivalTime = mouseDistanceTo / agent.speed;
            float catArrivalTime = catDistanceTo / CharacterMovement.instance.fastMovementSpeed;

            if (catArrivalTime < mouseArrivalTime)
                return false;
            else
                continue;
        
        }

        return true;
    
    }


    /// <summary>
    /// Indefinitely runs through the mouse hiding/hook seeking behavior.
    /// 1. Pause for minimumWait
    /// 2. Stay in hole if Blue is nearby.
    /// 3. Pick a random new hole.
    /// 4. If path is safe, run to that hole.
    /// 5. Otherwise, start over.
    /// </summary>
    /// <returns>Mouse core behavior coroutine.</returns>
    IEnumerator runToHoles()
    {
        while (true)
        {
#if DEBUG
            Debug.Log(Mousehole.holesRemaining);
#endif
            if((Mousehole.holesRemaining) <=1 )
            {
            //mess up coroutine
#if DEBUG
                Debug.Log("Messup routine.");
#endif
                yield return new WaitForSeconds(minimumWait);

            yield return StartCoroutine(selectHoleMistake());
            agent.Resume();
            yield return StartCoroutine(runToHoleMistake());
            }
            else
            {
            // the usual stuff
            warpToHole();
            yield return new WaitForSeconds(minimumWait);
            yield return StartCoroutine(selectHole());

            if (agent.path.status != NavMeshPathStatus.PathComplete)
                continue;

            if (pathIsSafe())
            {
                
                agent.Resume();
#if DEBUG
                Debug.Log("K GO");
#endif
                yield return StartCoroutine(runToHole());

            }
            else
            {
                agent.Stop();
#if DEBUG
                Debug.Log("HELL naw");
#endif
                continue;
            }
            }

        }

    }

    /// <summary>
    /// Stay at current mousehole until Blue is a safe distance away. (1 unit default.)
    /// </summary>
    /// <returns>Returns when blue is at least hideDistance away.</returns>
    IEnumerator keepHiding()
    {
        while (Vector3.Distance(CharacterMovement.instance.transform.position, gameObject.transform.position) <= hideDistance)
        {
            yield return null;
        }

    }

    /// <summary>
    /// Waits for each frame that mouse is not at destination.
    /// </summary>
    /// <returns>Returns when mouse is at destination.</returns>
    IEnumerator runToHole()
    {

        while(!atDestination())
        {


            if (agent.pathStatus == NavMeshPathStatus.PathComplete)
                nextHole.setIsUnreachable(false);
            else
                nextHole.setIsUnreachable(true);

            if(agent.pathStatus != NavMeshPathStatus.PathComplete || nextHole.isBlocked)
            {
#if DEBUG
                Debug.Log("Rerouting while out of hole.");
#endif

#if DEBUG
                Debug.Log(Mousehole.holesRemaining);
#endif
                if (Mousehole.holesRemaining <= 1)
                {
                yield return StartCoroutine(selectHoleMistake());
                }
                else
                yield return StartCoroutine(selectHole());
  
            }

            yield return null;
        }


    }


    /// <summary>
    /// Waits for each frame that mouse is not at destination.
    /// </summary>
    /// <returns>Returns when mouse is at destination.</returns>
    IEnumerator runToHoleMistake()
    {

        int i = 0;
        while (!atDestination())
        {
            yield return null;
        }


    }


    /// <summary>
    /// Checks whether mouse has arrived at destination, based on built-in NavAgent parameters.
    /// </summary>
    /// <returns>True if within stopping distance of destination, otherwise false.</returns>
    bool atDestination()
    {
        if (!agent.pathPending)
        {
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                {
                    return true;
                }
            }
        }

        return false;
    
        
    }



    /// <summary>
    /// Checks if anything on the visionblock layer (e.g. walls, floors, some furniture) is in the way of the mouse's vision.
    /// </summary>
    /// <returns>True if not blocked, false if blocked.</returns>
    public bool hasLineofSight()
    {
        RaycastHit hit;

        Ray sightRay = new Ray(gameObject.transform.position, (CharacterMovement.instance.transform.position - gameObject.transform.position).normalized);

        if (Physics.Raycast(sightRay, out hit, visionBlock))
        {
            return false;
        }


        return true;
    }


}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public enum AI_ENEMY_STATE
{
    IDLE = 2081823275,
    PATROL = 207038023,
    CHASE = 1463555229,
    ATTACK = 1080829965,
    FLEE = -73080692,
    DYE = 1274522470
};

public class AI_Enemy : MonoBehaviour
{
    [SerializeField]
    private AI_ENEMY_STATE CurrentState = AI_ENEMY_STATE.IDLE;
    public float AttackDamage = 15.0f;

    float Health = 100.0f;
    float HealthDangerLevel = 25.0f;
    [SerializeField]
    GameObject UICanvas;
    [SerializeField]
    GameObject HealthBar;

    Animator animator;
    NavMeshAgent agent;
    BoxCollider viewCollider;
    public Transform playerTransform;
    [SerializeField]
    bool CanSeePlayer = false;

    AudioSource audioSource;

    public AudioClip[] FootStepClips;
    int CurrentStep;

    Transform[] WayPoints;
    const float DistEps = 2.0f;
    const float FieldOfView = 45;
    const float ChaseTimeOut = 2.0f;
    const float AttackDelay = 1.0f;

    private void Awake()
    {
        CurrentStep = 0;

        audioSource = GetComponent<AudioSource>();

        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        viewCollider = GetComponent<BoxCollider>();

        if (playerTransform == null)
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        }

        GameObject[] objects = GameObject.FindGameObjectsWithTag("WayPoint");

        WayPoints = (from GameObject GO in objects select GO.transform).ToArray();
    }
    private void Update()
    {
        CanSeePlayer = false;

        if (viewCollider.bounds.Contains(playerTransform.position))
        {
            CanSeePlayer = HaveLineSightToPlayer();
        }

        UICanvas.transform.LookAt(playerTransform.position + playerTransform.up * 0.9f);
    }
    public void ChangeHealth(float Amount)
    {
        if (Health <= 0)
            return;

        Health += Amount;

        HealthBar.transform.localScale = new Vector3(Health / 100.0f, 1, 1);

        if (Health <= 0)
        {
            SetCurrentState(AI_ENEMY_STATE.DYE);
            agent.isStopped = true;
            Destroy(gameObject, 3);
            return;
        }

        if (Health < HealthDangerLevel)
        {
            StopAllCoroutines();
            StartCoroutine(StateFlee());
        }
    }
    private bool HaveLineSightToPlayer()
    {
        float Angle = Mathf.Abs(Vector3.Angle(transform.forward, (playerTransform.position - transform.position).normalized));

        if (Angle > FieldOfView)
        {
            return false;
        }

        if (Physics.Linecast(transform.position, playerTransform.position))
        {
            return false;
        }

        return true;
    }
    public void SetCurrentState(AI_ENEMY_STATE state)
    {
        CurrentState = state;
        animator.SetTrigger((int)state);
    }
    public IEnumerator StateIdle()
    {
        SetCurrentState(AI_ENEMY_STATE.IDLE);

        agent.isStopped = true;

        while (CurrentState == AI_ENEMY_STATE.IDLE)
        {
            if (CanSeePlayer)
            {
                StartCoroutine(StateChase());
                yield break;
            }
            yield return null;
        }
    }
    public void OnIdleAnimationCompleted()
    {
        StopAllCoroutines();
        StartCoroutine(StatePatrol());
    }
    public void OnFootStep()
    {
        audioSource.PlayOneShot(FootStepClips[CurrentStep]);
        CurrentStep = (++CurrentStep) % FootStepClips.Length;
    }
    private IEnumerator StatePatrol()
    {
        SetCurrentState(AI_ENEMY_STATE.PATROL);

        Transform RandomDest = WayPoints[UnityEngine.Random.Range(0, WayPoints.Length)];

        agent.SetDestination(RandomDest.position);
        agent.isStopped = false;
        agent.speed = 0.3f;
        agent.angularSpeed = 90;

        while (CurrentState == AI_ENEMY_STATE.PATROL)
        {
            if (CanSeePlayer)
            {
                StartCoroutine(StateChase());
                yield break;
            }

            if (Vector3.Distance(transform.position, RandomDest.position) <= DistEps)
            {
                StartCoroutine(StateIdle());
                yield break;
            }

            yield return null;
        }
    }
    private IEnumerator StateChase()
    {
        SetCurrentState(AI_ENEMY_STATE.CHASE);

        while (CurrentState == AI_ENEMY_STATE.CHASE)
        {
            agent.SetDestination(playerTransform.position);
            agent.isStopped = false;
            agent.speed = 3.0f;
            agent.angularSpeed = 150;

            if (!CanSeePlayer)
            {
                float Elapsedtime = 0.0f;
                while (true)
                {
                    Elapsedtime += Time.deltaTime;

                    agent.SetDestination(playerTransform.position);

                    if (CanSeePlayer)
                    {
                        break;
                    }

                    if (Elapsedtime >= ChaseTimeOut)
                    {
                        StartCoroutine(StateIdle());
                        yield break;
                    }

                    yield return null;
                }
            }
            if (Vector3.Distance(transform.position, playerTransform.position) <= DistEps)
            {
                StartCoroutine(StateAttack());
                yield break;
            }

            yield return null;
        }
    }
    private IEnumerator StateAttack()
    {
        SetCurrentState(AI_ENEMY_STATE.ATTACK);

        agent.SetDestination(transform.position);
        agent.isStopped = true;

        float ElapsedTime = 0.0f;

        playerTransform.SendMessage("ChangeHealth", -AttackDamage, SendMessageOptions.DontRequireReceiver);

        while (CurrentState == AI_ENEMY_STATE.ATTACK)
        {
            ElapsedTime += Time.deltaTime;

            if (!CanSeePlayer || Vector3.Distance(transform.position, playerTransform.position) > DistEps)
            {
                StartCoroutine(StateChase());
                yield break;
            }

            if (ElapsedTime >= AttackDelay)
            {
                ElapsedTime = 0.0f;
                playerTransform.SendMessage("ChangeHealth", -AttackDamage, SendMessageOptions.DontRequireReceiver);
            }

            yield return null;
        }
    }
    private IEnumerator StateFlee()
    {
        SetCurrentState(AI_ENEMY_STATE.FLEE);
        agent.isStopped = false;
        agent.speed = 3.0f;

        HealthRestore HR = null;

        while (CurrentState == AI_ENEMY_STATE.FLEE)
        {
            if (HR == null)
            {
                HR = GetNearestHealthRestore(transform);
                agent.SetDestination(HR.transform.position);
            }

            if (HR == null || Health > HealthDangerLevel)
            {
                StartCoroutine(StateIdle());
                yield break;
            }
            yield return null;
        }
    }
    private HealthRestore GetNearestHealthRestore(Transform Target)
    {
        HealthRestore[] Restores = GameObject.FindObjectsOfType<HealthRestore>();

        float DistanceToNearest = Mathf.Infinity;

        HealthRestore Nearest = null;

        foreach (HealthRestore HR in Restores)
        {
            float CurrentDistance = Vector3.Distance(Target.position, HR.transform.position);

            if (CurrentDistance < DistanceToNearest)
            {
                Nearest = HR;
                DistanceToNearest = CurrentDistance;
            }
        }

        return Nearest;
    }
}

namespace Charater
{
    // # System
    using System.Collections;
    using System.Collections.Generic;
    using Unity.VisualScripting;

    // # Unity
    using UnityEngine;
    using UnityEngine.AI;
    using UnityEngine.PlayerLoop;
    using UnityEngine.UI;
    [RequireComponent(typeof(Rigidbody))]
    public class Character : MonoBehaviour
    {
        #region enum
        protected enum Team
        {
            blue,
            red
        }
        protected enum State
        {
            Idle,
            Trace,
            IsAttack,
            Die,
            Winner
        }
        #endregion

        [Header("State")]
        [SerializeField] protected Team team;
        [SerializeField] protected State state;
        [SerializeField] protected float speed;
        [SerializeField] protected GameObject curEnemy;
        [SerializeField] protected float enemyDistance;
        private bool isFindEnemyCo = true;

        [Header("Stat")]
        [SerializeField] protected float attackRange;
        [SerializeField] protected float attackSpeed;
        [SerializeField] protected int damage;
        [SerializeField] protected int maxHp;
        [SerializeField] protected int curHp;

        [Header("NavMeshAgent")]
        [SerializeField] protected NavMeshAgent nmAgent;

        [Header("Animation")]
        Animator animator;
        protected readonly int hashIsRun = Animator.StringToHash("IsRun");
        protected readonly int hashAttack = Animator.StringToHash("Attack");
        protected readonly int hashDeath = Animator.StringToHash("Death");

        #region Unity_Function

        private void Awake()
        {
            nmAgent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
        }

        private void Start()
        {
            if (team == Team.blue)
            {
                GameManager.instance.blueUnits.Add(gameObject);
            }
            else if (team == Team.red)
            {
                GameManager.instance.redUnits.Add(gameObject);
            }
            StartCoroutine(Co_FindEnemy());
            curHp = maxHp;
        }

        private void Update()
        {
            attackSpeed -= Time.deltaTime; // 공격속도 돌리기
            CheckState();
            ChangeState();

            if (isFindEnemyCo == false)
            {
                StartCoroutine(Co_FindEnemy());
            }
        }

        private void LateUpdate()
        {
            if(state != State.Die && isFindEnemyCo == true && curEnemy == null)
            {
                StopCoroutine(Co_FindEnemy());
                StartCoroutine(Co_FindEnemy());
            }
        }
        #endregion

        #region Private_Function
        private void CheckState()
        {
            switch (state)
            {
                case State.Trace:
                    animator.SetBool(hashIsRun, true);
                    FollowEnemy();
                    break;
                case State.IsAttack:
                    animator.SetBool(hashIsRun, false);
                    if (attackSpeed <= 0)
                    {
                        Attack();
                    }
                    break;
                case State.Die:
                    StopCoroutine(Co_FindEnemy());
                    StartCoroutine(Co_DeathAnim());
                    break;
                case State.Winner:
                    break;
            }
        }

        /// <summary>
        /// 현재 상황에 맞는 상태로 전환시켜줌
        /// </summary>
        private void ChangeState()
        {
            if (curEnemy != null)
            {
                enemyDistance = Vector3.Distance(curEnemy.transform.position, transform.position);
                transform.LookAt(curEnemy.transform.position);

                if (enemyDistance <= attackRange) // 공격 범위 안에 적이 들어왔을 때
                {
                    state = State.IsAttack;
                }
                if (enemyDistance > attackRange) // 공격 범위 밖으로 적이 나갔을 때
                {
                    state = State.Trace;
                }
                if (curHp <= 0) // 체력이 0 이거나 아래로 내려갔을 때
                {
                    if (team == Team.red) // 레드팀이라면
                    {
                        GameManager.instance.redUnits.Remove(gameObject);
                    }
                    if (team == Team.blue) // 블루팀이라면
                    {
                        GameManager.instance.blueUnits.Remove(gameObject);
                    }
                    state = State.Die;
                }
                if (GameManager.instance.blueWin) // 블루팀이 승리했을 때
                {
                    if (team == Team.blue) // 블루팀이라면
                    {
                        state = State.Winner;
                    }
                }
                if (GameManager.instance.redWin) // 레드팀이 승리했을 때
                {
                    if (team == Team.red) // 레드팀이라면
                    {
                        state = State.Winner;
                    }
                }
            }
        }

        /// <summary>
        /// 적 따라가기
        /// </summary>
        private void FollowEnemy()
        {
            if (curEnemy != null)
            {
                nmAgent.SetDestination(curEnemy.transform.position);
            }
        }
        private void Attack()
        { 
            if(state != State.Die)
            animator.SetTrigger(hashAttack);
        }
        private void TakeDamage(int damage)
        {
            curHp -= damage;
        }
        #endregion

        #region Public_Function
        public void HitAttack()
        {
            if (curEnemy != null)
            {
                curEnemy.GetComponent<Character>().TakeDamage(damage);
            }
        }
        #endregion

        #region Coroutine_Function
        /// <summary>
        /// 적 찾기
        /// </summary>
        IEnumerator Co_FindEnemy()
        {
            isFindEnemyCo = true;
            yield return null;
            float curDistanceToTarger = float.MaxValue;

            if (team == Team.blue)
            {
                foreach (GameObject enemy in GameManager.instance.redUnits)
                {
                    float distanceToTarger = Vector2.Distance(enemy.transform.position, transform.position);
                    if (distanceToTarger <= curDistanceToTarger)
                    {
                        curEnemy = enemy;

                        curDistanceToTarger = distanceToTarger;
                    }
                }
            }
            if (team == Team.red)
            {
                foreach (GameObject enemy in GameManager.instance.blueUnits)
                {
                    float distanceToTarger = Vector2.Distance(enemy.transform.position, transform.position);
                    if (distanceToTarger <= curDistanceToTarger)
                    {
                        curEnemy = enemy;

                        curDistanceToTarger = distanceToTarger;
                    }
                }
            }
            yield return new WaitForSeconds(5f);
            yield return isFindEnemyCo = false;
            if (curEnemy == null || enemyDistance > attackRange)
            {
                StartCoroutine(Co_FindEnemy());
            }
        }

        IEnumerator Co_DeathAnim()
        {
            animator.SetTrigger(hashDeath);
            yield return new WaitForSeconds(0.05f);
            float curAnimationTime =animator.GetCurrentAnimatorStateInfo(0).length;
            yield return new WaitForSeconds(curAnimationTime);
            Destroy(gameObject);
        }
        #endregion
    }
}
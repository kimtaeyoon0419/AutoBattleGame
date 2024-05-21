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
            attackSpeed -= Time.deltaTime; // ���ݼӵ� ������
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
        /// ���� ��Ȳ�� �´� ���·� ��ȯ������
        /// </summary>
        private void ChangeState()
        {
            if (curEnemy != null)
            {
                enemyDistance = Vector3.Distance(curEnemy.transform.position, transform.position);
                transform.LookAt(curEnemy.transform.position);

                if (enemyDistance <= attackRange) // ���� ���� �ȿ� ���� ������ ��
                {
                    state = State.IsAttack;
                }
                if (enemyDistance > attackRange) // ���� ���� ������ ���� ������ ��
                {
                    state = State.Trace;
                }
                if (curHp <= 0) // ü���� 0 �̰ų� �Ʒ��� �������� ��
                {
                    if (team == Team.red) // �������̶��
                    {
                        GameManager.instance.redUnits.Remove(gameObject);
                    }
                    if (team == Team.blue) // �������̶��
                    {
                        GameManager.instance.blueUnits.Remove(gameObject);
                    }
                    state = State.Die;
                }
                if (GameManager.instance.blueWin) // �������� �¸����� ��
                {
                    if (team == Team.blue) // �������̶��
                    {
                        state = State.Winner;
                    }
                }
                if (GameManager.instance.redWin) // �������� �¸����� ��
                {
                    if (team == Team.red) // �������̶��
                    {
                        state = State.Winner;
                    }
                }
            }
        }

        /// <summary>
        /// �� ���󰡱�
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
        /// �� ã��
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
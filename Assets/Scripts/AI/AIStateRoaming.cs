using System.Collections;
using BLINK.RPGBuilder.Combat;
using UnityEngine;
namespace BLINK.RPGBuilder.AI
{
    public class AIStateRoaming : AIStateIdle
    {
        private AIStateRoamingTemplate roamingTemplate;
        private float timeSinceLastPositionCheck = 0f;
        private Vector3 previousPosition;
        private Vector3 spawnedPosition;


        protected float NextRoamPositionCheck;
        protected Vector3 RoamTargetPosition;

        protected float RoamingPauseTimeLeft;
        protected bool IsRoamingPaused;
        protected static readonly int walking = Animator.StringToHash("Walking");

        public override void Initialize(AIEntity entity, AIStateTemplate template)
        {
            ThisAIEntity = entity;
            roamingTemplate = (AIStateRoamingTemplate)template;
            ThisAIEntity.GetActiveStates()[GetName()].StateLoop = ThisAIEntity.StartCoroutine(StateLoop());
        }

        public override AIState Execute()
        {
            CombatEntity newTarget = ThisAIEntity.SearchTarget(roamingTemplate.viewDistance, roamingTemplate.viewAngle, roamingTemplate.AutoAggroDistance);
            if (newTarget != null)
            {
                ThisAIEntity.ThisCombatEntity.SetTarget(newTarget);
                return ThisAIEntity.GetChaseState();
            }

            if (ThisAIEntity.IsPlayerInteractionState()) return this;
            HandleRoaming();

            float distanceToTarget = Vector3.Distance(ThisAIEntity.transform.position, RoamTargetPosition);
            if (!IsRoamingPaused && distanceToTarget > roamingTemplate.RoamPointThreshold && distanceToTarget > roamingTemplate.MinDistanceToMove)
            {
                ThisAIEntity.MoveAgent(RoamTargetPosition);
            }
            else
            {
                ThisAIEntity.ResetMovement();
            }

            timeSinceLastPositionCheck += Time.deltaTime;
            if (timeSinceLastPositionCheck >= 5f)
            {
                float distanceMoved = Vector3.Distance(previousPosition, ThisAIEntity.transform.position);
                if (distanceMoved < 3f)
                {
                    Debug.Log("AI has moved less than 3 meters in the last 5 seconds. Finding a new valid point and making the AI walk there.");
                    GetNewRoamingPoint();
                }
                else
                {
                    previousPosition = ThisAIEntity.transform.position;
                }
                timeSinceLastPositionCheck = 0f;
            }

            return this;
        }




        public override void Enter()
        {
            IsActive = true;
            InitMovement();
            ThisAIEntity.EntityAnimator.SetBool(walking, true);
            ThisAIEntity.EntityAgent.enabled = true;
            ThisAIEntity.EntityAgent.speed = ThisAIEntity.GetMovementSpeed() * roamingTemplate.MovementSpeedModifier;
            ThisAIEntity.EntityAgent.acceleration = ThisAIEntity.EntityAgent.speed;
            ThisAIEntity.EntityAgent.updateRotation = true;
            MovementStateBlendCompleted = false;
            NextRoamPositionCheck = 0;
            GetNewRoamingPoint();

            spawnedPosition = ThisAIEntity.transform.position; // Store the spawned position
        }


        public override void Exit()
        {
            IsActive = false;
            ThisAIEntity.EntityAnimator.SetBool(walking, false);
            MovementStateBlendCompleted = true;

            ThisAIEntity.EntityAgent.velocity = Vector3.zero;
            ThisAIEntity.EntityAgent.speed = ThisAIEntity.GetMovementSpeed();
            ThisAIEntity.EntityAgent.acceleration = ThisAIEntity.EntityAgent.speed;
        }

        public override void UpdateMovementSpeed()
        {
            ThisAIEntity.EntityAgent.speed = ThisAIEntity.GetMovementSpeed() * roamingTemplate.MovementSpeedModifier;
            ThisAIEntity.EntityAgent.acceleration = ThisAIEntity.EntityAgent.speed;
        }

        public override void InitMovement()
        {
            ThisAIEntity.SetHorizontalMovement(0);
            ThisAIEntity.SetVerticalMovement(1);
            MovementStateBlendCompleted = false;
            ThisAIEntity.SetMovementSpeedModifier(roamingTemplate.MovementSpeedModifier);
        }

        protected virtual void HandleRoaming()
        {
            RoamingPauseTimeLeft -= Time.deltaTime;
            if (!(Time.timeSinceLevelLoad >= NextRoamPositionCheck)) return;
            NextRoamPositionCheck = Time.timeSinceLevelLoad + roamingTemplate.roamTargetCheckInterval;

            if (!IsRoamingPaused && NearRoamTarget(roamingTemplate.RoamPointThreshold))
            {
                if (roamingTemplate.PauseDuration > 0)
                {
                    IsRoamingPaused = true;
                    RoamingPauseTimeLeft = roamingTemplate.PauseDuration;
                    ThisAIEntity.ResetMovement();
                }
                else
                {
                    GetNewRoamingPoint();
                }
            }

            if (!(roamingTemplate.PauseDuration > 0) || !IsRoamingPaused || !(RoamingPauseTimeLeft < 0)) return;
            RoamingPauseTimeLeft = 0;
            IsRoamingPaused = false;
            GetNewRoamingPoint();
        }

        protected virtual void GetNewRoamingPoint()
        {
            if (roamingTemplate.roamAroundSpawner && ThisAIEntity.ThisCombatEntity.GetSpawner() != null)
            {
                RoamTargetPosition = GetValidPoint(ThisAIEntity.ThisCombatEntity.GetSpawner().transform.position, ThisAIEntity.ThisCombatEntity.GetSpawner().areaHeight,
                    ThisAIEntity.ThisCombatEntity.GetSpawner().groundLayers);
            }
            else
            {
                RoamTargetPosition = GetValidPoint(ThisAIEntity.transform.position, 40,
                    roamingTemplate.roamGroundLayers);
            }
            ThisAIEntity.StartMovement();
        }

        protected virtual Vector3 GetValidPoint(Vector3 basePosition, float height, LayerMask groundLayers)
        {
            Vector3 pos;
            int maxAttempts = 5;
            int attempts = 0;
            float timeLimit = 1f;
            bool foundPoint = false;

            while (attempts < maxAttempts && !foundPoint)
            {
                attempts++;
                Debug.Log($"[AIStateRoaming] Attempt {attempts} to find a valid point.");

                pos = TryGetValidPoint(basePosition, height, groundLayers, timeLimit);

                if (pos != basePosition)
                {
                    foundPoint = ThisAIEntity.IsPathAllowed(pos);
                }

                if (foundPoint)
                {
                    return pos;
                }
            }

            Debug.LogWarning("[AIStateRoaming] Failed to find a valid point after 5 attempts. Destroying AI entity.");
            Destroy(ThisAIEntity.gameObject);
            return basePosition;
        }

        private Vector3 TryGetValidPoint(Vector3 basePosition, float height, LayerMask groundLayers, float timeLimit)
        {
            float startTime = Time.realtimeSinceStartup;
            Vector3 pos = new Vector3();

            while (Time.realtimeSinceStartup - startTime < timeLimit)
            {
                pos = GetPoint(basePosition, height, groundLayers);
                if (pos != basePosition)
                {
                    break;
                }
            }

            if (pos == basePosition)
            {
                Debug.LogWarning("[AIStateRoaming] Failed to find a valid point within the time limit.");
            }

            return pos;
        }

        private Vector3 GetPoint(Vector3 basePosition, float height, LayerMask groundLayers)
        {
            Vector3 pos = new Vector3(
                Random.Range(basePosition.x - roamingTemplate.RoamDistance, basePosition.x + roamingTemplate.RoamDistance),
                basePosition.y + height,
                Random.Range(basePosition.z - roamingTemplate.RoamDistance, basePosition.z + roamingTemplate.RoamDistance));
            return Physics.Raycast(pos, -ThisAIEntity.transform.up, out var hit, height * 2, groundLayers) ? hit.point : basePosition;
        }

        protected virtual bool NearRoamTarget(float treshold)
        {
            return Vector3.Distance(ThisAIEntity.transform.position,
                new Vector3(RoamTargetPosition.x, ThisAIEntity.transform.position.y, RoamTargetPosition.z)) <= treshold;
        }

        protected override IEnumerator StateLoop()
        {
            while (true)
            {
                if (IsActive)
                {
                    if (!MovementStateBlendCompleted)
                    {
                        MovementStateBlendCompleted = ThisAIEntity.HandleMovementDirectionsBlend();
                    }
                }

                yield return new WaitForFixedUpdate();
            }
        }
    }
}


using System.Collections.Generic;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

namespace UncertainLuei.BaldiPlus.RecommendedChars
{
    public class ArtsWithWires : NPC
    {
        public SpriteRenderer sprite;

        public Sprite sprNormal;
        public Sprite sprAngry;

        public AudioManager audMan;
        public SoundObject audIntro;
        public SoundObject audLoop;

        public GrabbingGame gamePrefab;

        public float wanderSpeed = 26f;
        public float chargeSpeed = 30f;
        public float grip = 55f;

        public bool stareStacks;
        public float stareTime;

        public bool CooldownActive { get; private set; } = true;
        private float cooldown = 0f;

        public override void Initialize()
        {
            base.Initialize();
            stareTime = 0f;
            navigator.SetSpeed(wanderSpeed);
            behaviorStateMachine.ChangeState(new ArtsWithWires_Wandering(this));
        }

        public void ToggleAngry(bool angry)
        {
            audMan.FlushQueue(true);
            if (angry)
            {
                sprite.sprite = sprAngry;
                audMan.QueueAudio(audIntro);
                audMan.QueueAudio(audLoop);
                audMan.loop = true;
                return;
            }
            sprite.sprite = sprNormal;
        }

        public void SetCooldown(float time)
        {
            if (cooldown < time)
                cooldown = time;
            CooldownActive = true;
        }

        public void TickCooldown()
        {
            cooldown -= Time.deltaTime * TimeScale;
            if (cooldown <= 0)
                CooldownActive = false;
        }

        public void ReleasePlayer(PlayerManager player)
        {
            navigator.SetSpeed(wanderSpeed);
            ToggleAngry(false);
            SetCooldown(30f);
            behaviorStateMachine.ChangeState(new ArtsWithWires_Fleeing(this, player));
        }

        public override void Despawn()
        {
            if (behaviorStateMachine.CurrentState is ArtsWithWires_Grabbing grabbing)
                grabbing.End();

            base.Despawn();
        }
    }

    public class ArtsWithWires_StateBase : NpcState
    {
        protected readonly ArtsWithWires wires;

        public ArtsWithWires_StateBase(ArtsWithWires wires) :
            base(wires)
        {
            this.wires = wires;
        }
    }

    public class ArtsWithWires_Wandering : ArtsWithWires_StateBase
    {
        public ArtsWithWires_Wandering(ArtsWithWires wires) :
            base(wires)
        {
        }

        public override void Enter()
        {
            base.Enter();
            ChangeNavigationState(new NavigationState_WanderRandom(npc, 0));
        }

        public override void DestinationEmpty()
        {
            base.DestinationEmpty();
            ChangeNavigationState(new NavigationState_WanderRandom(npc, 0));
        }

        public override void PlayerInSight(PlayerManager player)
        {
            base.PlayerInSight(player);
            if (!wires.CooldownActive)
                npc.behaviorStateMachine.ChangeState(new ArtsWithWires_PlayerInSight(wires));
        }

        public override void Update()
        {
            base.Update();

            if (wires.CooldownActive)
                wires.TickCooldown();
        }
    }

    public class ArtsWithWires_PlayerInSight : ArtsWithWires_StateBase
    {
        public ArtsWithWires_PlayerInSight(ArtsWithWires wires) :
            base(wires)
        {
        }

        public override void Enter()
        {
            base.Enter();
            ChangeNavigationState(new NavigationState_DoNothing(npc, 0));
        }

        public override void InPlayerSight(PlayerManager player)
        {
            base.InPlayerSight(player);
            wires.stareTime += Time.deltaTime * npc.TimeScale;
            if (!player.Tagged && wires.stareTime >= 3F)
            {
                wires.ToggleAngry(true);
                npc.behaviorStateMachine.ChangeState(new ArtsWithWires_Chasing(wires, player));
            }
        }

        public override void PlayerLost(PlayerManager player)
        {
            npc.behaviorStateMachine.ChangeState(new ArtsWithWires_Wandering(wires));
        }

        public override void Unsighted()
        {
            if (!wires.stareStacks)
                wires.stareTime = 0f;
        }
    }

    public class ArtsWithWires_Chasing : ArtsWithWires_StateBase
    {
        protected readonly PlayerManager player;
        protected float chaseTime;
        private NavigationState_TargetPlayer targetState;

        public ArtsWithWires_Chasing(ArtsWithWires wires, PlayerManager player, float chaseTime = 15f) :
            base(wires)
        {
            this.player = player;
            this.chaseTime = chaseTime;
            targetState = new NavigationState_TargetPlayer(npc, 63, player.transform.position);
        }

        public override void Enter()
        {
            base.Enter();
            wires.stareTime = 0f;
            npc.navigator.SetSpeed(wires.chargeSpeed);
            ChangeNavigationState(targetState);
        }

        public override void Exit()
        {
            base.Exit();
        }

        public override void DestinationEmpty()
        {
            base.DestinationEmpty();
            ChangeNavigationState(new NavigationState_WanderRandom(npc, 0));
        }

        public override void PlayerLost(PlayerManager player)
        {
            base.PlayerLost(player);
            if (player == this.player)
                ChangeNavigationState(new NavigationState_WanderRandom(npc, 0));
        }

        public override void PlayerInSight(PlayerManager player)
        {
            base.PlayerInSight(player);
            if (player == this.player)
            {
                ChangeNavigationState(targetState);
                targetState.UpdatePosition(player.transform.position);
            }
        }

        public override void Update()
        {
            base.Update();
            chaseTime -= Time.deltaTime * npc.TimeScale;
            if (chaseTime <= 0f)
            {
                npc.navigator.SetSpeed(wires.wanderSpeed);
                wires.ToggleAngry(false);
                wires.SetCooldown(30f);
                npc.behaviorStateMachine.ChangeState(new ArtsWithWires_Wandering(wires));
            }
        }

        public override void OnStateTriggerEnter(Collider other)
        {
            base.OnStateTriggerEnter(other);
            if (other.CompareTag("Player") && other.transform == player.transform)
            {
                if (!player.plm.Entity.resistAddend)
                    npc.behaviorStateMachine.ChangeState(new ArtsWithWires_Grabbing(wires, player));
                else
                    npc.behaviorStateMachine.ChangeState(new ArtsWithWires_KeepingDistanceWithinPlayer(wires, player, chaseTime));
            }
        }
    }

    public class ArtsWithWires_KeepingDistanceWithinPlayer : ArtsWithWires_Chasing
    {
        public ArtsWithWires_KeepingDistanceWithinPlayer(ArtsWithWires wires, PlayerManager player, float chaseTime) : base(wires, player, chaseTime)
        {
        }

        public override void Enter()
        {
            // Temporarily flee from player until they're no longer touching A&W
            ChangeNavigationState(new NavigationState_WanderFlee(wires, 32, player.dijkstraMap));
        }

        public override void PlayerInSight(PlayerManager player)
        {
        }

        public override void OnStateTriggerEnter(Collider other)
        {
        }

        public override void OnStateTriggerExit(Collider other)
        {
            base.OnStateTriggerExit(other);
            // Revert back to 
            if (other.CompareTag("Player") && other.transform == player.transform)
                npc.behaviorStateMachine.ChangeState(new ArtsWithWires_Chasing(wires, player, chaseTime));
        }
    }

    public class ArtsWithWires_Grabbing : ArtsWithWires_StateBase
    {
        private GrabbingGame game;
        private readonly PlayerManager player;

        private readonly Transform playerTransform;
        private readonly Transform wiresTransform;

        public ArtsWithWires_Grabbing(ArtsWithWires wires, PlayerManager player) :
            base(wires)
        {
            this.player = player;

            playerTransform = player.transform;
            wiresTransform = wires.transform;
        }

        public override void Enter()
        {
            base.Enter();
            game = Object.Instantiate(wires.gamePrefab);
            game.player = player;
            game.wires = wires;

            ChangeNavigationState(new NavigationState_DoNothing(npc, 0));
        }

        public override void PlayerInSight(PlayerManager player)
        {
            base.PlayerInSight(player);
            if (player == this.player && Vector3.Distance(playerTransform.position, wiresTransform.position) > 5f)
                game.End(false);
        }

        public override void PlayerLost(PlayerManager player)
        {
            base.PlayerLost(player);
            if (player == this.player)
                game.End(false);
        }

        public void End()
        {
            game.End(false);
        }
    }

    public class ArtsWithWires_Fleeing : ArtsWithWires_Wandering
    {
        private readonly PlayerManager player;

        public ArtsWithWires_Fleeing(ArtsWithWires wires, PlayerManager player) :
            base(wires)
        {
            this.player = player;
        }

        public override void Enter()
        {
            ChangeNavigationState(new NavigationState_WanderFlee(npc, 63, player.dijkstraMap));
        }

        public override void DestinationEmpty()
        {
            npc.behaviorStateMachine.ChangeState(new ArtsWithWires_Wandering(wires));
        }
    }

    public class GrabbingGameContainer : MonoBehaviour
    {
        public List<GrabbingGame> grabbingGames = new List<GrabbingGame>();
        public PlayerManager pm;

        public bool TryCutGrabbingGames()
        {
            bool success = false;
            for (int i = grabbingGames.Count - 1; i >= 0; i--)
            {
                success = true;
                grabbingGames[i]?.End(false);
            }
            return success;
        }
    }

    public class GrabbingGame : MonoBehaviour
    {
        private static Dictionary<PlayerManager, List<GrabbingGame>> grabbingGames = new Dictionary<PlayerManager, List<GrabbingGame>>();

        private readonly MovementModifier moveMod = new MovementModifier(default, 0f);

        public Canvas textCanvas;
        public CanvasScaler textScaler;
        public TMP_Text instructionsTmp;

        public PlayerManager player;
        private GrabbingGameContainer playerGrabbingGames;

        public ArtsWithWires wires;

        public RectTransform needle;

        private const float needleXMin = -65f;
        private const float needleXLength = 130f;
        private Vector3 needlePosition;

        private float grabState = 50f;
        private float grabHit = 15f;
        private float grabMax = 100f;

        private void Start()
        {
            // Add this grabbing game
            if (!player.TryGetComponent(out playerGrabbingGames))
            {
                playerGrabbingGames = player.gameObject.AddComponent<GrabbingGameContainer>();
                playerGrabbingGames.pm = player;
            }
            playerGrabbingGames.grabbingGames.Add(this);

            player.am.moveMods.Add(moveMod);

            textCanvas.worldCamera = CoreGameManager.Instance.GetCamera(player.playerNumber).canvasCam;
            textCanvas.transform.SetParent(null);
            textCanvas.transform.position = Vector3.zero;
            if (!PlayerFileManager.Instance.authenticMode)
                textScaler.scaleFactor = Mathf.RoundToInt(PlayerFileManager.Instance.resolutionY / 360f);

            instructionsTmp.text = string.Format(LocalizationManager.Instance.GetLocalizedText("RecChars_Wires_Instructions"), InputManager.Instance.GetInputButtonName("Interact", "InGame", false));

            needlePosition = needle.anchoredPosition;
        }

        private void Update()
        {
            grabState -= wires.grip * Time.deltaTime;
            if (Singleton<InputManager>.Instance.GetDigitalInput("Interact", true))
                grabState += grabHit * Time.timeScale * player.playerTimeScale;

            if (grabState > grabMax)
            {
                End(true);
                return;
            }
            
            if (grabState < 0f)
                grabState = 0f;

            UpdateNeedlePosition();
        }   

        private void UpdateNeedlePosition()
        {
            needlePosition.x = needleXMin+ needleXLength * grabState / grabMax;
            needle.anchoredPosition = needlePosition;
        }

        public void End(bool success)
        {
            player.am.moveMods.Remove(moveMod);
            playerGrabbingGames.grabbingGames.Remove(this);

            if (success)
                CoreGameManager.Instance.AddPoints(20, player.playerNumber, true);
            else
                wires.grip += 4f;

            wires?.ReleasePlayer(player);
            Destroy(textCanvas.gameObject);
            Destroy(gameObject);
        }
    }
}

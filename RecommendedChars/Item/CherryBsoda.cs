using System.ComponentModel.Design;
using System.Net.Sockets;
using UnityEngine;
using static Rewired.Demos.CustomPlatform.MyPlatformControllerExtension;

namespace UncertainLuei.BaldiPlus.RecommendedChars
{
    public class ITM_CherryBsoda : Item, IEntityTrigger
    {
        private EnvironmentController ec;

        public LayerMaskObject layerMask;
        public Entity entity;
        public SoundObject sound;
        public SoundObject boing;
        public SpriteRenderer spriteRenderer;

        private PlayerManager currentPlayer;
        private MovementModifier moveMod = new MovementModifier(default,0f);

        public float speed = 35f;
        public float time = 8f;
        public byte bouncesLeft = 3;

        public override bool Use(PlayerManager pm)
        {
            ec = pm.ec;

            currentPlayer = pm;
            transform.position = pm.transform.position;
            transform.forward = CoreGameManager.Instance.GetCamera(pm.playerNumber).transform.forward;

            entity.Initialize(ec, transform.position);
            entity.OnEntityMoveInitialCollision += OnEntityMoveCollision;

            spriteRenderer.SetSpriteRotation(Random.Range(0f, 360f));
            CoreGameManager.Instance.audMan.PlaySingle(sound);
            pm.RuleBreak("Drinking", 0.8f, 0.25f);

            moveMod.priority = 1;
            pm.plm.Entity.ExternalActivity.moveMods.Add(moveMod);
            return true;
        }

        private void Update()
        {
            moveMod.movementAddend = entity.ExternalActivity.Addend + transform.forward * speed * ec.EnvironmentTimeScale;
            entity.MoveWithCollision(transform.forward * speed * ec.EnvironmentTimeScale * Time.deltaTime);

            time -= Time.deltaTime * ec.EnvironmentTimeScale;
            if (time > 0f) return;

            Destroy();
        }

        public void EntityTriggerEnter(Collider other)
        {
        }

        public void EntityTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") && other.transform == currentPlayer.transform)
                Destroy();
        }

        private void Destroy()
        {
            currentPlayer.plm.Entity.ExternalActivity.moveMods.Remove(moveMod);
            Destroy(gameObject);
        }

        public void EntityTriggerStay(Collider other)
        {
        }

        private void OnEntityMoveCollision(RaycastHit hit)
        {
            if (layerMask.Contains(hit.collider.gameObject.layer))
            {
                bouncesLeft--;
                if (bouncesLeft == 0)
                    Destroy();

                CoreGameManager.Instance.audMan.PlaySingle(boing);
                transform.forward = transform.forward - (2f * Vector3.Dot(hit.normal, transform.forward) * hit.normal);
                moveMod.movementAddend = entity.ExternalActivity.Addend + transform.forward * speed * ec.EnvironmentTimeScale;
                entity.MoveWithCollision(transform.forward * speed * ec.EnvironmentTimeScale * Time.deltaTime);
            }
        }
    }
}

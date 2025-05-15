using System.Linq;
using UnityEngine;

namespace UncertainLuei.BaldiPlus.RecommendedChars
{
    public class ITM_CherryBsoda : Item, IEntityTrigger
    {
        public ITM_BSODA bsoda;
        protected EnvironmentController Ec => bsoda.ec;
        protected MovementModifier MoveMod => bsoda.moveMod;
        protected Entity Entity => bsoda.entity;

        public LayerMaskObject layerMask;
        public SoundObject boing;

        protected PlayerManager currentPlayer;

        protected float Speed => bsoda.speed;
        public byte bouncesLeft = 3;

        private bool destroyQueued = false;

        public override bool Use(PlayerManager pm)
        {
            bsoda.Use(pm);
            currentPlayer = pm;

            Entity.OnEntityMoveInitialCollision += OnEntityMoveCollision;
            // Remove the ITM_BSODA EntityTrigger as it is grabbed by the Entity regardless of the component being disabled
            Entity.iEntityTrigger = Entity.iEntityTrigger.Where(x => !(x is ITM_BSODA)).ToArray();

            AddPlayerToMoveMod();
            return true;
        }

        protected virtual void AddPlayerToMoveMod()
        {
            pm.plm.Entity.ExternalActivity.moveMods.Add(MoveMod);
        }

        private void Update()
        {
            MoveMod.movementAddend = Entity.ExternalActivity.Addend + transform.forward * Speed * Ec.EnvironmentTimeScale;
            Entity.MoveWithCollision(transform.forward * Speed * Ec.EnvironmentTimeScale * Time.deltaTime);

            bsoda.time -= Time.deltaTime * Ec.EnvironmentTimeScale;
            if (bsoda.time > 0f) return;

            Destroy();
        }

        public void EntityTriggerEnter(Collider other)
        {
            if (!destroyQueued)
                VirtualTriggerEnter(other);
        }

        public void EntityTriggerExit(Collider other)
        {
            if (!destroyQueued)
                VirtualTriggerExit(other);
        }

        public void EntityTriggerStay(Collider other)
        {
        }

        protected virtual void VirtualTriggerEnter(Collider other)
        {
        }
        protected virtual void VirtualTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") && other.transform == currentPlayer.transform)
                Destroy();
        }

        protected void Destroy()
        {
            destroyQueued = true;
            VirtualDestroy();
            Destroy(gameObject);
        }

        protected virtual void VirtualDestroy()
        {
            currentPlayer.plm.am.moveMods.Remove(MoveMod);
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
                MoveMod.movementAddend = Entity.ExternalActivity.Addend + transform.forward * Speed * Ec.EnvironmentTimeScale;
                Entity.MoveWithCollision(transform.forward * Speed * Ec.EnvironmentTimeScale * Time.deltaTime);
            }
        }
    }

    public class ITM_CherryBsoda_PushesNpcs : ITM_CherryBsoda
    {   
        protected override void AddPlayerToMoveMod()
        {
        }

        protected override void VirtualTriggerEnter(Collider other)
        {
            bsoda.EntityTriggerEnter(other);
        }

        protected override void VirtualTriggerExit(Collider other)
        {
            bsoda.EntityTriggerExit(other);
            if (other.CompareTag("Player") && other.transform == currentPlayer.transform && currentPlayer.plm.am.moveMods.Contains(MoveMod))
                currentPlayer.plm.am.moveMods.Remove(MoveMod);
        }

        protected override void VirtualDestroy()
        {
            if (currentPlayer.plm.am.moveMods.Contains(MoveMod))
                currentPlayer.plm.am.moveMods.Remove(MoveMod);

            foreach (ActivityModifier activityMod in bsoda.activityMods)
                activityMod.moveMods.Remove(MoveMod);
        }
    }
}

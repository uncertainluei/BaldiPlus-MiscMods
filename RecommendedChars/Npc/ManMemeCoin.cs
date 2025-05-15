using System;
using System.Collections.Generic;
using System.Text;

namespace UncertainLuei.BaldiPlus.RecommendedChars
{
    public class ManMemeCoin : NPC, IClickable<int>
    {
        public bool ClickableHidden()
        {
            return false;
        }

        public bool ClickableRequiresNormalHeight()
        {
            return false;
        }
            
        public void ClickableSighted(int player)
        {
        }

        public void ClickableUnsighted(int player)
        {
        }

        public void Clicked(int player)
        {
        }
    }

    public abstract class ManMemeCoin_StateBase : NpcState
    {
        protected readonly ManMemeCoin coin;

        public ManMemeCoin_StateBase(ManMemeCoin coin) : base(coin)
        {
            this.coin = coin;
        }
    }

    public class ManMemeCoin_Wandering : ManMemeCoin_StateBase
    {
        public ManMemeCoin_Wandering(ManMemeCoin coin) : base(coin)
        {
        }
    }       
}

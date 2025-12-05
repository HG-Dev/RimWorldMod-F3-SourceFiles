using System;
using UnityEngine;
using Verse;

namespace HG.FFF
{
    public readonly struct QuotaState
    {
        public readonly int Amount;
        public readonly int AmountRequired;
        public readonly bool IsValid;
        
        public QuotaState(int amt, int amtRequired)
        {
            IsValid = amtRequired > 0;
            Amount = amt;
            AmountRequired = amtRequired;
        }
        
        public static QuotaState FromIngredientCount(IngredientCount ing, int startAmt = 0)
        {
            return new QuotaState(startAmt, Mathf.FloorToInt(ing.GetBaseCount()));
        }

        /// <summary>
        /// How much Amount exceeds AmountRequired, or zero if it does not. 
        /// </summary>
        public int AmountExcess => Math.Max(0, Amount - AmountRequired);
        /// <summary>
        /// How much is left until Amount reaches AmountRequired, or zero if Amount is greater than or equal to AmountRequired. 
        /// </summary>
        public int AmountMissing => Math.Max(0, AmountRequired - Amount);
        public bool IsMet => Amount >= AmountRequired;
        public float PercentageMet => IsValid ? (float)Amount / AmountRequired : 1;

        public QuotaState Zeroed => new QuotaState(0, AmountRequired);
        
        public static QuotaState operator +(QuotaState src, int added)
            => new QuotaState(src.Amount + added, src.AmountRequired);
    }
}
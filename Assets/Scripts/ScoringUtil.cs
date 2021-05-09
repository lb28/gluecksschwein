using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ScoringUtil
{
        public readonly struct BaseTariff
        {
                public BaseTariff(int low = 10, int medium = 20, int high = 50)
                {
                        Low = low;
                        Medium = medium;
                        High = high;
                }

                public int Low { get; }
                public int Medium { get; }
                public int High { get; }
                
                public override string ToString()
                {
                        return $"{Low}-{Medium}{High}";
                }
        }

        public static int GetPlayerRoundScore(Player player, Dictionary<PlayingCard.Stich, Player> completedStiches)
        {
                // old version. TODO update using tariffs (see #11)
                return  completedStiches
                        .Where(pair => pair.Value.Equals(player))
                        .Sum(pair => pair.Key.Worth);
        }
}
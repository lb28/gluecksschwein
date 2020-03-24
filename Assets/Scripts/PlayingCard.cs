﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public static class PlayingCard
{
 
    // use struct for easy serialization in SyncList
    public struct PlayingCardInfo
    {
        public Rank Rank;
        public Suit Suit;
    }

    public enum Suit
    {
        Blatt,
        Eichel,
        Schelln,
        Herz
    }

    public enum Rank
    {
        Sieben,
        Acht,
        Neun,
        Koenig,
        Zehn,
        Unter,
        Ober,
        Ass
    }


    public static Dictionary<PlayingCardInfo, Sprite> SpriteDict = new Dictionary<PlayingCardInfo, Sprite>();

    public static Sprite GetSprite(PlayingCardInfo cardInfo)
    {
        string pathSuffix = "";
        pathSuffix += cardInfo.Suit.ToString().ToLower();
        pathSuffix += GetRankFileSuffix(cardInfo.Rank);
        return Resources.Load<Sprite>($"Spielkarten/{pathSuffix}");
    }

    private static string GetRankFileSuffix(Rank rank)
    {
        switch (rank)
        {
            case Rank.Sieben:
                return "07";
            case Rank.Acht:
                return "08";
            case Rank.Neun:
                return "09";
            case Rank.Koenig:
                return "ko";
            case Rank.Zehn:
                return "10";
            case Rank.Unter:
                return "un";
            case Rank.Ober:
                return "ob";
            case Rank.Ass:
                return "as";
            default:
                throw new ArgumentOutOfRangeException(nameof(rank), rank, null);
        }
    }

    public static List<PlayingCardInfo> InitializeCardDeck()
    {
        var deck = new List<PlayingCardInfo>();
        
        var ranks = Enum.GetValues(typeof(Rank));
        var suits = Enum.GetValues(typeof(Suit));
        foreach (Suit suit in suits)
        {
            foreach (Rank rank in ranks)
            {
                PlayingCardInfo cardInfo = new PlayingCardInfo
                    {
                        Suit = suit,
                        Rank = rank,
                    };
                deck.Add(cardInfo);
                
                // also store the sprite in the dictionary for later access
                SpriteDict[cardInfo] = GetSprite(cardInfo);
            }
        }

        deck.Shuffle();

        return deck;
    }
    
    
}
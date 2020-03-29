﻿using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour
{
    public static GameManager Singleton;

    #region Game State Stuff

    public enum GameState
    {
        Waiting,        // Waiting until 4 players are connected 
        GameRunning,    // 4 players are connected, host can click button to deal cards
        PreRound,       // Players take turns deciding the game mode, i.e. Sauspiel, Solo, etc.
        RoundSau,       // Active during a round of Sauspiel
        RoundSolo,      // Active during a round of Solo
        RoundWenz,      // Active during a round of Wenz
        RoundRamsch,    // Active during a round of Ramsch
        RoundFinished   // We enter this state after the last "Stich". Here we can show/count scores
                        // TODO maybe we don't need RoundFinished and can just enter GameRunning instead
    }

    [field: SyncVar] public GameState CurrentGameState { get; set; }

    [SyncVar(hook = nameof(OnGameStateTextChanged))] [SerializeField]
    private string gameStateText;

    [SerializeField] private Text gameStateTextField;
    
    public enum RoundMode
    {
        SauspielBlatt,
        SauspielEichel,
        SauspielSchelln,
        Solo,
        Wenz,
        Ramsch
    }
    
    public enum PreRoundChoice
    {
        SauspielBlatt,
        SauspielEichel,
        SauspielSchelln,
        Solo,
        Wenz,
        Weiter
    }

    #endregion

    #region Player Management
    
    [Header("Players")]
    public List<Player> players = new List<Player>();
    public List<Button> localPlayerCardButtons;
    public Button dealCardsButton;
    private Player _startingPlayer; 
    
    [Header("Pre-Round")]
    public GameObject preRoundButtonPanel; 
    public Dropdown preRoundSauspielDropdown;
    public Button preRoundSoloButton;
    public Button preRoundWenzButton;
    public Button preRoundWeiterButton;

    #endregion

    #region Deck Management

    private List<PlayingCard.PlayingCardInfo> _cardDeck;
    public class SyncListPlayingCard : SyncListStruct<PlayingCard.PlayingCardInfo> { }
    private readonly SyncListPlayingCard _syncListCardDeck = new SyncListPlayingCard();

    #endregion

    #region Round Management
    
    [SerializeField] private List<GameObject> playedCardSlots;
    private readonly SyncListPlayingCard _playedCards = new SyncListPlayingCard();
    
    #endregion

    
    ///////////////////////////////////////////////
    /////////////////// Methods ///////////////////
    ///////////////////////////////////////////////

    #region Game State Transitions

    /// <summary>
    /// Used to enter the "WaitingForPlayers" state:
    /// * we are simply waiting until we have enough players
    /// </summary>
    private void EnterStateWaiting()
    {
        CurrentGameState = GameState.Waiting;
    }

    /// <summary>
    /// Used to enter the "Game Running" state
    /// * show scoreboard
    /// * host can start a new round
    /// </summary>
    private void EnterStateGameRunning()
    {
        // check previous game state
        if (CurrentGameState == GameState.Waiting)
        {
            // set starting player to the last one so that before the first round the player 0 gets selected
            _startingPlayer = players[3];
        }

        // update the game state
        CurrentGameState = GameState.GameRunning;

        gameStateText = "Bereit zum spielen";
        dealCardsButton.gameObject.SetActive(true);
        dealCardsButton.onClick.AddListener(EnterStatePreRound);
        // TODO: show scoreboard
    }

    /// <summary>
    /// Used to enter the "Pre Round" state
    /// * Cards are dealt to the players
    /// * All the players are asked whether they want to "play" or "pass" (startingPlayer is asked first)
    /// * (if everybody passes, a Ramsch has to be initialized)
    /// </summary>
    private void EnterStatePreRound()
    {
        // update the game state
        CurrentGameState = GameState.PreRound;

        // deal the cards to the players
        DealCards();
        
        // disable the dealCards Button
        dealCardsButton.gameObject.SetActive(false);

        // update the starting player
        _startingPlayer = SelectNextStartingPlayer();

        Player currentPreRoundDecider = _startingPlayer;

        // Display the Pre Round Buttons for the currently deciding player
        currentPreRoundDecider.RpcDisplayPreRoundButtons();
    }

    /// <summary>
    /// Used to enter the "RoundSau" state
    /// </summary>
    private void EnterStateRoundSau()
    {
        CurrentGameState = GameState.RoundSau;
    }

    /// <summary>
    /// Used to enter the "RoundSolo" state
    /// </summary>
    private void EnterStateRoundSolo()
    {
        CurrentGameState = GameState.RoundSolo;
    }

    /// <summary>
    /// Used to enter the "RoundWenz" state
    /// </summary>
    private void EnterStateRoundWenz()
    {
        CurrentGameState = GameState.RoundWenz;
    }

    /// <summary>
    /// Used to enter the "RoundRamsch" state
    /// </summary>
    private void EnterStateRoundRamsch()
    {
        CurrentGameState = GameState.RoundRamsch;
    }

    /// <summary>
    /// Used to enter the "Round Finished" state
    /// </summary>
    private void EnterStateRoundFinished()
    {
        CurrentGameState = GameState.RoundFinished;
    }

    private Player SelectNextStartingPlayer()
    {
        // gib mir den index des aktuellen _startingPlayers
        int lastStartingPlayerIndex = players.IndexOf(_startingPlayer);

        int newStartingPlayerIndex = lastStartingPlayerIndex + 1;
        if (newStartingPlayerIndex == 4)
        {
            newStartingPlayerIndex = 0;
        }

        return players[newStartingPlayerIndex];
    }

    [Command]
    public void CmdSelectPreRoundChoice(NetworkInstanceId playerId, PreRoundChoice playerChoice)
    {
        Debug.Log($"{MethodBase.GetCurrentMethod().DeclaringType}::{MethodBase.GetCurrentMethod().Name}: " +
                  $"player {playerId} chose {playerChoice}");
        
        // TODO compute remaining options for other players
        
        
        // TODO display buttons to the next player
        throw new NotImplementedException();
    }
    
    #endregion
    
    // Start is called before the first frame update
    private void Awake()
    {
        Singleton = this;
        _cardDeck = PlayingCard.InitializeCardDeck();
        _playedCards.Callback = OnCardPlayed;
    }

    public override void OnStartServer()
    {
        foreach (PlayingCard.PlayingCardInfo cardInfo in _cardDeck)
        {
            _syncListCardDeck.Add(cardInfo);
        }

        EnterStateWaiting();
    }

    #region Server Stuff / Commands

    [Command]
    public void CmdPlayCard(PlayingCard.PlayingCardInfo cardInfo)
    {
        Debug.Log($"{MethodBase.GetCurrentMethod().DeclaringType}::{MethodBase.GetCurrentMethod().Name}: " +
                  $"someone wants me (the server) to play {cardInfo}");

        // add the card to the played cards
        _playedCards.Add(cardInfo);
    }
    
    [Server]
    public void AddPlayer(Player player)
    {
        Debug.Log($"{MethodBase.GetCurrentMethod().DeclaringType}::{MethodBase.GetCurrentMethod().Name}: " +
                  $"adding Player {player.netId}");
        players.Add(player);
        
        gameStateText = $"Warte auf Spieler... ({players.Count})";

        if (players.Count == 4)
        {
            EnterStateGameRunning();
        }
    }

    /// <summary>
    /// Gives out the cards from the deck:
    /// card 00-07 to player 1, card 08-15 to player 2, card 16-23 to player 3, card 24-31 to player 4
    /// </summary>
    [Server]
    public void DealCards()
    {
        int handedCards = 0;
        foreach (Player player in players)
        {
            Debug.Log($"{MethodBase.GetCurrentMethod().DeclaringType}::{MethodBase.GetCurrentMethod().Name}: " +
                      $"Player {player.netId} should get cards {{handedCards}} to {{handedCards + 7}}");
            // this updates the player's cards on the server object, which then notifies his respective client object
            for (int i = handedCards; i < handedCards + 8; i++)
            {
                player.handCards.Add(_syncListCardDeck[i]);
            }

            handedCards += 8;
        }
    }

    #endregion

    #region SyncVar Callbacks/Hooks

    private void OnCardPlayed(SyncList<PlayingCard.PlayingCardInfo>.Operation op, int i)
    {
        Debug.Log($"{MethodBase.GetCurrentMethod().DeclaringType}::{MethodBase.GetCurrentMethod().Name}: " +
                  $"the server notified me that {_playedCards[i]} was played");

        // put the correct image in the position of the just played card
        playedCardSlots[i].SetActive(true);
        playedCardSlots[i].GetComponent<Image>().sprite = PlayingCard.SpriteDict[_playedCards[i]];
    }

    void OnGameStateTextChanged(string newText)
    {
        Debug.Log($"{MethodBase.GetCurrentMethod().DeclaringType}::{MethodBase.GetCurrentMethod().Name}: " +
                  $"new text = \"{gameStateText}\"");
        gameStateText = newText;
        gameStateTextField.text = gameStateText;
    }

    #endregion
}
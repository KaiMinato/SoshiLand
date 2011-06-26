﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
// Required to read XML file
using System.Xml;


namespace SoshiLandSilverlight
{
    class SoshilandGame
    {
        private List<Player> ListOfPlayers;             // Contains the list of players in the game. This will be in the order from first to last player
        private Player currentTurnsPlayers;             // Holds the Player of the current turn         
        private Tile[] Tiles = new Tile[48];            // Array of Tiles

        private static Random die = new Random();       // Need to create a static random die generator so it doesn't reuse the same seed over and over

        private bool DoublesRolled;                     // Flag to indicate doubles were rolled
        private int currentDiceRoll;                    // Global dice roll variable for special instances when we need to know (ie. determining player order)
        private int numberOfDoubles;                    // Keep track of the number of doubles rolled

        public static int Houses = 32;                  // Static Global variable for number of houses remaining
        public static int Hotels = 12;                  // Static Global variable for number of hotels remaining

        private bool gameInitialized = false;           // Flag for when the game is officially started
        private bool optionsCalculated = false;         // Flag for when player options are ready to prompt

        private bool displayJailMessageOnce = true;    // Flag to display message only once

        // Player Options during turn
        private bool optionPurchaseOrAuctionProperty = false;
        private bool optionPurchaseOrAuctionUtility = false;
        private bool optionDevelopProperty = false;
        private bool optionPromptMortgageOrTrade = false;
        private bool optionPromptLuxuryTax = false;
        private bool optionShoppingSpree = false;

        private bool taxesMustPayTenPercent = false;
        private bool taxesMustPayTwoHundred = false;
        // Phase Flags
        
        // 0 = Pre Roll Phase
        // Player has option to trade, develop or mortgage / unmortgage.
        // If player is in jail, player has option to Pay to get out of jail, or roll doubles
        // Phase ends after player chooses to roll dice
        
        // 1 = Roll Phase
        // Player has landed on a Tile.
        // If tile is a property, Player is forced to purchase or auction
        // If tile is Chance / Community Chest, Player is forced to follow card instructions immediately
        // If tile is Taxes, Player is forced to pay immediately (or choose option between 10% or $200 for luxury tax)
        // If tile is Jail, move piece to jail

        // 2 = Post Roll Phase
        // Player has option to trade, develop or mortgage / unmortgage.
        // Phase ends after playing chooses to end his or her turn

        private byte turnPhase = 0;                 

        private KeyboardState previousKeyboardInput;    

        // TEMPORARY
        Player[] playerArray;

        public SoshilandGame()
        {
            Initialization.InitializeTiles(Tiles);      // Initialize Tiles on the board
            InitializeGame();                           // Initialize Game
        }

        private void InitializeGame()
        {
            // Temporary list of players
            Player player1 = new Player("Mark");
            Player player2 = new Player("Wooski");
            Player player3 = new Player("Yook");
            Player player4 = new Player("Addy");
            Player player5 = new Player("Colby");
            Player player6 = new Player("Skylar");

            playerArray = new Player[6];
            playerArray[0] = player1;
            playerArray[1] = player2;
            playerArray[2] = player3;
            playerArray[3] = player4;
            playerArray[4] = player5;
            playerArray[5] = player6;
            // Determine order of players
            DeterminePlayerOrder(playerArray);
            // Players choose pieces (this can be implemented later)

            // Players are given starting money
            DistributeStartingMoney();
            // Place all Pieces on Go
            PlaceAllPiecesOnGo();
            startNextPlayerTurn();
        }

        public void startNextPlayerTurn()
        {
            if (Game1.DEBUG)
            {
                if (currentTurnsPlayers != null)
                {
                    Game1.debugMessageQueue.addMessageToQueue("Player " + "\"" + currentTurnsPlayers.getName + "\"'s " + " turn ends");
                    Console.WriteLine("Player " + "\"" + currentTurnsPlayers.getName + "\"'s " + " turn ends");
                }
            }

            int previousPlayersTurn = ListOfPlayers.IndexOf(currentTurnsPlayers);
            int nextPlayersTurn;

            // Checks if the player is at the end of the list
            if (previousPlayersTurn == ListOfPlayers.Count - 1)
                nextPlayersTurn = 0;
            else
                nextPlayersTurn = previousPlayersTurn + 1;

            PlayerTurn(ListOfPlayers.ElementAt(nextPlayersTurn));
        }

        private void PlayerTurn(Player player)
        {
            currentTurnsPlayers = player;
            
            if (Game1.DEBUG)
            {
                Game1.debugMessageQueue.addMessageToQueue("Player " + "\"" + currentTurnsPlayers.getName + "\"'s " + " turn begins");
                Console.WriteLine("Player " + "\"" + currentTurnsPlayers.getName + "\"'s " + " turn begins");
            }

            // Set phase to Pre Roll Phase
            turnPhase = 0;

            // Check if player is currently in Jail
            
            // Determine what Tile was landed on and give options
            

        }

        private void PlayerOptions(Player player)
        {
            int currentTile = player.CurrentBoardPosition;
            TileType currentTileType = Tiles[currentTile].getTileType;

            optionPurchaseOrAuctionProperty = false;
            optionDevelopProperty = false;

            // Determine Player Options and take any actions required
            switch (currentTileType)
            {
                case TileType.Property:
                    PropertyTile currentProperty = (PropertyTile)Tiles[currentTile];

                    if (currentProperty.Owner == null)                  // If the property is not owned yet
                        optionPurchaseOrAuctionProperty = true;
                    else if (currentProperty.Owner != player)           // If the property is owned by another player
                    {
                        if (player.getMoney >= currentProperty.getRent) // Check if the player has enough money to pay Rent
                        {
                            player.CurrentPlayerPaysPlayer(currentProperty.Owner, currentProperty.getRent);     // Pay rent
                            turnPhase = 2;          // Go to next phase
                        }
                        else
                            optionPromptMortgageOrTrade = true;         // Player must decide to mortgage or trade to get money
                    }

                    // Otherwise, player landed on his or her own property, so do nothing
                    break;

                case TileType.Utility:
                    UtilityTile currentUtility = (UtilityTile)Tiles[currentTile];
                    UtilityTile otherUtility;

                    if (currentTile == 15)
                        otherUtility = (UtilityTile)Tiles[33];
                    else
                        otherUtility = (UtilityTile)Tiles[15];

                    if (currentUtility.Owner == null)               // If the property is not owned yet
                        optionPurchaseOrAuctionUtility = true;

                    else if (currentUtility.Owner != player)        // If the property is owned by another player            
                    {
                        uint utilityRent;                           // Calculate the amount to pay for Utility Rent

                        if (currentUtility.Owner == otherUtility.Owner)     // Check if player owns both utilities
                            utilityRent = (uint)currentDiceRoll * 10;
                        else
                            utilityRent = (uint)currentDiceRoll * 4;


                        if (player.getMoney >= utilityRent)                 // Check if the player has enough money to pay Rent
                        {
                            player.CurrentPlayerPaysPlayer(currentUtility.Owner, utilityRent);  // Pay rent
                            turnPhase = 2;              // Go to next phase
                        }
                        else
                            optionPromptMortgageOrTrade = true;             // Player must decide to mortgage or trade to get money
                    }
                    break;

                case TileType.Chance:
                    break;
                case TileType.CommunityChest:
                    break;
                case TileType.FanMeeting:
                    turnPhase = 2;              // Nothing happens, so go to last phase
                    break;
                case TileType.Jail:
                    turnPhase = 2;              // Nothing happens, so go to last phase
                    break;
                case TileType.ShoppingSpree:
                    if (currentTurnsPlayers.getMoney >= 75)     // Check if player has enough money to pay tax
                        currentTurnsPlayers.PlayerPaysBank(75); // Pay Bank taxes
                        // Player does not have enough money
                    else
                    {
                        optionShoppingSpree = true;             // Set flag so game remembers that player has to pay
                        optionPromptMortgageOrTrade = true;     // Set flag to prompt player to get more money somehow
                    }
                    break;
                case TileType.SpecialLuxuryTax:
                        Game1.debugMessageQueue.addMessageToQueue("Player " + "\"" + currentTurnsPlayers.getName + "\"" + " must choose to pay 10% of net worth, or $200");
                        Console.WriteLine("Player " + "\"" + currentTurnsPlayers.getName + "\"" + " must choose to pay 10% of net worth, or $200");
                        Game1.debugMessageQueue.addMessageToQueue("Press K to pay 10% of net worth, or L to pay $200");
                        Console.WriteLine("Press K to pay 10% of net worth, or L to pay $200");
                        optionPromptLuxuryTax = true;
                    break;
                case TileType.GoToJail:
                    MovePlayerToJail(player);
                    break;
                case TileType.Go:
                    turnPhase = 2;
                    break;
            }

            optionsCalculated = true;

            if (Game1.DEBUG)
            {
                string optionsMessage = "Options Available: Trade,";
                if (optionDevelopProperty)
                    optionsMessage = optionsMessage + " Develop,";
                if (optionPurchaseOrAuctionProperty || optionPurchaseOrAuctionUtility)
                    optionsMessage = optionsMessage + " Purchase/Auction";

                Game1.debugMessageQueue.addMessageToQueue(optionsMessage);
                Console.WriteLine(optionsMessage);
            }
        }

        private void DistributeStartingMoney()
        {
            if (Game1.DEBUG)
            {
                Game1.debugMessageQueue.addMessageToQueue("Distributing Starting Money");
                Console.WriteLine("Distributing Starting Money");
            }

            foreach (Player p in ListOfPlayers)
            {
                // Starting money is $1500
                p.BankPaysPlayer(1500);
            }
        }

        private void PlaceAllPiecesOnGo()
        {
            if (Game1.DEBUG)
            {
                Game1.debugMessageQueue.addMessageToQueue("Placing all players on Go");
                Console.WriteLine("Placing all players on Go");
            }
            foreach (Player p in ListOfPlayers)
            {
                // Move player to Go
                MovePlayer(p, 0);
            }
            gameInitialized = true;
        }

        private void DeterminePlayerOrder(Player[] arrayOfPlayers)
        {
            // Note!
            // arrayOfPlayers is the order the players are sitting in around the board.
            // So the order is determined by starting at the player with the highest roll 
            // and moving clockwise around the board

            if (Game1.DEBUG)
            {
                Game1.debugMessageQueue.addMessageToQueue("Players rolling to determine Order");
                Console.WriteLine("Players rolling to determine Order");
            }

            int[] playerRolls = new int[arrayOfPlayers.Length];     // An array the size of the number of players to hold their dice rolls
            List<Player> tiedPlayers = new List<Player>();          // List of players that are tied for highest roll

            int currentHighestPlayer = 0;                           // Current player index in arrayOfPlayers with the highest roll

            // Have each player roll a pair of dice and store the result in the playerRolls array
            for (int i = 0; i < arrayOfPlayers.Length; i++)
            {
                RollDice(arrayOfPlayers[i]);
                playerRolls[i] = currentDiceRoll;

                // If the current highest player's roll is less than the new player's roll
                // Replace that player with the new player with the highest roll
                if (playerRolls[currentHighestPlayer] < playerRolls[i] && i != currentHighestPlayer)
                {
                    // Set the new Highest Player roll
                    currentHighestPlayer = i;
                    // Clear the list of tied players
                    tiedPlayers.Clear();
                }
                else if (playerRolls[currentHighestPlayer] == playerRolls[i] && i != currentHighestPlayer)
                {
                    // Only add the current highest player if the list is empty
                    // That player would've already been added to the list
                    if (tiedPlayers.Count == 0)
                        tiedPlayers.Add(arrayOfPlayers[currentHighestPlayer]);
                    // Add the new player to the list of tied players
                    tiedPlayers.Add(arrayOfPlayers[i]);
                }

                if (Game1.DEBUG)
                {
                    Game1.debugMessageQueue.addMessageToQueue("Player " + "\"" + arrayOfPlayers[currentHighestPlayer].getName + "\"" + " is the current highest roller with: " + playerRolls[currentHighestPlayer]);
                    Console.WriteLine("Player " + "\"" + arrayOfPlayers[currentHighestPlayer].getName + "\"" + " is the current highest roller with: " + playerRolls[currentHighestPlayer]);
                }
            }

            // Initialize the list of players
            ListOfPlayers = new List<Player>();

            // Check if there is a tie with highest rolls
            if (tiedPlayers.Count > 0)
            {
                if (Game1.DEBUG)
                {
                    Game1.debugMessageQueue.addMessageToQueue("There's a tie!");
                    Console.WriteLine("There's a tie!");
                }
                // New list to store second round of tied players
                List<Player> secondRoundOfTied = new List<Player>();
                // Keep rolling until no more tied players
                while (secondRoundOfTied.Count != 1)
                {
                    int currentHighestRoll = 0;

                    // Roll the dice for each player
                    foreach (Player p in tiedPlayers)
                    {

                        RollDice(p);                                                    // Roll the dice for the player
                        // If the new roll is higher than the current highest roll
                        if (currentDiceRoll > currentHighestRoll)
                        {
                            // Clear the list since everyone who may have been in the list is lower 
                            secondRoundOfTied.Clear();

                            // Set the new highest roll
                            currentHighestRoll = currentDiceRoll;
                            secondRoundOfTied.Add(p);
                        }
                        // If there's another tie, just add it to the new array without clearing it
                        else if (currentDiceRoll == currentHighestRoll)
                        {
                            secondRoundOfTied.Add(p);
                        }
                        // Otherwise, the player rolled less and is removed
                    }

                    // If there are still tied players, transfer them into the old List and clear the new List
                    if (secondRoundOfTied.Count > 1)
                    {
                        // Clear the players that did not roll high enough
                        tiedPlayers.Clear();
                        foreach (Player p in secondRoundOfTied)
                        {
                            tiedPlayers.Add(p);
                        }
                        secondRoundOfTied.Clear();
                    }
                }

                // Should be one clear winner now
                ListOfPlayers.Add(secondRoundOfTied[0]);
            }

            if (ListOfPlayers.Count == 0)
                ListOfPlayers.Add(arrayOfPlayers[currentHighestPlayer]);

            int firstPlayer = 0;
            // Search for the first player in the player array
            while (arrayOfPlayers[firstPlayer] != ListOfPlayers[0])
                firstPlayer++;

            // Populate the players in clockwise order
            for (int a = firstPlayer + 1; a < arrayOfPlayers.Length; a++)
                ListOfPlayers.Add(arrayOfPlayers[a]);
            if (firstPlayer != 0)
            {
                for (int b = 0; b < firstPlayer; b++)
                    ListOfPlayers.Add(arrayOfPlayers[b]);
            }


            if (Game1.DEBUG)
            {
                Game1.debugMessageQueue.addMessageToQueue("Player Order Determined! ");
                Console.WriteLine("Player Order Determined! ");
                for (int i = 1; i < ListOfPlayers.Count + 1; i++)
                {
                    Game1.debugMessageQueue.addMessageToQueue(i + ": " + ListOfPlayers[i - 1].getName);
                    Console.WriteLine(i + ": " + ListOfPlayers[i - 1].getName);
                }

            }
        }

        private void RollDice(Player p)
        {
            DoublesRolled = false;
            int dice1Int = die.Next(1, 6);
            int dice2Int = die.Next(1, 6);

            int total = dice1Int + dice2Int;

            currentDiceRoll = total;                // Set the global dice roll variable

            if (dice1Int == dice2Int && gameInitialized)
            {
                DoublesRolled = true;
                // Check if it's the third consecutive double roll
                if (numberOfDoubles == 2)
                    // Move player to jail
                    MovePlayerToJail(p);
                else
                    // Increment number of doubles
                    numberOfDoubles++;
            }
            
            if (Game1.DEBUG)
            {
                Game1.debugMessageQueue.addMessageToQueue("Player " + "\"" + p.getName + "\"" + " rolls dice: " + dice1Int + " and " + dice2Int + ". Total: " + total);
                Console.WriteLine("Player " + "\"" + p.getName + "\"" + " rolls dice: " + dice1Int + " and " + dice2Int + ". Total: " + total);
                if (DoublesRolled)
                {
                    Game1.debugMessageQueue.addMessageToQueue("Player " + "\"" + p.getName + "\"" + " rolled doubles!");
                    Console.WriteLine("Player " + "\"" + p.getName + "\"" + " rolled doubles!");
                }
            }
            
            // Only move if the player is not in jail
            if ((!p.inJail) && gameInitialized)
                MovePlayerDiceRoll(p, total);
        }

        private void MovePlayerDiceRoll(Player p, int roll)
        {
            int currentPosition = p.CurrentBoardPosition;
            int newPosition = currentPosition + roll;

            // If player passes or lands on Go
            if (newPosition > 47)
            {
                newPosition = Math.Abs(newPosition - 48);           // Get absolute value of the difference and move player to that new Tile
                p.BankPaysPlayer(200);                              // Pay player $200 for passing Go
            }
            // Move player to the new position
            MovePlayer(p, newPosition);
        }

        private void MovePlayer(Player p, int position)
        {
            // Update the player's current position to the new position
            p.CurrentBoardPosition = position;

            if (Game1.DEBUG)
            {
                Game1.debugMessageQueue.addMessageToQueue("Player " + "\"" + p.getName + "\"" + " moves to Tile \"" + Tiles[position].getName + "\"");
                Console.WriteLine("Player " + "\"" + p.getName + "\"" + " moves to Tile \"" + Tiles[position].getName + "\"");
            }
        }

        private void MovePlayerToJail(Player p)
        {
            if (Game1.DEBUG)
            {
                Game1.debugMessageQueue.addMessageToQueue("Player " + "\"" + p.getName + "\"" + " goes to jail!");
                Console.WriteLine("Player " + "\"" + p.getName + "\"" + " goes to jail!");
            }
            // Set jail flag for player
            p.inJail = true;
            MovePlayer(p, 12);

            // Set phase to Post Roll Phase
            turnPhase = 2;
        }
        
        public void PlayerInputUpdate()
        {
            KeyboardState kbInput = Keyboard.GetState();

            switch (turnPhase)
            {
                // Pre Roll Phase
                case 0:
                    // Check if player is in jail
                    if (currentTurnsPlayers.inJail)
                    {
                        if (Game1.DEBUG && displayJailMessageOnce)
                        {
                            Game1.debugMessageQueue.addMessageToQueue("Player " + "\"" + currentTurnsPlayers.getName + "\"" + " is currently in jail");
                            Console.WriteLine("Player " + "\"" + currentTurnsPlayers.getName + "\"" + " is currently in jail");
                            Game1.debugMessageQueue.addMessageToQueue("Press T to pay $50 to get out of jail, or R to try and roll doubles");
                            Console.WriteLine("Press T to pay $50 to get out of jail, or R to try and roll doubles");
                            displayJailMessageOnce = false;
                        }

                        // Player decides to roll for doubles
                        if (kbInput.IsKeyDown(Keys.R) && previousKeyboardInput.IsKeyUp(Keys.R))
                        {
                            // Roll Dice
                            RollDice(currentTurnsPlayers);

                            // Only move if doubles were rolled or if player has been in jail for the third turn
                            if (DoublesRolled || currentTurnsPlayers.turnsInJail == 2)
                            {
                                if (currentTurnsPlayers.turnsInJail == 2)
                                {
                                    Game1.debugMessageQueue.addMessageToQueue("Player " + "\"" + currentTurnsPlayers.getName + "\"" + " must pay $50 to get out of jail on third turn.");
                                    Console.WriteLine("Player " + "\"" + currentTurnsPlayers.getName + "\"" + " must pay $50 to get out of jail on third turn.");

                                    currentTurnsPlayers.PlayerPaysBank(50);             // Pay bank fine
                                    currentTurnsPlayers.inJail = false;                 // Set player out of jail
                                    currentTurnsPlayers.turnsInJail = 0;                // Set turns in jail back to zero
                                }

                                MovePlayerDiceRoll(currentTurnsPlayers, currentDiceRoll);   // Move player piece
                                PlayerOptions(currentTurnsPlayers);                         // Calculate options for player


                                DoublesRolled = false;  // Turn off doubles rolled flag because player is not supposed to take another turn after getting out of jail

                                turnPhase = 1;          // Set the next phase
                            }
                            else
                            {
                                if (Game1.DEBUG)
                                {
                                    Game1.debugMessageQueue.addMessageToQueue("You failed to roll doubles and stay in jail.");
                                    Console.WriteLine("You failed to roll doubles and stay in jail.");
                                }

                                currentTurnsPlayers.turnsInJail++;
                                turnPhase = 2;
                            }
                        }

                        // If player chooses to pay to get out of jail
                        if (kbInput.IsKeyDown(Keys.T) && previousKeyboardInput.IsKeyUp(Keys.T))
                        {
                            Game1.debugMessageQueue.addMessageToQueue("Player " + "\"" + currentTurnsPlayers.getName + "\"" + " pays $50 to escape from Babysitting Kyungsan");
                            Console.WriteLine("Player " + "\"" + currentTurnsPlayers.getName + "\"" + " pays $50 to escape from Babysitting Kyungsan");

                            currentTurnsPlayers.PlayerPaysBank(50);     // Pay bank fine
                            currentTurnsPlayers.turnsInJail = 0;        // Set turns in jail back to zero
                            currentTurnsPlayers.inJail = false;         // Set player to be out of Jail
                        }

                    }
                    else
                    {
                        // Roll Dice
                        if (kbInput.IsKeyDown(Keys.R) && previousKeyboardInput.IsKeyUp(Keys.R))
                        {
                            RollDice(currentTurnsPlayers);              // Rolls Dice and Move Piece to Tile
                            turnPhase = 1;                              // Set next phase
                            PlayerOptions(currentTurnsPlayers);         // Calculate options for player
                            
                        }
                    }
                    break;

                    // Roll Phase
                case 1:
                    if (optionsCalculated)
                    {
                        // Player chooses to purchase property
                        if (kbInput.IsKeyDown(Keys.P) && previousKeyboardInput.IsKeyUp(Keys.P))
                        {
                            bool successfulPurchase = false;
                            // Purchase Property
                            if (optionPurchaseOrAuctionProperty)
                                successfulPurchase = currentTurnsPlayers.PurchaseProperty((PropertyTile)Tiles[currentTurnsPlayers.CurrentBoardPosition]);
                            // Purchase Utility
                            else if (optionPurchaseOrAuctionUtility)
                                successfulPurchase = currentTurnsPlayers.PurchaseUtility((UtilityTile)Tiles[currentTurnsPlayers.CurrentBoardPosition]);
                            // Player cannot purchase right now
                            else
                            {
                                if (Game1.DEBUG)
                                {
                                    Game1.debugMessageQueue.addMessageToQueue("Player " + "\"" + currentTurnsPlayers.getName + "\"" + " cannot purchase \"" + Tiles[currentTurnsPlayers.CurrentBoardPosition].getName + "\"");
                                    Console.WriteLine("Player " + "\"" + currentTurnsPlayers.getName + "\"" + " cannot purchase \"" + Tiles[currentTurnsPlayers.CurrentBoardPosition].getName + "\"");
                                }
                            }
                            // Turn off option to purchase if successful purchase has been made
                            if (successfulPurchase)
                            {
                                // Set flags for purchase/auction off
                                optionPurchaseOrAuctionUtility = false;
                                optionPurchaseOrAuctionProperty = false;
                                // Set the next phase
                                turnPhase = 2;
                            }
                        }

                        // Player chooses to Auction

                        if (optionPromptLuxuryTax)
                        {
                            bool successfulTaxPayment = false;
                            // Player chooses to pay 10% (Luxury Tax)
                            if (kbInput.IsKeyDown(Keys.K) && previousKeyboardInput.IsKeyUp(Keys.K) && !taxesMustPayTwoHundred)
                            {
                                successfulTaxPayment = PayTenPercentWorthToBank(currentTurnsPlayers);       // Pay 10% to bank
                                if (successfulTaxPayment)
                                {
                                    turnPhase = 2;
                                    optionPromptLuxuryTax = false;                                          // Turn off the tax flag
                                }
                                else
                                {
                                    taxesMustPayTenPercent = true;              // Turn flag for paying 10%
                                    optionPromptMortgageOrTrade = true;         // Player is forced to mortgage
                                }
                            }
                            // Player chooses to pay $200 (Luxury Tax)
                            else if (kbInput.IsKeyDown(Keys.L) && previousKeyboardInput.IsKeyUp(Keys.L) && !taxesMustPayTenPercent)
                            {
                                if (currentTurnsPlayers.getMoney >= 200)            // Check if player has enough money
                                {
                                    currentTurnsPlayers.PlayerPaysBank(200);        // Pay $200 to bank
                                    optionPromptLuxuryTax = false;                  // Turn off the tax flag
                                    turnPhase = 2;                                  // Go to next phase
                                }
                                else
                                {
                                    taxesMustPayTwoHundred = true;                  // Turn flag on for paying $200
                                    optionPromptMortgageOrTrade = true;             // Player is forced to mortgage
                                }
                            }
                        }

                        // Player chooses to mortgage

                        // Player chooses to trade
                    }
                    break;
                    // Post Roll Phase

                case 2:
                    // Player chooses to end turn
                    if (kbInput.IsKeyDown(Keys.E) && previousKeyboardInput.IsKeyUp(Keys.E))
                    {
                        // Check if doubles has been rolled
                        if (DoublesRolled && !currentTurnsPlayers.inJail)
                        {
                            // Go back to phase 0 for current player
                            turnPhase = 0;

                            if (Game1.DEBUG)
                            {
                                Game1.debugMessageQueue.addMessageToQueue("Player " + "\"" + currentTurnsPlayers.getName + "\"" + " gets to roll again!");
                                Console.WriteLine("Player " + "\"" + currentTurnsPlayers.getName + "\"" + " gets to roll again!");
                            }
                        }
                        else
                        {
                            // Start next player's turn
                            startNextPlayerTurn();
                            // Set phase back to 0 for next player
                            turnPhase = 0;
                            optionsCalculated = false;
                            taxesMustPayTenPercent = false;
                            taxesMustPayTwoHundred = false;
                            // set number of doubles back to zero
                            numberOfDoubles = 0;
                        }
                    }
                    break;
            }

            previousKeyboardInput = kbInput;
        }

        private bool PayTenPercentWorthToBank(Player player)
        {
            uint tenPercent = (uint)Math.Round(player.getNetWorth * 0.10);  // Calculate 10% of Player's money

            if (player.getMoney >= tenPercent)              // Check if player has enough money to pay 10%
            {
                currentTurnsPlayers.PlayerPaysBank(tenPercent);                 // Player pays bank 10%

                if (Game1.DEBUG)
                {
                    Game1.debugMessageQueue.addMessageToQueue("Player " + "\"" + currentTurnsPlayers.getName + "\"" + " pays $" + tenPercent + " in taxes");
                    Console.WriteLine("Player " + "\"" + currentTurnsPlayers.getName + "\"" + " pays $" + tenPercent + " in taxes");
                }

                return true;
            }
            else
            {
                if (Game1.DEBUG)
                {
                    Game1.debugMessageQueue.addMessageToQueue("Player " + "\"" + currentTurnsPlayers.getName + "\"" + " needs to pay $" + tenPercent + " but does not have enough money");
                    Console.WriteLine("Player " + "\"" + currentTurnsPlayers.getName + "\"" + " needs to pay $" + tenPercent + " but does not have enough money");
                }

                return false;
            }


                
        }
    }
}

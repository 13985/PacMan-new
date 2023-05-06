using System.Collections.Generic;
using UnityEngine;

public interface IPlayer_manager{
    void CreatePlayerButton();//button
    void ReturnLoginButton();//button
    void DeletePlayerButton();//button
    void ChangePasswordButton();//button
    void Reset();//main_manager
    void PlayerLoginButton();//button
    void ChangePassword();//button
    void DeletePlayer();//button
    void CreatePlayer();//button
}//interface of player account control page

public interface Imain_manager{
    void Quit();//button
    void SelectMap();//button
    void ReturnTitle();//button
    void UnLoadAllCanvas();//button+game_manager
    void LogOff();//button
    void HelpPage();//button
    void LoadMap();//button+game_manager
    void ClearData();//button
    void ClearData(int index);//game_manager

    int GetHighestScores(int mapIndex);//game_manager
    void SaveScoreAndLive(int mapIndex,int scores,int lives);//game_manager
    void LoginSuccess(in string name);//Playe_manager
} //interface of game map selection screen


public interface Igame_manager{
    void EatPellet();//pacman
    bool EatPacman();//pacman
    void EatGhost();//pacman
    void EatEnergizer(GameObject node);//pacman
} //interface of gameplaying screen


public interface Ipacman_control{
    void LevelUp();//game_manager
} // increase the level of pacman and ghosts


public interface Ighost{
    void LevelUp();//game_manager
    bool BeingEaten();//pacman
    void SetEdible();//gama_manager
    void UnsetEdible();//game_manager
} //different function of ghosts


//
//  Game.h
//  GridInfect
//
//  Created by Christopher Mahar on 4/2/14.
//
//

#ifndef __GridInfect__Game__
#define __GridInfect__Game__

#include <iostream>
#include <vector>
#include "Level.h"
#include "Repel.h"
#include "Enums.h"
#include "EventHandler.h"

#include "cocos2d.h"

class Game
{
public:
    
    static Game* getInstance();
    
    GameMode getMode();
    Level* getLevel();
    int getLevelId();
    int getLevelIndex();
    void setLevel(int levelId);
    void setLevels(std::vector<Level*>* newLevels, Difficulty difficulty);
    bool nextLevel();
    bool nextFreePlayLevel();
    Difficulty getDifficulty();
    void setPiece(int index, int i, int j);
    void clearPiece(int index);
    void delayThenCheckForWin();
    void fullReset();
    int getClassicMenuPage();
    void setClassicMenuPage(int value);
    
private:
    Game();
    Game(Game const&){};
    void operator=(Game const&){};
    static Game* _instance;
    
    void propagatePiece(Piece* piece, bool fireEvents);
    bool propagatePiece(Piece* piece, bool fireEvents, int offset);
    void propagateRepel(Repel* repel);
    bool changeBoard(int i, int j, int value, bool fireEvents);
    int getBoardPosition(int i, int j);
    bool checkForWin();
    void resetBoard(int piece_i, int piece_j);
    
};

#endif /* defined(__GridInfect__Game__) */

//
//  Game.cpp
//  GridInfect
//
//  Created by Christopher Mahar on 4/2/14.
//
//

#include "Game.h"
#include "cocos2d.h"
#include <unistd.h>

Game* Game::_instance = NULL;
Game* Game::getInstance()
{
    if (!_instance)
    {
        _instance = new Game;
    }
    return _instance;
}

GameMode _gameMode;
int _levelId;
int _levelIndex;
int _classicMenuPage;

std::vector<Level*> _levels;
Difficulty _difficulty;

std::vector<Repel *> _repelsToRun;
bool _resetTripped;

Game::Game()
{
    _levels = std::vector<Level *>();
    _levels.push_back(new Level(0));
    _levelIndex = 0;
    _repelsToRun = std::vector<Repel *>();
    _classicMenuPage = 0;
}

int Game::getClassicMenuPage()
{
    return _classicMenuPage;
}

void Game::setClassicMenuPage(int value)
{
    _classicMenuPage = value;
}

GameMode Game::getMode()
{
    return _gameMode;
}

Level* Game::getLevel()
{
    return _levels.at(_levelIndex);
}

int Game::getLevelId()
{
    return _levelId;
}

int Game::getLevelIndex()
{
    return _levelIndex;
}

void Game::setLevel(int levelId)
{
    _gameMode = Classic;
    _levelId = levelId;
    _levelIndex = 0;
    _levels.clear();
    _levels.push_back(new Level(levelId));
}

void Game::setLevels(std::vector<Level*>* newLevels, Difficulty difficulty)
{
    _gameMode = FreePlay;
    _difficulty = difficulty;
    _levelId = 0;
    _levels.clear();
    for (int i = 0; i < newLevels->size(); i++) {
        _levels.push_back(newLevels->at(i));
    }
    _levelIndex = 0;
}

bool Game::nextLevel()
{
    int nextLevelId = getLevelId() + 1;
    setLevel(nextLevelId);
    return true;
}

bool Game::nextFreePlayLevel()
{
    _levelIndex++;
    if (_levelIndex >= _levels.size())
    {
        _levelIndex--;
        return false;
    }
    return true;
}

Difficulty Game::getDifficulty()
{
    return _difficulty;
}

void Game::setPiece(int index, int i, int j)
{
    //first clear any set pieces on this index
    clearPiece(index);
    
    _repelsToRun.clear();
    _resetTripped = false;

    Piece* piece = getLevel()->Pieces->at(index);
    piece->placed = true;
    piece->i = i;
    piece->j = j;
    
    //Propagate
    propagatePiece(piece, true);
}

void Game::clearPiece(int index)
{
    Piece* piece = getLevel()->Pieces->at(index);
    if (piece->placed) {
        resetBoard(piece->i, piece->j);
        
        piece->placed = false;
        piece->i = -1;
        piece->j = -1;
        
        std::vector<Piece *>::iterator i;
        for(i=getLevel()->Pieces->begin(); i != getLevel()->Pieces->end(); ++i)
        {
            Piece* placedPiece = (*i);
            if (placedPiece->placed) {
                propagatePiece(placedPiece, false);
                delayThenCheckForWin();
            }
        }
    
        for (int i = 0; i < Level::Height; i++) //y
        {
            for (int j = 0; j < Level::Width; j++) //x
            {
                int loc = i * Level::Width + j;
                if (getLevel()->Board[loc] != 0) {
                    if (getLevel()->Board[loc] == 99) {
                        getLevel()->Board[loc] = 1;
                    }
                    EventHandler::getInstance()->onChangeBoardIndex(i, j, getLevel()->Board[loc]);
                }
            }
        }
    }
}

bool _lStopped = false;
bool _rStopped = false;
bool _uStopped = false;
bool _dStopped = false;

void Game::propagatePiece(Piece* piece, bool fireEvents)
{
    changeBoard(piece->i, piece->j, 4, fireEvents);
    
    _lStopped = false;
    _rStopped = false;
    _uStopped = false;
    _dStopped = false;
    
    bool delay = false;
    for (int offset = 1; offset <= 10; offset++)
    {
        delay = propagatePiece(piece, fireEvents, offset);
    }
}

bool Game::propagatePiece(Piece* piece, bool fireEvents, int offset) {
    bool pause = false;
    Tile tile = piece->getTile();
    if (!_lStopped && (tile == L
                       || tile == LR || tile == LU || tile == LD
                       || tile == LRU || tile == LRD || tile == LUD
                       || tile == LRUD)) { //Any L
        
        int i = piece->i;
        int j = piece->j - offset;
        int bp = getBoardPosition(i, j);
        if (bp == 2) {
            _lStopped = true;
        } else if (bp == 3) {
            _lStopped = true;
            _repelsToRun.push_back(new Repel(i, j, R));
        } else if (bp == 5) {
            _lStopped = true;
            _resetTripped = true;
        } else if (bp == 99) {
        } else {
            pause |= changeBoard(i, j, 4, fireEvents);
        }
    }
    
    if (!_rStopped && (tile == R
                       || tile == LR || tile == RU || tile == RD
                       || tile == LRU || tile == LRD || tile == RUD
                       || tile == LRUD)) { //Any R
        
        int i = piece->i;
        int j = piece->j + offset;
        int bp = getBoardPosition(i, j);
        if (bp == 2) {
            _rStopped = true;
        } else if (bp == 3) {
            _rStopped = true;
            _repelsToRun.push_back(new Repel(i, j, L));
        } else if (bp == 5) {
            _rStopped = true;
            _resetTripped = true;
        } else if (bp == 99) {
        } else {
            pause |= changeBoard(i, j, 4, fireEvents);
        }
    }
    
    if (!_uStopped && (tile == U
                       || tile == LU || tile == RU || tile == UD
                       || tile == LRU || tile == LUD || tile == RUD
                       || tile == LRUD)) { //Any U
        
        int i = piece->i - offset;
        int j = piece->j;
        int bp = getBoardPosition(i, j);
        if (bp == 2) {
            _uStopped = true;
        } else if (bp == 3) {
            _uStopped = true;
            _repelsToRun.push_back(new Repel(i, j, D));
        } else if (bp == 5) {
            _uStopped = true;
            _resetTripped = true;
        } else if (bp == 99) {
        } else {
            pause |= changeBoard(i, j, 4, fireEvents);
        }
    }
    
    if (!_dStopped && (tile == D
                       || tile == LD || tile == RD || tile == UD
                       || tile == LRD || tile == LUD || tile == RUD
                       || tile == LRUD)) { //Any D
        
        int i = piece->i + offset;
        int j = piece->j;
        int bp = getBoardPosition(i, j);
        if (bp == 2) {
            _dStopped = true;
        } else if (bp == 3) {
            _dStopped = true;
            _repelsToRun.push_back(new Repel(i, j, U));
        } else if (bp == 5) {
            _dStopped = true;
            _resetTripped = true;
        } else if (bp == 99) {
        } else {
            pause |= changeBoard(i, j, 4, fireEvents);
        }
    }
    
    return fireEvents && pause;
}

void Game::propagateRepel(Repel* repel)
{
    for (int offset = 1; offset <= 10; offset++)
    {
        int i = 0;
        int j = 0;
        switch (repel->direction) {
			case L:
				i = repel->i;
				j = repel->j - offset;
				break;
			case R:
				i = repel->i;
				j = repel->j + offset;
				break;
			case U:
				i = repel->i - offset;
				j = repel->j;
				break;
			case D:
				i = repel->i + offset;
				j = repel->j;
				break;
			default:
				break;
        }
        int bp = getBoardPosition(i, j);
        
        for (int k = 0; k < getLevel()->Pieces->size(); k++)
        {
            Piece* piece = getLevel()->Pieces->at(k);
            if (piece->placed)
            {
                if (piece->i == i && piece->j == j)
                {

                    return; //hit a piece: we are done
                }
            }
        }
        
        if (bp == 4) {
            std::cout << "Repel Change:" << i << "," << j << std::endl;
            changeBoard(i, j, 1, true);
        }
    }
}

bool Game::changeBoard(int i, int j, int value, bool fireEvents)
{
    Level* level = getLevel();
    if (i < 0 || i >= Level::Height)
    {
        return false;
    }
    if (j < 0 || j >= Level::Width)
    {
        return false;
    }
    
    int loc = i * Level::Width + j;
    
    if (level->Board[loc] != 0 && level->Board[loc] != value)
    {
        level->Board[loc] = value;
        
        //Fire change event
        if (fireEvents) {
            EventHandler::getInstance()->onChangeBoardIndex(i, j, value);
        }
        
        return true;
    }
    return false;
}

int Game::getBoardPosition(int i, int j)
{
    Level* level = getLevel();
    
    if (i < 0 || i >= Level::Height)
    {
        return -1;
    }
    if (j < 0 || j >= Level::Width)
    {
        return -1;
    }
    
    int loc = i * Level::Width + j;
    return level->Board[loc];
}

bool Game::checkForWin()
{
    Level* level = getLevel();
    for (int i = 0; i < Level::Height; i++) //y
    {
        for (int j = 0; j < Level::Width; j++) //x
        {
            int loc = i * Level::Width + j;
            if (level->Board[loc] == 1)
            {
                return false;
            }
        }
    }
    return true;
}

void Game::resetBoard(int piece_i, int piece_j)
{
    Level* level = getLevel();
    for (int i = 0; i < Level::Height; i++) //y
    {
        for (int j = 0; j < Level::Width; j++) //x
        {
            int loc = i * Level::Width + j;
            if (i == piece_i || j == piece_j)
            {
                if (level->Board[loc] == 1)
                {
                    level->Board[loc] = 99;
                } else if (level->Board[loc] == 4)
                {
                    level->Board[loc] = 1;
                }
            }
        }
    }
}

void Game::delayThenCheckForWin()
{
    //Check for win and set
    bool win = checkForWin();
    Level* level = getLevel();
    level->setSolved(win);
    
    if (level->isSolved())
    {
        EventHandler::getInstance()->onLevelSolved();
    }
    else
    {
        if (_resetTripped)
        {
            fullReset();
        }
        else
        {
            for (int i = 0; i < _repelsToRun.size(); i++)
            {
                Repel* repel = _repelsToRun.at(i);
                propagateRepel(repel);
            }
        }
    }
}

void Game::fullReset()
{
    Level* level = getLevel();
    for (int i = 0; i < Level::Height; i++) //y
    {
        for (int j = 0; j < Level::Width; j++) //x
        {
            int loc = i * Level::Width + j;
            if (level->Board[loc] == 4) {
                level->Board[loc] = 1;
                EventHandler::getInstance()->onChangeBoardIndex(i, j, level->Board[loc]);
            }
        }
    }
    for (int i = 0; i < level->Pieces->size(); i++)
    {
        Piece* piece = level->Pieces->at(i);
        piece->i = -1;
        piece->j = -1;
        piece->placed = false;
    }
    EventHandler::getInstance()->onUnbindPieces();
}
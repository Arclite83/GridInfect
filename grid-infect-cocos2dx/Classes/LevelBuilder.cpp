//
//  LevelBuilder.cpp
//  GridInfect
//
//  Created by Christopher Mahar on 4/5/14.
//
//

#include "LevelBuilder.h"
#include "Enums.h"
#include "Level.h"
#include "Piece.h"
#include "cocos2d.h"
#include <cstdlib>
#include <iostream>
#include <vector>
#include <ctime>
using namespace std;

bool LevelBuilder::_seedset;

Level* LevelBuilder::generateLevel(Difficulty difficulty)
{
    if (!_seedset)
    {
        _seedset = true;
        struct cocos2d::cc_timeval now;
        cocos2d::CCTime::gettimeofdayCocos2d(&now, NULL);
    

        int seed = (now.tv_sec * 1000 + now.tv_usec / 1000);// * getpid();
        srand(seed);
    }
    
    switch (difficulty) {
        case Beginner:
            return generateLevel(difficulty, 2, 3, 5, 1, 5);
        case Easy:
            return generateLevel(difficulty, 3, 3, 6, 1, 5);
        case Medium:
            return generateLevel(difficulty, 4, 2, 7, 0, 6);
        case Hard:
            return generateLevel(difficulty, 4, 0, 11, 0, 6);
        case Challenging:
            return generateLevel(difficulty, 5, 0, 11, 0, 6);
        default:
            return NULL;
    }
}

std::string LevelBuilder::a() {
//case 99: {
//    Pieces->push_back(new Piece(LD));
//    Pieces->push_back(new Piece(LRU));
//    Pieces->push_back(new Piece(UD));
//    Pieces->push_back(new Piece(LR));
//    Pieces->push_back(new Piece(L));
//    int board_temp[] = {
//        0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0,
//        0, 1, 0, 0, 0, 0, 0, 1, 1, 0, 0,
//        0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0,
//        1, 1, 0, 1, 0, 1, 1, 1, 1, 0, 1,
//        0, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0,
//        0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
//    for (int i = 0; i < Level::Height * Level::Width; i++)
//    {
//        Board[i] = board_temp[i];
//    }
//    break;
//}
    std::stringstream stream;
    
    char *tileString[] = {
        "L",
        "R",
        "U",
        "D",
        "LR",
        "LU",
        "LD",
        "RU",
        "RD",
        "UD",
        "LRU",
        "LRD",
        "LUD",
        "RUD",
        "LRUD"
    };
    
    for (int l = 99; l < 128; l++) {
        stream << "case " << l << ": {" << '\n';
        
        Level* level = generateLevel(Challenging, 5, 0, 11, 0, 6);
        
        for (int j = 0; j < level->Pieces->size(); j++) {
            Tile tile = level->Pieces->at(j)->getTile();
            stream << "    Pieces->push_back(new Piece(" << tileString[tile] << "));" << '\n';
        }
        
        stream << "    int board_temp[] = {" << '\n';
        
        for (int i = 0; i < Level::Height; i++) //y
        {
            stream << "        ";
            for (int j = 0; j < Level::Width; j++) //x
            {
                int loc = i * Level::Width + j;
                stream << level->Board[loc];
                if (loc < Level::Width * Level::Height - 1) {
                    stream << ", ";
                }
            }
            stream << '\n';
        }
        stream << "    };" << '\n';
        
        stream << "    for (int i = 0; i < Level::Height * Level::Width; i++)" << '\n';
        stream << "    {" << '\n';
        stream << "        Board[i] = board_temp[i];" << '\n';
        stream << "    }" << '\n';
        stream << "    break;" << '\n';
        stream << "}" << '\n';
    }
    
    return stream.str();
}

Level* LevelBuilder::generateLevel(Difficulty difficulty, int piecesToSet,
                            int initial_xOffset, int initial_xCount,
                            int initial_yOffset, int initial_yCount) {
    
    Level* level = new Level();
    
    for (int l = 0; l < piecesToSet; l++) {
        Tile tile = LRUD;
        int x = -1;
        int y = -1;
        
        bool overlap = true;
        while (overlap) {
            tile = (Tile) (rand() % 15);
            int xOffset = initial_xOffset;
            int xCount = initial_xCount;
            int yOffset = initial_yOffset;
            int yCount = initial_yCount;
            
            //check if space is taken
            overlap = false;
            
            if (difficulty == Beginner && (tile == LRU ||
                                           tile == LRD ||
                                           tile == LUD ||
                                           tile == RUD ||
                                           tile == LRUD)) {
                //Skip more complex tiles for Beginner
                overlap = true;
            }
            else if (difficulty == Challenging && (tile == LR ||
                                                   tile == UD)) {
                //Skip LR and UD for Challenging
                overlap = true;
            }
            
            //Keep tiles unique(?)
            for (int k = 0; k < level->Pieces->size(); k++)
            {
                Piece* piece = level->Pieces->at(k);
                if (piece->getTile() == tile)
                {
                    overlap = true;
                }
            }
            
            switch (tile) {
                case L:
                    xOffset += 2;
                    xCount -= 2;
                    break;
                case R:
                    xCount -= 2;
                    break;
                case U:
                    yOffset += 2;
                    yCount -= 2;
                    break;
                case D:
                    yCount -= 2;
                    break;
                case LR:
                    xOffset += 2;
                    xCount -= 4;
                    break;
                case LU:
                    xOffset += 2;
                    xCount -= 2;
                    yOffset += 2;
                    yCount -= 2;
                    break;
                case LD:
                    xOffset += 2;
                    xCount -= 2;
                    yCount -= 2;
                    break;
                case RU:
                    xCount -= 2;
                    yOffset += 2;
                    yCount -= 2;
                    break;
                case RD:
                    xCount -= 2;
                    yCount -= 2;
                    break;
                case UD:
                    yOffset += 2;
                    yCount -= 2;
                    break;
                case LRU:
                    xOffset += 2;
                    xCount -= 4;
                    yOffset += 2;
                    yCount -= 2;
                    break;
                case LRD:
                    xOffset += 2;
                    xCount -= 4;
                    yCount -= 2;
                    break;
                case LUD:
                    xOffset += 2;
                    xCount -= 2;
                    yOffset += 2;
                    yCount -= 4;
                    break;
                case RUD:
                    xCount -= 2;
                    yOffset += 2;
                    yCount -= 4;
                    break;
                case LRUD:
                    xOffset += 2;
                    xCount -= 4;
                    yOffset += 2;
                    yCount -= 4;
                    break;
            }
            
            x = xOffset + (rand() % xCount);
            y = yOffset + (rand() % yCount);
            
            for (int k = 0; k < level->Pieces->size(); k++)
            {
                Piece* piece = level->Pieces->at(k);
                if (piece->i == y && piece->j == x) {
                    overlap = true;
                }
                
                //check if even on same col/row?
                //can prevent 'less than optimal' solutions from appearing.
                if (piece->i == y || piece->j == x) {
                    overlap = true;
                }
            }
        }
        
        Piece* newPiece = new Piece(tile);
        newPiece->i = y;
        newPiece->j = x;
        level->Pieces->push_back(newPiece);
        
        LevelBuilder::buildBoard(level, newPiece);
    }
    
    for (int k = 0; k < level->Pieces->size(); k++)
    {
        Piece* piece = level->Pieces->at(k);
        piece->i = -1;
        piece->j = -1;
    }
    
    return level;
}

void LevelBuilder::buildBoard(Level* level, Piece* piece)
{   
    int loc = piece->i * Level::Width + piece->j;
    level->Board[loc] = 1;
    
    bool lStopped = false;
    bool rStopped = false;
    bool uStopped = false;
    bool dStopped = false;
    
    
    Tile tile = piece->getTile();
    for (int offset = 1; offset <= 10; offset++) { //FOUND IT: RUNS OVER LINES, AND OUT OF ARRAY
        if (!lStopped && (tile == L
                          || tile == LR || tile == LU || tile == LD
                          || tile == LRU || tile == LRD || tile == LUD
                          || tile == LRUD)) { //Any L
            
            int i = piece->i;
            int j = piece->j - offset;
            
            if (i < 0 || i >= Level::Height ||
                j < 0 || j >= Level::Width)
            {
            }
            else
            {
                int l = i * Level::Width + j;
                try
                {
                    if ((rand() % 20) - offset > 4)
                    {
                        level->Board[l] = 1;
                    }
                }
                catch (std::exception ex)
                {
                }
            }
        }
        
        if (!rStopped && (tile == R
                          || tile == LR || tile == RU || tile == RD
                          || tile == LRU || tile == LRD || tile == RUD
                          || tile == LRUD)) { //Any R
            
            int i = piece->i;
            int j = piece->j + offset;
            
            if (i < 0 || i >= Level::Height ||
                j < 0 || j >= Level::Width)
            {
            }
            else
            {
                int l = i * Level::Width + j;
                try
                {
                    if ((rand() % 20) - offset > 4)
                    {
                        level->Board[l] = 1;
                    }
                } catch (std::exception ex)
                {
                }
            }
        }
        
        if (!uStopped && (tile == U
                          || tile == LU || tile == RU || tile == UD
                          || tile == LRU || tile == LUD || tile == RUD
                          || tile == LRUD)) { //Any U
            
            int i = piece->i - offset;
            int j = piece->j;
            
            if (i < 0 || i >= Level::Height ||
                j < 0 || j >= Level::Width)
            {
            }
            else
            {
                int l = i * Level::Width + j;
                try
                {
                    if ((rand() % 20) - offset > 4)
                    {
                        level->Board[l] = 1;
                    }
                } catch (std::exception ex)
                {
                
                }
            }
        }
        
        if (!dStopped && (tile == D
                          || tile == LD || tile == RD || tile == UD
                          || tile == LRD || tile == LUD || tile == RUD
                          || tile == LRUD)) { //Any D
            
            int i = piece->i + offset;
            int j = piece->j;
            
            
            if (i < 0 || i >= Level::Height ||
                j < 0 || j >= Level::Width)
            {
            }
            else
            {
                int l = i * Level::Width + j;
                try
                {
                    if ((rand() % 20) - offset > 4)
                    {
                        level->Board[l] = 1;
                    }
                }
                catch (std::exception ex)
                {
                
                }
            }
        }
    }
}

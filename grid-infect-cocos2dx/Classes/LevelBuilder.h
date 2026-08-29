//
//  LevelBuilder.h
//  GridInfect
//
//  Created by Christopher Mahar on 4/4/14.
//
//

#ifndef __GridInfect__LevelBuilder__
#define __GridInfect__LevelBuilder__

#include "Enums.h"
#include "Level.h"
#include "Piece.h"

class LevelBuilder
{
public:
    static std::string a();
    static Level* generateLevel(Difficulty difficulty);
    static Level* generateLevel(Difficulty difficulty, int piecesToSet,
                         int initial_xOffset, int initial_xCount,
                         int initial_yOffset, int initial_yCount);
   static void buildBoard(Level* level, Piece* piece);
    
   static bool _seedset;
};

#endif /* defined(__GridInfect__LevelBuilder__) */

//
//  Level.h
//  GridInfect
//
//  Created by Christopher Mahar on 4/1/14.
//
//

#ifndef __GridInfect__Level__
#define __GridInfect__Level__

#include "Piece.h"
#include <iostream>
#include <vector>

class Level
{
public:
    const static int Count = 128;
    const static int Height = 6;
    const static int Width = 11;
    
    Level(int level);
    Level();
    
    std::vector<Piece*>* Pieces;
    int Board[Width*Height];
    
    bool isSolved();
    void setSolved(bool solved);
    
private:
    void initByLevel(int level);
};

#endif /* defined(__GridInfect__Level__) */
